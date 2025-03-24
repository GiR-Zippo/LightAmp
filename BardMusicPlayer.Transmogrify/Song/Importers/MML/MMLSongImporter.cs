/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BardMusicPlayer.Quotidian.Structs;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace BardMusicPlayer.Transmogrify.Song.Importers.MML
{
    public enum Chap
    {
        NULL = 0,
        SETTINGS = 1,
        CHANNEL = 2,
        FINISHED = 3
    }

    public static class MMLSongImporter
    {
        private static readonly Dictionary<int, List<MMLCommand>> musicData = new();
        private static readonly Dictionary<int, string> musicInstrument = new();

        private static readonly string mmlPatterns =
            @"[tT]\d{1,3}|[lL](16|2|4|8|1|32|64)\.?|[vV]\d+|[oO]\d|<|>|[a-gA-G](\+|#|-)?(16|2|4|8|1|32|64)?\.?|[rR](16|2|4|8|1|32|64)?\.?|[nN]\d+\.?|&";

        private static Chap _chap { get; set; } = Chap.NULL;

        private static string Title { get; set; } = "None";
        private static int CurrentChannel { get; set; }

        /// <summary>
        ///     Opens and process a mmslong file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static MidiFile OpenMMLSongFile(string path)
        {
            if (!File.Exists(path)) throw new BmpTransmogrifyException("File " + path + " does not exist!");

            var rfile = new StreamReader(path);
            var musicdata = "";
            while (rfile.Peek() >= 0)
            {
                var line = rfile.ReadLine();
                if (line.Contains("[Settings]")) _chap = Chap.SETTINGS;

                if (line.StartsWith("[Channel", StringComparison.Ordinal))
                {
                    if (musicdata.Length > 0)
                    {
                        var matches = Regex.Matches(StripComments(musicdata), mmlPatterns);
                        for (var i = 0; i < matches.Count; ++i)
                            musicData[CurrentChannel].Add(MMLCommand.Parse(matches[i].Value));
                        musicdata = "";
                    }

                    _chap = Chap.CHANNEL;
                    CurrentChannel++;
                    musicData.Add(CurrentChannel, new List<MMLCommand>());
                    Console.WriteLine(CurrentChannel);
                    continue;
                }

                if (line.Contains("[3MLE EXTENSION]"))
                {
                    if (musicdata.Length > 0)
                    {
                        var matches = Regex.Matches(StripComments(musicdata), mmlPatterns);
                        for (var i = 0; i < matches.Count; ++i)
                            musicData[CurrentChannel].Add(MMLCommand.Parse(matches[i].Value));
                        musicdata = "";
                    }

                    _chap = Chap.FINISHED;
                }

                switch (_chap)
                {
                    case Chap.SETTINGS:
                        {
                            if (line.Split('=')[0] == "Title") Title = line.Split('=')[1];

                            break;
                        }
                    case Chap.CHANNEL when line.StartsWith("//", StringComparison.Ordinal):
                        {
                            var result = Instrument.Parse(StripComments(line));
                            if (result.Index != 0) musicInstrument[CurrentChannel] = result.Name;

                            continue;
                        }
                    case Chap.CHANNEL:
                        musicdata += StripComments(line);
                        break;
                }
            }

            return CreateMidi();
        }

        private static string StripComments(string code)
        {
            if (code.StartsWith("/*", StringComparison.Ordinal))
            {
                var idx = code.IndexOf("*/", StringComparison.Ordinal);
                code = code.Substring(idx + 2);
            }

            if (code.StartsWith("//", StringComparison.Ordinal))
            {
                var idx = code.IndexOf("//", StringComparison.Ordinal);
                code = code.Substring(idx + 2);
            }

            code = string.Concat(code.Where(static c => !char.IsWhiteSpace(c)));
            return code;
        }

        private static MidiFile CreateMidi()
        {
            var midiFile = new MidiFile();
            int CurrentTempo = 120;

            foreach (var t in musicData)
            {
                var m = t.Value.FirstOrDefault(n => n.Type == MMLCommandType.Tempo);
                if (m.Args != null)
                {
                    midiFile.ReplaceTempoMap(TempoMap.Create(Tempo.FromBeatsPerMinute(Convert.ToInt32(m.Args[0]))));
                    CurrentTempo = Convert.ToInt32(m.Args[0]);
                    break;
                }
            }
            foreach (var t in musicData)
            {
                double duration = 0;

                int CurrentLength = 4;
                bool CurrentLengthDotted = false;
                int CurrentOctave = 4;
                int CurrentNote = -1;
                bool inTie = false;

                musicInstrument.TryGetValue(t.Key, out var instrument);
                instrument ??= "Piano";

                var thisTrack = new TrackChunk(new SequenceTrackNameEvent(instrument));

                var result = Instrument.Parse(instrument);
                for (var i = 0; i < t.Value.Count;)
                {
                    var cmd = t.Value[i];
                    switch (cmd.Type)
                    {
                        case MMLCommandType.Tempo:
                            CurrentTempo = Convert.ToInt32(cmd.Args[0]);
                            break;
                        case MMLCommandType.Octave:
                            CurrentOctave = Convert.ToInt32(cmd.Args[0]);
                            break;
                        case MMLCommandType.OctaveDown:
                            CurrentOctave--;
                            break;
                        case MMLCommandType.OctaveUp:
                            CurrentOctave++;
                            break;
                        case MMLCommandType.Tie:
                            inTie = true;
                            break;
                        case MMLCommandType.Length:
                            CurrentLength = Convert.ToInt32(cmd.Args[0]);
                            CurrentLengthDotted = cmd.Args[1] == ".";
                            break;
                        case MMLCommandType.Rest:
                            if (CurrentNote != -1)
                            {
                                SetNoteOff(thisTrack, duration, (SevenBitNumber)CurrentNote);
                                CurrentNote = -1;
                            }
                            duration += GetLength(CurrentTempo, CurrentLength, CurrentLengthDotted, cmd.Args[0], cmd.Args[1]);
                            break;
                        case MMLCommandType.NoteNumber:
                            //Did we had a tie
                            if (i != 0)
                            {
                                if (t.Value[i - 1].Type == MMLCommandType.Tie && CurrentNote != -1)
                                {
                                    duration += GetLength(CurrentTempo, CurrentLength, CurrentLengthDotted);
                                    break;
                                }
                            }

                            if (CurrentNote != -1)
                            {
                                SetNoteOff(thisTrack, duration, (SevenBitNumber)CurrentNote);
                                CurrentNote = -1;
                            }

                            CurrentNote = Convert.ToInt32(cmd.Args[0]);
                            SetNoteOn(thisTrack, duration, (SevenBitNumber)CurrentNote);
                            duration += GetLength(CurrentTempo, CurrentLength, CurrentLengthDotted);
                            break;
                        case MMLCommandType.Note:

                            //Did we had a tie
                            if (i != 0)
                            {
                                if (t.Value[i - 1].Type == MMLCommandType.Tie && CurrentNote != -1)
                                {
                                    duration += GetLength(CurrentTempo, CurrentLength, CurrentLengthDotted, cmd.Args[2], cmd.Args[3]);
                                    break;
                                }
                            }

                            if (CurrentNote != -1)
                            {
                                SetNoteOff(thisTrack, duration, (SevenBitNumber)CurrentNote);
                                CurrentNote = -1;
                            }

                            CurrentNote = GetNote(cmd, CurrentOctave);
                            SetNoteOn(thisTrack, duration, (SevenBitNumber)CurrentNote);
                            duration += GetLength(CurrentTempo, CurrentLength, CurrentLengthDotted, cmd.Args[2], cmd.Args[3]);
                            break;
                    }
                    i++;
                }
                midiFile.Chunks.Add(thisTrack);
            }

            //midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)(60000 / _Tempo));
            //midiFile.ReplaceTempoMap(TempoMap.Create(Tempo.FromBeatsPerMinute(_Tempo)));

            musicData.Clear();
            return midiFile;
        }

        private static double GetLength(int CurrentTempo, int CurrentLength, bool CurrentDot, string ParamLength="", string ParamDot="")
        {
            if (ParamLength != "")
                return new MMLLength(Convert.ToInt32(ParamLength), ParamDot == ".").ToTimeSpan(Tempo.FromBeatsPerMinute(CurrentTempo).MicrosecondsPerQuarterNote / 1000).TotalMilliseconds;

            return new MMLLength(CurrentLength, ParamDot == "." || CurrentDot).ToTimeSpan(Tempo.FromBeatsPerMinute(CurrentTempo).MicrosecondsPerQuarterNote / 1000).TotalMilliseconds;
        }
        private static double GetLengthB(int tempo, double globLength, string multiplier, bool dottet = false)
        {
            double length = Tempo.FromBeatsPerMinute(tempo).MicrosecondsPerQuarterNote;
            if (multiplier == "")
            {
                length = globLength;
                if (dottet)
                    return length * 1.5;

                return length;
            }

            var length_multiplier = Convert.ToInt32(multiplier);
            if (dottet)
                return (length / length_multiplier) * 1.5;

            return length / length_multiplier;
        }

        /// <summary>
        /// Returns the NoteNumber
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="octave"></param>
        /// <returns></returns>
        private static SevenBitNumber GetNote(MMLCommand cmd, int octave)
        {
            var noteType = cmd.Args[0].ToLower();
            switch (cmd.Args[1])
            {
                case "#":
                case "+":
                    return Melanchall.DryWetMidi.MusicTheory.Note.Parse(noteType + "#" + octave.ToString()).NoteNumber;
                case "-":
                default:
                    return Melanchall.DryWetMidi.MusicTheory.Note.Parse(noteType + octave.ToString()).NoteNumber;
            }
        }

        /// <summary>
        /// Sets a note on
        /// </summary>
        /// <param name="track"></param>
        /// <param name="duration"></param>
        /// <param name="noteNumber"></param>
        private static void SetNoteOn(TrackChunk track, double duration, SevenBitNumber noteNumber)
        {
            using (var manager = new TimedObjectsManager<TimedEvent>(track.Events))
            {
                var timedEvents = manager.Objects;
                timedEvents.Add(new TimedEvent(new NoteOnEvent(noteNumber, (SevenBitNumber)127),
                    (long)duration / 1000));
            }
        }

        /// <summary>
        /// Sets a note off
        /// </summary>
        /// <param name="track"></param>
        /// <param name="duration"></param>
        /// <param name="noteNumber"></param>
        private static void SetNoteOff(TrackChunk track, double duration, SevenBitNumber noteNumber)
        {
            using (var manager = new TimedObjectsManager<TimedEvent>(track.Events))
            {
                var timedEvents = manager.Objects;
                timedEvents.Add(
                new TimedEvent(new NoteOffEvent(noteNumber, (SevenBitNumber)127), (long)duration / 1000));
            }
        }
    }
}