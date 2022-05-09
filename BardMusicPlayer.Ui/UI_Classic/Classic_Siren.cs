using BardMusicPlayer.Siren;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Functions;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BardMusicPlayer.Ui.Classic
{
    /// <summary>
    /// Interaktionslogik für Classic_MainView.xaml
    /// </summary>
    public partial class Classic_MainView : UserControl
    {
        private void Siren_Load_Click(object sender, RoutedEventArgs e)
        {
            BmpSong CurrentSong = null;
            string song = PlaylistContainer.SelectedItem as String;
            if (song == null)
            {
                CurrentSong = Siren_LoadMidiFile();
                if (CurrentSong == null)
                    return;
            }
            else
            {
                if ((string)PlaylistContainer.SelectedItem == "..")
                {
                    CurrentSong = Siren_LoadMidiFile();
                    if (CurrentSong == null)
                        return;
                }
                else
                    CurrentSong = PlaylistFunctions.GetSongFromPlaylist(_currentPlaylist, (string)PlaylistContainer.SelectedItem);
            }

            _ = BmpSiren.Instance.Load(CurrentSong);
            this.Siren_SongName.Content = BmpSiren.Instance.CurrentSongTitle;
        }

        private void Siren_Play_Click(object sender, RoutedEventArgs e)
        {
            if(BmpSiren.Instance.IsReadyForPlayback)
                BmpSiren.Instance.Play();
        }

        private void Siren_Stop_Click(object sender, RoutedEventArgs e)
        {
            if (BmpSiren.Instance.IsReadyForPlayback)
                BmpSiren.Instance.Stop();
        }


        private BmpSong Siren_LoadMidiFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "MIDI file|*.mid;*.midi|All files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return null;

            if (!openFileDialog.CheckFileExists)
                return null;

            return BmpSong.OpenMidiFile(openFileDialog.FileName).Result;
        }
        /*private void Siren_Volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Slider slider = e.OriginalSource as Slider;
            // ((float)slider.Value, 2, 100);
        }*/
    }
}
