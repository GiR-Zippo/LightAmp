﻿/*
 * Copyright(c) 2024 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Seer;
using System;
using System.Windows;
using System.Windows.Controls;
using BardMusicPlayer.Ui.Functions;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Siren;
using BardMusicPlayer.Quotidian;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Controls;
using BardMusicPlayer.Ui.Windows;
using System.Windows.Input;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.DalamudBridge;
using System.Linq;

namespace BardMusicPlayer.Ui.Classic
{
    /// <summary>
    /// Interaktionslogik für Classic_MainView.xaml
    /// </summary>
    public sealed partial class Classic_MainView : UserControl
    {
        private int MaxTracks = 1;
        private bool _directLoaded { get; set; } = false; //indicates if a song was loaded directly or from playlist
        private bool _showPlaylistGrid { get; set; } = true; //indicates if we showing the playlists or history

        //private NetworkPlayWindow _networkWindow = null;
        public Classic_MainView()
        {
            InitializeComponent();

            this.SongName.Text = PlaybackFunctions.GetSongName();
            BmpMaestro.Instance.OnPlaybackTimeChanged   += Instance_PlaybackTimeChanged;
            BmpMaestro.Instance.OnSongMaxTime           += Instance_PlaybackMaxTime;
            BmpMaestro.Instance.OnSongLoaded            += Instance_OnSongLoaded;
            BmpMaestro.Instance.OnPlaybackStarted       += Instance_PlaybackStarted;
            BmpMaestro.Instance.OnPlaybackStopped       += Instance_PlaybackStopped;
            BmpMaestro.Instance.OnTrackNumberChanged    += Instance_TrackNumberChanged;
            BmpMaestro.Instance.OnOctaveShiftChanged    += Instance_OctaveShiftChanged;
            BmpMaestro.Instance.OnSpeedChanged          += Instance_OnSpeedChange;

            BmpSeer.Instance.ChatLog                    += Instance_ChatLog;

            Siren_Volume.Value = BmpSiren.Instance.GetVolume();
            BmpSiren.Instance.SynthTimePositionChanged  += Instance_SynthTimePositionChanged;
            BmpSiren.Instance.SongLoaded                += Instance_SongLoaded;

            PlaylistCtl.OnLoadSongFromPlaylist          += Instance_PlaylistLoadSong;
            PlaylistCtl.OnSetPlaybuttonState            += Instance_SetPlaybuttonState;
            PlaylistCtl.OnLoadSongFromPlaylistToPreview += Instance_PlaylistLoadSongToPreview;
            PlaylistCtl.OnHeaderLabelDoubleClick        += Instance_SwitchPlaylistAndHistory;

            PlayedHistoryCtl.OnLoadSongFromHistory      += Instance_PlaylistLoadSong;
            PlayedHistoryCtl.OnHeaderLabelDoubleClick   += Instance_SwitchPlaylistAndHistory;

            SongBrowser.OnLoadSongFromBrowser           += Instance_SongBrowserLoadedSong;
            SongBrowser.OnAddSongFromBrowser            += Instance_SongBrowserAddSongToPlaylist;
            SongBrowser.OnLoadSongFromBrowserToPreview  += Instance_SongBrowserLoadSongToPreview;

            BmpSeer.Instance.MidibardPlaylistEvent      += Instance_MidibardPlaylistEvent;

            Globals.Globals.OnConfigReload              += Globals_OnConfigReload;

            if (!BmpPigeonhole.Instance.UsePluginForKeyOutput)
                this.KeyDown                            += Classic_MainView_KeyDown;

            LoadConfig();
        }

        private void Globals_OnConfigReload(object sender, EventArgs e)
        {
            LoadConfig(true);
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

        private void Instance_PlaybackStarted(object sender, bool e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.PlaybackStarted()));
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

        private void Instance_OnSpeedChange(object sender, Maestro.Events.SpeedShiftEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.SpeedShiftChange(e)));
        }

        private void Instance_ChatLog(Seer.Events.ChatLog seerEvent)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.AppendChatLog(seerEvent)));
        }

        /// <summary>
        /// triggered by the playlist when a song is loaded
        /// </summary>
        private void Instance_PlaylistLoadSong(object sender, BmpSong song)
        {
            //Inform the PlayedHistory
            if (BmpPigeonhole.Instance.EnableSongHistory)
                PlayedHistory.SongHistory.Add(song);

            PlaybackFunctions.LoadSongFromPlaylist(song);
            InstrumentInfo.Content = PlaybackFunctions.GetInstrumentNameForHostPlayer();
            _directLoaded = false;
        }

        /// <summary>
        /// triggered by the playlist
        /// </summary>
        private void Instance_SetPlaybuttonState(object sender, bool state)
        {
            Play_Button_State(state);
        }

        /// <summary>
        /// triggered by the playlist when a song will be previewed
        /// </summary>
        private void Instance_PlaylistLoadSongToPreview(object sender, BmpSong song)
        {
            loadSongToPreview(song);
        }

        /// <summary>
        /// triggered by the playlist when a song will be previewed
        /// </summary>
        private void Instance_SwitchPlaylistAndHistory(object sender, bool unused)
        {
            if (!BmpPigeonhole.Instance.EnableSongHistory)
                return;

            this.Dispatcher.BeginInvoke(new Action(() => 
            {
                if (PlaylistGrid.Visibility == Visibility.Visible)
                {
                    PlaylistGrid.Visibility = Visibility.Hidden;
                    HistoryGrid.Visibility = Visibility.Visible;
                    return;
                }

                if (HistoryGrid.Visibility == Visibility.Visible)
                {
                    HistoryGrid.Visibility = Visibility.Hidden;
                    PlaylistGrid.Visibility = Visibility.Visible;
                    return;
                }
            }));
        }

        /// <summary>
        /// triggered by the songbrowser or history if a file should be loaded
        /// </summary>
        private void Instance_SongBrowserLoadedSong(object sender, string filename)
        {
            if (PlaybackFunctions.LoadSong(filename))
            {
                //Inform the PlayedHistory
                if (BmpPigeonhole.Instance.EnableSongHistory)
                    PlayedHistory.SongHistory.Add(PlaybackFunctions.CurrentSong);

                InstrumentInfo.Content = PlaybackFunctions.GetInstrumentNameForHostPlayer();
                _directLoaded = true;
            }
        }

        /// <summary>
        /// triggered by the songbrowser if a file should be added to the playlist
        /// </summary>
        private void Instance_SongBrowserAddSongToPlaylist(object sender, string filename)
        {
            PlaylistCtl.AddSongToPlaylist(filename);
        }

        private void Instance_SongBrowserLoadSongToPreview(object sender, string filename)
        {
            loadSongToPreview(BmpSong.OpenFile(filename).Result);
        }

        private void Instance_MidibardPlaylistEvent(Seer.Events.MidibardPlaylistEvent seerEvent)
        {
            this.Dispatcher.BeginInvoke(new Action(() => PlaylistCtl.SelectSongByIndex(seerEvent.Song)));
        }

        private void Instance_SynthTimePositionChanged(string songTitle, double currentTime, double endTime, int activeVoices)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.Siren_PlaybackTimeChanged(currentTime, endTime, activeVoices)));
        }

        private void Instance_SongLoaded(string songTitle)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.Siren_SongName.Content = songTitle));
        }

        private void PlaybackTimeChanged(Maestro.Events.CurrentPlayPositionEvent e)
        {
            ElapsedTime.Content = HelperFunctions.TimeSpanToString(e.timeSpan);

            if (!_Playbar_dragStarted)
                Playbar_Slider.Value = e.tick;
        }

        private void PlaybackMaxTime(Maestro.Events.MaxPlayTimeEvent e)
        {
            TotalTime.Content = HelperFunctions.TimeSpanToString(e.timeSpan);
            Playbar_Slider.Maximum = e.tick;
        }

        private void OnSongLoaded(Maestro.Events.SongLoadedEvent e)
        {
            //Songtitle update
            this.SongName.Text = PlaybackFunctions.GetSongName();
            //Statistics update
            UpdateStats(e);
            //update heatmap
            KeyHeat.initUI(PlaybackFunctions.CurrentSong, NumValue, OctaveNumValue);
            SpeedNumValue = 1.0f;
            if (PlaybackFunctions.PlaybackState != PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYING)
                Play_Button_State(false);

            MaxTracks = e.MaxTracks;
            if (NumValue <= MaxTracks)
                return;
            NumValue = MaxTracks;

            BmpMaestro.Instance.SetTracknumberOnHost(MaxTracks);
        }

        public void PlaybackStarted()
        {
            PlaybackFunctions.PlaybackState = PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYING;
            Play_Button_State(true);
        }

        public void PlaybackStopped()
        {
            PlaybackFunctions.StopSong();
            Play_Button_State(false);

            //if this wasn't a song from the playlist, do nothing
            if (_directLoaded)
                return;

            if (BmpPigeonhole.Instance.PlaylistAutoPlay)
            {
                PlaylistCtl.PlayNextSong();
                Random rnd = new Random();
                PlaybackFunctions.PlaySong(rnd.Next(15, 35)*100);
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

        public void SpeedShiftChange(Maestro.Events.SpeedShiftEvent e)
        {
            if (e.IsHost)
                SpeedNumValue = e.SpeedShift;
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
                    PlaybackFunctions.PlaySong(3000);
                    Play_Button_State(true);
                }
            }
        }
        #endregion

        #region Track UP/Down
        private int _numValue = 1;
        public int NumValue
        {
            get { return _numValue; }
            set
            {
                _numValue = value;
                track_txtNum.Text = "t" + value.ToString();

                //update heatmap
                KeyHeat.initUI(PlaybackFunctions.CurrentSong, NumValue, OctaveNumValue);
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

        private void track_txtNum_KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (track_txtNum == null)
                    return;

                if (int.TryParse(track_txtNum.Text.ToLower().Replace("t", ""), out var num))
                {
                    if (num < 0 || num > MaxTracks)
                    {
                        track_txtNum.Text = "t" + NumValue.ToString();
                        return;
                    }
                    _numValue = num;
                    track_txtNum.Text = "t" + _numValue.ToString();
                    BmpMaestro.Instance.SetTracknumberOnHost(_numValue);
                }
            }
        }
        #endregion

        #region Octave UP/Down
        private int _octavenumValue = 1;
        public int OctaveNumValue
        {
            get { return _octavenumValue; }
            set
            {
                _octavenumValue = value;
                octave_txtNum.Text = @"ø" + value.ToString();
                KeyHeat.initUI(PlaybackFunctions.CurrentSong, NumValue, OctaveNumValue);
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
        #endregion

        #region Speed shift
        private float _speedNumValue = 1.0f;
        public float SpeedNumValue
        {
            get { return _speedNumValue; }
            set
            {
                _speedNumValue = value;
                speed_txtNum.Text = (value*100).ToString()+"%";
            }
        }

        private void speed_txtNum_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (speed_txtNum == null)
                return;

            int t = 0;
            if (int.TryParse(speed_txtNum.Text.Replace(@"%", ""), out t))
            {
                var speedShift = (Convert.ToDouble(t) / 100).Clamp(0.1f, 2.0f);
                BmpMaestro.Instance.SetSpeedShiftAll((float)speedShift);
            }
        }

        private void speed_cmdUp_Click(object sender, RoutedEventArgs e)
        {
            var speedShift = (SpeedNumValue +0.01);
            BmpMaestro.Instance.SetSpeedShiftAll((float)speedShift);
        }

        private void speed_cmdDown_Click(object sender, RoutedEventArgs e)
        {
            var speedShift = (SpeedNumValue - 0.01);
            BmpMaestro.Instance.SetSpeedShiftAll((float)speedShift);
        }
        #endregion


        private void ChatInputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var perf = BmpMaestro.Instance.GetAllPerformers().FirstOrDefault(n => n.HostProcess == true);
                if (perf == null)
                    return;

                if (!perf.UsesDalamud)
                    return;

                string text = new string(ChatInputText.Text.ToCharArray());
                GameExtensions.SendText(perf.game, ChatMessageChannelType.Say, text);
                ChatInputText.Text = "";
            }
        }

        private void Info_Button_Click(object sender, RoutedEventArgs e)
        {
            InfoBox _infoBox = new InfoBox();
            _infoBox.Show();
        }

        private void Script_Button_Click(object sender, RoutedEventArgs e)
        {
            /*if (_networkWindow == null)
                _networkWindow = new NetworkPlayWindow();
            _networkWindow.Visibility = Visibility.Visible;*/

            MacroLaunchpad macroLaunchpad = new MacroLaunchpad();
            macroLaunchpad.Visibility = Visibility.Visible;
        }

        private void Playlist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _showPlaylistGrid = !_showPlaylistGrid;
            if (_showPlaylistGrid)
            {
                PlaylistGrid.Visibility = Visibility.Visible;
                HistoryGrid.Visibility = Visibility.Hidden;
            }
            else
            {
                PlaylistGrid.Visibility = Visibility.Hidden;
                HistoryGrid.Visibility = Visibility.Visible;
            }
        }
    }
}