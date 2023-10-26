/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Siren;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BardMusicPlayer.Ui.Controls
{
    /// <summary>
    /// Interaktionslogik für VoiceMap.xaml
    /// </summary>
    public partial class VoiceMap : Window
    {
        private Polyline sirenXY = new Polyline { Stroke = Brushes.Red };
        private Polyline predictXY = new Polyline { Stroke = Brushes.Green };
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
                DrawPrediction();

            gOver.Text += String.Format("Active Voices Report:\r\n");
        }

        private Task<bool> DrawPrediction()
        {
            if (midi == null)
                return Task.FromResult(false);

            gOver.Text += String.Format("Midi Report:\r\n");
            gOver.Text += String.Format("Tracks: {0} \r\n", midi.GetTrackChunks().Count()-1);
            gOver.Text += String.Format("Notes: {0} \r\n", midi.GetNotes().Count() - 1);
            gOver.Text += String.Format("Events: {0} \r\n", midi.GetTimedEvents().Count() - 1);
            gOver.Text += String.Format("\r\n");
            Text(0.1, gCanvasPlot0.Height-30, "Par. Notes", Color.FromRgb(255, 255, 255));

            int iNumOfCycles = 3;
            int maxTraxx = midi.GetTrackChunks().Count()-1;
            double endTime = midi.GetDuration<MetricTimeSpan>().TotalMilliseconds;

            gOver.Text += String.Format("Parallel Notes Report:\r\n");
            int i = 0;
            foreach (TimedEvent x in midi.GetTimedEvents())
            {
                double dX = x.TimeAs<MetricTimeSpan>(midi.GetTempoMap()).TotalMilliseconds / (endTime / 2);
                if (x.Event is NoteOnEvent)
                {
                    i++;
                    predictXY.Points.Add(CorrespondingPoint(new Point(dX, ((i / gCanvasPlot0.Height) * 8) - 1), iNumOfCycles));
                }
                if (x.Event is NoteOffEvent)
                {
                    i--;
                    predictXY.Points.Add(CorrespondingPoint(new Point(dX, ((i / gCanvasPlot0.Height) * 8) - 1), iNumOfCycles));
                }

                if (i > maxTraxx)
                {
                    gOver.Text += String.Format(@"Time : {0:mm\:ss\.ff}", TimeSpan.FromMilliseconds(x.TimeAs<MetricTimeSpan>(midi.GetTempoMap()).TotalMilliseconds)) + " = Voices:" + i + " Tracks:"+ maxTraxx +  "\r\n";
                }
            }
            gOver.Text += String.Format("----------------------------------------------------------\r\n");
            gOver.SelectionStart = gOver.Text.Length;
            return Task.FromResult(true);
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
}
