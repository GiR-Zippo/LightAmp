/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.XIVMIDI.IO;

namespace BardMusicPlayer.XIVMIDI.Events
{
    public sealed class XIVMidiBMPSongsEvent : XIVMidiEvent
    {
        internal XIVMidiBMPSongsEvent(BMPResponseContainer.Root data) : base(0, false)
        {
            EventType = GetType();
            Songs = data;
        }

        public BMPResponseContainer.Root Songs { get; }
        public override bool IsValid() => true;
    }
}
