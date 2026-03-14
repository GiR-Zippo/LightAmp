/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.DalamudBridge;
using BardMusicPlayer.DalamudBridge.Helper.Dalamud;
using BardMusicPlayer.Maestro.Events;
using BardMusicPlayer.Maestro.FFXIV;
using BardMusicPlayer.Maestro.Sequencing;
using BardMusicPlayer.Maestro.Utils;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using System;
using System.Collections.Generic;

namespace BardMusicPlayer.Maestro.Performance
{
    public partial class Performer
    {
        private FFXIVHook _hook = new FFXIVHook();
        private Sequencer _sequencer { get; set; } = null;
        private Sequencer mainSequencer { get; set; } = null;
        private System.Timers.Timer _startDelayTimer { get; set; } = new System.Timers.Timer();
        private bool _holdNotes { get; set; } = true;
        private bool _forcePlayback { get; set; } = false;
        private int _trackNumber { get; set; } = 1;

        private long _lastNoteTimestamp { get; set; } = 0;
        private bool _livePlayDelay { get; set; } = false;

        public int SingerTrackNr { get; set; } = 0;
        public int OctaveShift { get; set; } = 0;
        public bool OctaveShiftEnabled { get { return !BmpPigeonhole.Instance.UseNoteOffset; } }
        public int TrackNumber { get { return _trackNumber; }
            set {
                if (value == _trackNumber)
                    return;

                if ((_sequencer == null) || (_sequencer.LoadedTrack == null))
                {
                    BmpMaestro.Instance.PublishEvent(new TrackNumberChangedEvent(game, _trackNumber, HostProcess));
                    return;
                }

                if (value > mainSequencer.MaxTrack || value < 0)
                {
                    BmpMaestro.Instance.PublishEvent(new TrackNumberChangedEvent(game, _trackNumber, HostProcess));
                    return;
                }

                _trackNumber = value;
                BmpMaestro.Instance.PublishEvent(new TrackNumberChangedEvent(game, _trackNumber, HostProcess));
                var tOctaveShift = mainSequencer.GetTrackPreferredOctaveShift(_sequencer.Sequence[this._trackNumber]);
                if (tOctaveShift != OctaveShift)
                {
                    OctaveShift = tOctaveShift;
                    BmpMaestro.Instance.PublishEvent(new OctaveShiftChangedEvent(game, OctaveShift, HostProcess));
                }
            }
        }

        public bool PerformerEnabled { get; set; } = true;
        public bool UsesDalamud {  get { return BmpPigeonhole.Instance.UsePluginForInstrumentOpen && GameExtensions.IsConnected(PId); } }
        public bool UsesDalamudForKeys { get; set; } = false;

        public bool HostProcess { get; set; } = false;
        public int PId { get; set; } = 0;
        public Game game;
        public string PlayerName { get { return game.PlayerName ?? "Unknown"; } }
        public string HomeWorld { get { return game.HomeWorld ?? "Unknown"; } }
        public string SongName 
        { 
            get
            {
                if (_sequencer.LoadedBmpSong == null) //no song, no title
                    return "";
                
                if (_sequencer.LoadedBmpSong.DisplayedTitle == null) //no displayed title, pretent the normal title
                    return _sequencer.LoadedBmpSong.Title;

                if (_sequencer.LoadedBmpSong.DisplayedTitle.Length < 2) //title with 1 char makes no sence for me
                    return _sequencer.LoadedBmpSong.Title;

                return _sequencer.LoadedBmpSong.DisplayedTitle; //finally, display the title
            } 
        }
        public string TrackInstrument 
        { 
            get {
                    if (_sequencer == null || _sequencer.LoadedBmpSong == null)
                        return "Unknown";
                    if (TrackNumber == 0)
                        return "None";
                    if (this._trackNumber >= _sequencer.Sequence.Count)
                        return "None";
                    if (_sequencer.LoadedBmpSong.TrackContainers.Count < TrackNumber)
                        return "None";

                    Transmogrify.Song.Config.ClassicProcessorConfig classicConfig = (Transmogrify.Song.Config.ClassicProcessorConfig)_sequencer.LoadedBmpSong.TrackContainers[TrackNumber - 1].ConfigContainers[0].ProcessorConfig; // track -1 cuz track 0 isn't in this container
                    return classicConfig.Instrument.Name;
                }
        }

