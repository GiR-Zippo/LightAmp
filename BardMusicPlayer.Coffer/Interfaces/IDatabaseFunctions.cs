/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Transmogrify.Song;
using LiteDB;
using System.Collections.Generic;

namespace BardMusicPlayer.Coffer.Interfaces;

public interface IDatabaseFunctions
{
    public void SetDatabase(LiteDatabase database);

    #region Playlist
    /// <summary>
    /// This creates a playlist containing songs that match the given tag.
    /// </summary>
    /// <param name="tag"></param>
    /// <returns><see cref="IPlaylist"/></returns>
    public IPlaylist CreatePlaylistFromTag(string tag);

    /// <summary>
    /// This creates a new empty playlist with the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns><see cref="IPlaylist"/></returns>
    public IPlaylist CreatePlaylist(string name);

    /// <summary>
    /// This retrieves a playlist with the given name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>The playlist if found or null if no matching playlist exists.</returns>
    public IPlaylist GetPlaylist(string name);

    /// <summary>
    /// This retrieves the names of all saved playlists.
    /// </summary>
    /// <returns></returns>
    public IList<string> GetPlaylistNames();

    /// <summary>
    /// This saves a playlist.
    /// </summary>
    /// <param name="songList"></param>
    /// <exception cref="BmpCofferException">This is thrown if a name conflict occurs on save.</exception>
    public void SavePlaylist(IPlaylist songList);

    /// <summary>
    /// This deletes a playlist.
    /// </summary>
    /// <param name="songList"></param>
    public void DeletePlaylist(IPlaylist songList);
    #endregion

    #region Songs
    /// <summary>
    /// This retrieves the song with the given title.
    /// </summary>
    /// <param name="title"></param>
    /// <returns>The song if found or null if no matching song exists.</returns>
    public BmpSong GetSong(string title);

    /// <summary>
    /// This retrieves the titles of all saved songs.
    /// </summary>
    /// <returns></returns>
    public IList<string> GetSongTitles();

    /// Simple check if song is in database
    /// </summary>
    /// <param name="song"></param>
    /// <returns></returns>
    public bool IsSongInDatabase(BmpSong song, bool strict = true);

    /// <summary>
    /// This saves a song.
    /// </summary>
    /// <param name="song"></param>
    /// <exception cref="BmpCofferException">This is thrown if a title conflict occurs on save.</exception>
    public void SaveSong(BmpSong song);

    /// <summary>
    /// This deletes a song.
    /// </summary>
    /// <param name="song"></param>
    /// <exception cref="BmpCofferException">This is thrown if a name conflict occurs on save.</exception>
    public void DeleteSong(BmpSong song);
    #endregion

    #region Utils
    /// <summary>
    /// Cleans the database
    /// <para/>
    /// Removes unbound songs and rebuild the LiteDB.
    /// </summary>
    public void CleanUpDB();

    /// <summary>
    /// Exports the current LiteDB database to a new file
    /// </summary>
    /// <param name="filename"></param>
    public void Export(string filename);

    #endregion

}
