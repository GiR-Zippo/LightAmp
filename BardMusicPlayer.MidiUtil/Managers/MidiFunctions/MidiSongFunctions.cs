using System;
using System.Collections.Generic;
using System.Linq;

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;

using BardMusicPlayer.MidiUtil.Utils;

namespace BardMusicPlayer.MidiUtil.Managers
{
    public partial class MidiManager
    {
        /// <summary>
        /// add a track
        /// </summary>
        public void AddTrack()
        {
            //sequence.Add(new Track());
        }

        /// <summary>
        /// Remove the <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        public void RemoveTrack(TrackChunk track)
        {
            currentSong.Chunks.Remove(track);
        }

        /// <summary>
        /// Sets the channel according to the first note in <see cref="TrackChunk"/>
        /// </summary>
        public void AutoSetChannelsForAllTracks()
        {
            foreach (TrackChunk tc in currentSong.GetTrackChunks())
            {
                int channel = GetChannelNumber(tc);
                SetChanNumber(tc, channel);
            }
        }

        /// <summary>
        /// Renumbers the channels for the song
        /// </summary>
        public void AutoRenumberChannels()
        {
            int idx = 0;
            foreach (TrackChunk tc in currentSong.GetTrackChunks())
            {
                SetChanNumber(tc, idx);
                if (idx == 15)
                    idx = 0;
                idx++;
            }
        }

        /// <summary>
        /// Merge Tracks via Melanchall
        /// Removes the sourceTrack
        /// </summary>
        /// <param name="sourceTrack"></param>
        /// <param name="destinationTrack"></param>
        public void MergeTracks(TrackChunk sourceTrack, TrackChunk destinationTrack)
        {
            int idx = currentSong.GetTrackChunks().IndexOf(destinationTrack);
            var newtrack = Melanchall.DryWetMidi.Core.TrackChunkUtilities.Merge(new List<TrackChunk> { sourceTrack, destinationTrack });
            currentSong.Chunks.Remove(sourceTrack);
            currentSong.Chunks.Remove(destinationTrack);
            currentSong.Chunks.Insert(idx, newtrack);
        }

        /// <summary>
        /// Merge tracks to the destination index
        /// </summary>
        /// <param name="sourceTrack"></param>
        /// <param name="destinationTrack"></param>
        /// <param name="destinationIndex"></param>
        public void MergeTracks(TrackChunk sourceTrack, TrackChunk destinationTrack, int destinationIndex)
        {
            Dictionary<int, int> channelIntrument = new Dictionary<int, int>();

            //Set the channel numbers
            int destChan = GetChannelNumber(destinationTrack);
            SetChanNumber(destinationTrack, destChan);

            int srcChan = GetChannelNumber(sourceTrack);
            if (destChan == srcChan)
            {
                if (destChan == 0)
                    srcChan = destChan + 1;
                if (destChan == 15)
                    srcChan = destChan - 1;
            }
            SetChanNumber(sourceTrack, srcChan);

            //Get channel instrument
            channelIntrument[GetChannelNumber(sourceTrack)] = GetInstrument(sourceTrack);
            channelIntrument[GetChannelNumber(destinationTrack)] = GetInstrument(destinationTrack);

            //Remove trackname from source
            using (var manager = sourceTrack.ManageTimedEvents())
            {
                manager.Events.RemoveAll(e => e.Event.EventType == MidiEventType.SequenceTrackName);
            }

            //Merge
            var newtrack = Melanchall.DryWetMidi.Core.TrackChunkUtilities.Merge(new List<TrackChunk> { sourceTrack, destinationTrack });

            //Clear progs
            ClearProgChanges(newtrack);

            //set new prog events
            using (var events = newtrack.ManageTimedEvents())
            {
                int lastchannel = -1;
                foreach (var mevent in events.Events)
                {
                    if (mevent.Event.EventType == MidiEventType.NoteOn)
                    {
                        NoteOnEvent no = mevent.Event as NoteOnEvent;
                        if (no.Channel != lastchannel)
                        {
                            var t = mevent.Time;
                            ProgramChangeEvent pc = new ProgramChangeEvent((SevenBitNumber)channelIntrument[no.Channel]);
                            pc.Channel = (FourBitNumber)destChan;
                            events.Events.AddEvent(pc, t);
                            lastchannel = no.Channel;
                        }
                    }
                }
            }

            SetInstrument(newtrack, (SevenBitNumber)channelIntrument[destChan]);

            //and finish it
            currentSong.Chunks.Remove(sourceTrack);
            currentSong.Chunks.Remove(destinationTrack);
            if (destinationIndex > currentSong.GetTrackChunks().Count())
                currentSong.Chunks.Add(newtrack);
            else
                currentSong.Chunks.Insert(destinationIndex, newtrack);
            destChan = GetChannelNumber(newtrack);
            SetChanNumber(newtrack, destChan);
        }

        /// <summary>
        /// Removes tracks without <see cref="Note"/> and <see cref="TimeSignature"/>
        /// </summary>
        public void RemoveEmptyTracks()
        {
            List<TrackChunk> remove = new List<TrackChunk>();
            foreach (TrackChunk track in currentSong.GetTrackChunks())
            {
                bool empty = true;
                if (track.ManageNotes().Notes.Any())
                    empty = false;

                var ev = track.Events.Where(e => e.EventType == MidiEventType.TimeSignature).FirstOrDefault();
                if (ev != null)
                    empty = false;
                
                ev = track.Events.Where(e => e.EventType == MidiEventType.SetTempo).FirstOrDefault();
                if (ev != null)
                    empty = false;

                if (empty)
                    remove.Add(track);
            }
            foreach(var t in remove)
                currentSong.Chunks.Remove(t);
        }
    }
}
