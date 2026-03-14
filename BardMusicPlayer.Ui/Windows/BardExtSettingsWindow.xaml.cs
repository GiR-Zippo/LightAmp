/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.DalamudBridge;
using BardMusicPlayer.Dalamud.Events;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Ui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CSCore.CoreAudioAPI;
using System.Threading.Tasks;
using System.ComponentModel;


namespace BardMusicPlayer.Ui.Windows
{
    /// <summary>
    /// Interaktionslogik für BardExtSettingsWindow.xaml
    /// </summary>
    public sealed partial class BardExtSettingsWindow : Window
    {
        private Performer _performer = null;
        private List<CheckBox> _cpuBoxes = new List<CheckBox>();

        public BardExtSettingsWindow(Performer performer)
        {
            _performer = performer;
            InitializeComponent();
            Title = "Settings for: " + _performer.PlayerName;

            Songtitle_Post_Type.SelectedIndex = 0;
            Songtitle_Chat_Type.SelectedIndex = 0;
            Chat_Type.SelectedIndex = 0;

            //Get the values for the song parsing bard
            var tpBard = BmpMaestro.Instance.GetSongTitleParsingBard();
            if (tpBard.Value != null)
            {
                if (tpBard.Value.game.Pid == _performer.game.Pid)
                {
                    Songtitle_Chat_Prefix.Text = tpBard.Key.prefix;

                    if (tpBard.Key.channelType.ChannelCode == ChatMessageChannelType.Say.ChannelCode)
                        Songtitle_Chat_Type.SelectedIndex = 0;
                    else if (tpBard.Key.channelType.ChannelCode == ChatMessageChannelType.Yell.ChannelCode)
                        Songtitle_Chat_Type.SelectedIndex = 1;
                    else if (tpBard.Key.channelType.ChannelCode == ChatMessageChannelType.Shout.ChannelCode)
                        Songtitle_Chat_Type.SelectedIndex = 2;

                    Songtitle_Post_Type.SelectedIndex = tpBard.Key.channelType.Equals(ChatMessageChannelType.None) ? 0 : 1;
                }
            }

            this.Lyrics_TrackNr.Value = performer.SingerTrackNr.ToString();
            GfxTest.IsChecked = _performer.game.GfxSettingsLow;
            SoundOn.IsChecked = _performer.game.SoundOn;

            if (!GameExtensions.IsConnected(_performer.game.Pid))
                MasterVolume.Value = MasterAudioVol(-1);
            else
            {
                GameExtensions.SetMasterVolume(_performer.game, -1);
                DalamudBridge.DalamudBridge.Instance.OnMasterVolumeChangedEvent += Instance_DalamudMasterVol;
                DalamudBridge.DalamudBridge.Instance.OnMasterVolumeMuteEvent    += Instance_DalamudMasterMute;
                DalamudBridge.DalamudBridge.Instance.OnVoiceVolumeMuteEvent     += Instance_DalamudVoiceMute;
                DalamudBridge.DalamudBridge.Instance.OnEffectVolumeMuteEvent    += Instance_DalamudEffectMute;
                this.Closing += Instance_WindowClose;
            }

            CharUUID_Label.Content = _performer.game.ConfigId;
            PopulateCPUTab();
        }

        #region EventCtrl
        private void Instance_WindowClose(object sender, CancelEventArgs e)
        {
            DalamudBridge.DalamudBridge.Instance.OnMasterVolumeChangedEvent -= Instance_DalamudMasterVol;
            DalamudBridge.DalamudBridge.Instance.OnMasterVolumeMuteEvent    -= Instance_DalamudMasterMute;
            DalamudBridge.DalamudBridge.Instance.OnVoiceVolumeMuteEvent     -= Instance_DalamudVoiceMute;
            DalamudBridge.DalamudBridge.Instance.OnEffectVolumeMuteEvent    -= Instance_DalamudEffectMute;
        }

