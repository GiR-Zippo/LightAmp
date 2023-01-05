using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.MusicTheory;
using Newtonsoft.Json;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Note = Melanchall.DryWetMidi.Interaction.Note;

namespace BardMusicPlayer.MidiUtil.Managers
{
    public class DrumMaps
    {
        public int MidiNote { get; set; } = 0;
        public string Instrument { get; set; } = "None";
        public int GameNote { get; set; } = 0;
    }

    public partial class MidiManager
    {
        internal Note CreateNote(int channel, int noteIndex, TrackChunk track, double start, double end, int velocity)
        {
            Note note = null;
            using (var notesManager = track.ManageNotes())
            {
                note = new Note(
                    (SevenBitNumber)noteIndex,
                    LengthConverter.ConvertFrom(
                        new MetricTimeSpan(hours: 0, minutes: 0, seconds: 0, milliseconds: (int)end-(int)start),
                        0,
                        currentSong.GetTempoMap()),
                    LengthConverter.ConvertFrom(
                        new MetricTimeSpan(hours: 0, minutes: 0, seconds: 0, milliseconds: (int)start),
                        (long)(end-start),
                        currentSong.GetTempoMap()));
                notesManager.Notes.Add(note);
            }
            return note;
        }

        internal void DeleteNote(TrackChunk track, Note note)
        {
            using (var events = track.ManageNotes())
            {
                var nn = events.Notes.Where(ev => ev.Time == note.Time && ev.NoteNumber == note.NoteNumber && ev.Length == ev.Length).FirstOrDefault();
                if (nn != null)
                {
                    events.Notes.Remove(nn);
                    events.SaveChanges();
                }
            }
        }

        #region Get/Set TrackName

        /// <summary>
        /// Get the name of the <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track">TrackChunk</param>
        /// <returns></returns>
        internal string GetTrackName(TrackChunk track)
        {
            var trackName = track.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text;
            if (trackName != null)
                return trackName;
            return "No Name";
        }

