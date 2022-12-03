/*
 * Copyright(c) 2022 Parulina, trotlinebeercan, GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;

using Timer = System.Timers.Timer;

using Sanford.Multimedia.Midi;
using System.Text.RegularExpressions;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Maestro.Sequencing.Internal;
using BardMusicPlayer.Transmogrify.Song;

namespace BardMusicPlayer.Maestro.Sequencing
{
    public sealed class Sequencer : Sequencer_Internal
    {
        InputDevice midiInput = null;

        private Dictionary<Track, Instrument> preferredInstruments = new Dictionary<Track, Instrument>();
        private Dictionary<Track, int> preferredOctaveShift = new Dictionary<Track, int>();

        public EventHandler OnLoad;
        public EventHandler<ChannelMessageEventArgs> OnNote;
        public EventHandler<ChannelMessageEventArgs> OffNote;
        public EventHandler<ChannelMessageEventArgs> ProgChange;
        public EventHandler<ChannelMessageEventArgs> ChannelAfterTouch;

        public EventHandler<MetaMessageEventArgs> OnLyric;
        public EventHandler<int> OnTempoChange;
        public EventHandler<string> OnTrackNameChange;

        private Timer secondTimer = new Timer(200);
        public EventHandler<int> OnTick;

        public Dictionary<Track, int> notesPlayedCount = new Dictionary<Track, int>();


        private string loadedError = string.Empty;
        public string LoadedError
        {
            get { return loadedError; }
        }

        public enum FILETYPES
        {
            None = 0,
            BmpSong = 1
        };

        FILETYPES loadedFileType = FILETYPES.None;
        public FILETYPES LoadedFileType
        {
            get
            {
                return loadedFileType;
            }
        }

        string loadedFilename => string.Empty;
        public string LoadedFilename
        {
            get
            {
                return loadedFilename;
            }
        }

        BmpSong loadedBmpSong = null;
        public BmpSong LoadedBmpSong
        {
            get
            {
                return loadedBmpSong;
            }
            set
            {
                loadedBmpSong = value;
            }
        }

        public bool Loaded
        {
            get
            {
                return (Sequence != null);
            }
        }

        int midiTempo = 120;

        public int CurrentTick
        {
            get { return this.Position; }
        }
        public int MaxTick
        {
            get { return this.Length; }
        }

        public string CurrentTime
        {
            get
            {
                float ms = GetTimeFromTick(CurrentTick);
                TimeSpan t = TimeSpan.FromMilliseconds(ms);
                return $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
            }
        }

        public TimeSpan CurrentTimeAsTimeSpan
        {
            get
            {
                float ms = GetTimeFromTick(CurrentTick);
                TimeSpan t = TimeSpan.FromMilliseconds(ms);
                return t;
            }
        }

        public string MaxTime
        {
            get
            {
                float ms = GetTimeFromTick(MaxTick - 1);
                TimeSpan t = TimeSpan.FromMilliseconds(ms);
                return $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
            }
        }

        public TimeSpan MaxTimeAsTimeSpan
        {
            get
            {
                float ms = GetTimeFromTick(MaxTick - 1);
                TimeSpan t = TimeSpan.FromMilliseconds(ms);
                return t;
            }
        }

        int loadedTrack = 0;
        private int intendedTrack = 0;
        public int CurrentTrack
        {
            get
            {
                return loadedTrack;
            }
        }

        int _lyricStartTrackIndex = 0;
        public int LyricStartTrack
        {
            get
            {
                return _lyricStartTrackIndex;
            }
        }

        int _maxTracks = 0;
        public int MaxTrack => _maxTracks < 0 ? 0 : _maxTracks;

        public int MaxAllTrack
        {
            get
            {
                if (Sequence.Count <= 0)
                {
                    return 0;
                }
                return this.Sequence.Count - 1;
            }
        }

        public Track LoadedTrack
        {
            get
            {
                if (loadedTrack >= Sequence.Count || loadedTrack < 0)
                    return null;

                return BmpPigeonhole.Instance.PlayAllTracks ? Sequence[0] : Sequence[CurrentTrack];
            }
        }

        public Sequencer() : base()
        {
            Sequence = new Sequence();

            this.ChannelMessagePlayed += OnChannelMessagePlayed;
            this.MetaMessagePlayed += OnMetaMessagePlayed;

            secondTimer.Elapsed += OnSecondTimer;
        }

        public int GetTrackNum(Track track)
        {
            for (int i = 0; i < Sequence.Count; i++)
            {
                if (Sequence[i] == track)
                {
                    return i;
                }
            }
            return -1;
        }

        private void OnSecondTimer(object sender, EventArgs e)
        {
            OnTick?.Invoke(this, this.Position);
        }

        public void Seek(double ms)
        {
            int ticks = (int)(Sequence.Division * ((midiTempo / 60000f) * ms));
            if ((this.Position + ticks) < this.MaxTick && (this.Position + ticks) >= 0)
            {
                this.Position = ticks;
            }
        }

        public void Seek(int ticks)
        {
            this.Position = ticks;
        }

        public new void Play()
        {
            secondTimer.Start();
            OnSecondTimer(this, EventArgs.Empty);
            base.Play();
        }
        public new void Pause()
        {
            secondTimer.Stop();
            base.Pause();
        }

        public static float GetTimeFromTick(int tick)
        {
            if (tick <= 0)
            {
                return 0f;
            }
            return tick; // midi ppq and tempo  tick = 1ms now.
        }

        private void Chaser_Chased(object sender, ChasedEventArgs e)
        {
            throw new NotImplementedException();
        }

        public void OpenInputDevice(int device)
        {
            if (device == -1)
            {
                Console.WriteLine("[Sequencer] No Midi input");
                return;
            }
            MidiInCaps cap = InputDevice.GetDeviceCapabilities(device);
            try
            {
                midiInput = new InputDevice(device);
                midiInput.StartRecording();
                midiInput.ChannelMessageReceived += OnSimpleChannelMessagePlayed;

                Console.WriteLine(string.Format("{0} opened.", cap.name));
            }
            catch (InputDeviceException)
            {
                Console.WriteLine(string.Format("Couldn't open input {0}.", device));
            }
        }

        public void OpenInputDevice(string device)
        {
            for (var i = 0; i < InputDevice.DeviceCount; i++)
            {
                var cap = InputDevice.GetDeviceCapabilities(i);
                if (cap.name != device) 
                    continue;

                try
                {
                    midiInput = new InputDevice(i);
                    midiInput.StartRecording();
                    midiInput.ChannelMessageReceived += OnSimpleChannelMessagePlayed;

                    Console.WriteLine("{0} opened.", cap.name);
                }
                catch (InputDeviceException)
                {
                    Console.WriteLine("Couldn't open input {0}.", device);
                }
            }
        }

        public void CloseInputDevice()
        {
            if (midiInput is not { IsDisposed: false }) 
                return;

            midiInput.StopRecording();
            midiInput.Close();
        }

        private void OnSimpleChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
        {
            var builder = new ChannelMessageBuilder(e.Message);
            var note = builder.Data1;
            var vel = builder.Data2;
            var cmd = e.Message.Command;
            if (cmd == ChannelCommand.NoteOff || (cmd == ChannelCommand.NoteOn && vel == 0)) OffNote?.Invoke(this, e);
            switch (cmd)
            {
                case ChannelCommand.NoteOn when vel > 0:
                    OnNote?.Invoke(this, e);
                    break;
                case ChannelCommand.ProgramChange:
                {
                    string instName = Instrument.ParseByProgramChange(e.Message.Data1);
                    if (!string.IsNullOrEmpty(instName))
                        ProgChange?.Invoke(this, e);
                    break;
                }
                case ChannelCommand.ChannelPressure:
                    ChannelAfterTouch?.Invoke(this, e);
                    break;
            }
        }

        private void OnChannelMessagePlayed(object sender, ChannelMessageEventArgs e)
        {
            OnSimpleChannelMessagePlayed(sender, e);
        }
        private void OnMetaMessagePlayed(object sender, MetaMessageEventArgs e)
        {
            switch (e.Message.MetaType)
            {
                case MetaType.Tempo:
                {
                    var builder = new TempoChangeBuilder(e.Message);
                    midiTempo = 60000000 / builder.Tempo;
                    OnTempoChange?.Invoke(this, midiTempo);
                    break;
                }
                case MetaType.Lyric:
                    OnLyric?.Invoke(this, e);
                    break;
                case MetaType.TrackName:
                {
                    var builder = new MetaTextBuilder(e.Message);
                    ParseTrackName(e.MidiTrack, builder.Text);
                    if (e.MidiTrack == LoadedTrack)
                        OnTrackNameChange?.Invoke(this, builder.Text);
                    break;
                }
            }

            if (e.Message.MetaType != MetaType.InstrumentName) 
                return;

            {
                var builder = new MetaTextBuilder(e.Message);
                OnTrackNameChange?.Invoke(this, builder.Text);
            }
        }

        public void ParseTrackName(Track track, string trackName)
        {
            if (track == null)
                return;

            if (string.IsNullOrEmpty(trackName))
            {
                preferredInstruments[track] = Instrument.Piano;
                preferredOctaveShift[track] = 0;
            }
            else
            {
                var rex = new Regex(@"^([A-Za-z]+)([-+]\d)?");
                var match = rex.Match(trackName);

                var instrument = match.Groups[1].Value;
                var octaveshift = match.Groups[2].Value;

                var foundInstrument = false;

                if (!string.IsNullOrEmpty(instrument))
                    if (Instrument.TryParse(instrument, out var tempInst))
                    {
                        preferredInstruments[track] = tempInst;
                        foundInstrument = true;
                    }

                if (!foundInstrument)
                    return;

                if (string.IsNullOrEmpty(octaveshift))
                    return;

                if (!int.TryParse(octaveshift, out var os))
                    return;

                if (Math.Abs(os) <= 4) 
                    preferredOctaveShift[track] = os;
            }
        }

        public Instrument GetTrackPreferredInstrument(int tracknumber)
        {
            return tracknumber >= preferredInstruments.Count
                ? Instrument.None
                : preferredInstruments.ElementAt(tracknumber).Value;
        }

        public Instrument GetTrackPreferredInstrument(Track track)
        {
            if (track == null) 
                return Instrument.Piano;

            return preferredInstruments.ContainsKey(track) ? preferredInstruments[track] : Instrument.Piano;
        }

        public int GetTrackPreferredOctaveShift(Track track)
        {
            if (track == null) 
                return 0;

            return preferredOctaveShift.ContainsKey(track) ? preferredOctaveShift[track] : 0;
        }

        public void Load(BmpSong bmpSong, int trackNum = 1)
        {
            if (bmpSong == null)
                return;

            loadedFileType = FILETYPES.BmpSong;
            loadedBmpSong = bmpSong;
            Sequence = new Sequence(bmpSong.GetSequencerMidi());
            load(Sequence, trackNum);
        }

        public void load(Sequence sequence, int trackNum = 1)
        {
            OnTrackNameChange?.Invoke(this, string.Empty);
            OnTempoChange?.Invoke(this, 0);

            loadedError = string.Empty;
            if (trackNum >= Sequence.Count) trackNum = Sequence.Count - 1;

            intendedTrack = trackNum;

            preferredInstruments.Clear();
            preferredOctaveShift.Clear();

            // Collect statistics
            notesPlayedCount.Clear();
            foreach (var track in Sequence)
            {
                notesPlayedCount[track] = 0;
                foreach (var ev in track.Iterator().Where(static ev =>
                             ev.MidiMessage is ChannelMessage { Command: ChannelCommand.NoteOn, Data2: > 0 }))
                    notesPlayedCount[track]++;
            }

            // Count notes and select fìrst that actually has stuff
            if (trackNum == 1)
            {
                while (trackNum < Sequence.Count)
                {
                    var tnotes = 0;

                    foreach (var ev in Sequence[trackNum].Iterator().Where(ev => intendedTrack == 1))
                        switch (ev.MidiMessage)
                        {
                            case ChannelMessage { Command: ChannelCommand.NoteOn }:
                            case MetaMessage { MetaType: MetaType.Lyric }:
                                tnotes++;
                                break;
                        }

                    if (tnotes == 0)
                        trackNum++;
                    else
                        break;
                }

                if (trackNum == Sequence.Count)
                {
                    Console.WriteLine("No playable track...");
                    trackNum = intendedTrack;
                }
            }

            // Show initial tempo
            foreach (var ev in Sequence[0].Iterator().Where(static ev => ev.AbsoluteTicks == 0))
                if (ev.MidiMessage is MetaMessage { MetaType: MetaType.Tempo } metaMsg)
                    OnMetaMessagePlayed(this, new MetaMessageEventArgs(Sequence[0], metaMsg));

            // Parse track names and octave shifts
            _maxTracks = -1;
            _lyricStartTrackIndex = -1;
            foreach (var track in Sequence)
                foreach (var ev in track.Iterator())
                    if (ev.MidiMessage is MetaMessage { MetaType: MetaType.TrackName } metaMsg)
                    {
                        var builder = new MetaTextBuilder(metaMsg);
                        if (builder.Text.ToLower().Contains("lyrics:") && _lyricStartTrackIndex == -1)
                        {
                            _lyricStartTrackIndex = _maxTracks + 1;
                        }
                        else
                        {
                            ParseTrackName(track, builder.Text);
                            _maxTracks++;
                        }
                    }


            loadedTrack = trackNum;
            // Search beginning for text stuff
            foreach (var ev in LoadedTrack.Iterator())
                switch (ev.MidiMessage)
                {
                    case MetaMessage { MetaType: MetaType.TrackName } msg:
                        OnMetaMessagePlayed(this, new MetaMessageEventArgs(LoadedTrack, msg));
                        break;
                    /*if (msg.MetaType == MetaType.Lyric)
                    {
                        lyricCount++;
                    }*/
                    case ChannelMessage { Command: ChannelCommand.ProgramChange } chanMsg:
                        OnSimpleChannelMessagePlayed(this, new ChannelMessageEventArgs(Sequence[0], chanMsg));
                        break;
                }

            OnLoad?.Invoke(this, EventArgs.Empty);
        }
    }
}
