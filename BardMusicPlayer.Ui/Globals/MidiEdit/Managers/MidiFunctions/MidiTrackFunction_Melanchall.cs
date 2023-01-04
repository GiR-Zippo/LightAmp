using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using BardMusicPlayer.Quotidian;

namespace BardMusicPlayer.Ui.MidiEdit.Managers
{
    public class DMaps
    {
        public int MidiNote { get; set; } = 0;
        public string Instrument { get; set; } = "None";
        public int GameNote { get; set; } = 0;
    }

    public partial class MidiManager
    {
        #region public accessor
        /// <summary>
        /// accesor for the drum mapper
        /// </summary>
        /// <param name="selectedTrack"></param>
        public void Drummapping(int selectedTrack)
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

            var data = memoryStream.ToArray();
            List<DMaps> drumlist = JsonConvert.DeserializeObject<List<DMaps>>(new UTF8Encoding(true).GetString(data));
            memoryStream.Close();
            memoryStream.Dispose();

            MidiFile midiFile = GetMelanchallMidiFile();
            midiFile = Drummapping(midiFile, selectedTrack, drumlist);
            melanchallToSequencer(midiFile);
        }

        /// <summary>
        /// Transpose a track x halftones
        /// </summary>
        /// <param name="selectedTrack"></param>
        /// <param name="halftones"></param>
        public void Transpose(int selectedTrack, int halftones)
        {
            MidiFile midiFile = GetMelanchallMidiFile();
            midiFile = Transpose(midiFile, selectedTrack, halftones);
            melanchallToSequencer(midiFile);
        }
        #endregion


        #region private MelanchallFunctions
        private MidiFile AutoSetChannelsForAllTracks(MidiFile midiFile)
        {
            int idx = 0;
            foreach (TrackChunk tc in midiFile.GetTrackChunks())
            {
                using (var notesManager = tc.ManageNotes())
                {
                    Parallel.ForEach(notesManager.Notes, note =>
                    {
                        note.Channel = (FourBitNumber)idx;
                    });
                }

                using (var manager = tc.ManageTimedEvents())
                {
                    Parallel.ForEach(manager.Events, midiEvent =>
                    {
                        if (midiEvent.Event is ProgramChangeEvent pe)
                            pe.Channel = (FourBitNumber)idx;
                        if (midiEvent.Event is ControlChangeEvent ce)
                            ce.Channel = (FourBitNumber)idx;
                        if (midiEvent.Event is PitchBendEvent pbe)
                            pbe.Channel = (FourBitNumber)idx;
                    });
                }
                idx++;
            }
            return midiFile;
        }

        /// <summary>
        /// The drum mapper
        /// </summary>
        /// <param name="midiFile">MidiFile</param>
        /// <param name="trackNumber">drum track</param>
        /// <param name="drumlist">drum list</param>
        /// <returns>MidiFile</returns>
        private MidiFile Drummapping(MidiFile midiFile, int trackNumber, List<DMaps> drumlist)
        {
            TrackChunk tc = midiFile.GetTrackChunks().ElementAt(trackNumber);

            Dictionary<string, TrackChunk> drumTracks = new Dictionary<string, TrackChunk>();
            foreach (Note note in tc.GetNotes())
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
                midiFile.Chunks.Add(nt.Value);

            return midiFile;
        }

        /// <summary>
        /// Remove all prog changes
        /// </summary>
        /// <param name="midiFile"></param>
        /// <param name="trackNumber"></param>
        /// <returns>MidiFile</returns>
        private MidiFile ClearProgChanges(MidiFile midiFile, int trackNumber)
        {
            Melanchall.DryWetMidi.Core.TrackChunk tc = midiFile.GetTrackChunks().ElementAt(trackNumber);
            using (var manager = tc.ManageTimedEvents())
            {
                manager.Events.RemoveAll(e => e.Event.EventType == MidiEventType.ProgramChange);
                manager.Events.RemoveAll(e => e.Event.EventType == MidiEventType.ProgramName);
            }
            return midiFile;
        }

        /// <summary>
        /// Merge Tracks vis Melanchall
        /// </summary>
        /// <param name="midiFile"></param>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        /// <returns>MidiFile</returns>
        private MidiFile MergeTracks(MidiFile midiFile, int source, int dest)
        {
            TrackChunk src_tc = midiFile.GetTrackChunks().ElementAt(source);
            TrackChunk dest_tc = midiFile.GetTrackChunks().ElementAt(dest);

            dest_tc = Melanchall.DryWetMidi.Core.TrackChunkUtilities.Merge(new List<TrackChunk> { src_tc, dest_tc });
            midiFile.Chunks.Remove(src_tc);
            midiFile.Chunks.Remove(dest_tc);
            midiFile.Chunks.Insert(dest, dest_tc);
            return midiFile;
        }

        /// <summary>
        /// Sets the channel number for a track
        /// </summary>
        /// <param name="midiFile"></param>
        /// <param name="trackNumber"></param>
        /// <returns>MidiFile</returns>
        private MidiFile SetChanNumber(MidiFile midiFile, int trackNumber)
        {
            Melanchall.DryWetMidi.Core.TrackChunk tc = midiFile.GetTrackChunks().ElementAt(trackNumber);

            using (var notesManager = tc.ManageNotes())
            {
                Parallel.ForEach(notesManager.Notes, note =>
                {
                    note.Channel = (Melanchall.DryWetMidi.Common.FourBitNumber)trackNumber;
                });
            }

            using (var manager = tc.ManageTimedEvents())
            {
                Parallel.ForEach(manager.Events, midiEvent =>
                {
                    if (midiEvent.Event is ProgramChangeEvent pe)
                        pe.Channel = (Melanchall.DryWetMidi.Common.FourBitNumber)trackNumber;
                    if (midiEvent.Event is ControlChangeEvent ce)
                        ce.Channel = (Melanchall.DryWetMidi.Common.FourBitNumber)trackNumber;
                    if (midiEvent.Event is PitchBendEvent pbe)
                        pbe.Channel = (Melanchall.DryWetMidi.Common.FourBitNumber)trackNumber;
                });
            }
            return midiFile;
        }

        /// <summary>
        /// Transpose a track with x halftones
        /// </summary>
        /// <param name="midiFile"></param>
        /// <param name="trackNumber"></param>
        /// <param name="halftones"></param>
        /// <returns>MidiFile</returns>
        private MidiFile Transpose(MidiFile midiFile, int trackNumber, int halftones)
        {
            TrackChunk tc = midiFile.GetTrackChunks().ElementAt(trackNumber);
            using (var notesManager = tc.ManageNotes())
            {
                NotesCollection notes = notesManager.Notes;
                Parallel.ForEach(notes, note =>
                {
                    note.NoteNumber = (SevenBitNumber)(note.NoteNumber + halftones);
                });
            }
            return midiFile;
        }
        #endregion

        #region private Helpers
        private MidiFile GetMelanchallMidiFile()
        {
            MemoryStream stream = GetMidiStreamFromSanford();
            var midiFile = MidiFile.Read(stream);
            stream.Close();
            stream.Dispose();
            return midiFile;
        }

        private void melanchallToSequencer(MidiFile midiFile)
        {
            MemoryStream stream = new MemoryStream();
            midiFile.Write(stream, MidiFileFormat.MultiTrack, new WritingSettings { TextEncoding = Encoding.ASCII });

            stream.Rewind();
            OpenFile(stream);
            stream.Close();
            stream.Dispose();
        }
        #endregion
    }
}

