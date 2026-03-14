/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;

namespace BardMusicPlayer.Transmogrify.Song.Importers.MML
{
    // ═══════════════════════════════════════════════════════════════════════
    //  MIDI-Event-Types (intern)
    // ═══════════════════════════════════════════════════════════════════════
    public enum MidiEventType { NoteOn, NoteOff, ProgramChange, Controller, Tempo }

    public class MidiEvent
    {
        public long Tick;
        public MidiEventType Type;
        public int Channel;    // 0-15
        public int Param1;     // Note or controller-Nr
        public int Param2;     // velocity or value
        public int Tempo;      // µs/quarter (only for Tempo-Events)
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Channel - State
    // ═══════════════════════════════════════════════════════════════════════
    class ChannelState
    {
        public int Octave = 4;
        public int DefaultLen = 4;  // Quarter
        public bool DefaultDot = false;
        public int Volume = 100;    // 0-127 MIDI-Velocity
        public int Pan = 64;        // 0-127
        public int Program = 0;     // GM-Instrument
        public long Tick = 0;       // current tick
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Parser – converts Token-Stream to MIDI-Events
    // ═══════════════════════════════════════════════════════════════════════
    public class MMLParser
    {
        public const int PPQ = 480;     // Ticks per quarter / resolution

        private List<Token> _tokens;
        private int _pos;
        private int _tempo = 120;       // BPM global
        private ChannelState _cur;
        private int _channel = 0;       // current midi channel
        private Dictionary<int, ChannelState> _channels = new Dictionary<int, ChannelState>();

        public List<MidiEvent> Parse(List<Token> tokens)
        {
            _tokens = tokens;
            _pos = 0;
            _cur = GetOrCreate(0);

            var events = new List<MidiEvent>();

            // Initial Tempo-Event
            events.Add(new MidiEvent
            {
                Tick = 0,
                Type = MidiEventType.Tempo,
                Tempo = BpmToMicros(_tempo)
            });

            while (Peek().Type != TokenType.Eof)
            {
                var t = Advance();
                switch (t.Type)
                {
                    case TokenType.Channel:
                        _channel = t.IntValue - 1;
                        _cur = GetOrCreate(_channel);
                        break;

                    case TokenType.Tempo:
                        _tempo = t.IntValue;
                        events.Add(new MidiEvent
                        {
                            Tick = _cur.Tick,
                            Type = MidiEventType.Tempo,
                            Tempo = BpmToMicros(_tempo)
                        });
                        break;

                    case TokenType.Octave: _cur.Octave = t.IntValue; break;
                    case TokenType.OctaveUp: _cur.Octave = Math.Min(8, _cur.Octave + 1); break;
                    case TokenType.OctaveDown: _cur.Octave = Math.Max(0, _cur.Octave - 1); break;

                    case TokenType.Length:
                        _cur.DefaultLen = t.IntValue;
                        _cur.DefaultDot = t.Dotted;
                        break;

                    case TokenType.Volume:
                        _cur.Volume = (int)(t.IntValue / 15.0 * 127);
                        break;

                    case TokenType.Pan:
                        _cur.Pan = t.IntValue;
                        events.Add(MakeCC(0x0A, _cur.Pan));
                        break;

                    case TokenType.Instrument:
                        _cur.Program = t.IntValue;
                        events.Add(new MidiEvent
                        {
                            Tick = _cur.Tick,
                            Type = MidiEventType.ProgramChange,
                            Channel = _channel,
                            Param1 = _cur.Program
                        });
                        break;

                    case TokenType.Rest:
                        _cur.Tick += CalcTicks(t.Length, t.Dotted);
                        break;

                    case TokenType.Note:
                        events.AddRange(EmitNote(t));
                        break;

                    case TokenType.ChordStart:
                        events.AddRange(EmitChord());
                        break;
                }
            }

            events.Sort((a, b) => a.Tick.CompareTo(b.Tick));
            return events;
        }

        // ── single note ──────────────────────────────────────────────────
        private IEnumerable<MidiEvent> EmitNote(Token t, long? startTick = null)
        {
            int midiNote = NoteToMidi(t.NoteChar, t.Accidental, _cur.Octave);
            long ticks = CalcTicks(t.Length, t.Dotted);
            long start = startTick ?? _cur.Tick;

            // Tie: Extend NoteOff time, accumulate tick advance
            long totalTicks = ticks;
            long endTick = start + ticks;
            while (Peek().Type == TokenType.Tie)
            {
                Advance(); // &
                if (Peek().Type == TokenType.Note)
                {
                    var tied = Advance();
                    long tieTicks = CalcTicks(tied.Length, tied.Dotted);
                    totalTicks += tieTicks;
                    endTick += tieTicks;
                }
            }

            yield return new MidiEvent
            {
                Tick = start,
                Type = MidiEventType.NoteOn,
                Channel = _channel,
                Param1 = midiNote,
                Param2 = _cur.Volume
            };
            yield return new MidiEvent
            {
                Tick = endTick - 1,
                Type = MidiEventType.NoteOff,
                Channel = _channel,
                Param1 = midiNote,
                Param2 = 0
            };

            if (startTick == null)
                _cur.Tick += totalTicks;
        }

        // ── Chords [CEG] ───────────────────────────────────────────────────
        private IEnumerable<MidiEvent> EmitChord()
        {
            var chordNotes = new List<Token>();
            while (Peek().Type != TokenType.ChordEnd && Peek().Type != TokenType.Eof)
            {
                var t = Advance();
                if (t.Type == TokenType.Note) chordNotes.Add(t);
            }
            if (Peek().Type == TokenType.ChordEnd) Advance();

            // optional length after ]
            int chordLen = 0; bool chordDot = false;
            if (Peek().Type == TokenType.Note && chordNotes.Count > 0)
            {
                // The duration is indicated directly next to the closing bracket
            }

            long startTick = _cur.Tick;
            long maxTicks = 0;
            var all = new List<MidiEvent>();

            foreach (var n in chordNotes)
            {
                long ticks = CalcTicks(n.Length, n.Dotted);
                if (ticks > maxTicks) maxTicks = ticks;
                all.AddRange(EmitNote(n, startTick));
            }

            _cur.Tick = startTick + maxTicks;
            return all;
        }

        // ── Calculation ──────────────────────────────────────────────────

        private long CalcTicks(int len, bool dotted)
        {
            if (len == 0) { len = _cur.DefaultLen; dotted = dotted || _cur.DefaultDot; }
            long ticks = (PPQ * 4) / len;
            if (dotted) ticks = ticks * 3 / 2;
            return ticks;
        }

        private static int NoteToMidi(char note, int accidental, int octave)
        {
            int[] semitone = { 0, 2, 4, 5, 7, 9, 11 };   // C D E F G A B
            int idx = "CDEFGAB".IndexOf(note);
            return 12 * (octave + 1) + semitone[idx] + accidental;
        }

        private static int BpmToMicros(int bpm) => 60000000 / bpm;

        private MidiEvent MakeCC(int cc, int value) => new MidiEvent
        {
            Tick = _cur.Tick,
            Type = MidiEventType.Controller,
            Channel = _channel,
            Param1 = cc,
            Param2 = value
        };

        private ChannelState GetOrCreate(int ch)
        {
            if (!_channels.TryGetValue(ch, out var s))
                _channels[ch] = s = new ChannelState();
            return s;
        }

        private Token Peek() => _tokens[_pos];
        private Token Advance() => _tokens[_pos++];
    }
}
