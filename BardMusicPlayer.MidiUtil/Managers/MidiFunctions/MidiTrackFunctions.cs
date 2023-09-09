using BardMusicPlayer.Transmogrify.Song.Manipulation;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Linq;
using System.Threading.Tasks;
using Note = Melanchall.DryWetMidi.Interaction.Note;

namespace BardMusicPlayer.MidiUtil.Managers
{
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
                notesManager.Objects.Add(note);
            }
            return note;
        }

        internal void DeleteNote(TrackChunk track, Note note)
        {
            using (var events = track.ManageNotes())
            {
                var nn = events.Objects.Where(ev => ev.Time == note.Time && ev.NoteNumber == note.NoteNumber && ev.Length == ev.Length).FirstOrDefault();
                if (nn != null)
                {
                    events.Objects.Remove(nn);
                    events.SaveChanges();
                }
            }
        }

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
                manager.Objects.RemoveAll(e => e.Event.EventType == MidiEventType.UnknownMeta);
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

            var drumTracks = TrackManipulations.DrumMapping(track, openFileDialog.FileName);
            if (drumTracks.Count < 1)
                return;
            if (drumTracks.First().Value == null)
            {
                UiManager.Instance.ThrowError(drumTracks.First().Key);
                return;
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
            TrackManipulations.ClearProgChanges(track);
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
                TimedObjectsCollection<Note> notes = notesManager.Objects;
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
