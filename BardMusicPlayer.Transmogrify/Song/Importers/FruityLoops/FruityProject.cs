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
        public Track[] Tracks { get; set; } = new Track[MaxTrackCount];
        public List<Pattern> Patterns { get; set; } = new List<Pattern>();
        public Insert[] Inserts { get; set; } = new Insert[MaxInsertCount];
        public bool PlayTruncatedNotes { get; set; } = false;

        public FruityProject()
        {
            for (var i = 0; i < MaxTrackCount; i++)
            {
                Tracks[i] = new Track();
            }

            for (var i = 0; i < MaxInsertCount; i++)
            {
                Inserts[i] = new Insert { Id = i, Name = $"Insert {i}" };
            }

            Inserts[0].Name = "Master";
        }
    }
}
