/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Transmogrify.Song.Importers.FruityLoops.FruityStrucs;
using System.Collections.Generic;

namespace BardMusicPlayer.Transmogrify.Song.Importers.FruityLoops
{
    public class FruityProject
    {
        public const int MaxInsertCount = 127;
        public const int MaxTrackCount = 199;

        public int MainVolume { get; set; } = 300;
        public int MainPitch { get; set; } = 0;
        public int Ppq { get; set; } = 0;
        public double Tempo { get; set; } = 140;
        public string ProjectTitle { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string VersionString { get; set; } = string.Empty;
        public int Version { get; set; } = 0x100;
        public List<Channel> Channels { get; set; } = new List<Channel>();
        public bool PlayTruncatedNotes { get; set; } = false;

        private Track[] _tracks;
        public Track[] Tracks => _tracks ??= new Track[MaxTrackCount];

        private Insert[] _inserts;
        public Insert[] Inserts => _inserts ??= BuildInserts();

        public List<Pattern> Patterns { get; set; } = new List<Pattern>();

        private static Insert[] BuildInserts()
        {
            var arr = new Insert[MaxInsertCount];
            return arr;
        }

        public Insert GetInsert(int i)
        {
            if (_inserts == null) _inserts = new Insert[MaxInsertCount];
            if (_inserts[i] == null)
                _inserts[i] = new Insert { Id = i, Name = i == 0 ? "Master" : $"Insert {i}" };
            return _inserts[i];
        }

        public Track GetTrack(int i)
        {
            if (_tracks == null) _tracks = new Track[MaxTrackCount];
            if (_tracks[i] == null)
                _tracks[i] = new Track();
            return _tracks[i];
        }
    }
}