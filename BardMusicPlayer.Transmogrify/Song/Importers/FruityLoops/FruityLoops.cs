/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian.Structs;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BardMusicPlayer.Transmogrify.Song.Importers.FruityLoops.FruityStrucs;

namespace BardMusicPlayer.Transmogrify.Song.Importers.FruityLoops
{
    public static class FlpToMidi
    {
        /// <summary>
        /// Konvertiert eine .flp-Datei in eine MidiFile.
        /// </summary>
        public static MidiFile Convert(string flpPath, Dictionary<string, byte> channelProgramMap = null)
        {
            using var fs = File.OpenRead(flpPath);
            using var br = new BinaryReader(fs, Encoding.GetEncoding(1252));
            return Convert(br, channelProgramMap);
        }

        public static MidiFile Convert(Stream stream, Dictionary<string, byte> channelProgramMap = null)
        {
            using var br = new BinaryReader(stream, Encoding.GetEncoding(1252), leaveOpen: true);
            return Convert(br, channelProgramMap);
        }

        public static MidiFile Convert(BinaryReader br, Dictionary<string, byte> channelProgramMap = null)
        {
            var project = new FruityProject();
            var globVars = new EnVars
            {
                CurPattern = new Pattern { Notes = new Dictionary<Channel, List<Note>>() },
                TrackIndex = 0
            };

            FruityParse.ParseHeader(br, ref project);
            FruityParse.ParseFldt(br);
            while (br.BaseStream.Position < br.BaseStream.Length)
                FruityParse.ParseEvent(br, ref globVars, ref project);

            return BuildMidiFile(project, channelProgramMap ?? new Dictionary<string, byte>());
        }

        // -------------------------------------------------------------------------
        // MIDI-Builder
        // -------------------------------------------------------------------------
        private static MidiFile BuildMidiFile(FruityProject project, Dictionary<string, byte> programOverrides)
        {
            var midiFile = new MidiFile();
            midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)project.Ppq);

            // --- Tempo-Track ---
            var tempoTrack = new TrackChunk();
            double bpm = project.Tempo > 0 ? project.Tempo : 125.0;
            int tempoMicro = (int)(60_000_000.0 / bpm);
            tempoTrack.Events.Add(new SetTempoEvent(tempoMicro) { DeltaTime = 0 });
            midiFile.Chunks.Add(tempoTrack);

            // --- Absolute Noten aus Playlist aufbauen ---
            // key: Channel, value: Liste (absoluteTick, note)
            var absoluteNotes = new Dictionary<Channel, List<(long tick, Note note)>>();

            foreach (var track in project.Tracks)
            {
                if (track?.Items == null) continue;
                foreach (var item in track.Items)
                {
                    if (item is PatternPlaylistItem ppi && !ppi.Muted)
                    {
                        long patternStart = ppi.Position;
                        foreach (var kvp in ppi.Pattern.Notes)
                        {
                            var ch = kvp.Key;
                            if (!absoluteNotes.ContainsKey(ch))
                                absoluteNotes[ch] = new List<(long, Note)>();
                            foreach (var n in kvp.Value)
                                absoluteNotes[ch].Add((patternStart + n.Position, n));
                        }
                    }
                }
            }

            // Fallback: Falls keine Playlist-Items (z.B. reine Pattern-Projekte ohne Arrangement),
            // alle Patterns direkt sequenziell hintereinander legen.
            if (!absoluteNotes.Any())
            {
                long cursor = 0;
                foreach (var pattern in project.Patterns)
                {
                    long patLen = 0;
                    foreach (var kvp in pattern.Notes)
                    {
                        var ch = kvp.Key;
                        if (!absoluteNotes.ContainsKey(ch))
                            absoluteNotes[ch] = new List<(long, Note)>();
                        foreach (var n in kvp.Value)
                        {
                            absoluteNotes[ch].Add((cursor + n.Position, n));
                            long end = n.Position + n.Length;
                            if (end > patLen) patLen = end;
                        }
                    }
                    cursor += patLen;
                }
            }

            if (!absoluteNotes.Any())
                return midiFile; // leeres Projekt

