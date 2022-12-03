/*
 * Copyright(c) 2022 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Sanford.Multimedia.Midi;

namespace BardMusicPlayer.Maestro.Utils
{
    public sealed class NoteEvent
    {
        public Track track;
        public int trackNum;
        public int note;
        public int origNote;
    };
    public sealed class ProgChangeEvent
    {
        public Track track;
        public int trackNum;
        public int voice;
    };

    public sealed class ChannelAfterTouchEvent
    {
        public Track track;
        public int trackNum;
        public int command;
    };

    public static class NoteHelper
    {
        public static int ApplyOctaveShift(int note, int octave)
        {
            return (note - (12 * 4)) + (12 * octave);
        }
    }
}
