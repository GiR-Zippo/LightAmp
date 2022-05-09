using Sanford.Multimedia.Midi;
using System.Collections.Generic;

namespace BardMusicPlayer.Maestro.Utils
{
    public class NoteEvent
    {
        public Track track;
        public int trackNum;
        public int note;
        public int origNote;
    };
    public class ProgChangeEvent
    {
        public Track track;
        public int trackNum;
        public int voice;
    };

    public static class NoteHelper
    {
        public static int ApplyOctaveShift(int note, int octave)
        {
            return (note - (12 * 4)) + (12 * octave);
        }
    }
}
