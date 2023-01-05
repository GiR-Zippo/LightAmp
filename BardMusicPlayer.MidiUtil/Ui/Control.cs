using System;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;
using System.Linq;

using Melanchall.DryWetMidi.Core;

using BardMusicPlayer.MidiUtil.Managers;
using BardMusicPlayer.MidiUtil.Ui.TrackLine;
using BardMusicPlayer.MidiUtil.Utils;

namespace BardMusicPlayer.MidiUtil.Ui
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

        public void AddTrack()
        {
            MidiManager.Instance.AddTrack();
            InitTracks();
        }

        internal void RemoveTrack()
        {
            if ((!MidiManager.Instance.GetTrackChunks().Any()) || (model.SelectedTrackChunk == null))
                return;

            MidiManager.Instance.RemoveTrack(model.SelectedTrackChunk);
            InitTracks();
        }

        internal void CleanUpSong()
        {
            if (!MidiManager.Instance.GetTrackChunks().Any())
                return;

            UiManager.Instance.mainWindow.DisableUserInterractions();
            MidiManager.Instance.RemoveEmptyTracks();
            MidiManager.Instance.AutoSetChannelsForAllTracks();
            InitTracks();
            UiManager.Instance.mainWindow.EnableUserInterractions();
        }

        internal void AutochannelSong()
        {
            if (!MidiManager.Instance.GetTrackChunks().Any())
                return;

            UiManager.Instance.mainWindow.DisableUserInterractions();
            MidiManager.Instance.AutoRenumberChannels();
            InitTracks();
            UiManager.Instance.mainWindow.EnableUserInterractions();
        }

        internal void RemoveAllEventsFromTrack()
        {
            if ((!MidiManager.Instance.GetTrackChunks().Any()) || (model.SelectedTrackChunk == null))
                return;
            MidiManager.Instance.RemoveAllEventsFromTrack(model.SelectedTrackChunk);
            MidiManager.Instance.SetInstrument(model.SelectedTrackChunk, Quotidian.Structs.Instrument.Parse(MidiManager.Instance.GetTrackName(model.SelectedTrackChunk)).MidiProgramChangeCode);
            UpdateTrackView(model.SelectedTrackChunk);
        }

        internal void Drummapping()
        {
            if ((!MidiManager.Instance.GetTrackChunks().Any()) || (model.SelectedTrackChunk == null))
                return;

            MidiManager.Instance.Drummapping(model.SelectedTrackChunk);
            InitTracks();
        }

        internal void TransposeTrack(int halftones)
        {
            if ((!MidiManager.Instance.GetTrackChunks().Any()) || (model.SelectedTrackChunk == null))
                return;

            MidiManager.Instance.Transpose(model.SelectedTrackChunk, halftones);
            UpdateTrackView(model.SelectedTrackChunk);
        }

        private void TrackMergeUp(object sender, TrackChunk e)
        {
            if ((!MidiManager.Instance.GetTrackChunks().Any()) || (model.SelectedTrackChunk == null))
                return;

            TrackChunk source = model.SelectedTrackChunk;
            var index = MidiManager.Instance.GetTrackChunks().IndexOf(source);
            if ((index == -1) || (index - 1 < 0))
                return;

            TrackChunk dest = MidiManager.Instance.GetTrackChunks().ElementAt(index - 1);
            MidiManager.Instance.MergeTracks(source, dest, index - 1);
            InitTracks();
        }

        private void TrackMergeDown(object sender, TrackChunk e)
        {
            if ((!MidiManager.Instance.GetTrackChunks().Any()) || (model.SelectedTrackChunk == null))
                return;

            TrackChunk source = model.SelectedTrackChunk;
            var index = MidiManager.Instance.GetTrackChunks().IndexOf(source);
            if ((index == -1) || (index + 1 >= MidiManager.Instance.GetTrackChunks().Count()))
                return;

            TrackChunk dest = MidiManager.Instance.GetTrackChunks().ElementAt(index + 1);
            MidiManager.Instance.MergeTracks(source, dest, index + 1);
            InitTracks();
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
            view.TracksPanel.Children.Clear();
            view.TracksPanel.RowDefinitions.Clear();
            UiManager.Instance.mainWindow.MidiLines.Clear();

            int i = 0;
            foreach (TrackChunk track in MidiManager.Instance.GetTrackChunks()) 
            {
                MidiLineView lineView = InitTrackLine(track);
                UiManager.Instance.mainWindow.MidiLines.Add(lineView);
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

        private MidiLineView InitTrackLine(TrackChunk track)
        {
            MidiLineView lineView = new MidiLineView(track);
            lineView.Ctrl.TrackFocused += FocusTrack;
            lineView.Ctrl.TrackMergeUp += TrackMergeUp;
            lineView.Ctrl.TrackMergeDown += TrackMergeDown;
            return lineView;
        }

        public void UpdateTrackView(TrackChunk track)
        {
            foreach (var item in UiManager.Instance.mainWindow.MidiLines)
            {
                if (item.Ctrl.model.Track.Equals(track))
                    item.Ctrl.Init();
            }
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

        private void FocusTrack(object sender, TrackChunk e)
        {
            model.SelectedTrackChunk = e;
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
