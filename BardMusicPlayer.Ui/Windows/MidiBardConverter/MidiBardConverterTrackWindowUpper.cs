/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    /// <summary>
    /// The note display part
    /// </summary>
    public partial class MidiBardConverterTrackWindow
    {
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
                        _isDragging = true;

                    _dragStartSnapshots = _selectedNoteBorders.Select(b =>
                    {
                        var d = (NoteData)b.Tag;
                        return (b, d.Pitch, d.Start, d.Duration);
                    }).ToList();

                    if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !_selectedNoteBorders.Contains(hitNote))
                        ClearSelection();
                    if (!_selectedNoteBorders.Contains(hitNote))
                    {
                        _selectedNoteBorders.Add(hitNote);
                        UpdateNoteVisuals(hitNote, true);
                    }

                    NotesCanvas.CaptureMouse();
                    e.Handled = true;
                    return;
                }

                _isSelecting = true;
                _isDragging = false;
                _selectionStartPoint = _lastMousePos;
                _selectedBordersAtStartOfSelection = _selectedNoteBorders.ToList();

                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    _selectedBordersAtStartOfSelection = _selectedNoteBorders.ToList();
                else
                {
                    ClearSelection();
                    _selectedBordersAtStartOfSelection.Clear();
                }

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
            {
                double x = Math.Min(_selectionStartPoint.X, cur.X);
                double y = Math.Min(_selectionStartPoint.Y, cur.Y);
                double w = Math.Abs(_selectionStartPoint.X - cur.X);
                double h = Math.Abs(_selectionStartPoint.Y - cur.Y);

                Canvas.SetLeft(SelectionRect, x);
                Canvas.SetTop(SelectionRect, y);
                SelectionRect.Width = w;
                SelectionRect.Height = h;

                Rect sel = new Rect(x, y, w, h);
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

                foreach (var b in NotesCanvas.Children.OfType<Border>().Where(node => node.Tag is NoteData))
                {
                    bool hit = sel.IntersectsWith(new Rect(Canvas.GetLeft(b), Canvas.GetTop(b), b.ActualWidth, b.ActualHeight));

                    if (isCtrl)
                    {
                        // --- INVERT MODUS ---
                        bool wasSelectedAtStart = _selectedBordersAtStartOfSelection.Contains(b);
                        if (hit)
                        {
                            // Invertieren: Wenn vorher an -> jetzt aus, wenn vorher aus -> jetzt an
                            if (wasSelectedAtStart)
                            {
                                if (_selectedNoteBorders.Contains(b))
                                {
                                    _selectedNoteBorders.Remove(b);
                                    UpdateNoteVisuals(b, false);
                                }
                            }
                            else
                            {
                                if (!_selectedNoteBorders.Contains(b))
                                {
                                    _selectedNoteBorders.Add(b);
                                    UpdateNoteVisuals(b, true);
                                }
                            }
                        }
                        else
                        {
                            // Außerhalb des Kastens: Den Zustand vom Start des Klicks wiederherstellen
                            if (wasSelectedAtStart)
                            {
                                if (!_selectedNoteBorders.Contains(b))
                                {
                                    _selectedNoteBorders.Add(b);
                                    UpdateNoteVisuals(b, true);
                                }
                            }
                            else
                            {
                                if (_selectedNoteBorders.Contains(b))
                                {
                                    _selectedNoteBorders.Remove(b);
                                    UpdateNoteVisuals(b, false);
                                }
                            }
                        }
                    }
                    else
                    {
                        // --- NORMAL MODUS (Alles wird im Kasten markiert, der Rest gelöscht) ---
                        if (hit)
                        {
                            if (!_selectedNoteBorders.Contains(b))
                            {
                                _selectedNoteBorders.Add(b);
                                UpdateNoteVisuals(b, true);
                            }
                        }
                        else
                        {
                            if (_selectedNoteBorders.Contains(b))
                            {
                                _selectedNoteBorders.Remove(b);
                                UpdateNoteVisuals(b, false);
                            }
                        }
                    }
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

        private Border AddNoteUI(int pitch, long start, long dur)
        {
            var d = new NoteData { Pitch = pitch, Start = start, Duration = dur };
            var b = new Border { Tag = d, CornerRadius = new CornerRadius(1), IsHitTestVisible = true };
            UpdateNoteVisuals(b, false);
            UpdateNotePosition(b);
            NotesCanvas.Children.Add(b);
            return b;
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

        private void UpdateNotePosition(Border n)
        {
            var d = (NoteData)n.Tag;
            Canvas.SetLeft(n, d.Start * _tickPixelScale);
            Canvas.SetTop(n, (127 - d.Pitch) * _noteHeight);
            n.Width = Math.Max(2, d.Duration * _tickPixelScale); n.Height = _noteHeight - 1;
        }
    }
}
