using BardMusicPlayer.Maestro;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Ui.Resources;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Resources;

namespace BardMusicPlayer.Ui.Classic
{
    public sealed partial class Classic_MainView : UserControl
    {
        /// <summary>
        /// load the settings
        /// </summary>
        private void LoadConfig(bool reload = false)
        {
            //Orchestra
            LocalOrchestraBox.IsChecked = BmpPigeonhole.Instance.LocalOrchestra;
            KeepTrackSettingsBox.IsChecked = BmpPigeonhole.Instance.EnsembleKeepTrackSetting;
            IgnoreProgchangeBox.IsChecked = BmpPigeonhole.Instance.IgnoreProgChange;
            Autostart_source.SelectedIndex = BmpPigeonhole.Instance.AutostartMethod;
            AutoEquipBox.IsChecked = BmpPigeonhole.Instance.AutoEquipBards;
            AutoselectHostBox.IsChecked = BmpPigeonhole.Instance.AutoselectHost;
            StartBardIndividuallyBox.IsChecked = BmpPigeonhole.Instance.EnsembleStartIndividual;

            //Playback
            HoldNotesBox.IsChecked = BmpPigeonhole.Instance.HoldNotes;
            ForcePlaybackBox.IsChecked = BmpPigeonhole.Instance.ForcePlayback;
            if (!reload)
            {
                MIDI_Input_DeviceBox.Items.Clear();
                MIDI_Input_DeviceBox.ItemsSource = Maestro.Utils.MidiInput.ReloadMidiInputDevices();
                MIDI_Input_DeviceBox.SelectedIndex = BmpPigeonhole.Instance.MidiInputDev + 1;
            }
            LiveMidiDelay.IsChecked = BmpPigeonhole.Instance.LiveMidiPlayDelay;

            //Misc
            AMPInFrontBox.IsChecked = BmpPigeonhole.Instance.BringBMPtoFront;
            MultiBox_Box.IsChecked = BmpPigeonhole.Instance.EnableMultibox;
            AutoequipDalamud.IsChecked = BmpPigeonhole.Instance.UsePluginForInstrumentOpen;
            MidiBardComp.IsChecked = BmpPigeonhole.Instance.MidiBardCompatMode;

            if (!BmpPigeonhole.Instance.ClassicUi)
            {
                SkinUiBox.Visibility = Visibility.Visible;
                SkinUiBox.IsChecked = !BmpPigeonhole.Instance.ClassicUi;
            }

            //Playlist
            AutoPlay_CheckBox.IsChecked = BmpPigeonhole.Instance.PlaylistAutoPlay;

            if (BmpPigeonhole.Instance.UsePluginForKeyOutput)
            {
                SkinUiBox.Visibility = Visibility.Visible;
                Sp_DalamudKeyOut.Visibility = Visibility.Visible;
                Sp_DalamudKeyOut.IsChecked = BmpPigeonhole.Instance.UsePluginForKeyOutput;
            }
        }

        #region Orchestra
        private void LocalOrchestraBox_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.LocalOrchestra = LocalOrchestraBox.IsChecked ?? false;
        }

        private void KeepTrackSettingsBox_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.EnsembleKeepTrackSetting = KeepTrackSettingsBox.IsChecked ?? false;
        }

        private void IgnoreProgchangeBox_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.IgnoreProgChange = IgnoreProgchangeBox.IsChecked ?? false;
        }

        private void Autostart_source_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int d = Autostart_source.SelectedIndex;
            BmpPigeonhole.Instance.AutostartMethod = (int)d;
        }


        private void AutoEquipBox_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.AutoEquipBards = AutoEquipBox.IsChecked ?? false;
            Globals.Globals.ReloadConfig();
        }

        private void AutoselectHost_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.AutoselectHost = AutoselectHostBox.IsChecked ?? false;
            Globals.Globals.ReloadConfig();
        }

        private void StartBardIndividually_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.EnsembleStartIndividual = StartBardIndividuallyBox.IsChecked ?? false;
        }
        #endregion

        #region Playback
        private void Hold_Notes_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.HoldNotes = HoldNotesBox.IsChecked ?? false;
        }

        private void Force_Playback_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.ForcePlayback = ForcePlaybackBox.IsChecked ?? false;
        }

        private void MIDI_Input_Device_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var d = (KeyValuePair<int, string>)MIDI_Input_DeviceBox.SelectedItem;
            BmpPigeonhole.Instance.MidiInputDev = d.Key;
            if (d.Key == -1)
            {
                BmpMaestro.Instance.CloseInputDevice();
                return;
            }

            BmpMaestro.Instance.OpenInputDevice(d.Key);
        }

        private void LiveMidiDelay_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.LiveMidiPlayDelay = (LiveMidiDelay.IsChecked ?? false);
        }
        #endregion

        #region Misc
        private void AMPInFrontBox_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.BringBMPtoFront = AMPInFrontBox.IsChecked ?? false;
        }

        private void MultiBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!BmpPigeonhole.Instance.EnableMultibox)
            {
                Task.Run(() =>
                {
                    foreach (var game in BmpSeer.Instance.Games.Values)
                        game.KillMutant(true);
                });
            }
            BmpPigeonhole.Instance.EnableMultibox = MultiBox_Box.IsChecked ?? false;
        }

        private void AutoequipDalamud_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.UsePluginForInstrumentOpen = AutoequipDalamud.IsChecked ?? false;
        }

        private void MidiBard_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.MidiBardCompatMode = MidiBardComp.IsChecked ?? false;
        }

        private void SkinUiBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)SkinUiBox.IsChecked)
            {
                var openFileDialog = new FolderPicker();
                if (openFileDialog.ShowDialog() != true)
                    return;

                if (openFileDialog.ResultPath == "")
                    return;

                BmpPigeonhole.Instance.LastSkin = openFileDialog.ResultPath+ "\\Skin.dll";
            }
            BmpPigeonhole.Instance.ClassicUi = !(SkinUiBox.IsChecked ?? true);
        }
        #endregion

        private void Sp_DalamudKeyOut_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.UsePluginForKeyOutput = (Sp_DalamudKeyOut.IsChecked ?? true);
        }

        private static readonly Key[] KonamiCode = { Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A };
        private readonly Queue<Key> _inputKeys = new Queue<Key>();
        private void Classic_MainView_KeyDown(object sender, KeyEventArgs e)
        {
            if (IsCompletedBy(e.Key))
            {
                Sp_DalamudKeyOut.Visibility = Visibility.Visible;
                SkinUiBox.Visibility = Visibility.Visible;
                this.KeyDown -= Classic_MainView_KeyDown;
            }
        }
        public bool IsCompletedBy(Key inputKey)
        {
            _inputKeys.Enqueue(inputKey);

            while (_inputKeys.Count > KonamiCode.Length)
                _inputKeys.Dequeue();

            return _inputKeys.SequenceEqual(KonamiCode);
        }
    }
}
