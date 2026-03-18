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

namespace BardMusicPlayer.Ui.Windows
{
    public partial class MidiBardConverterTrackWindow
    {

        private void NotesCanvas_SelectTool_MouseMove(object sender, MouseEventArgs e, Point cur)
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

        private void NotesCanvas_SelectTool_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            {
                // Strg gedrückt: Aktuellen Stand für Invertierung merken
                _selectedBordersAtStartOfSelection = _selectedNoteBorders.ToList();
            }
            else
            {
                // KEIN Strg: Alles plätten für neue Auswahl
                ClearSelection();
                _selectedBordersAtStartOfSelection.Clear();
            }

            SelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRect, _lastMousePos.X); Canvas.SetTop(SelectionRect, _lastMousePos.Y);
            SelectionRect.Width = 0; SelectionRect.Height = 0;
            NotesCanvas.CaptureMouse();
        }
    }
}
