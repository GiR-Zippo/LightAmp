/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using System.Collections.Generic;
using System.Linq;

namespace BardMusicPlayer.Transmogrify.Song.Importers.MML
{
    public static class MMLMidiWriter
    {
        public static MidiFile BuildMidiFile(List<MidiEvent> events, int ppq)
        {
            var midiFile = new MidiFile
            {
                TimeDivision = new TicksPerQuarterNoteTimeDivision((short)ppq)
            };

            // Track 0: Tempo
            var tempoTrack = new TrackChunk();
            long tempoUsed = 0;
            foreach (var e in events.Where(e => e.Type == MidiEventType.Tempo).OrderBy(e => e.Tick))
            {
                tempoTrack.Events.Add(new SetTempoEvent(e.Tempo) { DeltaTime = e.Tick - tempoUsed });
                tempoUsed = e.Tick;
            }
            midiFile.Chunks.Add(tempoTrack);

            // One channel per track
            foreach (var group in events
                .Where(e => e.Type != MidiEventType.Tempo)
                .GroupBy(e => e.Channel)
                .OrderBy(g => g.Key))
            {
                var track = new TrackChunk();
                long prevTick = 0;
                foreach (var e in group.OrderBy(e => e.Tick))
                {
                    var midiEv = ToMelanchallEvent(e);
                    if (midiEv == null) continue;
                    midiEv.DeltaTime = e.Tick - prevTick;
                    prevTick = e.Tick;
                    track.Events.Add(midiEv);
                }
                midiFile.Chunks.Add(track);
            }

            return midiFile;
        }

        private static Melanchall.DryWetMidi.Core.MidiEvent ToMelanchallEvent(MidiEvent e)
        {
            var ch = (FourBitNumber)(e.Channel & 0x0F);
            switch (e.Type)
            {
                case MidiEventType.NoteOn:
                    return new NoteOnEvent((SevenBitNumber)(e.Param1 & 0x7F), (SevenBitNumber)(e.Param2 & 0x7F)) { Channel = ch };
                case MidiEventType.NoteOff:
                    return new NoteOffEvent((SevenBitNumber)(e.Param1 & 0x7F), SevenBitNumber.MinValue) { Channel = ch };
                case MidiEventType.ProgramChange:
                    return new ProgramChangeEvent((SevenBitNumber)(e.Param1 & 0x7F)) { Channel = ch };
                case MidiEventType.Controller:
                    return new ControlChangeEvent((SevenBitNumber)(e.Param1 & 0x7F), (SevenBitNumber)(e.Param2 & 0x7F)) { Channel = ch };
                default:
                    return null;
            }
        }
    }
}