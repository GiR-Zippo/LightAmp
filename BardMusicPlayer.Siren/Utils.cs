/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Siren.AlphaTab.Audio.Generator;
using BardMusicPlayer.Siren.AlphaTab.Model;
using BardMusicPlayer.Transmogrify.Song;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using MidiFile = BardMusicPlayer.Siren.AlphaTab.Audio.Synth.Midi.MidiFile;

namespace BardMusicPlayer.Siren
{
    internal static class Utils
    {
        /// <summary>
        /// Convert the Midi for siren
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        internal static async Task<(MidiFile, Dictionary<int, Dictionary<long, string>>)> GetSynthMidi(this BmpSong song)
        {
            var file = new MidiFile { Division = 600 };
            var events = new AlphaSynthMidiFileHandler(file);
            events.AddTempo(0, 100);

            var trackCounter = byte.MinValue;
            var veryLast = 0L;

            var midiFile = await song.GetProcessedMidiFile();
            var trackChunks = midiFile.GetTrackChunks().ToList();

            var lyrics = new Dictionary<int, Dictionary<long, string>>();
            var lyricNum = 0;

            //Skip first track, is eh nur "All Tracks"
            foreach (var trackChunk in trackChunks.GetRange(1, trackChunks.Count-1))
            {
                Instrument instr = Instrument.None;
                int trackOctaveShift = 0;
                using (var manager = trackChunk.ManageTimedEvents())
                {
                    var trackName = trackChunk.Events.OfType<SequenceTrackNameEvent>().First().Text;
                    Regex rex = new Regex(@"^([A-Za-z _:]+)([-+]\d)?");
                    if (rex.Match(trackName) is Match match)
                    {
                        if (!string.IsNullOrEmpty(match.Groups[1].Value))
                        {
                            instr = Instrument.Parse(match.Groups[1].Value);
                        }
                        if (int.TryParse(match.Groups[2].Value, out int os))
                            trackOctaveShift = os;
                    }
                    Dictionary<float, KeyValuePair<NoteEvent, Instrument>> instrumentMap = new Dictionary<float, KeyValuePair<NoteEvent, Instrument>>();

                    foreach (TimedEvent _event in manager.Objects)
                    {
                        var noteEvent = _event.Event as NoteEvent;
                        var lyricsEvent = _event.Event as LyricEvent;
                        var programChangeEvent = _event.Event as ProgramChangeEvent;

                        if (noteEvent != null && _event.Event.EventType == MidiEventType.NoteOn)
                            instrumentMap.Add(_event.Time, new KeyValuePair<NoteEvent, Instrument>(noteEvent, instr));

                        if (programChangeEvent != null)
                        {
                            if (instr.InstrumentTone.Equals(InstrumentTone.ElectricGuitar))
                                instr = Instrument.ParseByProgramChange(programChangeEvent.ProgramNumber);
                        }
                        if (lyricsEvent != null)
                        {
                            if (lyrics.Count() < lyricNum+1)
                                lyrics.Add(lyricNum, new Dictionary<long, string>());
                            lyrics[lyricNum].Add(_event.Time, lyricsEvent.Text);
                            lyricNum++;
                        }
                    }

                    foreach (var note in trackChunk.GetNotes())
                    {
                        var instrument = instr;
                        KeyValuePair<NoteEvent, Instrument> test;
                        if (instrumentMap.TryGetValue(note.Time, out test))
                        {
                            if (note.NoteNumber == test.Key.NoteNumber)
                            {
                                instrument = test.Value;
                            }
                        }

                        var noteNum = note.NoteNumber+(12 * trackOctaveShift);
                        if (noteNum < 0)   noteNum = 0;
                        if (noteNum > 127) noteNum = 127;
                        var dur = (int)MinimumLength(instrument, noteNum - 48, note.Length);
                        var time = (int)note.Time;
                        events.AddProgramChange(trackCounter, time, trackCounter,
                            (byte)instrument.MidiProgramChangeCode);
                        events.AddNote(trackCounter, time, dur, (byte)noteNum, DynamicValue.FFF, trackCounter);
                        if (trackCounter == byte.MaxValue)
                            trackCounter = byte.MinValue;
                        else
                            trackCounter++;

                        if (time + dur > veryLast) veryLast = time + dur;
                    }
                    instrumentMap.Clear();
                }
            }
            trackChunks.Clear();
            events.FinishTrack(byte.MaxValue, (byte)veryLast);
            return (file, lyrics);
        }

        private static long MinimumLength(Instrument instrument, int note, long duration)
        {
            switch (instrument.Index)
            {
                case 1: // Harp
                    return note switch
                    {
                        <= 9 => 1338,
                        <= 19 => 1338,
                        <= 28 => 1334,
                        _ => 1136
                    };

                case 2: // Piano
                    return note switch
                    {
                        <= 11 => 1531,
                        <= 18 => 1531,
                        <= 25 => 1530,
                        <= 28 => 1332,
                        _ => 1531
                    };

                case 3: // Lute
                    return note switch
                    {
                        <= 14 => 1728,
                        <= 21 => 1727,
                        <= 28 => 1727,
                        _ => 1528
                    };

                case 4: // Fiddle
                    return note switch
                    {
                        <= 3 => 634,
                        <= 6 => 632,
                        <= 11 => 633,
                        <= 15 => 634,
                        <= 18 => 633,
                        <= 23 => 635,
                        <= 30 => 635,
                        _ => 635
                    };

                case 5: // Flute
                case 6: // Oboe
                case 7: // Clarinet
                case 8: // Fife
                case 9: // Panpipes
                    return duration > 4500 ? 4500 : duration < 500 ? 500 : duration;

                case 10: // Timpani
                    return note switch
                    {
                        <= 15 => 1193,
                        <= 23 => 1355,
                        _ => 1309
                    };

                case 11: // Bongo
                    return note switch
                    {
                        <= 7 => 720,
                        <= 21 => 544,
                        _ => 275
                    };

                case 12: // BassDrum
                    return note switch
                    {
                        <= 6 => 448,
                        <= 11 => 335,
                        <= 23 => 343,
                        _ => 254
                    };

                case 13: // SnareDrum
                    return 260;

                case 14: // Cymbal
                    return 700;

                case 15: // Trumpet
                case 16: // Trombone
                case 17: // Tuba
                case 18: // Horn
                case 19: // Saxophone
                case 20: // Violin
                case 21: // Viola
                case 22: // Cello
                case 23: // DoubleBass
                    return duration > 4500 ? 4500 : duration < 300 ? 300 : duration;
                case 24: // ElectricGuitarOverdriven
                    return duration > 4500 ? 4500 : duration < 300 ? 300 : duration;
                case 25: // ElectricGuitarClean
                case 27: // ElectricGuitarPowerChords
                    return duration > 4500 ? 4500 : duration < 300 ? 300 : duration;
                case 26: // ElectricGuitarMuted
                    return note switch
                    {
                        <= 18 => 186,
                        <= 21 => 158,
                        _ => 174
                    };
                case 28: // ElectricGuitarSpecial
                    return 1500;

                default: return duration;
            }
        }
    }
}
