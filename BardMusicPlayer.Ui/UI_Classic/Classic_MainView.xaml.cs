
using BardMusicPlayer.Seer;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using BardMusicPlayer.Ui.Functions;
using BardMusicPlayer.Coffer;
using BardMusicPlayer.Maestro;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Pigeonhole;

namespace BardMusicPlayer.Ui.Classic
{
    /// <summary>
    /// Interaktionslogik für Classic_MainView.xaml
    /// </summary>
    public partial class Classic_MainView : UserControl
    {
        private int MaxTracks = 1;

        public Classic_MainView()
        {
            InitializeComponent();

            //Always start with the playlists
            _showingPlaylists = true;
            //Fill the list
            PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();

            this.SongName.Text = PlaybackFunctions.GetSongName();
            BmpMaestro.Instance.OnPlaybackTimeChanged += Instance_PlaybackTimeChanged;
            BmpMaestro.Instance.OnSongMaxTime += Instance_PlaybackMaxTime;
            BmpMaestro.Instance.OnSongLoaded += Instance_OnSongLoaded;
            BmpMaestro.Instance.OnPlaybackStopped += Instance_PlaybackStopped;
            BmpMaestro.Instance.OnTrackNumberChanged += Instance_TrackNumberChanged;
            BmpMaestro.Instance.OnOctaveShiftChanged += Instance_OctaveShiftChanged;
            BmpSeer.Instance.ChatLog += Instance_ChatLog;
            BmpSeer.Instance.EnsembleStarted += Instance_EnsembleStarted;

            LoadConfig();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            KeyHeat.InitUi();
        }

