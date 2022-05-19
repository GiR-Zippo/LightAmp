using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BardMusicPlayer.Maestro.Events;
using BardMusicPlayer.Maestro.Performance;
using BardMusicPlayer.Maestro.Sequencing;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Seer;
using BardMusicPlayer.Transmogrify.Song;

namespace BardMusicPlayer.Maestro
{
    /// <summary>
    /// The brain of the operation;
    /// Automatically add the found games
    /// - creates the perfomers
    /// - creates the sequencers
    /// - load songs
    /// - manages play functions
    /// </summary>
    public class Orchestrator : IDisposable
    {
        private Sequencer sequencer { get; set; } = null;
        private CancellationTokenSource _updaterTokenSource;
        private bool LocalOchestraInitialized { get; set;} = false;
        public int HostPid { get; set; } = 0;

        public Game HostGame { get; set; } = null;
        private List<KeyValuePair<int, Performer>> performer { get; set; } = null;

        private Dictionary<Game, bool> _foundGames { get; set; } = null;
        private System.Timers.Timer _addPushedbackGamesTimer = null!;

        /// <summary>
        /// The constructor
        /// </summary>
        public Orchestrator()
        {
            performer = new List<KeyValuePair<int, Performer>>();
            _foundGames = new Dictionary<Game, bool>();
            sequencer = new Sequencer();
            BmpSeer.Instance.GameStarted += e => Instance_OnGameStarted(e.Game);
            BmpSeer.Instance.GameStopped += Instance_OnGameStopped;
            BmpSeer.Instance.EnsembleRequested += Instance_EnsembleRequested;

            _addPushedbackGamesTimer = new System.Timers.Timer();
            _addPushedbackGamesTimer.Interval = 4000;
            _addPushedbackGamesTimer.Enabled = false;
            _addPushedbackGamesTimer.Elapsed += CheckFoundGames;
        }

        #region public
        /// <summary>
        /// Gets all games
        /// </summary>
        public IEnumerable<Game> GetAllGames()
        {
            List<Game> games =  new List<Game>();
            foreach (KeyValuePair<int, Performer> performer in performer)
                games.Add(performer.Value.game);
            return games;
        }

        /// <summary>
        /// Gets all performers
        /// </summary>
        public IEnumerable<Performer> GetAllPerformers()
        {
            List<Performer> games = new List<Performer>();
            foreach (KeyValuePair<int, Performer> performer in performer)
                games.Add(performer.Value);
            return games;
        }

        /// <summary>
        /// Get the host bard track number
        /// </summary>
        /// <returns>tracknumber</returns>
        public int GetHostBardTrack()
        {
            Performer perf = performer.Where(perf => perf.Value.HostProcess).FirstOrDefault().Value;
            return perf == null ? 1 : perf.TrackNumber;
        }

        /// <summary>
        /// loads a BMPSong from the database
        /// </summary>
        /// <param name="song"></param>
        public void LoadBMPSong(BmpSong song)
        {
            if (!BmpPigeonhole.Instance.LocalOrchestra)
                LocalOchestraInitialized = false;

            sequencer.Load(song);

            foreach (var perf in performer)
            {
                perf.Value.Sequencer = sequencer;           //use the sequence from the main sequencer
                perf.Value.Sequencer.LoadedBmpSong = song;  //set the song
            }
            InitNewPerformance();
        }

        /// <summary>
        /// sets the octaveshift for host performer (used for Ui)
        /// </summary>
        /// <param name="octave"></param>
        public void SetOctaveshiftOnHost(int octave)
        {
            foreach (var perf in performer)
            {
                if (perf.Value.HostProcess)
                {
                    perf.Value.OctaveShift = octave;
                    BmpMaestro.Instance.PublishEvent(new OctaveShiftChangedEvent(perf.Value.game, octave, perf.Value.HostProcess));
                    return;
                }
            }
        }

        /// <summary>
        /// sets the octaveshift for host performer (used for Ui)
        /// </summary>
        /// <param name="performer"></param>
        /// <param name="octave"></param>
        public void SetOctaveshift(Performer p, int octave)
        {
            if (p == null)
                return;
            p.OctaveShift = octave;
            BmpMaestro.Instance.PublishEvent(new OctaveShiftChangedEvent(p.game, octave, p.HostProcess));
        }

