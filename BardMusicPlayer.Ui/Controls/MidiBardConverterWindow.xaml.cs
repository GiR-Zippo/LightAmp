/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Siren;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Transmogrify.Song.Importers;
using BardMusicPlayer.Transmogrify.Song.Manipulation;
using BardMusicPlayer.Ui.Functions;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace BardMusicPlayer.Ui.Controls
{
    public class MidiBardConverter_InstrumentHelper
    {
        public MidiBardConverter_InstrumentHelper() { }

        public static Dictionary<int, string> Instruments()
        {
            Dictionary<int, string> instrumentList = new Dictionary<int, string>();
            foreach (var instr in Instrument.All)
                instrumentList.Add(instr.Index, instr.Name);
            return instrumentList;
        }
    }

    /// <summary>
    /// Interaktionslogik für MidiBardConverterWindow.xaml
    /// </summary>
    public partial class MidiBardConverterWindow : Window
    {
        List<MidiBardImporter.MidiTrack> _tracks = null;
        string _midiName { get; set; } = "Unknown";
        MidiFile _midifile { get; set; } = null;
        bool _AlignMidiToFirstNote { get; set; } = false;
        object _Sender { get; set; } = null;

        public MidiBardConverterWindow()
        {
            _tracks = new List<MidiBardImporter.MidiTrack>();
            InitializeComponent();
            AlignToFirstNote_CheckBox.IsChecked = _AlignMidiToFirstNote;
        }

        public MidiBardConverterWindow(string filename)
        {
            _tracks = new List<MidiBardImporter.MidiTrack>();
            InitializeComponent();
            ReadMidi(filename);
        }

        void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_tracks.Count() > 0)
                _tracks.Clear();
        }

        private void ReadMidi(string filename)
        {
            _midiName = Path.GetFileNameWithoutExtension(filename);
            if (File.Exists(Path.ChangeExtension(filename, "json")))
                ReadWithConfig(filename);
            else
                ReadWithoutConfig(filename);
        }

        /// <summary>
        /// Called when there is a config
        /// </summary>
        /// <param name="filename"></param>
        private void ReadWithConfig(string filename)
        {
            MemoryStream memoryStream = new MemoryStream();
            FileStream fileStream = File.Open(Path.ChangeExtension(filename, "json"), FileMode.Open);
            fileStream.CopyTo(memoryStream);
            fileStream.Close();

            var data = memoryStream.ToArray();
            MidiBardImporter.MidiFileConfig pdatalist = JsonConvert.DeserializeObject<MidiBardImporter.MidiFileConfig>(new UTF8Encoding(true).GetString(data));
            GuitarModeSelector.SelectedIndex = pdatalist.ToneMode;

            //Read the midi
            _midifile = MidiFile.Read(filename);

            //create the dict for the cids to tracks
            Dictionary<int, int> cids = new Dictionary<int, int>();
            int idx = 0;
            int cid_count = 1;
            foreach (TrackChunk chunk in _midifile.GetTrackChunks())
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

                MidiBardImporter.MidiTrack midiTrack = new MidiBardImporter.MidiTrack();
                midiTrack.Index = pdatalist.Tracks[idx].Index;
                midiTrack.TrackNumber = cid;
                midiTrack.trackInstrument = pdatalist.Tracks[idx].Instrument - 1;
                midiTrack.Transpose = pdatalist.Tracks[idx].Transpose / 12;
                midiTrack.ToneMode = pdatalist.ToneMode;
                midiTrack.trackChunk = chunk;

                _tracks.Add(midiTrack);
                idx++;
            }
            TrackList.ItemsSource = _tracks;
            TrackList.Items.Refresh();
        }

        /// <summary>
        /// Called when there is no config
        /// </summary>
        /// <param name="filename"></param>
        private void ReadWithoutConfig(string filename)
        {
            _midifile = MidiFile.Read(filename);
            ReadMidiData();
        }

        public void MidiFromSong(BmpSong song)
        {
            if (song == null)
                return;
            _tracks.Clear();
            _midiName = song.Title;
            _midifile = song.GetMelanchallMidiFile();
            ReadMidiData();
        }

        private void ReadMidiData()
        {
            this.GuitarModeSelector.SelectedIndex = 3;

            int idx = 0;
            foreach (TrackChunk chunk in _midifile.GetTrackChunks())
            {
                if (chunk.GetNotes().Count < 1)
                    continue;

                var trackName = TrackManipulations.GetTrackName(chunk);
                int octaveShift = 0;
                int progNum = -1;

                Regex rex = new Regex(@"^([A-Za-z _]+)([-+]\d)?");
                if (rex.Match(trackName) is Match match)
                    if (!string.IsNullOrEmpty(match.Groups[1].Value))
                    {
                        progNum = Instrument.Parse(match.Groups[1].Value).MidiProgramChangeCode;
                        if (!string.IsNullOrEmpty(match.Groups[2].Value))
                            if (int.TryParse(match.Groups[2].Value, out int os))
                                octaveShift = os;
                    }

                MidiBardImporter.MidiTrack midiTrack = new MidiBardImporter.MidiTrack();
                midiTrack.Index = idx + 1;
                midiTrack.TrackNumber = idx + 1;
                midiTrack.trackInstrument = Instrument.ParseByProgramChange(progNum).Index-1;
                midiTrack.Transpose = octaveShift;
                midiTrack.ToneMode = 3;
                midiTrack.trackChunk = chunk;

                _tracks.Add(midiTrack);
                idx++;
            }

            TrackList.ItemsSource = _tracks;
            TrackList.Items.Refresh();
        }

        #region Octave Up/Down

        private void OctaveControl_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OctaveNumericUpDown ctl = sender as OctaveNumericUpDown;
            ctl.OnValueChanged += OnOctaveValueChanged;
        }

        private static void OnOctaveValueChanged(object sender, int s)
        {
            MidiBardImporter.MidiTrack track = (sender as OctaveNumericUpDown).DataContext as MidiBardImporter.MidiTrack;
            track.Transpose = s;
            OctaveNumericUpDown ctl = sender as OctaveNumericUpDown;
            ctl.OnValueChanged -= OnOctaveValueChanged;
        }
        #endregion

        #region Drag&Drop
        /// <summary>
        /// Hier geht mitm Drag&Drop los
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        bool bnb = false;
        private void TrackListItem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (sender is System.Windows.Controls.ListViewItem celltext && !bnb)
                {
                    DragDrop.DoDragDrop(TrackList, celltext, DragDropEffects.Move);
                    e.Handled = true;
                }
                bnb = false;
            }
        }

        /// <summary>
        /// Called when there is a drop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackListItem_Drop(object sender, DragEventArgs e)
        {
            System.Windows.Controls.ListViewItem draggedObject = e.Data.GetData(typeof(System.Windows.Controls.ListViewItem)) as System.Windows.Controls.ListViewItem;
            System.Windows.Controls.ListViewItem targetObject = ((System.Windows.Controls.ListViewItem)(sender));

            var drag = draggedObject.Content as MidiBardImporter.MidiTrack;
            var drop = targetObject.Content as MidiBardImporter.MidiTrack;

            if (drag == drop)
                return;

            SortedDictionary<int, MidiBardImporter.MidiTrack> newTracks = new SortedDictionary<int, MidiBardImporter.MidiTrack>();
            int index = 0;
            foreach (var p in _tracks)
            {
                if (p == drag)
                    continue;

                if (p == drop)
                {
                    if (drop.Index < drag.Index)
                    {
                        newTracks.Add(index, drag); index++;
                        newTracks.Add(index, drop); index++;
                    }
                    else if (drop.Index > drag.Index)
                    {
                        newTracks.Add(index, drop); index++;
                        newTracks.Add(index, drag); index++;
                    }
                }
                else
                {
                    newTracks.Add(index, p);
                    index++;
                }
            }
            
            index = 0;
            foreach (var p in newTracks)
            {
                p.Value.Index = index;
                index++;
            }

            _tracks.Clear();
            foreach (var oT in newTracks)
                _tracks.Add(oT.Value);

            TrackList.ItemsSource = _tracks;
            TrackList.Items.Refresh();
            newTracks.Clear();
        }

        /// <summary>
        /// Helper for D&D
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BardNumBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bnb = true;
        }
        #endregion

        #region Sidemenu

        private void Sequencer_Click(object sender, RoutedEventArgs e)
        {
            if (_midifile == null)
                return;
            if (_tracks.Count() <= 0)
                return;

            MemoryStream myStream = new MemoryStream();
            if (_AlignMidiToFirstNote)
            {
                RealignMidiFile(MidiBardImporter.Convert(_midifile, CloneTracks())).Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings
                { TextEncoding = Encoding.UTF8 });
            }
            else
            {
                MidiBardImporter.Convert(_midifile, CloneTracks()).Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings
                { TextEncoding = Encoding.UTF8 });
            }
            myStream.Rewind();
            var song = BmpSong.ImportMidiFromByte(myStream.ToArray(), _midiName);
            Maestro.BmpMaestro.Instance.SetSong(song.Result);
            PlaybackFunctions.LoadSongFromPlaylist(song.Result);
            myStream.Close();
            myStream.Dispose();
        }

        /// <summary>
        /// Send song to Siren
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Siren_Click(object sender, RoutedEventArgs e)
        {
            if (_midifile == null)
                return;
            if (_tracks.Count() <= 0)
                return;

            MemoryStream myStream = new MemoryStream();
            if (_AlignMidiToFirstNote)
            {
                RealignMidiFile(MidiBardImporter.Convert(_midifile, CloneTracks())).Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings
                { TextEncoding = Encoding.UTF8 });
            }
            else
            {
                MidiBardImporter.Convert(_midifile, CloneTracks()).Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings
                { TextEncoding = Encoding.UTF8 });
            }
            myStream.Rewind();

            var song = BmpSong.ImportMidiFromByte(myStream.ToArray(), _midiName);
            _ = BmpSiren.Instance.Load(song.Result);
            myStream.Close();
            myStream.Dispose();
        }

        /// <summary>
        /// MidiExport
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (_midifile == null)
                return;
            if (_tracks.Count() <= 0)
                return;

            Stream myStream;
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "MIDI file (*.mid)|*.mid";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.OverwritePrompt = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                if ((myStream = saveFileDialog.OpenFile()) != null)
                {
                    if (_AlignMidiToFirstNote)
                    {
                        RealignMidiFile(MidiBardImporter.Convert(_midifile, CloneTracks())).Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings
                        { TextEncoding = Encoding.UTF8 });
                    }
                    else
                    {
                        MidiBardImporter.Convert(_midifile, CloneTracks()).Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings
                        { TextEncoding = Encoding.UTF8 });
                    }
                    myStream.Close();
                    myStream.Dispose();
                }
            }
        }

        /// <summary>
        /// Set the GuitarMode
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GuitarModeSelector_Selected(object sender, RoutedEventArgs e)
        {
            Parallel.ForEach(_tracks, track =>
            {
                track.ToneMode = GuitarModeSelector.SelectedIndex;
            });
            TrackList.Items.Refresh();
        }

        private void AlignToFirstNote_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _AlignMidiToFirstNote = (bool)AlignToFirstNote_CheckBox.IsChecked;
        }


        /// <summary>
        /// Realign the the notes and Events in a <see cref="MidiFile"/> to the beginning
        /// </summary>
        /// <param name="midi"></param>
        /// <returns><see cref="MidiFile"/></returns>
        private MidiFile RealignMidiFile(MidiFile midi)
        {
            //realign the events
            var x = midi.GetTrackChunks().GetNotes().First().GetTimedNoteOnEvent().Time;
            Parallel.ForEach(midi.GetTrackChunks(), chunk =>
            {
                chunk = RealignTrackEvents(chunk, x).Result;
            });
            return midi;
        }

        /// <summary>
        /// Realigns the track events in <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="originalChunk"></param>
        /// <param name="delta"></param>
        /// <returns><see cref="Task{TResult}"/> is <see cref="TrackChunk"/></returns>
        internal static Task<TrackChunk> RealignTrackEvents(TrackChunk originalChunk, long delta)
        {
            using (var manager = originalChunk.ManageTimedEvents())
            {
                foreach (TimedEvent _event in manager.Objects)
                {
                    long newStart = _event.Time - delta;
                    if (newStart <= -1)
                        _event.Time = 0;
                    else
                        _event.Time = newStart;
                }
            }
            return Task.FromResult(originalChunk);
        }
