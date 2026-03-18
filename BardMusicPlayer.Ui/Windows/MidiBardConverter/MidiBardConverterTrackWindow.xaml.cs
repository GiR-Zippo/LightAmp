/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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

    public class UndoAction
    {
        public string Name { get; set; }
        public List<NoteMemento> Before { get; set; }
        public List<NoteMemento> After { get; set; }
        public Action ApplyUndo { get; set; }
        public Action ApplyRedo { get; set; }
    }

    public class NoteMemento
    {
        public NoteData Data { get; set; }
        public int Pitch { get; set; }
        public long Start { get; set; }
        public long Duration { get; set; }
        public bool Exists { get; set; }
    }

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
        private Stack<UndoAction> _undoStack = new Stack<UndoAction>();
        private Stack<UndoAction> _redoStack = new Stack<UndoAction>();
        private List<(Border Border, int Pitch, long Start, long Duration)> _dragStartSnapshots;
        private List<Border> _selectedBordersAtStartOfSelection = new List<Border>();

        //LowerWindow
        private bool _isDraggingAutomation;
        private FrameworkElement _activeAutomationPoint;
        private AutomationPoint _automationStartSnapshot;

        public MidiBardConverterTrackWindow(MidiFile midiFile, TrackChunk track)
        {
            InitializeComponent();
            _tempoMap = midiFile.GetTempoMap();
            if (midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision tpq) _ticksPerQuarterNote = tpq.TicksPerQuarterNote;

            NotesCanvas.MouseLeftButtonDown += NotesCanvas_MouseLeftButtonDown;
            NotesCanvas.MouseMove += NotesCanvas_MouseMove;
            NotesCanvas.MouseLeftButtonUp += NotesCanvas_MouseLeftButtonUp;

            // PreviewKeyDown prevent unwanted scroll
            this.PreviewKeyDown += Window_PreviewKeyDown;

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

        private void SwitchTool(EditorTool tool)
        {
            _currentTool = tool;
            UpdateToolUI();
            ClearSelection();
        }

        private void UpdateToolUI()
        {
            CurrentToolText.Text = $"Tool: {_currentTool.ToString().ToUpper()}";
            NotesCanvas.Cursor = _currentTool == EditorTool.Draw ? Cursors.Pen : (_currentTool == EditorTool.Erase ? Cursors.Cross : Cursors.Arrow);
        }

        private void ClearSelection()
        {
            foreach (var b in _selectedNoteBorders)
                UpdateNoteVisuals(b, false);
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
            NotesCanvas.Focus(); // Focus Key-Events

            if (_currentTool == EditorTool.Erase)
            {
                DependencyObject dep = e.OriginalSource as DependencyObject;
                while (dep != null && !(dep is Border && ((Border)dep).Tag is NoteData)) 
                    dep = VisualTreeHelper.GetParent(dep);
                if (dep is Border b) 
                    NotesCanvas.Children.Remove(b);
                return;
            }

            if (_currentTool == EditorTool.Select)
                NotesCanvas_SelectTool_MouseLeftButtonDown(sender, e);

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

            if (!_isDragging && !_isResizing && !_isSelecting && _currentTool == EditorTool.Select)
            {
                DependencyObject dep = e.OriginalSource as DependencyObject;
                while (dep != null && !(dep is Border && ((Border)dep).Tag is NoteData)) 
                    dep = VisualTreeHelper.GetParent(dep);

                if (dep is Border b)
                {
                    double mouseX = e.GetPosition(b).X;
                    NotesCanvas.Cursor = (mouseX < 7.0 || mouseX > b.ActualWidth - 7.0) ? Cursors.SizeWE : Cursors.Hand;
                }
                else 
                    NotesCanvas.Cursor = Cursors.Arrow;

                _lastMousePos = cur;
            }

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

            if (_isDragging && _selectedNoteBorders.Any())
            {
                long tickOff = (long)(delta.X / _tickPixelScale);
                int pitchOff = (int)(-delta.Y / _noteHeight);
                if (tickOff != 0 || pitchOff != 0)
                {
                    bool canMove = _selectedNoteBorders.All(nb =>
                    {
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

            if (_isSelecting)
                NotesCanvas_SelectTool_MouseMove(sender, e, cur);

            if (_newNoteBeingCreated != null)
            {
                var d = (NoteData)_newNoteBeingCreated.Tag;
                d.Duration = Math.Max(_gridSnapTicks, Snap((long)(cur.X / _tickPixelScale)) - d.Start);
                UpdateNotePosition(_newNoteBeingCreated);
            }
        }

        private void NotesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Move / Resize
            if ((_isDragging || _isResizing) && _dragStartSnapshots != null && _selectedNoteBorders.Any())
            {
                var endStates = _selectedNoteBorders.Select(b =>
                {
                    var d = (NoteData)b.Tag;
                    return new { Border = b, Data = d, NPitch = d.Pitch, NStart = d.Start, NDuration = d.Duration };
                }).ToList();

                var startStates = _dragStartSnapshots.ToList();
                var firstMatch = endStates.FirstOrDefault();
                var oldMatch = startStates.FirstOrDefault(s => s.Border == firstMatch?.Border);

                if (oldMatch.Border != null && (oldMatch.Pitch != firstMatch.NPitch ||
                    oldMatch.Start != firstMatch.NStart || oldMatch.Duration != firstMatch.NDuration))
                {
                    ExecuteAndRegisterUndo("Modify Note",
                        undoAction: () =>
                        {
                            foreach (var s in startStates)
                            {
                                var d = (NoteData)s.Border.Tag;
                                d.Pitch = s.Pitch; d.Start = s.Start; d.Duration = s.Duration;
                                UpdateNotePosition(s.Border);
                            }
                        },
                        redoAction: () =>
                        {
                            foreach (var s in endStates)
                            {
                                s.Data.Pitch = s.NPitch; s.Data.Start = s.NStart; s.Data.Duration = s.NDuration;
                                UpdateNotePosition(s.Border);
                            }
                        }
                    );
                }
            }
            //Draw new note
            else if (_newNoteBeingCreated != null)
            {
                var createdNote = _newNoteBeingCreated;
                var data = (NoteData)createdNote.Tag;

                int p = data.Pitch;
                long s = data.Start;
                long d = data.Duration;

                ExecuteAndRegisterUndo("Draw Note",
                    undoAction: () => NotesCanvas.Children.Remove(createdNote),
                    redoAction: () =>
                    {
                        data.Pitch = p; data.Start = s; data.Duration = d;
                        if (!NotesCanvas.Children.Contains(createdNote)) NotesCanvas.Children.Add(createdNote);
                        UpdateNotePosition(createdNote);
                    }
                );

                _newNoteBeingCreated = null; // WICHTIG: Hier wird der Stift "losgelassen"
            }

            _isDragging = false;
            _isResizing = false;
            _isSelecting = false;
            _newNoteBeingCreated = null;
            _dragStartSnapshots = null;
            SelectionRect.Visibility = Visibility.Collapsed;

            if (NotesCanvas.IsMouseCaptured)
                NotesCanvas.ReleaseMouseCapture();

            RefreshLayout();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool isArrowKey = e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right;
            if (isArrowKey)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    if (_selectedNoteBorders.Any())
                    {
                        e.Handled = true;
                        MoveSelectedNotes(e.Key);
                    }
                }
            }
        }

        private void MoveSelectedNotes(Key key)
        {
            if (!_selectedNoteBorders.Any()) return;

            //Delta
            int pitchDelta = 0;
            long tickDelta = 0;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                switch (key)
                {
                    case Key.Up: pitchDelta = 12; break;
                    case Key.Down: pitchDelta = -12; break;
                    case Key.Left: tickDelta = -_gridSnapTicks * 4; break;
                    case Key.Right: tickDelta = _gridSnapTicks * 4; break;
                }
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                switch (key)
                {
                    case Key.Up: pitchDelta = 1; break;
                    case Key.Down: pitchDelta = -1; break;
                    case Key.Left: tickDelta = -_gridSnapTicks; break;
                    case Key.Right: tickDelta = _gridSnapTicks; break;
                }
            }

            if (pitchDelta == 0 && tickDelta == 0) return;

            //Snapshot old and new values
            var snapshots = _selectedNoteBorders.Select(b =>
            {
                var d = (NoteData)b.Tag;
                return new
                {
                    Border = b,
                    Data = d,
                    OldPitch = d.Pitch,
                    OldStart = d.Start,
                    NewPitch = Math.Max(0, Math.Min(127, d.Pitch + pitchDelta)),
                    NewStart = Math.Max(0, d.Start + tickDelta)
                };
            }).ToList();

            //to Undo
            ExecuteAndRegisterUndo("Move Notes",
                undoAction: () =>
                {
                    foreach (var s in snapshots)
                    {
                        s.Data.Pitch = s.OldPitch;
                        s.Data.Start = s.OldStart;
                        UpdateNotePosition(s.Border);
                    }
                    ScrollToNote(snapshots.First().Border);
                },
                redoAction: () =>
                {
                    foreach (var s in snapshots)
                    {
                        s.Data.Pitch = s.NewPitch;
                        s.Data.Start = s.NewStart;
                        UpdateNotePosition(s.Border);
                    }
                    ScrollToNote(snapshots.First().Border);
                }
            );
        }

        private void ScrollToNote(Border refNote)
        {
            double noteLeft = Canvas.GetLeft(refNote);
            double noteTop = Canvas.GetTop(refNote);
            double padding = 100;

            if (noteLeft < MainScroll.HorizontalOffset)
                MainScroll.ScrollToHorizontalOffset(Math.Max(0, noteLeft - padding));
            else if (noteLeft + refNote.ActualWidth > MainScroll.HorizontalOffset + MainScroll.ViewportWidth)
                MainScroll.ScrollToHorizontalOffset(noteLeft + refNote.ActualWidth - MainScroll.ViewportWidth + padding);

            if (noteTop < MainScroll.VerticalOffset)
                MainScroll.ScrollToVerticalOffset(Math.Max(0, noteTop - padding));
            else if (noteTop + refNote.ActualHeight > MainScroll.VerticalOffset + MainScroll.ViewportHeight)
                MainScroll.ScrollToVerticalOffset(noteTop + refNote.ActualHeight - MainScroll.ViewportHeight + padding);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //Tool selection
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.None) SwitchTool(EditorTool.Select);
            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.None) SwitchTool(EditorTool.Draw);
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.None) SwitchTool(EditorTool.Erase);

            //Delete selection
            if (e.Key == Key.Delete)
            {
                DeleteSelectedNotes();
                e.Handled = true;
            }

            //Select all
            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ClearSelection();
                foreach (var b in NotesCanvas.Children.OfType<Border>().Where(x => x.Tag is NoteData))
                {
                    _selectedNoteBorders.Add(b);
                    UpdateNoteVisuals(b, true);
                }
                e.Handled = true;
            }

            //Undo
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Undo_Click(null, null);
                e.Handled = true;
            }

            //Redo
            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Redo_Click(null, null);
                e.Handled = true;
            }

            //Copy
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

            //Paste
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

            //Duplicate
            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_selectedNoteBorders.Any())
                {
                    List<Border> newBorders = new List<Border>();
                    long offset = _gridSnapTicks;

                    ExecuteAndRegisterUndo("Duplicate",
                        undoAction: () =>
                        {
                            foreach (var b in newBorders) NotesCanvas.Children.Remove(b);
                        },
                        redoAction: () =>
                        {
                            foreach (var b in _selectedNoteBorders.ToList())
                            {
                                var oldData = (NoteData)b.Tag;
                                var newNote = AddNoteUI(oldData.Pitch, oldData.Start + offset, oldData.Duration);
                                newBorders.Add(newNote);
                            }
                            ClearSelection();
                            foreach (var nb in newBorders) { _selectedNoteBorders.Add(nb); UpdateNoteVisuals(nb, true); }
                        }
                    );
                }
                e.Handled = true;
            }
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
            var highest = NotesCanvas.Children.OfType<Border>().Where(b => b.Tag is NoteData).OrderByDescending(b => ((NoteData)b.Tag).Pitch).FirstOrDefault();
            if (highest != null) MainScroll.ScrollToVerticalOffset(Math.Max(0, Canvas.GetTop(highest) - 100));
        }

        private void RefreshLayout()
        {
            double w = Math.Max(MainScroll.ActualWidth, (_maxTick * _tickPixelScale) + 500);
            EditorGrid.Width = w; EditorGrid.Height = 128 * _noteHeight;
            GridCanvas.Width = w; GridCanvas.Height = EditorGrid.Height;
            RulerCanvas.Width = w; AutomationCanvas.Width = w; MarkersCanvas.Width = w;
            DrawGrid();
            DrawRuler();
            DrawMarkers();
            DrawAutomation();
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

        #region LowerWindow
        private void DrawAutomation()
        {
            AutomationCanvas.Children.Clear();

            if (!_programChanges.Any())
                return;

            Polyline line = new Polyline { Stroke = Brushes.Orange, StrokeThickness = 2, IsHitTestVisible = false };
            double h = AutomationCanvas.ActualHeight > 0 ? AutomationCanvas.ActualHeight : 80;

            foreach (var pt in _programChanges.OrderBy(p => p.Tick))
            {
                double x = pt.Tick * _tickPixelScale;
                double y = h - (pt.Value / 127.0 * h);

                if (line.Points.Any())
                    line.Points.Add(new Point(x, line.Points.Last().Y));

                line.Points.Add(new Point(x, y));
                Ellipse point = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.Orange,
                    Tag = pt,
                    Cursor = Cursors.Hand,
                    Opacity = 0.8 // Standard-Deckkraft
                };

                // --- SCHICKES HOVER-LABEL (POPUP) ---
                point.MouseEnter += (s, e) =>
                {
                    // Visuelles Feedback
                    ((Ellipse)s).Opacity = 1.0;
                    ((Ellipse)s).Width = 8;
                    ((Ellipse)s).Height = 8;
                    Canvas.SetLeft(((Ellipse)s), x - 4);
                    Canvas.SetTop(((Ellipse)s), y - 4);

                    ValuePopupText.Text = (pt.Value + 1).ToString();
                    ValuePopup.IsOpen = true;
                };

                point.MouseLeave += (s, e) =>
                {
                    // Visuelles Feedback zurücksetzen
                    ((Ellipse)s).Opacity = 0.8;
                    ((Ellipse)s).Width = 6;
                    ((Ellipse)s).Height = 6;
                    Canvas.SetLeft(((Ellipse)s), x - 3); // Re-zentrieren
                    Canvas.SetTop(((Ellipse)s), y - 3);

                    // Popup verstecken
                    ValuePopup.IsOpen = false;
                };
                // ------------------------------------

                point.MouseLeftButtonDown += AutomationPoint_MouseDown;
                Canvas.SetLeft(point, x - 3);
                Canvas.SetTop(point, y - 3);
                AutomationCanvas.Children.Add(point);
            }

            if (line.Points.Any())
                line.Points.Add(new Point(AutomationCanvas.ActualWidth, line.Points.Last().Y));

            AutomationCanvas.Children.Insert(0, line);
        }

        private void AutomationCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is Ellipse)
                return;

            if (e.ClickCount == 2)
            {
                Point pos = e.GetPosition(AutomationCanvas);
                long ticks = Snap(XToTicks(pos.X));
                int val = (int)((1.0 - (pos.Y / AutomationCanvas.ActualHeight)) * 127);
                val = Math.Max(0, Math.Min(val, 127));

                var newPoint = new AutomationPoint { Tick = ticks, Value = val };
                _programChanges.Add(newPoint);

                DrawAutomation();
            }
        }

        private void AutomationCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingAutomation || _activeAutomationPoint == null) return;

            Point mousePos = e.GetPosition(AutomationCanvas);
            var data = (AutomationPoint)_activeAutomationPoint.Tag;

            data.Tick = Math.Max(0, Snap(XToTicks(mousePos.X)));
            double height = AutomationCanvas.ActualHeight;
            if (height > 0)
            {
                double normalized = 1.0 - (mousePos.Y / height);
                data.Value = Math.Max(0, Math.Min((int)(normalized * 127), 127));
            }

            ValuePopupText.Text = (data.Value + 1).ToString();
            AutomationValueLabel.Text = (data.Value + 1).ToString();

            UpdateAutomationPointPosition(_activeAutomationPoint);
        }

        private void AutomationPoint_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Ellipse point && point.Tag is AutomationPoint data)
            {
                _activeAutomationPoint = point;
                _isDraggingAutomation = true;
                _automationStartSnapshot = new AutomationPoint
                {
                    Tick = data.Tick,
                    Value = data.Value
                };

                point.CaptureMouse();
                e.Handled = true;
            }
        }

        private void AutomationCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingAutomation && _activeAutomationPoint != null)
            {
                var element = _activeAutomationPoint;
                var data = (AutomationPoint)element.Tag;

                if (_automationStartSnapshot != null &&
                   (data.Value != _automationStartSnapshot.Value || data.Tick != _automationStartSnapshot.Tick))
                {
                    int oldVal = _automationStartSnapshot.Value;
                    long oldTick = _automationStartSnapshot.Tick;
                    int newVal = data.Value;
                    long newTick = data.Tick;

                    ExecuteAndRegisterUndo("Move Automation",
                        undoAction: () =>
                        {
                            data.Value = oldVal; data.Tick = oldTick;
                            DrawAutomation();
                        },
                        redoAction: () =>
                        {
                            data.Value = newVal; data.Tick = newTick;
                            DrawAutomation();
                        }
                    );
                }

                if (element.IsMouseCaptured)
                    element.ReleaseMouseCapture();
            }
            ValuePopup.IsOpen = false;
            _isDraggingAutomation = false;
            _activeAutomationPoint = null;
            _automationStartSnapshot = null;
            DrawAutomation();
        }

        private void UpdateAutomationPointPosition(FrameworkElement element)
        {
            if (element == null || !(element.Tag is AutomationPoint data)) return;

            double x = TicksToX(data.Tick);
            double height = AutomationCanvas.ActualHeight;
            double y = height * (1.0 - (data.Value / 127.0));
            Canvas.SetLeft(element, x - (element.ActualWidth / 2));
            Canvas.SetTop(element, y - (element.ActualHeight / 2));
        }

        private void AutomationCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Height > 0)
            {
                DrawAutomation();
            }
        }

        private double _zoomX = 0.1;

        private double TicksToX(long ticks)
        {
            return ticks * _zoomX;
        }

        private long XToTicks(double x)
        {
            if (_zoomX <= 0) return 0;
            return (long)(x / _zoomX);
        }
        #endregion

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
        private void ExecuteAndRegisterUndo(string name, Action undoAction, Action redoAction)
        {
            redoAction();
            _undoStack.Push(new UndoAction { Name = name, ApplyUndo = undoAction, ApplyRedo = redoAction });
            _redoStack.Clear();

            // Max. 50 Steps
            if (_undoStack.Count > 50)
            {
                var temp = _undoStack.Reverse().Skip(1).Reverse();
                _undoStack = new Stack<UndoAction>(temp);
            }
        }

        private void DeleteSelectedNotes()
        {
            if (!_selectedNoteBorders.Any()) return;

            var notesToDelete = _selectedNoteBorders.ToList();

            ExecuteAndRegisterUndo("Delete",
                undoAction: () =>
                {
                    foreach (var b in notesToDelete) NotesCanvas.Children.Add(b);
                },
                redoAction: () =>
                {
                    foreach (var b in notesToDelete)
                    {
                        NotesCanvas.Children.Remove(b);
                        _selectedNoteBorders.Remove(b);
                    }
                }
            );
        }

        #region Save Logic
        public TrackChunk ResultTrackChunk { get; private set; }

        private void SaveTrack_Click(object sender, RoutedEventArgs e)
        {
            var trackChunk = new TrackChunk();
            var notes = NotesCanvas.Children.OfType<Border>()
                .Where(b => b.Tag is NoteData)
                .Select(b => (NoteData)b.Tag)
                .ToList();

            var rawEvents = new List<(long AbsoluteTick, MidiEvent Event)>();
            foreach (var note in notes)
            {
                rawEvents.Add((note.Start, new NoteOnEvent((SevenBitNumber)note.Pitch, (SevenBitNumber)64)));
                rawEvents.Add((note.Start + note.Duration, new NoteOffEvent((SevenBitNumber)note.Pitch, (SevenBitNumber)0)));
            }

            if (_programChanges != null)
            {
                foreach (var pt in _programChanges)
                {
                    var pcEvent = new ProgramChangeEvent((SevenBitNumber)pt.Value);
                    rawEvents.Add((pt.Tick, pcEvent));
                }
            }

            var sorted = rawEvents
                .OrderBy(x => x.AbsoluteTick)
                .ThenBy(x =>
                {
                    if (x.Event is ProgramChangeEvent) return 0;
                    if (x.Event is NoteOnEvent) return 1;
                    return 2;
                })
                .ToList();

            long lastTick = 0;
            foreach (var item in sorted)
            {
                item.Event.DeltaTime = item.AbsoluteTick - lastTick;
                trackChunk.Events.Add(item.Event);
                lastTick = item.AbsoluteTick;
            }

            this.ResultTrackChunk = trackChunk;
            this.DialogResult = true;
            this.Close();
        }
        #endregion

        #region MenuStuff
        private void AutoTranspose_Click(object sender, RoutedEventArgs e)
        {
            var targets = _selectedNoteBorders.Any()
                ? _selectedNoteBorders.ToList()
                : NotesCanvas.Children.OfType<Border>().Where(b => b.Tag is NoteData).ToList();

            if (!targets.Any())
                return;

            double averagePitch = targets.Average(b => ((NoteData)b.Tag).Pitch);
            int targetCenter = 66;
            int octaveShift = (int)Math.Round((targetCenter - averagePitch) / 12.0) * 12;

            if (octaveShift == 0)
                return;

            var startStates = targets.Select(b =>
            {
                var d = (NoteData)b.Tag;
                return (Border: b, OldPitch: d.Pitch);
            }).ToList();

            ExecuteAndRegisterUndo($"Auto-Transpose ({octaveShift})",
                undoAction: () =>
                {
                    foreach (var s in startStates)
                    {
                        var d = (NoteData)s.Border.Tag;
                        d.Pitch = s.OldPitch;
                        UpdateNotePosition(s.Border);
                    }
                },
                redoAction: () =>
                {
                    foreach (var s in startStates)
                    {
                        var d = (NoteData)s.Border.Tag;
                        d.Pitch += octaveShift;
                        UpdateNotePosition(s.Border);
                    }
                }
            );
            RefreshLayout();
        }

        private void Quantize_Click(object sender, RoutedEventArgs e)
        {
            var targets = _selectedNoteBorders.Any()
                ? _selectedNoteBorders.ToList()
                : NotesCanvas.Children.OfType<Border>().Where(b => b.Tag is NoteData).ToList();

            if (!targets.Any()) return;

            var snapshots = targets.Select(b =>
            {
                var d = (NoteData)b.Tag;
                return new
                {
                    Border = b,
                    Data = d,
                    OldStart = d.Start,
                    OldDuration = d.Duration
                };
            }).ToList();

            ExecuteAndRegisterUndo("Quantize",
                undoAction: () =>
                {
                    foreach (var s in snapshots)
                    {
                        s.Data.Start = s.OldStart;
                        s.Data.Duration = s.OldDuration;
                        UpdateNotePosition(s.Border);
                    }
                },
                redoAction: () =>
                {
                    foreach (var s in snapshots)
                    {
                        s.Data.Start = Snap(s.Data.Start);
                        s.Data.Duration = Math.Max(_gridSnapTicks, Snap(s.Data.Duration));
                        UpdateNotePosition(s.Border);
                    }
                }
            );

            RefreshLayout();
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Any())
            {
                var action = _undoStack.Pop();
                action.ApplyUndo();
                _redoStack.Push(action);
            }
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (_redoStack.Any())
            {
                var action = _redoStack.Pop();
                action.ApplyRedo();
                _undoStack.Push(action);
            }
        }
        #endregion
    }
}