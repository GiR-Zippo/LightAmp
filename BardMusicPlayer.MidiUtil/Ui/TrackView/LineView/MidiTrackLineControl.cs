using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Collections.Generic;

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using BardMusicPlayer.MidiUtil.Managers;

namespace BardMusicPlayer.MidiUtil.Ui.TrackView
{
    public class MidiTrackLineControl
    {

        #region CTOR

        public MidiTrackLineView view { get; set; }
        public MidiTrackLineModel model { get; set; }

        public MidiTrackLineControl(MidiTrackLineModel model, MidiTrackLineView view)
        {
            this.model = model;
            this.view = view;
            Init();
        }

        public void Init()
        {
            // track body
            DrawPianoRoll();
            DrawMidiEvents();
        }

        public event EventHandler<TrackChunk> TrackFocused;

        public static readonly DependencyProperty AttachedNoteProperty =
            DependencyProperty.RegisterAttached(
                "AttachedNote",
                typeof(Note),
                typeof(MidiTrackLineControl)
        );

        #endregion

        #region INTERACTIONS

        internal void TrackGotFocus(object sender, RoutedEventArgs e)
        {
            if (TrackFocused == null)
                return;
            TrackFocused.Invoke(sender, model.Track);
        }

        internal void InsertNote(double start, double end, int noteIndex)
        {
            if (MidiManager.Instance.IsPlaying)
                return;

            // Generate Midi Note
            int channel = 0;
            int velocity = UiManager.Instance.plotVelocity;
            var msgs = MidiManager.Instance.CreateNote(
                channel,
                noteIndex,
                model.Track,
                start,
                end,
                velocity);
            // Draw it on MidiRoll
            DrawNote(start, end, noteIndex, msgs);
            UiManager.Instance.mainWindow.Ctrl.UpdateTrackView(model.Track);
        }

        #endregion

        #region DRAW GRID

        public void DrawPianoRoll()
        {
            view.TrackNotes.Children.Clear();
            int noteWithoutOctave;
            Brush currentColor = Brushes.White;
            for (int i = 0; i < 128; i++)
            {
                // identify note
                noteWithoutOctave = i % 12;
                // choose note color
                switch (noteWithoutOctave)
                {
                    case 0: currentColor = Brushes.White; break;
                    case 1: currentColor = Brushes.Black; break;
                    case 2: currentColor = Brushes.White; break;
                    case 3: currentColor = Brushes.Black; break;
                    case 4: currentColor = Brushes.White; break;
                    case 5: currentColor = Brushes.White; break;
                    case 6: currentColor = Brushes.Black; break;
                    case 7: currentColor = Brushes.White; break;
                    case 8: currentColor = Brushes.Black; break;
                    case 9: currentColor = Brushes.White; break;
                    case 10: currentColor = Brushes.Black; break;
                    case 11: currentColor = Brushes.White; break;
                }
                if (i == 48) currentColor = Brushes.Red;
                if (i == 60) currentColor = Brushes.Yellow;
                if (i == 69) currentColor = Brushes.Cyan;
                // make rectangle
                Rectangle rec = new Rectangle
                {
                    Width = 15,
                    Height = model.CellHeigth,
                    Fill = currentColor,
                    Stroke = Brushes.Gray,
                    StrokeThickness = .5f
                };
                // place rectangle
                Canvas.SetLeft(rec, 0);
                Canvas.SetTop(rec, (127 - i) * model.CellHeigth);
                // piano toucn on rectangle
                int j = i;
                rec.MouseLeftButtonDown += (s, e) => MidiManager.Instance.Playback(true, j);
                rec.MouseLeftButtonUp += (s, e) => MidiManager.Instance.Playback(false, j);
                rec.MouseLeave += (s, e) => MidiManager.Instance.Playback(false, j);
                // add it to the control
                view.TrackNotes.Children.Add(rec);
                view.TrackNotes.Height = 127 * model.CellHeigth;
                view.TrackBody.Height = 127 * model.CellHeigth;
            }
        }

        #endregion

        #region DRAW MIDI EVENT

        public void DrawMidiEvents()
        {
            view.TrackBody.Children.Clear();
            DrawNotes();
        }

        private void DrawNotes()
        {
            TempoMap tempo = MidiManager.Instance.GetTempoMap();
            foreach (Note note in model.Track.GetNotes())
            {
                DrawNote(
                    (double)note.GetTimedNoteOnEvent().TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds / 1000 / model.DAWhosReso,
                    (double)note.GetTimedNoteOffEvent().TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds / 1000 / model.DAWhosReso,
                    note.NoteNumber,
                    note
                );
            }
        }

        private void DrawNote(double start, double end, int noteIndex, Note note)
        {
            Rectangle rec = new Rectangle();
            try
            {
                rec.Width = (end - start) * model.CellWidth;
            }
            catch
            {
                rec.Width = 1;
            }
            rec.Height = model.CellHeigth;
            rec.Fill = Brushes.Red;
            rec.Stroke = Brushes.DarkRed;
            rec.StrokeThickness = .5f;
            Canvas.SetLeft(rec, start * model.CellWidth);
            Canvas.SetTop(rec, ((127 - noteIndex) * model.CellHeigth));
            rec.IsMouseDirectlyOverChanged += Rec_IsMouseDirectlyOverChanged;
            rec.MouseLeftButtonDown += NoteLeftDown;
            rec.MouseRightButtonDown += NoteRightDown;
            rec.SetValue(AttachedNoteProperty, note);
            view.TrackBody.Children.Add(rec);
        }

        public List<object> SelectedNotes = new List<object>();
        public void ClearSelection()
        {
            foreach (var obj in SelectedNotes)
            {
                Rectangle rec = (Rectangle)obj;
                rec.Fill = Brushes.Red;
            };
            SelectedNotes.Clear();
        }

        public void DeleteSelected()
        {
            foreach (var obj in SelectedNotes)
            {
                Rectangle rec = (Rectangle)obj;
                Note noteOn = (Note)rec.GetValue(AttachedNoteProperty);
                view.TrackBody.Children.Remove(rec);

                MidiManager.Instance.DeleteNote(model.Track, noteOn);
                UiManager.Instance.mainWindow.Ctrl.UpdateTrackView(model.Track);
            };
            SelectedNotes.Clear();
        }

        private void Rec_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Rectangle rec = (Rectangle)sender;
                rec.Fill = Brushes.Gray;
                SelectedNotes.Add(sender); 
            }
        }

        private void NoteLeftDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (e.ClickCount == 1)
            {
                foreach (var obj in SelectedNotes)
                {
                    Rectangle rec = (Rectangle)obj;
                    rec.Fill = Brushes.Red;
                };
                SelectedNotes.Clear();
                Console.WriteLine();
            }

            if (e.ClickCount > 1)
            {
                if (MidiManager.Instance.IsPlaying) return;
                Rectangle rec = (Rectangle)sender;
                Note noteOn = (Note)rec.GetValue(AttachedNoteProperty);
                view.TrackBody.Children.Remove(rec);

                MidiManager.Instance.DeleteNote(model.Track, noteOn);
                UiManager.Instance.mainWindow.Ctrl.UpdateTrackView(model.Track);
            }
        }

        private void NoteRightDown(object sender, MouseButtonEventArgs e)
        {
        }

        #endregion
    }
}
