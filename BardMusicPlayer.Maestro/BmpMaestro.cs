﻿/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Transmogrify.Song;

namespace BardMusicPlayer.Maestro
{
    public partial class BmpMaestro : IDisposable
    {
        private static readonly Lazy<BmpMaestro> LazyInstance = new(() => new BmpMaestro());

        private Orchestrator _orchestrator;
        /// <summary>
        /// 
        /// </summary>
        public bool Started { get; private set; }

        private BmpMaestro()
        {
            //Create the orchestrator
            _orchestrator = new Orchestrator();
        }

        public static BmpMaestro Instance => LazyInstance.Value;

        #region Getters
        /// <summary>
        /// Get all performers the orchestrator has created
        /// </summary>
        /// <returns><see cref="IEnumerable{Performer}"/></returns>
        public IEnumerable<Performer> GetAllPerformers()
        {
            if (_orchestrator != null)
                return _orchestrator.GetAllPerformers();
            return new List<Performer>();
        }

        /// <summary>
        /// Get the host bard track number
        /// </summary>
        /// <returns><see cref="int"/> tracknumber</returns>
        public int GetHostBardTrack()
        {
            if (_orchestrator != null)
                return _orchestrator.GetHostBardTrack();
            return 1;
        }

        /// <summary>
        /// Get host bard Pid
        /// </summary>
        /// <returns><see cref="int"/> ProcessId</returns>
        public int GetHostPid()
        {
            if (_orchestrator != null)
                return _orchestrator.HostPid;
            return -1;
        }

        /// <summary>
        /// Sets the song title parsing bard
        /// </summary>
        /// <param name="performer"></param>
        public KeyValuePair<TitleParsingHelper, Performer> GetSongTitleParsingBard()
        {
            if (_orchestrator != null)
                return _orchestrator.GetSongTitleParsingBard();
            return new KeyValuePair<TitleParsingHelper, Performer>(new TitleParsingHelper{}, null);
        }
        #endregion

        #region Setters
        /// <summary>
        /// Sets the host bard
        /// </summary>
        /// <param name="game"></param>
        public void SetHostBard(Game game)
        {
            if (_orchestrator != null)
                _orchestrator.SetHostBard(game);
        }

        /// <summary>
        /// Sets the host bard
        /// </summary>
        /// <param name="performer"></param>
        public void SetHostBard(Performer performer)
        {
            if (_orchestrator != null)
                _orchestrator.SetHostBard(performer);
        }

        /// <summary>
        /// Sets the song title parsing bard and the prefix like /yell
        /// </summary>
        /// <param name="performer"></param>
        public void SetSongTitleParsingBard(ChatMessageChannelType channel, string prefix, Performer performer)
        {
            if (_orchestrator != null)
                _orchestrator.SetSongTitleParsingBard(channel, prefix, performer);
        }

        /// <summary>
        /// sets the octave shift for performer
        /// </summary>
        /// <param name="octave"></param>
        public void SetOctaveshift(Performer p, int octave)
        {
            if (_orchestrator != null)
                _orchestrator.SetOctaveshift(p, octave);
        }

        /// <summary>
        /// sets the octave shift for host performer
        /// </summary>
        /// <param name="octave"></param>
        public void SetOctaveshiftOnHost(int octave)
        {
            if (_orchestrator != null)
                _orchestrator.SetOctaveshiftOnHost(octave);
        }

        /// <summary>
        /// sets the speed for bard
        /// </summary>
        public void SetSpeedShift(Performer p, float percentage)
        {
            if (_orchestrator != null)
                _orchestrator.SetSpeedshift(p, percentage);
        }

        /// <summary>
        /// sets the speed for bard
        /// </summary>
        public void SetSpeedShiftAll(float percentage)
        {
            if (_orchestrator == null)
                return;

            var performers = _orchestrator.GetAllPerformers();
            Parallel.ForEach(performers, perf =>
            {
                _orchestrator.SetSpeedshift(perf, percentage);
            });
        }

        /// <summary>
        /// sets the speed for host performer
        /// </summary>
        public void SetSpeedShiftOnHost(float percentage)
        {
            if (_orchestrator != null)
                _orchestrator.SetSpeedshiftOnHost(percentage);
        }

        /// <summary>
        /// Sets the playback at position (timeindex in ticks)
        /// </summary>
        /// <param name="ticks">time ticks</param>
        public void SetPlaybackStart(int ticks)
        {
            if (_orchestrator != null)
                _orchestrator.Seek(ticks);
        }

        /// <summary>
        /// Sets the playback at position (timeindex in miliseconds)
        /// </summary>
        /// <param double="miliseconds"></param>
        public void SetPlaybackStart(double miliseconds)
        {
            if (_orchestrator != null)
                _orchestrator.Seek(miliseconds);
        }

        /// <summary>
        /// Sets a new song for the sequencer
        /// </summary>
        /// <param name="bmpSong"></param>
        public void SetSong(BmpSong bmpSong)
        {
            if (_orchestrator != null)
            {
                _orchestrator.Stop();
                _orchestrator.LoadBMPSong(bmpSong);
            }
        }

