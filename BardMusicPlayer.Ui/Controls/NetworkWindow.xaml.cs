/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree;
using BardMusicPlayer.Jamboree.Events;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// The NetworkCtrl
    /// </summary>
    public partial class NetworkControl : UserControl
    {
        private object _Sender { get; set; } = null;
        public ObservableCollection<SessionMembers> Members { get; set; } = new ObservableCollection<SessionMembers>();

        public NetworkControl()
        {
            InitializeComponent();

            BmpJamboree.Instance.OnPartyCreated += Instance_PartyCreated;
            BmpJamboree.Instance.OnPartyJoined += Instance_PartyJoined;
            BmpJamboree.Instance.OnPartyManifest += Instance_PartyManifest;
            BmpJamboree.Instance.OnMidiReceived += Instance_MidiReceived;

            BmpJamboree.Instance.OnPartyLog += Instance_PartyLog;
            BmpJamboree.Instance.OnPartyDebugLog += Instance_PartyDebugLog;

            this.DataContext = this;
            UpdateSongList();
        }


        private void Instance_PartyCreated(object sender, PartyCreatedEvent e)
        {
            string token = e.Data.code;
            this.Dispatcher.BeginInvoke(new Action(() => PartyToken_Text.Text = token));
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + "Code: " + e.Data.code+"\r\n"));
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + "Session ID: " + e.Data.sessionId + "\r\n"));
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + "Hosttoken: " + e.Data.hostToken + "\r\n"));
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + "Expires: " + e.Data.expiresAt + "\r\n"));
            this.Dispatcher.BeginInvoke(new Action(() => Create_Join_Btn.Content = "Leave"));
        }

        private void Instance_PartyJoined(object sender, PartyJoinedEvent e)
        { 
            var t = e.Data;
            this.Dispatcher.BeginInvoke(new Action(() => Create_Join_Btn.Content = "Leave"));
        }

        private async void Instance_PartyManifest (object sender, SessionManifestEvent e)
        {
            if (e.Data == null)
                return;

            if (e.Data.members != null)
            {
                await this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Members.Clear();
                    foreach (var member in BmpJamboree.Instance.GetCurrentPartyMembers())
                        Members.Add(member);
                }));
            }

            if (e.Data.items == null)
                return;

            foreach (var item in e.Data.items)
            {
                await BmpJamboree.Instance.GetMidiFile(item.itemId);
            }
        }

        private async void Instance_MidiReceived(object sender, PartyMidiEvent e)
        {
            if (e.Data == null)
                return;
            var playlist = BmpJamboree.Instance.GetPlaylist();
            if (playlist == null)
                return;

            var item = playlist.Where(n => n.itemId == e.fileId).FirstOrDefault();
            if (item == default)
                return;

            await this.Dispatcher.BeginInvoke(new Action(() =>
            {
                Dictionary<byte[], string> list = new Dictionary<byte[], string>();
                list.Add(e.Data, item.filename);
                SongContainer.ItemsSource = list;
            }));
        }

        private void Instance_PartyLog(object sender, PartyLogEvent e)
        {
            string logtext = e.LogString;
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyMessage_Text.Text = this.PartyMessage_Text.Text + logtext + "\r\n"));
        }

        private void Instance_PartyDebugLog(object sender, PartyDebugLogEvent e)
        {
            string logtext = e.LogString;
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + logtext));
        }



        private void Leave_Click(object sender, RoutedEventArgs e)
        {
            BmpJamboree.Instance.LeaveParty();
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
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

            BmpJamboree.Instance.SendPlaylist(openFileDialog.FileNames.ToList());
        }

        public void UploadSong(string filename)
        { 
            if (filename == null) return;
            if (filename .Length == 0) return;

            BmpJamboree.Instance.SendPlaylist(filename);
        }

        private async void ForcePlay_Click(object sender, RoutedEventArgs e)
        {
            //await BMPApi.Instance.GetPlaylist();
            /*var openFileDialog = new OpenFileDialog
            {
                Filter = Globals.Globals.FileFilters,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            if (!openFileDialog.CheckFileExists)
                return;

            await BMPApi.Instance.SendPlaylist(openFileDialog.FileNames.ToList());*/
        }


        #region Ui Stuff
        /// <summary>
        /// The Create/Join/Leave Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Create_Join_Click(object sender, RoutedEventArgs e)
        {
            if (Create_Join_Btn.Content.ToString() == "Leave")
            {
                BmpJamboree.Instance.LeaveParty();
                Create_Join_Btn.Content = "Create";
                PartyToken_Text.Text = "";
                return;
            }
            if (BmpJamboree.Instance.IsConnected())
                return;

            string token = PartyToken_Text.Text;
            if (token == "")
            {
                BmpJamboree.Instance.CreateParty();
                return;
            }
            else
                BmpJamboree.Instance.JoinParty(token, "Insert Coin");
        }

        /// <summary>
        /// Textbox for party code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PartyToken_Text_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!PartyToken_Text.IsFocused)
                return;

            if (BmpJamboree.Instance.IsConnected())
                return;

            if (PartyToken_Text.Text == "")
                Create_Join_Btn.Content = "Create";
            else
                Create_Join_Btn.Content = "Join";
        }


        private void SongContainer_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
        }

        private void OnListViewItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _Sender = sender; //set the sender to the item we hovered over
            e.Handled = true;
        }

        private void UpdateSongList()
        {
            
        }

        #endregion

        #region MemberControl

        private void TrackMinus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SessionMembers member)
            {
                // Wert verringern, aber nicht unter 0 gehen (falls gewünscht)
                if (member.trackNumber > 0)
                {
                    member.trackNumber--;

                    // Dem DataGrid Bescheid stoßen, dass sich der Wert geändert hat
                    // (Nötig, weil Record kein INotifyPropertyChanged hat)
                    var cell = FindParent<DataGridCell>(button);
                    if (cell != null) cell.BindingGroup?.CommitEdit();
                }
            }
        }

        private void TrackPlus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is SessionMembers member)
            {
                // Falls trackNumber null war, fangen wir bei 0 an, sonst einfach hochzählen
                member.trackNumber = (member.trackNumber ?? 0) + 1;

                // UI-Aktualisierung erzwingen
                var cell = FindParent<DataGridCell>(button);
                if (cell != null) cell.BindingGroup?.CommitEdit();
            }
        }

        // Kleiner WPF-Helper, um die Zelle im Visual Tree zu finden (für den UI-Refresh)
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Erlaubt nur Ziffern
            e.Handled = !int.TryParse(e.Text, out _);
        }

        // 3. Erlaubt Hoch-/Runter-Zählen mit dem Mausrad
        private void NumericTextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                int.TryParse(textBox.Text, out int value);

                if (e.Delta > 0)
                {
                    textBox.Text = (value + 1).ToString();
                }
                else if (e.Delta < 0)
                {
                    if (value > 0)
                        textBox.Text = (value - 1).ToString();
                }
                e.Handled = true;
            }
        }

        // 4. Markiert den Text beim Reinklicken sofort (erleichtert das Tippen)
        private void NumericTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        #endregion



    }
}