        /// <summary>
        /// sets the track for all performer
        /// </summary>
        /// <param name="tracknumber"></param>
        public void SetTracknumber(int tracknumber)
        {
            foreach (var perf in performer)
                perf.Value.TrackNumber = tracknumber;
            BmpMaestro.Instance.PublishEvent(new TrackNumberChangedEvent(null, tracknumber));
        }

        /// <summary>
        /// sets the track for specific performer
        /// </summary>
        /// <param name="game"></param>
        /// <param name="tracknumber"></param>
        public void SetTracknumber(Game game, int tracknumber)
        {
            foreach (var perf in performer)
            {
                if (perf.Value.game.Pid == game.Pid)
                {
                    perf.Value.TrackNumber = tracknumber;
                    BmpMaestro.Instance.PublishEvent(new TrackNumberChangedEvent(perf.Value.game, tracknumber, perf.Value.HostProcess));
                }
            }
        }

        /// <summary>
        /// sets the track for specific performer
        /// </summary>
        /// <param name="performer"></param>
        /// <param name="tracknumber"></param>
        public void SetTracknumber(Performer perf, int tracknumber)
        {
            if (perf == null)
                return;

            perf.TrackNumber = tracknumber;
            BmpMaestro.Instance.PublishEvent(new TrackNumberChangedEvent(perf.game, tracknumber, perf.HostProcess));
        }

        /// <summary>
        /// sets the track for host performer (used for Ui)
        /// </summary>
        /// <param name="game"></param>
        /// <param name="tracknumber"></param>
        public void SetTracknumberOnHost(int tracknumber)
        {
            foreach (var perf in performer)
            {
                if (perf.Value.HostProcess)
                {
                    perf.Value.TrackNumber = tracknumber;
                    BmpMaestro.Instance.PublishEvent(new TrackNumberChangedEvent(perf.Value.game, tracknumber, true));
                    return;
                }
            }
        }

        /// <summary>
        /// Sets the host game
        /// </summary>
        /// <param name="game"></param>
        public void SetHostBard(Game game)
        {
            if (game == null)
                return;
            foreach (var perf in performer)
                if (perf.Value.PId == game.Pid)
                {
                    perf.Value.HostProcess = true;
                    HostPid = game.Pid;
                    HostGame = game;
                }
                else
                    perf.Value.HostProcess = false;
        }

        /// <summary>
        /// Sets the host game
        /// </summary>
        /// <param name="p"></param>
        public void SetHostBard(Performer p)
        {
            if (p == null)
                return;
            foreach (var perf in performer)
                if (perf.Value.PId == p.PId)
                {
                    perf.Value.HostProcess = true;
                    HostPid = p.PId;
                    HostGame = p.game;
                }
                else
                    perf.Value.HostProcess = false;
        }

        /// <summary>
        /// Set the MidiInput for the first performer
        /// </summary>
        /// <param name="device"></param>
        public void OpenInputDevice(int device)
        {
            foreach (var perf in performer)
            {
                if (perf.Value.HostProcess)
                {
                    perf.Value.Sequencer.CloseInputDevice();
                    perf.Value.Sequencer.OpenInputDevice(device);
                }
            }
        }

        /// <summary>
        /// Close the MidiInput for the first performer
        /// </summary>
        public void CloseInputDevice()
        {
            foreach (var perf in performer)
            {
                if (perf.Value.HostProcess)
                    perf.Value.Sequencer.CloseInputDevice();
            }
        }

        /// <summary>
        /// Seeks the song to absolute position
        /// </summary>
        /// <param name="ticks"></param>
        public void Seek(int ticks)
        {
            foreach (var perf in performer)
                perf.Value.Sequencer.Seek(ticks);
        }

        /// <summary>
        /// Seeks the song to absolute position
        /// </summary>
        /// <param name="miliseconds"></param>
        public void Seek(double miliseconds)
        {
            foreach (var perf in performer)
                perf.Value.Sequencer.Seek(miliseconds);
        }

        /// <summary>
        /// Starts the playback
        /// </summary>
        public void Start()
        {
            if (performer.Count() == 0)
                return;

            //if we are a not a local orchestra
            if (!BmpPigeonhole.Instance.LocalOrchestra)
            {
                var res = performer.Find(i => i.Value.HostProcess == true);
                res.Value.Play(true);
            }

            foreach (var perf in performer)
                perf.Value.Play(true);
        }

