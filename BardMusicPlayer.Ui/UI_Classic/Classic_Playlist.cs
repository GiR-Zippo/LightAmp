using BardMusicPlayer.Coffer;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Functions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Resources;

namespace BardMusicPlayer.Ui.Classic
{
    /// <summary>
    /// Interaktionslogik für Classic_MainView.xaml
    /// </summary>
    public partial class Classic_MainView : UserControl
    {
        private bool _playlistRepeat = false;
        private bool _playlistShuffle = false;
        private bool _showingPlaylists = false;     //are we displaying the playlist or the songs
        private IPlaylist _currentPlaylist = null;  //the current selected playlist

        /// <summary>
        /// Plays the next song from the playlist
        /// </summary>
        private void playNextSong()
        {
            if (PlaylistContainer.Items.Count == 0)
                return;

            if (_playlistShuffle)
            {
                Random rnd = new Random();
                int random = rnd.Next(1, PlaylistContainer.Items.Count);

                if (random >= PlaylistContainer.SelectedIndex) 
                    random = (random + 1) % PlaylistContainer.Items.Count;
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
            PlaybackFunctions.LoadSongFromPlaylist(PlaylistFunctions.GetSongFromPlaylist(_currentPlaylist, (string)PlaylistContainer.SelectedItem));
            this.SongName.Text = PlaybackFunctions.GetSongName();
            this.InstrumentInfo.Content = PlaybackFunctions.GetInstrumentNameForHostPlayer();
        }

        #region upper playlist button functions
        /// <summary>
        /// Create a new playlist but don't save it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_New_Button_Click(object sender, RoutedEventArgs e)
        {
            var inputbox = new TextInputWindow("Playlistname");
            if (inputbox.ShowDialog() == true)
            {
                _currentPlaylist = PlaylistFunctions.CreatePlaylist(inputbox.ResponseText);
                PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(_currentPlaylist, true);
                _showingPlaylists = false;
            }
        }

        /// <summary>
        /// Add file(s) to the selected playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null)
                return;

            var openFileDialog = new OpenFileDialog
            {
                Filter = Globals.Globals.FileFilters,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            foreach (var d in openFileDialog.FileNames)
            {
                BmpSong song = BmpSong.OpenFile(d).Result;
                _currentPlaylist.Add(song);
                BmpCoffer.Instance.SaveSong(song);
            }
            BmpCoffer.Instance.SavePlaylist(_currentPlaylist);
            PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(_currentPlaylist, true);
        }

        /// <summary>
        /// remove a song from the playlist but don't save
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Remove_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null)
                return;

            foreach (string s in PlaylistContainer.SelectedItems)
            {
                BmpSong song = PlaylistFunctions.GetSongFromPlaylist(_currentPlaylist, s);
                if (song == null)
                    continue;
                _currentPlaylist.Remove(song);
                BmpCoffer.Instance.DeleteSong(song);
            }
            BmpCoffer.Instance.SavePlaylist(_currentPlaylist);

            PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(_currentPlaylist, true);
        }

        /// <summary>
        /// Delete a playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null)
                return;

            _showingPlaylists = true;
            BmpCoffer.Instance.DeletePlaylist(_currentPlaylist);
            PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
        }
        #endregion

        #region PlaylistContainer actions
        /// <summary>
        /// if a song or playlist in the list was doubleclicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((string)PlaylistContainer.SelectedItem == null)
                return;

            if (_showingPlaylists)
            {
                if ((string)PlaylistContainer.SelectedItem == "..")
                    return;

                _currentPlaylist = BmpCoffer.Instance.GetPlaylist((string)PlaylistContainer.SelectedItem);
                _showingPlaylists = false;
                PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(_currentPlaylist, true);
                return;
            }
            else
            {
                if((string)PlaylistContainer.SelectedItem == (string)"..")
                {
                    _showingPlaylists = true;
                    PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
                    return;
                }
            }

            PlaybackFunctions.LoadSongFromPlaylist(PlaylistFunctions.GetSongFromPlaylist(_currentPlaylist, (string)PlaylistContainer.SelectedItem));
            this.SongName.Text = PlaybackFunctions.GetSongName();
            this.InstrumentInfo.Content = PlaybackFunctions.GetInstrumentNameForHostPlayer();
            _directLoaded = false;
            return;
        }

        /// <summary>
        /// Drag start function to move songs in the playlist
        /// </summary>
        private void PlaylistContainer_MouseMove(object sender, MouseEventArgs e)
        {
            TextBlock celltext = sender as TextBlock;
            if (celltext != null && e.LeftButton == MouseButtonState.Pressed && !_showingPlaylists)
            {
                DragDrop.DoDragDrop(PlaylistContainer, celltext, DragDropEffects.Move);
            }
        }

        /// <summary>
        /// And the drop
        /// </summary>
        private void Playlist_Drop(object sender, DragEventArgs e)
        {
            TextBlock droppedDataTB = e.Data.GetData(typeof(TextBlock)) as TextBlock;
            string droppedDataStr = droppedDataTB.DataContext as string;
            string target = ((TextBlock)(sender)).DataContext as string;

            if ((droppedDataStr.Equals("..")) || (target.Equals("..")))
                return;


            int removedIdx = PlaylistContainer.Items.IndexOf(droppedDataStr);
            int targetIdx = PlaylistContainer.Items.IndexOf(target);

            if (removedIdx < targetIdx)
            {
                _currentPlaylist.Move(removedIdx-1, targetIdx-1);
                BmpCoffer.Instance.SavePlaylist(_currentPlaylist);
                PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(_currentPlaylist, true);

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
                    _currentPlaylist.Move(removedIdx-1, targetIdx-1);
                    BmpCoffer.Instance.SavePlaylist(_currentPlaylist);
                    PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(_currentPlaylist, true);
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
            _playlistRepeat = !_playlistRepeat;
            this.PlaylistRepeat_Button.Opacity = _playlistRepeat ? 1 : 0.5f;
        }

        /// <summary>
        /// The playlist shuffle toggle button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlaylistShuffle_Button_Click(object sender, RoutedEventArgs e)
        {
            _playlistShuffle = !_playlistShuffle;
            this.PlaylistShuffle_Button.Opacity = _playlistShuffle ? 1 : 0.5f;
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
            openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\"+ Globals.Globals.DataPath;

            if (openFileDialog.ShowDialog() != true)
                return;

            BmpCoffer.Instance.LoadNew(openFileDialog.FileName);
            Pigeonhole.BmpPigeonhole.Instance.LastLoadedCatalog = openFileDialog.FileName;
            PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
            _showingPlaylists = true;
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
            Pigeonhole.BmpPigeonhole.Instance.LastLoadedCatalog = openFileDialog.FileName;
            PlaylistContainer.ItemsSource = BmpCoffer.Instance.GetPlaylistNames();
            _showingPlaylists = true;
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
        #endregion
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
    }
}
