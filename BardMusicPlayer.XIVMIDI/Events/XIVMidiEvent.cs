/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;

namespace BardMusicPlayer.XIVMIDI.Events
{
    public abstract class XIVMidiEvent
    {
        internal XIVMidiEvent(byte dedupeThreshold = 0, bool highPriority = false)
        {
            DedupeThreshold = dedupeThreshold;
            HighPriority = highPriority;
        }

        public long TimeStamp { get; }

        internal byte DedupeThreshold { get; }

        internal bool HighPriority { get; }

        public Type EventType { get; protected set; }

        public abstract bool IsValid();
    }
}
