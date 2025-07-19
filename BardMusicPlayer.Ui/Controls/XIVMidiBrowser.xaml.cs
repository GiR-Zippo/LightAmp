/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Transmogrify.Song;
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
        public EventHandler<BmpSong> OnLoadSongFromBrowser;
        public EventHandler<BmpSong> OnLoadSongFromBrowserToPreview;
        public EventHandler<BmpSong> OnAddSongFromBrowser;
        private static string DownloadUrl { get; } = "https://xivmidi.com";
        public string DownloadOption { get; set; } = "";

        /// Temporary sender object
        private object _Sender { get; set; } = null;

        public XIVMidiBrowser()
        {
            InitializeComponent();

            XIVMIDI.XIVMIDI.Instance.OnRequestFinished += Instance_RequestFinished;

            PerformerSize_box.ItemsSource = XIVMIDI.IO.Misc.PerformerSize.Values;
            PerformerSize_box.SelectedIndex = 1;
        }

        private void Instance_RequestFinished(object sender, object e)
        {
            if (e == null)
                return;

            if (e is XIVMIDI.IO.GetRequest)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SongbrowserContainer.ItemsSource = new Dictionary<string, string> { { "none", "Service not available" } };
                }));
            }

            if (e is XIVMIDI.IO.ResponseContainer.ApiResponse)
            {
                var data = e as XIVMIDI.IO.ResponseContainer.ApiResponse;
                Dictionary<string, string> list = new Dictionary<string, string>();
                foreach (var file in data.data.files)
                {
                    try
                    {
                        if (!file.websitePublished)
                            continue;
                        list.Add(file.websiteFilePath, (file.artist ?? "") + " - " + (file.title ?? "") + " - " + (file.editor?? ""));
                    }
                    catch { }
                }
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SongbrowserContainer.ItemsSource = list;
                    SongbrowserContainer.Items.Filter = RefreshContainer;
                }));
            }
            else if (e is XIVMIDI.IO.ResponseContainer.MidiFile)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var data = e as XIVMIDI.IO.ResponseContainer.MidiFile;
                    if (this.DownloadOption.Equals("OnLoadSongFromBrowser"))
                        OnLoadSongFromBrowser?.Invoke(this, BmpSong.ImportMidiFromByte(data.data, data.Filename).Result);
                    else if (this.DownloadOption.Equals("OnAddSongFromBrowser"))
                        OnAddSongFromBrowser?.Invoke(this, BmpSong.ImportMidiFromByte(data.data, data.Filename).Result);
                    else if (this.DownloadOption.Equals("OnLoadSongFromBrowserToPreview"))
                        OnLoadSongFromBrowserToPreview?.Invoke(this, BmpSong.ImportMidiFromByte(data.data, data.Filename).Result);
                    DownloadOption = "";
                }));
            }
        }

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

            DownloadOption = "OnLoadSongFromBrowser";
            XIVMIDI.XIVMIDI.Instance.AddToQueue(new XIVMIDI.IO.GetRequest()
            {
                Url = DownloadUrl + Uri.EscapeUriString(filename),
                Host = "xivmidi.com",
                Accept = "audio/midi",
                Requester = XIVMIDI.IO.Requester.DOWNLOAD
            });
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

            DownloadOption = "OnAddSongFromBrowser";
            XIVMIDI.XIVMIDI.Instance.AddToQueue(new XIVMIDI.IO.GetRequest()
            {
                Url = DownloadUrl + Uri.EscapeUriString(filename),
                Host = "xivmidi.com",
                Accept = "audio/midi",
                Requester = XIVMIDI.IO.Requester.DOWNLOAD
            });
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

            DownloadOption = "OnLoadSongFromBrowserToPreview";
            XIVMIDI.XIVMIDI.Instance.AddToQueue(new XIVMIDI.IO.GetRequest()
            {
                Url = DownloadUrl + Uri.EscapeUriString(filename),
                Host = "xivmidi.com",
                Accept = "audio/midi",
                Requester = XIVMIDI.IO.Requester.DOWNLOAD
            });
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

        private void PerformerSize_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SendRequest();
        }

        private void SendRequest()
        {
            XIVMIDI.XIVMIDI.Instance.AddToQueue(new XIVMIDI.IO.GetRequest()
            {
                Url = new XIVMIDI.IO.RequestBuilder() { bandSize = PerformerSize_box.SelectedIndex }.BuildRequest(),
                Host = "xivmidi.com",
                Requester = XIVMIDI.IO.Requester.JSON
            });
        }
    }
}
