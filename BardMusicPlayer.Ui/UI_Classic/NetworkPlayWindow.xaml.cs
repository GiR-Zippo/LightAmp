/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.ComponentModel;
using System.Windows;

namespace BardMusicPlayer.Ui.Classic
{
    /// <summary>
    /// Interaktionslogik für NetworkPlayWindow.xaml
    /// </summary>
    public partial class NetworkPlayWindow : Window
    {
        public NetworkPlayWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

    }
}
