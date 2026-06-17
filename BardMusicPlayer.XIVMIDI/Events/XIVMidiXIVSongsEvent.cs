/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.XIVMIDI.IO;

namespace BardMusicPlayer.XIVMIDI.Events
{
    public sealed class XIVMidiXIVSongsEvent : XIVMidiEvent
    {
        internal XIVMidiXIVSongsEvent(XIVMIDIResponseContainer.ApiResponse data) : base(0, false)
        {
            EventType = GetType();
            Songs = data;
        }

        public XIVMIDIResponseContainer.ApiResponse Songs { get; }
        public override bool IsValid() => true;
    }
}
