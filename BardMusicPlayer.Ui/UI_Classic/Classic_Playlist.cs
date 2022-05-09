using BardMusicPlayer.Coffer;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Functions;
using System;
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
        private IPlaylist _currentPlaylist;         //the current selected playlist


        private void playNextSong()
        {
            if (PlaylistContainer.Items.Count == 0)
                return;

            if ((PlaylistContainer.SelectedIndex == -1) || (PlaylistContainer.SelectedIndex == 0))
            {
                PlaylistContainer.SelectedIndex = 1;
            }
            else
            {
                if (PlaylistContainer.SelectedIndex == PlaylistContainer.Items.Count -1)
                {
                    PlaylistContainer.SelectedIndex = 1;
                }
                else
                    PlaylistContainer.SelectedIndex = PlaylistContainer.SelectedIndex + 1;
            }
            PlaybackFunctions.LoadSongFromPlaylist(PlaylistFunctions.GetSongFromPlaylist(_currentPlaylist, (string)PlaylistContainer.SelectedItem));
            this.SongName.Text = PlaybackFunctions.GetSongName();
            this.InstrumentInfo.Content = PlaybackFunctions.GetInstrumentNameForHostPlayer();
        }

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
                PlaylistContainer.ItemsSource = PlaylistFunctions.GetCurrentPlaylistItems(_currentPlaylist);
                _showingPlaylists = false;
            }
        }

        /// <summary>
        /// Save a playlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Playlist_Save_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null)
                return;
            BmpCoffer.Instance.SavePlaylist(_currentPlaylist);
        }


        private void Playlist_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null)
                return;

            if (PlaybackFunctions.CurrentSong == null)
                return;

            BmpCoffer.Instance.SaveSong(PlaybackFunctions.CurrentSong);
            _currentPlaylist.Add(PlaybackFunctions.CurrentSong);
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

            string name = PlaylistContainer.SelectedItem as string;
            foreach (BmpSong s in _currentPlaylist)
            {
                if (s.Title == name)
                {
                    _currentPlaylist.Remove(s);
                    break;
                }
            }
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

        private void PlaylistContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if ((string)PlaylistContainer.SelectedItem == null)
                return;

            if (_showingPlaylists)
            {
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
            return;
        }

        private void PlaylistRepeat_Button_Click(object sender, RoutedEventArgs e)
        {
            _playlistRepeat = !_playlistRepeat;
            this.PlaylistRepeat_Button.Opacity = _playlistRepeat ? 1 : 0.5f;
        }

        private void PlaylistShuffle_Button_Click(object sender, RoutedEventArgs e)
        {
            _playlistShuffle = !_playlistShuffle;
            this.PlaylistShuffle_Button.Opacity = _playlistShuffle ? 1 : 0.5f;
        }

        private void AutoPlay_Checked(object sender, RoutedEventArgs e)
        {
            BmpPigeonhole.Instance.PlaylistAutoPlay = AutoPlay_CheckBox.IsChecked ?? false;
        }
    }
}
