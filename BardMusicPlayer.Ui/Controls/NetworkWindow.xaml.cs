/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree;
using BardMusicPlayer.Jamboree.Events;
using BardMusicPlayer.Transmogrify.Song;
using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// The NetworkCtrl
    /// </summary>
    public partial class NetworkControl : UserControl
    {
        public EventHandler<BmpSong> OnLoadSongFromNetwork;

        private object _Sender { get; set; } = null;
        private ObservableCollection<CharacterState> Members { get; set; } = new ObservableCollection<CharacterState>();

        public NetworkControl()
        {
            InitializeComponent();


            BmpJamboree.Instance.OnPartyLog += Instance_PartyLog;
            BmpJamboree.Instance.OnPartyDebugLog += Instance_PartyDebugLog;

            this.DataContext = this;
        }

        #region EventHandlers
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
    }
}
