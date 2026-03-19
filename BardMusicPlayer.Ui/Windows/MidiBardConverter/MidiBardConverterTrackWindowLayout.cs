
/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Melanchall.DryWetMidi.Interaction;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    /// <summary>
    /// The layout stuff
    /// </summary>
    public partial class MidiBardConverterTrackWindow
    {
        /// <summary>
        /// Refresh the whole layout
        /// </summary>
        private void RefreshLayout()
        {
            double w = Math.Max(MainScroll.ActualWidth, (_maxTick * _tickPixelScale) + 500);
            EditorGrid.Width = w; EditorGrid.Height = 128 * _noteHeight;
            GridCanvas.Width = w; GridCanvas.Height = EditorGrid.Height;
            RulerCanvas.Width = w; AutomationCanvas.Width = w; MarkersCanvas.Width = w;
            DrawGrid();
            DrawRuler();
            DrawMarkers();
            DrawAutomation();
            DrawAutomationGrid();
        }

        /// <summary>
        /// Draw the piano bar on the left
        /// </summary>
        private void BuildPianoKeys()
        {
            PianoKeysStack.Children.Clear();
            int[] blacks = { 1, 3, 6, 8, 10 };
            for (int i = 127; i >= 0; i--)
            {
                var b = new Border { Height = _noteHeight, Background = blacks.Contains(i % 12) ? new SolidColorBrush(Color.FromRgb(40, 40, 40)) : Brushes.White, BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1) };
                if (i % 12 == 0)
                    b.Child = new TextBlock { Text = "C" + ((i / 12) - 1), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Black, Margin = new Thickness(5, 0, 0, 0) };
                PianoKeysStack.Children.Add(b);
            }
        }

        /// <summary>
        /// Draw the raster
        /// </summary>
        private void DrawGrid()
        {
            GridCanvas.Children.Clear();
            var ts = _tempoMap.GetTimeSignatureAtTime((MidiTimeSpan)0);
            long tpb = _ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;
            for (int i = 0; i <= 128; i++)
                GridCanvas.Children.Add(new Line { X1 = 0, X2 = EditorGrid.Width, Y1 = i * _noteHeight, Y2 = i * _noteHeight, Stroke = new SolidColorBrush(Color.FromRgb(35, 35, 35)), StrokeThickness = 0.5 });
            for (long t = 0; t < EditorGrid.Width / _tickPixelScale; t += _gridSnapTicks)
            {
                double x = t * _tickPixelScale; bool isB = t % tpb == 0;
                GridCanvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = 0, Y2 = EditorGrid.Height, Stroke = isB ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) : new SolidColorBrush(Color.FromRgb(30, 30, 30)), StrokeThickness = 0.5 });
            }
        }

        /// <summary>
        /// Draw raster on automata grid
        /// </summary>
        private void DrawAutomationGrid()
        {
            AutomationGridCanvas.Children.Clear();
            double w = AutomationCanvas.ActualWidth;
            double h = AutomationCanvas.ActualHeight;

            if (w <= 0 || h <= 0) return;

            var ts = _tempoMap.GetTimeSignatureAtTime((MidiTimeSpan)0);
            long tpb = _ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;

            long maxTicks = (long)(w / _tickPixelScale);
            for (long t = 0; t <= maxTicks; t += _gridSnapTicks)
            {
                double x = t * _tickPixelScale;
                bool isB = (t % tpb == 0);
                AutomationGridCanvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = 0, Y2 = h, Stroke = isB ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) : new SolidColorBrush(Color.FromRgb(35, 35, 35)), StrokeThickness = 0.5 });
            }

            double[] yPositions = { 0, h / 2, h - 1 };
            foreach (var yPos in yPositions)
                AutomationGridCanvas.Children.Add(new Line { X1 = 0, X2 = w, Y1 = yPos, Y2 = yPos, Stroke = new SolidColorBrush(Color.FromRgb(40, 40, 40)), StrokeThickness = 1 });
        }

        /// <summary>
        /// Draw "Ruler" on top for the bars
        /// </summary>
        private void DrawRuler()
        {
            RulerCanvas.Children.Clear();
            var ts = _tempoMap.GetTimeSignatureAtTime((MidiTimeSpan)0);
            long tpb = _ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;
            for (long t = 0; t < RulerCanvas.Width / _tickPixelScale; t += _ticksPerQuarterNote)
            {
                double x = t * _tickPixelScale; bool isB = t % tpb == 0;
                RulerCanvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = isB ? 0 : 18, Y2 = 30, Stroke = Brushes.Gray, StrokeThickness = isB ? 1 : 0.5 });
                if (isB) RulerCanvas.Children.Add(new TextBlock { Text = $"{(t / tpb) + 1}", Foreground = Brushes.DimGray, FontSize = 12, Margin = new Thickness(x + 3, 2, 0, 0) });
            }
        }

        /// <summary>
        /// Draw octave-scale line we are using in XIV
        /// </summary>
        private void DrawMarkers()
        {
            MarkersCanvas.Children.Clear();
            foreach (int n in new[] { 84, 47 })
            {
                double y = (127 - n) * _noteHeight;
                MarkersCanvas.Children.Add(new Line { X1 = 0, X2 = EditorGrid.Width, Y1 = y, Y2 = y, Stroke = Brushes.Red, StrokeThickness = 1, Opacity = 0.5 });
            }
        }
    }
}
