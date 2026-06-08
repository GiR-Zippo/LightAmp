/*
 * Copyright(c) 2026 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BardMusicPlayer.Jamboree
{
    public partial class BmpJamboree : IDisposable
    {

        #region Instance Constructor/Destructor
        private static readonly Lazy<BmpJamboree> LazyInstance = new(() => new BmpJamboree());
        private BMPApi _Api { get; set; } = null;

        /// <summary>
        /// 
        /// </summary>
        public bool Started { get; private set; }

        private BmpJamboree()
        {
            _Api = new BMPApi();
        }

        public static BmpJamboree Instance => LazyInstance.Value;

        /// <summary>
        /// Start the eventhandler
        /// </summary>
        /// <returns></returns>
        public void Start()
        {
            if (Started) return;
            StartEventsHandler();
            _Api.StartService();
            Started = true;
        }

        /// <summary>
        /// Stop the eventhandler
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            if (!Started) return;
            StopEventsHandler();
            _Api.StopService();
            Started = false;
        }

        ~BmpJamboree() { Dispose(); }

        public void Dispose()
        {
            _Api.Dispose();
            Stop();
            GC.SuppressFinalize(this);
        }
        #endregion

        /// <summary>
        /// Create a party
        /// </summary>
        public async void CreateParty() { await _Api.CreateSession(); }

        /// <summary>
        /// Join a party by code and playername
        /// </summary>
        /// <param name="code">Shared token</param>
        /// <param name="name">Player name</param>
        public async void JoinParty(string code, string name) { await _Api.JoinParty(code, name); }

        /// <summary>
        /// Leave the party and clean up
        /// </summary>
        public void LeaveParty() => _Api.LeaveParty();

        /// <summary>
        /// Sends a song as playlist
        /// </summary>
        /// <param name="files"></param>
        public async void SendPlaylist(string file)
        {
            List<string> files = new();
            files.Add(file);
            await _Api.SendPlaylist(files);
        }

        /// <summary>
        /// Send a list of songs as a playlist
        /// </summary>
        /// <param name="files"></param>
        public async void SendPlaylist(List<string> files) { await _Api.SendPlaylist(files); }

        /// <summary>
        /// Sets the song to play
        /// </summary>
        /// <param name="song"></param>
        public async Task SetSong(string song) => await _Api.SelectSong(song);

        /// <summary>
        /// Are we connected?
        /// </summary>
        /// <returns></returns>
        public bool IsConnected() => _Api.IsConnected();

        /// <summary>
        /// Get the memberlist
        /// </summary>
        /// <returns></returns>
        public List<SessionMembers> GetCurrentPartyMembers() => _Api.GetCurrentParty().GetMembers();

        /// <summary>
        /// Sets the Track for Member
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="trackNumber"></param>
        public void SetTrack(string memberId, int trackNumber) => _Api.SetTrackNumber(memberId, trackNumber);

        /// <summary>
        /// Sets the Instrument for Member
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="trackNumber"></param>
        public void SetInstrument(string memberId, string instrument) => _Api.SetInstrument(memberId, instrument);

        /// <summary>
        /// Get the MidiData bytes[]
        /// </summary>
        /// <param name="itemId"></param>
        public byte[] GetMidiData(string itemId) => _Api.GetMidiData(itemId);
    }
}
