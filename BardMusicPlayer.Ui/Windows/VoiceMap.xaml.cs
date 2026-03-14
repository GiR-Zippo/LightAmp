/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Siren;
using BardMusicPlayer.Transmogrify.Song;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BardMusicPlayer.Ui.Windows
{
    /// <summary>
    /// Interaktionslogik für VoiceMap.xaml
    /// </summary>
    public partial class VoiceMap : Window
    {
        private Polyline sirenXY = new Polyline { Stroke = Brushes.Red };
        private Polyline predictXY = new Polyline { Stroke = Brushes.Green };
        private Polyline vlsusXY = new Polyline { Stroke = Brushes.Aqua };
        
        MidiFile midi = null;
        public VoiceMap(MidiFile arg)
        {
            InitializeComponent();
            BmpSiren.Instance.SynthTimePositionChanged += Instance_SynthTimePositionChanged;
            BmpSiren.Instance.SongLoaded += Instance_SongLoaded;
            //AddPlot();
            midi = arg;
            //if (arg == null)
                ClearData();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BmpSiren.Instance.SynthTimePositionChanged -= Instance_SynthTimePositionChanged;
            BmpSiren.Instance.SongLoaded -= Instance_SongLoaded;
        }

        private void Instance_SongLoaded(string songTitle)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.ClearData()));
        }

        private void Instance_SynthTimePositionChanged(string songTitle, double currentTime, double endTime, int activeVoices)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.SirenPlot(currentTime, endTime, activeVoices)));
        }

        private void ClearData()
        {
            gCanvasPlot0.Children.Clear();

            Polyline redline = new Polyline { Stroke = Brushes.Red };
            redline.Width = gCanvasPlot0.Width;
            redline.Points.Add(CorrespondingPoint(new Point(0, ((41 / gCanvasPlot0.Height) * 8) - 1), 3));
            redline.Points.Add(CorrespondingPoint(new Point(10, ((41 / gCanvasPlot0.Height) * 8) - 1), 3));
            gCanvasPlot0.Children.Add(redline);
            Text(0.1, 218, "OVER", Color.FromRgb(255, 0, 0));

            redline = new Polyline { Stroke = Brushes.Yellow };
            redline.Width = gCanvasPlot0.Width;
            redline.Points.Add(CorrespondingPoint(new Point(0, ((30 / gCanvasPlot0.Height) * 8) - 1), 3));
            redline.Points.Add(CorrespondingPoint(new Point(10, ((30 / gCanvasPlot0.Height) * 8) - 1), 3));
            gCanvasPlot0.Children.Add(redline);
            Text(0.1, 258, "O.O", Color.FromRgb(255, 255, 0));

            sirenXY = new Polyline { Stroke = Brushes.Red };
            gCanvasPlot0.Children.Add(sirenXY);

            predictXY = new Polyline { Stroke = Brushes.Green };
            gCanvasPlot0.Children.Add(predictXY);

            gOver.Text = "";
            gScroller.ScrollToVerticalOffset(gScroller.MaxHeight);

            if (midi != null)
            {
                gOver.Text += String.Format("Midi Report:\r\n");
                gOver.Text += String.Format("Tracks: {0} \r\n", midi.GetTrackChunks().Count() - 1);
                gOver.Text += String.Format("Notes: {0} \r\n", midi.GetNotes().Count() - 1);
                gOver.Text += String.Format("Events: {0} \r\n", midi.GetTimedEvents().Count() - 1);
                gOver.Text += String.Format("\r\n");

                var vl = DrawParNotes();
                if (vl.Value != null)
                {
                    Text(0.1, gCanvasPlot0.Height - 30, "Par. Notes", Color.FromRgb(255, 255, 255));
                    vlsusXY = vl.Value;
                    gCanvasPlot0.Children.Add(vlsusXY);
                    foreach (string text in vl.Key)
                        gOver.Text += text;
                    gOver.SelectionStart = gOver.Text.Length;
                }

                vl = DrawVLSus().Result;
                if (vl.Value != null)
                {
                    Text(0.1, gCanvasPlot0.Height - 50, "Voice Pred.", Color.FromRgb(255, 255, 255));
                    vlsusXY = vl.Value;
                    gCanvasPlot0.Children.Add(vlsusXY);
                    foreach (string text in vl.Key)
                        gOver.Text += text;
                    gOver.SelectionStart = gOver.Text.Length;
                }
            }
            gOver.Text += String.Format("Active Voices Report:\r\n");
        }

        /// <summary>
        /// Draw the par. notes and build the report
        /// </summary>
        private KeyValuePair<List<string>, Polyline> DrawParNotes()
        {
            List<string> outText = new List<string>();
            var intGraph = new Polyline { Stroke = Brushes.LightGreen };
            if (midi == null)
                return new KeyValuePair<List<string>, Polyline>(outText, intGraph);

            int iNumOfCycles = 3;
            int maxTraxx = midi.GetTrackChunks().Count() - 1;
            double endTime = midi.GetDuration<MetricTimeSpan>().TotalMilliseconds;

            intGraph.Visibility = Visibility.Hidden;
            outText.Add(String.Format("Parallel Notes Report:\r\n"));
            int i = 0;
            foreach (TimedEvent x in midi.GetTimedEvents())
            {
                double dX = x.TimeAs<MetricTimeSpan>(midi.GetTempoMap()).TotalMilliseconds / (endTime / 2);
                if (x.Event is NoteOnEvent)
                {
                    i++;
                    intGraph.Points.Add(CorrespondingPoint(new Point(dX, ((i / gCanvasPlot0.Height) * 8) - 1), iNumOfCycles));
                }
                if (x.Event is NoteOffEvent)
                {
                    i--;
                    intGraph.Points.Add(CorrespondingPoint(new Point(dX, ((i / gCanvasPlot0.Height) * 8) - 1), iNumOfCycles));
                }

                if (i > maxTraxx)
                {
                    outText.Add(String.Format(@"Time : {0:mm\:ss\.ff}", TimeSpan.FromMilliseconds(x.TimeAs<MetricTimeSpan>(midi.GetTempoMap()).TotalMilliseconds)) + " = Voices:" + i + " Tracks:" + maxTraxx + "\r\n");
                }
            }
            outText.Add(String.Format("----------------------------------------------------------\r\n"));
            intGraph.Visibility = Visibility.Visible;
            return new KeyValuePair<List<string>, Polyline>(outText, intGraph);
        }

        /// <summary>
        /// Draw the voicelimit approx async and build the report
        /// </summary>
        private async Task<KeyValuePair<List<string>, Polyline>> DrawVLSus()
        {
            List<string> outText = new List<string>();
            var intGraph = new Polyline { Stroke = Brushes.Aqua };
            if (midi == null)
                return new KeyValuePair<List<string>, Polyline>(outText, intGraph);

            int iNumOfCycles = 3;
            double endTime = midi.GetDuration<MetricTimeSpan>().TotalMilliseconds;

            intGraph.Visibility = Visibility.Hidden;
            outText.Add(String.Format("VoiceCount Prediction:\r\n"));

            MemoryStream myStream = new MemoryStream();
            midi.Write(myStream, MidiFileFormat.MultiTrack, settings: new WritingSettings
            { TextEncoding = Encoding.UTF8 });
            var song = BmpSong.ImportMidiFromByte(myStream.ToArray(), "TestFile").Result;
            Dictionary<double, int> x = await Utils.GetSynthMidi(song);

            foreach (var tSeries in x.OrderBy(s => s.Key))
            {
                intGraph.Points.Add(CorrespondingPoint(new Point(tSeries.Key / (endTime / 2), (((tSeries.Value*2.0) / gCanvasPlot0.Height) * 8) - 1), iNumOfCycles));
                if (tSeries.Value > 20)
                {
                    outText.Add(String.Format(@"Time : {0:mm\:ss\.ff}", TimeSpan.FromMilliseconds(tSeries.Key)) + " = Voices:" + tSeries.Value + "\r\n");
                }
            }
            outText.Add(String.Format("----------------------------------------------------------\r\n"));
            intGraph.Visibility = Visibility.Visible;
            return new KeyValuePair<List<string>, Polyline>(outText, intGraph);
        }

        private void SirenPlot(double currentTime, double endTime, int activeVoices)
        {
            if (currentTime == endTime)
                return;

            //if (activeVoices > 0 && diff == -1)
            //    diff = currentTime;


            int iNumOfCycles = 3;
            double dX = (currentTime) / (endTime /2);
            if (activeVoices > 40)
            {
                sirenXY.Stroke = Brushes.Red;
                gOver.Text += String.Format(@"Time : {0:mm\:ss\.ff}", TimeSpan.FromMilliseconds(currentTime)) + " = Voices:" + activeVoices +"\r\n";
            }
            if (activeVoices <= 40)
                sirenXY.Stroke = Brushes.Blue;

            sirenXY.Points.Add(CorrespondingPoint(new Point(dX, ((activeVoices / gCanvasPlot0.Height) * 8)-1), iNumOfCycles));
        }

        private Point CorrespondingPoint(Point pt, int iNumOfCycles)
        {
            double dXmin = 0;
            double dXmax = iNumOfCycles;
            double dYmin = -1.1;
            double dYmax = 1.1;
            double dPlotWidth = dXmax - dXmin;
            double dPlotHeight = dYmax - dYmin;

            var poResult = new Point
            {
                X = (pt.X - dXmin) * gCanvasPlot0.Width / (dPlotWidth),
                Y = gCanvasPlot0.Height - (pt.Y - dYmin) * gCanvasPlot0.Height /
                    (dPlotHeight)
            };
            return poResult;
        }

        private void Text(double x, double y, string text, Color color)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = new SolidColorBrush(color);
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            gCanvasPlot0.Children.Add(textBlock);
        }
    }

    internal static class Utils
    {
        class NoteInfo
        {
            public double time;
            public double dur;
            public int note;
        }

        /// <summary>
        /// time in ms, notecount
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        internal static async Task< Dictionary<double, int> > GetSynthMidi(this BmpSong song)
        {
            Dictionary<double, int> countDict = new Dictionary<double, int>();

            Dictionary<NoteInfo,  int> tempDict = new Dictionary<NoteInfo, int>();

            var trackCounter = byte.MinValue;
            var midiFile = await song.GetProcessedMidiFile();
            var trackChunks = midiFile.GetTrackChunks().ToList();

            var lyrics = new Dictionary<int, Dictionary<long, string>>();
            var lyricNum = 0;

            //Skip first track, is eh nur "All Tracks"
            foreach (var trackChunk in trackChunks.GetRange(1, trackChunks.Count-1))
            {
                Instrument instr = Instrument.None;
                int trackOctaveShift = 0;
                using (var manager = trackChunk.ManageTimedEvents())
                {
                    var trackName = trackChunk.Events.OfType<SequenceTrackNameEvent>().First().Text;
                    Regex rex = new Regex(@"^([A-Za-z _:]+)([-+]\d)?");
                    if (rex.Match(trackName) is Match match)
                    {
                        if (!string.IsNullOrEmpty(match.Groups[1].Value))
                        {
                            instr = Instrument.Parse(match.Groups[1].Value);
                        }
                        if (int.TryParse(match.Groups[2].Value, out int os))
                            trackOctaveShift = os;
                    }
                    Dictionary<float, KeyValuePair<NoteEvent, Instrument>> instrumentMap = new Dictionary<float, KeyValuePair<NoteEvent, Instrument>>();

                    foreach (TimedEvent _event in manager.Objects)
                    {
                        var noteEvent = _event.Event as NoteEvent;
                        var lyricsEvent = _event.Event as LyricEvent;
                        var programChangeEvent = _event.Event as ProgramChangeEvent;

                        if (noteEvent != null && _event.Event.EventType == MidiEventType.NoteOn)
                            instrumentMap.Add(_event.Time, new KeyValuePair<NoteEvent, Instrument>(noteEvent, instr));

                        if (programChangeEvent != null)
                        {
                            if (instr.InstrumentTone.Equals(InstrumentTone.ElectricGuitar))
                                instr = Instrument.ParseByProgramChange(programChangeEvent.ProgramNumber);
                        }
                        if (lyricsEvent != null)
                        {
                            if (lyrics.Count() < lyricNum+1)
                                lyrics.Add(lyricNum, new Dictionary<long, string>());
                            lyrics[lyricNum].Add(_event.Time, lyricsEvent.Text);
                            lyricNum++;
                        }
                    }

                    List<NoteInfo> notesActive = new List<NoteInfo>();

                    foreach (var note in trackChunk.GetNotes())
                    {
                        var instrument = instr;
                        KeyValuePair<NoteEvent, Instrument> test;
                        if (instrumentMap.TryGetValue(note.Time, out test))
                        {
                            if (note.NoteNumber == test.Key.NoteNumber)
                            {
                                instrument = test.Value;
                            }
                        }

                        var noteNum = note.NoteNumber+(12 * trackOctaveShift);
                        if (noteNum < 0)   noteNum = 0;
                        if (noteNum > 127) noteNum = 127;
                        var dur = (double)MinimumLength(instrument, noteNum - 48, note.Length) + (double)note.LengthAs<MetricTimeSpan>(midiFile.GetTempoMap()).TotalMilliseconds;
                        var time = (double)note.TimeAs<MetricTimeSpan>(midiFile.GetTempoMap()).TotalMilliseconds;


                        var t = notesActive.FindAll(n => n.time + n.dur >= time );
                        if (!countDict.ContainsKey(time))
                            countDict.Add(time, t.Count+1);
                        else
                            countDict[time] = countDict[time] + t.Count+1;
                        //foreach (var x in notesActive.FindAll(n => n.time + n.dur <= time))
                        
//notesActive.Remove(x);

                        NoteInfo info = new NoteInfo() { note = noteNum, dur = dur, time = time };
                        notesActive.Add(info);

                        if (trackCounter == byte.MaxValue)
                            trackCounter = byte.MinValue;
                        else
                            trackCounter++;


                    }
                    instrumentMap.Clear();
                    notesActive.Clear();
                }
            }
            trackChunks.Clear();
            return countDict;
        }

        private static long MinimumLength(Instrument instrument, int note, long duration)
        {
            switch (instrument.Index)
            {
                case 1: // Harp
                    if (note <= 9) return 1338;
                    else if (note <= 19) return 1338;
                    else if (note <= 28) return 1334;
                    else return 1136;
                case 2: // Piano
                    if (note <= 11) return 1531;
                    else if (note <= 18) return 1531;
                    else if (note <= 25) return 1530;
                    else if (note <= 28) return 1332;
                    else return 1531;
                case 3: // Lute
                    if (note <= 14) return 1728;
                    else if (note <= 21) return 1727;
                    else if (note <= 28) return 1727;
                    else return 1528;
                case 4: // Fiddle
                    if (note <= 3) return 634;
                    else if (note <= 6) return 634;
                    else if (note <= 11) return 633;
                    else if (note <= 15) return 634;
                    else if (note <= 18) return 633;
                    else if (note <= 23) return 635;
                    else if (note <= 30) return 635;
                    else return 635;
                case 5: // Flute
                case 6: // Oboe
                case 7: // Clarinet
                case 8: // Fife
                case 9: // Panpipes
                    return duration > 4500 ? 4500 : duration < 500 ? 500 : duration;

                case 10: // Timpani
                    if (note <= 15) return 1193;
                    else if (note <= 23) return 1355;
                    else return 1309;
                case 11: // Bongo
                    if (note <= 7) return 720;
                    else if (note <= 21) return 544;
                    else return 275;
                case 12: // BassDrum
                    if (note <= 6) return 448;
                    else if (note <= 11) return 335;
                    else if (note <= 23) return 343;
                    else return 254;
                case 13: // SnareDrum
                    return 260;

                case 14: // Cymbal
                    return 700;

                case 15: // Trumpet
                case 16: // Trombone
                case 17: // Tuba
                case 18: // Horn
                case 19: // Saxophone
                case 20: // Violin
                case 21: // Viola
                case 22: // Cello
                case 23: // DoubleBass
                    return duration > 4500 ? 4500 : duration < 300 ? 300 : duration;
                case 24: // ElectricGuitarOverdriven
                    return duration > 4500 ? 4500 : duration < 300 ? 300 : duration;
                case 25: // ElectricGuitarClean
                case 27: // ElectricGuitarPowerChords
                    return duration > 4500 ? 4500 : duration < 300 ? 300 : duration;
                case 26: // ElectricGuitarMuted
                    if (note <= 18) return 186;
                    else if (note <= 21) return 158;
                    else return 174;
                case 28: // ElectricGuitarSpecial
                    return 1500;

                default: return duration;
            }
        }
    }
}