        /// <summary>
        /// Sets the <see cref="TrackChunk"/> name
        /// </summary>
        /// <param name="track"></param>
        /// <param name="TrackName"></param>
        internal void SetTrackName(TrackChunk track, string TrackName)
        {
            using (var events = track.ManageTimedEvents())
            {
                var fev = events.Events.Where(e => e.Event.EventType == MidiEventType.SequenceTrackName).FirstOrDefault();
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

        #region Get/Set Init Instrument

        /// <summary>
        /// Get the program number of the <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <returns>The <see cref="int"/> representation of the instrument</returns>
        internal int GetInstrument(TrackChunk track)
        {
            var ev = track.Events.Where(e => e.EventType == MidiEventType.ProgramChange).FirstOrDefault();
            if (ev != null)
                return (ev as ProgramChangeEvent).ProgramNumber;
            return 1; //return a "None" instrument cuz we don't have all midi instrument in XIV
        }

        /// <summary>
        /// Create or overwrite the first progchange in <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <param name="instrument"></param>
        internal void SetInstrument(TrackChunk track, int instrument)
        {
            int channel = GetChannelNumber(track);
            if (channel == -1)
                return;

            using (var events = track.ManageTimedEvents())
            {
                var ev = events.Events.Where(e => e.Event.EventType == MidiEventType.ProgramChange).FirstOrDefault();
                if (ev != null)
                {
                    var prog = ev.Event as ProgramChangeEvent;
                    prog.ProgramNumber = (SevenBitNumber)instrument;
                }
                else
                {
                    var pe = new ProgramChangeEvent((SevenBitNumber)instrument);
                    pe.Channel = (FourBitNumber)channel;
                    events.Events.Add(new TimedEvent(pe, 0));
                }
                events.SaveChanges();
            }
        }

        #endregion

        #region Get/Set Channel

        /// <summary>
        /// Get channel number by first note on
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        internal int GetChannelNumber(TrackChunk track)
        {
            var ev = track.Events.OfType<NoteOnEvent>().FirstOrDefault();
            if (ev != null)
                return ev.Channel;
            return -1;
        }

        /// <summary>
        /// Sets the channel number for a track
        /// </summary>
        /// <param name="track"></param>
        /// <param name="trackNumber"></param>
        /// <returns>MidiFile</returns>
        private void SetChanNumber(TrackChunk track, int channelNumber)
        {
            if (channelNumber <= 0)
                return;

            using (var notesManager = track.ManageNotes())
            {
                Parallel.ForEach(notesManager.Notes, note =>
                {
                    note.Channel = (FourBitNumber)channelNumber;
                });
                notesManager.SaveChanges();
            }

            using (var manager = track.ManageTimedEvents())
            {
                Parallel.ForEach(manager.Events, midiEvent =>
                {
                    if (midiEvent.Event is ProgramChangeEvent pe)
                        pe.Channel = (FourBitNumber)channelNumber;
                    if (midiEvent.Event is ControlChangeEvent ce)
                        ce.Channel = (FourBitNumber)channelNumber;
                    if (midiEvent.Event is PitchBendEvent pbe)
                        pbe.Channel = (FourBitNumber)channelNumber;
                });
                manager.SaveChanges();
            }
        }

        #endregion

        #region Misc

        /// <summary>
        /// Remove all bank switches from <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        private void ClearUnkMeta(TrackChunk track)
        {
            using (var manager = track.ManageTimedEvents())
            {
                manager.Events.RemoveAll(e => e.Event.EventType == MidiEventType.UnknownMeta);
                manager.SaveChanges();
            }
        }

        /// <summary>
        /// Remove all prog changes from <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        /// <returns></returns>
        private void ClearProgChanges(TrackChunk track)
        {
            using (var manager = track.ManageTimedEvents())
            {
                manager.Events.RemoveAll(e => e.Event.EventType == MidiEventType.ProgramChange);
                manager.Events.RemoveAll(e => e.Event.EventType == MidiEventType.ProgramName);
                manager.SaveChanges();
            }
        }

        /// <summary>
        /// Split drums in <see cref="TrackChunk"/> into new <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        public void Drummapping(TrackChunk track)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Drum map | *.json",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            MemoryStream memoryStream = new MemoryStream();
            FileStream fileStream = File.Open(openFileDialog.FileName, FileMode.Open);
            fileStream.CopyTo(memoryStream);
            fileStream.Close();

            List<DrumMaps> drumlist = null;
            var data = memoryStream.ToArray();
            try
            {
                drumlist = JsonConvert.DeserializeObject<List<DrumMaps>>(new UTF8Encoding(true).GetString(data));
            }
            catch
            {
                UiManager.Instance.ThrowError("Malformed drum map!");
                return;
            }
            memoryStream.Close();
            memoryStream.Dispose();

            if (drumlist == null)
            {
                UiManager.Instance.ThrowError("Drum map is empty!");
                return;
            }

            //And do it
            Dictionary<string, TrackChunk> drumTracks = new Dictionary<string, TrackChunk>();
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
                        NotesCollection notes = notesManager.Notes;
                        note.NoteNumber = (SevenBitNumber)drum.GameNote;
                        notes.Add(note);
                    }
                }
                else
                {
                    using (var notesManager = drumTracks[drum.Instrument].ManageNotes())
                    {
                        NotesCollection notes = notesManager.Notes;
                        note.NoteNumber = (SevenBitNumber)drum.GameNote;
                        notes.Add(note);
                    }
                }
            }
            foreach (var nt in drumTracks)
                currentSong.Chunks.Add(nt.Value);
        }

        /// <summary>
        /// Remove all progchanges from <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        internal void RemoveAllEventsFromTrack(TrackChunk track)
        {
            ClearProgChanges(track);
            ClearUnkMeta(track);
        }

        /// <summary>
        /// Transpose all notes in <see cref="TrackChunk"/> with x halftones
        /// </summary>
        /// <param name="track"></param>
        /// <param name="halftones"></param>
        /// <returns></returns>
        public void Transpose(TrackChunk track, int halftones)
        {
            using (var notesManager = track.ManageNotes())
            {
                NotesCollection notes = notesManager.Notes;
                Parallel.ForEach(notes, note =>
                {
                    note.NoteNumber = (SevenBitNumber)(note.NoteNumber + halftones);
                });
                notesManager.SaveChanges();
            }
        }

        #endregion
    }
}