#endregion

        #region Context Menu
        private void TrackListItem_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _Sender = sender;
            e.Handled = true;
        }

        private void TrackListItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_Sender is System.Windows.Controls.ListViewItem)
            {
                var t = (_Sender as System.Windows.Controls.ListViewItem).Content as MidiBardImporter.MidiTrack;
                _tracks.Remove(t);
                RenumberTracks();
            }
            _Sender = null;
        }
        #endregion

        /// <summary>
        /// Renumber tracks
        /// </summary>
        private void RenumberTracks()
        {
            int index = 0;
            foreach (var p in _tracks)
            {
                p.Index = index;
                index++;
            }
            TrackList.Items.Refresh();
        }

        /// <summary>
        /// Clone the tracks
        /// </summary>
        /// <returns></returns>
        private List<MidiBardImporter.MidiTrack> CloneTracks()
        {
            List<MidiBardImporter.MidiTrack> tracks = new List<MidiBardImporter.MidiTrack>();
            foreach (var a in _tracks)
            {
                MidiBardImporter.MidiTrack ntrack = new MidiBardImporter.MidiTrack();
                ntrack.Index = a.Index;
                ntrack.TrackNumber = a.TrackNumber;
                ntrack.trackInstrument = a.trackInstrument;
                ntrack.Transpose = a.Transpose;
                ntrack.ToneMode = a.ToneMode;
                ntrack.trackChunk = (TrackChunk)a.trackChunk.Clone();
                tracks.Add(ntrack);
            }
            return tracks;
        }
    }
}
