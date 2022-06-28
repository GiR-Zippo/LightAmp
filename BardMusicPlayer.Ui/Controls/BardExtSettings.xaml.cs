using BardMusicPlayer.Maestro;
using BardMusicPlayer.Maestro.Performance;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für BardExtSettings.xaml
    /// </summary>
    public partial class BardExtSettings : Window
    {
        private Performer _performer = null;
        public BardExtSettings(Performer performer)
        {
            _performer = performer;
            InitializeComponent();
            Title = "Settings for: " + _performer.PlayerName;

            if (BmpMaestro.Instance.GetSongTitleParsingBard() == null)
                PostSongTitle.IsChecked = false;
            else
            {
                if (BmpMaestro.Instance.GetSongTitleParsingBard().PId == _performer.PId)
                    PostSongTitle.IsChecked = true;
                else
                    PostSongTitle.IsChecked = false;
            }

            this.Singer.IsChecked = performer.IsSinger;

        }

        private void ChatInputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ComboBoxItem t = Chat_Type.SelectedValue as ComboBoxItem;
                string chattype = "";
                if (t != null)
                    chattype = (string)t.Content;

                _performer.SendText("/"+chattype+" "+ ChatInputText.Text);
            }
        }

        private void PostSongTitle_Checked(object sender, RoutedEventArgs e)
        {
            ComboBoxItem t = Chat_Type.SelectedValue as ComboBoxItem;
            string chattype = "";
            if (t != null)
                chattype = (string)t.Content;

            if ((bool)PostSongTitle.IsChecked)
                BmpMaestro.Instance.SetSongTitleParsingBard("/"+chattype, _performer);
            else
                BmpMaestro.Instance.SetSongTitleParsingBard("", null);
        }

        private void Singer_Checked(object sender, RoutedEventArgs e)
        {
            _performer.IsSinger = (bool)Singer.IsChecked;
        }
    }
}
