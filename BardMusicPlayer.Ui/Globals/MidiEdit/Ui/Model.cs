using System.Windows.Controls;
using BardMusicPlayer.Ui.MidiEdit.Managers;
using BardMusicPlayer.Ui.MidiEdit.Ui.TrackLine;
using BardMusicPlayer.Ui.MidiEdit.Utils;

namespace BardMusicPlayer.Ui.MidiEdit.Ui
{
    public class Model : HandleBinding
    {

        #region Project Values
        public string ProjectName { get; set; } = "New Project";
        #endregion

        #region config 

        public int timeWidth = 1;
        public double midiResolution = 1;

        #endregion

        #region State

        public int SelectedTrack { get; set; } = 0;
        public bool ManuallyScrolling { get; set; } = false;
        public bool Closing { get; set; } = false;
        public bool Playing { get; set; } = false;

        public int Tempo
        {
            get { return MidiManager.Instance.Tempo; }
            set { MidiManager.Instance.Tempo=value; RaisePropertyChanged("Tempo"); }
        }

        #endregion

        #region Zoom & Offset

        public Grid TracksPanel { get; set; }

        /// <summary>
        /// Pan tracks
        /// </summary>
        private double xOffset = 0;
        public double XOffset {
            get { return xOffset; }
            set
            {
                if (value < 0)
                    value = 0;
                xOffset = value;
                RaisePropertyChanged("XOffset");
                foreach (var obj in TracksPanel.Children)
                {
                    Frame track = obj as Frame;
                    if (track == null)
                        continue;
                    ((MidiLineView)track.Content).Model.XOffset = xOffset;
                }
                UiManager.Instance.mainWindow.MasterScroller.Value = xOffset;
            }
        } 
        
        /// <summary>
        /// Zoom tracks
        /// </summary>
        private float xZoom = 0.01f;
        public float XZoom {
            get { return xZoom; }
            set
            {
                if (value < .01f) 
                    value = .01f;
                xZoom = value;
                RaisePropertyChanged("XZoom");

                foreach (var obj in TracksPanel.Children)
                {
                    Frame track = obj as Frame;
                    if (track == null)
                        continue;
                    ((MidiLineView)track.Content).Model.CellWidth = (int)(XZoom);
                }
                UiManager.Instance.mainWindow.MasterScroller.Maximum = (MidiManager.Instance.GetLength() * XZoom);
                if (UiManager.Instance.mainWindow.MasterScroller.Value > UiManager.Instance.mainWindow.MasterScroller.Maximum)
                    UiManager.Instance.mainWindow.MasterScroller.Value = 0;

                UiManager.Instance.mainWindow.HandleTimeBar();
            }
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
                if (value < .1f) value = .1f;
                yZoom = value;
                RaisePropertyChanged("YZoom");
                foreach (var obj in TracksPanel.Children)
                {
                    Frame track = obj as Frame;
                    if (track == null)
                        continue;
                    ((MidiLineView)track.Content).Model.CellHeigth =
                        (int)(YZoom);
                }
            }
        }

        public double Headerwidth { get; internal set; } = 200;

        #endregion

        private double plotDivider=4;
        public double PlotDivider
        {
            get { return plotDivider; }
            set
            {
                plotDivider = value;
                RaisePropertyChanged("PlotDivider");
            }
        }


        private int plotVelocity = 100;
        public int PlotVelocity
        {
            get { return plotVelocity; }
            set
            {
                plotVelocity = value;
                RaisePropertyChanged("PlotVelocity");
            }
        }
    }
}
