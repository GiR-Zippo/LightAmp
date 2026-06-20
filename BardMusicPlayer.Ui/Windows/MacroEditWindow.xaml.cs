/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Pigeonhole;
using Microsoft.Win32;
using System;
using System.Windows;

namespace BardMusicPlayer.Ui.Windows
{
    /// <summary>
    /// Interaktionslogik für MacroEditWindow.xaml
    /// </summary>
    public sealed partial class MacroEditWindow : Window
    {
        Macro _macro { get; set; } = null;


        public MacroEditWindow(Macro macro)
        {
            InitializeComponent();

            if (BmpPigeonhole.Instance.LastSkin.EndsWith(".xaml"))
                Globals.Globals.SetTheme(this, BmpPigeonhole.Instance.LastSkin);

            _macro = macro;
            MacroName.Text = _macro.DisplayedText;
            MacroFileName.Content = _macro.File;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Script file | *.bas;*.lua",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            if (!openFileDialog.FileName.ToLower().EndsWith(".bas", StringComparison.Ordinal) &&
                !openFileDialog.FileName.ToLower().EndsWith(".lua", StringComparison.Ordinal))
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