        /// <summary>
        /// Pause the playback
        /// </summary>
        public void Pause()
        {
            if (performer.Count() == 0)
                return;

            //if we are a not a local orchestra
            if (!BmpPigeonhole.Instance.LocalOrchestra)
            {
                var res = performer.AsParallel().Where(i => i.Value.HostProcess == true);
                res.First().Value.Play(false);
            }

            foreach (var perf in performer)
                perf.Value.Play(false);
        }

        /// <summary>
        /// Stops the playback
        /// </summary>
        public void Stop()
        {
            if (performer.Count() == 0)
                return;

            //if we are a not a local orchestra
            if (!BmpPigeonhole.Instance.LocalOrchestra)
            {
                var res = performer.AsParallel().Where(i => i.Value.HostProcess == true);
                res.First().Value.Stop();
            }

            foreach (var perf in performer)
                perf.Value.Stop();
        }

        /// <summary>
        /// Equip the bard with it's instrument
        /// </summary>
        public void EquipInstruments()
        {
            Thread.Sleep(100);  //Wait
            Parallel.ForEach(performer, perf =>
            {
                perf.Value.OpenInstrument();
            });
        }

        /// <summary>
        /// Remove the bards instrument
        /// </summary>
        public void UnEquipInstruments()
        {
            Thread.Sleep(100);
            Parallel.ForEach(performer, perf =>
            {
                perf.Value.CloseInstrument();
            });
        }

        /// <summary>
        /// Disposing
        /// </summary>
        public void Dispose()
        {
            if (sequencer != null)
                sequencer.Dispose();
            // Dispose managed resources.
            if (_updaterTokenSource != null)
                _updaterTokenSource.Cancel();

            foreach (var perf in performer)
                perf.Value.Close();

            GC.SuppressFinalize(this);
        }
        #endregion

        #region private

        /// <summary>
        /// Called if a game was found
        /// </summary>
        /// <param name="game">the found game</param>
        private void Instance_OnGameStarted(Game game)
        {
            if (BmpSeer.Instance.Games.Count == 1)
                AddPerformer(game, true);
            else
                AddPerformer(game, false);
        }

        /// <summary>
        /// Called when a game was stopped
        /// </summary>
        /// <param name="g"></param>
        private void Instance_OnGameStopped(Seer.Events.GameStopped g)
        {
            RemovePerformer(g.Pid);
        }

        /// <summary>
        /// Called if a enseble request started
        /// </summary>
        /// <param name="seerEvent"></param>
        private void Instance_EnsembleRequested(Seer.Events.EnsembleRequested seerEvent)
        {
            //If we don't have alocal ochestra enabled get outa here
            if (!BmpPigeonhole.Instance.LocalOrchestra)
                return;

            _ = EnsembleAcceptAsync(seerEvent);
        }

        private async Task<int> EnsembleAcceptAsync(Seer.Events.EnsembleRequested seerEvent)
        {
            await Task.Delay(BmpPigeonhole.Instance.EnsebleReadyDelay);
            foreach (var i in performer)
                if (i.Value.game.Pid == seerEvent.Game.Pid)
                    i.Value.EnsembleAccept();
            return 0;
        }

        /// <summary>
        /// Creates the performer. Is waiting till the game is ready for access
        /// </summary>
        /// <param name="game">the game</param>
        /// <param name="IsHost">is it the host game</param>
        /// <returns></returns>
        private void AddPerformer(Game game, bool IsHost)
        {
            var result = performer.Find(kvp => kvp.Key == game.Pid);
            if (result.Key == game.Pid)
                return;
 
            lock(_foundGames)
            { 
                if (!_foundGames.ContainsKey(game))
                    _foundGames.Add(game, IsHost); 
            }
            _addPushedbackGamesTimer.Enabled = true;
        }

