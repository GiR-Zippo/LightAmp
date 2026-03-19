/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

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
    /// The Lower / Automation part of the window
    /// </summary>
    public partial class MidiBardConverterTrackWindow
    {
        private List<AutomationPoint> _programChanges = new List<AutomationPoint>();
        private bool _isDraggingAutomation;
        private FrameworkElement _activeAutomationPoint;
        private AutomationPoint _automationStartSnapshot;

        private void DrawAutomation()
        {
            AutomationCanvas.Children.Clear();

            if (!_programChanges.Any())
                return;

            Polyline line = new Polyline { Stroke = Brushes.Orange, StrokeThickness = 2, IsHitTestVisible = false };
            double h = AutomationCanvas.ActualHeight > 0 ? AutomationCanvas.ActualHeight : 80;

            var sorted = _programChanges.OrderBy(p => p.Tick).ToList();

            foreach (var pt in sorted)
            {
                // Identisch zu UpdateNotePosition: Start-Tick * Scale
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
                    Opacity = 0.8
                };

                // --- SCHICKES HOVER-LABEL (POPUP) ---
                point.MouseEnter += (s, e) =>
                {
                    ((Ellipse)s).Opacity = 1.0;
                    ((Ellipse)s).Width = 8;
                    ((Ellipse)s).Height = 8;

                    // Zentrierung: Wir ziehen die halbe Breite (4) ab, damit die MITTE auf x liegt
                    Canvas.SetLeft(((Ellipse)s), (pt.Tick * _tickPixelScale) - 4);
                    Canvas.SetTop(((Ellipse)s), (h - (pt.Value / 127.0 * h)) - 4);

                    ValuePopupText.Text = (pt.Value + 1).ToString();
                    ValuePopup.IsOpen = true;
                };

                point.MouseLeave += (s, e) =>
                {
                    ((Ellipse)s).Opacity = 0.8;
                    ((Ellipse)s).Width = 6;
                    ((Ellipse)s).Height = 6;

                    // Zentrierung: halbe Breite (3) abziehen
                    Canvas.SetLeft(((Ellipse)s), (pt.Tick * _tickPixelScale) - 3);
                    Canvas.SetTop(((Ellipse)s), (h - (pt.Value / 127.0 * h)) - 3);

                    ValuePopup.IsOpen = false;
                };
                // ------------------------------------

                point.MouseLeftButtonDown += AutomationPoint_MouseDown;

                // Hier ziehen wir 3 ab, damit die 6px Ellipse genau ZENTRISCH auf dem Tick sitzt
                Canvas.SetLeft(point, x - 3);
                Canvas.SetTop(point, y - 3);
                AutomationCanvas.Children.Add(point);
            }

            if (line.Points.Any())
            {
                // Wir ziehen die Linie bis zum Ende des Canvas
                // Stelle sicher, dass AutomationCanvas.Width = NotesCanvas.Width gesetzt wurde!
                double finalWidth = AutomationCanvas.Width > 0 ? AutomationCanvas.Width : AutomationCanvas.ActualWidth;
                line.Points.Add(new Point(finalWidth, line.Points.Last().Y));
            }

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
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                AutomationGridCanvas.Width = e.NewSize.Width;
                AutomationGridCanvas.Height = e.NewSize.Height;
                DrawAutomationGrid();
                DrawAutomation();
            }
        }

        private double TicksToX(long ticks)
        {
            return ticks * _tickPixelScale;
        }

        private long XToTicks(double x)
        {
            if (_tickPixelScale <= 0) return 0;
            return (long)(x / _tickPixelScale);
        }
    }
}
