/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    /// <summary>
    /// Virtualised rendering of all notes via a single DrawingVisual. Works directly with
    /// Melanchall’s Note objects – no longer uses its own NoteData class.
    /// </summary>
    public class NoteVisualHost : FrameworkElement
    {
        private readonly DrawingVisual _visual = new DrawingVisual();

        private static readonly SolidColorBrush DefaultBrush;
        private static readonly SolidColorBrush SelectedBrush;
        private static readonly Pen DefaultPen;
        private static readonly Pen SelectedPen;

        static NoteVisualHost()
        {
            DefaultBrush = new SolidColorBrush(Color.FromRgb(58, 134, 255));
            SelectedBrush = new SolidColorBrush(Colors.Orange);
            DefaultPen = new Pen(Brushes.Transparent, 0.5);
            SelectedPen = new Pen(Brushes.White, 1.5);
            DefaultBrush.Freeze();
            SelectedBrush.Freeze();
            DefaultPen.Freeze();
            SelectedPen.Freeze();
        }

        public NoteVisualHost()
        {
            AddVisualChild(_visual);
            Focusable = true;
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _visual;

        /// <summary>
        /// Redraws all notes. Only notes in viewport are rendered
        /// </summary>
        public void Render(IReadOnlyList<Note> notes, HashSet<Note> selectedNotes, double tickPixelScale, double noteHeight, Rect viewport)
        {
            using DrawingContext dc = _visual.RenderOpen();

            double cullLeft = viewport.Left - 10;
            double cullRight = viewport.Right + 10;
            double cullTop = viewport.Top - noteHeight;
            double cullBot = viewport.Bottom + noteHeight;

            foreach (Note note in notes)
            {
                double x = note.Time * tickPixelScale;
                double y = (127 - (int)note.NoteNumber) * noteHeight;
                double w = Math.Max(2, note.Length * tickPixelScale);
                double h = noteHeight - 1;
                if (h < 0)
                    h = h * -1;
                if (x + w < cullLeft || x > cullRight) continue;
                if (y + h < cullTop || y > cullBot) continue;

                bool sel = selectedNotes.Contains(note);
                dc.DrawRoundedRectangle(
                    sel ? SelectedBrush : DefaultBrush,
                    sel ? SelectedPen : DefaultPen,
                    new Rect(x, y, w, h), 1.5, 1.5);
            }
        }

        /// <summary>
        /// Hit test: returns the note at the canvas position (the topmost note takes precedence).
        /// </summary>
        public Note HitTest(Point pos, IReadOnlyList<Note> notes, double tickPixelScale, double noteHeight)
        {
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                Note n = notes[i];
                double x = n.Time * tickPixelScale;
                double y = (127 - (int)n.NoteNumber) * noteHeight;
                double w = Math.Max(2, n.Length * tickPixelScale);
                if (new Rect(x, y, w, noteHeight).Contains(pos))
                    return n;
            }
            return null;
        }

        /// <summary>
        /// Resize handle test: true = left edge, false = right edge, null = no edge.
        /// </summary>
        public bool? HitTestResizeHandle(Point pos, Note note, double tickPixelScale, double noteHeight, double handleWidth = 7.0)
        {
            double x = note.Time * tickPixelScale;
            double w = Math.Max(2, note.Length * tickPixelScale);
            if (pos.X >= x && pos.X <= x + handleWidth) return true;
            if (pos.X >= x + w - handleWidth && pos.X <= x + w) return false;
            return null;
        }
    }
}