        /// <summary>
        /// check if the ConfigId is kown and add the performer. Triggered by the Timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckFoundGames(object sender, System.Timers.ElapsedEventArgs e)
        {
            List<Game> added = new List<Game>();
            lock (_foundGames)
            {
                foreach (var game in _foundGames)
                {
                    if (game.Key.ConfigId.Length > 0)
                    {
                        //Bard is loaded and prepared
                        Performer perf = new Performer(game.Key);
                        perf.HostProcess = game.Value;
                        perf.Sequencer = sequencer;
                        perf.TrackNumber = 1;
                        lock (performer)
                        {
                            performer.Add(new KeyValuePair<int, Performer>(game.Key.Pid, perf));    //Add the performer
                        }
                        BmpMaestro.Instance.PublishEvent(new PerformersChangedEvent());     //And trigger an event
                        if (game.Value)
                        {
                            HostPid = game.Key.Pid;
                            HostGame = game.Key;
                        }
                        added.Add(game.Key);
                    }
                }

                foreach (Game g in added)
                    _foundGames.Remove(g);

                if (_foundGames.Count() <=0)
                    _addPushedbackGamesTimer.Enabled = false;
            }
        }

        /// <summary>
        /// Removes a performer
        /// </summary>
        /// <param name="Pid"></param>
        private void RemovePerformer(int Pid)
        {
            var result = performer.Find(i => i.Key == Pid);
            if (result.Value == null)
                return;

            lock (performer)
            {
                performer.Remove(result);
            }
            result.Value.Close();
            BmpMaestro.Instance.PublishEvent(new PerformersChangedEvent());     //trigger the event
        }

        /// <summary>
        /// Sets all events and starts the updater
        /// Called at every new song
        /// </summary>
        private void InitNewPerformance()
        {
            if (_updaterTokenSource != null)
                if(!_updaterTokenSource.IsCancellationRequested)
                    _updaterTokenSource.Cancel();

            //if we have a local orchestra, spread the tracknumbers across the performers
            if ((!LocalOchestraInitialized) && BmpPigeonhole.Instance.LocalOrchestra)
            {
                int index = 1;
                foreach (var p in performer)
                {
                    p.Value.TrackNumber = index;
                    if (index == sequencer.MaxTrack)
                        index = 1;
                    else
                        index++;
                }
                LocalOchestraInitialized = true;
            }

            BmpMaestro.Instance.PublishEvent(new MaxPlayTimeEvent(sequencer.MaxTimeAsTimeSpan, sequencer.MaxTick));
            BmpSeer.Instance.InstrumentHeldChanged += delegate (Seer.Events.InstrumentHeldChanged e) 
            {
                Instance_InstrumentHeldChanged(e);
            };
            BmpMaestro.Instance.PublishEvent(new SongLoadedEvent(sequencer.MaxTrack, sequencer));

            Performer perf = performer.Where(perf => perf.Value.HostProcess).FirstOrDefault().Value;
            if (perf != null)
                perf.Sequencer.PlayEnded += Sequencer_PlayEnded;

            _updaterTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() => Updater(_updaterTokenSource.Token), TaskCreationOptions.LongRunning);
        }

        private void Sequencer_PlayEnded(object sender, EventArgs e)
        {
            BmpMaestro.Instance.PublishEvent(new PlaybackStoppedEvent());
        }

        /// <summary>
        /// Seer event for stopping the bards performance
        /// </summary>
        private void Instance_InstrumentHeldChanged(Seer.Events.InstrumentHeldChanged seerEvent)
        {
            Game game = seerEvent.Game;
            foreach (var perf in performer)
            {
                if (perf.Value.game.Equals(game))
                {
                    if (game.InstrumentHeld.Equals(Instrument.None))
                        perf.Value.Stop();
                    perf.Value.PerformerEnabled = !game.InstrumentHeld.Equals(Instrument.None);
                }
            }
        }

        /// <summary>
        /// the updater
        /// </summary>
        /// <param name="token"></param>
        private async Task Updater(CancellationToken token)
        {
            Performer perf = performer.Where(perf => perf.Value.HostProcess).FirstOrDefault().Value;
            while (!token.IsCancellationRequested)
            {
                //Get host performer
                if (perf == null)
                    perf = performer.Find(perf => perf.Value.HostProcess).Value;
                else
                    BmpMaestro.Instance.PublishEvent(new CurrentPlayPositionEvent(perf.Sequencer.CurrentTimeAsTimeSpan, perf.Sequencer.CurrentTick));

                await Task.Delay(200, token).ContinueWith(tsk => { });
            }
            return;
        }
        #endregion
    }
}
