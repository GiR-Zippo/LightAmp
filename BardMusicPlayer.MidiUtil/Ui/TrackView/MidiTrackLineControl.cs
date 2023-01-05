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
            // track header
            FillInstrumentBox();

            view.ComboInstruments.SelectedIndex = MidiManager.Instance.GetInstrument(model.Track);
            view.ChannelId.Content = MidiManager.Instance.GetChannelNumber(model.Track) + 1;
            //Check if the instrument is "None"
            if (view.ComboInstruments.Items.GetItemAt(view.ComboInstruments.SelectedIndex) is ComboBoxItem it)
            {
                if (it.Content.ToString() == "None")
                {
                    var trackName = MidiManager.Instance.GetTrackName(model.Track);
                    Regex rex = new Regex(@"^([A-Za-z _]+)([-+]\d)?");
                    if (rex.Match(trackName) is Match match)
                    {
                        if (!string.IsNullOrEmpty(match.Groups[1].Value))
                        {
                            var num = Instrument.Parse(match.Groups[1].Value).MidiProgramChangeCode;
                            view.ComboInstruments.SelectedIndex = num;
                            MidiManager.Instance.SetInstrument(model.Track, num);
                        }
                    }
                }
            }
            if (!Instrument.ParseByProgramChange(view.ComboInstruments.SelectedIndex).Equals(Instrument.None))
                MidiManager.Instance.SetTrackName(model.Track, Instrument.ParseByProgramChange(view.ComboInstruments.SelectedIndex).Name);

            //Check if we got a drum track
            if (view.ChannelId.Content.ToString() == "10")
                view.TrackName.Content = MidiManager.Instance.GetTrackName(model.Track) + " or Drums";
            else
                view.TrackName.Content = MidiManager.Instance.GetTrackName(model.Track);

            // track body
            DrawPianoRoll();
            DrawMidiEvents();
        }

        private void FillInstrumentBox()
        {
            Dictionary<int, string> instlist = new Dictionary<int, string>();
            for (int i = 0; i != 128; i++)
                instlist.Add(i, "None");

            foreach (Instrument instrument in Instrument.All)
            {
                instlist[instrument.MidiProgramChangeCode] = instrument.Name;
            }

            foreach (var instrument in instlist)
            {
                view.ComboInstruments.Items.Add(
                    new ComboBoxItem()
                    {
                        Content = instrument.Value
                    }
                );
            }
            view.ComboInstruments.SelectedIndex = 0;
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


            //note.TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds;

            /*int i = 0;
            foreach (var midiEvent in model.Track.Iterator())
            {
                if (midiEvent.MidiMessage.MessageType == MessageType.Channel)
                {
                    DrawChannelMsg(midiEvent);
                }
                i++;
            }*/
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

        private void DrawChannelMsg(MidiEvent midiEvent)
        {
            /*int status = midiEvent.MidiMessage.Status;
            int position = midiEvent.AbsoluteTicks;
            // NOTE OFF
            if (status >= (int)ChannelCommand.NoteOff &&
                status <= (int)ChannelCommand.NoteOff + ChannelMessage.MidiChannelMaxValue)
            {
                int noteIndex = (int)midiEvent.MidiMessage.GetBytes()[1];
                if (model.LastNotesOn.TryGetValue(noteIndex, out Tuple<int, MidiEvent> onPosition))
                {
                    DrawNote(
                        (double)onPosition.Item1 / model.DAWhosReso,
                        (double)position / model.DAWhosReso,
                        noteIndex,
                        onPosition.Item2,
                        midiEvent
                    );
                    model.LastNotesOn.Remove(noteIndex);
                }
            }
            // NOTE ON
            if (status >= (int)ChannelCommand.NoteOn &&
                status <= (int)ChannelCommand.NoteOn + ChannelMessage.MidiChannelMaxValue)
            {
                int noteIndex = (int)midiEvent.MidiMessage.GetBytes()[1];
                int velocity = (int)midiEvent.MidiMessage.GetBytes()[2];
                if (velocity > 0)
                {
                    model.LastNotesOn[noteIndex] = new Tuple<int, MidiEvent>(position, midiEvent);
                }
                else
                {
                    if (model.LastNotesOn.TryGetValue(noteIndex, out Tuple<int, MidiEvent> onPosition))
                    {
                        DrawNote(onPosition.Item1, position, noteIndex, onPosition.Item2, midiEvent);
                        model.LastNotesOn.Remove(noteIndex);
                    }
                }
            }
            // ProgramChange
            if (status >= (int)ChannelCommand.ProgramChange &&
                status <= (int)ChannelCommand.ProgramChange + ChannelMessage.MidiChannelMaxValue)
            {
                model.MidiInstrument = (int)midiEvent.MidiMessage.GetBytes()[1];
            }*/
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
            rec.MouseLeftButtonDown += NoteLeftDown;
            rec.MouseRightButtonDown += NoteRightDown;
            rec.SetValue(AttachedNoteProperty, note);
            view.TrackBody.Children.Add(rec);
        }

        private void NoteLeftDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
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
