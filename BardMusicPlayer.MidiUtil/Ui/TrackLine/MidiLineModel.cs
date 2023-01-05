using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows;
using Melanchall.DryWetMidi.Core;
using BardMusicPlayer.MidiUtil.Utils;
using BardMusicPlayer.MidiUtil.Managers;

namespace BardMusicPlayer.MidiUtil.Ui.TrackLine
{
    public class MidiLineModel : HandleBinding
    {
        #region CTOR

        public MidiLineControl Ctrl { get; set; }
        public TrackChunk Track { get; }
        private Color trackColor { get; set; }

        public MidiLineModel(TrackChunk track)
        {
            Random rnd = new Random();
            trackColor = Color.FromRgb(
                    (byte)rnd.Next(0, 255),
                    (byte)rnd.Next(0, 255),
                    (byte)rnd.Next(0, 255)
                );
            this.Track = track;
            tColor = new SolidColorBrush(trackColor);
            LastNotesOn = new Dictionary<int, Tuple<int, MidiEvent>>();
        }

        #endregion


        public readonly Thickness SelectedBorderThickness = new Thickness(.5f);
        public readonly Thickness UnselectedBorderThickness = new Thickness(0);
        public Point mouseDragStartPoint;
        public Point mouseDragEndPoint;
        public bool isDragging = false;

        #region ATRB

        private SolidColorBrush tColor;
        public SolidColorBrush TColor
        {
            get { return tColor; }
            set { tColor = value; RaisePropertyChanged("TColor"); }
        }

        public int MidiInstrument { get; internal set; }

        public Dictionary<int, Tuple<int, MidiEvent>> LastNotesOn { get; set; }

        #region ZOOM
        #pragma warning disable S3237

        public float CellWidth
        {
            get
            {
                return (UiManager.Instance.mainWindow.Model.XZoom);
            }
            set
            {
                RaisePropertyChanged("CellWidth");
                RaisePropertyChanged("CellHeigth");
                Ctrl.DrawPianoRoll();
                Ctrl.DrawMidiEvents();
            }
        }

        public float CellHeigth
        {
            get
            {
                return (UiManager.Instance.mainWindow.Model.YZoom);
            }
            set
            {
                RaisePropertyChanged("CellHeigth");
                RaisePropertyChanged("CellWidth");
                Ctrl.DrawPianoRoll();
                Ctrl.DrawMidiEvents();
            }
        }

#pragma warning restore S3237
        #endregion

        private double xOffset;
        public double XOffset
        {
            get { return xOffset; }
            set
            {
                xOffset = value;
                Ctrl.view.TrackBody.Margin = new Thickness(-XOffset,0,0,0);
            }
        }
        public double DAWhosReso { get; } = 1;
        public double PlotReso
        {
            get
            {
                return 1 / UiManager.Instance.plotDivider;
            }
        }

        #endregion

    }
}
