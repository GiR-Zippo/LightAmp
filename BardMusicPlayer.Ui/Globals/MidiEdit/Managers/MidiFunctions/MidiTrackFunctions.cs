using Sanford.Multimedia.Midi;
using System.Linq;

namespace BardMusicPlayer.Ui.MidiEdit.Managers
{
    public partial class MidiManager
    {
        /// <summary>
        /// Remove all progchanges
        /// </summary>
        /// <param name="selectedTrack"></param>
        internal void RemoveAllEventsFromTrack(int selectedTrack)
        {
            var t = ToIEnumerable(sequence.GetEnumerator());
            Track track = t.ElementAt(selectedTrack);
            var flt = track.Iterator().AsParallel().Where(ev => ev.MidiMessage.MessageType is MessageType.Channel);
            foreach (MidiEvent ev in flt)
            {
                if (ev.MidiMessage is ChannelMessage metaMsg)
                {
                    if (metaMsg.Command == ChannelCommand.ProgramChange)
                    {
                        track.Remove(ev);
                    }
                }
            }
        }

        /// <summary>
        /// Get the name of the track
        /// </summary>
        /// <param name="selectedTrack"></param>
        /// <returns></returns>
        internal int GetChannelNumber(int selectedTrack)
        {
            var t = ToIEnumerable(sequence.GetEnumerator());
            Track track = t.ElementAt(selectedTrack);

            var x = track.Iterator().FirstOrDefault(ev => ev.MidiMessage is ChannelMessage msg && msg.Command == ChannelCommand.NoteOn);
            if (x != null)
            {
                var prog = x.MidiMessage as ChannelMessage;
                return prog.MidiChannel;
            }
            return -1; //return a "None" instrument cuz we don't have all midi instrument in XIV
        }

        /// <summary>
        /// Set the Channel number
        /// </summary>
        internal void SetChanNumber(int track)
        {
            Melanchall.DryWetMidi.Core.MidiFile midiFile = GetMelanchallMidiFile();
            midiFile = SetChanNumber(midiFile, track);
            melanchallToSequencer(midiFile);
        }


        #region Get/Set Init Instrument
        /// <summary>
        /// Get the name of the track
        /// </summary>
        /// <param name="selectedTrack"></param>
        /// <returns></returns>
        internal int GetInstrument(int selectedTrack)
        {
            var t = ToIEnumerable(sequence.GetEnumerator());
            Track track = t.ElementAt(selectedTrack);

            var x = track.Iterator().FirstOrDefault(ev => ev.MidiMessage is ChannelMessage msg && msg.Command == ChannelCommand.ProgramChange);
            if (x != null)
            {
                var prog = x.MidiMessage as ChannelMessage;
                return prog.Data1;
            }
            return 1; //return a "None" instrument cuz we don't have all midi instrument in XIV
        }

        /// <summary>
        /// Create or overwrite the first progchange and set track name
        /// </summary>
        /// <param name="selectedTrack"></param>
        /// <param name="instrument"></param>
        internal void SetInstrument(int selectedTrack, int instrument)
        {
            var t = ToIEnumerable(sequence.GetEnumerator());
            Track track = t.ElementAt(selectedTrack);

            cmBuilder.Command = ChannelCommand.ProgramChange;
            cmBuilder.Data1 = instrument;
            cmBuilder.Data2 = 64;
            cmBuilder.MidiChannel = selectedTrack;
            cmBuilder.Build();

            var x = track.Iterator().FirstOrDefault(ev => ev.MidiMessage is ChannelMessage msg && msg.Command == ChannelCommand.ProgramChange);
            if (x != null)
            {
                track.Remove(x);
                track.Insert(x.AbsoluteTicks, cmBuilder.Result);
            }
            else
                track.Insert(0, cmBuilder.Result);
        }
        #endregion

        #region Get/Set TrackName
        /// <summary>
        /// Get the name of the track
        /// </summary>
        /// <param name="selectedTrack"></param>
        /// <returns></returns>
        internal string GetTrackName(int selectedTrack)
        {
            var t = ToIEnumerable(sequence.GetEnumerator());
            Track track = t.ElementAt(selectedTrack);

            var x = track.Iterator().FirstOrDefault(ev => ev.MidiMessage is MetaMessage msg && msg.MetaType == MetaType.TrackName);
            if (x != null)
            {
                var mm = x.MidiMessage as MetaMessage;
                MetaTextBuilder builder = new MetaTextBuilder(mm);
                return builder.Text;
            }
            return "No Name";
        }

        /// <summary>
        /// Sets the track name
        /// </summary>
        /// <param name="selectedTrack"></param>
        /// <param name="TrackName"></param>
        internal void SetTrackName(int selectedTrack, string TrackName)
        {
            var t = ToIEnumerable(sequence.GetEnumerator());
            Track track = t.ElementAt(selectedTrack);

            var x = track.Iterator().FirstOrDefault(ev => ev.MidiMessage is MetaMessage msg && msg.MetaType == MetaType.TrackName);
            if (x != null)
            {
                track.Remove(x);
                var mm = x.MidiMessage as MetaMessage;
                MetaTextBuilder builder = new MetaTextBuilder(mm);
                builder.Text = TrackName;
                builder.Type = MetaType.TrackName;
                builder.Build();
                track.Insert(x.AbsoluteTicks, builder.Result);
            }
        }
        #endregion

    }
}
