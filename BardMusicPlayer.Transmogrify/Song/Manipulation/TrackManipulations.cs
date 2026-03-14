/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian.Structs;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BardMusicPlayer.Transmogrify.Song.Manipulation
{
    public static class TrackManipulations
    {
        #region Get/Set Channel

        /// <summary>
        /// Get channel number by first note on from a <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <returns>Channelnumber as <see cref="int"/></returns>
        public static int GetChannelNumber(TrackChunk track)
        {
            var ev = track.Events.OfType<NoteOnEvent>().FirstOrDefault();
            if (ev != null)
                return ev.Channel;
            return -1;
        }

        /// <summary>
        /// Sets the channel number for a <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <param name="channelNumber"></param>
        /// <returns></returns>
        public static void SetChanNumber(TrackChunk track, int channelNumber)
        {
            if (channelNumber < 0)
                return;
            channelNumber = (channelNumber & 0x0F);

            using (var notesManager = track.ManageNotes())
            {
                Parallel.ForEach(notesManager.Objects, note =>
                {
                    note.Channel = (FourBitNumber)channelNumber;
                });
                notesManager.SaveChanges();
            }

            using (var manager = track.ManageTimedEvents())
            {
                Parallel.ForEach(manager.Objects, midiEvent =>
                {
                    if (midiEvent.Event is ProgramChangeEvent pe)
                        pe.Channel = (FourBitNumber)channelNumber;
                    if (midiEvent.Event is ControlChangeEvent ce)
                        ce.Channel = (FourBitNumber)channelNumber;
                    if (midiEvent.Event is ChannelAftertouchEvent ca)
                        ca.Channel = (FourBitNumber)channelNumber;
                    if (midiEvent.Event is NoteAftertouchEvent na)
                        na.Channel = (FourBitNumber)channelNumber;
                    if (midiEvent.Event is PitchBendEvent pbe)
                        pbe.Channel = (FourBitNumber)channelNumber;
                });
                manager.SaveChanges();
            }
        }

        #endregion

        #region Get/Set Instrument-ProgramChangeEvent
        /// <summary>
        /// Get the program number of the <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <returns>The <see cref="int"/> representation of the instrument</returns>
        public static int GetInstrument(TrackChunk track)
        {
            var ev = track.Events.Where(e => e.EventType == MidiEventType.ProgramChange).FirstOrDefault();
            if (ev != null)
                return (ev as ProgramChangeEvent).ProgramNumber;
            return 1; //return a "None" instrument cuz we don't have all midi instrument in XIV
        }

        /// <summary>
        /// Get the program number of the <see cref="TrackChunk"/> by <see cref="SequenceTrackNameEvent"/> 
        /// </summary>
        /// <param name="track"></param>
        /// <returns>The <see cref="int"/> representation of the instrument</returns>
        public static int GetInstrumentBySeqName(TrackChunk track)
        {
            var trackName = TrackManipulations.GetTrackName(track);
            int progNum = -1;

            Regex rex = new Regex(@"^([A-Za-z _:]+)([-+]\d)?");
            if (rex.Match(trackName) is Match match)
            {
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    if (match.Groups[1].Value == "Program:ElectricGuitar")
                    {
                        TimedEvent noteEvent = track.GetTimedEvents().FirstOrDefault(n => n.Event.EventType == MidiEventType.NoteOn);
                        if (noteEvent != default)
                        {
                            TimedEvent progEvent = track.GetTimedEvents().LastOrDefault(n => n.Event.EventType == MidiEventType.ProgramChange && n.Time <= noteEvent.Time);
                            if (progEvent != default)
                                progNum = (progEvent.Event as ProgramChangeEvent).ProgramNumber;
                        }
                    }
                    else
                    {
                        progNum = Instrument.Parse(match.Groups[1].Value).MidiProgramChangeCode;
                    }
                }
            }
            return progNum;
        }

        /// <summary>
        /// Create or overwrite the first progchange in <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <param name="instrument"></param>
        public static void SetInstrument(TrackChunk track, int instrument)
        {
            int channel = GetChannelNumber(track);
            if (channel == -1)
                return;

            using (var events = track.ManageTimedEvents())
            {
                var ev = events.Objects.Where(e => e.Event.EventType == MidiEventType.ProgramChange).FirstOrDefault();
                if (ev != null)
                {
                    var prog = ev.Event as ProgramChangeEvent;
                    prog.ProgramNumber = (SevenBitNumber)instrument;
                }
                else
                {
                    var pe = new ProgramChangeEvent((SevenBitNumber)instrument);
                    pe.Channel = (FourBitNumber)channel;
                    events.Objects.Add(new TimedEvent(pe, 0));
                }
                events.SaveChanges();
            }
        }
        #endregion

        #region Get/Set TrackName
        /// <summary>
        /// Get the name of the <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track">TrackChunk</param>
        /// <returns>The track-name as <see cref="string"/></returns>
        public static string GetTrackName(TrackChunk track)
        {
            var trackName = track.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text;
            if (trackName != null)
                return trackName;
            return "No Name";
        }

        /// <summary>
        /// Get the name of the <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track">TrackChunk</param>
        /// <returns>The track-name as <see cref="string"/></returns>
        public static string GetTrackNameByFirstProgram(TrackChunk track)
        {
            var progNum = track.Events.OfType<ProgramChangeEvent>().FirstOrDefault()?.ProgramNumber;
            if (progNum != null)
            {
                Instrument instrumentName;
                if (Instrument.TryParseByProgramChange((int)progNum, out instrumentName))
                    return instrumentName.Name;

            }
            return "No Name";
        }

        /// <summary>
        /// Sets the <see cref="TrackChunk"/> name
        /// </summary>
        /// <param name="track"></param>
        /// <param name="TrackName"></param>
        public static void SetTrackName(TrackChunk track, string TrackName)
        {
            using (var events = track.ManageTimedEvents())
            {
                var fev = events.Objects.Where(e => e.Event.EventType == MidiEventType.SequenceTrackName).FirstOrDefault();
                if (fev != null)
                {
                    (fev.Event as SequenceTrackNameEvent).Text = TrackName;
                    events.SaveChanges();
                }
                else
                {
                    SequenceTrackNameEvent name = new SequenceTrackNameEvent(TrackName);
                    track.Events.Insert(0, name);
                }

            }
        }
        #endregion

        #region Misc
        /// <summary>
        /// Remove all prog changes from <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        public static void ClearProgChanges(TrackChunk track)
        {
            using (var manager = track.ManageTimedEvents())
            {
                manager.Objects.RemoveAll(e => e.Event.EventType == MidiEventType.ProgramChange);
                manager.Objects.RemoveAll(e => e.Event.EventType == MidiEventType.ProgramName);
                manager.SaveChanges();
            }
        }

        /// <summary>
        /// DrumMapper Helper
        /// </summary>
        public class DrumMaps
        {
            public int MidiNote { get; set; } = 0;
            public string Instrument { get; set; } = "None";
            public int GameNote { get; set; } = 0;
        }

        /// <summary>
        /// Maps a midi drum track to specific notes and corrosponding separate <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <param name="fileName"></param>
        /// <returns>The Dictionary(InstrumentName, TrackChunk) or in case of an error: (ErrorMsg, Null)</returns>
        public static Dictionary<string, TrackChunk> DrumMapping(TrackChunk track, string fileName)
        {
            MemoryStream memoryStream = new MemoryStream();
            FileStream fileStream = File.Open(fileName, FileMode.Open);
            fileStream.CopyTo(memoryStream);
            fileStream.Close();

            Dictionary<string, TrackChunk> drumTracks = new Dictionary<string, TrackChunk>();
            List<DrumMaps> drumlist = null;
            var data = memoryStream.ToArray();
            try
            {
                drumlist = JsonConvert.DeserializeObject<List<DrumMaps>>(new UTF8Encoding(true).GetString(data));
            }
            catch
            {
                drumTracks.Add("Malformed drum map!", null);
                return drumTracks;
            }
            memoryStream.Close();
            memoryStream.Dispose();

            if (drumlist == null)
            {
                drumTracks.Add("Drum map is empty!", null);
                return drumTracks;
            }

            //And do it
            foreach (Note note in track.GetNotes())
            {
                var drum = drumlist.Where(dm => dm.MidiNote == note.NoteNumber).FirstOrDefault();
                if (drum == null)
                    continue;

                var ret = drumTracks.Where(item => item.Key == drum.Instrument).FirstOrDefault();
                if (ret.Key == null)
                {
                    drumTracks[drum.Instrument] = new TrackChunk(new SequenceTrackNameEvent(drum.Instrument));
                    using (var notesManager = drumTracks[drum.Instrument].ManageNotes())
                    {
                        TimedObjectsCollection<Note> notes = notesManager.Objects;
                        note.NoteNumber = (SevenBitNumber)drum.GameNote;
                        notes.Add(note);
                    }
                }
                else
                {
                    using (var notesManager = drumTracks[drum.Instrument].ManageNotes())
                    {
                        TimedObjectsCollection<Note> notes = notesManager.Objects;
                        note.NoteNumber = (SevenBitNumber)drum.GameNote;
                        notes.Add(note);
                    }
                }
            }
            drumlist.Clear();
            return drumTracks;
        }

        /// <summary>
        /// Merge <see cref="TrackChunk"/> into a new <see cref="TrackChunk"/> and appends or inserts it
        /// </summary>
        /// <remarks>
        /// <para>
        /// mode 0 - just merge everything and use the SequenceTrackName from the first <see cref="TrackChunk"/> 
        /// </para>
        /// <para>
        /// mode 1 - just merge the <see cref="Note"/> and insert <see cref="ProgramChangeEvent"/> according to <see cref="SequenceTrackNameEvent"/>
        /// </para>
        /// <para>
        /// position - the position to insert the new track in the <see cref="MidiFile"/>, counting at the first <see cref="TrackChunk"/> with <see cref="NoteOnEvent"/> 
        /// </para>
        /// </remarks>
        /// <param name="tracks"></param>
        /// <param name="midiFile"></param>
        /// <param name="mode"></param>
        /// <param name="position"></param>
        public static void MergeTracks(List<TrackChunk> tracks, MidiFile midiFile, byte mode =0, int position = -1)
        {
            TrackChunk trackChunk = null;
            string TrackName = "";
            //Normal merge
            if (mode == 0)
            {
                trackChunk = Melanchall.DryWetMidi.Core.TrackChunkUtilities.Merge(tracks);
                TrackName = GetTrackName(tracks[0]);
                trackChunk.RemoveTimedEvents(e => e.Event.EventType == MidiEventType.SequenceTrackName);
            }
            //MidiBard2 GuitarTrackMerge
            else if (mode == 1)
            {
                trackChunk = new TrackChunk();

                //Get prog changes
                Dictionary<int, SevenBitNumber> programs = new Dictionary<int, SevenBitNumber>();
                List<TimedEvent> newProgEvents = new List<TimedEvent>();
                for (int i = 0; i < tracks.Count; i++)
                {
                    programs.Add(i, new SevenBitNumber((byte)TrackManipulations.GetInstrumentBySeqName(tracks[i])));
                    TrackManipulations.SetChanNumber(tracks[i], i);
                }
                
                //Merge notes only
                foreach (var track in tracks)
                {
                    var Notes = track.GetNotes();
                    using (var noteMgr = trackChunk.ManageNotes())
                    {
                        noteMgr.Objects.Add(Notes);
                        noteMgr.SaveChanges();
                    }
                }

                //Build progList
                int currProg = -1;
                using (var tObject = trackChunk.ManageTimedEvents())
                {
                    foreach (var noteOnEvent in tObject.Objects.Where(e => e.Event.EventType == MidiEventType.NoteOn))
                    {
                        NoteOnEvent noteOn = noteOnEvent.Event as NoteOnEvent;
                        if (programs[noteOn.Channel] != currProg)
                        {
                            TimedEvent tEvent = new TimedEvent(new ProgramChangeEvent(programs[noteOn.Channel]), noteOnEvent.Time);
                            var ti = (tEvent.TimeAs<MetricTimeSpan>(midiFile.GetTempoMap()).TotalMilliseconds - 30);
                            if (ti > 0)
                                tEvent.Time = TimeConverter.ConvertFrom(new MetricTimeSpan((long)ti * 1000), midiFile.GetTempoMap());
                            else
                                tEvent.Time = 0;
                            newProgEvents.Add(tEvent);
                            currProg = programs[noteOn.Channel];
                        }
                    }
                    tObject.Objects.Add(newProgEvents);
                    tObject.SaveChanges();
                }
                //Set TrackName
                TrackName = GetTrackNameByFirstProgram(trackChunk);
            }

            //Set TrackName
            SequenceTrackNameEvent name = new SequenceTrackNameEvent(TrackName);
            trackChunk.Events.Insert(0, name);

            //cleanup
            TrackManipulations.SetChanNumber(trackChunk, (tracks[0].Events.FirstOrDefault(e => e.EventType == MidiEventType.NoteOn) as NoteOnEvent).Channel);
            foreach (var track in tracks)
                midiFile.Chunks.Remove(track);

            //Get first track with note events
            int idx = 0;
            foreach (TrackChunk chunk in midiFile.Chunks)
            {
                var s = chunk.Events.FirstOrDefault(e => e.EventType == MidiEventType.NoteOn);
                if (s != null)
                    break;
                idx++;
            }
            position += idx;
            if (position == -1)
                midiFile.Chunks.Add(trackChunk);
            else
                midiFile.Chunks.Insert(position, trackChunk);
        }
        #endregion

        #region All Tracks handling
        /// <summary>
        /// Check if this midi uses "all tracks"
        /// </summary>
        /// <param name="midiFile"></param>
        /// <returns></returns>
        public static bool HasAllTracks(MidiFile midiFile)
        {
            bool allTracks = false;
            Parallel.ForEach(midiFile.GetTrackChunks(), (originalChunk, loopState) =>
            {
                using (var timedEventsManager = new TimedObjectsManager<TimedEvent>(originalChunk.Events))
                {
                    TimedObjectsCollection<TimedEvent> events = timedEventsManager.Objects;
                    List<TimedEvent> prefixList = events.Where(static e => e.Event is NoteOnEvent).ToList();
                    int chan = -1;
                    foreach (TimedEvent tevent in prefixList)
                    {
                        if (tevent.Event is NoteOnEvent)
                        {
                            if ((tevent.Event as NoteOnEvent).Channel != chan)
                            {
                                if (allTracks)
                                    break;
                                if (chan == -1)
                                    chan = (tevent.Event as NoteOnEvent).Channel;
                                else
                                {
                                    allTracks = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            });
            return allTracks;
        }

        public static void ChannelsToPrograms(MidiFile midiFile)
        {
            int TrackNum = 0;
            TrackChunk ctlChunk = new TrackChunk();

            foreach (var originalChunk in midiFile.GetTrackChunks().AsParallel())
            {
                using (var events = originalChunk.ManageTimedEvents())
                {
                    //The proglist
                    List<TimedEvent> progEvents = events.Objects.Where(e => e.Event.EventType == MidiEventType.ProgramChange).ToList();
                    //new Progs
                    List<TimedEvent> newProgEvents = new List<TimedEvent>();

                    int currProg = -1;
                    using (var note = originalChunk.ManageNotes())
                    {
                        foreach (var noteon in note.Objects)
                        {
                            var prog = progEvents.FirstOrDefault(e => (e.Event as ProgramChangeEvent).Channel == noteon.Channel);
                            if (prog != default)
                            {
                                var newProg = prog.Clone() as TimedEvent;
                                newProg.Time = noteon.Time;

                                var ti = (newProg.TimeAs<MetricTimeSpan>(midiFile.GetTempoMap()).TotalMilliseconds - 30);
                                if (ti > 0)
                                {
                                    long ticksFromMetricLength = TimeConverter.ConvertFrom(new MetricTimeSpan((long)ti * 1000), midiFile.GetTempoMap());
                                    newProg.Time = ticksFromMetricLength;
                                }
                                else
                                    newProg.Time = 0;

                                if ((newProg.Event as ProgramChangeEvent).ProgramNumber != currProg)
                                {
                                    currProg = (newProg.Event as ProgramChangeEvent).ProgramNumber;
                                    newProgEvents.Add(newProg);
                                }
                            }
                        }
                        note.SaveChanges();
                        events.Objects.Remove(progEvents);
                        events.Objects.Add(newProgEvents);
                        events.SaveChanges();
                    }
                }
                TrackManipulations.SetChanNumber(originalChunk, TrackNum);
                TrackNum++;
            }
        }

        #endregion
    }
}
