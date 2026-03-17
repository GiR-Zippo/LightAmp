
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace BardMusicPlayer.Transmogrify.Song.Importers.MML
{
    public static class MidiToMMLConverter
    {
        /// <summary>Einfacher MML-String, ein Track pro Zeile.</summary>
        public static string ToRawMML(string midiPath) =>
            ToRawMML(MidiFile.Read(midiPath));

        public static string ToRawMML(MidiFile midi)
        {
            var tracks = ConvertTracks(midi, MMLDialect.Generic, maxTracks: 16);
            return string.Join("\r\n", tracks);
        }

        // ── Kern-Konvertierung ────────────────────────────────────────────

        private static List<string> ConvertTracks(
            MidiFile midi, MMLDialect dialect, int maxTracks)
        {
            var tempoMap = midi.GetTempoMap();
            int ppq = ((TicksPerQuarterNoteTimeDivision)midi.TimeDivision).TicksPerQuarterNote;

            // get all notes grouped by channel
            var notesByChannel = new Dictionary<int, List<NoteEntry>>();

            foreach (var trackChunk in midi.GetTrackChunks())
            {
                var notes = trackChunk.GetNotes();
                foreach (var note in notes)
                {
                    int ch = note.Channel;
                    if (!notesByChannel.ContainsKey(ch))
                        notesByChannel[ch] = new List<NoteEntry>();
                    notesByChannel[ch].Add(new NoteEntry
                    {
                        StartTick = note.Time,
                        EndTick = note.Time + note.Length,
                        NoteNum = note.NoteNumber,
                        Velocity = note.Velocity
                    });
                }
            }

            // collect Tempo-Events
            var tempoChanges = GetTempoChanges(midi);
            int defaultTempo = tempoChanges.Count > 0 ? tempoChanges[0].Bpm : 120;

            var result = new List<string>();
            int trackCount = 0;

            foreach (var kvp in notesByChannel.OrderBy(k => k.Key))
            {
                if (trackCount >= maxTracks) break;
                string MML = BuildTrackMML(kvp.Value, tempoChanges, ppq, defaultTempo, dialect);
                if (!string.IsNullOrWhiteSpace(MML))
                {
                    result.Add(MML);
                    trackCount++;
                }
            }

            return result;
        }

        private static string BuildTrackMML(
            List<NoteEntry> notes,
            List<TempoChange> tempoChanges,
            int ppq,
            int defaultTempo,
            MMLDialect dialect)
        {
            if (notes.Count == 0) return "";

            notes.Sort((a, b) => a.StartTick.CompareTo(b.StartTick));

            var sb = new StringBuilder();
            long cursor = 0;
            int curOctave = 4;
            int curVelocity = -1;
            int tempoIdx = 0;
            bool firstToken = true;

            // Tempo
            if (tempoChanges.Count > 0)
                AppendToken(sb, "t" + tempoChanges[0].Bpm, ref firstToken);

            foreach (var note in notes)
            {
                // Tempo-Changes before Note
                while (tempoIdx < tempoChanges.Count &&
                       tempoChanges[tempoIdx].Tick <= note.StartTick)
                {
                    if (tempoIdx > 0) // Index 0 at the beginning
                        AppendToken(sb, "t" + tempoChanges[tempoIdx].Bpm, ref firstToken);
                    tempoIdx++;
                }

                // Pause before Note
                long gap = note.StartTick - cursor;
                if (gap > 0)
                {
                    foreach (var token in TicksToMML(gap, ppq, isRest: true, dialect: dialect))
                        AppendToken(sb, token, ref firstToken);
                }

                // Velocity
                int vel = (int)Math.Round(note.Velocity / 127.0 * 15);
                vel = Math.Max(0, Math.Min(15, vel));
                if (vel != curVelocity)
                {
                    AppendToken(sb, "v" + vel, ref firstToken);
                    curVelocity = vel;
                }

                // Octave
                int octave = (note.NoteNum / 12) - 1;
                octave = Math.Max(1, Math.Min(8, octave));
                if (octave != curOctave)
                {
                    int diff = octave - curOctave;
                    if (diff == 1) AppendToken(sb, ">", ref firstToken);
                    else if (diff == -1) AppendToken(sb, "<", ref firstToken);
                    else AppendToken(sb, "o" + octave, ref firstToken);
                    curOctave = octave;
                }

                // Note
                long noteTicks = note.EndTick - note.StartTick;
                var noteTokens = TicksToMML(noteTicks, ppq, isRest: false,
                                            noteNum: note.NoteNum,
                                            dialect: dialect);
                foreach (var token in noteTokens)
                    AppendToken(sb, token, ref firstToken);

                cursor = note.EndTick;
            }

            return sb.ToString().Trim();
        }

        // ── Ticks → MML-Token(s) ─────────────────────────────────────────

        // Standard-Längen in Ticks (bei PPQ=480):
        // 1=1920, 2=960, 4=480, 8=240, 16=120, 32=60, 64=30
        // Punktiert: *1.5

        private static readonly int[] BaseLengths = { 1, 2, 4, 8, 16, 32, 64 };

        private static List<string> TicksToMML(
            long ticks, int ppq, bool isRest,
            int noteNum = 60, MMLDialect dialect = MMLDialect.Generic)
        {
            var tokens = new List<string>();
            long remaining = ticks;
            bool firstNote = true;

            while (remaining > 0)
            {
                // Beste Länge finden (größte die <= remaining)
                int bestLen = 64;
                bool bestDot = false;
                long bestTicks = TicksForLength(1, ppq); // Minimum

                foreach (int len in BaseLengths)
                {
                    long t = TicksForLength(len, ppq);
                    long tDot = t * 3 / 2;

                    if (t <= remaining && t > bestTicks)
                        bestLen = len; bestDot = false; bestTicks = t;
                    if (tDot <= remaining && tDot > bestTicks)
                        bestLen = len; bestDot = true; bestTicks = tDot;
                }

                string lenStr = bestLen.ToString() + (bestDot ? "." : "");

                if (isRest)
                    tokens.Add("r" + lenStr);
                else
                {
                    string noteName = NoteNumToName(noteNum, dialect);
                    if (firstNote)
                        tokens.Add(noteName + lenStr);
                    else
                        tokens.Add("&" + noteName + lenStr); // Tie
                    firstNote = false;
                }

                remaining -= bestTicks;
                // Schutz vor Endlosschleife bei sehr kleinen Werten
                if (bestTicks == 0) break;
            }

            return tokens;
        }

        private static long TicksForLength(int length, int ppq)
        {
            return (ppq * 4L) / length;
        }

        // ── Noten-Name ────────────────────────────────────────────────────

        private static string NoteNumToName(int noteNum, MMLDialect dialect)
        {
            // Englische Noten-Namen (Standard und MapleStory 2)
            string[] englishNotes = { "c", "c+", "d", "d+", "e", "f", "f+", "g", "g+", "a", "a+", "b" };
            // Deutsche Noten-Namen (Mabinogi): b-flat = "b", b-natural = "b+"
            string[] mabiNotes = { "c", "c+", "d", "d+", "e", "f", "f+", "g", "g+", "a", "a+", "b+" };
            // Mabinogi: "b" (ohne +) = Bb, also a+ und b sind beide Bb?
            // Nein: a+ = Bb (MIDI 10), b = Bb in Mabinogi = MIDI 10
            // b+ = B-natural (MIDI 11)
            // Also: MIDI 10 → "a+" ODER "b" (beide korrekt, "a+" ist eindeutiger)
            // MIDI 11 → "b+" in Mabinogi

            string[] notes = (dialect == MMLDialect.Mabinogi) ? mabiNotes : englishNotes;
            return notes[noteNum % 12];
        }

        // ── Tempo-Extraktion ──────────────────────────────────────────────

        private static int ExtractTempo(MidiFile midi)
        {
            var changes = GetTempoChanges(midi);
            return changes.Count > 0 ? changes[0].Bpm : 120;
        }

        private static List<TempoChange> GetTempoChanges(MidiFile midi)
        {
            var result = new List<TempoChange>();
            foreach (var chunk in midi.GetTrackChunks())
            {
                long tick = 0;
                foreach (var evt in chunk.Events)
                {
                    tick += evt.DeltaTime;
                    if (evt is SetTempoEvent ste)
                    {
                        int bpm = (int)Math.Round(60000000.0 / ste.MicrosecondsPerQuarterNote);
                        bpm = Math.Max(20, Math.Min(600, bpm));
                        // Duplikate überspringen
                        if (result.Count == 0 || result[result.Count - 1].Bpm != bpm)
                            result.Add(new TempoChange { Tick = tick, Bpm = bpm });
                    }
                }
            }
            if (result.Count == 0) result.Add(new TempoChange { Tick = 0, Bpm = 120 });
            return result;
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────

        private static void AppendToken(StringBuilder sb, string token, ref bool first)
        {
            if (!first) sb.Append(' ');
            sb.Append(token);
            first = false;
        }

        // ── Interne Datenstrukturen ───────────────────────────────────────

        private class NoteEntry
        {
            public long StartTick;
            public long EndTick;
            public int NoteNum;
            public int Velocity;
        }

        private class TempoChange
        {
            public long Tick;
            public int Bpm;
        }
    }
}
