using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Globalization;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace BardMusicPlayer.Ui.Windows
{
    public enum EditorTool { Select, Draw, Erase }

    public class NoteData : INotifyPropertyChanged
    {
        private int _pitch; private long _start; private long _duration;
        public int Pitch { get => _pitch; set { _pitch = value; OnPropertyChanged("Pitch"); } }
        public long Start { get => _start; set { _start = value; OnPropertyChanged("Start"); } }
        public long Duration { get => _duration; set { _duration = value; OnPropertyChanged("Duration"); } }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class AutomationPoint { public long Tick { get; set; } public int Value { get; set; } }

    public partial class MidiBardConverterTrackWindow : Window
    {
        private double _noteHeight = 20;
        private double _tickPixelScale = 0.1;
        private long _gridSnapTicks = 120;
        private short _ticksPerQuarterNote = 480;
        private long _maxTick = 4800;
        private EditorTool _currentTool = EditorTool.Select;
        private TempoMap _tempoMap;

        private Point _lastMousePos;
        private bool _isDragging = false;
        private bool _isResizing = false;
        private bool _isResizingLeft = false;
        private Point _selectionStartPoint;
        private bool _isSelecting = false;
        private List<Border> _selectedNoteBorders = new List<Border>();
        private Border _newNoteBeingCreated = null;
        private List<AutomationPoint> _programChanges = new List<AutomationPoint>();
        private List<NoteData> _clipboard = new List<NoteData>();

        public MidiBardConverterTrackWindow(MidiFile midiFile, TrackChunk track)
        {
            InitializeComponent();
            _tempoMap = midiFile.GetTempoMap();
            if (midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision tpq) _ticksPerQuarterNote = tpq.TicksPerQuarterNote;

            NotesCanvas.MouseLeftButtonDown += NotesCanvas_MouseLeftButtonDown;
            NotesCanvas.MouseMove += NotesCanvas_MouseMove;
            NotesCanvas.MouseLeftButtonUp += NotesCanvas_MouseLeftButtonUp;

            this.Loaded += (s, e) =>
            {
                UpdateSnapTicks();
                ExtractMidiData(track);
                BuildPianoKeys();
                RefreshLayout();
                LoadNotes(track);
                UpdateToolUI();
            };
        }

        private void ExtractMidiData(TrackChunk track)
        {
            var notes = track.GetNotes();
            if (notes.Any()) _maxTick = Math.Max(4800, notes.Max(n => n.Time + n.Length) + 1000);
            _programChanges = track.Events.OfType<ProgramChangeEvent>().Select(e => new AutomationPoint { Tick = e.DeltaTime, Value = e.ProgramNumber }).OrderBy(p => p.Tick).ToList();
        }

        private void SwitchTool(EditorTool tool) { _currentTool = tool; UpdateToolUI(); ClearSelection(); }

        private void UpdateToolUI()
        {
            CurrentToolText.Text = $"Tool: {_currentTool.ToString().ToUpper()}";
            NotesCanvas.Cursor = _currentTool == EditorTool.Draw ? Cursors.Pen : (_currentTool == EditorTool.Erase ? Cursors.Cross : Cursors.Arrow);
        }

        private void ClearSelection()
        {
            foreach (var b in _selectedNoteBorders) UpdateNoteVisuals(b, false);
            _selectedNoteBorders.Clear();
        }

        private void UpdateNoteVisuals(Border note, bool isSelected)
        {
            note.Background = isSelected ? Brushes.Orange : new SolidColorBrush(Color.FromRgb(58, 134, 255));
            note.BorderBrush = isSelected ? Brushes.White : Brushes.Transparent;
            note.BorderThickness = new Thickness(isSelected ? 1.5 : 0.5);
            Panel.SetZIndex(note, isSelected ? 100 : 10);
        }

        private void NotesCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(NotesCanvas);

            if (_currentTool == EditorTool.Erase)
            {
                DependencyObject dep = e.OriginalSource as DependencyObject;
                while (dep != null && !(dep is Border && ((Border)dep).Tag is NoteData)) dep = VisualTreeHelper.GetParent(dep);
                if (dep is Border b) NotesCanvas.Children.Remove(b);
                return;
            }

            if (_currentTool == EditorTool.Select)
            {
                DependencyObject dep = e.OriginalSource as DependencyObject;
                while (dep != null && !(dep is Border && ((Border)dep).Tag is NoteData)) dep = VisualTreeHelper.GetParent(dep);
                Border hitNote = dep as Border;

                if (hitNote != null)
                {
                    double mouseX = e.GetPosition(hitNote).X;
                    if (mouseX < 7.0 || mouseX > hitNote.ActualWidth - 7.0)
                    {
                        _isResizing = true;
                        _isResizingLeft = mouseX < 7.0;
                    }
                    else
                    {
                        _isDragging = true;
                    }

                    if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !_selectedNoteBorders.Contains(hitNote)) ClearSelection();
                    if (!_selectedNoteBorders.Contains(hitNote)) { _selectedNoteBorders.Add(hitNote); UpdateNoteVisuals(hitNote, true); }

                    NotesCanvas.CaptureMouse();
                    e.Handled = true;
                    return;
                }

                _isSelecting = true;
                _isDragging = false;
                _selectionStartPoint = _lastMousePos;
                if (!Keyboard.IsKeyDown(Key.LeftCtrl)) ClearSelection();
                SelectionRect.Visibility = Visibility.Visible;
                Canvas.SetLeft(SelectionRect, _lastMousePos.X); Canvas.SetTop(SelectionRect, _lastMousePos.Y);
                SelectionRect.Width = 0; SelectionRect.Height = 0;
                NotesCanvas.CaptureMouse();
            }

            if (_currentTool == EditorTool.Draw && e.Source == NotesCanvas)
            {
                int p = 127 - (int)(_lastMousePos.Y / _noteHeight);
                long s = Snap((long)(_lastMousePos.X / _tickPixelScale));
                _newNoteBeingCreated = AddNoteUI(p, s, _gridSnapTicks);
                NotesCanvas.CaptureMouse();
            }
        }

        private void NotesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point cur = e.GetPosition(NotesCanvas);
            Vector delta = cur - _lastMousePos;

            // Cursor & Drag-Point Check
            if (!_isDragging && !_isResizing && !_isSelecting && _currentTool == EditorTool.Select)
            {
                DependencyObject dep = e.OriginalSource as DependencyObject;
                while (dep != null && !(dep is Border && ((Border)dep).Tag is NoteData)) dep = VisualTreeHelper.GetParent(dep);

                if (dep is Border b)
                {
                    double mouseX = e.GetPosition(b).X;
                    NotesCanvas.Cursor = (mouseX < 7.0 || mouseX > b.ActualWidth - 7.0) ? Cursors.SizeWE : Cursors.Hand;
                }
                else NotesCanvas.Cursor = Cursors.Arrow;

                _lastMousePos = cur; // Wichtig für Strg+V Position
            }

            // Resizing
            if (_isResizing && _selectedNoteBorders.Any())
            {
                long tickDiff = (long)(delta.X / _tickPixelScale);
                if (tickDiff != 0)
                {
                    foreach (var nb in _selectedNoteBorders)
                    {
                        var nd = (NoteData)nb.Tag;
                        if (_isResizingLeft)
                        {
                            long newStart = nd.Start + tickDiff;
                            long newDur = nd.Duration - tickDiff;
                            if (newDur > 10 && newStart >= 0) { nd.Start = newStart; nd.Duration = newDur; }
                        }
                        else
                        {
                            long newDur = nd.Duration + tickDiff;
                            if (newDur > 10) nd.Duration = newDur;
                        }
                        UpdateNotePosition(nb);
                    }
                    _lastMousePos = cur;
                }
                return;
            }

            // Verschieben
            if (_isDragging && _selectedNoteBorders.Any())
            {
                long tickOff = (long)(delta.X / _tickPixelScale);
                int pitchOff = (int)(-delta.Y / _noteHeight);
                if (tickOff != 0 || pitchOff != 0)
                {
                    bool canMove = _selectedNoteBorders.All(nb => {
                        var nd = (NoteData)nb.Tag;
                        return nd.Start + tickOff >= 0 && nd.Pitch + pitchOff >= 0 && nd.Pitch + pitchOff <= 127;
                    });
                    if (canMove)
                    {
                        foreach (var nb in _selectedNoteBorders)
                        {
                            var nd = (NoteData)nb.Tag;
                            nd.Start += tickOff; nd.Pitch += pitchOff;
                            UpdateNotePosition(nb);
                        }
                        _lastMousePos = cur;
                    }
                }
                return;
            }

            // Auswahlrahmen
            if (_isSelecting)
            {
                double x = Math.Min(_selectionStartPoint.X, cur.X); double y = Math.Min(_selectionStartPoint.Y, cur.Y);
                double w = Math.Abs(_selectionStartPoint.X - cur.X); double h = Math.Abs(_selectionStartPoint.Y - cur.Y);
                Canvas.SetLeft(SelectionRect, x); Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = w; SelectionRect.Height = h;
                Rect sel = new Rect(x, y, w, h);
                foreach (var b in NotesCanvas.Children.OfType<Border>().Where(x => x.Tag is NoteData))
                {
                    bool hit = sel.IntersectsWith(new Rect(Canvas.GetLeft(b), Canvas.GetTop(b), b.ActualWidth, b.ActualHeight));
                    if (hit && !_selectedNoteBorders.Contains(b)) { _selectedNoteBorders.Add(b); UpdateNoteVisuals(b, true); }
                    else if (!hit && _selectedNoteBorders.Contains(b) && !Keyboard.IsKeyDown(Key.LeftCtrl)) { _selectedNoteBorders.Remove(b); UpdateNoteVisuals(b, false); }
                }
            }

            if (_newNoteBeingCreated != null)
            {
                var d = (NoteData)_newNoteBeingCreated.Tag;
                d.Duration = Math.Max(_gridSnapTicks, Snap((long)(cur.X / _tickPixelScale)) - d.Start);
                UpdateNotePosition(_newNoteBeingCreated);
            }
        }

        private void NotesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging || _isResizing)
            {
                foreach (var nb in _selectedNoteBorders)
                {
                    var nd = (NoteData)nb.Tag;
                    nd.Start = Snap(nd.Start);
                    nd.Duration = Snap(nd.Duration);
                    UpdateNotePosition(nb);
                }
            }
            _isDragging = false;
            _isResizing = false;
            _isSelecting = false;
            SelectionRect.Visibility = Visibility.Collapsed;
            _newNoteBeingCreated = null;
            NotesCanvas.ReleaseMouseCapture();
            RefreshLayout();
        }

        private Border AddNoteUI(int pitch, long start, long dur)
        {
            var d = new NoteData { Pitch = pitch, Start = start, Duration = dur };
            var b = new Border { Tag = d, CornerRadius = new CornerRadius(1), IsHitTestVisible = true };
            UpdateNoteVisuals(b, false);
            UpdateNotePosition(b);
            NotesCanvas.Children.Add(b);
            return b;
        }

        private void UpdateNotePosition(Border n)
        {
            var d = (NoteData)n.Tag;
            Canvas.SetLeft(n, d.Start * _tickPixelScale); Canvas.SetTop(n, (127 - d.Pitch) * _noteHeight);
            n.Width = Math.Max(2, d.Duration * _tickPixelScale); n.Height = _noteHeight - 1;
        }

        private void FocusHighestNote()
        {
            var highest = NotesCanvas.Children.OfType<Border>().Where(b => b.Tag is NoteData)
                            .OrderByDescending(b => ((NoteData)b.Tag).Pitch).FirstOrDefault();
            if (highest != null) MainScroll.ScrollToVerticalOffset(Math.Max(0, Canvas.GetTop(highest) - 100));
        }

        private void RefreshLayout()
        {
            double w = Math.Max(MainScroll.ActualWidth, (_maxTick * _tickPixelScale) + 500);
            EditorGrid.Width = w; EditorGrid.Height = 128 * _noteHeight;
            GridCanvas.Width = w; GridCanvas.Height = EditorGrid.Height;
            RulerCanvas.Width = w; AutomationCanvas.Width = w; MarkersCanvas.Width = w;
            DrawGrid(); DrawRuler(); DrawMarkers(); DrawAutomation();
        }

        private void DrawGrid()
        {
            GridCanvas.Children.Clear();
            var ts = _tempoMap.GetTimeSignatureAtTime((MidiTimeSpan)0);
            long tpb = _ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;
            for (int i = 0; i <= 128; i++) GridCanvas.Children.Add(new Line { X1 = 0, X2 = EditorGrid.Width, Y1 = i * _noteHeight, Y2 = i * _noteHeight, Stroke = new SolidColorBrush(Color.FromRgb(35, 35, 35)), StrokeThickness = 0.5 });
            for (long t = 0; t < EditorGrid.Width / _tickPixelScale; t += _gridSnapTicks)
            {
                double x = t * _tickPixelScale; bool isB = t % tpb == 0;
                GridCanvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = 0, Y2 = EditorGrid.Height, Stroke = isB ? new SolidColorBrush(Color.FromRgb(70, 70, 70)) : new SolidColorBrush(Color.FromRgb(30, 30, 30)), StrokeThickness = 0.5 });
            }
        }

        private void DrawRuler()
        {
            RulerCanvas.Children.Clear();
            var ts = _tempoMap.GetTimeSignatureAtTime((MidiTimeSpan)0);
            long tpb = _ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;
            for (long t = 0; t < RulerCanvas.Width / _tickPixelScale; t += _ticksPerQuarterNote)
            {
                double x = t * _tickPixelScale; bool isB = t % tpb == 0;
                RulerCanvas.Children.Add(new Line { X1 = x, X2 = x, Y1 = isB ? 0 : 18, Y2 = 30, Stroke = Brushes.Gray, StrokeThickness = isB ? 1 : 0.5 });
                if (isB) RulerCanvas.Children.Add(new TextBlock { Text = $"{(t / tpb) + 1}", Foreground = Brushes.DimGray, FontSize = 12, Margin = new Thickness(x + 3, 2, 0, 0) });
            }
        }

        private void DrawMarkers()
        {
            MarkersCanvas.Children.Clear();
            foreach (int n in new[] { 84, 35 })
            {
                double y = (127 - n) * _noteHeight;
                MarkersCanvas.Children.Add(new Line { X1 = 0, X2 = EditorGrid.Width, Y1 = y, Y2 = y, Stroke = Brushes.Red, StrokeThickness = 1, Opacity = 0.5 });
            }
        }

        private void DrawAutomation()
        {
            AutomationCanvas.Children.Clear();
            if (!_programChanges.Any()) return;
            Polyline line = new Polyline { Stroke = Brushes.Orange, StrokeThickness = 2 };
            double h = AutomationCanvas.ActualHeight > 0 ? AutomationCanvas.ActualHeight : 80;
            foreach (var pt in _programChanges.OrderBy(p => p.Tick))
            {
                double x = pt.Tick * _tickPixelScale; double y = h - (pt.Value / 127.0 * h);
                if (line.Points.Any()) line.Points.Add(new Point(x, line.Points.Last().Y));
                line.Points.Add(new Point(x, y));
                AutomationCanvas.Children.Add(new Ellipse { Width = 4, Height = 4, Fill = Brushes.Orange, Margin = new Thickness(x - 2, y - 2, 0, 0) });
            }
            if (line.Points.Any()) line.Points.Add(new Point(AutomationCanvas.Width, line.Points.Last().Y));
            AutomationCanvas.Children.Add(line);
        }

        private void AutomationCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(AutomationCanvas);
            long tick = Snap((long)(p.X / _tickPixelScale));
            int val = (int)((1 - (p.Y / AutomationCanvas.ActualHeight)) * 127);
            _programChanges.RemoveAll(x => x.Tick == tick); _programChanges.Add(new AutomationPoint { Tick = tick, Value = val });
            DrawAutomation();
        }

        private void BuildPianoKeys()
        {
            PianoKeysStack.Children.Clear(); int[] blacks = { 1, 3, 6, 8, 10 };
            for (int i = 127; i >= 0; i--)
            {
                var b = new Border { Height = _noteHeight, Background = blacks.Contains(i % 12) ? new SolidColorBrush(Color.FromRgb(40, 40, 40)) : Brushes.White, BorderBrush = Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1) };
                if (i % 12 == 0) b.Child = new TextBlock { Text = "C" + ((i / 12) - 1), FontSize = 9, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.DarkGray, Margin = new Thickness(5, 0, 0, 0) };
                PianoKeysStack.Children.Add(b);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S) SwitchTool(EditorTool.Select);
            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.None) SwitchTool(EditorTool.Draw);
            if (e.Key == Key.E) SwitchTool(EditorTool.Erase);

            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                foreach (var b in _selectedNoteBorders.ToList()) NotesCanvas.Children.Remove(b);
                _selectedNoteBorders.Clear();
            }

            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ClearSelection();
                foreach (var b in NotesCanvas.Children.OfType<Border>().Where(x => x.Tag is NoteData))
                {
                    _selectedNoteBorders.Add(b);
                    UpdateNoteVisuals(b, true);
                }
                e.Handled = true;
                return;
            }

            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_selectedNoteBorders.Any())
                {
                    _clipboard.Clear();
                    foreach (var b in _selectedNoteBorders)
                    {
                        var nd = (NoteData)b.Tag;
                        _clipboard.Add(new NoteData { Pitch = nd.Pitch, Start = nd.Start, Duration = nd.Duration });
                    }
                    e.Handled = true;
                }
            }

            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_clipboard.Any())
                {
                    ClearSelection();
                    long minStart = _clipboard.Min(n => n.Start);
                    long targetStart = Snap((long)(_lastMousePos.X / _tickPixelScale));
                    foreach (var oldData in _clipboard)
                    {
                        var newNote = AddNoteUI(oldData.Pitch, oldData.Start - minStart + targetStart, oldData.Duration);
                        _selectedNoteBorders.Add(newNote);
                        UpdateNoteVisuals(newNote, true);
                    }
                    RefreshLayout();
                    e.Handled = true;
                }
            }

            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_selectedNoteBorders.Any())
                {
                    var newCopies = new List<Border>();
                    long offset = _gridSnapTicks;
                    foreach (var b in _selectedNoteBorders)
                    {
                        var oldData = (NoteData)b.Tag;
                        var copy = AddNoteUI(oldData.Pitch, oldData.Start + offset, oldData.Duration);
                        newCopies.Add(copy);
                    }
                    ClearSelection();
                    foreach (var copy in newCopies) { _selectedNoteBorders.Add(copy); UpdateNoteVisuals(copy, true); }
                    e.Handled = true;
                }
            }

            if (_selectedNoteBorders.Any() && !_isDragging && !_isResizing)
            {
                int pAdd = 0; long tAdd = 0;
                if (e.Key == Key.Up) pAdd = 1;
                if (e.Key == Key.Down) pAdd = -1;
                if (e.Key == Key.Left) tAdd = -_gridSnapTicks;
                if (e.Key == Key.Right) tAdd = _gridSnapTicks;

                if (pAdd != 0 || tAdd != 0)
                {
                    foreach (var nb in _selectedNoteBorders)
                    {
                        var nd = (NoteData)nb.Tag;
                        nd.Pitch = Math.Max(0, Math.Min(127, nd.Pitch + pAdd));
                        nd.Start = Math.Max(0, nd.Start + tAdd);
                        UpdateNotePosition(nb);
                    }
                    e.Handled = true;
                }
            }
        }

        private long Snap(long ticks) => (long)(Math.Round((double)ticks / _gridSnapTicks) * _gridSnapTicks);
        private void SetTool_Click(object sender, RoutedEventArgs e) => SwitchTool((EditorTool)Enum.Parse(typeof(EditorTool), (string)((MenuItem)sender).Tag));
        private void MainScroll_ScrollChanged(object sender, ScrollChangedEventArgs e) { Canvas.SetTop(PianoKeysStack, -e.VerticalOffset); Canvas.SetLeft(RulerCanvas, -e.HorizontalOffset); ControllerScroll.ScrollToHorizontalOffset(e.HorizontalOffset); }
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { _tickPixelScale = e.NewValue; if (IsLoaded) { RefreshLayout(); foreach (var b in NotesCanvas.Children.OfType<Border>()) UpdateNotePosition(b); } }
        private void MainScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e) { if (Keyboard.Modifiers == ModifierKeys.Control) { ZoomSlider.Value += e.Delta > 0 ? 0.01 : -0.01; e.Handled = true; } }
        private void UpdateSnapTicks() { if (SnapComboBox?.SelectedItem is ComboBoxItem item) _gridSnapTicks = (long)(_ticksPerQuarterNote * double.Parse(item.Tag.ToString(), CultureInfo.InvariantCulture)); }
        private void SnapComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSnapTicks();
        private void LoadNotes(TrackChunk t)
        {
            foreach (var n in t.GetNotes()) AddNoteUI((int)n.NoteNumber, n.Time, n.Length);
            Dispatcher.BeginInvoke(new Action(FocusHighestNote), System.Windows.Threading.DispatcherPriority.Background);
        }
        private void SaveTrack_Click(object sender, RoutedEventArgs e) { DialogResult = true; Close(); }
    }
}