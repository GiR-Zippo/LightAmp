/*
 * Copyright(c) 2021 MoogleTroupe, trotlinebeercan
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Transmogrify.Song;

namespace BardMusicPlayer.Maestro
{
    public partial class BmpMaestro : IDisposable
    {
        private static readonly Lazy<BmpMaestro> LazyInstance = new(() => new BmpMaestro());

        public Game SelectedBard { get; set; }

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
        /// Get all game the orchestrator is accessing
        /// </summary>
        public IEnumerable<Game> GetAllGames()
        {
            return _orchestrator.GetAllGames();
        }

        /// <summary>
        /// Get all performers the orchestrator has created
        /// </summary>
        public IEnumerable<Performer> GetAllPerformers()
        {
            return _orchestrator.GetAllPerformers();
        }

        /// <summary>
        /// Get the host bard track number
        /// </summary>
        /// <returns>tracknumber</returns>
        public int GetHostBardTrack()
        {
            return _orchestrator.GetHostBardTrack();
        }

        /// <summary>
        /// Get host bard Pid
        /// </summary>
        /// <returns>Pid</returns>
        public Game GetHostGame()
        {
            return _orchestrator.HostGame;
        }

        /// <summary>
        /// Get host bard Pid
        /// </summary>
        /// <returns>Pid</returns>
        public int GetHostPid()
        {
            return _orchestrator.HostPid;
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
            _orchestrator.Stop();
            _orchestrator.LoadBMPSong(bmpSong);
        }

        /// <summary>
        /// Sets the song for the sequencer
        /// </summary>
        /// <param name="filename">midi file with full path</param>
        public void SetSong(string filename)
        {
           _orchestrator.Stop();
           _orchestrator.LoadMidiFile(filename);
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

        #region Playback
        /// <summary>
        /// Starts the playback
        /// </summary>
        public void StartLocalPerformer()
        {
            if (_orchestrator != null)
            {
                _orchestrator.Start();
            }
        }

        /// <summary>
        /// Pause the song playback
        /// </summary>
        public void PauseLocalPerformer()
        {
            if (_orchestrator != null)
            {
                _orchestrator.Pause();
            }
        }

        /// <summary>
        /// Stops the song playback
        /// </summary>
        public void StopLocalPerformer()
        {
            if (_orchestrator != null)
            {
                _orchestrator.Stop();
            }
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

        /// <summary>
        /// Destroys the sequencer
        /// </summary>
        public void DestroySongFromLocalPerformer()
        {
            if (_orchestrator != null)
                _orchestrator.Dispose();
        }

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