/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.XIVMIDI;
using BardMusicPlayer.XIVMIDI.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// The XIVMidiBrowser
    /// </summary>
    public sealed partial class XIVMidiBrowser : UserControl
    {
        private enum DownloadOption
        {
            OnLoadSongFromBrowser = 0,
            OnAddSongFromBrowser,
            OnLoadSongFromBrowserToPreview
        }

        public EventHandler<BmpSong> OnLoadSongFromBrowser;
        public EventHandler<BmpSong> OnLoadSongFromBrowserToPreview;
        public EventHandler<BmpSong> OnAddSongFromBrowser;

        /// Temporary sender object
        private object _Sender { get; set; } = null;

        public XIVMidiBrowser()
        {
            InitializeComponent();

            XIVMidiApi.Instance.OnBMPSongList += Instance_OnBMPSongList;
            XIVMidiApi.Instance.OnXIVSongList += Instance_OnXIVSongList;
            XIVMidiApi.Instance.OnXIVMidiFile += Instance_OnMidiFile;
            XIVMidiApi.Instance.OnXIVRequestError += Instance_OnRequestError;

            Source_box.ItemsSource = Misc.Sources.Values;
            Source_box.SelectedIndex = 0;

            PerformerSize_box.ItemsSource = Misc.PerformerSize.Values;
            PerformerSize_box.SelectedIndex = 1;
        }

        #region callback handlers
        private void Instance_OnBMPSongList(object sender, XIVMidiBMPSongsEvent e)
        {
            Dictionary<string, string> list = new Dictionary<string, string>();
            foreach (var file in e.Songs.docs)
            {
                try
                {
                    if (file.url.Length <= 2)
                        continue;
                    list.Add(file.url, (file.artist ?? "") + " - " + (file.title ?? "") + " - " + (file.arranger ?? ""));
                }
                catch { }
            }
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                SongbrowserContainer.ItemsSource = list;
                SongbrowserContainer.Items.Filter = RefreshContainer;
            }));
        }

        private void Instance_OnXIVSongList(object sender, XIVMidiXIVSongsEvent e)
        {
            Dictionary<string, string> list = new Dictionary<string, string>();
            foreach (var file in e.Songs.data.files)
            {
                try
                {
                    if (file.websiteFilePath.Length <= 2)
                        continue;
                    list.Add(file.websiteFilePath, (file.artist ?? "") + " - " + (file.title ?? "") + " - " + (file.editor ?? ""));
                }
                catch { }
            }
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                SongbrowserContainer.ItemsSource = list;
                SongbrowserContainer.Items.Filter = RefreshContainer;
            }));
        }

        private void Instance_OnMidiFile(object sender, XIVMidiFileEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                DownloadOption option = (DownloadOption)e.Arguments;
                if (option == DownloadOption.OnAddSongFromBrowser)
                    OnAddSongFromBrowser?.Invoke(this, BmpSong.ImportMidiFromByte(e.MidiData.data, e.MidiData.Filename).Result);
                else if (option == DownloadOption.OnLoadSongFromBrowser)
                    OnLoadSongFromBrowser?.Invoke(this, BmpSong.ImportMidiFromByte(e.MidiData.data, e.MidiData.Filename).Result);
                else if (option == DownloadOption.OnLoadSongFromBrowserToPreview)
                    OnLoadSongFromBrowserToPreview?.Invoke(this, BmpSong.ImportMidiFromByte(e.MidiData.data, e.MidiData.Filename).Result);
            }));
        }

        private void Instance_OnRequestError(object sender, XIVMidiApiErrorEvent e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.ErrorCode == 503)
                    SongbrowserContainer.ItemsSource = new Dictionary<string, string> { { "none", "Service not available" } };
                else
                    SongbrowserContainer.ItemsSource = new Dictionary<string, string> { { "none", e.Message } };
            }));
        }

        #endregion

        private bool RefreshContainer(object item)
        {
            if (String.IsNullOrEmpty(SongSearch.Text))
                return true;
            else
                return ((KeyValuePair<string, string>) item).Value.IndexOf(SongSearch.Text, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Load the doubleclicked song into the sequencer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SongbrowserContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string filename = GetFilenameFromSelection();
            if (filename == "")
                return;
            DownloadSong(filename, DownloadOption.OnLoadSongFromBrowser);
        }

        /// <summary>
        /// Sets the search parameter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SongSearch_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            CollectionViewSource.GetDefaultView(SongbrowserContainer.ItemsSource).Refresh();
        }

        /// <summary>
        /// Sets the songs folder path by folderselection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SendRequest();
        }

        /// <summary>
        /// Handle the right click on an item from ListView
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnListViewItemPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _Sender = sender; //set the sender to the item we hovered over
            e.Handled = true;
        }

        /// <summary>
        /// Handle add to playlist context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            string filename = GetFilenameFromSelection();
            if (filename == "")
                return;
            DownloadSong(filename, DownloadOption.OnAddSongFromBrowser);
        }

        /// <summary>
        /// Handle the load to preview context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadSongToPreview(object sender, RoutedEventArgs e)
        {
            string filename = GetFilenameFromSelection();
            if (filename == "")
                return;
            DownloadSong(filename, DownloadOption.OnLoadSongFromBrowserToPreview);
        }

        /// <summary>
        /// Get the selected filename
        /// </summary>
        /// <returns></returns>
        private string GetFilenameFromSelection()
        {
            try
            {
                var filename = SongbrowserContainer.SelectedItems.OfType<KeyValuePair<string, string>>();
                return filename.First().Key;
            }
            catch
            {
                return "";
            }
        }

        private void Source_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SendRequest();
        }

        private void PerformerSize_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SendRequest();
        }

        private void SendRequest()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                SongbrowserContainer.ItemsSource = new Dictionary<string, string> { { "none", "Loading..." } };
            }));

            if (Source_box.SelectedIndex == 0) //XIVMIDI
                XIVMidiApi.Instance.GetSonglist(new XIVMIDI.IO.XIVMIDIRequestBuilder() { bandSize = PerformerSize_box.SelectedIndex });
            else //BMPAPI
                XIVMidiApi.Instance.GetSonglist(new XIVMIDI.IO.BMPAPIRequestBuilder() { bandSize = PerformerSize_box.SelectedIndex });
        }

        private void DownloadSong(string filename, DownloadOption DownloadOption)
        {
            if (filename.Contains(" "))
                filename = Uri.EscapeUriString(filename);

            XIVMidiApi.Instance.GetMidiFile(filename, DownloadOption, Source_box.SelectedIndex == 1);
        }
    }
}