        private void Instance_DalamudMasterVol(object sender, MasterVolumeChangedEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() => 
            { 
                if (e.PId == this._performer.game.Pid)
                    MasterVolume.Value = e.MasterVolume;
            }));
        }

        private void Instance_DalamudMasterMute(object sender, MasterVolumeMuteEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.PId == this._performer.game.Pid)
                    SoundOn.IsChecked = !e.MasterState;
            }));
        }

        private void Instance_DalamudVoiceMute(object sender, VoiceVolumeMuteEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.PId == this._performer.game.Pid)
                    VoiceOn.IsChecked = !e.State;
            }));
        }

        private void Instance_DalamudEffectMute(object sender, EffectVolumeMuteEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.PId == this._performer.game.Pid)
                    EffectOn.IsChecked = !e.State;
            }));
        }
        #endregion

        #region ChatControl
        private void Songtitle_Post_Type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChatMessageChannelType chanType = ChatMessageChannelType.None;
            switch (Songtitle_Chat_Type.SelectedIndex)
            {
                case 0:
                    chanType = ChatMessageChannelType.Say;
                    break;
                case 1:
                    chanType = ChatMessageChannelType.Yell;
                    break;
                case 2:
                    chanType = ChatMessageChannelType.Shout;
                    break;
            }

            switch (Songtitle_Post_Type.SelectedIndex)
            {
                case 0:
                    BmpMaestro.Instance.SetSongTitleParsingBard(ChatMessageChannelType.None, "", null);
                    break;
                case 1:
                    BmpMaestro.Instance.SetSongTitleParsingBard(chanType, Songtitle_Chat_Prefix.Text, _performer);
                    break;
            }
        }

        private void PostSongTitle_Click(object sender, RoutedEventArgs e)
        {
            if (_performer.SongName == "")
                return;

            ChatMessageChannelType chanType = ChatMessageChannelType.None;
            switch (Songtitle_Chat_Type.SelectedIndex)
            {
                case 0:
                    chanType = ChatMessageChannelType.Say;
                    break;
                case 1:
                    chanType = ChatMessageChannelType.Yell;
                    break;
            }
            string songName = $"{Songtitle_Chat_Prefix.Text} {_performer.SongName.Replace('_', ' ')} {Songtitle_Chat_Prefix.Text}";
            GameExtensions.SendText(_performer.game, chanType, songName);
        }

        private void ChatInputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ChatMessageChannelType chanType = ChatMessageChannelType.None;
                switch (Chat_Type.SelectedIndex)
                {
                    case 0:
                        chanType = ChatMessageChannelType.Say;
                        break;
                    case 1:
                        chanType = ChatMessageChannelType.Yell;
                        break;
                    case 2:
                        chanType = ChatMessageChannelType.Party;
                        break;
                    case 3:
                        chanType = ChatMessageChannelType.FC;
                        break;
                    case 4:
                        chanType = ChatMessageChannelType.None;
                        break;
                }
                string text = new string(ChatInputText.Text.ToCharArray());
                GameExtensions.SendText(_performer.game, chanType, text);
                ChatInputText.Text = "";
            }
        }

        private void Lyrics_TrackNr_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            NumericUpDown ctl = sender as NumericUpDown;
            ctl.OnValueChanged += Lyrics_TrackNr_OnValueChanged;
        }

        private void Lyrics_TrackNr_OnValueChanged(object sender, int s)
        {
            _performer.SingerTrackNr = s;
            NumericUpDown ctl = sender as NumericUpDown;
            ctl.OnValueChanged -= Lyrics_TrackNr_OnValueChanged;
        }

        /// <summary>
        /// Sets the GFX to Low or back to High
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GfxTest_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)GfxTest.IsChecked)
            {
                if (_performer.game.GfxSettingsLow)
                    return;
                if (!GameExtensions.GfxSetLow(_performer.game, true).Result)
                    _performer.game.SetGfxLow();
                _performer.game.GfxSettingsLow = true;
            }
            else
            {
                if (!_performer.game.GfxSettingsLow)
                    return;
                if (!GameExtensions.GfxSetLow(_performer.game, false).Result)
                    _performer.game.RestoreGFXSettings();
                _performer.game.GfxSettingsLow = false;
            }
        }

        /// <summary>
        /// Kills the client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KillClient_Click(object sender, RoutedEventArgs e)
        {
            if (!GameExtensions.TerminateClient(_performer.game).Result)
                _performer.game.Process.Kill();
        }
        #endregion

        #region Sound Tab
        /// <summary>
        /// Enables/Disables the master sound
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SoundOn_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)SoundOn.IsChecked)
            {
                if (_performer.game.SoundOn)
                    return;
                if (!GameExtensions.SetSoundOnOff(_performer.game, true).Result)
                {
                    _performer.game.SetSoundOnOffLegacy(true);
                    MuteAudio(false);
                }
                _performer.game.SoundOn = true;
            }
            else
            {
                if (!_performer.game.SoundOn)
                    return;
                if (!GameExtensions.SetSoundOnOff(_performer.game, false).Result)
                {
                    _performer.game.SetSoundOnOffLegacy(false);
                    MuteAudio(true);
                }
                _performer.game.SoundOn = false;
            }
        }

        /// <summary>
        /// Sets the master volume (by dalamud or legacy windows mixer)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MasterVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!GameExtensions.SetMasterVolume(_performer.game, (short)e.NewValue).Result)
                MasterAudioVol((float)e.NewValue);
        }

        /// <summary>
        /// Enables/Disables the voices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VoiceOn_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)VoiceOn.IsChecked)
            {
                if (!GameExtensions.SetVoiceOnOff(_performer.game, true).Result)
                    return;
            }
            else
            {
                if (!GameExtensions.SetVoiceOnOff(_performer.game, false).Result)
                    return;
            }
        }

        /// <summary>
        /// Enables/Disables the effects
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EffectOn_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)EffectOn.IsChecked)
            {
                if (!GameExtensions.SetEffectOnOff(_performer.game, true).Result)
                    return;
            }
            else
            {
                if (!GameExtensions.SetEffectOnOff(_performer.game, false).Result)
                    return;
            }
        }

        /// <summary>
        /// Mutes the audio out in legacy mode
        /// </summary>
        /// <param name="state"></param>
        private void MuteAudio(bool state)
        {
            var sessionManager = Task.Run(() => Task.FromResult(GetDefaultAudioSessionManager2(DataFlow.Render))).Result;

            //using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        using (var simpleVolume = session.QueryInterface<SimpleAudioVolume>())
                        using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                        {
                            if (sessionControl.ProcessID == _performer.game.Pid)
                                simpleVolume.IsMuted = state;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Sets the Mastervolume in legacy mode
        /// </summary>
        /// <param name="state"></param>
        /// <returns><see cref="float"/> volume</returns>
        private float MasterAudioVol(float state)
        {
            var sessionManager = Task.Run(() => Task.FromResult(GetDefaultAudioSessionManager2(DataFlow.Render))).Result;

            //using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        using (var simpleVolume = session.QueryInterface<SimpleAudioVolume>())
                        using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                        {
                            if (sessionControl.ProcessID == _performer.game.Pid)
                                if (state == -1)
                                    return simpleVolume.MasterVolume * 100;
                                else
                                    simpleVolume.MasterVolume = state /100;
                        }
                    }
                }
            }
            return state;
        }

        private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    //Debug.WriteLine("DefaultDevice: " + device.FriendlyName);
                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }
        #endregion

        #region CPU-Tab
        /// <summary>
        /// Populates the CPU-Tab
        /// </summary>
        private void PopulateCPUTab()
        {
            //Get the our application's process.
            Process process = _performer.game.Process;

            //Get the processor count of our machine.
            int cpuCount = Environment.ProcessorCount;
            long AffinityMask = (long)_performer.game.GetAffinity();

            int res = (int)Math.Ceiling((double)cpuCount / (double)3);
            int idx = 1;
            for (int col = 0; col != 3; col++)
            {
                CPUDisplay.ColumnDefinitions.Add(new ColumnDefinition());
                
                for (int i = 0; i != res + 1; i++)
                {
                    if (idx == cpuCount+1)
                        break;
                    if (CPUDisplay.RowDefinitions.Count < res +1)
                        CPUDisplay.RowDefinitions.Add(new RowDefinition());
                    var uc = new CheckBox
                    {
                        Name = "CPU" + idx,
                        Content = "CPU" + idx
                    };
                    if ((AffinityMask & (1 << idx-1)) > 0) //-1 since we count at 1
                        uc.IsChecked = true;
                    _cpuBoxes.Add(uc);
                    CPUDisplay.Children.Add(uc);
                    Grid.SetRow(uc, i);
                    Grid.SetColumn(uc, CPUDisplay.ColumnDefinitions.Count - 1);
                    idx++;
                }
            }
        }

        /// <summary>
        /// Saves the CPU affinity settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_CPU_Click(object sender, RoutedEventArgs e)
        {
            long mask = 0;
            int idx = 0;
            foreach (CheckBox box in _cpuBoxes)
            {
                if ((bool)box.IsChecked)
                    mask += 0b1 << idx;
                else
                    mask += 0b0 << idx;
                idx++;
            }
            //If mask == 0 show an error
            if (mask == 0)
            {
                var result = MessageBox.Show("No CPU was selected", "Error Affinity", MessageBoxButton.OK, MessageBoxImage.Error);
                if (result == MessageBoxResult.OK)
                    return;
            }
            else
                _performer.game.SetAffinity(mask);
        }

        /// <summary>
        /// Clears the CPU aff. mask
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_CPU_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox box in _cpuBoxes)
            {
                box.IsChecked = false;
            }
        }

        /// <summary>
        /// Resets the CPU aff. to default
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void Reset_CPU_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox box in _cpuBoxes)
            {
                box.IsChecked = true;
            }
        }
        #endregion
    }
}
