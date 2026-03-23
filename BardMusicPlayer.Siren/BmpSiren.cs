/*
 * Copyright(c) 2026 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BardMusicPlayer.Quotidian;
using BardMusicPlayer.Siren.AlphaTab;
using BardMusicPlayer.Siren.AlphaTab.Audio.Synth;
using BardMusicPlayer.Siren.AlphaTab.Audio.Synth.Midi;
using BardMusicPlayer.Siren.AlphaTab.Util;
using BardMusicPlayer.Siren.Properties;
using BardMusicPlayer.Transmogrify.Song;
using NAudio.CoreAudioApi;

namespace BardMusicPlayer.Siren
{
    public sealed class BmpSiren
    {
        /// <summary>
        ///     Event fired when there is a lyric line.
        /// </summary>
        /// <param name="singer"></param>
        /// <param name="line"></param>
        public delegate void Lyric(int singer, string line);
        public event Lyric LyricTrigger;

        /// <summary>
        ///     Event fired when the position of a synthesized song changes.
        /// </summary>
        /// <param name="songTitle">The title of the current song.</param>
        /// <param name="currentTime">The current time of this song in milliseconds</param>
        /// <param name="endTime">The total length of this song in milliseconds</param>
        /// <param name="activeVoices">Active voice count.</param>
        public delegate void SynthTimePosition(string songTitle, double currentTime, double endTime, int activeVoices);
        public event SynthTimePosition SynthTimePositionChanged;

        /// <summary>
        ///    Event fired when a new song is loaded 
        /// </summary>
        /// <param name="songTitle"></param>
        public delegate void NewSongLoaded(string songTitle);
        public event NewSongLoaded SongLoaded;

        private static readonly System.Lazy<BmpSiren> LazyInstance = new(static () => new BmpSiren());

        private readonly TaskQueue _taskQueue = new();
        private double _lyricIndex;
        private Dictionary<int, Dictionary<long, string>> _lyrics;
        private MMDevice _mdev;

        private IAlphaSynth _player;

        internal BmpSiren()
        {
        }

        public string CurrentSongTitle { get; private set; } = "";
        public BmpSong CurrentSong { get; private set; }
        public static BmpSiren Instance => LazyInstance.Value;

        /// <summary>
        ///     Gets a collection of available MMDevice objects
        /// </summary>
        public MMDeviceCollection AudioDevices =>
            new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        /// <summary>
        /// </summary>
        public bool IsReady => _player is { IsReady: true };

        /// <summary>
        /// </summary>
        public bool IsReadyForPlayback => IsReady && _player.IsReadyForPlayback;

        ~BmpSiren()
        {
            ShutDown();
        }

        /// <summary>
        /// </summary>
        /// <param name="device"></param>
        /// <param name="defaultVolume"></param>
        /// <param name="bufferCount"></param>
        /// <param name="latency"></param>
        public void Setup(MMDevice device, float defaultVolume = 0.8f, byte bufferCount = 3, byte latency = 100)
        {
            ShutDown();
            _mdev = device;
            _player = new ManagedThreadAlphaSynthWorkerApi(new NAudioSynthOutput(device, bufferCount, latency),
                LogLevel.None, BeginInvoke);
            foreach (var resource in Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, true))
                _player.LoadSoundFont((byte[])((DictionaryEntry)resource).Value, true);
            _player.PositionChanged += NotifyTimePosition;
            _player.MasterVolume = defaultVolume;
        }

        /// <summary>
        ///     Sets the volume
        /// </summary>
        public int GetVolume()
        {
            if (_player == null) return 0;
            return (int)(_mdev.AudioSessionManager.AudioSessionControl.SimpleAudioVolume.Volume * 100);
        }

        /// <summary>
        ///     Sets the volume
        /// </summary>
        /// <param name="x"></param>
        public void SetVolume(float x)
        {
            if (_player == null) return;
            _mdev.AudioSessionManager.AudioSessionControl.SimpleAudioVolume.Volume = x / 100;
        }

        /// <summary>
        ///     Sets the volume
        /// </summary>
        /// <param name="x"></param>
        public bool GetMute()
        {
            if (_player == null) return false;
            return _mdev.AudioSessionManager.AudioSessionControl.SimpleAudioVolume.Mute;
        }

        /// <summary>
        ///     Sets mute state
        /// </summary>
        /// <param name="x"></param>
        public void SetMute(bool muted)
        {
            if (_player == null) return;
            _mdev.AudioSessionManager.AudioSessionControl.SimpleAudioVolume.Mute = muted;
        }

        internal void BeginInvoke(Action action)
        {
            _taskQueue.Enqueue(() => Task.Run(action));
        }

        /// <summary>
        /// </summary>
        /// <param name="defaultVolume"></param>
        /// <param name="bufferCount"></param>
        /// <param name="latency"></param>
        public void Setup(float defaultVolume = 0.8f, byte bufferCount = 2, byte latency = 100)
        {
            try
            {
                var mmAudio = new MMDeviceEnumerator().GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                Setup(mmAudio, defaultVolume, bufferCount, latency);
            }
            catch
            { }
        }

        /// <summary>
        /// </summary>
        public void ShutDown()
        {
            if (_player == null) return;

            _player.Stop();
            _player.PositionChanged -= NotifyTimePosition;
            _player.Destroy();
        }

        /// <summary>
        ///     Loads a BmpSong into the synthesizer
        /// </summary>
        /// <param name="song"></param>
        /// <returns>This BmpSiren</returns>
        public async Task<BmpSiren> Load(BmpSong song)
        {
            if (!IsReady) throw new BmpException("Siren not initialized.");

            if (_player.State == PlayerState.Playing) _player.Stop();
            MidiFile midiFile;
            (midiFile, _lyrics) = await song.GetSynthMidi();
            _lyricIndex = 0;
            _player.LoadMidiFile(midiFile);
            CurrentSongTitle = song.Title;
            CurrentSong = song;
            SongLoaded?.Invoke(CurrentSongTitle);
            return this;
        }

        /// <summary>
        ///     Loads a single Melanchall track chunk directly into the synthesizer.
        /// </summary>
        public BmpSiren LoadTrackChunk(Melanchall.DryWetMidi.Core.TrackChunk trackChunk, Melanchall.DryWetMidi.Core.MidiFile sourceMidiFile, string title = "Preview")
        {
            if (!IsReady) throw new BmpException("Siren not initialized.");
            if (trackChunk == null) throw new ArgumentNullException(nameof(trackChunk));
            return LoadTrackChunks(new List<Melanchall.DryWetMidi.Core.TrackChunk> { trackChunk }, sourceMidiFile, title);
        }

        /// <summary>
        ///     Loads multiple Melanchall track chunks into Sirens' sequencer.
        /// </summary>
        public BmpSiren LoadTrackChunks(IEnumerable<Melanchall.DryWetMidi.Core.TrackChunk> tracks, Melanchall.DryWetMidi.Core.MidiFile sourceMidiFile, string title = "Preview")
        {
            if (!IsReady) throw new BmpException("Siren not initialized.");
            if (tracks == null) throw new ArgumentNullException(nameof(tracks));

            if (_player.State == PlayerState.Playing) _player.Stop();

            var sirenFile = Utils.ConvertToSirenMidi(tracks.ToList(), sourceMidiFile);
            _lyrics = new Dictionary<int, Dictionary<long, string>>();
            _lyricIndex = 0;
            _player.LoadMidiFile(sirenFile);
            CurrentSongTitle = title;
            SongLoaded?.Invoke(CurrentSongTitle);
            return this;
        }

        /// <summary>
        ///     Starts the playback if possible
        /// </summary>
        /// <returns>This BmpSiren</returns>
        public BmpSiren Record(string filename)
        {
            if (!IsReadyForPlayback) throw new BmpException("Siren not loaded with a song.");

            if (filename.Length <= 0)
                return this;

            _player.Record(filename);
            return this;
        }

        /// <summary>
        ///     Starts the playback if possible
        /// </summary>
        /// <returns>This BmpSiren</returns>
        public BmpSiren Play()
        {
            if (!IsReadyForPlayback) return this; //throw new BmpException("Siren not loaded with a song.");

            _player.Play();
            return this;
        }

        /// <summary>
        ///     Pauses the playback if was running
        /// </summary>
        /// <returns>This BmpSiren</returns>
        public BmpSiren Pause()
        {
            if (!IsReadyForPlayback) return this; //throw new BmpException("Siren not loaded with a song.");

            _player.Pause();
            return this;
        }

        /// <summary>
        ///     Stops the playback
        /// </summary>
        /// <returns>This BmpSiren</returns>
        public BmpSiren Stop()
        {
            if (!IsReadyForPlayback) return this; //throw new BmpException("Siren not loaded with a song.");

            _player.Stop();
            _lyricIndex = 0;
            return this;
        }

        /// <summary>
        ///     Sets the current position of this song in milliseconds
        /// </summary>
        /// <returns>This BmpSiren</returns>
        public BmpSiren SetPosition(int time)
        {
            if (!IsReadyForPlayback) return this; // throw new BmpException("Siren not loaded with a song.");

            if (time < 0)
                time = 0;

            //if (time > _player.PlaybackRange.EndTick) return Stop();
            _player.Stop();
            Task.Delay(50).Wait();
            _player.TickPosition = time;
            _lyricIndex = time;
            Task.Delay(50).Wait();
            _player.Play();
            return this;
        }

        // ══════════════════════════════════════════════════════════════
        // REALTIME-SYNTHESIZER API
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        ///     Plays a note immediately via the synthesiser.
        ///     No need to load a MIDI file – works straight after Setup().
        /// </summary>
        /// <param name="channel">MIDI channel 0–15 (default: 0)</param>
        /// <param name="key">Note number 0–127 (60 = middle C)</param>
        /// <param name="velocity">Velocity 0.0–1.0 (default: 1.0)</param>
        public BmpSiren NoteOn(int channel, int key, float velocity = 1.0f)
        {
            if (!IsReady) return this;
            channel = Math.Max(0, Math.Min(15, channel));
            key = Math.Max(0, Math.Min(127, key));
            velocity = Math.Max(0f, Math.Min(1f, velocity));
            _player.NoteOn(channel, key, velocity);
            return this;
        }

        /// <summary>
        ///     Stops a playing note (with a release envelope).
        /// </summary>
        /// <param name="channel">MIDI channel 0–15</param>
        /// <param name="key">Note number 0–127</param>
        public BmpSiren NoteOff(int channel, int key)
        {
            if (!IsReady) return this;
            channel = Math.Max(0, Math.Min(15, channel));
            key = Math.Max(0, Math.Min(127, key));
            _player.NoteOff(channel, key);
            return this;
        }

        /// <summary>
        ///     Stops all currently playing notes immediately (MIDI Panic).
        /// </summary>
        public BmpSiren AllNotesOff()
        {
            if (!IsReady) return this;
            _player.AllNotesOff();
            return this;
        }

        /// <summary>
        ///     Sets the instrument (MIDI programme) for a channel.
        ///     Must be called before NoteOn if an instrument other than piano is required.
        /// </summary>
        /// <param name="channel">MIDI channel 0–15</param>
        /// <param name="program">MIDI programme number 0–127</param>
        public BmpSiren SetProgram(int channel, byte program)
        {
            if (!IsReady) return this;
            channel = Math.Max(0, Math.Min(15, channel));
            _player.SetChannelProgram(channel, program);
            return this;
        }

        internal void NotifyTimePosition(PositionChangedEventArgs obj)
        {
            SynthTimePositionChanged?.Invoke(CurrentSongTitle, obj.CurrentTime, obj.EndTime, obj.ActiveVoices);
            if (_lyrics != null)
            {
                for (var singer = 0; singer < _lyrics.Count; singer++)
                {
                    var line = _lyrics[singer].FirstOrDefault(x => x.Key > _lyricIndex && x.Key < obj.CurrentTime).Value;
                    if (!string.IsNullOrWhiteSpace(line)) LyricTrigger?.Invoke(singer, line);
                }
            }
            _lyricIndex = obj.CurrentTime;
        }
    }
}