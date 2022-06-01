using BardMusicPlayer.Maestro.Events;
using BardMusicPlayer.Maestro.FFXIV;
using BardMusicPlayer.Maestro.Sequencing;
using BardMusicPlayer.Maestro.Utils;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Seer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BardMusicPlayer.Maestro.Performance
{
    public class Performer
    {
        private FFXIVHotbarDat _hotbar = new FFXIVHotbarDat();
        private FFXIVHook _hook = new FFXIVHook();
        private System.Timers.Timer _startDelayTimer { get; set; } = new System.Timers.Timer();
        private bool _holdNotes { get; set; } = true;
        private bool _forcePlayback { get; set; } = false;
        private Sequencer _sequencer;
        private Sequencer mainSequencer { get; set; } = null;

        public Instrument ChosenInstrument { get; set; } = Instrument.Piano;
        public int OctaveShift { get; set; } = 0;
        public int TrackNumber { get; set; } = 1;
        public bool PerformerEnabled { get; set; } = true;

        public EventHandler onUpdate;
        public bool HostProcess { get; set; } = false;
        public int PId = 0;
        public Game game;
        public string PlayerName { get { return game.PlayerName ?? "Unknown"; } }
        public string HomeWorld { get { return game.HomeWorld ?? "Unknown"; } }

        public string TrackInstrument 
        { 
            get {
                    if (_sequencer == null || _sequencer.LoadedBmpSong == null)
                        return "Unknown";
                    if (TrackNumber == 0)
                        return "None";

                    Transmogrify.Song.Config.ClassicProcessorConfig classicConfig = (Transmogrify.Song.Config.ClassicProcessorConfig)_sequencer.LoadedBmpSong.TrackContainers[TrackNumber - 1].ConfigContainers[0].ProcessorConfig; // track -1 cuz track 0 isn't in this container
                    
                    //Leave it here cuz it's gettn called anyway
                    var tOctaveShift = mainSequencer.GetTrackPreferredOctaveShift(_sequencer.Sequence[this.TrackNumber]);
                    if (tOctaveShift != OctaveShift)
                    {
                        OctaveShift = tOctaveShift;
                        BmpMaestro.Instance.PublishEvent(new OctaveShiftChangedEvent(game, OctaveShift, HostProcess));
                    }
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
                    
                    //Close the input else it will hang
                    if (_sequencer is Sequencer)
                        _sequencer.CloseInputDevice();

                    this.mainSequencer = value;

                    this._sequencer = new Sequencer();
                    if (value.LoadedFileType == Sequencer.FILETYPES.BmpSong)
                    {
                        this._sequencer.Sequence = mainSequencer.Sequence;
                        this.OctaveShift = 0;
                    }

                    if (HostProcess)
                    {
                        if (BmpPigeonhole.Instance.MidiInputDev != -1)
                            _sequencer.OpenInputDevice(BmpPigeonhole.Instance.MidiInputDev);
                    }

                    this._sequencer.OnNote += InternalNote;
                    this._sequencer.OffNote += InternalNote;
                    this._sequencer.ProgChange += InternalProg;

                    _holdNotes = BmpPigeonhole.Instance.HoldNotes;
                    if (HostProcess && BmpPigeonhole.Instance.ForcePlayback)
                        _forcePlayback = true;

                    // set the initial octave shift here, if we have a track to play
                    if (this.TrackNumber < _sequencer.Sequence.Count)
                    {
                        this.OctaveShift = mainSequencer.GetTrackPreferredOctaveShift(_sequencer.Sequence[this.TrackNumber]);
                        BmpMaestro.Instance.PublishEvent(new OctaveShiftChangedEvent(game, OctaveShift, HostProcess));
                    }
                    this.Update(value);
                }
            }
        }

#region public
        public Performer(Game arg)
        {
            this.ChosenInstrument = this.ChosenInstrument;

            if (arg != null)
            {
                _hook.Hook(arg.Process, false);
                _hotbar.LoadHotbarDat(arg.ConfigId);
                PId = arg.Pid;
                game = arg;
                _startDelayTimer.Elapsed += startDelayTimer_Elapsed;
            }
        }

        public void ProcessOnNote(NoteEvent note)
        {
            if (!_forcePlayback)
            {
                if (!this.PerformerEnabled)
                    return;

                if (game.InstrumentHeld.Equals(Instrument.None))
                    return;
            }

            if (note.note < 0 || note.note > 36)
                return;

            if (game.NoteKeys[(Quotidian.Enums.NoteKey)note.note] is Quotidian.Enums.Keys keybind)
            {
                if (game.ChatStatus && !_forcePlayback)
                    return;

                if (BmpPigeonhole.Instance.HoldNotes)
                    _hook.SendKeybindDown(keybind);
                else
                    _hook.SendAsyncKeybind(keybind);
            }
        }

        public void ProcessOffNote(NoteEvent note)
        {
            if (!this.PerformerEnabled)
                return;

            if (note.note < 0 || note.note > 36)
                return;

            if (game.NoteKeys[(Quotidian.Enums.NoteKey)note.note] is Quotidian.Enums.Keys keybind)
            {
                if (game.ChatStatus && !_forcePlayback)
                    return;

                if (_holdNotes)
                    _hook.SendKeybindUp(keybind);
            }
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
                }
            }
        }

        public void Stop()
        {
            if (_startDelayTimer.Enabled)
                _startDelayTimer.Enabled = false;

            if (_sequencer is Sequencer)
            {
                _sequencer.Stop();
                _hook.ClearLastPerformanceKeybinds();
            }
        }

        public void Update(Sequencer bmpSeq)
        {
            int tn = TrackNumber;

            if (!(bmpSeq is Sequencer))
            {
                return;
            }

            Sanford.Multimedia.Midi.Sequence seq = bmpSeq.Sequence;
            if (!(seq is Sanford.Multimedia.Midi.Sequence))
            {
                return;
            }

            if ((tn >= 0 && tn < seq.Count) && seq[tn] is Sanford.Multimedia.Midi.Track track)
            {
                // OctaveNum now holds the track octave and the selected octave together
                Console.WriteLine(String.Format("Track #{0}/{1} setOctave: {2} prefOctave: {3}", tn, bmpSeq.MaxTrack, OctaveShift, bmpSeq.GetTrackPreferredOctaveShift(track)));
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
                ChosenInstrument = bmpSeq.GetTrackPreferredInstrument(track);
            }
        }

        public void OpenInstrument()
        {
            // if we are the host, we do this by our own
            if (!game.InstrumentHeld.Equals(Instrument.None))
                return;

            // don't open instrument if we don't have anything loaded
            if (_sequencer == null || _sequencer.Sequence == null)
                return;

            // don't open instrument if we're not on a valid track
            if (TrackNumber == 0 || TrackNumber >= _sequencer.Sequence.Count)
                return;

            var t = Instrument.Parse(TrackInstrument);
            _hook.SendSyncKeybind(game.InstrumentKeys[t]);
        }

        /// <summary>
        /// Close the instrument
        /// </summary>
        public void CloseInstrument()
        {
            if (game.InstrumentHeld.Equals(Instrument.None))
                return;

            _hook.ClearLastPerformanceKeybinds();
            _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.ESC]);
            //performanceUp = false;
        }

        /// <summary>
        /// Accept the ready check
        /// </summary>
        public void EnsembleAccept()
        {
            _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
            Task.Delay(200);
            _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
        }


        public void Close()
        {
            if (_sequencer is Sequencer)
            {
                _sequencer.CloseInputDevice();
                _sequencer.Dispose();
            }
            _hook.ClearLastPerformanceKeybinds();
        }

