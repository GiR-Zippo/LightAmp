/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Coffer;
using BardMusicPlayer.Coffer.Interfaces;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Functions;
using BardMusicPlayer.Ui.Windows;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Resources;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für Playlist.xaml
    /// </summary>
    public partial class Playlist : UserControl
    {
        private bool playlistRepeat { get; set; } = false;
        private bool playlistShuffle { get; set; } = false;
        private bool showingPlaylists { get; set; } = false;     //are we displaying the playlist or the songs
        private IPlaylist currentPlaylist { get; set; } = null;  //the current selected playlist
        private bool importInProgress { get; set; } = false;

        public EventHandler<BmpSong> OnLoadSongFromPlaylist;
        public EventHandler<bool> OnSetPlaybuttonState;
        public EventHandler<BmpSong> OnLoadSongFromPlaylistToPreview;
        public EventHandler<bool> OnHeaderLabelDoubleClick;

        public Playlist()
        {
            InitializeComponent();

            //Always start with the playlists
            showingPlaylists = true;
            //Fill the list
            PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
            Playlist_Header.Header = "Playlists";
            AutoPlay_CheckBox.IsChecked = BmpPigeonhole.Instance.PlaylistAutoPlay;
        }

        /// <summary>
        /// Plays the next song from the playlist
        /// </summary>
        public void PlayNextSong()
        {
            if (currentPlaylist == null)
                return;

            if (PlaylistContainer.Items.Count == 0)
                return;

            if (playlistShuffle)
            {
                Random rnd = new Random();
                int random = rnd.Next(1, PlaylistContainer.Items.Count);

                if (random == PlaylistContainer.SelectedIndex)
                    random = (random + 1) % PlaylistContainer.Items.Count;

                if (random == 0)
                    random = 1;

                PlaylistContainer.SelectedIndex = random;
            }
            else
            {
                if ((PlaylistContainer.SelectedIndex == -1) || (PlaylistContainer.SelectedIndex == 0))
                {
                    PlaylistContainer.SelectedIndex = 1;
                }
                else
                {
                    if (PlaylistContainer.SelectedIndex == PlaylistContainer.Items.Count - 1)
                    {
                        PlaylistContainer.SelectedIndex = 1;
                    }
                    else
                        PlaylistContainer.SelectedIndex = PlaylistContainer.SelectedIndex + 1;
                }
            }

            OnLoadSongFromPlaylist?.Invoke(this, currentPlaylist?.FirstOrDefault(item => item.Title == (string)PlaylistContainer.SelectedItem));
        }

        public void SelectSongByIndex(int idx)
        {
            //are we at MB compat?
            if (!BmpPigeonhole.Instance.MidiBardCompatMode)
                return;

            if (PlaylistContainer.Items.Count == 0)
                return;

            PlaylistContainer.SelectedIndex = idx;

            OnLoadSongFromPlaylist?.Invoke(this, currentPlaylist?.FirstOrDefault(item => item.Title == (string)PlaylistContainer.SelectedItem));
        }

        #region upper playlist button functions

        private void PlaylistLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OnHeaderLabelDoubleClick?.Invoke(this, true);
        }

        /// <summary>
        /// Create a new playlist but don't save it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_New_Button_Click(object sender, RoutedEventArgs e)
        {
            var inputbox = new TextInputWindow("Playlist Name");
            if (inputbox.ShowDialog() == true)
            {
                if (inputbox.ResponseText.Length < 1)
                    return;

                currentPlaylist = PlaylistFunctions.CreatePlaylist(inputbox.ResponseText);
                refreshPlaylistSongsAndTimes();
                showingPlaylists = false;
            }
        }

        /// <summary>
        /// Add file(s) to the selected playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (currentPlaylist == null)
                return;

            if (!PlaylistFunctions.AddFilesToPlaylist(currentPlaylist))
                return;

            refreshPlaylistSongsAndTimes();
        }

        public void AddSongToPlaylist(string filename)
        {
            if (currentPlaylist == null)
                return;

            if (PlaylistFunctions.AddFilesToPlaylist(currentPlaylist, filename))
                refreshPlaylistSongsAndTimes();

            return;
        }

        public void AddSongToPlaylist(BmpSong song)
        {
            if (currentPlaylist == null)
                return;

            if (PlaylistFunctions.AddSongToPlaylist(currentPlaylist, song))
                refreshPlaylistSongsAndTimes();

            return;
        }

        /// <summary>
        /// Add file(s) to the selected playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Playlist_Add_Button_RightClick(object sender, RoutedEventArgs e)
        {
            if (currentPlaylist == null)
                return;

            if (importInProgress)
                return;

            importInProgress = true;
            if (await PlaylistFunctions.AddFolderToPlaylist(currentPlaylist))
                refreshPlaylistSongsAndTimes();
            importInProgress = false;
        }

        /// <summary>
        /// remove a song from the playlist but don't save
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            if (currentPlaylist == null)
                return;

            if (showingPlaylists)
                return;

            foreach (string s in PlaylistContainer.SelectedItems)
            {
                BmpSong song = PlaylistFunctions.GetSongFromPlaylist(currentPlaylist, s);
                if (song == null)
                    continue;
                currentPlaylist.Remove(song);
                BmpCoffer.Instance.DeleteSong(song);
            }
            BmpCoffer.Instance.SavePlaylist(currentPlaylist);

            refreshPlaylistSongsAndTimes();
        }

        /// <summary>
        /// Delete a playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            //Showing the playlists?
            if (showingPlaylists)
            {
                if ((string)PlaylistContainer.SelectedItem == null)
                    return;

                var pls = BmpCoffer.Instance.GetPlaylist((string)PlaylistContainer.SelectedItem);
                if (pls == null)
                    return;

                BmpCoffer.Instance.DeletePlaylist(pls);
                PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
                return;
            }

            if (currentPlaylist == null)
                return;

            showingPlaylists = true;
            BmpCoffer.Instance.DeletePlaylist(currentPlaylist);
            PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
            Playlist_Header.Header = "Playlists";
            currentPlaylist = null;
        }
        #endregion

        #region PlaylistContainer actions
        /// <summary>
        /// Click on the head of the DataGrid brings you back to the playlists
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistContainer_HeaderClick(object sender, RoutedEventArgs e)
        {
            var columnHeader = sender as System.Windows.Controls.Primitives.DataGridColumnHeader;
            if (columnHeader != null)
            {
                showingPlaylists = true;
                PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
                Playlist_Header.Header = "Playlists";
                currentPlaylist = null;
            }
        }

        /// <summary>
        /// if a song or playlist in the list was doubleclicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((string)PlaylistContainer.SelectedItem == null)
                return;

            if (showingPlaylists)
            {
                if ((string)PlaylistContainer.SelectedItem == "..")
                    return;

                currentPlaylist = BmpCoffer.Instance.GetPlaylist((string)PlaylistContainer.SelectedItem);
                showingPlaylists = false;
                refreshPlaylistSongsAndTimes();
                return;
            }
            else
            {
                if ((string)PlaylistContainer.SelectedItem == (string)"..")
                {
                    showingPlaylists = true;
                    PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
                    Playlist_Header.Header = "Playlists";
                    currentPlaylist = null;
                    return;
                }
            }

            //If no playlist is active assume we are in the song selection
            if (currentPlaylist == null)
                OnLoadSongFromPlaylist?.Invoke(this, BmpCoffer.Instance.GetSong((string)PlaylistContainer.SelectedItem));
            else
                OnLoadSongFromPlaylist?.Invoke(this, PlaylistFunctions.GetSongFromPlaylist(currentPlaylist, (string)PlaylistContainer.SelectedItem));
            return;
        }

        /// <summary>
        /// Drag start function to move songs in the playlist
        /// </summary>
        private void PlaylistContainer_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock celltext && e.LeftButton == MouseButtonState.Pressed && !showingPlaylists)
                DragDrop.DoDragDrop(PlaylistContainer, celltext, DragDropEffects.Move);
        }

        /// <summary>
        /// And the drop
        /// </summary>
        private void Playlist_Drop(object sender, DragEventArgs e)
        {
            if (currentPlaylist == null)
                return;

            TextBlock droppedDataTB = e.Data.GetData(typeof(TextBlock)) as TextBlock;
            string droppedDataStr = droppedDataTB.DataContext as string;
            string target = ((TextBlock)(sender)).DataContext as string;

            if ((droppedDataStr.Equals("..")) || (target.Equals("..")))
                return;


            int removedIdx = PlaylistContainer.Items.IndexOf(droppedDataStr);
            int targetIdx = PlaylistContainer.Items.IndexOf(target);

            if (removedIdx < targetIdx)
            {
                currentPlaylist.Move(removedIdx - 1, targetIdx - 1);
                BmpCoffer.Instance.SavePlaylist(currentPlaylist);
                PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(currentPlaylist, true);

            }
            else if (removedIdx == targetIdx)
            {
                PlaylistContainer.SelectedIndex = targetIdx;
            }
            else
            {
                int remIdx = removedIdx + 1;
                if (PlaylistContainer.Items.Count + 1 > remIdx)
                {
                    currentPlaylist.Move(removedIdx - 1, targetIdx - 1);
                    BmpCoffer.Instance.SavePlaylist(currentPlaylist);
                    PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(currentPlaylist, true);
                }
            }
        }

        #endregion

        #region lower playlist button functions
        /// <summary>
        /// The playlist repeat toggle button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistRepeat_Button_Click(object sender, RoutedEventArgs e)
        {
            playlistRepeat = !playlistRepeat;
            this.PlaylistRepeat_Button.Opacity = playlistRepeat ? 1 : 0.5f;
        }

        /// <summary>
        /// The playlist shuffle toggle button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistShuffle_Button_Click(object sender, RoutedEventArgs e)
        {
            playlistShuffle = !playlistShuffle;
            this.PlaylistShuffle_Button.Opacity = playlistShuffle ? 1 : 0.5f;
        }

        /// <summary>
        /// Skips the current song (only works on autoplay)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SkipSong_Button_Click(object sender, RoutedEventArgs e)
        {
            PlayNextSong();

            if (!BmpPigeonhole.Instance.PlaylistAutoPlay)
                return;

            Random rnd = new Random();
            PlaybackFunctions.PlaySong(rnd.Next(15, 35) * 100);
            OnSetPlaybuttonState?.Invoke(this, true);
        }

        /// <summary>
        /// The Auto-Play button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoPlay_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.PlaylistAutoPlay = AutoPlay_CheckBox.IsChecked ?? false;
        }
        #endregion

        #region other "..." playlist menu function
        /// <summary>
        /// Search for a song in the playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            var inputbox = new TextInputWindow("Search for...", 80);
            inputbox.Focus();
            if (inputbox.ShowDialog() == true)
            {
                try
                {
                    //Showing all songs
                    if (currentPlaylist == null && !showingPlaylists)
                    {
                        currentPlaylist = null;
                        showingPlaylists = false;
                        PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetSongTitles().Where(x => x.ToLower().Contains(inputbox.ResponseText.ToLower())).ToList();
                        Playlist_Header.Header = "Search result";
                    }
                    else
                    {
                        var song = currentPlaylist.Where(x => x.Title.ToLower().Contains(inputbox.ResponseText.ToLower())).First();
                        PlaylistContainer.SelectedIndex = PlaylistContainer.Items.IndexOf(song.Title);
                        PlaylistContainer.ScrollIntoView(PlaylistContainer.Items[PlaylistContainer.SelectedIndex]);
                        PlaylistContainer.UpdateLayout();
                    }
                }
                catch
                {
                    MessageBox.Show("Nothing found", "Nope", MessageBoxButton.OK);
                }
            }
        }

        /// <summary>
        /// Creates a new music catalog, loads it and refreshes the listed items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_New_Cat_Button(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new SaveFileDialog
            {
                Filter = Globals.Globals.MusicCatalogFilters
            };
            openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + Globals.Globals.DataPath;

            if (openFileDialog.ShowDialog() != true)
                return;

            BmpCoffer.Instance.LoadNew(openFileDialog.FileName);
            BmpPigeonhole.Instance.LastLoadedCatalog = openFileDialog.FileName;
            PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
            showingPlaylists = true;
        }

        /// <summary>
        /// Loads a MusicCatalog, loads it and refreshes the listed items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Open_Cat_Button(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = Globals.Globals.MusicCatalogFilters,
                Multiselect = false
            };
            openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + Globals.Globals.DataPath;

            if (openFileDialog.ShowDialog() != true)
                return;

            if (!openFileDialog.CheckFileExists)
                return;

            BmpCoffer.Instance.LoadNew(openFileDialog.FileName);
            BmpPigeonhole.Instance.LastLoadedCatalog = openFileDialog.FileName;
            PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
            showingPlaylists = true;
        }

        /// <summary>
        /// the export function, triggered from the Ui
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Export_Cat_Button(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new SaveFileDialog
            {
                Filter = Globals.Globals.MusicCatalogFilters
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            BmpCoffer.Instance.Export(openFileDialog.FileName);
        }

        /// <summary>
        /// triggeres the reabase function from Coffer
        /// </summary>
        private void Playlist_Cleanup_Cat_Button(object sender, RoutedEventArgs e)
        {
            BmpCoffer.Instance.CleanUpDB();
        }

        /// <summary>
        /// triggeres the reabase function from Coffer
        /// </summary>
        private void Playlist_Import_JSon_Button(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Playlist file | *.plz",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            if (!openFileDialog.FileName.ToLower().EndsWith(".plz"))
                return;

            if (currentPlaylist == null)
                Playlist_New_Button_Click(null, null);

            if (currentPlaylist == null)
                return;

            var list = JsonPlaylist.Load(openFileDialog.FileName);
            foreach (var rawdata in list)
            {
                var song = BmpSong.ImportMidiFromByte(rawdata.Data, "dummy").Result;
                song.Title = rawdata.Name;
                currentPlaylist.Add(song);
                BmpCoffer.Instance.SaveSong(song);
            }
            BmpCoffer.Instance.SavePlaylist(currentPlaylist);
            PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(currentPlaylist, true);
        }

        /// <summary>
        /// triggeres the reabase function from Coffer
        /// </summary>
        private void Playlist_Export_JSon_Button(object sender, RoutedEventArgs e)
        {
            if (currentPlaylist == null)
                return;

            var openFileDialog = new SaveFileDialog
            {
                Filter = "Playlist file | *.plz"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            System.Collections.Generic.List<SongContainer> songs = new System.Collections.Generic.List<SongContainer>();
            foreach (var song in currentPlaylist)
            {
                SongContainer sc = new SongContainer();
                sc.Name = song.Title;
                sc.Data = song.GetExportMidi().ToArray();
                songs.Add(sc);
            }
            JsonPlaylist.Save(openFileDialog.FileName, songs);
        }

        private void Playlist_ShowSongs_Click(object sender, RoutedEventArgs e)
        {
            currentPlaylist = null;
            showingPlaylists = false;
            PlaylistContainer.ItemsSource = PlaylistFunctions.GeAllSongsInDB(true);
            Playlist_Header.Header = "All Songs";
        }

        /// <summary>
        /// Button context menu routine
        /// </summary>
        private void MenuButton_PreviewMouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            Button rectangle = sender as Button;
            ContextMenu contextMenu = rectangle.ContextMenu;
            contextMenu.PlacementTarget = rectangle;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            contextMenu.IsOpen = true;
        }
        #endregion

        private void PlaylistPreview_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var contextMenu = (ContextMenu)menuItem.Parent;
            var item = (DataGrid)contextMenu.PlacementTarget;
            String iName = item.SelectedCells[0].Item as String;
            if (iName.Equals(""))
                return;

            BmpSong song;
            if (currentPlaylist == null)
                song = BmpCoffer.Instance.GetSong(iName);
            else
                song = PlaylistFunctions.GetSongFromPlaylist(currentPlaylist, iName);

            if (song == null)
                return;

            OnLoadSongFromPlaylistToPreview?.Invoke(this, song);
        }

        /// <summary>
        /// Opens the edit window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistMetaEdit_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = (MenuItem)sender;
            var contextMenu = (ContextMenu)menuItem.Parent;
            var item = (DataGrid)contextMenu.PlacementTarget;
            String iName = item.SelectedCells[0].Item as String;
            if (iName.Equals(""))
                return;

            BmpSong song;
            if (currentPlaylist == null)
                song = BmpCoffer.Instance.GetSong(iName);
            else
                song = PlaylistFunctions.GetSongFromPlaylist(currentPlaylist, iName);

            if (song == null)
                return;

            SongEditWindow sew = new SongEditWindow(song);
        }

        #region common routines
        private void refreshPlaylistSongsAndTimes()
        {
            PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(currentPlaylist, true);
            Playlist_Header.Header = currentPlaylist.GetName().PadRight(75 - currentPlaylist.GetName().Length, ' ') + new DateTime(PlaylistFunctions.GetTotalTime(currentPlaylist).Ticks).ToString("HH:mm:ss");
        }
        #endregion

    }
}
