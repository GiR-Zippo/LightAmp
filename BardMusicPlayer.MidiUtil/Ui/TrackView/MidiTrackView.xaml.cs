using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BardMusicPlayer.MidiUtil.Managers;
using BardMusicPlayer.Quotidian.Structs;
using System.Text.RegularExpressions;
using Melanchall.DryWetMidi.Core;
using BardMusicPlayer.Transmogrify.Song.Manipulation;

namespace BardMusicPlayer.MidiUtil.Ui.TrackView
{
    /// <summary>
    /// Interaktionslogik für MidiTrackView.xaml
    /// </summary>
    public partial class MidiTrackView : Window
    {
        public enum EditState
        {
            NoteEdit = 0,
            Select = 1,
            None
        };
        public EditState editState { get; set; } = EditState.NoteEdit;
        ContextMenu theMenu = null;

        private MidiTrackLineView TrackLineView { get; set; } = null;
        private MidiTrackEventView TrackEventView { get; set; } = null;
        public MidiTrackView(TrackChunk lineView)
        {
            TrackLineView = new MidiTrackLineView(lineView);
            TrackEventView = new MidiTrackEventView(lineView);

            InitializeComponent();
            CreateContextMenu();

            YZoom = 3.00f;
            this.PreviewMouseWheel += MidiTrackView_MouseWheel;
            this.PreviewMouseDown += MidiTrackView_PreviewMouseDown;
            MasterScroller.Scroll += new System.Windows.Controls.Primitives.ScrollEventHandler(UpdateHorizontalScrolling);

            var trackLine = new Frame()
            {
                Content = TrackLineView
            };
            TracksPanel.Children.Add(trackLine);
            TrackLineView = TrackLineView;

            var eventsLine = new Frame()
            {
                Content = TrackEventView
            };
            EventPanel.Children.Add(eventsLine);

            //Init the Header
            Init();
        }

