/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Transmogrify.Song.Manipulation;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BardMusicPlayer.Transmogrify.Song.Importers
{
    public static class MidiBardImporter
    {
        public class MidiFileConfig
        {
            public List<TrackConfig> Tracks = new List<TrackConfig>();
            public int ToneMode = 0;
            public bool AdaptNotes = true;
            public float Speed = 1;
        }

        public class TrackConfig
        {
            public int Index = 0;
            public bool Enabled = true;
            public string Name ="";
            public int Transpose = 0;
            public int Instrument = 0;
            public List<long> AssignedCids = new List<long>();
        }

        public class MidiTrack
        {
            public int Index { get; set; }
            public int TrackNumber { get; set; }
            public int trackInstrument { get; set; }
            public int Transpose { get; set; }
            public MusicalTimeSpan Quantize { get; set; } = null;
            public int ToneMode { get; set; }
            public Note MinNote { get; set; } = new Note((SevenBitNumber)127);
            public Note MaxNote { get; set; } = new Note((SevenBitNumber)0);
            public TrackChunk trackChunk { get; set; }
        }

        /// <summary>
        /// Config lesen
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static MidiFile OpenMidiFile(string filename)
        {
            List<MidiTrack> tracks = new List<MidiTrack>();
            MemoryStream memoryStream = new MemoryStream();
            FileStream fileStream = File.Open(Path.ChangeExtension(filename, "json"), FileMode.Open);
            fileStream.CopyTo(memoryStream);
            fileStream.Close();

            var data = memoryStream.ToArray();
            MidiFileConfig pdatalist = JsonConvert.DeserializeObject<MidiFileConfig>(new UTF8Encoding(true).GetString(data));

            //Read the midi
            MidiFile midifile = MidiFile.Read(filename);

            //create the dict for the cids to tracks
            Dictionary<int, int> cids = new Dictionary<int, int>();
            int idx = 0;
            int cid_count = 1;
            foreach (TrackChunk chunk in midifile.GetTrackChunks())
            {
                if (chunk.GetNotes().Count < 1)
                    continue;

                int cid = (int)pdatalist.Tracks[idx].AssignedCids[0];
                if (cids.ContainsKey(cid))
                    cid = cids[cid];
                else
                {
                    cids.Add(cid, cid_count);
                    cid = cid_count;
                    cid_count++;
                }

                MidiTrack midiTrack = new MidiTrack();
                midiTrack.Index = pdatalist.Tracks[idx].Index;
                midiTrack.TrackNumber = cid;
                midiTrack.trackInstrument = pdatalist.Tracks[idx].Instrument - 1;
                midiTrack.Transpose = pdatalist.Tracks[idx].Transpose / 12;
                midiTrack.ToneMode = pdatalist.ToneMode;
                midiTrack.trackChunk = chunk;

                tracks.Add(midiTrack);
                idx++;
            }
            pdatalist = null;
            return Convert(midifile, tracks).Result;
        }

        public static void PrepareGuitarTrack(TrackChunk tc, int mode, int prognumber)
        {
            if (mode == 3)
            {
                TrackManipulations.ClearProgChanges(tc);
            }
        }

        public static Task<MidiFile> Convert(MidiFile midiFile, List<MidiTrack> tracks)
        {
            MidiFile exportMidi = new MidiFile();
            exportMidi.ReplaceTempoMap(midiFile.GetTempoMap());

            List<TrackChunk> chunks = new List<TrackChunk>();
            List<int> trackNums = new List<int>();

            //tracks suchen
            foreach (var midiTrack in tracks)
            {
                if (trackNums.Contains(midiTrack.TrackNumber))
                    continue;

                trackNums.Add(midiTrack.TrackNumber);
            }

            //und verarbeiten
            foreach (int TrackNum in trackNums)
            {
                var midiTrackList = tracks.FindAll(n => n.TrackNumber == TrackNum);
                if (midiTrackList.Count < 1)
                    continue;

                //The fast way
                else if (midiTrackList.Count == 1)
                {
                    //Get min-max transpose
                    foreach (var item in midiTrackList.First().trackChunk.GetNotes())
                    {
                        if (item.NoteNumber < midiTrackList.First().MinNote.NoteNumber)
                            midiTrackList.First().MinNote = item;
                        if (item.NoteNumber > midiTrackList.First().MaxNote.NoteNumber)
                            midiTrackList.First().MaxNote = item;
                    };
                    var transpose = getMaxTranspose(midiTrackList.First(), midiTrackList.First().Transpose);
                    int chanNum = midiTrackList.First().Index; // TrackNum - 1;

                    PrepareGuitarTrack(midiTrackList.First().trackChunk, midiTrackList.First().ToneMode, Instrument.Parse(midiTrackList.First().trackInstrument + 1).MidiProgramChangeCode);
                    TrackManipulations.SetTrackName(midiTrackList.First().trackChunk, Instrument.Parse(midiTrackList.First().trackInstrument + 1).Name);
                    TrackManipulations.SetInstrument(midiTrackList.First().trackChunk, Instrument.Parse(midiTrackList.First().trackInstrument + 1).MidiProgramChangeCode);
                    midiTrackList.First().trackChunk.ProcessNotes(n =>
                    {
                        if ((n.NoteNumber + 12 * transpose) >= 0 && (n.NoteNumber + 12 * transpose) <= 127)
                            n.NoteNumber = (SevenBitNumber)(n.NoteNumber + 12 * transpose);
                    });

                    TrackManipulations.SetChanNumber(midiTrackList.First().trackChunk, chanNum);
                    exportMidi.Chunks.Add(midiTrackList.First().trackChunk);
                }
                else if (midiTrackList.Count > 1) //merge track groups
                {
                    //Get min-max transpose
                    foreach (var item in midiTrackList.First().trackChunk.GetNotes())
                    {
                        if (item.NoteNumber < midiTrackList.First().MinNote.NoteNumber)
                            midiTrackList.First().MinNote = item;
                        if (item.NoteNumber > midiTrackList.First().MaxNote.NoteNumber)
                            midiTrackList.First().MaxNote = item;
                    };
                    var transpose = getMaxTranspose(midiTrackList.First(), midiTrackList.First().Transpose);
                    int chanNum = midiTrackList.First().Index; // TrackNum - 1;

                    //Do the octave shift and push them into a list
                    List<KeyValuePair<long, KeyValuePair<int, TimedEvent>>> tis = new List<KeyValuePair<long, KeyValuePair<int, TimedEvent>>>();
                    foreach (var subChunk in midiTrackList)
                    {
                        midiTrackList.First().trackChunk.ProcessNotes(n =>
                        {
                            if ((n.NoteNumber + 12 * transpose) >= 0 && (n.NoteNumber + 12 * transpose) <= 127)
                                n.NoteNumber = (SevenBitNumber)(n.NoteNumber + 12 * transpose);
                        });

                        foreach (TimedEvent t in subChunk.trackChunk.GetTimedEvents())
                        {
                            if (t.Event.EventType == MidiEventType.NoteOn ||
                                t.Event.EventType == MidiEventType.NoteOff)
                                tis.Add(new KeyValuePair<long, KeyValuePair<int, TimedEvent>>(t.Time, new KeyValuePair<int, TimedEvent>(Instrument.Parse(subChunk.trackInstrument + 1).MidiProgramChangeCode, t)));
                        }
                    }

                    TrackChunk newTrackChunk = new TrackChunk(new SequenceTrackNameEvent("None"));
                    int instr = -1;
                    using (var events = newTrackChunk.ManageTimedEvents())
                    {
                        foreach (var t in tis.OrderBy(n => n.Key))
                        {
                            long time = t.Key;
                            TimedEvent ev = t.Value.Value;

                            if (instr != t.Value.Key)
                            {
                                if (instr == -1)
                                {
                                    var fev = events.Objects.Where(fe => fe.Event.EventType == MidiEventType.SequenceTrackName).FirstOrDefault();
                                    if (fev != null)
                                        (fev.Event as SequenceTrackNameEvent).Text = Instrument.ParseByProgramChange(t.Value.Key).Name;
                                    var pe = new ProgramChangeEvent((SevenBitNumber)t.Value.Key);
                                    pe.Channel = (FourBitNumber)chanNum;
                                    events.Objects.Add(new TimedEvent(pe, 0));
                                }

                                instr = t.Value.Key;
                                var noteOn = ev.Event as NoteOnEvent;
                                if (noteOn != null)
                                {
                                    ProgramChangeEvent pc = new ProgramChangeEvent((SevenBitNumber)instr);
                                    events.Objects.Add(new TimedEvent(pc, ev.Time));
                                }
                            }
                            events.Objects.Add(ev);
                        }
                    }
                    TrackManipulations.SetChanNumber(newTrackChunk, chanNum);
                    exportMidi.Chunks.Add(newTrackChunk);
                    tis.Clear();
                }
            }
            return Task.FromResult(exportMidi);
        }

        public static Task<MidiFile> Convert_old(MidiFile midiFile, List<MidiTrack> tracks)
        {
            MidiFile exportMidi = new MidiFile();
            exportMidi.ReplaceTempoMap(midiFile.GetTempoMap());

            List<TrackChunk> chunks = new List<TrackChunk>();
            List<int> trackNums = new List<int>();

            //tracks suchen
            foreach (var d in tracks)
            {
                if (trackNums.Contains(d.TrackNumber))
                    continue;

                trackNums.Add(d.TrackNumber);
            }

            //und verarbeiten
            foreach (int TrackNum in trackNums)
            {
                var d = tracks.FindAll(n => n.TrackNumber == TrackNum);
                if (d.Count < 1)
                    continue;

                //The fast way
                else if (d.Count == 1)
                {
                    //Get min-max transpose
                    foreach (var item in d.First().trackChunk.GetNotes())
                    {
                        if (item.NoteNumber < d.First().MinNote.NoteNumber)
                            d.First().MinNote = item;
                        if (item.NoteNumber > d.First().MaxNote.NoteNumber)
                            d.First().MaxNote = item;
                    };
                    var transpose = getMaxTranspose(d.First(), d.First().Transpose);
                    int chanNum = d.First().Index; // TrackNum - 1;

                    PrepareGuitarTrack(d.First().trackChunk, d.First().ToneMode, Instrument.Parse(d.First().trackInstrument + 1).MidiProgramChangeCode);
                    TrackManipulations.SetTrackName(d.First().trackChunk, Instrument.Parse(d.First().trackInstrument + 1).Name);
                    TrackManipulations.SetInstrument(d.First().trackChunk, Instrument.Parse(d.First().trackInstrument + 1).MidiProgramChangeCode);
                    d.First().trackChunk.ProcessNotes(n =>
                    {
                        if ((n.NoteNumber + 12 * transpose) >= 0 && (n.NoteNumber + 12 * transpose) <= 127)
                            n.NoteNumber = (SevenBitNumber)(n.NoteNumber + 12*transpose);
                    });

                    TrackManipulations.SetChanNumber(d.First().trackChunk, chanNum);
                    exportMidi.Chunks.Add(d.First().trackChunk);
                }
                else if (d.Count > 1) //merge track groups
                {
                    //Get min-max transpose
                    foreach (var item in d.First().trackChunk.GetNotes())
                    {
                        if (item.NoteNumber < d.First().MinNote.NoteNumber)
                            d.First().MinNote = item;
                        if (item.NoteNumber > d.First().MaxNote.NoteNumber)
                            d.First().MaxNote = item;
                    };
                    var transpose = getMaxTranspose(d.First(), d.First().Transpose);
                    int chanNum = d.First().Index; // TrackNum - 1;

                    //Do the octave shift and push them into a list
                    List<KeyValuePair<long, KeyValuePair<int, TimedEvent>>> tis = new List<KeyValuePair<long, KeyValuePair<int, TimedEvent>>>();
                    foreach (var subChunk in d)
                    {
                        d.First().trackChunk.ProcessNotes(n =>
                        {
                            if ((n.NoteNumber + 12 * transpose) >= 0 && (n.NoteNumber + 12 * transpose) <= 127)
                                n.NoteNumber = (SevenBitNumber)(n.NoteNumber + 12 * transpose);
                        });

                        foreach (TimedEvent t in subChunk.trackChunk.GetTimedEvents())
                        {
                            if (t.Event.EventType == MidiEventType.NoteOn ||
                                t.Event.EventType == MidiEventType.NoteOff)
                                tis.Add(new KeyValuePair<long, KeyValuePair<int, TimedEvent>>(t.Time, new KeyValuePair<int, TimedEvent>(Instrument.Parse(subChunk.trackInstrument + 1).MidiProgramChangeCode, t)));
                        }
                    }

                    TrackChunk newTC = new TrackChunk(new SequenceTrackNameEvent("None"));
                    int instr = -1;
                    using (var events = newTC.ManageTimedEvents())
                    {
                        foreach (var t in tis.OrderBy(n => n.Key))
                        {
                            long time = t.Key;
                            TimedEvent ev = t.Value.Value;

                            if (instr != t.Value.Key)
                            {
                                if (instr == -1)
                                {
                                    var fev = events.Objects.Where(fe => fe.Event.EventType == MidiEventType.SequenceTrackName).FirstOrDefault();
                                    if (fev != null)
                                        (fev.Event as SequenceTrackNameEvent).Text = Instrument.ParseByProgramChange(t.Value.Key).Name;
                                    var pe = new ProgramChangeEvent((SevenBitNumber)t.Value.Key);
                                    pe.Channel = (FourBitNumber)chanNum;
                                    events.Objects.Add(new TimedEvent(pe, 0));
                                }

                                instr = t.Value.Key;
                                var x = ev.Event as NoteOnEvent;
                                if (x != null)
                                {
                                    ProgramChangeEvent pc = new ProgramChangeEvent((SevenBitNumber)instr);
                                    pc.Channel = x.Channel;
                                    if (ev.TimeAs<MetricTimeSpan>(midiFile.GetTempoMap()).TotalMilliseconds > 30)
                                    {
                                        var newTime = ev.TimeAs<MetricTimeSpan>(midiFile.GetTempoMap()).Subtract(new MetricTimeSpan(0, 0, 0, 30), TimeSpanMode.TimeTime);
                                        events.Objects.Add(new TimedEvent(pc, TimeConverter.ConvertFrom(newTime, midiFile.GetTempoMap())));
                                    }
                                    else
                                        events.Objects.Add(new TimedEvent(pc, ev.Time));
                                }
                            }
                            events.Objects.Add(ev);
                        }
                    }
                    TrackManipulations.SetChanNumber(newTC, chanNum);
                    exportMidi.Chunks.Add(newTC);
                    tis.Clear();
                }
            }
            return Task.FromResult(exportMidi);
        }

        /// <summary>
        /// Get the lowest and highest note
        /// </summary>
        /// <param name="trackChunk"></param>
        /// <returns></returns>
        private static int getMaxTranspose(MidiTrack track, int transpose)
        {
            var x = track.MinNote.NoteNumber + (12 * transpose);
            var y = track.MaxNote.NoteNumber + (12 * transpose);

            int minTranspose = -1;
            int maxTranspose = -1;
            if (x < 0)
                minTranspose = (int)Math.Ceiling((double)-x / 12);
            if (y > 127)
                maxTranspose = (int)Math.Ceiling((double)(y - 127) / 12);

            if (minTranspose != -1)
                return transpose + minTranspose;
            if (maxTranspose != -1)
                return transpose - maxTranspose;
            return transpose;
        }

        /// <summary>
        /// Get the lowest and highest note
        /// </summary>
        /// <param name="trackChunk"></param>
        /// <returns></returns>
        private static int getMaxTranspose(TrackChunk trackChunk, int transpose)
        {
            int low = 127;
            int high = 0;
            foreach (var note in trackChunk.GetNotes())
            {
                if (note.NoteNumber < low)
                    low = note.NoteNumber;
                if (note.NoteNumber > high)
                    high = note.NoteNumber;
            }
            var x = low + (12 * transpose);
            var y = high + (12 * transpose);

            int minTranspose = -1;
            int maxTranspose = -1;
            if (x < 0)
                minTranspose = (int)Math.Ceiling((double)-x / 12);
            if (y > 127 )
                maxTranspose = (int)Math.Floor((double)(y-127) / 12);

            if (minTranspose != -1)
                return minTranspose;
            if (maxTranspose != -1)
                return maxTranspose;
            return transpose;
        }
    }
}
