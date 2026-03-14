/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using BardMusicPlayer.Coffer;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Functions;
using UI.Resources;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für PlayedHistory.xaml
    /// </summary>
    public partial class PlayedHistory : UserControl
    {
        public static ObservableCollection<BmpSong> SongHistory = new ObservableCollection<BmpSong>();

        public EventHandler<BmpSong> OnLoadSongFromHistory;
        public EventHandler<bool> OnHeaderLabelDoubleClick;

        public PlayedHistory()
        {
            InitializeComponent();
            SongHistory.CollectionChanged += SongHistory_CollectionChanged;
        }

        private void SongHistory_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HistoryContainer.Items.Clear();
            int idx = 0;
            foreach (BmpSong entry in SongHistory)
            {
                HistoryContainer.Items.Add(new KeyValuePair<string, BmpSong>(idx.ToString() + " - " + entry.Title, entry));
                idx++;
            }
        }

        /// <summary>
        /// Get the selected filename
        /// </summary>
        /// <returns></returns>
        private BmpSong GetFilenameFromSelection()
        {
            try
            {
                var filename = HistoryContainer.SelectedItems.OfType<KeyValuePair<string, BmpSong>>();
                if (!(filename.First().Value != null) || filename.First().Value == null)
                    return null;

                return filename.First().Value;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Load the doubleclicked song into the sequencer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HistoryContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            BmpSong filename = GetFilenameFromSelection();
            if (filename == null)
                return;

            OnLoadSongFromHistory?.Invoke(this, filename);
        }

        private void HistoryLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OnHeaderLabelDoubleClick?.Invoke(this, true);
        }

        private void ToPlaylist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var inputbox = new TextInputWindow("Playlist Name");
            if (inputbox.ShowDialog() == true)
            {
                if (inputbox.ResponseText.Length < 1)
                    return;

                var Playlist = PlaylistFunctions.CreatePlaylist(inputbox.ResponseText);

                foreach (KeyValuePair<string, BmpSong> item in HistoryContainer.Items)
                {
                    BmpSong song = item.Value;
                    if (Playlist.SingleOrDefault(x => x.Title.Equals(song.Title)) == null)
                        Playlist.Add(song);

                    BmpCoffer.Instance.SaveSong(song);
                }

                BmpCoffer.Instance.SavePlaylist(Playlist);
            }
        }

        private void Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SongHistory.Clear();
        }
    }
}
