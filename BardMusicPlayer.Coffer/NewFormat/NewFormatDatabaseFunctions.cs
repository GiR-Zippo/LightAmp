using BardMusicPlayer.Coffer.Interfaces;
using BardMusicPlayer.Coffer.Legacy;
using BardMusicPlayer.Transmogrify.Song;
using LiteDB;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace BardMusicPlayer.Coffer.DatabaseFunctions;


public class NewFormatDatabaseFunctions : IDatabaseFunctions
{
    private LiteDatabase dbi = null;
    public void SetDatabase(LiteDatabase database)
    {
        this.dbi = database;
    }

    #region Playlist
    public IPlaylist CreatePlaylistFromTag(string tag)
    {
        var songCol = this.GetSongCollection();

        // TODO: This is brute force and not memory efficient; there has to be a better
        // way to do this, but my knowledge of LINQ and BsonExpressions isn't there yet.
        var allSongs = songCol.FindAll();
        var songList = allSongs.Where(entry => TagMatches(tag, entry)).ToList();

        if (songList.Count == 0)
            return null;

        var dbList = new BmpPlaylist()
        {
            Name = tag,
            Songs = songList,
            Id = null
        };

        return new BmpPlaylistDecorator(dbList);
    }

    public IPlaylist CreatePlaylist(string name)
    {
        var dbList = new BmpPlaylist()
        {
            Songs = new List<BmpSong>(),
            Name = name,
            Id = null
        };

        return new BmpPlaylistDecorator(dbList);
    }

    public IPlaylist GetPlaylist(string name)
    {
        var playlists = this.GetPlaylistCollection();

        // We guarantee uniqueness in index and code, therefore
        // there should be one and only one list.
        var dbList = playlists.Query()
            .Include(static x => x.Songs)
            .Where(x => x.Name == name)
            .Single();

        return (dbList != null) ? new BmpPlaylistDecorator(dbList) : null;
    }

    public IList<string> GetPlaylistNames()
    {
        var playlists = this.GetPlaylistCollection();

        // Want to ensure we don't pull in the trackchunk data.
        return playlists.Query()
            .Select<string>(static x => x.Name)
            .ToList();
    }

    public void SavePlaylist(IPlaylist songList)
    {
        var playlists = this.GetPlaylistCollection();
        var dbList = ((BmpPlaylistDecorator)songList).GetBmpPlaylist();
        try
        {
            if (dbList.Id == null)
            {
                dbList.Id = ObjectId.NewObjectId();
                playlists.Insert(dbList);
            }
            else
            {
                playlists.Update(dbList);
            }
        }
        catch (LiteException e)
        {
            throw new BmpCofferException(e.Message, e);
        }
    }

    public void DeletePlaylist(IPlaylist songList)
    {
        var playlists = this.GetPlaylistCollection();

        var dbList = ((BmpPlaylistDecorator)songList).GetBmpPlaylist();

        try
        {
            if (dbList.Id != null)
            {
                foreach (var song in dbList.Songs)
                    DeleteSong(song);
                playlists.Delete(dbList.Id);
            }
        }
        catch (LiteException e)
        {
            throw new BmpCofferException(e.Message, e);
        }
    }
    #endregion

    #region Songs
    public BmpSong GetSong(string title)
    {
        var songCol = this.GetSongCollection();
        BmpSong song = songCol.FindOne(x => x.Title == title);
        var files = dbi.FileStorage
            .Find(file => file.Id.StartsWith(song.Id.ToString()+"/"));
        foreach (var file in files)
            song.TrackContainers[Convert.ToInt32(file.Filename)].SourceTrackChunk = LoadChunks(file.Id);
        return song;
    }

    public IList<string> GetSongTitles()
    {
        var songCol = this.GetSongCollection();

        return songCol.Query()
            .Select<string>(x => x.Title)
            .ToList();
    }

    public bool IsSongInDatabase(BmpSong song, bool strict = true)
    {
        var songCol = this.GetSongCollection();
        IEnumerable<BmpSong> sList = null;
        if (strict)
            sList = songCol.Find(x => x.Title == song.Title);
        else
            sList = songCol.Find(x => x.Title.StartsWith(song.Title));

        bool inList = false;
        foreach (var s in sList)
        {
            if (s.TrackContainers.Count() != song.TrackContainers.Count())
            {
                for (int i = 0; i != s.TrackContainers.Count(); i++)
                {
                    if (s.TrackContainers[i].SourceTrackChunk.GetNotes().Count() == song.TrackContainers[i].SourceTrackChunk.GetNotes().Count())
                        inList = true;
                }
            }
            else
                inList = true;

            if (s.Duration.TotalMilliseconds != song.Duration.TotalMilliseconds)
                inList = true;
        }
        return inList;
    }

