using BardMusicPlayer.Jamboree;
using BardMusicPlayer.Jamboree.Events;
using BardMusicPlayer.Maestro;
using BardMusicPlayer.Maestro.Events;
using BardMusicPlayer.Transmogrify.Song;
using BardMusicPlayer.Transmogrify.Song.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BardMusicPlayer.Ui.Functions.Network
{
    /// <summary>
    /// A collection of functions and wrapper for Networkplay
    /// </summary>
    public class NetworkFunctions : IDisposable
    {
        public EventHandler<bool> OnConnectionChanged;
        public EventHandler<bool> OnUpdateMembers;

        private List<NetworkPerformer> _networkPerformers = new();
        private bool _connected { get; set; } = false;

        public List<NetworkPerformer> GetPerformers() {  return _networkPerformers; }
        public bool IsConnected() { return _connected; }
        public string GetCode()
        {
            if (!_connected)
                return "";

            return BmpJamboree.Instance.GetCode();
        }

        public NetworkFunctions()
        {
            _networkPerformers.Clear();

            BmpJamboree.Instance.OnPartyCreated += Instance_PartyCreated;
            BmpJamboree.Instance.OnPartyJoined += Instance_PartyJoined;
            BmpJamboree.Instance.OnPartyChanged += Instance_PartyChanged;

            BmpJamboree.Instance.OnPlaylistChangedEvent += Instance_PlaylistChanged;
            BmpJamboree.Instance.OnPartySelectSong += Instance_PartySelectSong;

            BmpMaestro.Instance.OnSongLoaded += Instance_SongLoaded;
        }

        public void Dispose()
        {
            _networkPerformers.Clear();
        }

        #region Jamboree
        /// <summary>
        /// Create the party
        /// </summary>
        public void CreateParty() => BmpJamboree.Instance.CreateParty();

        /// <summary>
        /// Join the party
        /// </summary>
        public void JoinParty(string code)
        {
            List<KeyValuePair<string, string>> names = new List<KeyValuePair<string, string>>();
            var performers = BmpMaestro.Instance.GetAllPerformers();
            foreach (var performer  in performers)
                names.Add(new KeyValuePair<string, string>(performer.PlayerName, performer.HomeWorld));
            BmpJamboree.Instance.JoinParty(code, names);
        }

        /// <summary>
        /// Leave the party
        /// </summary>
        public void LeaveParty()
        {
            BmpJamboree.Instance.LeaveParty();
            _connected = false;
            _networkPerformers.Clear();
            OnConnectionChanged?.Invoke(this, _connected);
            OnUpdateMembers?.Invoke(this, true);
        }

        /// <summary>
        /// Set the tracknumber for Networkperformer / updated by Instance_PartyChanged
        /// </summary>
        /// <param name="trackNumber"></param>
        public static void SetTrackNumber(NetworkPerformer performer, int trackNumber)
        {
            if (trackNumber > PlaybackFunctions.CurrentSong.TrackContainers.Count())
                return;
            performer.TrackNumber = trackNumber;
            ClassicProcessorConfig classicConfig = (ClassicProcessorConfig)PlaybackFunctions.CurrentSong.TrackContainers[performer.TrackNumber - 1].ConfigContainers[0].ProcessorConfig;
            performer.TrackInstrument = classicConfig.Instrument.Name;
            BmpJamboree.Instance.SetTrack(performer.CharId(), performer.TrackNumber);
        }

        #endregion

        #region Callbackhandlers
        /// <summary>
        /// Triggered when a party was created, contains the party code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_PartyCreated(object sender, PartyCreatedEvent e)
        {
            if (!e.Connected)
                return;
            _networkPerformers.Clear();
            _connected = true;
            OnConnectionChanged?.Invoke(this, _connected);
        }

        /// <summary>
        /// Triggered when a party was joined
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Instance_PartyJoined(object sender, PartyJoinedEvent e)
        {
            _networkPerformers.Clear();
            _connected = true;
            OnConnectionChanged?.Invoke(this, _connected);
        }

        /// <summary>
        /// Triggered when the party was changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        private async void Instance_PartyChanged(object sender, PartyChangedEvent ev)
        {
            var current = BmpJamboree.Instance.GetCurrentPartyMembers();
            var serverCharIds = current.Select(c => c.charId).ToList();
            _networkPerformers.RemoveAll(c => !serverCharIds.Contains(c.CharId()));

            foreach (var member in current)
            {
                var matchingPerformer = _networkPerformers.FirstOrDefault(p => p.CharId() == member.charId);
                if (matchingPerformer != null)
                {
                    matchingPerformer.TrackInstrument = member.instrument;
                    matchingPerformer.TrackNumber = member.trackNumber ?? 1;
                }
                else
                    _networkPerformers.Add(new NetworkPerformer(member));
            }
            OnUpdateMembers?.Invoke(this, true);
        }

        /// <summary>
        /// Triggered if playlist has changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        private async void Instance_PlaylistChanged(object sender, PartyPlaylistChangeEvent ev)
        {
            //host only
            if (!BmpJamboree.Instance.IsHost())
                return;

            var song = ev.Playlist.FirstOrDefault();
            if (song == null)
                return;
            await BmpJamboree.Instance.SetSong(song.itemId);
        }

        /// <summary>
        /// The song selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ev"></param>
        public async void Instance_PartySelectSong(object sender, PartySelectSongEvent ev)
        {
            //host has the song
            if (BmpJamboree.Instance.IsHost())
                return;

            var songId = ev.SongId;
            if (songId == null)
                return;
            var data = BmpJamboree.Instance.GetMidiData(songId);
            if (data.Item1 == "" || data.Item2 == null)
                return;
            var song = BmpSong.ImportMidiFromByte(data.Item2, data.Item1).Result;
            PlaybackFunctions.LoadSongFromPlaylist(song);
        }

        /// <summary>
        /// Songloaded by maestro
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Instance_SongLoaded(object sender, SongLoadedEvent e)
        {
            if (BmpJamboree.Instance.IsHost())
            {
                var song = PlaybackFunctions.CurrentSong.GetExportMidi();
                BmpJamboree.Instance.SendSong(PlaybackFunctions.CurrentSong.Title, song.ToArray());
            }

            foreach (var netPerformer in _networkPerformers)
            {
                if (netPerformer.TrackNumber <= 0)
                    netPerformer.TrackNumber = 1;
                ClassicProcessorConfig classicConfig = (ClassicProcessorConfig)PlaybackFunctions.CurrentSong.TrackContainers[netPerformer.TrackNumber - 1].ConfigContainers[0].ProcessorConfig;
                BmpJamboree.Instance.SetInstrument(netPerformer.CharId(), classicConfig.Instrument.Name);
            }
        }
        #endregion
    }
}
