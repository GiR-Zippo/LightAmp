/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using BardMusicPlayer.Ui.Functions;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Pigeonhole;
using System.Threading.Tasks;
using BardMusicPlayer.Ui.Controls;

namespace BardMusicPlayer.Ui.Classic
{
    /// <summary>
    /// Interaktionslogik für Classic_MainView.xaml
    /// </summary>
    public sealed partial class Classic_MainView : UserControl
    {
        private bool _alltracks = false;
        private bool _Playbar_dragStarted = false;
        private bool _Siren_Playbar_dragStarted = false;

        /* Playbuttonstate */
        private void Play_Button_State(bool playing = false)
        {
            Play_Button.Content = !playing ? @"▶" : @"⏸";
        }

        /* Playback */
        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            if (PlaybackFunctions.PlaybackState == PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYING)
            {
                PlaybackFunctions.PauseSong();
                Play_Button_State(false);
            }
            else
            {
                PlaybackFunctions.PlaySong(0);
                Play_Button_State(true);
            }
        }


        private void Play_Button_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PlaybackFunctions.PlaybackState == PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYING)
                return;

            Task task = Task.Run(() =>
            {
                BmpMaestro.Instance.EquipInstruments();
                Task.Delay(2000).Wait();
                BmpMaestro.Instance.StartEnsCheck();
            });
        }

        /* Song Select */
        private void SongName_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (PlaybackFunctions.LoadSong())
                {
                    //Inform the PlayedHistory
                    if (BmpPigeonhole.Instance.EnableSongHistory)
                        PlayedHistory.SongHistory.Add(PlaybackFunctions.CurrentSong);
                    InstrumentInfo.Content = PlaybackFunctions.GetInstrumentNameForHostPlayer();
                    _directLoaded = true;
                }
            }

            if (e.RightButton == MouseButtonState.Pressed)
                SongName.SelectAll();
        }

        /* All tracks */
        private void all_tracks_button_Click(object sender, RoutedEventArgs e)
        {
            _alltracks = !_alltracks;
            track_cmdDown.IsEnabled = !_alltracks;
            track_cmdUp.IsEnabled = !_alltracks;
            track_txtNum.IsEnabled = !_alltracks;

            if (_alltracks)
            {
                BmpMaestro.Instance.SetTracknumberOnHost(0);
                BmpPigeonhole.Instance.PlayAllTracks = true;
                all_tracks_button.Background = Brushes.LightSteelBlue;
            }
            else
            {
                BmpPigeonhole.Instance.PlayAllTracks = false;
                BmpMaestro.Instance.SetTracknumberOnHost(1);
                NumValue = BmpMaestro.Instance.GetHostBardTrack();
                all_tracks_button.ClearValue(Button.BackgroundProperty);
            }
        }

        private void Rewind_Click(object sender, RoutedEventArgs e)
        {
            PlaybackFunctions.StopSong();
            Play_Button.Content = @"▶";
        }

        private void Playbar_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
        }

        private void Playbar_Slider_DragStarted(object sender, DragStartedEventArgs e)
        {
            this._Playbar_dragStarted = true;
        }

        private void Playbar_Slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            BmpMaestro.Instance.SetPlaybackStart((int)((Slider)sender).Value);
            this._Playbar_dragStarted = false;
        }
    }
}
