/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.MidiUtil.Managers;
using BardMusicPlayer.Ui.Functions;
using BardMusicPlayer.Ui.Windows;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BardMusicPlayer.Ui.Classic
{
    /// <summary>
    /// only here cuz someone would like to have it back
    /// </summary>
    public sealed partial class Classic_MainView : UserControl
    {
        private List<int> _notesCountForTracks = new List<int>();
        private void UpdateStats(Maestro.Events.SongLoadedEvent e)
        {
            this.Statistics_Total_Tracks_Label.Content = e.MaxTracks.ToString();
            this.Statistics_Total_Note_Count_Label.Content = e.TotalNoteCount.ToString();

            this._notesCountForTracks.Clear();
            this._notesCountForTracks = e.CurrentNoteCountForTracks;

            if (NumValue >= _notesCountForTracks.Count)
            {
                this.Statistics_Track_Note_Count_Label.Content = "Invalid track";
                return;
            }
            this.Statistics_Track_Note_Count_Label.Content = _notesCountForTracks[NumValue];
        }

        private void UpdateNoteCountForTrack()
        {
            if (PlaybackFunctions.CurrentSong == null)
                return;

            if (NumValue >= _notesCountForTracks.Count)
            {
                this.Statistics_Track_Note_Count_Label.Content = "Invalid track";
                return;
            }

            this.Statistics_Track_Note_Count_Label.Content = _notesCountForTracks[NumValue];
        }

        private void ExportAsMidi(object sender, RoutedEventArgs e)
        {
            PlaylistFunctions.ExportSong(PlaybackFunctions.CurrentSong);
        }

        private void QuickMidiProcessing_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = Globals.Globals.FileFilters,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            if (!openFileDialog.CheckFileExists)
                return;

            MidiBardConverterWindow conv = new MidiBardConverterWindow(openFileDialog.FileName);
            conv.Visibility = Visibility.Visible;
        }

        private void MidiProcessing_Click(object sender, RoutedEventArgs e)
        {
            MidiUtil.MidiUtil.Instance.Start();
            //UiManager.Instance.mainWindow = new MidiEditWindow();
            if (PlaybackFunctions.CurrentSong != null)
                MidiManager.Instance.OpenFile(PlaybackFunctions.CurrentSong.GetExportMidi());
        }
        
    }
}
