/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree;
using BardMusicPlayer.Jamboree.Events;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Windows;
using System.Windows.Controls;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// The songbrowser but much faster than the BMP 1.x had
    /// </summary>
    public partial class NetworkControl : UserControl
    {
        public NetworkControl()
        {
            InitializeComponent();

            BmpJamboree.Instance.OnPartyCreated += Instance_PartyCreated;
            BmpJamboree.Instance.OnPartyJoined += Instance_PartyJoined;
            BmpJamboree.Instance.OnPartyPlaylist += Instance_PartyPlaylist;
            BmpJamboree.Instance.OnMidiReceived += Instance_MidiReceived;

            BmpJamboree.Instance.OnPartyLog += Instance_PartyLog;
            BmpJamboree.Instance.OnPartyDebugLog += Instance_PartyDebugLog;
        }


        private void Instance_PartyCreated(object sender, PartyCreatedEvent e)
        {
            string token = e.Data.code;
            this.Dispatcher.BeginInvoke(new Action(() => PartyToken_Text.Text = token));
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + "Code: " + e.Data.code+"\r\n"));
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + "Session ID: " + e.Data.sessionId + "\r\n"));
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + "Hosttoken: " + e.Data.hostToken + "\r\n"));
            this.Dispatcher.BeginInvoke(new Action(() => this.PartyLog_Text.Text = this.PartyLog_Text.Text + "Expires: " + e.Data.expiresAt + "\r\n"));
        }

        private void Instance_PartyJoined(object sender, PartyJoinedEvent e)
        { 
            var t = e.Data;
            Console.WriteLine("");
        }

        private async void Instance_PartyPlaylist(object sender, PartyPlaylistEvent e)
        {
            if (e.Data == null)
                return;
            if (e.Data.items == null)
                return;
            foreach (var item in e.Data.items)
            {
                await BMPApi.Instance.GetMidiFile(item.itemId);
            }
        }

        private async void Instance_MidiReceived(object sender, PartyMidiEvent e)
        {
            if (e.Data == null)
                return;
            File.WriteAllBytes("D:\\tmp\\output.mid", e.Data);
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

        private async void Join_Click(object sender, RoutedEventArgs e)
        {
            string token = PartyToken_Text.Text;
            if (token == "")
            {
                await BMPApi.Instance.CreateSession();
                return;
            }
            else
                await BMPApi.Instance.JoinParty(token);
        }

        private void Leave_Click(object sender, RoutedEventArgs e)
        {
            BMPApi.Instance.LeaveParty();
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

            await BMPApi.Instance.SendPlaylist(openFileDialog.FileNames.ToList());
        }

        private async void ForcePlay_Click(object sender, RoutedEventArgs e)
        {
            await BMPApi.Instance.GetPlaylist();
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


    }
}
