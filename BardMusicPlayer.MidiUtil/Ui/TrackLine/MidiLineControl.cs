using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.MidiUtil.Managers;

namespace BardMusicPlayer.MidiUtil.Ui.TrackLine
{
    public class MidiLineControl
    {

        #region CTOR

        public MidiLineView view { get; set; }
        public MidiLineModel model { get; set; }
        
        public MidiLineControl (MidiLineModel model, MidiLineView view)
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
            for (int i=0; i != 128; i++)
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
        public event EventHandler<TrackChunk> TrackMergeUp;
        public event EventHandler<TrackChunk> TrackMergeDown;

        public static readonly DependencyProperty AttachedNoteProperty =
            DependencyProperty.RegisterAttached(
                "AttachedNote",
                typeof(Note),
                typeof(MidiLineControl)
        );

        #endregion

        #region INTERACTIONS

        internal void TrackGotFocus(object sender, RoutedEventArgs e)
        {
            if (TrackFocused == null)
                return;
            TrackFocused.Invoke(sender, model.Track);
        }

        internal void MergeUp(object sender, RoutedEventArgs e)
        {
            if (TrackFocused == null)
                return;
            TrackFocused.Invoke(sender, model.Track);
            TrackMergeUp.Invoke(sender, model.Track);
        }

        internal void MergeDown(object sender, RoutedEventArgs e)
        {
            if (TrackFocused == null)
                return;
            TrackFocused.Invoke(sender, model.Track);
            TrackMergeDown.Invoke(sender, model.Track);
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
            DrawNote(start,end,noteIndex, msgs);
        }

        #endregion

        #region DRAW GRID

        public void DrawPianoRoll()
        {
            view.TrackNotes.Children.Clear();
            int noteWithoutOctave;
            Brush currentColor = Brushes.White;
            for (int i=0;i<128;i++)
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
                Canvas.SetTop(rec, (127 - i)*model.CellHeigth);
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
                    (double)note.GetTimedNoteOnEvent().TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds/1000 / model.DAWhosReso,
                    (double)note.GetTimedNoteOffEvent().TimeAs<MetricTimeSpan>(tempo).TotalMicroseconds/1000 / model.DAWhosReso,
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
                rec.Width = (end-start)* model.CellWidth;
            }
            catch
            {
                rec.Width = 1;
            }
            rec.Height = model.CellHeigth;
            rec.Fill = Brushes.Red;
            rec.Stroke = Brushes.DarkRed;
            rec.StrokeThickness = .5f;
            Canvas.SetLeft(rec,start*model.CellWidth);
            Canvas.SetTop(rec, ((127 - noteIndex)*model.CellHeigth));
            rec.SetValue(AttachedNoteProperty, note);
            view.TrackBody.Children.Add(rec);
        }
        #endregion
    }
}