        public Sequencer Sequencer
        {
            get{ return _sequencer; }
            set
            {
                if (value != null)
                {
                    if ((value.LoadedFileType == Sequencer.FILETYPES.None) && !HostProcess)
                        return;

                    this.UsesDalamudForKeys = BmpPigeonhole.Instance.UsePluginForKeyOutput && GameExtensions.IsConnected(PId);

                    //Close the input else it will hang
                    if (_sequencer is Sequencer)
                        _sequencer.CloseInputDevice();

                    this._startDelayTimer.Enabled = false;
                    this.mainSequencer = value;

                    this._sequencer = new Sequencer();
                    if (value.LoadedFileType == Sequencer.FILETYPES.BmpSong)
                    {
                        this._sequencer.Sequence = mainSequencer.Sequence;
                        this.OctaveShift = 0;
                    }

                    if (HostProcess)
                    {
                        if (BmpPigeonhole.Instance.MidiInputDev != -1 && 
                           (BmpPigeonhole.Instance.MidiInputDev < Sanford.Multimedia.Midi.InputDevice.DeviceCount))
                                _sequencer.OpenInputDevice(BmpPigeonhole.Instance.MidiInputDev);
                    }

                    this._sequencer.OnNote += InternalNote;
                    this._sequencer.OffNote += InternalNote;
                    this._sequencer.ProgChange += InternalProg;
                    this._sequencer.OnLyric += InternalLyrics;
                    this._sequencer.ChannelAfterTouch += InternalAT;

                    _holdNotes = BmpPigeonhole.Instance.HoldNotes;
                    _lastNoteTimestamp = 0;
                    _livePlayDelay = BmpPigeonhole.Instance.LiveMidiPlayDelay;

                    if (HostProcess && BmpPigeonhole.Instance.ForcePlayback)
                        _forcePlayback = true;

                    // set the initial octave shift here, if we have a track to play
                    if (this._trackNumber < _sequencer.Sequence.Count)
                    {
                        this.OctaveShift = mainSequencer.GetTrackPreferredOctaveShift(_sequencer.Sequence[this._trackNumber]);
                        BmpMaestro.Instance.PublishEvent(new OctaveShiftChangedEvent(game, OctaveShift, HostProcess));
                    }
                    this.Update(value);
                }
            }
        }

#region public
        public Performer(Game arg)
        {
            if (arg != null)
            {
                _hook.Hook(arg.Process, false);
                PId = arg.Pid;
                game = arg;
                _startDelayTimer.Elapsed += startDelayTimer_Elapsed;
                _lyricsTick.Elapsed += LyricsTick_Elapsed;
            }
        }

        /// <summary>
        /// Close the input device
        /// </summary>
        public void Close()
        {
            if (_sequencer is Sequencer)
            {
                _sequencer.CloseInputDevice();
                _sequencer.Dispose();
            }
            _hook.ClearLastPerformanceKeybinds();

            _lyricsTick.Elapsed -= LyricsTick_Elapsed;
            _lyricsTick.Dispose();

            _startDelayTimer.Elapsed -= startDelayTimer_Elapsed;
            _startDelayTimer.Dispose();
        }

        public void SetProgress(int progress)
        {
            if (_sequencer is Sequencer)
            {
                _sequencer.Position = progress;
            }
        }

