using BardMusicPlayer.Ui.Classic;
using Microsoft.Win32;
using System.Windows;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für MacroEditWindow.xaml
    /// </summary>
    public partial class MacroEditWindow : Window
    {
        Macro _macro { get; set; } = null;


        public MacroEditWindow(Macro macro)
        {
            InitializeComponent();
            _macro = macro;
            MacroName.Text = _macro.DisplayedText;
            MacroFileName.Content = _macro.File;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Basic file | *.bas",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            if (!openFileDialog.FileName.ToLower().EndsWith(".bas"))
                return;

            _macro.File = openFileDialog.FileName;
            MacroFileName.Content = openFileDialog.FileName;
        }

        private void MacroName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _macro.DisplayedText = MacroName.Text;
        }

        

    }
}
