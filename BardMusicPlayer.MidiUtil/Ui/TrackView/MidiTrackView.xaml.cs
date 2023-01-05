using Melanchall.DryWetMidi.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BardMusicPlayer.MidiUtil.Managers;

namespace BardMusicPlayer.MidiUtil.Ui.TrackView
{
    /// <summary>
    /// Interaktionslogik für MidiTrackView.xaml
    /// </summary>
    public partial class MidiTrackView : Window
    {
        public MidiTrackView()
        {
            InitializeComponent();
            YZoom = 3.00f;
            this.PreviewMouseWheel += MidiTrackView_MouseWheel;
            MasterScroller.Scroll += new System.Windows.Controls.Primitives.ScrollEventHandler(UpdateHorizontalScrolling);
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

        public void Init(MidiTrackLineView lineView)
        {
            var trackLine = new Frame()
            {
                Content = lineView
            };
            TracksPanel.Children.Add(trackLine);
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
                //UiManager.Instance.mainWindow.MasterScroller.Value = xOffset;
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