#endregion
#region private
        private void startDelayTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_sequencer is Sequencer)
            {
                if (!_sequencer.IsPlaying)
                    _sequencer.Play();
                _startDelayTimer.Enabled = false;
            }
        }

        private void InternalNote(Object o, Sanford.Multimedia.Midi.ChannelMessageEventArgs args)
        {
            Sanford.Multimedia.Midi.ChannelMessageBuilder builder = new Sanford.Multimedia.Midi.ChannelMessageBuilder(args.Message);

            NoteEvent noteEvent = new NoteEvent
            {
                note = builder.Data1,
                origNote = builder.Data1,
                trackNum = _sequencer.GetTrackNum(args.MidiTrack),
                track = args.MidiTrack,
            };

            if ((_sequencer.GetTrackNum(noteEvent.track) == this.TrackNumber) || !_sequencer.IsPlaying)
            {
                noteEvent.note = NoteHelper.ApplyOctaveShift(noteEvent.note, this.OctaveShift);

                Sanford.Multimedia.Midi.ChannelCommand cmd = args.Message.Command;
                int vel = builder.Data2;
                if ((cmd == Sanford.Multimedia.Midi.ChannelCommand.NoteOff) || (cmd == Sanford.Multimedia.Midi.ChannelCommand.NoteOn && vel == 0))
                {
                    this.ProcessOffNote(noteEvent);
                }
                if ((cmd == Sanford.Multimedia.Midi.ChannelCommand.NoteOn) && vel > 0)
                {
                    this.ProcessOnNote(noteEvent);
                }
            }
        }

        private void InternalProg(object sender, Sanford.Multimedia.Midi.ChannelMessageEventArgs args)
        {
            if (!_forcePlayback)
            {
                if (!this.PerformerEnabled)
                    return;

                if (game.InstrumentHeld.Equals(Instrument.None))
                    return;
            }

            var builder = new Sanford.Multimedia.Midi.ChannelMessageBuilder(args.Message);
            var programEvent = new ProgChangeEvent
            {
                track = args.MidiTrack,
                trackNum = _sequencer.GetTrackNum(args.MidiTrack),
                voice = args.Message.Data1,
            };
            if (programEvent.voice < 27 || programEvent.voice > 31)
                return;

            if (_sequencer.GetTrackNum(programEvent.track) == this.TrackNumber)
            {
                if (game.ChatStatus && !_forcePlayback)
                    return;

                int tone = -1;
                switch (programEvent.voice)
                {
                    case 29: // overdriven guitar
                        tone = 0;
                        break;
                    case 27: // clean guitar
                        tone = 1;
                        break;
                    case 28: // muted guitar
                        tone = 2;
                        break;
                    case 30: // power chords
                        tone = 3;
                        break;
                    case 31: // special guitar
                        tone = 4;
                        break;
                }

                if (tone > -1 && tone < 5 && game.InstrumentToneMenuKeys[(Quotidian.Enums.InstrumentToneMenuKey)tone] is Quotidian.Enums.Keys keybind)
                    _hook.SendSyncKey(keybind);
            }
        }
#endregion
    }
}

