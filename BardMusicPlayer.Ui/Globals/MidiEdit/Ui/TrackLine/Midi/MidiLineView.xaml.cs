using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Melanchall.DryWetMidi.Core;
using BardMusicPlayer.Ui.MidiEdit.Managers;

namespace BardMusicPlayer.Ui.MidiEdit.Ui.TrackLine
{

    public partial class MidiLineView : Page
    {

        #region CTOR

        public MidiLineControl Ctrl { get; set; }
        public MidiLineModel Model { get; set; }

        public MidiLineView(TrackChunk track)
        {
            Model = new MidiLineModel(track);
            DataContext = Model;
            InitializeComponent();
            Ctrl = new MidiLineControl(Model,this);
            Model.Ctrl = Ctrl;
            Loaded += MyWindow_Loaded;
            TrackBody.MouseWheel += MouseWheel;
            //TrackHeader.MouseWheel += MouseWheeled;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BodyScroll.ScrollToVerticalOffset(BodyScroll.ScrollableHeight / 2);
        }

        #endregion

        #region SHOW BORDERS ON FOCUS

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            Border.BorderThickness = Model.SelectedBorderThickness;
            TrackHeader.Background = new SolidColorBrush(Colors.LightGray);
            Ctrl.TrackGotFocus(sender, e); 
        }

        private void Grid_LostFocus(object sender, RoutedEventArgs e)
        {
            Border.BorderThickness = Model.UnselectedBorderThickness;
            TrackHeader.Background = new SolidColorBrush(Colors.Gray);
        }

        private void MergeUp_Click(object sender, RoutedEventArgs e)
        {
            Border.BorderThickness = Model.SelectedBorderThickness;
            Ctrl.TrackGotFocus(sender, e);
            Ctrl.MergeUp(sender, e);
        }

        private void MergeDown_Click(object sender, RoutedEventArgs e)
        {
            Border.BorderThickness = Model.SelectedBorderThickness;
            Ctrl.TrackGotFocus(sender, e);
            Ctrl.MergeDown(sender, e);
        }
        #endregion

        #region MOUSE GESTION
        private double last_pos;
        private bool in_drag = false;
        private void TrackBody_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount>1)
            {
                Model.mouseDragStartPoint = e.GetPosition((Canvas)sender);
                last_pos = Model.mouseDragStartPoint.X / Model.CellWidth;
                in_drag = true;
            }
        }

        private void TrackBody_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (in_drag)
            {
                Model.mouseDragStartPoint = e.GetPosition((Canvas)sender);
                double point = Model.mouseDragStartPoint.X / Model.CellWidth;
                int noteIndex = 127 - (int)(Model.mouseDragStartPoint.Y / Model.CellHeigth);
                Ctrl.InsertNote(PreviousFirstPosition(last_pos), NextFirstPosition(point), noteIndex);
                in_drag = false;
            }
        }

        private double NextFirstPosition(double point)
        {
            return Model.PlotReso * (1+((int)(point / Model.PlotReso)));
        }

        private double PreviousFirstPosition(double point)
        {
            return Model.PlotReso * ((int)(point / Model.PlotReso));
        }

        private void MouseWheel(object sender, MouseWheelEventArgs e)
        {
            UiManager.Instance.mainWindow.HandleWheel(sender, e);
        }

        #endregion

        #region HEADER

        // TODO color picker
        private void TrackColor_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            Color color = Color.FromRgb(
                (byte)rnd.Next(0, 255),
                (byte)rnd.Next(0, 255),
                (byte)rnd.Next(0, 255)
            );
            //SetColor(color);
            Model.TColor = new SolidColorBrush(color); 
        }
        
        private void InstrumentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.ComboInstruments.IsDropDownOpen)
            {
                this.ComboInstruments.IsDropDownOpen = false;
                MidiManager.Instance.SetInstrument(Model.Track, ComboInstruments.SelectedIndex);
                MidiManager.Instance.SetTrackName(Model.Track, Quotidian.Structs.Instrument.ParseByProgramChange(ComboInstruments.SelectedIndex).Name);
                UiManager.Instance.mainWindow.Ctrl.InitTracks();
            }
        }

        #endregion
    }

}
