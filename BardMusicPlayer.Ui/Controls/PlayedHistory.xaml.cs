/*
 * Copyright(c) 2024 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Windows.Controls;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für PlayedHistory.xaml
    /// </summary>
    public partial class PlayedHistory : UserControl
    {
        public static ObservableCollection<string> SongHistory = new ObservableCollection<string>();

        public EventHandler<string> OnLoadSongFromHistory;
        public PlayedHistory()
        {
            InitializeComponent();
            SongHistory.CollectionChanged += SongHistory_CollectionChanged;
        }

        private void SongHistory_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            HistoryContainer.Items.Clear();
            int idx = 0;
            foreach (string entry in SongHistory)
            {
                HistoryContainer.Items.Add(new KeyValuePair<string, string>(idx.ToString() + " - " + Path.GetFileNameWithoutExtension(entry), entry));
                idx++;
            }
        }

        /// <summary>
        /// Get the selected filename
        /// </summary>
        /// <returns></returns>
        private string GetFilenameFromSelection()
        {
            try
            {
                var filename = HistoryContainer.SelectedItems.OfType<KeyValuePair<string, string>>();
                if (!File.Exists(filename.First().Value) || filename.First().Value == null)
                    return "";

                return filename.First().Value;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Load the doubleclicked song into the sequencer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HistoryContainer_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string filename = GetFilenameFromSelection();
            if (filename == "")
                return;

            OnLoadSongFromHistory?.Invoke(this, filename);
        }

        private void ToPlaylist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SongHistory.Clear();
        }

        private void Clear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SongHistory.Clear();
        }
    }
}
