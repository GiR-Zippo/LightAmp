using BardMusicPlayer.Transmogrify.Song.Manipulation;
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