        #region EventHandler
        private void Instance_PlaybackTimeChanged(object sender, Maestro.Events.CurrentPlayPositionEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.PlaybackTimeChanged(e)));
        }

        private void Instance_PlaybackMaxTime(object sender, Maestro.Events.MaxPlayTimeEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.PlaybackMaxTime(e)));
        }

        private void Instance_OnSongLoaded(object sender, Maestro.Events.SongLoadedEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.OnSongLoaded(e)));
        }

        private void Instance_PlaybackStopped(object sender, bool e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.PlaybackStopped()));
        }

        private void Instance_TrackNumberChanged(object sender, Maestro.Events.TrackNumberChangedEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.TracknumberChanged(e)));
        }

        private void Instance_OctaveShiftChanged(object sender, Maestro.Events.OctaveShiftChangedEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.OctaveShiftChanged(e)));
        }

        private void Instance_ChatLog(Seer.Events.ChatLog seerEvent)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.AppendChatLog(seerEvent)));
        }

        private void Instance_EnsembleStarted(Seer.Events.EnsembleStarted seerEvent)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.EnsembleStart()));
        }

        private void PlaybackTimeChanged(Maestro.Events.CurrentPlayPositionEvent e)
        {
            string time;
            string Seconds = e.timeSpan.Seconds.ToString();
            string Minutes = e.timeSpan.Minutes.ToString();
            time = ((Minutes.Length == 1) ? "0" + Minutes : Minutes) + ":" + ((Seconds.Length == 1) ? "0" + Seconds : Seconds);
            ElapsedTime.Content = time;

            if (!_Playbar_dragStarted)
                Playbar_Slider.Value = e.tick;
        }

        private void PlaybackMaxTime(Maestro.Events.MaxPlayTimeEvent e)
        {
            string time;
            string Seconds = e.timeSpan.Seconds.ToString();
            string Minutes = e.timeSpan.Minutes.ToString();
            time = ((Minutes.Length == 1) ? "0" + Minutes : Minutes) + ":" + ((Seconds.Length == 1) ? "0" + Seconds : Seconds);
            TotalTime.Content = time;

            Playbar_Slider.Maximum = e.tick;

        }

        private void OnSongLoaded(Maestro.Events.SongLoadedEvent e)
        {
            //Statistics update
            UpdateStats(e);
            //update heatmap
            KeyHeat.initUI(PlaybackFunctions.CurrentSong, NumValue);

            if (PlaybackFunctions.PlaybackState != PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYING)
                Play_Button_State(false);

            MaxTracks = e.MaxTracks;
            if (NumValue <= MaxTracks)
                return;
            NumValue = MaxTracks;

            BmpMaestro.Instance.SetTracknumberOnHost(MaxTracks);
        }

        public void PlaybackStopped()
        {
            PlaybackFunctions.StopSong();
            Play_Button_State(false);

            if (BmpPigeonhole.Instance.PlaylistAutoPlay)
            {
                playNextSong();
                PlaybackFunctions.PlaySong();
                Play_Button_State(true);
            }
        }

        public void TracknumberChanged(Maestro.Events.TrackNumberChangedEvent e)
        {
            if (e.IsHost)
            {
                NumValue = e.TrackNumber;
                UpdateNoteCountForTrack();
            }
        }

        public void OctaveShiftChanged(Maestro.Events.OctaveShiftChangedEvent e)
        {
            if (e.IsHost)
                OctaveNumValue = e.OctaveShift;
        }

        public void EnsembleStart()
        {
            if (BmpPigeonhole.Instance.AutostartMethod != (int)Globals.Globals.Autostart_Types.VIA_METRONOME)
                return;
            if (PlaybackFunctions.PlaybackState == PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYING)
                return;

            Thread.Sleep(2475);
            PlaybackFunctions.PlaySong();
            Play_Button_State(true);
        }

        public void AppendChatLog(Seer.Events.ChatLog ev)
        {
            if (BmpMaestro.Instance.GetHostPid() == ev.ChatLogGame.Pid)
            {
                BmpChatParser.AppendText(ChatBox, ev);
                this.ChatBox.ScrollToEnd();
            }

            if (ev.ChatLogCode == "0039")
            {
                if (ev.ChatLogLine.Contains(@"Anzählen beginnt") ||
                    ev.ChatLogLine.Contains("The count-in will now commence.") ||
                    ev.ChatLogLine.Contains("orchestre est pr"))
                {
                    if (BmpPigeonhole.Instance.AutostartMethod != (int)Globals.Globals.Autostart_Types.VIA_CHAT)
                        return;
                    if (PlaybackFunctions.PlaybackState == PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYING)
                        return;
                    Thread.Sleep(3000);
                    PlaybackFunctions.PlaySong();
                    Play_Button_State(true);
                }
            }
        }
        #endregion

        /* Track UP/Down */
        private int _numValue = 1;
        public int NumValue
        {
            get { return _numValue; }
            set
            {
                _numValue = value;
                track_txtNum.Text = "t" + value.ToString();

                //update heatmap
                KeyHeat.initUI(PlaybackFunctions.CurrentSong, NumValue);
                this.InstrumentInfo.Content = PlaybackFunctions.GetInstrumentNameForHostPlayer();
            }
        }
        private void track_cmdUp_Click(object sender, RoutedEventArgs e)
        {
            if (NumValue == MaxTracks)
                return;
            NumValue++;
            BmpMaestro.Instance.SetTracknumberOnHost(NumValue);
        }

        private void track_cmdDown_Click(object sender, RoutedEventArgs e)
        {
            if (NumValue == 1)
                return;
            NumValue--;
            BmpMaestro.Instance.SetTracknumberOnHost(NumValue);
        }

        private void track_txtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (track_txtNum == null)
                return;

            if (int.TryParse(track_txtNum.Text.Replace("t", ""), out _numValue))
            {
                if (_numValue < 0 || _numValue > MaxTracks)
                    return;
                track_txtNum.Text = "t" + _numValue.ToString();
                BmpMaestro.Instance.SetTracknumberOnHost(_numValue);
            }
        }
        /* Octave UP/Down */
        private int _octavenumValue = 1;
        public int OctaveNumValue
        {
            get { return _octavenumValue; }
            set
            {
                _octavenumValue = value;
                octave_txtNum.Text = @"ø" + value.ToString();
            }
        }
        private void octave_cmdUp_Click(object sender, RoutedEventArgs e)
        {
            OctaveNumValue++;
            BmpMaestro.Instance.SetOctaveshiftOnHost(OctaveNumValue);
        }

        private void octave_cmdDown_Click(object sender, RoutedEventArgs e)
        {
            OctaveNumValue--;
            BmpMaestro.Instance.SetOctaveshiftOnHost(OctaveNumValue);
        }

        private void octave_txtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (octave_txtNum == null)
                return;

            if (int.TryParse(octave_txtNum.Text.Replace(@"ø", ""), out _octavenumValue))
            {
                octave_txtNum.Text = @"ø" + _octavenumValue.ToString();
                BmpMaestro.Instance.SetOctaveshiftOnHost(_octavenumValue);
            }
        }

    }
}