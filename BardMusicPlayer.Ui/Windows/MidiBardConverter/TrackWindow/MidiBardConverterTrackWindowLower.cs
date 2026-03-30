/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian.Structs;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    /// <summary>
    /// Lower / Automation area.
    /// Keyboard shortcuts (when AutomationCanvas is active):
    ///   Ctrl+A    = Select all automation points
    ///   Delete    = Delete selected automation points
    ///   Ctrl+C    = Copy selected points
    ///   Ctrl+V    = Paste points at mouse position
    ///   Ctrl+Z/Y  = Undo/Redo (via global handler in _xaml.cs)
    /// </summary>
    public partial class MidiBardConverterTrackWindow
    {
        private List<AutomationPoint> _programChanges = new List<AutomationPoint>();
        private HashSet<AutomationPoint> _selectedAutoPoints = new HashSet<AutomationPoint>();
        private List<AutomationPoint> _autoClipboard = new List<AutomationPoint>();

        // Drag'n'Drop
        private bool _isDraggingAutomation;
        private bool _automationDragStarted;
        private Point _automationMouseDownPos;
        private FrameworkElement _activeAutomationPoint;
        private AutomationPoint _automationStartSnapshot;

        // Rubber-Band Selection
        private bool _isSelectingAutomation;
        private Point _autoSelectionStart;
        private HashSet<AutomationPoint> _autoSelectedAtStart = new HashSet<AutomationPoint>();

        // Range Mode
        private bool _automationRangeMode = false;
        private bool _isSelectingRange = false;
        private Point _rangeSelectionStart;
        private long _rangeStartTick = -1;
        private long _rangeEndTick = -1;

        private void DrawAutomation()
        {
            // Clear the children keep the AutomationSelectionRect
            AutomationCanvas.Children.Clear();
            AutomationCanvas.Children.Add(AutomationSelectionRect);
            if (!_programChanges.Any()) return;

            Polyline line = new Polyline
            {
                Stroke = Brushes.Orange,
                StrokeThickness = 2,
                IsHitTestVisible = false
            };
            double h = AutomationCanvas.ActualHeight > 0 ? AutomationCanvas.ActualHeight : 80;

            foreach (var pt in _programChanges.OrderBy(p => p.Tick))
            {
                double x = Snap(pt.Tick) * _tickPixelScale;
                double y = h - (pt.Value / 127.0 * h);

                if (line.Points.Any())
                    line.Points.Add(new Point(x, line.Points.Last().Y));
                line.Points.Add(new Point(x, y));

                bool isSelected = _selectedAutoPoints.Contains(pt);
                Ellipse point = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = isSelected ? Brushes.Green : Brushes.Orange,
                    Stroke = isSelected ? Brushes.Orange : null,
                    StrokeThickness = isSelected ? 1.5 : 0,
                    Tag = pt,
                    Cursor = Cursors.Hand,
                    Opacity = 0.8
                };

                point.MouseEnter += (s, e) =>
                {
                    ((Ellipse)s).Opacity = 1.0;
                    ((Ellipse)s).Width = 8; ((Ellipse)s).Height = 8;
                    Canvas.SetLeft(((Ellipse)s), x - 4); Canvas.SetTop(((Ellipse)s), y - 4);
                    ValuePopupText.Text = (pt.Value + 1).ToString();
                    ValuePopup.IsOpen = true;
                };
                point.MouseLeave += (s, e) =>
                {
                    ((Ellipse)s).Opacity = 0.8;
                    ((Ellipse)s).Width = 6; ((Ellipse)s).Height = 6;
                    Canvas.SetLeft(((Ellipse)s), x - 3); Canvas.SetTop(((Ellipse)s), y - 3);
                    ValuePopup.IsOpen = false;
                };
                point.MouseLeftButtonDown += AutomationPoint_MouseDown;

                Canvas.SetLeft(point, x - 3);
                Canvas.SetTop(point, y - 3);
                AutomationCanvas.Children.Add(point);
            }

            if (line.Points.Any())
            {
                double fw = AutomationCanvas.Width > 0 ? AutomationCanvas.Width : AutomationCanvas.ActualWidth;
                line.Points.Add(new Point(fw, line.Points.Last().Y));
            }
            AutomationCanvas.Children.Insert(0, line);
        }

        #region MouseStuff
        private void AutomationCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AutomationCanvas.Focus();

            // ── Range-Mode ────────────────────────────────────────────
            if (_automationRangeMode)
            {
                _rangeSelectionStart = e.GetPosition(AutomationCanvas);
                _isSelectingRange = true;
                _rangeStartTick = Snap(XToTicks(_rangeSelectionStart.X));
                _rangeEndTick = _rangeStartTick;

                Canvas.SetLeft(AutomationRangeRect, _rangeSelectionStart.X);
                Canvas.SetTop(AutomationRangeRect, 0);
                AutomationRangeRect.Width = 0;
                AutomationRangeRect.Height = AutomationCanvas.ActualHeight;
                AutomationRangeRect.Visibility = Visibility.Visible;
                AutomationCanvas.CaptureMouse();
                e.Handled = true;
                return;
            }

            // click on the ellipse is handled via AutomationPoint_MouseDown + MouseUp
            if (e.OriginalSource is Ellipse) return;

            // Double-click on empty area adds a new point
            if (e.ClickCount == 2)
            {
                Point pos = e.GetPosition(AutomationCanvas);
                long ticks = Snap(XToTicks(pos.X));
                int val = (int)((1.0 - (pos.Y / AutomationCanvas.ActualHeight)) * 127);
                val = Math.Max(0, Math.Min(val, 127));

                var newPt = new AutomationPoint { Tick = ticks, Value = val };
                ExecuteAndRegisterUndo("Add Automation",
                    undoAction: () => { _programChanges.Remove(newPt); _selectedAutoPoints.Remove(newPt); DrawAutomation(); },
                    redoAction: () => { _programChanges.Add(newPt); DrawAutomation(); });
                return;
            }

            // Single-click on an empty area - Start rubber band
            _autoSelectionStart = e.GetPosition(AutomationCanvas);
            _isSelectingAutomation = true;

            _autoSelectedAtStart = new HashSet<AutomationPoint>(
                Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)
                    ? _selectedAutoPoints
                    : Enumerable.Empty<AutomationPoint>());

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                _selectedAutoPoints.Clear();

            Canvas.SetLeft(AutomationSelectionRect, _autoSelectionStart.X);
            Canvas.SetTop(AutomationSelectionRect, _autoSelectionStart.Y);
            AutomationSelectionRect.Width = 0;
            AutomationSelectionRect.Height = 0;
            AutomationSelectionRect.Visibility = Visibility.Visible;
            AutomationCanvas.CaptureMouse();
        }

        private void AutomationCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point cur = e.GetPosition(AutomationCanvas);

            // ── Range-Mode ────────────────────────────────────────────
            if (_isSelectingRange)
            {
                double x = Math.Min(_rangeSelectionStart.X, cur.X);
                double w = Math.Abs(_rangeSelectionStart.X - cur.X);
                Canvas.SetLeft(AutomationRangeRect, x);
                AutomationRangeRect.Width = w;
                _rangeStartTick = Snap(XToTicks(Math.Min(_rangeSelectionStart.X, cur.X)));
                _rangeEndTick = Snap(XToTicks(Math.Max(_rangeSelectionStart.X, cur.X)));

                // Live display: Start and end times as beat + duration
                UpdateRangeLabel();
                return;
            }

            // ── Rubber-Band ────────────────────────────────────────────
            if (_isSelectingAutomation)
            {
                double x = Math.Min(_autoSelectionStart.X, cur.X);
                double y = Math.Min(_autoSelectionStart.Y, cur.Y);
                double w = Math.Abs(_autoSelectionStart.X - cur.X);
                double h = Math.Abs(_autoSelectionStart.Y - cur.Y);

                Canvas.SetLeft(AutomationSelectionRect, x);
                Canvas.SetTop(AutomationSelectionRect, y);
                AutomationSelectionRect.Width = w;
                AutomationSelectionRect.Height = h;

                Rect selRect = new Rect(x, y, w, h);
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                double canvasH = AutomationCanvas.ActualHeight;

                foreach (var pt in _programChanges)
                {
                    double px = Snap(pt.Tick) * _tickPixelScale;
                    double py = canvasH - (pt.Value / 127.0 * canvasH);
                    bool hit = selRect.Contains(new Point(px, py));

                    if (isCtrl)
                    {
                        bool was = _autoSelectedAtStart.Contains(pt);
                        if (hit) { if (was) _selectedAutoPoints.Remove(pt); else _selectedAutoPoints.Add(pt); }
                        else { if (was) _selectedAutoPoints.Add(pt); else _selectedAutoPoints.Remove(pt); }
                    }
                    else
                    {
                        if (hit) _selectedAutoPoints.Add(pt); else _selectedAutoPoints.Remove(pt);
                    }
                }
                DrawAutomation();
                return;
            }

            // ── Point-Drag ─────────────────────────────────────────────
            if (_activeAutomationPoint == null) return;

            // Do not start dragging until the mouse has moved at least 3 pixels (threshold)
            if (!_isDraggingAutomation)
            {
                Vector moved = cur - _automationMouseDownPos;
                if (Math.Abs(moved.X) < 3 && Math.Abs(moved.Y) < 3) return;
                _isDraggingAutomation = true;
                _automationDragStarted = true;
            }

            var data = (AutomationPoint)_activeAutomationPoint.Tag;
            data.Tick = Math.Max(0, Snap(XToTicks(cur.X)));
            double height = AutomationCanvas.ActualHeight;
            if (height > 0)
            {
                double normalized = 1.0 - (cur.Y / height);
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
                // snapshot and record the mouse position, but do NOT start dragging yet
                _activeAutomationPoint = point;
                _isDraggingAutomation = false;   // set at MouseMove
                _automationDragStarted = false;
                _automationStartSnapshot = new AutomationPoint { Tick = data.Tick, Value = data.Value };
                _automationMouseDownPos = e.GetPosition(AutomationCanvas);
                point.CaptureMouse();
                e.Handled = true;
            }
        }

        private void AutomationCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // ── End Range-Mode ─────────────────────────────────────────
            if (_isSelectingRange)
            {
                _isSelectingRange = false;
                if (AutomationCanvas.IsMouseCaptured) AutomationCanvas.ReleaseMouseCapture();
                AutomationRangeLabel.Visibility = Visibility.Collapsed;

                if (_rangeEndTick > _rangeStartTick)
                {
                    AutomationCtx_SetProgramItem.IsEnabled = true;
                    OpenProgramInputPopup();
                }
                else
                {
                    AutomationRangeRect.Visibility = Visibility.Collapsed;
                }
                return;
            }

            // ── End Rubber-Band ────────────────────────────────────────
            if (_isSelectingAutomation)
            {
                _isSelectingAutomation = false;
                AutomationSelectionRect.Visibility = Visibility.Collapsed;
                if (AutomationCanvas.IsMouseCaptured) AutomationCanvas.ReleaseMouseCapture();
                DrawAutomation();
                return;
            }

            // ── End Point-Drag ─────────────────────────────────────────
            if (_activeAutomationPoint != null)
            {
                var element = _activeAutomationPoint;
                var data = (AutomationPoint)element.Tag;

                if (_automationDragStarted)
                {
                    if (_automationStartSnapshot != null &&
                        (data.Value != _automationStartSnapshot.Value || data.Tick != _automationStartSnapshot.Tick))
                    {
                        int oldVal = _automationStartSnapshot.Value;
                        long oldTick = _automationStartSnapshot.Tick;
                        int newVal = data.Value;
                        long newTick = data.Tick;
                        ExecuteAndRegisterUndo("Move Automation",
                            undoAction: () => { data.Value = oldVal; data.Tick = oldTick; DrawAutomation(); },
                            redoAction: () => { data.Value = newVal; data.Tick = newTick; DrawAutomation(); });
                    }
                }
                else
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                        _selectedAutoPoints.Clear();
                    if (_selectedAutoPoints.Contains(data)) _selectedAutoPoints.Remove(data);
                    else _selectedAutoPoints.Add(data);
                }

                if (element.IsMouseCaptured) element.ReleaseMouseCapture();
            }

            ValuePopup.IsOpen = false;
            _isDraggingAutomation = false;
            _automationDragStarted = false;
            _activeAutomationPoint = null;
            _automationStartSnapshot = null;
            DrawAutomation();
        }
        #endregion

        private void UpdateAutomationPointPosition(FrameworkElement element)
        {
            if (element == null || !(element.Tag is AutomationPoint data)) return;
            double x = TicksToX(data.Tick);
            double y = AutomationCanvas.ActualHeight * (1.0 - (data.Value / 127.0));
            Canvas.SetLeft(element, x - (element.ActualWidth / 2));
            Canvas.SetTop(element, y - (element.ActualHeight / 2));
        }

        private void AutomationCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                AutomationGridCanvas.Width = e.NewSize.Width;
                AutomationGridCanvas.Height = e.NewSize.Height;
                DrawAutomationGrid();
                DrawAutomation();
            }
        }

        /// <summary>
        /// Keyboard shortcuts (when AutomationCanvas is active):
        ///   Ctrl+A    = Select all automation points
        ///   Delete    = Delete selected automation points
        ///   Ctrl+C    = Copy selected points
        ///   Ctrl+V    = Paste points at mouse position
        ///   Ctrl+Z/Y  = Undo/Redo (via global handler in _xaml.cs)
        /// </summary>
        private void AutomationCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+A – Select all
            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                _selectedAutoPoints.Clear();
                foreach (var pt in _programChanges) _selectedAutoPoints.Add(pt);
                DrawAutomation();
                e.Handled = true;
                return;
            }

            // Delete – delete selected
            if (e.Key == Key.Delete && _selectedAutoPoints.Any())
            {
                var toDelete = _selectedAutoPoints.ToList();
                ExecuteAndRegisterUndo("Delete Automation",
                    undoAction: () =>
                    {
                        foreach (var pt in toDelete) if (!_programChanges.Contains(pt)) _programChanges.Add(pt);
                        DrawAutomation();
                    },
                    redoAction: () =>
                    {
                        foreach (var pt in toDelete) { _programChanges.Remove(pt); _selectedAutoPoints.Remove(pt); }
                        DrawAutomation();
                    });
                e.Handled = true;
                return;
            }

            // Ctrl+C – copy
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_selectedAutoPoints.Any())
                {
                    _autoClipboard = _selectedAutoPoints
                        .Select(pt => new AutomationPoint { Tick = pt.Tick, Value = pt.Value })
                        .ToList();
                    e.Handled = true;
                }
                return;
            }

            // Ctrl+V – paste
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (_autoClipboard.Any())
                {
                    long minTick = _autoClipboard.Min(pt => pt.Tick);
                    long targetTick = Snap(XToTicks(_lastMousePos.X));
                    var pasted = _autoClipboard
                        .Select(old => new AutomationPoint
                        { Tick = old.Tick - minTick + targetTick, Value = old.Value })
                        .ToList();

                    ExecuteAndRegisterUndo("Paste Automation",
                        undoAction: () =>
                        {
                            foreach (var pt in pasted) { _programChanges.Remove(pt); _selectedAutoPoints.Remove(pt); }
                            DrawAutomation();
                        },
                        redoAction: () =>
                        {
                            _selectedAutoPoints.Clear();
                            foreach (var pt in pasted) { _programChanges.Add(pt); _selectedAutoPoints.Add(pt); }
                            DrawAutomation();
                        });
                    e.Handled = true;
                }
            }
        }

        private void UpdateRangeLabel()
        {
            var tempoMap = _midiFile.GetTempoMap();
            var ts = tempoMap.GetTimeSignatureAtTime((Melanchall.DryWetMidi.Interaction.MidiTimeSpan)0);
            long tpb = _ticksPerQuarterNote * ts.Numerator * 4 / ts.Denominator;

            long lenTicks = _rangeEndTick - _rangeStartTick;
            long lenBars = lenTicks / tpb;
            long lenBeats = (lenTicks % tpb) / _ticksPerQuarterNote;

            string lenStr = lenBars > 0 ? $"{lenBars}T {lenBeats}b" : $"{lenBeats}b";
            AutomationRangeLabelText.Text = $"{TickToBarBeat(_rangeStartTick, tpb)} → {TickToBarBeat(_rangeEndTick, tpb)}  ({lenStr})";

            // Position relative to the AutomationLabelCanvas (overlaps the canvas; no clipping)
            Point mouse = Mouse.GetPosition(AutomationLabelCanvas);
            double labelX = mouse.X + 12;
            double labelY = mouse.Y - 24;
            if (labelY < 2) labelY = mouse.Y + 8;

            Canvas.SetLeft(AutomationRangeLabel, labelX);
            Canvas.SetTop(AutomationRangeLabel, labelY);
            AutomationRangeLabel.Visibility = Visibility.Visible;
        }

        private string TickToBarBeat(long ticks, long tpb)
        {
            long bar = ticks / tpb + 1;
            long beat = (ticks % tpb) / _ticksPerQuarterNote + 1;
            return $"{bar}:{beat}";
        }

        private void AutomationCanvas_MouseEnter(object sender, MouseEventArgs e)
            => _automationAreaActive = true;

        private void AutomationCanvas_MouseLeave(object sender, MouseEventArgs e)
            => _automationAreaActive = false;

        private void AutomationCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            AutomationCtx_SetProgramItem.IsEnabled = _rangeEndTick > _rangeStartTick;
        }

        #region ContextMenu
        private void AutomationCtx_SelectionMode(object sender, System.Windows.RoutedEventArgs e)
        {
            _automationRangeMode = false;
            AutomationRangeRect.Visibility = Visibility.Collapsed;
            _rangeStartTick = _rangeEndTick = -1;
        }

        private void AutomationCtx_RangeMode(object sender, System.Windows.RoutedEventArgs e)
        {
            _automationRangeMode = true;
        }

        private void AutomationCtx_SetProgramForRange(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_rangeEndTick <= _rangeStartTick) return;
            OpenProgramInputPopup();
        }
        #endregion

        #region Program-Input Popup
        /// <summary>Fills the Instrument combo box and opens the popup.</summary>
        private void OpenProgramInputPopup()
        {
            // ComboBox mit Instrument-Namen befüllen (einmalig)
            if (ProgramInstrumentBox.Items.Count == 0)
            {
                foreach (var instr in Instrument.All)
                    ProgramInstrumentBox.Items.Add(new ComboBoxItem
                    {
                        Content = instr.Name,
                        Tag = instr.MidiProgramChangeCode
                    });
            }

            // Preselection: current program at the beginning of the range (or the first one before it)
            var current = _programChanges
                .Where(p => p.Tick <= _rangeStartTick)
                .OrderByDescending(p => p.Tick)
                .FirstOrDefault();
            int currentVal = current?.Value ?? 0;

            for (int i = 0; i < ProgramInstrumentBox.Items.Count; i++)
            {
                if ((int)((ComboBoxItem)ProgramInstrumentBox.Items[i]).Tag == currentVal)
                {
                    ProgramInstrumentBox.SelectedIndex = i;
                    break;
                }
            }

            ProgramInputPopup.IsOpen = true;
            ProgramInstrumentBox.Focus();
        }

        private void ProgramInstrumentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProgramInstrumentBox.SelectedItem is ComboBoxItem item)
                AutomationValueLabel.Text = item.Content.ToString();
        }

        private void ProgramInputBox_OK(object sender, RoutedEventArgs e)
            => ApplyProgramForRange();

        private void ProgramInputBox_Cancel(object sender, RoutedEventArgs e)
        {
            ProgramInputPopup.IsOpen = false;
            AutomationRangeRect.Visibility = Visibility.Collapsed;
            _rangeStartTick = _rangeEndTick = -1;
            AutomationCtx_SetProgramItem.IsEnabled = false;
        }
        #endregion
        private void ApplyProgramForRange()
        {
            ProgramInputPopup.IsOpen = false;

            if (!(ProgramInstrumentBox.SelectedItem is ComboBoxItem selected)) return;
            int program = (int)selected.Tag; // MidiProgramChangeCode (0-127)

            bool restore = ProgramRestoreCheck.IsChecked == true;

            // Get previous program
            var prevPc = _programChanges
                .Where(p => p.Tick < _rangeStartTick)
                .OrderByDescending(p => p.Tick)
                .FirstOrDefault();

            // Save all programs
            var toRemove = _programChanges
                .Where(p => p.Tick >= _rangeStartTick && p.Tick <= _rangeEndTick)
                .ToList();

            var newPts = new List<AutomationPoint>
            {
                new AutomationPoint { Tick = _rangeStartTick, Value = program }
            };

            // Switch back to prog we had before
            if (restore && prevPc != null)
                newPts.Add(new AutomationPoint { Tick = _rangeEndTick, Value = prevPc.Value });

            ExecuteAndRegisterUndo("Set Program for Range",
                undoAction: () =>
                {
                    foreach (var pt in newPts) _programChanges.Remove(pt);
                    foreach (var pt in toRemove) if (!_programChanges.Contains(pt)) _programChanges.Add(pt);
                    _programChanges = _programChanges.OrderBy(p => p.Tick).ToList();
                    DrawAutomation();
                },
                redoAction: () =>
                {
                    foreach (var pt in toRemove) { _programChanges.Remove(pt); _selectedAutoPoints.Remove(pt); }
                    foreach (var pt in newPts) if (!_programChanges.Contains(pt)) _programChanges.Add(pt);
                    _programChanges = _programChanges.OrderBy(p => p.Tick).ToList();
                    DrawAutomation();
                });

            _rangeStartTick = _rangeEndTick = -1;
            AutomationRangeRect.Visibility = Visibility.Collapsed;
            AutomationCtx_SetProgramItem.IsEnabled = false;
        }

        private double TicksToX(long ticks) => ticks * _tickPixelScale;
        private long XToTicks(double x) => _tickPixelScale <= 0 ? 0 : (long)(x / _tickPixelScale);
    }
}