        /// <summary>
        /// Change the tracknumber; 0 all tracks
        /// </summary>
        /// <param name="performer">the bard</param>
        /// <param name="tracknumber"></param>
        public void SetTracknumber(Performer p, int tracknumber)
        {
            if (_orchestrator != null)
                _orchestrator.SetTracknumber(p, tracknumber);
        }

        /// <summary>
        /// Set the tracknumber 0 all tracks
        /// </summary>
        /// <param name="game">the bard</param>
        /// <param name="tracknumber">track</param>
        public void SetTracknumber(Game game, int tracknumber)
        {
            if (_orchestrator != null)
                _orchestrator.SetTracknumber(game, tracknumber);
        }

        /// <summary>
        /// sets the track for host performer
        /// </summary>
        /// <param name="tracknumber"></param>
        public void SetTracknumberOnHost(int tracknumber)
        {
            if (_orchestrator != null)
                _orchestrator.SetTracknumberOnHost(tracknumber);
        }
        #endregion

        #region MidiInput
        /// <summary>
        /// Opens a MidiInput device
        /// </summary>
        /// <param int="device"></param>
        public void OpenInputDevice(int device)
        {
            if (_orchestrator == null)
                _orchestrator = new Orchestrator();
            _orchestrator.OpenInputDevice(device);
        }


        /// <summary>
        /// close the MidiInput device
        /// </summary>
        public void CloseInputDevice()
        {
            if (_orchestrator == null)
                _orchestrator = new Orchestrator();
            _orchestrator.CloseInputDevice();
        }
        #endregion

        #region Playback
        /// <summary>
        /// Starts the performance
        /// </summary>
        /// <param name="delay">delay in ms</param>
        public void StartLocalPerformer(int delay)
        {
            if (_orchestrator != null)
                _orchestrator.Start(delay);
        }

        /// <summary>
        /// Pause the song playback
        /// </summary>
        public void PauseLocalPerformer()
        {
            if (_orchestrator != null)
                _orchestrator.Pause();
        }

        /// <summary>
        /// Stops the song playback
        /// </summary>
        public void StopLocalPerformer()
        {
            if (_orchestrator != null)
                _orchestrator.Stop();
        }

        /// <summary>
        /// Start the ensemble check
        /// </summary>
        public void StartEnsCheck()
        {
            if (_orchestrator == null)
                return;
            
            var perf = _orchestrator.GetAllPerformers();
            foreach (var p in perf)
                if (p.HostProcess)
                    p.DoReadyCheck();
        }

        /// <summary>
        /// Equip the bard with it's instrument
        /// </summary>
        public void EquipInstruments()
        {
            if (_orchestrator != null)
                _orchestrator.EquipInstruments();
        }

        /// <summary>
        /// Remove the bards instrument
        /// </summary>
        public void UnEquipInstruments()
        {
            if (_orchestrator != null)
                _orchestrator.UnEquipInstruments();
        }
        #endregion

        #region Routines for Scripting
        /// <summary>
        /// Send a chat text; "All" or specific bard name
        /// </summary>
        public void SendText(string BardName, ChatMessageChannelType type, string text, List<string> unselected_bards = null)
        {
            if (_orchestrator == null)
                return;

            if (BardName == "")
                return;

            var perf = _orchestrator.GetAllPerformers();
            if (BardName.ToLower().Equals("all"))
            {
                Parallel.ForEach(perf, p =>
                {
                    if (unselected_bards == null || unselected_bards.Count() <= 0)
                        p.SendText(type, text);
                    else
                    {
                        if (!unselected_bards.Contains(p.game.PlayerName))
                            p.SendText(type, text);
                    }
                });
            }
            else
            {
                try
                {
                    Performer performer = perf.AsParallel().Where(p => p.game.PlayerName.Equals(BardName)).First();
                    performer.SendText(type, text);
                }
                catch {}
            }
        }

        /// <summary>
        /// Tap a key
        /// </summary>
        /// <param name="BardName"></param>
        /// <param name="modifier"></param>
        /// <param name="character"></param>
        /// <param name="unselected_bards"></param>
        public void TapKey(string BardName, string modifier, string character, List<string> unselected_bards = null)
        {
            if (_orchestrator == null)
                return;

            if (BardName == "")
                return;

            var perf = _orchestrator.GetAllPerformers();
            if (BardName.ToLower().Equals("all"))
            {
                Parallel.ForEach(perf, p =>
                {
                    if (unselected_bards == null || unselected_bards.Count() <= 0)
                        p.TapKey(modifier, character);
                    else
                    {
                        if (!unselected_bards.Contains(p.game.PlayerName))
                            p.TapKey(modifier, character);
                    }
                });
            }
            else
            {
                try
                {
                    Performer performer = perf.AsParallel().Where(p => p.game.PlayerName.Equals(BardName)).First();
                    performer.TapKey(modifier, character);
                }
                catch { }
            }
        }
        #endregion

        /// <summary>
        /// Start the eventhandler
        /// </summary>
        public void Start()
        {
            if (Started) return;
            StartEventsHandler();
            Started = true;
        }

        /// <summary>
        /// Stop the eventhandler
        /// </summary>
        public void Stop()
        {
            if (!Started) return;
            StopEventsHandler();
            Started = false;
            Dispose();
        }

        ~BmpMaestro() { Dispose(); }

        public void Dispose()
        {
            Stop();
            _orchestrator.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}