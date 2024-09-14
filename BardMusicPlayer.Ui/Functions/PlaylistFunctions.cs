/*
 * Copyright(c) 2024 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Coffer;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Resources;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BardMusicPlayer.Ui.Functions
{
    /// <summary>
    /// simplyfied functions both Ui are using
    /// </summary>
    public static class PlaylistFunctions
    {
        /// <summary>
        /// Add file to the playlist
        /// </summary>
        /// <param name="currentPlaylist"></param>
        /// <param name="filename"></param>
        /// <returns>true if success</returns>
        public static bool AddFilesToPlaylist(IPlaylist currentPlaylist, string filename)
        {
            var song = BmpSong.OpenFile(filename).Result;
            {
                if (currentPlaylist.SingleOrDefault(x => x.Title.Equals(song.Title)) == null)
                    currentPlaylist.Add(song);
                /*else
                {
                    if (BmpCoffer.Instance.IsSongInDatabase(song))
                    {
                        var sList = BmpCoffer.Instance.GetSongTitles().Where(x => x.StartsWith(song.Title)).ToList();
                        song.Title = song.Title + "(" + sList.Count() + ")";
                        currentPlaylist.Add(song);
                    }
                }*/
                BmpCoffer.Instance.SaveSong(song);
            }
            BmpCoffer.Instance.SavePlaylist(currentPlaylist);
            return true;
        }

        /// <summary>
        /// Add file(s) to the playlist
        /// </summary>
        /// <param name="currentPlaylist"></param>
        /// <returns>true if success</returns>
        public static bool AddFilesToPlaylist(IPlaylist currentPlaylist)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = Globals.Globals.FileFilters,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return false;

            foreach (string song in openFileDialog.FileNames)
                AddFilesToPlaylist(currentPlaylist, song);
            return true;
        }

        /// <summary>
        /// Add a folder + subfolders to the playlist
        /// </summary>
        /// <param name="currentPlaylist"></param>
        /// <returns>true if success</returns>
        public static async Task<bool> AddFolderToPlaylist(IPlaylist currentPlaylist)
        {
            var dlg = new FolderPicker();

            if (System.IO.Directory.Exists(Pigeonhole.BmpPigeonhole.Instance.SongDirectory))
                dlg.InputPath = System.IO.Path.GetFullPath(Pigeonhole.BmpPigeonhole.Instance.SongDirectory);
            else
                dlg.InputPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.ResultPath;

                if (!System.IO.Directory.Exists(path))
                    return false;

                return await Task.Run(() =>
                {
                    ProgressBar.Show(path, "Importing folder");
                    string[] files = System.IO.Directory.EnumerateFiles(path, "*.*", System.IO.SearchOption.AllDirectories).Where(s => s.EndsWith(".mid") || s.EndsWith(".mml") || s.EndsWith(".mmsong")).ToArray();
                    foreach (var it in files.Select((x, i) => new { Value = x, Index = i }))
                    {
                        BmpSong song = BmpSong.OpenFile(it.Value).Result;
                        if (currentPlaylist.SingleOrDefault(x => x.Title.Equals(song.Title)) == null)
                            currentPlaylist.Add(song);
                        BmpCoffer.Instance.SaveSong(song);
                        ProgressBar.Update(ProgressBar.GetPercent(it.Index, files.Count()));
                    }
                    BmpCoffer.Instance.SavePlaylist(currentPlaylist);
                    ProgressBar.WndClose();
                    return true;
                });
            }
            return false;
        }

        /// <summary>
        /// gets the first playlist or null if none was found
        /// </summary>
        /// <param name="playlistname"></param>
        public static IPlaylist GetFirstPlaylist()
        {
            if (BmpCoffer.Instance.GetPlaylistNames().Count > 0)
                return BmpCoffer.Instance.GetPlaylist(BmpCoffer.Instance.GetPlaylistNames()[0]);
            return null;
        }

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
            return playlist?.FirstOrDefault(item => item.Title == songname);
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

            data.AddRange(playlist.Select(item => item.Title));
            return data;
        }

        public static IEnumerable<string> GetCurrentPlaylistItems(IPlaylist playlist, bool withupselector = false)
        {
            List<string> data = new List<string>();
            if (playlist == null)
                return data;
            if (withupselector)
                data.Add("..");
            data.AddRange(playlist.Select(item => item.Title));
            return data;
        }

        public static IEnumerable<string> GeAllSongsInDB(bool withupselector = false)
        {
            List<string> data = new List<string>();
            if (withupselector)
                data.Add("..");
            data.AddRange(BmpCoffer.Instance.GetSongTitles().Select(item => item));
            return data;
        }

        /// <summary>
        /// Get the total time of all items in the playlist
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns><see cref="TimeSpan"/></returns>
        public static TimeSpan GetTotalTime(IPlaylist playlist)
        {
            TimeSpan totalTime = new TimeSpan(0);
            return playlist.Aggregate(totalTime, (current, p) => current + p.Duration);
        }

        /// <summary>
        /// Export a song to Midi
        /// </summary>
        /// <param name="song"></param>
        public static bool ExportSong(BmpSong song)
        {
            if (song == null)
                return false;

            Stream myStream;
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "MIDI file (*.mid)|*.mid";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.OverwritePrompt = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                if ((myStream = saveFileDialog.OpenFile()) != null)
                {
                    song.GetExportMidi().WriteTo(myStream);
                    myStream.Close();
                    return true;
                }
            }
            return false;
        }
    }
}
