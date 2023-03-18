using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.MidiUtil.Managers;
using System.Threading.Tasks;

namespace BardMusicPlayer.MidiUtil.Ui.TrackView
{
    public class MidiTrackEventControl
    {

        #region CTOR

        public MidiTrackEventView view { get; set; }
        public MidiTrackEventModel model { get; set; }

        public MidiTrackEventControl(MidiTrackEventModel model, MidiTrackEventView view)
        {
            this.model = model;
            this.view = view;
            Init();
        }

        public void Init()
        {
            // track body
            DrawMidiEvents();
        }

        public event EventHandler<TrackChunk> TrackFocused;

        public static readonly DependencyProperty AttachedNoteProperty =
            DependencyProperty.RegisterAttached(
                "AttachedEvent",
                typeof(Note),
                typeof(MidiTrackEventControl)
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


            foreach (TimedEvent ev in model.Track.GetTimedEvents())
            {
                if (ev.Event.EventType == MidiEventType.ProgramChange)
                {
                    var prog = ev.Event as ProgramChangeEvent;
                    DrawNote(
                    (double)(ev.TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds +2) / 1000 / model.DAWhosReso,
                    (double)(ev.TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds + 3) / 1000 / model.DAWhosReso,
                    prog.ProgramNumber,
                    null);
                }
                /*DrawNote(
                    (double)prog..TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds / 1000 / model.DAWhosReso,
                    (double)note.GetTimedNoteOffEvent().TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds / 1000 / model.DAWhosReso,
                    note.NoteNumber,
                    note
                );*/
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
