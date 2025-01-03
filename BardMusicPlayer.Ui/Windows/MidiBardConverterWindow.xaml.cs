/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Siren;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Transmogrify.Song.Importers;
using BardMusicPlayer.Transmogrify.Song.Manipulation;
using BardMusicPlayer.Ui.Controls;
using BardMusicPlayer.Ui.Functions;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Tools;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BardMusicPlayer.Ui.Windows
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

    public class MidiBardConverter_GroupHelper
    {
        public MidiBardConverter_GroupHelper() { }

        public static Dictionary<int, string> TrackGroups()
        {
            Dictionary<int, string> instrumentList = new Dictionary<int, string>();
            for (int i =1; i != 40; i++)
                instrumentList.Add(i, Convert.ToString(i-1));
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

        NumericUpDown currentNumericControl { get; set; } = null;

        NumericUpDown NumericUpDown { get; set; } = null;

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
            if (currentNumericControl != null)
                currentNumericControl.OnValueChanged -= SongSpeed_Percent_OnValueChanged;

            if (_tracks.Count() > 0)
                _tracks.Clear();
        }

        private void ReadMidi(string filename)
        {
            _midiName = Path.GetFileNameWithoutExtension(filename);
            string ext = Path.GetExtension(filename);
            if (ext.StartsWith(".gp"))
                ReadGPX(filename);
            else
            {
                if (File.Exists(Path.ChangeExtension(filename, "json")))
                    ReadWithConfig(filename);
                else
                    ReadWithoutConfig(filename);
            }
        }

        private void ReadGPX(string filename)
        {
            _midifile = Transmogrify.Song.Importers.GuitarPro.ImportGuitarPro.OpenGTPSongFile(filename);
            ReadMidiData();
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
            AnalyzeLoadedMidi();
            TrackList.ItemsSource = _tracks;
            TrackList.Items.Refresh();
        }

        /// <summary>
        /// Called when there is no config
        /// </summary>
        /// <param name="filename"></param>
        private void ReadWithoutConfig(string filename)
        {
            try
            {
                _midifile = MidiFile.Read(filename);
                ReadMidiData();
            }
            catch //last resort if regular read fail
            {
                BmpSong bmpSong = BmpSong.OpenFile(filename).Result;
                MemoryStream stream = bmpSong.GetExportMidi();
                _midifile = MidiFile.Read(stream);
                stream.Close();
                stream.Dispose();
                ReadMidiData();
            }
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

                Regex rex = new Regex(@"^([A-Za-z _:]+)([-+]\d)?");
                if (rex.Match(trackName) is Match match)
                {
                    if (!string.IsNullOrEmpty(match.Groups[1].Value))
                    {
                        if (match.Groups[1].Value == "Program:ElectricGuitar")
                        {
                            TimedEvent noteEvent = chunk.GetTimedEvents().FirstOrDefault(n => n.Event.EventType == MidiEventType.NoteOn);
                            if (noteEvent != default)
                            {
                                TimedEvent progEvent = chunk.GetTimedEvents().LastOrDefault(n => n.Event.EventType == MidiEventType.ProgramChange && n.Time <= noteEvent.Time);
                                if (progEvent != default)
                                    progNum = (progEvent.Event as ProgramChangeEvent).ProgramNumber;
                            }
                        }
                        else
                        {
                            progNum = Instrument.Parse(match.Groups[1].Value).MidiProgramChangeCode;
                            if (!string.IsNullOrEmpty(match.Groups[2].Value))
                                if (int.TryParse(match.Groups[2].Value, out int os))
                                    octaveShift = os;
                        }
                    }
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
            AnalyzeLoadedMidi();
            TrackList.ItemsSource = _tracks;
            TrackList.Items.Refresh();
        }

        private void AnalyzeLoadedMidi()
        {
            foreach (var track in _tracks)
                AnalyzeTrack(track);

        }

        private void AnalyzeTrack(MidiBardImporter.MidiTrack track)
        {
            foreach (var item in track.trackChunk.GetNotes())
            {
                if (item.NoteNumber < track.MinNote.NoteNumber)
                    track.MinNote = item;
                if (item.NoteNumber > track.MaxNote.NoteNumber)
                    track.MaxNote = item;
            };
        }

        #region Octave Up/Down

        private void OctaveControl_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OctaveNumericUpDown ctl = sender as OctaveNumericUpDown;
            ctl.OnValueChanged += OnOctaveValueChanged;
            bnb = false;
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
            if (bnb)
            {
                e.Handled = true;
                return;
            }
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (sender is ListViewItem celltext && !bnb)
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
            ListViewItem draggedObject = e.Data.GetData(typeof(ListViewItem)) as ListViewItem;
            ListViewItem targetObject = ((ListViewItem)(sender));

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

        private void Instrument_Selector_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            bnb = false;
        }
        private void Instrument_Selector_DropDownClosed(object sender, System.EventArgs e)
        {
            bnb = false;
        }

        #endregion

        #region Sidemenu

        /// <summary>
        /// Check if we have valid data and input
        /// </summary>
        /// <returns></returns>
        private bool IsValidInput()
        {
            if (_midifile == null)
                return false;
            if (_tracks.Count() <= 0)
                return false;

            double divider = (float)(120 / (120 * (double)Convert.ToInt16(SongSpeed_Percent.Value) / 100));
            foreach (var trackChunk in _midifile.GetTrackChunks())
            {
                foreach (var TempoEvent in trackChunk.Events.OfType<SetTempoEvent>())
                {
                    var microsecondsPerQuarterNote = TempoEvent.MicrosecondsPerQuarterNote;
                    if ((long)Math.Round(microsecondsPerQuarterNote * divider) > 16777215)
                    {
                        MessageBox.Show("Invalid tempo settings!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
            return true;
        }

        private MemoryStream PrepareMidi()
        {
            List<MidiBardImporter.MidiTrack> tracks = CloneTracks();

            //Quantize if needed
            foreach (var x in tracks)
            {
                if (x.Quantize != null)
                {
                    x.trackChunk.QuantizeObjects(
                                ObjectType.TimedEvent,
                                new SteppedGrid(x.Quantize),
                                _midifile.GetTempoMap(),
                                new QuantizingSettings
                                {
                                    DistanceCalculationType = TimeSpanType.BarBeatTicks
                                });
                }
            }
            
            MemoryStream myStream = new MemoryStream();
            MidiFile outputMidi = MidiBardImporter.Convert(_midifile.Clone(), tracks).Result;

            if ((bool)AlignProgramChanges_CheckBox.IsChecked)
                outputMidi = RealignProgrmChanges(outputMidi);

            if (_AlignMidiToFirstNote)
                outputMidi = RealignMidiFile(outputMidi);

            if(Convert.ToInt16(SongSpeed_Percent.Value) != 100)
            {
                double divider = (float) (120 / (120 * (double)Convert.ToInt16(SongSpeed_Percent.Value) / 100));
                foreach (var trackChunk in outputMidi.GetTrackChunks())
                {
                    foreach (var setTempoEvent in trackChunk.Events.OfType<SetTempoEvent>())
                    {
                        var microsecondsPerQuarterNote = setTempoEvent.MicrosecondsPerQuarterNote;
                        setTempoEvent.MicrosecondsPerQuarterNote = (long)Math.Round(microsecondsPerQuarterNote * divider);
                    }
                }
            }

            outputMidi.Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings { TextEncoding = Encoding.UTF8 });

            tracks.Clear();
            myStream.Rewind();
            return myStream;
        }

        private void MBardSave_Click(object sender, RoutedEventArgs e)
        {
            if (_midifile == null)
                return;
            if (_tracks.Count() <= 0)
                return;

            var config = new MidiBardImporter.MidiFileConfig();
            int toneMode = -1;
            foreach (var x in CloneTracks())
            {
                var track = new MidiBardImporter.TrackConfig();
                track.Index = x.Index;
                track.Enabled = true;
                track.Name = Instrument.Parse(x.trackInstrument+1) + "(Bard " + Convert.ToString(x.TrackNumber)+")";
                track.Transpose = x.Transpose*12;
                track.Instrument = x.trackInstrument+1;
                track.AssignedCids.Add(x.TrackNumber);
                toneMode = x.ToneMode;
                config.Tracks.Add(track);
            }
            config.Speed = 1;
            config.AdaptNotes = false;
            config.ToneMode = toneMode;

            Stream myStream;
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "Config file (*.json)|*.json";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.OverwritePrompt = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                if ((myStream = saveFileDialog.OpenFile()) != null)
                {
                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    using (StreamWriter swriter = new StreamWriter(myStream))
                        swriter.Write(json);
                    myStream.Close();
                    myStream.Dispose();
                }
            }
        }

        private void Sequencer_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValidInput())
                return;

            MemoryStream myStream = PrepareMidi();
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
            if (!IsValidInput())
                return;

            MemoryStream myStream = PrepareMidi();
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
            if (!IsValidInput())
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
                    PrepareMidi().CopyTo(myStream);
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
            int mode = GuitarModeSelector.SelectedIndex;
            Parallel.ForEach(_tracks, track =>
            {
                track.ToneMode = mode;
            });
            TrackList.Items.Refresh();
        }

        private void AlignToFirstNote_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _AlignMidiToFirstNote = (bool)AlignToFirstNote_CheckBox.IsChecked;
        }

        private void VoiceMap_Click(object sender, RoutedEventArgs e)
        {
            VoiceMap vm = new VoiceMap(_midifile);
            vm.Visibility = Visibility.Visible;
        }
        #endregion

        #region Context Menu
        private void TrackListItem_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _Sender = sender;
            e.Handled = true;
        }

        private void TrackListItem_Duplicate_Click(object sender, RoutedEventArgs e)
        {
            if (_Sender is ListViewItem)
            {
                var t = (_Sender as ListViewItem).Content as MidiBardImporter.MidiTrack;
                MidiBardImporter.MidiTrack ntrack = new MidiBardImporter.MidiTrack();
                ntrack.Index = t.Index+1;
                ntrack.TrackNumber = t.TrackNumber+1;
                ntrack.trackInstrument = t.trackInstrument;
                ntrack.Transpose = t.Transpose;
                ntrack.ToneMode = t.ToneMode;
                ntrack.trackChunk = (TrackChunk)t.trackChunk.Clone();
                _tracks.Insert(t.Index, ntrack);
                AnalyzeTrack(ntrack);
                RenumberTracks();
            }
        }

        private void TrackListItem_Autotranspose_Click(object sender, RoutedEventArgs e)
        {
            if (_Sender is ListViewItem)
            {
                int highOctave = -1;
                int lowOctave = -1;
                var t = (_Sender as ListViewItem).Content as MidiBardImporter.MidiTrack;
                

                if (t.MinNote.Octave < 3)
                    lowOctave = t.MinNote.Octave;

                if (t.MaxNote.NoteNumber > 84)
                    highOctave =t.MaxNote.Octave;


                if (highOctave != -1 && lowOctave != -1)
                    return;

                if (lowOctave != -1)
                {
                    int newOctave = 3-lowOctave;
                    if (newOctave != t.Transpose)
                        t.Transpose = newOctave;
                }

                if (highOctave != -1)
                {
                    int newOctave = t.MaxNote.Octave - 5;
                    if (-newOctave != t.Transpose)
                        t.Transpose = -newOctave;
                }
                TrackList.Items.Refresh();
            }
        }

        private void TrackListItem_DrumMap_Click(object sender, RoutedEventArgs e)
        {
            if (_Sender is ListViewItem)
            {
                var t = (_Sender as ListViewItem).Content as MidiBardImporter.MidiTrack;
                Drummapping(t.trackChunk);

                var Result = MessageBox.Show("Delete old drum-track?\r\n", "Warning!", MessageBoxButton.YesNo);
                if (Result == MessageBoxResult.No)
                    return;

                _tracks.Remove(t);
                RenumberTracks();
            }
        }

        private void PitchConvert_Click(object sender, RoutedEventArgs e)
        {
            if (_Sender is ListViewItem)
            {
                var t = (_Sender as ListViewItem).Content as MidiBardImporter.MidiTrack;
                AutoPitchConvert(t.trackChunk);
            }
        }

        private void QuantCheck_Checked(object sender, RoutedEventArgs e)
        {
            MenuItem[] array = new MenuItem[] { Quant64, Quant32, Quant16, Quant8, Quant4, Quant0 };
            MusicalTimeSpan qu = null; 

            //get/reset the checked items
            if (e.Source is MenuItem)
            {
                var x = e.Source as MenuItem;
                foreach (MenuItem p in array)
                {
                    if (p.Name == x.Name)
                        continue;
                    p.IsChecked = false;
                }

                switch(x.Name)
                {
                    case "Quant64":
                        qu = MusicalTimeSpan.SixtyFourth;
                        break;
                    case "Quant32":
                        qu = MusicalTimeSpan.ThirtySecond;
                        break;
                    case "Quant16":
                        qu = MusicalTimeSpan.Sixteenth;
                        break;
                    case "Quant8":
                        qu = MusicalTimeSpan.Eighth;
                        break;
                    case "Quant4":
                        qu = MusicalTimeSpan.Quarter;
                        break;
                    case "Quant2":
                        qu = MusicalTimeSpan.Half;
                        break;
                    case "Quant0":
                        qu = null;
                        break;
                }
            }

            if (_Sender is ListViewItem)
            {
                var t = (_Sender as ListViewItem).Content as MidiBardImporter.MidiTrack;
                t.Quantize = qu;
            }
            _Sender = null;
            array = null;
        }

        private void QuantCheck_UnChecked(object sender, RoutedEventArgs e)
        {
        }

        private void TrackListItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_Sender is ListViewItem)
            {
                var Result = MessageBox.Show("Delete this track?\r\n", "Warning!", MessageBoxButton.YesNo);
                if (Result == MessageBoxResult.No)
                    return;

                var t = (_Sender as ListViewItem).Content as MidiBardImporter.MidiTrack;
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
                ntrack.MinNote = a.MinNote;
                ntrack.MaxNote = a.MaxNote;
                ntrack.Quantize = a.Quantize;
                ntrack.trackChunk = (TrackChunk)a.trackChunk.Clone();
                tracks.Add(ntrack);
            }
            return tracks;
        }

        /// <summary>
        /// Split drums in <see cref="TrackChunk"/> into new <see cref="TrackChunk"/>
        /// </summary>
        /// <param name="track"></param>
        public void Drummapping(TrackChunk track)
        {
            if ((int)track.GetNotes().First().Channel != 9)
            {
                var Result = MessageBox.Show("Looks like, this isn't a drum-track\r\nContinue the mapping?", "Warning!", MessageBoxButton.YesNo);
                if (Result == MessageBoxResult.No)
                    return;
            }
            var openFileDialog = new OpenFileDialog
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
                MessageBox.Show(drumTracks.First().Key, "Error!", MessageBoxButton.OK);
                return;
            }

            var lastTrack = _tracks.Last();
            int idx = 1;
            foreach (var nt in drumTracks)
            {
                MidiBardImporter.MidiTrack ntrack = new MidiBardImporter.MidiTrack();
                ntrack.Index = lastTrack.Index+idx;
                ntrack.TrackNumber = lastTrack.TrackNumber+idx;
                ntrack.trackInstrument = Instrument.Parse(nt.Key).Index-1;
                ntrack.Transpose = 0;
                ntrack.ToneMode = 0;
                ntrack.trackChunk = nt.Value;
                _tracks.Add(ntrack);
                idx++;
            }
            RenumberTracks();
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

        private MidiFile RealignProgrmChanges(MidiFile midi)
        {
            Parallel.ForEach(midi.GetTrackChunks(), chunk =>
            {
                chunk = RealignProgrmChangeTrackEvents(chunk, midi).Result;
            });
            return midi;
        }

        internal static Task<TrackChunk> RealignProgrmChangeTrackEvents(TrackChunk originalChunk, MidiFile midi)
        {
            using (var manager = originalChunk.ManageTimedEvents())
            {
                foreach (TimedEvent _event in manager.Objects)
                {
                    if (_event.Event.EventType != MidiEventType.ProgramChange)
                        continue;

                    if (_event.Time == 0)
                        continue;

                    Note overlap = originalChunk.GetNotes().FirstOrDefault(n => n.Time < _event.Time && _event.Time <= n.EndTime);
                    if (overlap == null)
                        continue;

                    Note nextNote = originalChunk.GetNotes().FirstOrDefault(n => n.Time >= overlap.EndTime);
                    if (nextNote == null) //no next note? ignore it
                        continue;
                    if (_event.Time == overlap.EndTime) //leave it if it's at the end
                        continue;
                    else if (overlap.EndTime == nextNote.Time) //move it to the start of the next note
                        _event.Time = nextNote.Time;
                    else if ((nextNote.TimeAs<MetricTimeSpan>(midi.GetTempoMap()).TotalMilliseconds - overlap.EndTimeAs<MetricTimeSpan>(midi.GetTempoMap()).TotalMilliseconds) < 20) //below 20ms, move it to the beginning
                        _event.Time = nextNote.Time;
                    else //in any other case
                        _event.Time = overlap.EndTime;
                }
            }
            return Task.FromResult(originalChunk);
        }


        private struct PitchData
        {
            public int Pitch;
            public long start;
            public long end;
        }
        private void AutoPitchConvert(TrackChunk originalChunk)
        {
            if (originalChunk.GetTimedEvents().Where(e => e.Event is PitchBendEvent).Count() == 0)
                return;

            List<PitchData> activePitchbends = new List<PitchData>();
            PitchData tempPitch = new PitchData() { Pitch = 0xff, end = -1 };
            int lastPitch = 0;
            foreach (var timedEvent in originalChunk.GetTimedEvents().Where(e => e.Event is PitchBendEvent))
            {
                PitchBendEvent pitch = timedEvent.Event as PitchBendEvent;
                int newPitch = 0;
                if (pitch.PitchValue < 4096)
                    newPitch = -2;
                else if (pitch.PitchValue < 8192)
                    newPitch = -1;
                else if (pitch.PitchValue < 12288)
                    newPitch = 0;
                else if (pitch.PitchValue < 16383)
                    newPitch = 1;
                else
                    newPitch = 2;

                if (newPitch == lastPitch)
                    continue;

                tempPitch.end = timedEvent.Time;

                if (tempPitch.Pitch != 0xff)
                    activePitchbends.Add(tempPitch);

                tempPitch.Pitch = newPitch;
                tempPitch.start = timedEvent.Time;
                tempPitch.end = -1;
                lastPitch = newPitch;
            }
            activePitchbends.Add(tempPitch);

            originalChunk.RemoveTimedEvents(e => e.Event is PitchBendEvent);

            //Split times for the split grid
            List<ITimeSpan> timeSpan = new List<ITimeSpan>();

            foreach (var v in activePitchbends)
                timeSpan.Add(new MidiTimeSpan(v.start));

            using (var notesManager = originalChunk.ManageNotes())
            {
                //Split every note
                var splitnotesList = Splitter.SplitObjectsByGrid(originalChunk.GetNotes(), new ArbitraryGrid(timeSpan.ToArray()), _midifile.GetTempoMap());
                //remove all notes, we'll insert it back with the pitch
                notesManager.Objects.Clear();

                PitchData lastPitchData = new PitchData() { Pitch = 0 };
                foreach (var note in splitnotesList)
                {
                    var pData = activePitchbends.Where(n => n.start == note.Time);
                    if (pData.Count() > 0)
                        lastPitchData = pData.FirstOrDefault();

                    if (lastPitchData.Pitch != 0)
                    {
                        Note newPitchNote = note as Note;
                        newPitchNote.NoteNumber = (SevenBitNumber)(newPitchNote.NoteNumber + lastPitchData.Pitch);
                        notesManager.Objects.Add(newPitchNote);
                    }
                    else
                        notesManager.Objects.Add(note as Note);
                }
                notesManager.SaveChanges();
                return;
            }
        }

        private void SongSpeed_Percent_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentNumericControl == null)
            {
                currentNumericControl = sender as NumericUpDown;
                currentNumericControl.OnValueChanged += SongSpeed_Percent_OnValueChanged;
            }
        }

        private void SongSpeed_Percent_OnValueChanged(object sender, int s)
        {
            SongSpeed_Percent.Value = s.ToString();
        }
    }
}
