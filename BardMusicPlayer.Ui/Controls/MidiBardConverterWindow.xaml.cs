/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.MidiUtil.Managers;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Quotidian.UtcMilliTime;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Transmogrify.Song.Importers;
using BardMusicPlayer.Transmogrify.Song.Manipulation;
using BardMusicPlayer.Transmogrify.Song.Utilities;
using BardMusicPlayer.Ui.Functions;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Win32;
using Newtonsoft.Json;
using Sanford.Multimedia.Timers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Text.RegularExpressions;
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
        MidiFile _midifile = null;
        
        public MidiBardConverterWindow(string filename)
        {
            _tracks = new List<MidiBardImporter.MidiTrack>();

            InitializeComponent();
            ReadMidi(filename);
        }

        private void ReadMidi(string filename)
        {
            if (File.Exists(System.IO.Path.ChangeExtension(filename, "json")))
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
            FileStream fileStream = File.Open(System.IO.Path.ChangeExtension(filename, "json"), FileMode.Open);
            fileStream.CopyTo(memoryStream);
            fileStream.Close();

            var data = memoryStream.ToArray();
            MidiBardImporter.MidiFileConfig pdatalist = JsonConvert.DeserializeObject<MidiBardImporter.MidiFileConfig>(new UTF8Encoding(true).GetString(data));

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
        }

        /// <summary>
        /// Called when there is no config
        /// </summary>
        /// <param name="filename"></param>
        private void ReadWithoutConfig(string filename)
        {
            //Read the midi
            _midifile = MidiFile.Read(filename);

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
        }

        /// <summary>
        /// MidiExport
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            BmpSong song = PlaybackFunctions.CurrentSong;
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
                    MidiBardImporter.Convert(_midifile, _tracks).Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings
                    {
                        TextEncoding = System.Text.Encoding.UTF8
                    });
                    myStream.Close();
                }
            }
        }

        private void Sequencer_Click(object sender, RoutedEventArgs e)
        {
            //var song = BmpSong.ImportMidiFromByte(Convert().ToArray(), "Import");
            //Maestro.BmpMaestro.Instance.SetSong(song.Result);
        }

        /* Octave UP/Down */
        private void OctaveControl_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OctaveNumericUpDown ctl = sender as OctaveNumericUpDown;
            ctl.OnValueChanged += OnOctaveValueChanged;
        }

        private static void OnOctaveValueChanged(object sender, int s)
        {
            MidiBardImporter.MidiTrack track = (sender as OctaveNumericUpDown).DataContext as MidiBardImporter.MidiTrack;
            track.Transpose = s;

            Debug.WriteLine("");
            //BmpMaestro.Instance.SetOctaveshift(performer, s);

            OctaveNumericUpDown ctl = sender as OctaveNumericUpDown;
            ctl.OnValueChanged -= OnOctaveValueChanged;
        }

        /// <summary>
        /// Hier geht mitm Drag&Drop los
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackListItem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (sender is System.Windows.Controls.ListViewItem celltext)
                    DragDrop.DoDragDrop(TrackList, celltext, DragDropEffects.Move);
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

            Dictionary<int, MidiBardImporter.MidiTrack> newTracks = new Dictionary<int, MidiBardImporter.MidiTrack>();
            int newIdx = 0;
            foreach (var oT in _tracks)
            {
                if (oT.Index == drag.Index)
                    continue;

                if (oT.Index == drop.Index)
                {
                    drag.Index = newIdx;
                    newTracks.Add(newIdx, drag);
                    newIdx++;
                }

                oT.Index = newIdx;
                newTracks.Add(newIdx, oT);
                newIdx++;
            }

            _tracks.Clear();
            foreach (var oT in newTracks)
                _tracks.Add(oT.Value);

            TrackList.ItemsSource = _tracks;
            TrackList.Items.Refresh();
            newTracks.Clear();
        }
    }
}
