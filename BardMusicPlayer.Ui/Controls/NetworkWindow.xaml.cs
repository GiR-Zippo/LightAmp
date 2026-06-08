/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree;
using BardMusicPlayer.Jamboree.Events;
using BardMusicPlayer.Quotidian.Structs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Transmogrify.Song;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// The NetworkCtrl
    /// </summary>
    public partial class NetworkControl : UserControl
    {
        public EventHandler<BmpSong> OnLoadSongFromNetwork;

        private object _Sender { get; set; } = null;
        private ObservableCollection<SessionMembers> Members { get; set; } = new ObservableCollection<SessionMembers>();

        public NetworkControl()
        {
            InitializeComponent();

            //Party events
            BmpJamboree.Instance.OnPartyCreated += Instance_PartyCreated;
            BmpJamboree.Instance.OnPartyJoined += Instance_PartyJoined;
            BmpJamboree.Instance.OnPartyChanged += Instance_PartyChanged;

            //playlist and playback events
            BmpJamboree.Instance.OnPlaylistChangedEvent += Instance_PlaylistChanged;
            BmpJamboree.Instance.OnPartySelectSong += Instance_PartySelectSong;

            BmpJamboree.Instance.OnPartyLog += Instance_PartyLog;
            BmpJamboree.Instance.OnPartyDebugLog += Instance_PartyDebugLog;

            this.DataContext = this;
            PartyList.ItemsSource = Members;
            UpdateSongList();
        }

        #region EventHandlers

        /// <summary>
        /// Triggered when a party was created, contains the party code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_PartyCreated(object sender, PartyCreatedEvent e)
        {
            if (!e.Connected)
                return;

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                PartyToken_Text.Text = e.Code;
                Create_Join_Btn.Content = "Leave";
            }));
        }

        /// <summary>
        /// Triggered when a party was joined
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_PartyJoined(object sender, PartyJoinedEvent e)
        { 
            if (e.Connected)
                this.Dispatcher.BeginInvoke(new Action(() => Create_Join_Btn.Content = "Leave"));
        }

        /// <summary>
        /// Triggered when the party was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        private async void Instance_PartyChanged(object sender, PartyChangedEvent ev)
        {
            var current = BmpJamboree.Instance.GetCurrentPartyMembers();
            await this.Dispatcher.BeginInvoke(new Action(() =>
            {
                Members.Clear();
                foreach (var m in current)
                    Members.Add(m);
            }));

            await this.Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var member in Members)
                {
                    var row = PartyList.ItemContainerGenerator.ContainerFromItem(member) as DataGridRow;
                    if (row == null) continue;

                    var combo = FindVisualChild<ComboBox>(row);
                    if (combo == null) continue;

                    if (Instrument.TryParse(member.instrument, out Instrument instr))
                        combo.SelectedIndex = instr.Index - 1;
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Triggered if playlist has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        private async void Instance_PlaylistChanged(object sender, PartyPlaylistChangeEvent ev)
        {
            await this.Dispatcher.BeginInvoke(new Action(() =>
            {
                Dictionary<string, string> list = new Dictionary<string, string>();
                foreach (var data in ev.Playlist)
                    list.Add(data.itemId, data.filename);
                SongContainer.ItemsSource = list;
            }));
        }

        /// <summary>
        /// The song selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        public async void Instance_PartySelectSong(object sender, PartySelectSongEvent ev)
        {
            await this.Dispatcher.BeginInvoke(new Action(() =>
            {
                var foundItem = SongContainer.Items
                        .Cast<KeyValuePair<string, string>>()
                        .FirstOrDefault(item => item.Key.SequenceEqual(ev.SongId));
                int index = SongContainer.Items.IndexOf(foundItem);
                SongContainer.SelectedIndex = index;
                var data = BmpJamboree.Instance.GetMidiData(foundItem.Key);
                var song = BmpSong.ImportMidiFromByte(data, foundItem.Value).Result;
                OnLoadSongFromNetwork?.Invoke(this, song);
            }));
        }

        /// <summary>
        /// Party log function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_PartyLog(object sender, PartyLogEvent e)
        {
            string logtext = e.LogString;
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyMessage_Text.Text = this.PartyMessage_Text.Text + logtext + "\r\n"));
        }

        /// <summary>
        /// Debug log function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_PartyDebugLog(object sender, PartyDebugLogEvent e)
        {
            string logtext = e.LogString;
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + logtext));
        }

        #endregion

        /// <summary>
        /// Upload a song to party
        /// </summary>
        /// <param name="filename"></param>
        public void UploadSong(string filename)
        { 
            if (filename == null) return;
            if (filename .Length == 0) return;
            BmpJamboree.Instance.SendPlaylist(filename);
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
                Members.Clear();
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


        private async void SongContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic item = SongContainer.SelectedItem;
            string filename = item.Key;
            await BmpJamboree.Instance.SetSong(filename);
        }

        private void OnListViewItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _Sender = sender; //set the sender to the item we hovered over
            e.Handled = true;
        }

        private void UpdateSongList()
        {
            
        }

        #region MemberControl

        #region TrackControl
        /// <summary>
        /// Track Minus button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TrackMinus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Parent is Grid grid)
            {
                var textBox = grid.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == "TrackBox");
                if (textBox != null && int.TryParse(textBox.Text, out int currentTrack))
                {
                    if (currentTrack > 1)
                    {
                        textBox.Text = (currentTrack - 1).ToString();
                        if (button.DataContext is SessionMembers member)
                            BmpJamboree.Instance.SetTrack(member.memberId, (int)member.trackNumber);
                    }
                }
            }
        }

        /// <summary>
        /// Track Plus Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackPlus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Parent is Grid grid)
            {
                var textBox = grid.Children.OfType<TextBox>().FirstOrDefault(x => x.Name == "TrackBox");
                if (textBox != null && int.TryParse(textBox.Text, out int currentTrack))
                {
                    textBox.Text = (currentTrack + 1).ToString();
                    if (button.DataContext is SessionMembers member)
                        BmpJamboree.Instance.SetTrack(member.memberId, (int)member.trackNumber);
                }
            }
        }

        /// <summary>
        /// Textbox to edit
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox button && button.DataContext is SessionMembers member)
            {
                int value = -1;
                e.Handled = !int.TryParse(e.Text, out value);
                member.trackNumber = value;
                BmpJamboree.Instance.SetTrack(member.memberId, (int)member.trackNumber);
            }
        }
        #endregion

        private void InstrumentComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is SessionMembers member)
            {
                if (!comboBox.IsMouseOver)
                    return;
                Instrument newInstrument;
                if(Instrument.TryParse(comboBox.SelectedIndex+1, out newInstrument))
                {
                    member.instrument = newInstrument.Name;
                    BmpJamboree.Instance.SetInstrument(member.memberId, newInstrument);
                }
            }
        }

        #endregion

        /// <summary>
        /// Helper to find the stuff in members
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
        #endregion
    }
}
