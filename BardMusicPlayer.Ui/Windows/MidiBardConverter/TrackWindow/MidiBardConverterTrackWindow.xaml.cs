/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Quotidian.Structs;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    public class AutomationPoint { public long Tick { get; set; } public int Value { get; set; } }

    public partial class MidiBardConverterTrackWindow : Window
    {
        // ── Undo / Redo ────────────────────────────────────────────────
        MidiBardConverterUndo _undoHandler { get; set; }

        // ── MIDI data ──────────────────────────────────────────────────
        private TrackChunk _activeTrack { get; set; }
        private MidiFile _midiFile { get; set; }

        // ── Render-Debounce ────────────────────────────────────────────
        private DispatcherTimer _renderDebounceTimer { get; set; }

        // ── Zoom-Debounce ──────────────────────────────────────────────
        private DispatcherTimer _zoomDebounceTimer { get; set; }
        private double _zoomAnchorScale { get; set; }
        private ScaleTransform _noteHostScale { get; set; } = new ScaleTransform();

        // ── Scroll-Transform ──────────────────────────────────────────
        private TranslateTransform _noteHostTranslate { get; set; } = new TranslateTransform();
        private double _lastRenderedHOffset { get; set; } = 0;

        // ── Editor parameters ──────────────────────────────────────────
        private double _noteHeight { get; set; } = 20;
        private double _tickPixelScale { get; set; } = 0.1;
        private long _gridSnapTicks { get; set; } = 120;
        private short _ticksPerQuarterNote { get; set; } = 480;
        private long _maxTick { get; set; } = 4800;

        // ── Note data ─────────────────────────────────────────────────
        private List<Note> _notes { get; set; } = new List<Note>();
        private HashSet<Note> _selectedNotes { get; set; } = new HashSet<Note>();
        private NoteVisualHost _noteHost { get; set; }

        // ── Interaction state ──────────────────────────────────────────
        private Point _lastMousePos { get; set; }

        // ── Clipboard ─────────────────────────────────────────────────
        private List<Note> _clipboard { get; set; } = new List<Note>();

        // ── Automation active ─────────────────────────────────────────
        private bool _automationAreaActive { get; set; } = false;

        // ── Playback ──────────────────────────────────────────────────
        private MidiTrackPlayer _player { get; set; }
        private DispatcherTimer _playbackUITimer { get; set; }
        private bool _useSiren { get; set; } = false;

        // ── Constructor ────────────────────────────────────────────────
        public MidiBardConverterTrackWindow(MidiFile midiFile, TrackChunk track)
        {
            InitializeComponent();
            _undoHandler = new MidiBardConverterUndo();
            _midiFile = midiFile;
            _activeTrack = track;

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(_noteHostScale);
            transformGroup.Children.Add(_noteHostTranslate);

            _noteHost = new NoteVisualHost
            {
                Focusable = true,
                RenderTransform = transformGroup
            };
            NotesCanvas.Children.Insert(0, _noteHost);

            // Scroll-Debounce
            _renderDebounceTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(80)
            };
            _renderDebounceTimer.Tick += (s, e) => { _renderDebounceTimer.Stop(); RenderNotes(); };

            // Zoom-Debounce
            _zoomDebounceTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(150)
            };
            _zoomDebounceTimer.Tick += (s, e) =>
            {
                _zoomDebounceTimer.Stop();
                _noteHostScale.ScaleX = 1.0;
                _noteHostScale.ScaleY = 1.0;
                RefreshLayout();
                RenderNotes();
            };

            // Playback-UI-Timer (100ms)
            _playbackUITimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _playbackUITimer.Tick += (s, e) => UpdatePlaybackUI();

            if (_midiFile.TimeDivision is TicksPerQuarterNoteTimeDivision tpq)
                _ticksPerQuarterNote = tpq.TicksPerQuarterNote;

            NotesCanvas.MouseLeftButtonDown += NotesCanvas_MouseLeftButtonDown;
            NotesCanvas.MouseMove += NotesCanvas_MouseMove;
            NotesCanvas.MouseLeftButtonUp += NotesCanvas_MouseLeftButtonUp;

            this.PreviewKeyDown += Window_PreviewKeyDown_Global;
            this.Closing += (s, e) => CleanupPlayer();

            this.Loaded += (s, e) =>
            {
                UpdateSnapTicks();
                LoadFromTrack();
                BuildPianoKeys();
                RefreshLayout();
                RenderNotes();
                FocusHighestNote();
                UpdateToolUI();
            };

            MainScroll.ScrollChanged += (s, e) =>
            {
                Canvas.SetTop(PianoKeysStack, -e.VerticalOffset);
                Canvas.SetLeft(RulerCanvas, -e.HorizontalOffset);
                if (e.HorizontalChange != 0)
                {
                    ControllerScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
                    _noteHostTranslate.X = _lastRenderedHOffset - e.HorizontalOffset;
                }
                _renderDebounceTimer.Stop();
                _renderDebounceTimer.Start();
            };
        }

        // ── Load TrackChunk ───────────────────────────────────────
        private void LoadFromTrack()
        {
            this.Title = _activeTrack.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault()?.Text ?? "Track Editor";
            _notes = _activeTrack.GetNotes().ToList();
            if (_notes.Any())
                _maxTick = Math.Max(4800, _notes.Max(n => n.Time + n.Length) + 1000);

            _programChanges = _activeTrack.GetTimedEvents()
                .Where(te => te.Event.EventType == MidiEventType.ProgramChange)
                .Select(te => new AutomationPoint
                {
                    Tick = te.Time,
                    Value = ((ProgramChangeEvent)te.Event).ProgramNumber
                })
                .OrderBy(p => p.Tick)
                .ToList();
        }

        // ── UI Callbacks ───────────────────────────────────────────────
        private void SetTool_Click(object sender, RoutedEventArgs e)
            => SwitchTool((EditorTool)Enum.Parse(typeof(EditorTool), (string)((MenuItem)sender).Tag));

        #region Scrool / Zoom

        private void MainScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                ZoomSlider.Value += e.Delta > 0 ? 0.01 : -0.01;
                e.Handled = true;
            }
        }

        #endregion

        private void UpdateSnapTicks()
        {
            if (SnapComboBox?.SelectedItem is ComboBoxItem item)
                _gridSnapTicks = (long)(_ticksPerQuarterNote
                    * double.Parse(item.Tag.ToString(), CultureInfo.InvariantCulture));
        }

        private void SnapComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => UpdateSnapTicks();

        private long Snap(long ticks)
            => (long)(Math.Round((double)ticks / _gridSnapTicks) * _gridSnapTicks);

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            if (!_zoomDebounceTimer.IsEnabled)
                _zoomAnchorScale = _tickPixelScale;
            _tickPixelScale = e.NewValue;
            if (_zoomAnchorScale > 0)
                _noteHostScale.ScaleX = _tickPixelScale / _zoomAnchorScale;
            _zoomDebounceTimer.Stop();
            _zoomDebounceTimer.Start();
        }

        // ── Undo / Redo ────────────────────────────────────────────────
        private void ExecuteAndRegisterUndo(string name, Action undoAction, Action redoAction)
        {
            _undoHandler.ExecuteAndRegisterUndo(name, undoAction, redoAction);
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            _undoHandler.Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            _undoHandler.Redo();
        }

        // ── Global Keyboard ────────────────────────────────────────────
        private void Window_PreviewKeyDown_Global(object sender, KeyEventArgs e)
        {
            // Space = Play/Pause toggle
            if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None)
            {
                TogglePlayPause();
                e.Handled = true;
                return;
            }

            // Undo / Redo – global
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            { Undo_Click(null, null); e.Handled = true; return; }

            if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            { Redo_Click(null, null); e.Handled = true; return; }

            // Automation-Bereich aktiv -> weiterleiten
            if (_automationAreaActive)
            {
                AutomationCanvas_KeyDown(sender, e);
                return;
            }

            // Pfeil-Tasten: Noten verschieben
            bool isArrow = e.Key == Key.Up || e.Key == Key.Down
                        || e.Key == Key.Left || e.Key == Key.Right;
            if (isArrow && (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)
                         || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)))
            {
                if (_selectedNotes.Any()) { e.Handled = true; MoveSelectedNotes(e.Key); }
            }
        }

        #region Playback
        private void PlayButton_Click(object sender, RoutedEventArgs e) => StartPlayback();
        private void PauseButton_Click(object sender, RoutedEventArgs e) => TogglePlayPause();
        private void StopButton_Click(object sender, RoutedEventArgs e) => StopPlayback();

        private void SirenToggle_Checked(object sender, RoutedEventArgs e)
        {
            _useSiren = true;
            SirenToggle.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            _player = new MidiTrackPlayer(BuildPreviewTrack(), _midiFile, _useSiren);
        }

        private void SirenToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _useSiren = false;
            SirenToggle.Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170));
            _player = new MidiTrackPlayer(BuildPreviewTrack(), _midiFile, _useSiren);
        }

        private void StartPlayback()
        {
            // if paused -> cont
            if (_player != null && _player.IsPaused)
            {
                _player.Play();
                _playbackUITimer.Start();
                UpdatePlaybackButtonStates();
                return;
            }

            CleanupPlayer();

            try
            {
                _player = new MidiTrackPlayer(BuildPreviewTrack(), _midiFile, _useSiren);
                _player.Finished += (s, e) => Dispatcher.Invoke(() =>
                {
                    _playbackUITimer.Stop();
                    PlaybackTimeText.Text = "0:00.000";
                    UpdatePlaybackButtonStates();
                });
                _player.Play();
                _playbackUITimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Playback-Fehler: {ex.Message}", "Fehler",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CleanupPlayer();
            }

            UpdatePlaybackButtonStates();
        }

        private void TogglePlayPause()
        {
            if (_player == null)
            {
                StartPlayback();
                return;
            }
            if (_player.IsPlaying)
            {
                _player.Pause();
                _playbackUITimer.Stop();
            }
            else if (_player.IsPaused)
            {
                //rebuild track and set seek to curr pos again
                var time = _player.CurrentTime;
                _player = new MidiTrackPlayer(BuildPreviewTrack(), _midiFile, _useSiren);
                _player?.SeekTo(time);
                _player.Play();
                _playbackUITimer.Start();
            }
            else
            {
                // Stopped ? Restart
                StartPlayback();
                return;
            }
            UpdatePlaybackButtonStates();
        }

        private void StopPlayback()
        {
            _player?.Stop();
            _playbackUITimer.Stop();
            PlaybackTimeText.Text = "0:00.000";
            HidePlayhead();
            UpdatePlaybackButtonStates();
        }

        private void CleanupPlayer()
        {
            _playbackUITimer?.Stop();
            _player?.Dispose();
            _player = null;
        }

        private TrackChunk BuildPreviewTrack()
        {
            byte noteChannel = _notes.Any() ? (byte)_notes.First().Channel : (byte)0;
            var rawEvents = new List<(long Tick, int SortKey, MidiEvent Event)>();

            // SequenceTrackNameEvent bei Tick 0
            var trackName = _activeTrack.Events.OfType<SequenceTrackNameEvent>().FirstOrDefault();
            if (trackName != null)
                rawEvents.Add((0, -1, new SequenceTrackNameEvent(trackName.Text)));

            //Set right trackname if instrument differs from program
            if (_programChanges?.Any() == true)
            {
                var pt = _programChanges.First();
                if (pt.Tick == 0)
                {
                    Instrument instrumentName;
                    if (Instrument.TryParseByProgramChange(pt.Value, out instrumentName))
                        if (!trackName.Equals(instrumentName.Name))
                        {
                            rawEvents.Clear();
                            rawEvents.Add((0, -1, new SequenceTrackNameEvent(instrumentName.Name)));
                        }
                }
            }

            // ProgramChanges
            if (_programChanges?.Any() == true)
            {
                foreach (var pt in _programChanges)
                {
                    var pc = new ProgramChangeEvent((SevenBitNumber)pt.Value) { Channel = (FourBitNumber)noteChannel };
                    rawEvents.Add((pt.Tick, 0, pc));
                }
            }

            // Notes
            foreach (var note in _notes)
            {
                rawEvents.Add((note.Time,
                    1, new NoteOnEvent(note.NoteNumber, note.Velocity) { Channel = note.Channel }));
                rawEvents.Add((note.Time + note.Length,
                    2, new NoteOffEvent(note.NoteNumber, note.OffVelocity) { Channel = note.Channel }));
            }

            // Sort and Delta-Times
            var chunk = new TrackChunk();
            long lastTick = 0;
            foreach (var (tick, _, ev) in rawEvents.OrderBy(x => x.Tick).ThenBy(x => x.SortKey))
            {
                ev.DeltaTime = tick - lastTick;
                chunk.Events.Add(ev);
                lastTick = tick;
            }
            return chunk;
        }
        #endregion

        #region RULER SCRUBBING
        private bool _isScrubbing { get; set; } = false;

        private void RulerCanvasParent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_player == null)
            {
                e.Handled = true;
                return;
            }
            _isScrubbing = true;
            _player.Pause();
            _playbackUITimer.Stop();
            UpdatePlaybackButtonStates();
            RulerCanvasParent.CaptureMouse();
            SeekToRulerPosition(e.GetPosition(RulerCanvasParent).X);
            e.Handled = true;
        }

        private void RulerCanvasParent_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isScrubbing) return;
            SeekToRulerPosition(e.GetPosition(RulerCanvasParent).X);
        }

        private void RulerCanvasParent_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_player == null) return;
            if (!_isScrubbing) return;
            _isScrubbing = false;
            if (RulerCanvasParent.IsMouseCaptured)
                RulerCanvasParent.ReleaseMouseCapture();
            _player.Pause();
            _playbackUITimer.Stop();
            UpdatePlaybackButtonStates();
        }

        /// <summary>
        /// Converts the X position in the Ruler to ticks, then to time, and finally to SeekTo.
        /// The X position in the RulerCanvasParent corresponds to the viewport position,
        /// so the HorizontalOffset must be added to obtain the absolute canvas X coordinate.
        /// </summary>
        private void SeekToRulerPosition(double rulerX)
        {
            double canvasX = rulerX + MainScroll.HorizontalOffset;
            long ticks = Math.Max(0, (long)(canvasX / _tickPixelScale));
            var time = (MetricTimeSpan)TimeConverter.ConvertTo<MetricTimeSpan>(
                                 ticks, _midiFile.GetTempoMap());

            UpdatePlayheadPosition(time);
            PlaybackTimeText.Text = $"{(int)((TimeSpan)time).TotalMinutes}:{((TimeSpan)time).Seconds:D2}.{((TimeSpan)time).Milliseconds:D3}";
            _player?.SeekTo(time);
        }

        private void UpdatePlaybackUI()
        {
            if (_player == null) return;
            var t = _player.CurrentTime;
            PlaybackTimeText.Text = $"{(int)t.TotalMinutes}:{t.Seconds:D2}.{t.Milliseconds:D3}";
            UpdatePlayheadPosition(t);
        }

        /// <summary>
        /// Calculates the playhead's X position based on the current time
        /// and moves the line and arrow accordingly.
        /// </summary>
        private void UpdatePlayheadPosition(TimeSpan time)
        {
            // Time -> Ticks -> Pixel
            var tempoMap = _midiFile.GetTempoMap();
            long ticks = TimeConverter.ConvertFrom(new MetricTimeSpan(time), tempoMap);
            double xInCanvas = ticks * _tickPixelScale;

            // Position relative to the ScrollViewer viewport
            double xInViewport = xInCanvas - MainScroll.HorizontalOffset;

            // Visible only if within the viewport
            bool visible = xInViewport >= 0 && xInViewport <= MainScroll.ViewportWidth;
            PlayheadLine.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            Canvas.SetLeft(PlayheadLine, xInCanvas);

            PlayheadRulerLine.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            Canvas.SetLeft(PlayheadRulerLine, xInViewport);

            PlayheadArrow.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            Canvas.SetLeft(PlayheadArrow, xInViewport);
            Canvas.SetTop(PlayheadArrow, 0);

            // Auto-Scroll
            if (_player?.IsPlaying == true && xInViewport > MainScroll.ViewportWidth - 40)
                MainScroll.ScrollToHorizontalOffset(
                    xInCanvas - MainScroll.ViewportWidth / 2);
        }

        private void HidePlayhead()
        {
            PlayheadLine.Visibility = Visibility.Collapsed;
            PlayheadRulerLine.Visibility = Visibility.Collapsed;
            PlayheadArrow.Visibility = Visibility.Collapsed;
        }

        private void UpdatePlaybackButtonStates()
        {
            bool playing = _player?.IsPlaying ?? false;
            bool paused = _player?.IsPaused ?? false;
            PlayButton.Foreground = new SolidColorBrush(playing
                ? Color.FromRgb(76, 175, 80) : Color.FromRgb(100, 100, 100));
            PauseButton.Foreground = new SolidColorBrush(paused
                ? Color.FromRgb(255, 193, 7) : Color.FromRgb(100, 100, 100));
        }
        #endregion

        #region MenuStuff
        private void AutoTranspose_Click(object sender, RoutedEventArgs e)
        {
            var targets = _selectedNotes.Any() ? _selectedNotes.ToList() : _notes.ToList();
            if (!targets.Any()) return;
            double avg = targets.Average(n => (int)n.NoteNumber);
            int octaveShift = (int)Math.Round((66 - avg) / 12.0) * 12;
            if (octaveShift == 0) return;
            var snapshots = targets.Select(n => (Note: n, OldPitch: (int)n.NoteNumber)).ToList();
            ExecuteAndRegisterUndo($"Auto-Transpose ({octaveShift})",
                undoAction: () =>
                {
                    foreach (var s in snapshots)
                        s.Note.NoteNumber = (SevenBitNumber)Math.Max(0, Math.Min(127, s.OldPitch));
                    RenderNotes();
                },
                redoAction: () =>
                {
                    foreach (var s in snapshots)
                        s.Note.NoteNumber = (SevenBitNumber)Math.Max(0, Math.Min(127, s.OldPitch + octaveShift));
                    RenderNotes();
                });
            RefreshLayout();
        }

        private void Quantize_Click(object sender, RoutedEventArgs e)
        {
            var targets = _selectedNotes.Any() ? _selectedNotes.ToList() : _notes.ToList();
            if (!targets.Any()) return;
            var snapshots = targets.Select(n => (Note: n, OldTime: n.Time, OldLen: n.Length)).ToList();
            ExecuteAndRegisterUndo("Quantize",
                undoAction: () =>
                {
                    foreach (var s in snapshots) { s.Note.Time = s.OldTime; s.Note.Length = s.OldLen; }
                    RenderNotes();
                },
                redoAction: () =>
                {
                    foreach (var s in snapshots)
                    {
                        s.Note.Time = Snap(s.Note.Time);
                        s.Note.Length = Math.Max(_gridSnapTicks, Snap(s.Note.Length));
                    }
                    RenderNotes();
                });
            RefreshLayout();
        }
        #endregion

        #region Save Logic
        public TrackChunk ResultTrackChunk { get; private set; }

        private void SaveTrack_Click(object sender, RoutedEventArgs e)
        {
            StopPlayback();
            var trackChunk = new TrackChunk();
            using (var manager = trackChunk.ManageNotes())
            {
                foreach (var note in _notes)
                    manager.Objects.Add(new Note(note.NoteNumber, note.Length, note.Time)
                    {
                        Velocity = note.Velocity,
                        OffVelocity = note.OffVelocity,
                        Channel = note.Channel
                    });
            }

            if (_programChanges?.Any() == true)
            {
                var rawEvents = _programChanges
                    .Select(pt => (Tick: pt.Tick, Ev: (MidiEvent)new ProgramChangeEvent((SevenBitNumber)pt.Value)))
                    .OrderBy(x => x.Tick)
                    .ToList();

                var allEvents = trackChunk.Events
                    .Select((ev, idx) =>
                    {
                        long abs = 0;
                        foreach (var prev in trackChunk.Events.Take(idx + 1)) abs += prev.DeltaTime;
                        return (AbsTick: abs, Event: ev);
                    })
                    .Concat(rawEvents.Select(r => (AbsTick: r.Tick, Event: r.Ev)))
                    .OrderBy(x => x.AbsTick)
                    .ThenBy(x => x.Event is ProgramChangeEvent ? 0 : 1)
                    .ToList();

                trackChunk.Events.Clear();
                long last = 0;
                foreach (var item in allEvents)
                {
                    item.Event.DeltaTime = item.AbsTick - last;
                    trackChunk.Events.Add(item.Event);
                    last = item.AbsTick;
                }
            }

            this.ResultTrackChunk = trackChunk;
            this.DialogResult = true;
            this.Close();
        }
        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            StopPlayback();
            CleanupPlayer();
        }
    }
}