            // --- Pro Channel einen MIDI-Track ---
            // MIDI-Kanal-Zuordnung: Channel.Id % 16 (Channel 9 = Drums überspringen)
            int midiChanCounter = 0;
            foreach (var kvp in absoluteNotes.OrderBy(x => x.Key.Id))
            {
                var ch = kvp.Key;
                var noteList = kvp.Value;
                if (!noteList.Any()) continue;

                // MIDI-Kanal: 9 ist Drums, den überspringen
                if (midiChanCounter == 9) midiChanCounter++;
                byte midiChan = (byte)(midiChanCounter % 16);
                midiChanCounter++;

                string trackName = string.IsNullOrWhiteSpace(ch.ChannelName)
                    ? $"Channel {ch.Id + 1}"
                    : ch.ChannelName;

                byte program;
                var instr = Instrument.Parse(trackName);
                program = instr.Equals(Instrument.None)
                    ? ResolveProgram(trackName, programOverrides)
                    : (byte)instr.MidiProgramChangeCode;

                var evList = new List<(long tick, MidiEvent ev)>
                {
                    (0, new ProgramChangeEvent((SevenBitNumber)program) { Channel = (FourBitNumber)midiChan })
                };

                foreach (var (startTick, note) in noteList)
                {
                    long endTick = startTick + Math.Max(1, note.Length);
                    byte vel = note.Velocity == 0 ? (byte)100 : note.Velocity;
                    evList.Add((startTick, new NoteOnEvent((SevenBitNumber)note.Key, (SevenBitNumber)vel)
                    { Channel = (FourBitNumber)midiChan }));
                    evList.Add((endTick, new NoteOffEvent((SevenBitNumber)note.Key, (SevenBitNumber)0)
                    { Channel = (FourBitNumber)midiChan }));
                }

                // ProgramChange-Events für diesen Channel
                if (ch.Data is OldAutomationData oad)
                {
                    foreach (var pc in oad.Programchanges)
                    {
                        if (pc.InstrumentNumber == 0) continue;
                        evList.Add(((long)pc.Timestamp, new ProgramChangeEvent((SevenBitNumber)(pc.InstrumentNumber - 1))
                        { Channel = (FourBitNumber)midiChan }));
                    }
                }

                evList.Sort((a, b) =>
                {
                    if (a.tick != b.tick) return a.tick.CompareTo(b.tick);
                    int Pri(MidiEvent e) => e is NoteOnEvent ? 1 : 0;
                    return Pri(a.ev).CompareTo(Pri(b.ev));
                });

                var track = new TrackChunk();
                track.Events.Add(new SequenceTrackNameEvent(trackName) { DeltaTime = 0 });

                long lastTick = 0;
                foreach (var (tick, ev) in evList)
                {
                    ev.DeltaTime = Math.Max(0, tick - lastTick);
                    lastTick = Math.Max(lastTick, tick);
                    track.Events.Add(ev);
                }

                midiFile.Chunks.Add(track);
            }

            return midiFile;
        }

        private static byte ResolveProgram(string channelName, Dictionary<string, byte> overrides)
        {
            foreach (var kv in overrides)
                if (string.Equals(kv.Key, channelName, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;

            var lower = channelName.ToLowerInvariant();
            foreach (var kv in overrides)
                if (lower.Contains(kv.Key.ToLowerInvariant()))
                    return kv.Value;

            return GuessProgram(lower);
        }

        private static byte GuessProgram(string nameLower)
        {
            if (nameLower.Contains("kick") || nameLower.Contains("bassdrum")) return 117;
            if (nameLower.Contains("snare")) return 115;
            if (nameLower.Contains("cymbal")) return 127;
            if (nameLower.Contains("hihat") || nameLower.Contains("hi-hat")) return 127;
            if (nameLower.Contains("drum")) return 118;
            if (nameLower.Contains("perc")) return 116;

            if (nameLower.Contains("doublebass") || nameLower.Contains("contrabass")) return 43;
            if (nameLower.Contains("bass")) return 33;

            if (nameLower.Contains("overdrive")) return 30;
            if (nameLower.Contains("guitar")) return 29;

            if (nameLower.Contains("sax")) return 65;
            if (nameLower.Contains("trumpet")) return 56;
            if (nameLower.Contains("trombone")) return 57;
            if (nameLower.Contains("flute")) return 73;
            if (nameLower.Contains("oboe")) return 68;
            if (nameLower.Contains("clarinet")) return 71;

            if (nameLower.Contains("violin") || nameLower.Contains("fiddle")) return 40;
            if (nameLower.Contains("cello")) return 42;
            if (nameLower.Contains("string")) return 48;

            if (nameLower.Contains("piano")) return 0;
            if (nameLower.Contains("organ")) return 16;
            if (nameLower.Contains("harp")) return 46;
            if (nameLower.Contains("marimba")) return 12;

            if (nameLower.Contains("lead")) return 80;
            if (nameLower.Contains("pad")) return 88;
            if (nameLower.Contains("synth")) return 81;

            return 0;
        }
    }
}