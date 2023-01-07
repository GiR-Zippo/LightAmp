using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Melanchall.DryWetMidi.Core;
using BardMusicPlayer.MidiUtil.Managers;
using System.Windows.Shapes;
using System.Linq;
using System.Windows.Controls.Primitives;

namespace BardMusicPlayer.MidiUtil.Ui.TrackView
{

    public partial class MidiTrackLineView : Page
    {

        #region CTOR

        bool selectionMouseDown = false;
        Point selectionMouseDownPos;

        public MidiTrackLineControl Ctrl { get; set; }
        public MidiTrackLineModel Model { get; set; }

        public MidiTrackLineView(TrackChunk track)
        {
            Model = new MidiTrackLineModel(track);
            DataContext = Model;
            InitializeComponent();
            Ctrl = new MidiTrackLineControl(Model,this);
            Model.Ctrl = Ctrl;
            Loaded += MyWindow_Loaded;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BodyScroll.ScrollToVerticalOffset(BodyScroll.ScrollableHeight / 2);
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                Ctrl.DeleteSelected();
        }

        #region SelectionBox

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Capture and track the mouse.
            selectionMouseDown = true;
            selectionMouseDownPos = e.GetPosition(theGrid);
            theGrid.CaptureMouse();

            // Initial placement of the drag selection box.         
            Canvas.SetLeft(selectionBox, selectionMouseDownPos.X);
            Canvas.SetTop(selectionBox, selectionMouseDownPos.Y);
            selectionBox.Width = 0;
            selectionBox.Height = 0;

            // Make the drag selection box visible.
            selectionBox.Visibility = Visibility.Visible;
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Release the mouse capture and stop tracking it.
            selectionMouseDown = false;
            theGrid.ReleaseMouseCapture();

            // Hide the drag selection box.
            selectionBox.Visibility = Visibility.Collapsed;

            Ctrl.ClearSelection();

            Point mouseUpPos = e.GetPosition(theGrid);
            foreach (var child in TrackBody.Children)
            {
                var selectedElement = e.Source as UIElement;
                Rectangle rec = child as Rectangle;
                if (rec != null)
                {
                    UIElement container = VisualTreeHelper.GetParent(selectedElement) as UIElement;
                    Point relativeLocation = rec.TranslatePoint(new Point(0, 0), container);
                    if ((relativeLocation.X >= selectionMouseDownPos.X && relativeLocation.X <= mouseUpPos.X) && (relativeLocation.Y >= selectionMouseDownPos.Y && relativeLocation.Y <= mouseUpPos.Y))
                    {
                        rec.Fill = Brushes.Gray;
                        Ctrl.SelectedNotes.Add(rec);
                    }
                }
            }
        }

        private void Grid_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectionMouseDown)
            {
                // When the mouse is held down, reposition the drag selection box.
                Point mousePos = e.GetPosition(theGrid);
                if (selectionMouseDownPos.X < mousePos.X)
                {
                    Canvas.SetLeft(selectionBox, selectionMouseDownPos.X);
                    selectionBox.Width = mousePos.X - selectionMouseDownPos.X;
                }
                else
                {
                    Canvas.SetLeft(selectionBox, mousePos.X);
                    selectionBox.Width = selectionMouseDownPos.X - mousePos.X;
                }

                if (selectionMouseDownPos.Y < mousePos.Y)
                {
                    Canvas.SetTop(selectionBox, selectionMouseDownPos.Y);
                    selectionBox.Height = mousePos.Y - selectionMouseDownPos.Y;
                }
                else
                {
                    Canvas.SetTop(selectionBox, mousePos.Y);
                    selectionBox.Height = selectionMouseDownPos.Y - mousePos.Y;
                }
            }
        }
        #endregion

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
                Ctrl.Init();
            }
        }

        #endregion
    }

}
