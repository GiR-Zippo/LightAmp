/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    internal static class MidiBardConverterDrawHelper
    {
        // ── Frozen Pens ───────────────────────────────────────────────
        private static readonly Pen _penGridBeat = MakePen(Color.FromRgb(70, 70, 70), 0.5);
        private static readonly Pen _penGridSubdiv = MakePen(Color.FromRgb(30, 30, 30), 0.5);
        private static readonly Pen _penGridH = MakePen(Color.FromRgb(35, 35, 35), 0.5);
        private static readonly Pen _penRulerBar = MakePen(Colors.Gray, 1.0);
        private static readonly Pen _penRulerBeat = MakePen(Colors.Gray, 0.5);

        private static readonly Brush _brushRulerText =
            new SolidColorBrush(Colors.DimGray).Also(b => b.Freeze());
        private static readonly Typeface _rulerTypeface = new Typeface("Segoe UI");

        private static Pen MakePen(Color c, double thickness, double opacity = 1.0)
        {
            var p = new Pen(new SolidColorBrush(c) { Opacity = opacity }, thickness);
            p.Freeze();
            return p;
        }

        /// <summary>
        /// Draws the vertical/horizontal grid in the specified host.
        /// </summary>
        /// <param name="host">Target DrawingVisualHost</param>
        /// <param name="midiFile">Source file (for TempoMap / TimeSignature)</param>
        /// <param name="width">Width in pixels</param>
        /// <param name="height">Height in pixels</param>
        /// <param name="tickPixelScale">Pixels per tick</param>
        /// <param name="gridSnapTicks">Grid resolution in ticks</param>
        /// <param name="ticksPerQuarterNote">TPQ from MidiFile.TimeDivision</param>
        /// <param name="noteHeight">Height of a note line (for staff lines, 0 = none)</param>
        public static void DrawGrid(DrawingVisualHost host, MidiFile midiFile, double width, double height, double tickPixelScale, long gridSnapTicks, short ticksPerQuarterNote, double noteHeight = 0)
        {
            if (host == null || midiFile == null) return;

            var ts = midiFile.GetTempoMap().GetTimeSignatureAtTime((MidiTimeSpan)0);
            long tpb = ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;

            using DrawingContext dc = host.Open();

            // Horizontal Lines
            if (noteHeight > 0)
            {
                int rows = (int)(height / noteHeight) + 1;
                for (int i = 0; i <= rows; i++)
                    dc.DrawLine(_penGridH, new Point(0, i * noteHeight), new Point(width, i * noteHeight));
            }

            // Vert Lines
            for (long t = 0; t < width / tickPixelScale; t += gridSnapTicks)
            {
                double x = t * tickPixelScale;
                dc.DrawLine(t % tpb == 0 ? _penGridBeat : _penGridSubdiv,
                    new Point(x, 0), new Point(x, height));
            }
        }

        /// <summary>
        /// Draws the beat ruler in the specified host.
        /// </summary>
        /// <param name="host">Target DrawingVisualHost</param>
        /// <param name="midiFile">Source file</param>
        /// <param name="totalWidth">Total width in pixels</param>
        /// <param name="height">Ruler height in pixels (typically 30)</param>
        /// <param name="tickPixelScale">Pixels per tick</param>
        /// <param name="ticksPerQuarterNote">TPQ</param>
        /// <param name="dpi">PixelsPerDip for FormattedText</param>
        public static void DrawRuler(DrawingVisualHost host, MidiFile midiFile, double totalWidth, double height, double tickPixelScale, short ticksPerQuarterNote, double dpi)
        {
            if (host == null || midiFile == null) return;

            var ts = midiFile.GetTempoMap().GetTimeSignatureAtTime((MidiTimeSpan)0);
            long tpb = ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;

            double pixelsPerBar = tpb * tickPixelScale;
            int labelStep = 1;
            while (pixelsPerBar * labelStep < 60.0)
                labelStep *= 2;

            using DrawingContext dc = host.Open();

            for (long t = 0; t < totalWidth / tickPixelScale; t += ticksPerQuarterNote)
            {
                double x = t * tickPixelScale;
                bool isBar = t % tpb == 0;
                long barNum = (t / tpb) + 1;

                dc.DrawLine(isBar ? _penRulerBar : _penRulerBeat,
                    new Point(x, isBar ? 0 : height * 0.6), new Point(x, height));

                if (isBar && (barNum == 1 || (barNum - 1) % labelStep == 0))
                {
                    var ft = new FormattedText(
                        $"{barNum}",
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        _rulerTypeface, 12,
                        _brushRulerText, dpi);
                    dc.DrawText(ft, new Point(x + 3, 2));
                }
            }
        }
    }
}
