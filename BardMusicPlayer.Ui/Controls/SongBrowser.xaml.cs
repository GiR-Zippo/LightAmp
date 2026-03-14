/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Ui.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// The songbrowser
    /// </summary>
    public sealed partial class SongBrowser : UserControl
    {
        public EventHandler<string> OnLoadSongFromBrowser;
        public EventHandler<string> OnLoadSongFromBrowserToPreview;
        public EventHandler<string> OnAddSongFromBrowser;

        /// Temporary sender object
        private object _Sender { get; set; } = null;

        public SongBrowser()
        {
            InitializeComponent();
            SongPath.Text = BmpPigeonhole.Instance.SongDirectory;
            RefreshContainer();
        }

        private void RefreshContainer()
        {
            if (!Directory.Exists(SongPath.Text))
                return;

            string[] files = Directory.EnumerateFiles(SongPath.Text, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mid") || s.EndsWith(".mml") || s.EndsWith(".mmsong")).ToArray();

            Dictionary<string, string> list = new Dictionary<string, string>();
            string last_dir = "";

            foreach (string file in files)
            {
                if (SongSearch.Text != "")
                {
                    if (file.ToLower().Contains(SongSearch.Text.ToLower()))
                    {
                        if (Path.GetDirectoryName(file) != last_dir)
                        {
                            last_dir = Path.GetDirectoryName(file);
                            list.Add("+" + last_dir + "+", " ");
                            list.Add("-" + last_dir, "-" + last_dir);
                            list.Add("+" + last_dir + "-", "------------------------------------------------------------------");
                        }
                        list.Add(file, Path.GetFileNameWithoutExtension(file));
                    }
                }
                else
                {
                    if (Path.GetDirectoryName(file) != last_dir)
                    {
                        last_dir = Path.GetDirectoryName(file);
                        list.Add("+" + last_dir + "+", " ");
                        list.Add("-" + last_dir, "-" + last_dir);
                        list.Add("+" + last_dir + "-", "------------------------------------------------------------------");
                    }
                    list.Add(file, Path.GetFileNameWithoutExtension(file));
                }
            }
            SongbrowserContainer.ItemsSource = list;
        }

        /// <summary>
        /// Load the doubleclicked song into the sequencer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SongbrowserContainer_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string filename = GetFilenameFromSelection();
            if (filename == "")
                return;

            OnLoadSongFromBrowser?.Invoke(this, filename);
        }

        /// <summary>
        /// Sets the search parameter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SongSearch_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            RefreshContainer();
        }

        /// <summary>
        /// Sets the songs folder path by typing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SongPath_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
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
            var dlg = new FolderPicker();

            if (Directory.Exists(BmpPigeonhole.Instance.SongDirectory))
                dlg.InputPath = Path.GetFullPath(BmpPigeonhole.Instance.SongDirectory);
            else
                dlg.InputPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (dlg.ShowDialog() == true)
            {
                string path = dlg.ResultPath;
                if (!Directory.Exists(path))
                    return;

                path = path + (path.EndsWith("\\") ? "" : "\\");
                SongPath.Text = path;
                BmpPigeonhole.Instance.SongDirectory = path;
                SongSearch_PreviewTextInput(null, null);
            }
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
            string filename = GetFilenameFromSender(_Sender);
            if (filename == "")
                return;
            OnAddSongFromBrowser?.Invoke(this, filename);
        }

        /// <summary>
        /// Handle the load to preview context menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadSongToPreview(object sender, RoutedEventArgs e)
        {
            string filename = GetFilenameFromSender(_Sender);
            if (filename == "")
                return;
            OnLoadSongFromBrowserToPreview?.Invoke(this, filename);
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
                if (!File.Exists(filename.First().Key) || filename.First().Key == null)
                    return "";

                return filename.First().Key;
            }
            catch
            {
                return "";
            }
        }
    }
}
