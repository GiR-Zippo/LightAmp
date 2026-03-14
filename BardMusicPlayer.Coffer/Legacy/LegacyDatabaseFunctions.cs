/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe, isaki
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Coffer.Interfaces;
using BardMusicPlayer.Transmogrify.Song;
using LiteDB;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;


namespace BardMusicPlayer.Coffer.Legacy
{
    internal class LegacyDatabaseFunctions : IDatabaseFunctions
    {
        private LiteDatabase dbi = null;
        public void SetDatabase(LiteDatabase database)
        {
            dbi = database;
        }

        #region Playlist
        public IPlaylist CreatePlaylistFromTag(string tag)
        {
            var songCol = GetSongCollection();

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
            var playlists = GetPlaylistCollection();

            // We guarantee uniqueness in index and code, therefore
            // there should be one and only one list.
            var dbList = playlists.Query()
                .Include(static x => x.Songs)
                .Where(x => x.Name == name)
                .Single();

            return dbList != null ? new BmpPlaylistDecorator(dbList) : null;
        }

        public IList<string> GetPlaylistNames()
        {
            var playlists = GetPlaylistCollection();

            // Want to ensure we don't pull in the trackchunk data.
            return playlists.Query()
                .Select(static x => x.Name)
                .ToList();
        }

        public void SavePlaylist(IPlaylist songList)
        {
            var playlists = GetPlaylistCollection();

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
            var playlists = GetPlaylistCollection();

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
            var songCol = GetSongCollection();
            return songCol.FindOne(x => x.Title == title);
        }

        public IList<string> GetSongTitles()
        {
            var songCol = GetSongCollection();

            return songCol.Query()
                .Select(x => x.Title)
                .ToList();
        }

        public bool IsSongInDatabase(BmpSong song, bool strict = true)
        {
            var songCol = GetSongCollection();
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
            var songCol = GetSongCollection();
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
            if ((from x in GetPlaylistCollection().Query().ToArray()
                 from y in x.Songs
                 where y.Id.Equals(song.Id)
                 select y.Id).Count() > 1)
                return;

            //if not, remove it
            var songCol = GetSongCollection();
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
                List<ObjectId> differenceQuery = GetSongCollection().Query().Select(x => x.Id).ToList()
                                                 .Except(from x in GetPlaylistCollection().Query().ToArray()
                                                         from y in x.Songs
                                                         select y.Id).ToList();
                //and remove them
                foreach (var id in differenceQuery)
                    GetSongCollection().Delete(id);

                differenceQuery.Clear();

                dbi.Checkpoint();
                dbi.Rebuild();
            }
            catch { }
        }

        public void Export(string filename)
        {
            var t = new LiteDatabase(filename);
            var names = dbi.GetCollectionNames();
            foreach (var name in names)
            {
                var col2 = dbi.GetCollection(name);
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
            return dbi.GetCollection<BmpPlaylist>(Constants.PLAYLIST_COL_NAME);
        }

        /// <summary>
        /// Utility method.
        /// </summary>
        /// <returns></returns>
        private ILiteCollection<BmpSong> GetSongCollection()
        {
            return dbi.GetCollection<BmpSong>(Constants.SONG_COL_NAME);
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
    }
}
