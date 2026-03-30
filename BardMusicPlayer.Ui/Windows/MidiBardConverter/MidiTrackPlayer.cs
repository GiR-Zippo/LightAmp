/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Siren;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BardMusicPlayer.Ui.Windows.MidiBardConverter
{
    public sealed class MidiTrackPlayer : IDisposable
    {
        private bool _disposed;
        private Playback _playback;
        private OutputDevice _outputDevice;

        private readonly bool _sirenMode;
        private double _sirenEndTime { get; set; }
        private double _sirenCurrentMs { get; set; }

        public event EventHandler Finished;

        public bool IsPlaying => _sirenMode
            ? BmpSiren.Instance.IsReadyForPlayback && _sirenIsPlaying
            : _playback?.IsRunning ?? false;

        public bool IsPaused => _sirenMode
            ? BmpSiren.Instance.IsReadyForPlayback && !_sirenIsPlaying && _sirenCurrentMs > 0
            : _playback != null && !_playback.IsRunning
              && ((TimeSpan)_playback.GetCurrentTime<MetricTimeSpan>()) > TimeSpan.Zero;

        public bool IsStopped => !IsPlaying && !IsPaused;

        public TimeSpan CurrentTime => _sirenMode
            ? TimeSpan.FromMilliseconds(_sirenCurrentMs)
            : _playback?.GetCurrentTime<MetricTimeSpan>() ?? TimeSpan.Zero;

        private bool _sirenIsPlaying;

        public MidiTrackPlayer(MidiFile midiFile, bool useSiren = false)
        {
            if (midiFile == null) throw new ArgumentNullException(nameof(midiFile));
            _sirenMode = useSiren;
            if (useSiren)
                InitializeSiren(midiFile.GetTrackChunks().ToList(), midiFile);
            else
                InitializeMidi(midiFile.GetTrackChunks()
                    .SelectMany(t => t.Events)
                    .GetPlayback(midiFile.GetTempoMap(), _outputDevice = GetFirstOutputDevice()));
        }

        public MidiTrackPlayer(TrackChunk trackChunk, MidiFile midiFile, bool useSiren = false)
        {
            if (trackChunk == null) throw new ArgumentNullException(nameof(trackChunk));
            if (midiFile == null) throw new ArgumentNullException(nameof(midiFile));
            _sirenMode = useSiren;
            if (useSiren)
                InitializeSiren(new List<TrackChunk> { trackChunk }, midiFile);
            else
                InitializeMidi(trackChunk.Events
                    .GetPlayback(midiFile.GetTempoMap(), _outputDevice = GetFirstOutputDevice()));
        }

        public MidiTrackPlayer(IEnumerable<TrackChunk> tracks, MidiFile midiFile, bool useSiren = false)
        {
            if (tracks == null) throw new ArgumentNullException(nameof(tracks));
            if (midiFile == null) throw new ArgumentNullException(nameof(midiFile));
            _sirenMode = useSiren;
            var list = tracks.ToList();
            if (useSiren)
                InitializeSiren(list, midiFile);
            else
                InitializeMidi(list.SelectMany(t => t.Events)
                    .GetPlayback(midiFile.GetTempoMap(), _outputDevice = GetFirstOutputDevice()));
        }

        private static OutputDevice GetFirstOutputDevice()
        {
            try
            {
                return OutputDevice.GetAll().FirstOrDefault()
                    ?? throw new InvalidOperationException("No MIDI-Output-Device found.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Couldn't open MIDI-Output: " + ex.Message, ex);
            }
        }

        private void InitializeMidi(Playback playback)
        {
            _playback = playback;
            _playback.InterruptNotesOnStop = true;
            _playback.TrackNotes = true;
            _playback.Finished += (s, e) => Finished?.Invoke(this, EventArgs.Empty);
        }

        private void InitializeSiren(List<TrackChunk> tracks, MidiFile midiFile)
        {
            if (!BmpSiren.Instance.IsReady)
                throw new InvalidOperationException(
                    "Siren not initialized.");

            BmpSiren.Instance.LoadTrackChunks(tracks, midiFile, "Preview");
            Finished?.Invoke(this, EventArgs.Empty);
            BmpSiren.Instance.SynthTimePositionChanged += OnSirenPositionChanged;
        }

        private void OnSirenPositionChanged(string title, double currentMs, double endMs, int voices)
        {
            if (_disposed) return;
            _sirenCurrentMs = currentMs;
            _sirenEndTime = endMs;
            _sirenIsPlaying = currentMs < endMs;

            // Playback end
            if (currentMs >= endMs && _sirenIsPlaying == false && endMs > 0)
            {
                _sirenIsPlaying = false;
                Finished?.Invoke(this, EventArgs.Empty);
                BmpSiren.Instance.SynthTimePositionChanged -= OnSirenPositionChanged;
            }
        }

        #region Playback control
        public void Play()
        {
            ThrowIfDisposed();
            if (_sirenMode) { _sirenIsPlaying = true; BmpSiren.Instance.Play(); }
            else _playback?.Start();
        }

        public void Pause()
        {
            ThrowIfDisposed();
            if (_sirenMode) { _sirenIsPlaying = false; BmpSiren.Instance.Pause(); }
            else _playback?.Stop();
        }

        public void Stop()
        {
            ThrowIfDisposed();
            if (_sirenMode)
            {
                _sirenIsPlaying = false;
                _sirenCurrentMs = 0;
                BmpSiren.Instance.Stop();
            }
            else
            {
                _playback?.Stop();
                _playback?.MoveToStart();
            }
        }

        public void SeekTo(TimeSpan position)
        {
            ThrowIfDisposed();
            if (_sirenMode)
                BmpSiren.Instance.SetPosition((int)position.TotalMilliseconds);
            else
                _playback?.MoveToTime(new MetricTimeSpan(position));
        }
        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_sirenMode)
                BmpSiren.Instance.SynthTimePositionChanged -= OnSirenPositionChanged;
            else
            {
                try
                {
                    _playback?.Stop();
                }
                catch { }
                _playback?.Dispose();
                _outputDevice?.Dispose();
                _playback = null;
                _outputDevice = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MidiTrackPlayer));
        }
    }
}