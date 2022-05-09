using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BardMusicPlayer.Ui.Globals
{
    public static class Globals
    {
        public static bool IsBeta;
        public static int Build;
        public static string Commit;
        public static string ExePath;
        public static string ResourcePath;
        public static string DataPath;
        public enum Autostart_Types
        {
            NONE = 0,
            VIA_CHAT,
            VIA_METRONOME,
            UNUSED
        }
    }
}
