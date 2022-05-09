using BardMusicPlayer.Coffer;
using BardMusicPlayer.Siren;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Ui.Functions;
using BardMusicPlayer.Ui.Globals.SkinContainer;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BardMusicPlayer.Ui.Skinned
{
    /// <summary>
    /// Interaktionslogik für Skinned_PlaylistView.xaml
    /// </summary>
    public partial class Skinned_PlaylistView : Window
    {
        public EventHandler<BmpSong> OnLoadSongFromPlaylist;

        private IPlaylist _currentPlaylist = null; //The currently used playlist
        public bool NormalPlay { get; set; } = true; //True if normal or false if shuffle
        public bool LoopPlay { get; set; } = false; //if true play the whole playlist and repeat

        public Skinned_PlaylistView()
        {
            InitializeComponent();
            ApplySkin();
            SkinContainer.OnNewSkinLoaded += SkinContainer_OnNewSkinLoaded;
            BmpSiren.Instance.SynthTimePositionChanged += Instance_SynthTimePositionChanged;

            _currentPlaylist = PlaylistFunctions.CreatePlaylist("Default");
            RefreshPlaylist();
        }

        #region Skinning
        private void SkinContainer_OnNewSkinLoaded(object sender, EventArgs e)
        { ApplySkin(); }

        public void ApplySkin()
        {
            var col = SkinContainer.PLAYLISTCOLOR[SkinContainer.PLAYLISTCOLOR_TYPES.PLAYLISTCOLOR_NORMALBG];
            this.Background = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));

            this.Playlist_Top_Left.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_TOP_LEFT_CORNER];
            this.PLAYLIST_TITLE_BAR.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_TITLE_BAR];
            this.PLAYLIST_TOP_TILE.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_TOP_TILE];
            this.PLAYLIST_TOP_TILE_II.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_TOP_TILE];
            this.PLAYLIST_TOP_RIGHT_CORNER.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_TOP_RIGHT_CORNER];

            this.PLAYLIST_LEFT_TILE.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_LEFT_TILE];
            this.PLAYLIST_RIGHT_TILE.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_RIGHT_TILE];

            this.PLAYLIST_BOTTOM_LEFT_CORNER.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_BOTTOM_LEFT_CORNER];
            this.PLAYLIST_BOTTOM_TILE.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_BOTTOM_TILE];
            this.PLAYLIST_BOTTOM_RIGHT_CORNER.Fill = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_BOTTOM_RIGHT_CORNER];

            this.Close_Button.Background = SkinContainer.PLAYLIST[SkinContainer.PLAYLIST_TYPES.PLAYLIST_CLOSE_SELECTED];
            this.Close_Button.Background.Opacity = 0;

            col = SkinContainer.PLAYLISTCOLOR[SkinContainer.PLAYLISTCOLOR_TYPES.PLAYLISTCOLOR_NORMALBG];
            this.PlaylistContainer.Background = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));
            col = SkinContainer.PLAYLISTCOLOR[SkinContainer.PLAYLISTCOLOR_TYPES.PLAYLISTCOLOR_NORMAL];
            this.PlaylistContainer.Foreground = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));

            PlaylistContainer_SelectionChanged(null, null);
        }
        #endregion

        private void RefreshPlaylist()
        {
            PlaylistContainer.Items.Clear();
            if (_currentPlaylist == null)
                return;
            foreach (BmpSong d in _currentPlaylist)
                PlaylistContainer.Items.Add(d.Title);
            Style itemContainerStyle = new Style(typeof(ListBoxItem));
            itemContainerStyle.Setters.Add(new Setter(ListBoxItem.AllowDropProperty, true));
            itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(PlaylistContainer_PreviewMouseLeftButtonDown)));
            itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.DropEvent, new DragEventHandler(PlaylistContainer_Drop)));
            PlaylistContainer.ItemContainerStyle = itemContainerStyle;
        }

        public void PlayPrevSong()
        {
            if (NormalPlay)
            {
                int idx = PlaylistContainer.SelectedIndex;
                if (idx- 1 <= -1)
                    return;
                PlaylistContainer.SelectedIndex = idx - 1;
            }
            else
            {
                Random rnd = new Random();
                PlaylistContainer.SelectedIndex = rnd.Next(0, PlaylistContainer.Items.Count);
            }

            string item = PlaylistContainer.SelectedItem as string;
            BmpSong song = PlaylistFunctions.GetSongFromPlaylist(_currentPlaylist, item);
            PlaybackFunctions.LoadSongFromPlaylist(song);
            PlaybackFunctions.PlaybackState = PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYNEXT;
            OnLoadSongFromPlaylist?.Invoke(this, song);
        }

        public void PlayNextSong()
        {
            if (NormalPlay)
            {
                int idx = PlaylistContainer.SelectedIndex;
                if ( idx + 1 >= PlaylistContainer.Items.Count)
                {
                    if (LoopPlay)
                        PlaylistContainer.SelectedIndex = 0;
                    else
                        return;
                }
                PlaylistContainer.SelectedIndex = idx + 1;
            }
            else
            {
                Random rnd = new Random();
                PlaylistContainer.SelectedIndex = rnd.Next(0, PlaylistContainer.Items.Count);
            }
            string item = PlaylistContainer.SelectedItem as string;
            BmpSong song = PlaylistFunctions.GetSongFromPlaylist(_currentPlaylist, item);
            PlaybackFunctions.LoadSongFromPlaylist(song);
            PlaybackFunctions.PlaybackState = PlaybackFunctions.PlaybackState_Enum.PLAYBACK_STATE_PLAYNEXT;
            OnLoadSongFromPlaylist?.Invoke(this, song);
        }

        private void PlaylistContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaybackFunctions.StopSong();
            foreach (BmpSong song in _currentPlaylist)
            {
                if (song.Title == PlaylistContainer.SelectedItem as string)
                {
                    PlaybackFunctions.LoadSongFromPlaylist(song);
                    OnLoadSongFromPlaylist?.Invoke(this, song);
                    return;
                }
            }
        }

        private void PlaylistContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentsongIndex = PlaylistContainer.SelectedIndex;
            var col = SkinContainer.PLAYLISTCOLOR[SkinContainer.PLAYLISTCOLOR_TYPES.PLAYLISTCOLOR_NORMAL];
            var fcol = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));
            col = SkinContainer.PLAYLISTCOLOR[SkinContainer.PLAYLISTCOLOR_TYPES.PLAYLISTCOLOR_NORMALBG];
            var bcol = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));
            for (int i = 0; i < PlaylistContainer.Items.Count; i++)
            {
                ListViewItem lvitem = PlaylistContainer.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                if (lvitem == null)
                    continue;
                lvitem.Foreground = fcol;
                lvitem.Background = bcol;
            }
            col = SkinContainer.PLAYLISTCOLOR[SkinContainer.PLAYLISTCOLOR_TYPES.PLAYLISTCOLOR_CURRENT];
            fcol = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));
            col = SkinContainer.PLAYLISTCOLOR[SkinContainer.PLAYLISTCOLOR_TYPES.PLAYLISTCOLOR_SELECTBG];
            bcol = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));

            var lvtem = PlaylistContainer.ItemContainerGenerator.ContainerFromItem(PlaylistContainer.SelectedItem) as ListViewItem;
            if (lvtem == null)
                return;
            lvtem.Foreground = fcol;
            lvtem.Background = bcol;
        }

        private void PlaylistContainer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem)
            {
                ListBoxItem draggedItem = sender as ListBoxItem;
                PlaylistContainer.SelectedItem = draggedItem;

                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        void PlaylistContainer_Drop(object sender, DragEventArgs e)
        {
            string droppedData = e.Data.GetData(typeof(string)) as string;
            string target = ((ListBoxItem)(sender)).DataContext as string;

            int removedIdx = PlaylistContainer.Items.IndexOf(droppedData);
            int targetIdx = PlaylistContainer.Items.IndexOf(target);

            if (removedIdx < targetIdx)
            {
                PlaylistContainer.Items.Insert(targetIdx + 1, droppedData);
                PlaylistContainer.Items.RemoveAt(removedIdx);

                _currentPlaylist.Move(removedIdx, targetIdx);
                BmpCoffer.Instance.SavePlaylist(_currentPlaylist);

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
                    PlaylistContainer.Items.Insert(targetIdx, droppedData);
                    PlaylistContainer.Items.RemoveAt(remIdx);
                    _currentPlaylist.Move(removedIdx, targetIdx);
                    BmpCoffer.Instance.SavePlaylist(_currentPlaylist);
                }
            }
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null)
                return;

            var openFileDialog = new OpenFileDialog
            {
                Filter = "MIDI file|*.mid;*.midi|All files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;
            foreach (var d in openFileDialog.FileNames)
            {
                BmpSong song = BmpSong.OpenMidiFile(d).Result;
                _currentPlaylist.Add(song);
                BmpCoffer.Instance.SaveSong(song);
            }
            BmpCoffer.Instance.SavePlaylist(_currentPlaylist);
            RefreshPlaylist();
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveSelected_Click(object sender, RoutedEventArgs e)
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
                break;
            }
            BmpCoffer.Instance.SavePlaylist(_currentPlaylist);
            RefreshPlaylist();
        }

        private void ClearPlaylist_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPlaylist == null)
                return;

            foreach (string s in PlaylistContainer.Items)
            {
                BmpSong song = PlaylistFunctions.GetSongFromPlaylist(_currentPlaylist, s);
                if (song == null)
                    continue;
                _currentPlaylist.Remove(song);
                BmpCoffer.Instance.DeleteSong(song);
            }
            BmpCoffer.Instance.SavePlaylist(_currentPlaylist);
            RefreshPlaylist();
        }

        private void MenuButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Rectangle rectangle = sender as Rectangle;
                ContextMenu contextMenu = rectangle.ContextMenu;
                contextMenu.PlacementTarget = rectangle;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
                contextMenu.IsOpen = true;
            }
        }

        /// <summary>
        /// opens the playlists browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MediaBrowser mb = new MediaBrowser();
            mb.Show();
            mb.OnPlaylistChanged += OnPlaylistChanged;
        }

        /// <summary>
        /// triggered from playlist browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnPlaylistChanged(object sender, string e)
        {
            _currentPlaylist = BmpCoffer.Instance.GetPlaylist(e);
            RefreshPlaylist();
        }

        #region Titlebar functions and buttons
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;
        }
        private void Close_Button_Down(object sender, MouseButtonEventArgs e)
        {
            this.Close_Button.Background.Opacity = 1;
        }
        private void Close_Button_Up(object sender, MouseButtonEventArgs e)
        {
            this.Close_Button.Background.Opacity = 0;
        }
        #endregion
    }
}
