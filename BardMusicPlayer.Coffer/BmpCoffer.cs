/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe, isaki
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Coffer.DatabaseFunctions;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Transmogrify.Song;
using LiteDB;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BardMusicPlayer.Coffer
{
    public sealed class BmpCoffer : IDisposable
    {
        private static BmpCoffer _instance;

        /// <summary>
        /// Initializes the default coffer instance
        /// </summary>
        /// <param name="filename">full path to the coffer file</param>
        public static void Initialize(string filename)
        {
            if (Initialized) return;
            _instance = CreateInstance(filename);
        }

        /// <summary>
        /// Returns true if the default coffer instance is initialized
        /// </summary>
        public static bool Initialized => _instance != null;

        /// <summary>
        /// Gets the default coffer instance
        /// </summary>
        public static BmpCoffer Instance => _instance ?? throw new BmpCofferException("This coffer must be initialized first.");

        /// <summary>
        /// Database functions to we use
        /// </summary>
        private IDatabaseFunctions DatabaseFunctions { get; set; } = null;
        private readonly LiteDatabase dbi;
        private bool disposedValue;

        /// <summary>
        /// Internal constructor; this object is constructed with a factory pattern.
        /// </summary>
        /// <param name="dbi"></param>
        private BmpCoffer(LiteDatabase dbi)
        {
            this.dbi = dbi;
            this.disposedValue = false;

            DatabaseFunctions = new LegacyDatabaseFunctions();
            DatabaseFunctions.SetDatabase(this.dbi);
        }

        #region MainRoutines: Create / Load / Save / CleanUp
        /// <summary>
        /// Generates the <see cref="BsonMapper"/>
        /// </summary>
        /// <returns> <see cref="BsonMapper"/> </returns>
        internal static BsonMapper GenerateMapper()
        {
            var mapper = new BsonMapper();
            mapper.RegisterType(static group => group.Index, static bson => Instrument.Parse(bson.AsInt32));
            mapper.RegisterType(static group => group.Index, static bson => InstrumentTone.Parse(bson.AsInt32));
            mapper.RegisterType(static group => group.Index, static bson => OctaveRange.Parse(bson.AsInt32));
            mapper.RegisterType(static tempoMap => SerializeTempoMap(tempoMap), static bson => DeserializeTempoMap(bson.AsBinary));
            mapper.RegisterType(static trackChunk => SerializeTrackChunk(trackChunk), static bson => DeserializeTrackChunk(bson.AsBinary));
            return mapper;
        }

        /// <summary>
        /// Create a new instance of the <see cref="BmpCoffer"/> manager based on the given LiteDB database.
        /// </summary>
        /// <param name="dbPath"></param>
        /// <returns> <see cref="BmpCoffer"/> </returns>
        internal static BmpCoffer CreateInstance(string dbPath)
        {
            var dbi = new LiteDatabase(@"filename=" + dbPath + "; journal = false", GenerateMapper());
            MigrateDatabase(dbi);

            return new BmpCoffer(dbi);
        }

        /// <summary>
        /// Loads a LiteDB database from file
        /// </summary>
        /// <param name="file"></param>
        public void LoadNew(string file)
        {
            this.dbi.Dispose();
            var dbi = new LiteDatabase(@"filename=" + file + "; journal = false", GenerateMapper()); //turn journal off, for big containers
            MigrateDatabase(dbi);

            _instance = new BmpCoffer(dbi);
            return;
        }

        /// <summary>
        /// Exports the current LiteDB database to a new file
        /// </summary>
        /// <param name="filename"></param>
        public void Export(string filename)
        {
            DatabaseFunctions.Export(filename);
        }

        /// <summary>
        /// Cleans the database
        /// <para/>
        /// Removes unbound songs and rebuild the LiteDB.
        /// </summary>
        public void CleanUpDB()
        {
            DatabaseFunctions.CleanUpDB();
        }
        #endregion

        #region Serializations
        /// <summary>
        /// Serializes a TempoMap from DryWetMidi.
        /// </summary>
        /// <param name="tempoMap"></param>
        /// <returns></returns>
        private static byte[] SerializeTempoMap(TempoMap tempoMap)
        {
            var midiFile = new MidiFile(new TrackChunk());
            midiFile.ReplaceTempoMap(tempoMap);
            using var memoryStream = new MemoryStream();
            midiFile.Write(memoryStream);
            var bson = memoryStream.ToArray();
            memoryStream.Dispose();
            return bson;
        }

        /// <summary>
        /// Deserializes a TempoMap from DryWetMidi.
        /// </summary>
        /// <param name="bson"></param>
        /// <returns></returns>
        private static TempoMap DeserializeTempoMap(byte[] bson)
        {
            using var memoryStream = new MemoryStream(bson);
            var midiFile = MidiFile.Read(memoryStream);
            var tempoMap = midiFile.GetTempoMap().Clone();
            memoryStream.Dispose();
            return tempoMap;
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
        #endregion

        #region Playlist
        /// <summary>
        /// This creates a playlist containing songs that match the given tag.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns><see cref="IPlaylist"/></returns>
        public IPlaylist CreatePlaylistFromTag(string tag)
        {
            return DatabaseFunctions.CreatePlaylistFromTag(tag);
        }

        /// <summary>
        /// This creates a new empty playlist with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns><see cref="IPlaylist"/></returns>
        public IPlaylist CreatePlaylist(string name)
        {
            return DatabaseFunctions.CreatePlaylist(name);
        }

        /// <summary>
        /// This retrieves a playlist with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The playlist if found or null if no matching playlist exists.</returns>
        public IPlaylist GetPlaylist(string name)
        {
            return DatabaseFunctions.GetPlaylist(name);
        }

        /// <summary>
        /// This retrieves the names of all saved playlists.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetPlaylistNames()
        {
            return DatabaseFunctions.GetPlaylistNames();
        }

        /// <summary>
        /// This saves a playlist.
        /// </summary>
        /// <param name="songList"></param>
        /// <exception cref="BmpCofferException">This is thrown if a name conflict occurs on save.</exception>
        public void SavePlaylist(IPlaylist songList)
        {
            DatabaseFunctions.SavePlaylist(songList);
        }

        /// <summary>
        /// This deletes a playlist.
        /// </summary>
        /// <param name="songList"></param>
        public void DeletePlaylist(IPlaylist songList)
        {
            DatabaseFunctions.DeletePlaylist(songList);
        }
        #endregion

        #region Songs
        /// <summary>
        /// This retrieves the song with the given title.
        /// </summary>
        /// <param name="title"></param>
        /// <returns>The song if found or null if no matching song exists.</returns>
        public BmpSong GetSong(string title)
        {
            return DatabaseFunctions.GetSong(title);
        }

        /// <summary>
        /// This retrieves the titles of all saved songs.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetSongTitles()
        {
            return DatabaseFunctions.GetSongTitles();
        }

        /// Simple check if song is in database
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public bool IsSongInDatabase(BmpSong song, bool strict = true)
        {
            return DatabaseFunctions.IsSongInDatabase(song, strict);
        }

        /// <summary>
        /// This saves a song.
        /// </summary>
        /// <param name="song"></param>
        /// <exception cref="BmpCofferException">This is thrown if a title conflict occurs on save.</exception>
        public void SaveSong(BmpSong song)
        {
            DatabaseFunctions.SaveSong(song);
        }

        /// <summary>
        /// This deletes a song.
        /// </summary>
        /// <param name="song"></param>
        /// <exception cref="BmpCofferException">This is thrown if a name conflict occurs on save.</exception>
        public void DeleteSong(BmpSong song)
        {
            DatabaseFunctions.DeleteSong(song);
        }
        #endregion

        /// <summary>
        /// Generated by VS2019.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose method.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (disposedValue) return;
            if (disposing) dbi.Dispose();
            disposedValue = true;
        }

        /// <summary>
        /// Database creation/migration method.
        /// </summary>
        /// <param name="dbi"></param>
        internal static void MigrateDatabase(LiteDatabase dbi)
        {
            // This method exists to provide a way to have different versions of key
            // API objects, such as MMSong, and a way to migrate that data (or nuke it
            // if required).

            // Currently, we are version 1, so the only thing to do is to inject the requisite metadata.
            var schemaData = dbi.GetCollection<LiteDBSchema>(Constants.SCHEMA_COL_NAME);
            int dataCount = schemaData.Count();
            if (dataCount > 1)
            {
                throw new Exception("Invalid schema collection in database");
            }

            bool insertRequired;
            if (dataCount == 0)
            {
                insertRequired = true;
            }
            else
            {
                var result = schemaData.FindOne(static x => true);
                if (result.Id == Constants.SCHEMA_VERSION)
                {
                    insertRequired = false;
                }
                else
                {
                    schemaData.DeleteAll();
                    insertRequired = true;
                }
            }

            if (insertRequired)
            {
                var schema = new LiteDBSchema();
                schemaData.Insert(schema);
            }

            // Create the song collection and add indicies
            var songs = dbi.GetCollection<BmpSong>(Constants.SONG_COL_NAME);
            songs.EnsureIndex(static x => x.Title);
            songs.EnsureIndex(static x => x.Tags);

            // Create the custom playlist collection and add indicies
            var playlists = dbi.GetCollection<BmpPlaylist>(Constants.PLAYLIST_COL_NAME);
            playlists.EnsureIndex(static x => x.Name, unique: true);
        }
    }
}
