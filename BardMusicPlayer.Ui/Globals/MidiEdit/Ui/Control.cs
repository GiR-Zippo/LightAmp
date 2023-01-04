using Sanford.Multimedia.Midi;
using System;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using BardMusicPlayer.Ui.MidiEdit.Managers;
using BardMusicPlayer.Ui.MidiEdit.Ui.TrackLine;
using BardMusicPlayer.Ui.MidiEdit.Utils.TrackExtensions;

namespace BardMusicPlayer.Ui.MidiEdit.Ui
{
    public class Control
    {
        #region CTOR

        readonly Model model;
        readonly MidiEditWindow view;

        public Control(Model model, MidiEditWindow view)
        {
            this.model = model;
            this.view = view;
            model.TracksPanel = view.TracksPanel;
            //MidiManager.Instance.Timer.Tick += Update;
        }

        public void InitView()
        {
            view.TracksPanel.Background = Brushes.Transparent;
            view.TracksPanel.MouseWheel += view.HandleWheel;
            view.Title = model.ProjectName;
            view.MasterScroller.Scroll += new System.Windows.Controls.Primitives.ScrollEventHandler(ManualScroll);
        }

        #endregion
                    
        #region MENU GESTION

        public void Open(string fileName)
        {
            MidiManager.Instance.OpenFile(fileName);
        }

        public void Save(string fileName)
        {
            MidiManager.Instance.SaveFile(fileName);
        }
        #endregion

        #region Scroll

        public void Update(object sender, EventArgs e)
        {
            view.Dispatcher.BeginInvoke(new Action(() => view.TimeUpdate()));
        }
        

        internal void ManualScroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            view.UpdateHorizontalScrolling();
        }

        #endregion

        #region TRACK GESTION

        public void InitTracks()
        {
            MidiManager.Instance.Tracks.First().ResetExtentions(); //Reset the extentions list

            view.TracksPanel.Children.Clear();
            view.TracksPanel.RowDefinitions.Clear();
            int i = 0;
            foreach (Track track in MidiManager.Instance.Tracks) 
            {
                MidiLineView lineView = InitTrackLine(i,track);
                AddTrackGridRow(i,lineView);
                AddSeparatorGridRow(i);
                i++;
            }
            view.TracksPanel.RowDefinitions.Add(
               new RowDefinition()
               {
                   Height = new GridLength(400, GridUnitType.Pixel)
               }
           );

        }

        private MidiLineView InitTrackLine(int rowIndex, Track track)
        {
            track.Id();
            //track.Channel = rowIndex;
            MidiLineView lineView = new MidiLineView(track);
            lineView.Ctrl.TrackFocused += FocusTrack;
            lineView.Ctrl.TrackMergeUp += TrackMergeUp;
            lineView.Ctrl.TrackMergeDown += TrackMergeDown;
            return lineView;
        }

        private void AddSeparatorGridRow(int rowIndex)
        {
            // make row
            view.TracksPanel.RowDefinitions.Add(
                new RowDefinition()
                {
                    Height = new GridLength(3, GridUnitType.Pixel)
                }
            );
            // add separator in row
            var separator = new GridSplitter()
            {
                Height = 3,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = Brushes.Black,
            };
            view.TracksPanel.Children.Add(separator);
            Grid.SetRow(separator, 2 * rowIndex + 1);
        }

        private void AddTrackGridRow(int rowIndex, MidiLineView lineView)
        {
            // make row
            view.TracksPanel.RowDefinitions.Add(
                new RowDefinition()
                {
                    Height = new GridLength(UiManager.Instance.TrackHeightDefault, GridUnitType.Pixel),
                    MaxHeight = UiManager.Instance.TrackHeightMax,
                    MinHeight = UiManager.Instance.TrackHeightMin,
                }
            );
            // add trackline in row
            var trackLine = new Frame()
            {
                Content = lineView
            };
            view.TracksPanel.Children.Add(trackLine);
            Grid.SetRow(trackLine, 2 * rowIndex);
        }

        public void AddTrack()
        {
            MidiManager.Instance.AddTrack();
            InitTracks();
        }

        internal void RemoveTrack()
        {
            if (!MidiManager.Instance.Tracks.Any()) 
                return;
            MidiManager.Instance.RemoveTrack(model.SelectedTrack);
            MidiManager.Instance.AutoSetChanNumber();
            InitTracks();
        }

        internal void CleanUpSong()
        {
            if (!MidiManager.Instance.Tracks.Any())
                return;

            UiManager.Instance.mainWindow.DisableUserInterractions();
            MidiManager.Instance.RemoveEmptyTracks();
            UiManager.Instance.mainWindow.DisableUserInterractions();
            MidiManager.Instance.AutoSetChanNumber();
            UiManager.Instance.mainWindow.DisableUserInterractions();
            InitTracks();
            UiManager.Instance.mainWindow.EnableUserInterractions();
        }

        internal void RemoveAllEventsFromTrack()
        {
            if (!MidiManager.Instance.Tracks.Any())
                return;
            MidiManager.Instance.RemoveAllEventsFromTrack(model.SelectedTrack);
            MidiManager.Instance.SetInstrument(model.SelectedTrack, Quotidian.Structs.Instrument.Parse(MidiManager.Instance.GetTrackName(model.SelectedTrack)).MidiProgramChangeCode);
            InitTracks();
        }

        internal void Drummapping()
        {
            if (!MidiManager.Instance.Tracks.Any())
                return;
            MidiManager.Instance.Drummapping(model.SelectedTrack);
        }

        internal void TransposeTrack(int halftones)
        {
            if (!MidiManager.Instance.Tracks.Any())
                return;
            MidiManager.Instance.Transpose(model.SelectedTrack, halftones);
        }

        private void FocusTrack(object sender, int e)
        {
            model.SelectedTrack = e;
        }

        private void TrackMergeUp(object sender, Track e)
        {
            model.SelectedTrack = e.Id();
            if (!MidiManager.Instance.Tracks.Any())
                return;

            if ((model.SelectedTrack > MidiManager.Instance.Tracks.Count()) ||
                ((model.SelectedTrack - 1) > MidiManager.Instance.Tracks.Count()) ||
                ((model.SelectedTrack - 1) < 0))
                return;

            MidiManager.Instance.MergeTracks(model.SelectedTrack, model.SelectedTrack-1);
            MidiManager.Instance.RemoveTrack(model.SelectedTrack);
            MidiManager.Instance.AutoSetChanNumber();
            InitTracks();
        }

        private void TrackMergeDown(object sender, Track e)
        {
            model.SelectedTrack = e.Id();
            if (!MidiManager.Instance.Tracks.Any())
                return;

            if ((model.SelectedTrack > MidiManager.Instance.Tracks.Count()) ||
                ((model.SelectedTrack - 1) > MidiManager.Instance.Tracks.Count()) ||
                ((model.SelectedTrack - 1) < 0))
                return;

            MidiManager.Instance.MergeTracks(model.SelectedTrack, model.SelectedTrack + 1);
            MidiManager.Instance.RemoveTrack(model.SelectedTrack);
            MidiManager.Instance.AutoSetChanNumber();
            InitTracks();
        }

        #endregion

        #region ZOOM

        internal void TranslateTracks(int delta)
        {
            model.XOffset+=delta;
        }

        internal void ZoomTracksX(int delta)
        {
            model.XZoom += (float)delta/7;
        }

        internal void ZoomTracksY(int delta)
        {
            if (MidiManager.Instance.IsPlaying) return;
            model.YZoom += (float)delta /10;
        }

        #endregion

    }

}
