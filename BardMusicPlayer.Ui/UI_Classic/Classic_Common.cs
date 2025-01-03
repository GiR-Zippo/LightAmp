/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Siren;
using BardMusicPlayer.Transmogrify.Song;
using System.Windows.Controls;

namespace BardMusicPlayer.Ui.Classic
{
    public sealed partial class Classic_MainView : UserControl
    {
        private void loadSongToPreview(BmpSong song)
        {
            if (BmpSiren.Instance.IsReadyForPlayback)
                BmpSiren.Instance.Stop();

            _ = BmpSiren.Instance.Load(song);

            //Fill the lyrics editor
            lyricsData.Clear();
            foreach (var line in song.LyricsContainer)
                lyricsData.Add(new LyricsContainer(line.Key, line.Value));
            Siren_Lyrics.DataContext = lyricsData;
            Siren_Lyrics.Items.Refresh();
        }
    }
}
