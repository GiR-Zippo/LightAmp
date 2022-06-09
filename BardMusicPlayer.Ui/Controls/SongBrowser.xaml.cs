using BardMusicPlayer.Maestro;
using BardMusicPlayer.Maestro.Events;
using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Seer.Events;
using BardMusicPlayer.Ui.Functions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;


namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für SongBrowser.xaml
    /// </summary>
    public partial class SongBrowser : UserControl
    {
        public SongBrowser()
        {
            InitializeComponent();
            string[] files = Directory.GetFiles( SongPath.Text, "*", SearchOption.AllDirectories);
            List<string> list = new List<string>(files);
            SongbrowserContainer.ItemsSource = list;
        }

        private void SongbrowserContainer_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string filename = SongbrowserContainer.SelectedItem as String;
            Console.WriteLine(filename);
            PlaybackFunctions.LoadSong(filename);
        }

        private void SongSearch_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            string[] files = Directory.GetFiles(SongPath.Text, "*", SearchOption.AllDirectories);
            List<string> list = new List<string>(files);
            if (SongSearch.Text != "")
                list = list.FindAll(delegate (string s) { return s.Contains(SongSearch.Text); });
            SongbrowserContainer.ItemsSource = list;
        }
    }
}
