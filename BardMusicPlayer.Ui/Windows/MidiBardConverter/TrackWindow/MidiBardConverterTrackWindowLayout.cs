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

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    /// <summary>
    /// The layout stuff – Grid, Ruler, Markers und AutomationGrid werden per DrawingVisual
    /// gerendert (kein Line/TextBlock-Objekt im Canvas).
    /// </summary>
    public partial class MidiBardConverterTrackWindow
    {
        // DrawingVisual-Hosts
        private DrawingVisualHost _gridVisual;

        private DrawingVisualHost _rulerVisual;
        private DrawingVisualHost _markersVisual;
        private DrawingVisualHost _automationGridVisual;

        // Frozen Pens / Brushes
        private static readonly Pen _penMarker = MakePen(Colors.Red, 1.0, opacity: 0.5);
        private static readonly Pen _penAutoGridBeat = MakePen(Color.FromRgb(70, 70, 70), 0.5);
        private static readonly Pen _penAutoGridSub = MakePen(Color.FromRgb(35, 35, 35), 0.5);
        private static readonly Pen _penAutoGridH = MakePen(Color.FromRgb(40, 40, 40), 1.0);

        private static Pen MakePen(Color c, double thickness, double opacity = 1.0)
        {
            var p = new Pen(new SolidColorBrush(c) { Opacity = opacity }, thickness);
            p.Freeze();
            return p;
        }

        // RefreshLayout
        private void RefreshLayout()
        {
            double w = Math.Max(MainScroll.ActualWidth, (_maxTick * _tickPixelScale) + 500);
            double h = 128 * _noteHeight;

            EditorGrid.Width = w;
            EditorGrid.Height = h;
            NotesCanvas.Width = w;
            NotesCanvas.Height = h;

            EnsureVisualHosts();

            _gridVisual.Width = w; _gridVisual.Height = h;
            _rulerVisual.Width = w; _rulerVisual.Height = 30;
            _markersVisual.Width = w; _markersVisual.Height = h;

            RulerCanvas.Width = w;
            MarkersCanvas.Width = w;
            AutomationCanvas.Width = w;

            DrawGrid();
            DrawRuler();
            DrawMarkers();
            DrawAutomation();
            DrawAutomationGrid();
            RenderNotes();
        }

        private void EnsureVisualHosts()
        {
            if (_gridVisual == null)
            {
                _gridVisual = new DrawingVisualHost();
                GridCanvas.Children.Add(_gridVisual);
            }
            if (_rulerVisual == null)
            {
                _rulerVisual = new DrawingVisualHost();
                RulerCanvas.Children.Add(_rulerVisual);
            }
            if (_markersVisual == null)
            {
                _markersVisual = new DrawingVisualHost();
                MarkersCanvas.Children.Add(_markersVisual);
            }
            if (_automationGridVisual == null)
            {
                _automationGridVisual = new DrawingVisualHost();
                AutomationGridCanvas.Children.Add(_automationGridVisual);
            }
        }

        private void BuildPianoKeys()
        {
            PianoKeysStack.Children.Clear();
            int[] blacks = { 1, 3, 6, 8, 10 };
            for (int i = 127; i >= 0; i--)
            {
                var b = new Border
                {
                    Height = _noteHeight,
                    Background = blacks.Contains(i % 12)
                                        ? new SolidColorBrush(Color.FromRgb(40, 40, 40))
                                        : Brushes.White,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };
                if (i % 12 == 0)
                    b.Child = new TextBlock
                    {
                        Text = "C" + ((i / 12) - 1),
                        FontSize = 12,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.Black,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                PianoKeysStack.Children.Add(b);
            }
        }

        private void DrawGrid()
        {
            MidiBardConverterDrawHelper.DrawGrid(
                _gridVisual, _midiFile,
                EditorGrid.Width, EditorGrid.Height,
                _tickPixelScale, _gridSnapTicks, _ticksPerQuarterNote,
                noteHeight: _noteHeight);
        }

        private void DrawRuler()
        {
            MidiBardConverterDrawHelper.DrawRuler(
                _rulerVisual, _midiFile,
                RulerCanvas.Width, 30,
                _tickPixelScale, _ticksPerQuarterNote,
                dpi: VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private void DrawMarkers()
        {
            if (_markersVisual == null) return;

            double w = EditorGrid.Width;
            using DrawingContext dc = _markersVisual.Open();

            foreach (int n in new[] { 84, 47 })
            {
                double y = (127 - n) * _noteHeight;
                dc.DrawLine(_penMarker, new Point(0, y), new Point(w, y));
            }
        }

        private void DrawAutomationGrid()
        {
            if (_automationGridVisual == null) return;

            double w = AutomationCanvas.ActualWidth;
            double h = AutomationCanvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            _automationGridVisual.Width = w;
            _automationGridVisual.Height = h;

            var ts = _midiFile.GetTempoMap().GetTimeSignatureAtTime((MidiTimeSpan)0);
            long tpb = _ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;

            using DrawingContext dc = _automationGridVisual.Open();

            long maxTicks = (long)(w / _tickPixelScale);
            for (long t = 0; t <= maxTicks; t += _gridSnapTicks)
            {
                double x = t * _tickPixelScale;
                dc.DrawLine(t % tpb == 0 ? _penAutoGridBeat : _penAutoGridSub,
                    new Point(x, 0), new Point(x, h));
            }

            foreach (double y in new[] { 0.0, h / 2, h - 1 })
                dc.DrawLine(_penAutoGridH, new Point(0, y), new Point(w, y));
        }
    }

    /// <summary>
    /// A lightweight FrameworkElement wrapper for a DrawingVisual. Replaces Canvas and numerous
    /// line and shape objects with a single render pass.
    /// </summary>
    internal class DrawingVisualHost : FrameworkElement
    {
        private readonly DrawingVisual _visual = new DrawingVisual();

        public DrawingVisualHost()
        {
            AddVisualChild(_visual);
            IsHitTestVisible = false;
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _visual;

        public DrawingContext Open() => _visual.RenderOpen();
    }

    // Extension helper
    internal static class BrushExtensions
    {
        public static T Also<T>(this T obj, Action<T> action)
        { action(obj); return obj; }
    }
}