/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.XIVMIDI.IO;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace BardMusicPlayer.Ui.Windows
{
    public partial class UploadData : Window
    {
        public UploadData(string filename)
        {
            filename = Path.GetFileNameWithoutExtension(filename);
            Console.WriteLine(filename);
            InitializeComponent();

            string pattern = @"^(?<interpret>.+?)\s*-\s*(?<titel>.+)$";
            regex_txt.Text = pattern;

            Match match = Regex.Match(filename, pattern);
            if (match.Success)
            {
                artist_txt.Text = match.Groups["interpret"].Value.Trim();
                title_txt.Text = match.Groups["titel"].Value.Trim();
                source_txt.Text = "Midi Archive";
            }
        }

        #region WindowEvents
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion

        BMPUploadBuilder bmpUpload = new BMPUploadBuilder();

        private void Okay_Click(object sender, RoutedEventArgs e)
        {
            bmpUpload.ApiKey = BmpPigeonhole.Instance.BMPApiKey;
            bmpUpload.title = title_txt.Text;
            bmpUpload.artist = artist_txt.Text;
            bmpUpload.source = source_txt.Text;
            bmpUpload.originalSourceUrl = source_url_txt.Text;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            bmpUpload = null;
            this.Close();
        }

#pragma warning disable CS0108
        public BMPUploadBuilder ShowDialog()
        {
            base.ShowDialog();
            return bmpUpload;
        }
#pragma warning restore CS0108
    }
}
