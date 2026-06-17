/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.XIVMIDI.IO;

namespace BardMusicPlayer.XIVMIDI.Events
{
    public sealed class XIVMidiFileEvent : XIVMidiEvent
    {
        internal XIVMidiFileEvent(XIVMIDIResponseContainer.MidiFile midi, object args) : base(0, false)
        {
            EventType = GetType();
            MidiData = midi;
            Arguments = args;
        }

        public XIVMIDIResponseContainer.MidiFile MidiData { get; }
        public object Arguments { get; }
        public override bool IsValid() => true;
    }
}
