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

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
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
        //Undo / Redo stack
        private Stack<UndoAction> _undoStack { get; set; } = new Stack<UndoAction>();
        private Stack<UndoAction> _redoStack { get; set; } = new Stack<UndoAction>();

        private TrackChunk _activeTrack { get; set; }

        private double _noteHeight { get; set; } = 20;
        private double _tickPixelScale { get; set; } = 0.1;
        private long _gridSnapTicks { get; set; } = 120;
        private short _ticksPerQuarterNote { get; set; } = 480;
        private long _maxTick { get; set; } = 4800;
        private EditorTool _currentTool { get; set; } = EditorTool.Select;
        private TempoMap _tempoMap { get; set; }

        private Point _lastMousePos { get; set; }
        private bool _isDragging { get; set; } = false;
        private bool _isResizing { get; set; } = false;
        private bool _isResizingLeft { get; set; } = false;
        private Point _selectionStartPoint { get; set; }
        private bool _isSelecting { get; set; } = false;
        private List<Border> _selectedNoteBorders { get; set; } = new List<Border>();
        private Border _newNoteBeingCreated { get; set; } = null;
        private List<NoteData> _clipboard { get; set; } = new List<NoteData>();
        private List<(Border Border, int Pitch, long Start, long Duration)> _dragStartSnapshots { get; set; }
        private List<Border> _selectedBordersAtStartOfSelection { get; set; } = new List<Border>();

        public MidiBardConverterTrackWindow(MidiFile midiFile, TrackChunk track)
        {
            InitializeComponent();
            _tempoMap = midiFile.GetTempoMap();
            if (midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision tpq)
                _ticksPerQuarterNote = tpq.TicksPerQuarterNote;

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

            _programChanges = track.GetTimedEvents()
                                   .Where(te => te.Event.EventType == MidiEventType.ProgramChange)
                                   .Select(te => new AutomationPoint { Tick = te.Time, Value = ((ProgramChangeEvent)te.Event).ProgramNumber })
                                   .OrderBy(p => p.Tick)
                                   .ToList();
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

        private void FocusHighestNote()
        {
            var highest = NotesCanvas.Children.OfType<Border>().Where(b => b.Tag is NoteData).OrderByDescending(b => ((NoteData)b.Tag).Pitch).FirstOrDefault();
            if (highest != null) MainScroll.ScrollToVerticalOffset(Math.Max(0, Canvas.GetTop(highest) - 100));
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