using BardMusicPlayer.Coffer;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Functions;
using System;
using System.Collections.Generic;

namespace BardMusicPlayer.Ui.Functions
{
    /// <summary>
    /// simplyfied functions both Ui are using
    /// </summary>
    public static class PlaylistFunctions
    {
        /// <summary>
        /// Creates and return a new playlist or return the existing one with the given name
        /// </summary>
        /// <param name="playlistname"></param>
        public static IPlaylist CreatePlaylist(string playlistname)
        {
            if (BmpCoffer.Instance.GetPlaylistNames().Contains(playlistname))
                return BmpCoffer.Instance.GetPlaylist(playlistname);
            return BmpCoffer.Instance.CreatePlaylist(playlistname);
        }

        /// <summary>
        /// Get a song fromt the playlist
        /// </summary>
        /// <param name="playlist"></param>
        /// <param name="songname"></param>
        public static BmpSong GetSongFromPlaylist(IPlaylist playlist, string songname)
        {
            if (playlist == null)
                return null;

            foreach (var item in playlist)
            {
                if (item.Title == songname)
                    return item;
            }
            return null;
        }

        /// <summary>
        /// get the songnames as list
        /// </summary>
        /// <param name="playlist"></param>
        /// used: classic view
        public static List<string> GetCurrentPlaylistItems(IPlaylist playlist)
        {
            List<string> data = new List<string>();
            if (playlist == null)
                return data;

            foreach (var item in playlist)
                data.Add(item.Title);
            return data;
        }

        public static List<string> GetCurrentPlaylistItems(IPlaylist playlist, bool withupselector = false)
        {
            List<string> data = new List<string>();
            if (playlist == null)
                return data;
            if (withupselector)
                data.Add("..");
            foreach (var item in playlist)
                data.Add(item.Title);
            return data;
        }
    }
}
