using BardMusicPlayer.Coffer;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Ui.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Resources;

namespace BardMusicPlayer.Ui.Classic
{
    public partial class Classic_MainView : UserControl
    {
        private void LoadConfig()
        {
            this.AutoPlay_CheckBox.IsChecked = BmpPigeonhole.Instance.PlaylistAutoPlay;

            this.Autostart_source.SelectedIndex = BmpPigeonhole.Instance.AutostartMethod;

            this.LocalOrchestraBox.IsChecked = BmpPigeonhole.Instance.LocalOrchestra;
            this.HoldNotesBox.IsChecked = BmpPigeonhole.Instance.HoldNotes;
            this.ForcePlaybackBox.IsChecked = BmpPigeonhole.Instance.ForcePlayback;

            MIDI_Input_DeviceBox.Items.Clear();
            MIDI_Input_DeviceBox.ItemsSource = Maestro.Utils.MidiInput.ReloadMidiInputDevices();
            this.MIDI_Input_DeviceBox.SelectedIndex = BmpPigeonhole.Instance.MidiInputDev+1;

            this.SkinUiBox.IsChecked = !BmpPigeonhole.Instance.ClassicUi;

            this.Settings_EffectsHost.IsChecked = BmpPigeonhole.Instance.IsChoreoHost;
        }

        private void Autostart_source_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int d = Autostart_source.SelectedIndex;
            BmpPigeonhole.Instance.AutostartMethod = (int)d;
        }

        private void LocalOrchestraBox_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.LocalOrchestra = LocalOrchestraBox.IsChecked ?? false;
        }

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

        private void SkinUiBox_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.ClassicUi = !(SkinUiBox.IsChecked ?? true);
        }

        private void Settings_EffectsHost_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.IsChoreoHost = Settings_EffectsHost.IsChecked ?? false;
        }
    }
}