        #region ContextMenu
        private void MidiTrackView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                foreach (var item in theMenu.Items)
                {
                    var i = item as MenuItem;
                    if ((string)i.Header == "Edit")
                        i.IsChecked = editState == EditState.NoteEdit ? true : false;
                    if ((string)i.Header == "Select")
                        i.IsChecked = editState == EditState.Select ? true : false;
                }
                theMenu.IsOpen = true;
            }
        }

        private void CreateContextMenu()
        {
            theMenu = new ContextMenu();
            MenuItem mia = new MenuItem();
            mia.Header = "Edit";
            mia.IsChecked = editState == EditState.NoteEdit ? true : false;
            mia.Click += Edit_Click;
            theMenu.Items.Add(mia);

            MenuItem mib = new MenuItem();
            mib.Header = "Select";
            mib.IsChecked = editState == EditState.Select ? true : false;
            mib.Click += Select_Click;
            theMenu.Items.Add(mib);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            TrackLineView.selectionBox.Visibility = Visibility.Hidden;
            TrackLineView.selectionBox.IsEnabled = false;
            TrackLineView.editState = EditState.NoteEdit;
            editState = EditState.NoteEdit;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            TrackLineView.selectionBox.Visibility = Visibility.Visible;
            TrackLineView.selectionBox.IsEnabled = true;
            TrackLineView.editState = EditState.Select;
            editState = EditState.Select;
        }
        #endregion

        public void Init()
        {
            // track header
            FillInstrumentBox();

            ComboInstruments.SelectedIndex = TrackManipulations.GetInstrument(TrackLineView.Model.Track);
            ChannelId.Content = TrackManipulations.GetChannelNumber(TrackLineView.Model.Track) + 1;
            //Check if the instrument is "None"
            if (ComboInstruments.Items.GetItemAt(ComboInstruments.SelectedIndex) is ComboBoxItem it)
            {
                if (it.Content.ToString() == "None")
                {
                    var trackName = TrackManipulations.GetTrackName(TrackLineView.Model.Track);
                    Regex rex = new Regex(@"^([A-Za-z _]+)([-+]\d)?");
                    if (rex.Match(trackName) is Match match)
                    {
                        if (!string.IsNullOrEmpty(match.Groups[1].Value))
                        {
                            var num = Instrument.Parse(match.Groups[1].Value).MidiProgramChangeCode;
                            ComboInstruments.SelectedIndex = num;
                            TrackManipulations.SetInstrument(TrackLineView.Model.Track, num);
                        }
                    }
                }
            }
            if (!Instrument.ParseByProgramChange(ComboInstruments.SelectedIndex).Equals(Instrument.None))
                TrackManipulations.SetTrackName(TrackLineView.Model.Track, Instrument.ParseByProgramChange(ComboInstruments.SelectedIndex).Name);

            //Check if we got a drum track
            if (ChannelId.Content.ToString() == "10")
                TrackName.Content = TrackManipulations.GetTrackName(TrackLineView.Model.Track) + " or Drums";
            else
                TrackName.Content = TrackManipulations.GetTrackName(TrackLineView.Model.Track);
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
                ComboInstruments.Items.Add(
                    new ComboBoxItem()
                    {
                        Content = instrument.Value
                    }
                );
            }
            ComboInstruments.SelectedIndex = 0;
        }


        private void MidiTrackView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!this.IsActive)
                return;

            int value = e.Delta / 120;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                TranslateTracks(e.Delta / 5);
            }
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                ZoomTracksX(value);
            }
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                ZoomTracksY(value);

            }
        }

        /// <summary>
        /// Update horizontal scrolling
        /// </summary>
        public void UpdateHorizontalScrolling(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            if (TracksPanel.Children.Count <= 0)
                return;
            XOffset = MasterScroller.Value;
            UpdateLayout();
        }

        private void InstrumentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ComboInstruments.IsDropDownOpen)
            {
                this.ComboInstruments.IsDropDownOpen = false;
                TrackManipulations.SetInstrument(TrackLineView.Model.Track, ComboInstruments.SelectedIndex);
                TrackManipulations.SetTrackName(TrackLineView.Model.Track, Instrument.ParseByProgramChange(ComboInstruments.SelectedIndex).Name);
                UiManager.Instance.mainWindow.Ctrl.InitTracks();
                TrackLineView.Ctrl.Init();
                Init();
            }
        }

        #region ZOOM

        internal void TranslateTracks(int delta)
        {
            XOffset += delta;
        }

        /// <summary>
        /// Pan tracks
        /// </summary>
        private double xOffset = 0;
        public double XOffset
        {
            get { return xOffset; }
            set
            {
                if (value < 0)
                    value = 0;
                xOffset = value;
                foreach (var obj in TracksPanel.Children)
                {
                    Frame track = obj as Frame;
                    if (track == null)
                        continue;
                    ((MidiTrackLineView)track.Content).Model.XOffset = xOffset;
                }
                MasterScroller.Value = xOffset;
            }
        }

        internal void ZoomTracksX(int delta)
        {
            XZoom += (float)delta / 7;
        }

        /// <summary>
        /// Zoom tracks
        /// </summary>
        private float xZoom = 0.01f;
        public float XZoom
        {
            get { return xZoom; }
            set
            {
                if (value < 0.01f)
                    value = 0.01f;
                xZoom = value;

                foreach (var obj in TracksPanel.Children)
                {
                    Frame track = obj as Frame;
                    if (track == null)
                        continue;

                    ((MidiTrackLineView)track.Content).Model.CellWidth = XZoom;
                }
                MasterScroller.Maximum = (MidiManager.Instance.GetLength() * XZoom);
                if (MasterScroller.Value > MasterScroller.Maximum)
                    MasterScroller.Value = 0;
            }
        }

        internal void ZoomTracksY(int delta)
        {
            if (MidiManager.Instance.IsPlaying) return;
            YZoom += (float)delta / 10;
        }

        private float yZoom = 1;
        internal double marginPercent = 0.25;
        internal double absoluteTimePosition = 0;
        internal double relativeTimePosition = 0;


        public const int touchOffset = 15;

        public float YZoom
        {
            get { return yZoom; }
            set
            {
                if (value < .1f) 
                    value = .1f;
                yZoom = value;
                foreach (var obj in TracksPanel.Children)
                {
                    Frame track = obj as Frame;
                    if (track == null)
                        continue;
                    Console.WriteLine(YZoom);
                    ((MidiTrackLineView)track.Content).Model.CellHeigth = YZoom;
                }
            }
        }
        #endregion
    }
}
