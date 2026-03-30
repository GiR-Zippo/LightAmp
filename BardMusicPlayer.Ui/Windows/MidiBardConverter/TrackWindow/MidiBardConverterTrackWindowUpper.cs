/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    /// <summary>
    /// Upper / Piano Roll area.
    /// Keyboard shortcuts:
    ///   S/D/E          - Switch tools
    ///   Delete         - Delete selected notes
    ///   Ctrl+A         - Select all
    ///   Ctrl+C/V/D     - Copy / Paste / Duplicate
    ///   Ctrl/Shift+Arrow - Move notes (via global handler)
    ///   Ctrl+Z/Y       - Undo/Redo (via global handler)
    /// </summary>
    public partial class MidiBardConverterTrackWindow
    {
        // Editor tool
        public enum EditorTool { Select, Draw, Erase }
        private EditorTool _currentTool { get; set; } = EditorTool.Select;

        // Drag'n'Drop
        private bool _isDragging { get; set; } = false;
        private List<(Note Note, int Pitch, long Start, long Duration)> _dragStartSnapshots { get; set; }

        // Select
        private Point _selectionStartPoint { get; set; }
        private bool _isSelecting { get; set; } = false;
        private HashSet<Note> _selectedAtStartOfSelection { get; set; } = new HashSet<Note>();

        // resize
        private bool _isResizing { get; set; } = false;
        private bool _isResizingLeft { get; set; } = false;

        // note creation
        private Note _newNoteBeingCreated { get; set; } = null;
        private Note _activeNote { get; set; } = null;

        // ── Note ──────────────────────────────────────────────────────
        private Note AddNote(int pitch, long start, long duration)
        {
            var n = new Note((SevenBitNumber)pitch, duration, start);
            _notes.Add(n);
            RenderNotes();
            return n;
        }

        #region select stuff
        private void SelectNote(Note note)
        {
            _selectedNotes.Add(note);
            RenderNotes();
        }

        private void ClearSelection()
        {
            _selectedNotes.Clear();
            RenderNotes();
        }
        #endregion

        #region Mouse Stuff
        private void NotesCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePos = e.GetPosition(NotesCanvas);
            NotesCanvas.Focus();

            // ERASE
            if (_currentTool == EditorTool.Erase)
            {
                var hit = _noteHost.HitTest(_lastMousePos, _notes, _tickPixelScale, _noteHeight);
                if (hit != null)
                {
                    var deleted = hit;
                    ExecuteAndRegisterUndo("Erase Note",
                        undoAction: () => { _notes.Add(deleted); RenderNotes(); },
                        redoAction: () => { _notes.Remove(deleted); _selectedNotes.Remove(deleted); RenderNotes(); });
                }
                return;
            }

            // SELECT
            if (_currentTool == EditorTool.Select)
            {
                var hitNote = _noteHost.HitTest(_lastMousePos, _notes, _tickPixelScale, _noteHeight);
                if (hitNote != null)
                {
                    _activeNote = hitNote;
                    _selectionStartPoint = _lastMousePos; // start for relative movement

                    // If hitNote, select the entire selection. Otherwise -> only hitNote.
                    var notesToSnapshot = _selectedNotes.Contains(hitNote)
                        ? _selectedNotes.ToList()
                        : new List<Note> { hitNote };

                    // fill Snapshot
                    _dragStartSnapshots = notesToSnapshot
                        .Select(n => (Note: (Note)n.Clone(), Pitch: (int)n.NoteNumber, Start: n.Time, Duration: n.Length))
                        .ToList();

                    bool? handle = _noteHost.HitTestResizeHandle(_lastMousePos, hitNote, _tickPixelScale, _noteHeight);
                    if (handle.HasValue) { _isResizing = true; _isResizingLeft = handle.Value; }
                    else _isDragging = true;

                    if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !_selectedNotes.Contains(hitNote))
                        ClearSelection();
                    if (!_selectedNotes.Contains(hitNote))
                        SelectNote(hitNote);

                    NotesCanvas.CaptureMouse();
                    e.Handled = true;
                    return;
                }

                // Rubber-Band
                _isSelecting = true;
                _isDragging = false;
                _selectionStartPoint = _lastMousePos;
                _selectedAtStartOfSelection = new HashSet<Note>(
                    Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)
                        ? _selectedNotes : Enumerable.Empty<Note>());

                if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                    ClearSelection();

                SelectionRect.Visibility = Visibility.Visible;
                Canvas.SetLeft(SelectionRect, _lastMousePos.X);
                Canvas.SetTop(SelectionRect, _lastMousePos.Y);
                SelectionRect.Width = SelectionRect.Height = 0;
                NotesCanvas.CaptureMouse();
            }

            // DRAW
            if (_currentTool == EditorTool.Draw && e.Source == NotesCanvas)
            {
                int p = 127 - (int)(_lastMousePos.Y / _noteHeight);
                long s = Snap((long)(_lastMousePos.X / _tickPixelScale));
                _newNoteBeingCreated = AddNote(p, s, _gridSnapTicks);
                NotesCanvas.CaptureMouse();
            }
        }

        private void NotesCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point cur = e.GetPosition(NotesCanvas);
            Vector delta = cur - _lastMousePos;

            // Cursor-Hint
            if (!_isDragging && !_isResizing && !_isSelecting && _currentTool == EditorTool.Select)
            {
                var hovered = _noteHost.HitTest(cur, _notes, _tickPixelScale, _noteHeight);
                NotesCanvas.Cursor = hovered != null
                    ? (_noteHost.HitTestResizeHandle(cur, hovered, _tickPixelScale, _noteHeight).HasValue
                        ? Cursors.SizeWE : Cursors.Hand)
                    : Cursors.Arrow;
                _lastMousePos = cur;
            }

            // RESIZE – snap to grid
            if (_isResizing && _dragStartSnapshots != null)
            {
                long deltaTicks = Snap((long)((cur.X - _selectionStartPoint.X) / _tickPixelScale));

                // Wir mappen über den Index vom Snapshot auf die aktiven Noten
                var targets = _selectedNotes.Contains(_activeNote) ? _selectedNotes.ToList() : new List<Note> { _activeNote };

                for (int i = 0; i < targets.Count; i++)
                {
                    if (i >= _dragStartSnapshots.Count) break;
                    var target = targets[i];
                    var source = _dragStartSnapshots[i];

                    if (_isResizingLeft)
                    {
                        long newTime = source.Start + deltaTicks;
                        long newLen = source.Duration - deltaTicks;
                        if (newLen >= _gridSnapTicks && newTime >= 0)
                        { target.Time = newTime; target.Length = newLen; }
                    }
                    else
                    {
                        long newLen = source.Duration + deltaTicks;
                        if (newLen >= _gridSnapTicks) target.Length = newLen;
                    }
                }
                RenderNotes();
                return;
            }

            // DRAG
            if (_isDragging && _dragStartSnapshots != null)
            {
                long deltaTicks = Snap((long)((cur.X - _selectionStartPoint.X) / _tickPixelScale));
                int deltaPitch = (int)Math.Round((_selectionStartPoint.Y - cur.Y) / _noteHeight);

                var targets = _selectedNotes.Contains(_activeNote) ? _selectedNotes.ToList() : new List<Note> { _activeNote };

                for (int i = 0; i < targets.Count; i++)
                {
                    if (i >= _dragStartSnapshots.Count) break;
                    var target = targets[i];
                    var source = _dragStartSnapshots[i];

                    target.Time = Math.Max(0, source.Start + deltaTicks);
                    int newPitch = source.Pitch + deltaPitch;
                    if (newPitch >= 0 && newPitch <= 127)
                        target.NoteNumber = (SevenBitNumber)newPitch;
                }
                RenderNotes();
                return;
            }

            // RUBBER-BAND
            if (_isSelecting)
            {
                double x = Math.Min(_selectionStartPoint.X, cur.X);
                double y = Math.Min(_selectionStartPoint.Y, cur.Y);
                double w = Math.Abs(_selectionStartPoint.X - cur.X);
                double h = Math.Abs(_selectionStartPoint.Y - cur.Y);
                Canvas.SetLeft(SelectionRect, x); Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = w; SelectionRect.Height = h;

                Rect selRect = new Rect(x, y, w, h);
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

                foreach (var n in _notes)
                {
                    double nx = n.Time * _tickPixelScale;
                    double ny = (127 - (int)n.NoteNumber) * _noteHeight;
                    double nw = Math.Max(2, n.Length * _tickPixelScale);
                    bool hit = selRect.IntersectsWith(new Rect(nx, ny, nw, _noteHeight));
                    if (isCtrl)
                    {
                        bool was = _selectedAtStartOfSelection.Contains(n);
                        if (hit) { if (was) _selectedNotes.Remove(n); else _selectedNotes.Add(n); }
                        else { if (was) _selectedNotes.Add(n); else _selectedNotes.Remove(n); }
                    }
                    else
                    {
                        if (hit) _selectedNotes.Add(n); else _selectedNotes.Remove(n);
                    }
                }
                RenderNotes();
            }

            // DRAW
            if (_newNoteBeingCreated != null)
            {
                _newNoteBeingCreated.Length = Math.Max(
                    _gridSnapTicks,
                    Snap((long)(cur.X / _tickPixelScale)) - _newNoteBeingCreated.Time);
                RenderNotes();
            }
        }

        private void NotesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if ((_isDragging || _isResizing) && _dragStartSnapshots != null && _activeNote != null)
            {
                // Same we have in MouseMove
                var targets = _selectedNotes.Contains(_activeNote)
                    ? _selectedNotes.ToList()
                    : new List<Note> { _activeNote };

                // Create a snapshot
                var endStates = targets
                    .Select(n => (Note: n, NPitch: (int)n.NoteNumber, NTime: n.Time, NLen: n.Length))
                    .ToList();

                var startStates = _dragStartSnapshots;

                // Something changed?
                var firstTarget = endStates.FirstOrDefault();
                var firstSource = startStates.FirstOrDefault();

                if (firstTarget.Note != null && firstSource.Note != null &&
                    (firstSource.Pitch != firstTarget.NPitch ||
                     firstSource.Start != firstTarget.NTime ||
                     firstSource.Duration != firstTarget.NLen))
                {
                    ExecuteAndRegisterUndo("Modify Note",
                        undoAction: () =>
                        {
                            for (int i = 0; i < startStates.Count; i++)
                            {
                                var s = startStates[i];
                                targets[i].NoteNumber = (SevenBitNumber)s.Pitch;
                                targets[i].Time = s.Start;
                                targets[i].Length = s.Duration;
                            }
                            RenderNotes();
                        },
                        redoAction: () =>
                        {
                            for (int i = 0; i < endStates.Count; i++)
                            {
                                var eState = endStates[i];
                                targets[i].NoteNumber = (SevenBitNumber)eState.NPitch;
                                targets[i].Time = eState.NTime;
                                targets[i].Length = eState.NLen;
                            }
                            RenderNotes();
                        });
                }
            }
            else if (_newNoteBeingCreated != null)
            {
                var created = _newNoteBeingCreated;
                var snap = (Pitch: (int)created.NoteNumber, Time: created.Time, Len: created.Length);
                ExecuteAndRegisterUndo("Draw Note",
                    undoAction: () => { _notes.Remove(created); _selectedNotes.Remove(created); RenderNotes(); },
                    redoAction: () =>
                    {
                        created.NoteNumber = (SevenBitNumber)snap.Pitch;
                        created.Time = snap.Time; created.Length = snap.Len;
                        if (!_notes.Contains(created)) _notes.Add(created);
                        RenderNotes();
                    });
            }

            // Cleanup
            _isDragging = _isResizing = _isSelecting = false;
            _newNoteBeingCreated = null;
            _dragStartSnapshots = null;
            _activeNote = null;
            SelectionRect.Visibility = Visibility.Collapsed;
            if (NotesCanvas.IsMouseCaptured) NotesCanvas.ReleaseMouseCapture();
            RefreshLayout();
        }
        #endregion

        #region Note functions
        private void RenderNotes()
        {
            if (_noteHost == null) return;
            _noteHost.Width = NotesCanvas.Width;
            _noteHost.Height = NotesCanvas.Height;
            var viewport = new Rect(
                MainScroll.HorizontalOffset,
                MainScroll.VerticalOffset,
                MainScroll.ViewportWidth > 0 ? MainScroll.ViewportWidth : ActualWidth,
                MainScroll.ViewportHeight > 0 ? MainScroll.ViewportHeight : ActualHeight);
            _noteHost.Render(_notes, _selectedNotes, _tickPixelScale, _noteHeight, viewport);
            _lastRenderedHOffset = MainScroll.HorizontalOffset;
            _noteHostTranslate.X = 0;
        }

        private void DeleteSelectedNotes()
        {
            if (!_selectedNotes.Any()) return;
            var toDelete = _selectedNotes.ToList();
            ExecuteAndRegisterUndo("Delete",
                undoAction: () => { foreach (var n in toDelete) if (!_notes.Contains(n)) _notes.Add(n); RenderNotes(); },
                redoAction: () => { foreach (var n in toDelete) { _notes.Remove(n); _selectedNotes.Remove(n); } RenderNotes(); });
        }

        private void MoveSelectedNotes(Key key)
        {
            if (!_selectedNotes.Any()) return;
            int pitchDelta = 0;
            long tickDelta = 0;

            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                switch (key)
                {
                    case Key.Up: pitchDelta = 12; break;
                    case Key.Down: pitchDelta = -12; break;
                    case Key.Left: tickDelta = -_gridSnapTicks * 4; break;
                    case Key.Right: tickDelta = _gridSnapTicks * 4; break;
                }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                switch (key)
                {
                    case Key.Up: pitchDelta = 1; break;
                    case Key.Down: pitchDelta = -1; break;
                    case Key.Left: tickDelta = -_gridSnapTicks; break;
                    case Key.Right: tickDelta = _gridSnapTicks; break;
                }

            if (pitchDelta == 0 && tickDelta == 0) return;

            var snapshots = _selectedNotes.Select(n => new
            {
                Note = n,
                OldPitch = (int)n.NoteNumber,
                OldTime = n.Time,
                NewPitch = Math.Max(0, Math.Min(127, (int)n.NoteNumber + pitchDelta)),
                NewTime = Math.Max(0, n.Time + tickDelta)
            }).ToList();

            var first = snapshots.FirstOrDefault();
            ExecuteAndRegisterUndo("Move Notes",
                undoAction: () =>
                {
                    foreach (var s in snapshots)
                    { s.Note.NoteNumber = (SevenBitNumber)s.OldPitch; s.Note.Time = s.OldTime; }
                    if (first != null) ScrollToNote(first.Note);
                    RenderNotes();
                },
                redoAction: () =>
                {
                    foreach (var s in snapshots)
                    { s.Note.NoteNumber = (SevenBitNumber)s.NewPitch; s.Note.Time = s.NewTime; }
                    if (first != null) ScrollToNote(first.Note);
                    RenderNotes();
                });
        }
        #endregion

        #region Helper
        private void FocusHighestNote()
        {
            if (!_notes.Any()) return;
            var highest = _notes.OrderByDescending(n => (int)n.NoteNumber).First();
            MainScroll.ScrollToVerticalOffset(Math.Max(0, (127 - (int)highest.NoteNumber) * _noteHeight - 100));
        }

        private void ScrollToNote(Note note)
        {
            double left = note.Time * _tickPixelScale;
            double top = (127 - (int)note.NoteNumber) * _noteHeight;
            double width = Math.Max(2, note.Length * _tickPixelScale);
            double padding = 100;
            if (left < MainScroll.HorizontalOffset)
                MainScroll.ScrollToHorizontalOffset(Math.Max(0, left - padding));
            else if (left + width > MainScroll.HorizontalOffset + MainScroll.ViewportWidth)
                MainScroll.ScrollToHorizontalOffset(left + width - MainScroll.ViewportWidth + padding);
            if (top < MainScroll.VerticalOffset)
                MainScroll.ScrollToVerticalOffset(Math.Max(0, top - padding));
            else if (top + _noteHeight > MainScroll.VerticalOffset + MainScroll.ViewportHeight)
                MainScroll.ScrollToVerticalOffset(top + _noteHeight - MainScroll.ViewportHeight + padding);
        }
        #endregion

        #region Tool
        private void SwitchTool(EditorTool tool)
        {
            _currentTool = tool;
            UpdateToolUI();
            ClearSelection();
        }

        private void UpdateToolUI()
        {
            CurrentToolText.Text = $"Tool: {_currentTool.ToString().ToUpper()}";
            NotesCanvas.Cursor = _currentTool == EditorTool.Draw ? Cursors.Pen
                                 : _currentTool == EditorTool.Erase ? Cursors.Cross
                                 : Cursors.Arrow;
        }
        #endregion

        // ══════════════════════════════════════════════════════════════
        // UPPER KEYBOARD HANDLER
        // Active only when NotesCanvas has focus.
        // Handles: Tool Switch, Select All, Delete, Copy/Paste/Duplicate
        // ══════════════════════════════════════════════════════════════
        private void NotesCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            // Tool-Switch
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.None) { SwitchTool(EditorTool.Select); return; }
            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.None) { SwitchTool(EditorTool.Draw); return; }
            if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.None) { SwitchTool(EditorTool.Erase); return; }

            // Delete
            if (e.Key == Key.Delete)
            { DeleteSelectedNotes(); e.Handled = true; return; }

            // Ctrl+A – alles selektieren
            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _selectedNotes.Clear();
                foreach (var n in _notes) _selectedNotes.Add(n);
                RenderNotes();
                e.Handled = true;
                return;
            }

            // Ctrl+C – Copy
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_selectedNotes.Any())
                {
                    _clipboard = _selectedNotes
                        .Select(n => new Note(n.NoteNumber, n.Length, n.Time)
                        { Velocity = n.Velocity, OffVelocity = n.OffVelocity, Channel = n.Channel })
                        .ToList();
                    e.Handled = true;
                }
                return;
            }

            // Ctrl+V – Paste
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_clipboard.Any())
                {
                    long minStart = _clipboard.Min(n => n.Time);
                    long targetStart = Snap((long)(_lastMousePos.X / _tickPixelScale));
                    var pasted = _clipboard
                        .Select(old => new Note(old.NoteNumber, old.Length, old.Time - minStart + targetStart)
                        { Velocity = old.Velocity, OffVelocity = old.OffVelocity, Channel = old.Channel })
                        .ToList();

                    ExecuteAndRegisterUndo("Paste",
                        undoAction: () =>
                        {
                            foreach (var n in pasted) { _notes.Remove(n); _selectedNotes.Remove(n); }
                            RenderNotes();
                        },
                        redoAction: () =>
                        {
                            _selectedNotes.Clear();
                            foreach (var n in pasted) { if (!_notes.Contains(n)) _notes.Add(n); _selectedNotes.Add(n); }
                            RenderNotes();
                        });
                    RefreshLayout();
                    e.Handled = true;
                }
                return;
            }

            // Ctrl+D – Duplicate
            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (_selectedNotes.Any())
                {
                    var originals = _selectedNotes.ToList();
                    var dupes = new List<Note>();
                    ExecuteAndRegisterUndo("Duplicate",
                        undoAction: () =>
                        {
                            foreach (var n in dupes) { _notes.Remove(n); _selectedNotes.Remove(n); }
                            foreach (var n in originals) _selectedNotes.Add(n);
                            RenderNotes();
                        },
                        redoAction: () =>
                        {
                            dupes.Clear();
                            foreach (var old in originals)
                            {
                                var n = new Note(old.NoteNumber, old.Length, old.Time + _gridSnapTicks)
                                { Velocity = old.Velocity, OffVelocity = old.OffVelocity, Channel = old.Channel };
                                dupes.Add(n); _notes.Add(n);
                            }
                            _selectedNotes.Clear();
                            foreach (var n in dupes) _selectedNotes.Add(n);
                            RenderNotes();
                        });
                }
                e.Handled = true;
            }
        }
    }
}
