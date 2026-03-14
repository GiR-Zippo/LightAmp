/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Collections.Generic;

namespace BardMusicPlayer.Transmogrify.Song.Importers
{
    public sealed class MMSongContainer
    {
        public List<MMSong> songs = new List<MMSong>();
        public MMSongContainer()
        {
            var s = new MMSong();
            songs.Add(s);
        }
    }

    public sealed class MMSong
    {
        public List<MMBards> bards = new List<MMBards>();
        public List<MMLyrics> lyrics = new List<MMLyrics>();
        public string title{ get; set; } ="";
        public string description { get; set; } = "";
    }

    public class MMBards
    {
        public int instrument { get; set; } = 0;
        public Dictionary<int, int> sequence = new Dictionary<int, int>();
    }

    public class MMLyrics
    {
        public string description { get; set; } = "";
        public Dictionary<int, string> lines = new Dictionary<int, string>();
        public Dictionary<int, int> sequence = new Dictionary<int, int>();
    }

}
