using System.Windows;

namespace BardMusicPlayer.MidiUtil.Ui
{
    /// <summary>
    /// Interaktionslogik für TextInputWindow.xaml
    /// </summary>
    public sealed partial class TextInputWindow : Window
    {
        public TextInputWindow(string infotext, int maxinputlength = 42)
        {
            InitializeComponent();
            this.InfoText.Text = infotext;
            this.ResponseTextBox.Focus();
            this.ResponseTextBox.MaxLength = maxinputlength;
        }

        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }

        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = false;
        }

    }
}
