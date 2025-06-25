using System.Collections.Generic;

namespace BardMusicPlayer.XIVMIDI.IO
{
    public enum Requester
    {
        NONE = 0,
        JSON = 1,
        DOWNLOAD = 2
    }

    public static class Misc
    {
        public static readonly Dictionary<int, string> PerformerSize = new Dictionary<int, string>
        {
            [0] = "None",
            [1] = "Solo",
            [2] = "Duet",
            [3] = "Trio",
            [4] = "Quartet",
            [5] = "Quintet",
            [6] = "Sextet",
            [7] = "Septet",
            [8] = "Octet"
        };

    }
}