        public void Play(bool play, int delay = 0)
        {
            if (_sequencer is Sequencer)
            {
                if (play)
                {
                    if (_sequencer.IsPlaying)
                        return;

                    if (delay == 0)
                        _sequencer.Play();
                    else
                    {
                        if (_startDelayTimer.Enabled)
                            return;
                        _startDelayTimer.Interval = delay;
                        _startDelayTimer.Enabled = true;
                    }
                }
                else
                {
                    _sequencer.Pause();

                    if (UsesDalamud)
                        DalamudBridge.DalamudBridge.Instance.ActionToQueue(new DalamudBridgeCommandStruct { messageType = MessageType.NoteOn, game = game, BoolData = false });
                    else
                        _hook.ClearLastPerformanceKeybinds();
                }
            }
        }

        public void Stop()
        {
            if (_startDelayTimer.Enabled)
                _startDelayTimer.Enabled = false;

            StopLyricsTimer();

            if (_sequencer is Sequencer)
            {
                _sequencer.Stop();

                if (UsesDalamud)
                    DalamudBridge.DalamudBridge.Instance.ActionToQueue(new DalamudBridgeCommandStruct { messageType = MessageType.NoteOn, game = game, BoolData = false });
                else
                    _hook.ClearLastPerformanceKeybinds();
            }
        }

        #endregion

        #region private
        private void Update(Sequencer bmpSeq)
        {
            //Check if bmpSeq is a sequencer
            if (bmpSeq is null)
                return;

            Sanford.Multimedia.Midi.Sequence seq = bmpSeq.Sequence;
            //Check if there's a sequence in the sequencer
            if (seq is null)
                return;

            if ((_trackNumber >= 0 && _trackNumber < seq.Count) && seq[_trackNumber] is Sanford.Multimedia.Midi.Track track)
            {
                // OctaveNum now holds the track octave and the selected octave together
                Console.WriteLine(String.Format("Track #{0}/{1} setOctave: {2} prefOctave: {3}", _trackNumber, bmpSeq.MaxTrack, OctaveShift, bmpSeq.GetTrackPreferredOctaveShift(track)));
                List<int> notes = new List<int>();
                foreach (Sanford.Multimedia.Midi.MidiEvent ev in track.Iterator())
                {
                    if (ev.MidiMessage.MessageType == Sanford.Multimedia.Midi.MessageType.Channel)
                    {
                        Sanford.Multimedia.Midi.ChannelMessage msg = (ev.MidiMessage as Sanford.Multimedia.Midi.ChannelMessage);
                        if (msg.Command == Sanford.Multimedia.Midi.ChannelCommand.NoteOn)
                        {
                            int note = msg.Data1;
                            int vel = msg.Data2;
                            if (vel > 0)
                            {
                                notes.Add(NoteHelper.ApplyOctaveShift(note, this.OctaveShift));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if we are ont the right track and channel
        /// </summary>
        /// <returns></returns>
        private bool trackAndChannelOk()
        {
            // don't open instrument if we don't have anything loaded
            if (_sequencer == null || _sequencer.Sequence == null)
                return false;

            // don't open instrument if we're not on a valid track
            if (_trackNumber == 0 || _trackNumber >= _sequencer.Sequence.Count)
                return false;
            return true;
        }

        private void startDelayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_sequencer is Sequencer)
            {
                _sequencer.Play();
                _startDelayTimer.Enabled = false;
            }
        }


        private void InternalAT(object sender, Sanford.Multimedia.Midi.ChannelMessageEventArgs args)
        {
            /*var builder = new Sanford.Multimedia.Midi.ChannelMessageBuilder(args.Message);
            var atevent = new ChannelAfterTouchEvent
            {
                track = args.MidiTrack,
                trackNum = _sequencer.GetTrackNum(args.MidiTrack),
                command = args.Message.Data1,
            };

            if (_sequencer.GetTrackNum(atevent.track) != this._trackNumber)
                return;

            switch (atevent.command)
            {
                case 0:
                    OpenInstrument();
                    break;
                case 1:
                    CloseInstrument();
                    break;
            }*/

        }
#endregion
    }
}

