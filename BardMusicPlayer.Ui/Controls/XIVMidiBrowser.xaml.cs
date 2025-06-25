/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// The XIVMidiBrowser
    /// </summary>
    public sealed partial class XIVMidiBrowser : UserControl
    {
        public EventHandler<string> OnLoadSongFromBrowser;
        public EventHandler<string> OnLoadSongFromBrowserToPreview;
        public EventHandler<string> OnAddSongFromBrowser;
        private static string DownloadUrl { get; } = "https://xivmidi.com";
        public string DownloadOption { get; set; } = "";

        /// Temporary sender object
        private object _Sender { get; set; } = null;

        public XIVMidiBrowser()
        {
            InitializeComponent();
            RefreshContainer();

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
                foreach (var file in data.files)
                {
                    try
                    {
                        if (file.website_file_path == null)
                            continue;
                        list.Add(file.website_file_path, (file.artist ?? "") + " - " + file.title ?? "");
                    }
                    catch { }
                }
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SongbrowserContainer.ItemsSource = list;
                }));
            }
            else if (e is XIVMIDI.IO.ResponseContainer.MidiFile)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var data = e as XIVMIDI.IO.ResponseContainer.MidiFile;
                    if (!Directory.Exists(App.TempPath))
                        Directory.CreateDirectory(App.TempPath);
                    string filename = Path.GetDirectoryName(App.TempPath) + "\\" + data.Filename;
                    File.WriteAllBytes(filename, data.data);

                    if (File.Exists(filename))
                    {
                        if (this.DownloadOption.Equals("OnLoadSongFromBrowser"))
                            OnLoadSongFromBrowser?.Invoke(this, filename);
                        else if (this.DownloadOption.Equals("OnAddSongFromBrowser"))
                            OnAddSongFromBrowser?.Invoke(this, filename);
                        else if (this.DownloadOption.Equals("OnLoadSongFromBrowserToPreview"))
                            OnLoadSongFromBrowserToPreview?.Invoke(this, filename);
                    }
                    DownloadOption = "";
                }));

            }
        }

        private void RefreshContainer()
        {
            
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
            RefreshContainer();
        }

        /// <summary>
        /// Sets the songs folder path by typing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SongPath_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            RefreshContainer();
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
        /// Get the filename from the sender
        /// </summary>
        /// <param name="sender"></param>
        /// <returns></returns>
        private string GetFilenameFromSender(object sender)
        {
            var filename = "";
            if (sender is ListViewItem)
            {
                KeyValuePair<string, string> f = (KeyValuePair<string, string>)(sender as ListViewItem).Content;
                filename = f.Key;
            }
            _Sender = null; //set the sender to null

            if (filename == "")
                filename = GetFilenameFromSelection();

            return filename;
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
                Url = new XIVMIDI.IO.RequestBuilder() { Performers = PerformerSize_box.SelectedIndex }.BuildRequest(),
                Host = "xivmidi.com",
                Requester = XIVMIDI.IO.Requester.JSON
            });
        }
    }
}
