using Sanford.Multimedia.Midi;
using System.Collections.Generic;
using System.Linq;

namespace BardMusicPlayer.Ui.MidiEdit.Managers
{
    public partial class MidiManager
    {
#region Primary Track Functions
        /// <summary>
        /// add a track
        /// </summary>
        internal void AddTrack()
        {
            sequence.Add(new Track());
        }

        /// <summary>
        /// Merge tracks
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        internal void MergeTracks(int source, int dest)
        {
            //Set the channel numbers
            Melanchall.DryWetMidi.Core.MidiFile midiFile = GetMelanchallMidiFile();
            midiFile = SetChanNumber(midiFile, source);
            midiFile = SetChanNumber(midiFile, dest);
            melanchallToSequencer(midiFile);

            //Do this with sanford, Melanchall is too slow
            Track src_track = ToIEnumerable(sequence.GetEnumerator()).ElementAt(source);
            Track dest_track = ToIEnumerable(sequence.GetEnumerator()).ElementAt(dest);

            //Remove all progchanges
            var flt = src_track.Iterator().AsParallel().Where(ev => ev.MidiMessage is ChannelMessage msg && msg.Command == ChannelCommand.ProgramChange);
            foreach (MidiEvent ev in flt)
                src_track.Remove(ev);

            flt = dest_track.Iterator().AsParallel().Where(ev => ev.MidiMessage is ChannelMessage msg && msg.Command == ChannelCommand.ProgramChange);
            foreach (MidiEvent ev in flt)
                dest_track.Remove(ev);


            sequence.MergeTracks(source, dest);

            dest_track = ToIEnumerable(sequence.GetEnumerator()).ElementAt(dest);

            int lastchannel = -1;
            foreach (MidiEvent ev in dest_track.Iterator())
            {
                if (ev.MidiMessage is ChannelMessage chanMsg)
                {
                    if (chanMsg.Command == ChannelCommand.NoteOn)
                    {
                        if (chanMsg.MidiChannel != lastchannel)
                        {
                            cmBuilder.Command = ChannelCommand.ProgramChange;
                            cmBuilder.Data1 = Quotidian.Structs.Instrument.Parse(GetTrackName(chanMsg.MidiChannel)).MidiProgramChangeCode;
                            cmBuilder.Data2 = 64;
                            cmBuilder.MidiChannel = chanMsg.MidiChannel;
                            cmBuilder.Build();
                            dest_track.Insert(ev.AbsoluteTicks, cmBuilder.Result);
                            lastchannel = chanMsg.MidiChannel;
                        }
                    }
                }
            };

            SetChanNumber(dest);
        }

        /// <summary>
        /// Remove the track[id]
        /// </summary>
        /// <param name="selectedTrack"></param>
        internal void RemoveTrack(int selectedTrack)
        {
            sequence.RemoveAt(selectedTrack);
        }
#endregion

        /// <summary>
        /// Set the Channel numbers for all tracks
        /// </summary>
        internal void AutoSetChanNumber()
        {
            Melanchall.DryWetMidi.Core.MidiFile midiFile = GetMelanchallMidiFile();
            midiFile = AutoSetChannelsForAllTracks(midiFile);
            melanchallToSequencer(midiFile);
        }

        /// <summary>
        /// Removes tracks without notes
        /// </summary>
        internal void RemoveEmptyTracks()
        {
            List<Track> empty = new List<Track>();

            foreach (Track track in ToIEnumerable(sequence.GetEnumerator()))
            {
                bool hasNotes = false;
                foreach (MidiEvent ev in track.Iterator())
                {
                    if (ev.MidiMessage is MetaMessage metaMsg)
                    {
                        if (metaMsg.MetaType == MetaType.TimeSignature)
                        {
                            hasNotes = true;
                            break;
                        }
                    }
                    if (ev.MidiMessage is ChannelMessage chanMsg)
                        if (chanMsg.Command == ChannelCommand.NoteOn)
                        {
                            hasNotes = true;
                            break;
                        }
                }
                if (!hasNotes)
                    empty.Add(track);
            }

            foreach (Track t in empty)
                sequence.Remove(t);
        }
    }
}