    public void SaveSong(BmpSong song)
    {
        var songCol = this.GetSongCollection();
        try
        {
            if (song.Id == null)
            {
                //TODO: Fix this if more than one song with the name exists
                var results = songCol.Find(x => x.Title.Equals(song.Title));
                if (results.Count() > 0)
                {
                    //Get the ID from the found song and update the data
                    song.Id = results.First().Id;
                    songCol.Update(song);
                    return;
                }

                song.Id = ObjectId.NewObjectId();

                //Serialize SourceTrackChunk
                foreach (var t in song.TrackContainers)
                {
                    SaveChunks(song.Id.ToString()+ "/SourceTrackChunk/"+t.Key.ToString(), t.Key.ToString(), SerializeTrackChunk(t.Value.SourceTrackChunk));
                    t.Value.SourceTrackChunk = null;
                }



                songCol.Insert(song);
            }
            else
                songCol.Update(song);
        }
        catch (LiteException e)
        {
            throw new BmpCofferException(e.Message, e);
        }
    }

    public void DeleteSong(BmpSong song)
    {
        //Check if the song is in use in other playlists
        if ((from x in this.GetPlaylistCollection().Query().ToArray()
             from y in x.Songs
             where y.Id.Equals(song.Id)
             select y.Id).Count() > 1)
            return;

        //if not, remove it
        var songCol = this.GetSongCollection();
        try
        {
            if (song.Id == null)
                return;

            var results = songCol.Find(x => x.Id.Equals(song.Id));
            if (results.Any())
                songCol.Delete(song.Id);
        }
        catch (LiteException e)
        {
            throw new BmpCofferException(e.Message, e);
        }
    }
    #endregion

    #region Utils
    public void CleanUpDB()
    {
        //Try it and catch if the log file can't be removed
        try
        {
            //Check if we have songs without a playlist
            List<ObjectId> differenceQuery = this.GetSongCollection().Query().Select(x => x.Id).ToList()
                                             .Except(from x in this.GetPlaylistCollection().Query().ToArray()
                                                     from y in x.Songs
                                                     select y.Id).ToList();
            //and remove them
            foreach (var id in differenceQuery)
                this.GetSongCollection().Delete(id);

            differenceQuery.Clear();

            this.dbi.Checkpoint();
            this.dbi.Rebuild();
        }
        catch { }
    }

    public void Export(string filename)
    {
        var t = new LiteDatabase(filename);
        var names = this.dbi.GetCollectionNames();
        foreach (var name in names)
        {
            var col2 = this.dbi.GetCollection(name);
            var col = t.GetCollection(name);
            try
            {
                col.InsertBulk(col2.FindAll());
            }
            catch { }
        }
        t.Dispose();
    }

    /// <summary>
    /// Utility method.
    /// </summary>
    /// <returns></returns>
    private ILiteCollection<BmpPlaylist> GetPlaylistCollection()
    {
        return this.dbi.GetCollection<BmpPlaylist>(Constants.PLAYLIST_COL_NAME);
    }

    /// <summary>
    /// Utility method.
    /// </summary>
    /// <returns></returns>
    private ILiteCollection<BmpSong> GetSongCollection()
    {
        return this.dbi.GetCollection<BmpSong>(Constants.SONG_COL_NAME);
    }

    /// <summary>
    /// Tag matching algorithm.
    /// </summary>
    /// <param name="search"></param>
    /// <param name="song"></param>
    /// <returns></returns>
    private static bool TagMatches(string search, BmpSong song)
    {
        var tags = song.Tags;
        return tags is { Count: > 0 } &&
               tags.Any(t => string.Equals(search, t, StringComparison.OrdinalIgnoreCase));
    }
    #endregion

    public void SaveChunks(string id, string filename, byte[] data)
    {
        if (data == null)
            return;
        var dbfs = dbi.FileStorage;
        dbfs.Upload(id, filename, new MemoryStream(data));
    }

    public TrackChunk LoadChunks(string filename)
    {
        var file = dbi.FileStorage.FindById(filename);
        using (var memoryStream = new MemoryStream())
        {
            file.OpenRead().CopyTo(memoryStream);
            return DeserializeTrackChunk(memoryStream.ToArray());
        }
    }

    /// <summary>
    /// Serializes a TrackChunk from DryWetMidi.
    /// </summary>
    /// <param name="trackChunk"></param>
    /// <returns></returns>
    private static byte[] SerializeTrackChunk(TrackChunk trackChunk)
    {
        var midiFile = new MidiFile(trackChunk);
        using var memoryStream = new MemoryStream();
        midiFile.Write(memoryStream);
        var bson = memoryStream.ToArray();
        memoryStream.Dispose();
        return bson;
    }

    /// <summary>
    /// Deserializes a TrackChunk from DryWetMidi.
    /// </summary>
    /// <param name="bson"></param>
    /// <returns></returns>
    private static TrackChunk DeserializeTrackChunk(byte[] bson)
    {
        using var memoryStream = new MemoryStream(bson);
        var midiFile = MidiFile.Read(memoryStream);
        //shouldn't happen, but in rare cases it does
        if (midiFile.GetTrackChunks().Count() <= 0)
            return new TrackChunk();
        //In case we have more than 1 chunk per track, combine them
        TrackChunk trackChunk = Melanchall.DryWetMidi.Core.TrackChunkUtilities.Merge(midiFile.GetTrackChunks());
        memoryStream.Dispose();
        return trackChunk;
    }

}
