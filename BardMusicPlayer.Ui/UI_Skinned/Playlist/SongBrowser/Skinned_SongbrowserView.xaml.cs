using BardMusicPlayer.Coffer;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Siren;
using BardMusicPlayer.Ui.Functions;
using BardMusicPlayer.Ui.Globals.SkinContainer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public partial class Skinned_SongbrowserView : Window
    {
        public EventHandler<string> OnLoadSongFromBrowser;
        public EventHandler<string> OnAddSongToPlaylistFromBrowser;
        public EventHandler<int> OnToggleView;

        private string SongPath { get; set; } = "";

        public Skinned_SongbrowserView()
        {
            InitializeComponent();
            ApplySkin();
            SkinContainer.OnNewSkinLoaded              += SkinContainer_OnNewSkinLoaded;
            BmpSiren.Instance.SynthTimePositionChanged += Instance_SynthTimePositionChanged;    //Handled in Skinned_PlaylistView_Siren.cs

            SongPath = BmpPigeonhole.Instance.SongDirectory;
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
            this.Search_Box.Background = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));

            col = SkinContainer.PLAYLISTCOLOR[SkinContainer.PLAYLISTCOLOR_TYPES.PLAYLISTCOLOR_NORMAL];
            this.PlaylistContainer.Foreground = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));
            this.Search_Box.Foreground = new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));

            PlaylistContainer_SelectionChanged(null, null);
        }
        #endregion

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                OnToggleView?.Invoke(this, 0);
            }
        }

        /// <summary>
        /// Refreshes the PlaylistContainer, clears the items and rereads them
        /// </summary>
        private void RefreshPlaylist()
        {
            if (!Directory.Exists(SongPath))
                return;

            string[] files = Directory.EnumerateFiles(SongPath, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mid") || s.EndsWith(".mml") || s.EndsWith(".mmsong")).ToArray();
            
            Dictionary<string, string> list = new Dictionary<string, string>();
            string last_dir = "";

            foreach (string file in files)
            {
                if (Search_Box.Text != "")
                {
                    if (file.ToLower().Contains(Search_Box.Text.ToLower()))
                    {
                        if (System.IO.Path.GetDirectoryName(file) != last_dir)
                        {
                            last_dir = System.IO.Path.GetDirectoryName(file);
                            list.Add("+" + last_dir + "+", " ");
                            list.Add("-" + last_dir, "-" + last_dir);
                            list.Add("+" + last_dir + "-", "------------------------------------------------------------------");
                        }
                        list.Add(file, System.IO.Path.GetFileNameWithoutExtension(file));
                    }
                }
                else
                {
                    if (System.IO.Path.GetDirectoryName(file) != last_dir)
                    {
                        last_dir = System.IO.Path.GetDirectoryName(file);
                        list.Add("+" + last_dir + "+", " ");
                        list.Add("-" + last_dir, "-" + last_dir);
                        list.Add("+" + last_dir + "-", "------------------------------------------------------------------");
                    }
                    list.Add(file, System.IO.Path.GetFileNameWithoutExtension(file));
                }
            }
            PlaylistContainer.ItemsSource = list;
        }

        #region PlaylistContainer actions
        /// <summary>
        /// MouseDoubleClick action: load the clicked song into the sequencer
        /// </summary>
        private void PlaylistContainer_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaybackFunctions.StopSong();
            var filename = PlaylistContainer.SelectedItems.OfType<KeyValuePair<string, string> >();
            if (!File.Exists(filename.First().Key) || filename.First().Key == null)
                return;
            OnLoadSongFromBrowser?.Invoke(this, filename.First().Key);
        }

        
        private void MenuButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Rectangle listView = sender as Rectangle;
                ContextMenu contextMenu = listView.ContextMenu;
                contextMenu.PlacementTarget = listView;
                contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
                contextMenu.IsOpen = true;
            }
        }

        private void AddItemToPlaylist_Click(object sender, RoutedEventArgs e)
        {
            var filename = PlaylistContainer.SelectedItems.OfType<KeyValuePair<string, string>>();
            if (!File.Exists(filename.First().Key) || filename.First().Key == null)
                return;
            OnAddSongToPlaylistFromBrowser?.Invoke(this, filename.First().Key);
        }

        private void ShowPlaylists_MenuClick(object sender, RoutedEventArgs e)
        {
            OnToggleView?.Invoke(this, 0);
        }

        /// <summary>
        /// the selection changed action. Set the selected song and change the highlight color
        /// </summary>
        private void PlaylistContainer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SirenCurrentSongIndex = PlaylistContainer.SelectedIndex; //tell siren our current song index
            
            //Fancy coloring
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

        #endregion

        /// <summary>
        /// Sets the search parameter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_Box_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
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
