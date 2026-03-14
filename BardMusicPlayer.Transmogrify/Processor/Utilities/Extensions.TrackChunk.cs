/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Quotidian.Structs;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BardMusicPlayer.Transmogrify.Processor.Utilities
{
    internal static partial class Extensions
    {
        /// <summary>
        /// Creates a NoteDictionary from the <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="originalChunk"></param>
        /// <param name="tempoMap"></param>
        /// <param name="firstNoteus"></param>
        /// <param name="noteVelocity"></param>
        /// <returns></returns>
        internal static Task<Dictionary<int, Dictionary<long, Note>>> GetNoteDictionary(TrackChunk originalChunk, TempoMap tempoMap, long firstNoteus, int noteVelocity)
        {
            tempoMap = tempoMap.Clone();
            Dictionary<int, Dictionary<long, Note>> notesDictionary = new Dictionary<int, Dictionary<long, Note>>();
            for (int i = 0; i < 128; i++)
                notesDictionary.Add(i, new Dictionary<long, Note>());

            foreach (Note note in originalChunk.GetNotes())
            {
                long noteOnMS;
                long noteOffMS;

                try
                {
                    noteOnMS = 5000000 + (note.GetTimedNoteOnEvent().TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds) - firstNoteus;
                    noteOffMS = 5000000 + (note.GetTimedNoteOffEvent().TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds) - firstNoteus;
                }
                catch (Exception)
                { continue; }

                int noteNumber = note.NoteNumber;

                Note newNote = new Note((SevenBitNumber)noteNumber,
                                        time: noteOnMS / 1000,
                                        length: (noteOffMS / 1000) - (noteOnMS / 1000)
                                        )
                {
                    Channel = (FourBitNumber)0,
                    Velocity = (SevenBitNumber)noteVelocity,
                    OffVelocity = (SevenBitNumber)noteVelocity
                };

                if (notesDictionary[noteNumber].ContainsKey(noteOnMS))
                {
                    Note previousNote = notesDictionary[noteNumber][noteOnMS];
                    if (previousNote.Length < note.Length)
                        notesDictionary[noteNumber][noteOnMS] = newNote;
                }
                else
                    notesDictionary[noteNumber].Add(noteOnMS, newNote);
            }

            return Task.FromResult(notesDictionary);
        }

        /// <summary>
        /// Add the program change events to <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="originalChunk"></param>
        /// <param name="tempoMap"></param>
        /// <param name="firstNote"></param>
        /// <returns><see cref="Task{TResult}"/> is <see cref="TrackChunk"/></returns>
        internal static Task<TrackChunk> AddProgramChangeEvents(TrackChunk originalChunk, TempoMap tempoMap, long firstNote)
        {
            TrackChunk newChunk = new TrackChunk();
            IEnumerable<TimedEvent> events = originalChunk.ManageTimedEvents().Objects.Where(e => e.Event.EventType == MidiEventType.ProgramChange);
            foreach (TimedEvent timedEvent in events)
            {
                if (timedEvent.Event is not ProgramChangeEvent programChangeEvent)
                    continue;

                //Skip all except guitar | implement if we need this again
                if ((programChangeEvent.ProgramNumber < 27) || (programChangeEvent.ProgramNumber > 31))
                    continue;

                var channel = programChangeEvent.Channel;
                using (var manager = new TimedObjectsManager(newChunk.Events, ObjectType.TimedEvent | ObjectType.Note))
                {
                    TimedObjectsCollection<ITimedObject> timedEvents = manager.Objects;
                    if ((5000 + (timedEvent.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000) - firstNote) < 0)
                        timedEvents.Add(new TimedEvent(new ProgramChangeEvent(programChangeEvent.ProgramNumber), 5000));
                    else
                        timedEvents.Add(new TimedEvent(new ProgramChangeEvent(programChangeEvent.ProgramNumber), 5000 + (timedEvent.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000) - firstNote/* Absolute time too */));
                }
            }
            return Task.FromResult(newChunk);
        }

        /// <summary>
        /// Add the lyric events to <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="originalChunk"></param>
        /// <param name="tempoMap"></param>
        /// <param name="firstNote"></param>
        /// <returns><see cref="Task{TResult}"/> is <see cref="TrackChunk"/></returns>
        internal static Task<TrackChunk> AddLyricsEvents(TrackChunk originalChunk, TempoMap tempoMap, long firstNote)
        {
            TrackChunk newChunk = new TrackChunk();
            IEnumerable<TimedEvent> events = originalChunk.ManageTimedEvents().Objects.Where(e => e.Event.EventType == MidiEventType.Lyric);
            foreach (var timedEvent in events)
            {
                if (timedEvent.Event is not LyricEvent lyricsEvent)
                    continue;

                using (var manager = new TimedObjectsManager(newChunk.Events, ObjectType.TimedEvent))
                {
                    TimedObjectsCollection<ITimedObject> timedEvents = manager.Objects;
                    if ((5000 + (timedEvent.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000) - firstNote) < 5000)
                        timedEvents.Add(new TimedEvent(new LyricEvent(lyricsEvent.Text), 5000));
                    else
                        timedEvents.Add(new TimedEvent(new LyricEvent(lyricsEvent.Text), 5000 + (timedEvent.TimeAs<MetricTimeSpan>(tempoMap).TotalMicroseconds / 1000) - firstNote));
                }
            }
            return Task.FromResult(newChunk);
        }

        /// <summary>
        /// Realigns the track events in <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="originalChunk"></param>
        /// <param name="delta"></param>
        /// <returns><see cref="Task{TResult}"/> is <see cref="TrackChunk"/></returns>
        internal static Task<TrackChunk> RealignTrackEvents(TrackChunk originalChunk, long delta)
        {
            //realign the progchanges
            originalChunk = RealignProgramChanges(originalChunk).Result;

            int offset = Instrument.Parse(originalChunk.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text).SampleOffset; //get the offset
            using (var manager = originalChunk.ManageTimedEvents())
            {
                foreach (TimedEvent _event in manager.Objects)
                {
                    var noteEvent = _event.Event as NoteEvent;
                    var programChangeEvent = _event.Event as ProgramChangeEvent;
                    var lyricsEvent = _event.Event as LyricEvent;
                    long newStart = _event.Time + offset - delta;

                    //Note alignment
                    if (noteEvent != null)
                        _event.Time = newStart;

                    //lyrics
                    if (lyricsEvent != null)
                    {
                        if (newStart <= -1)
                            manager.Objects.Remove(_event);
                        else
                            _event.Time = newStart;
                    }

                    //Prog alignment
                    if (programChangeEvent != null)
                    {
                        if (newStart <= -1)
                            manager.Objects.Remove(_event);
                        else
                            _event.Time = newStart;

                        //if theres a new offset, use this one
                        if ((programChangeEvent.ProgramNumber >= 27) && (programChangeEvent.ProgramNumber <= 31))
                            offset = Instrument.ParseByProgramChange(programChangeEvent.ProgramNumber).SampleOffset;
                    }
                }


                /*foreach (TimedEvent _event in manager.Events)
                {
                    var programChangeEvent = _event.Event as ChannelAftertouchEvent;
                    if (programChangeEvent == null)
                        continue;

                    long newStart = _event.Time - delta;
                    if (newStart <= -1)
                        manager.Events.Remove(_event);
                    else
                        _event.Time = newStart;
                }*/

            }
            return Task.FromResult(originalChunk);
        }

        /// <summary>
        /// Realigns the track events in <see cref="TrackChunk"/> by note offset
        /// </summary>
        /// <param name="originalChunk"></param>
        /// <param name="delta"></param>
        /// <returns><see cref="Task{TResult}"/> is <see cref="TrackChunk"/></returns>
        internal static Task<TrackChunk> RealignTrackEventsByNoteOffset(TrackChunk originalChunk, long delta)
        {
            //realign the progchanges
            originalChunk = RealignProgramChanges(originalChunk).Result;

            bool mb2Compat = BmpPigeonhole.Instance.MidiBardCompatMode;
            bool toadEnabled = BmpPigeonhole.Instance.UsePluginForKeyOutput;
            Instrument instrument = Instrument.Parse(originalChunk.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text);
            int lastNoteNumber = -1;

            using (var manager = originalChunk.ManageTimedEvents())
            {
                foreach (TimedEvent _event in manager.Objects)
                {
                    var noteEvent = _event.Event as NoteEvent;
                    var programChangeEvent = _event.Event as ProgramChangeEvent;
                    var lyricsEvent = _event.Event as LyricEvent;

                    //Note alignment
                    if (noteEvent != null)
                    {
                        _event.Time = _event.Time + instrument.NoteSampleOffsetOrDefault(noteEvent.NoteNumber, mb2Compat, toadEnabled) - delta;
                        lastNoteNumber = noteEvent.NoteNumber;
                    }
                    //lyrics
                    if (lyricsEvent != null)
                    {
                        if (lastNoteNumber == -1)
                        {
                            if (_event.Time + 10 - delta <= -0)
                                manager.Objects.Remove(_event);
                            else
                                _event.Time = _event.Time + 10 - delta;
                        }
                        else
                            _event.Time = _event.Time + instrument.NoteSampleOffsetOrDefault(lastNoteNumber, mb2Compat) - delta;
                    }
                    //Prog alignment
                    if (programChangeEvent != null)
                    {
                        if (lastNoteNumber == -1)
                        {
                            if (_event.Time + 10 - delta <= -0)
                                manager.Objects.Remove(_event);
                            else
                                _event.Time = _event.Time + 10 - delta;
                        }
                        else
                            _event.Time = _event.Time + instrument.NoteSampleOffsetOrDefault(lastNoteNumber, mb2Compat, toadEnabled) - delta;

                        //if theres a new offset, use this one
                        if ((programChangeEvent.ProgramNumber >= 27) && (programChangeEvent.ProgramNumber <= 31))
                            instrument = Instrument.ParseByProgramChange(programChangeEvent.ProgramNumber);
                    }
                }


                /*foreach (TimedEvent _event in manager.Events)
                {
                    var programChangeEvent = _event.Event as ChannelAftertouchEvent;
                    if (programChangeEvent == null)
                        continue;

                    long newStart = _event.Time - delta;
                    if (newStart <= -1)
                        manager.Events.Remove(_event);
                    else
                        _event.Time = newStart;
                }*/

            }
            return Task.FromResult(originalChunk);
        }

        /// <summary>
        /// Realigns the programchange events in <see cref="TrackChunk"/> to compensate the game input latency
        /// </summary>
        /// <param name="originalChunk"></param>
        /// <param name="delta"></param>
        /// <returns><see cref="Task{TResult}"/> is <see cref="TrackChunk"/></returns>
        internal static Task<TrackChunk> RealignProgramChanges(TrackChunk originalChunk)
        {
            //check progs
            using (var manager = originalChunk.ManageTimedEvents())
            {
                foreach (TimedEvent _event in manager.Objects)
                {
                    if (_event.Event.EventType != MidiEventType.ProgramChange)
                        continue;

                    if (_event.Time <= 5000)
                        continue;

                    var fEvent = manager.Objects.FirstOrDefault(n =>
                                                                _event.Time - n.Time > -20 && _event.Time - n.Time <= 0 &&
                                                                n.Event.EventType == MidiEventType.NoteOn);
                    if (fEvent != null)
                    {
                        long d = _event.Time - fEvent.Time;
                        if (d < 20)
                            _event.Time -= 20 + d;
                    }
                }
            }
            return Task.FromResult(originalChunk);
        }
    }
}
