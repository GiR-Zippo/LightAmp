/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree.Events;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Timers;

namespace BardMusicPlayer.Jamboree
{
    public partial class BMPApi : IDisposable
    {
        public BMPApi() {}

        public static readonly string ApiUrl = "https://bardmusicplayer.com/api/party/sessions";
        public static readonly string UserAgent = "XIVMIDI CLIENT V2 (LightAmp)";

        private HttpClient _HttpClient { get; set; } = null;
        private HttpClientHandler _HttpClientHandler { get; set; } = null;

        /// <summary>
        /// The timer for the heartbeat, only when we are client
        /// </summary>
        private Timer _Heartbeat { get; set; } = new Timer();

        private HeartbeatResponse _HeartbeatResponse { get; set; } = null;
        /// <summary>
        /// The hostData, if we are the host
        /// </summary>
        private SessionCreated _HostData { get; set; } = null;

        /// <summary>
        /// The client data, only if we are a client
        /// </summary>
        private MemberStateResponse _ClientData { get; set; } = null;

        /// <summary>
        /// Get the current session code
        /// </summary>
        private string GetCode() { return _ClientData == null ? _HostData.code : _ClientData.code; }

        /// <summary>
        /// The playlist we got
        /// </summary>
        private SessionManifest _SessionManifest { get; set; } = null;

        /// <summary>
        /// The party
        /// </summary>
        private Party _Party { get; set; } = null;

        /// <summary>
        /// The playlist
        /// </summary>
        private PartySongs _Playlist { get; set; } = null;

        /// <summary>
        /// Starts the http client
        /// </summary>
        public void StartService()
        {
            _HttpClientHandler = new HttpClientHandler
            {
                UseCookies = true,
                UseProxy = true,
                MaxAutomaticRedirections = 2,
                MaxConnectionsPerServer = 2
            };

            _HttpClient = new HttpClient(handler: _HttpClientHandler);
            _HttpClient.Timeout = TimeSpan.FromMinutes(5);
            _HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

            _Heartbeat.Stop();
            _Heartbeat.Elapsed += Timer_Elapsed;

            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
        }

        /// <summary>
        /// The heartbeat timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            GetSessionManifest().ConfigureAwait(true);
        }

        /// <summary>
        /// Stops and dispose the http stuff
        /// </summary>
        public void StopService()
        {
            if (IsConnected())
                LeaveParty();

            _Heartbeat.Elapsed -= Timer_Elapsed;
            _Heartbeat.Dispose();

            _HttpClient.Dispose();
            _HttpClientHandler.Dispose();
        }

        /// <summary>
        /// Leave the party and clean up
        /// </summary>
        public void LeaveParty()
        {
            _Heartbeat.Stop();

            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;

            _ClientData = null;
            _HostData = null;
            _SessionManifest = null;
            _Party.Dispose();
            _Party = null;
            _Playlist = null;
            BmpJamboree.Instance.PublishEvent(new PartyLogEvent("Party left..."));
        }

        #region Helper
        /// <summary>
        /// Are we connected?
        /// </summary>
        /// <returns></returns>
        public bool IsConnected() {  return _ClientData != null || _HostData != null;  }

        /// <summary>
        /// Gets the current party
        /// </summary>
        /// <returns></returns>
        public Party GetCurrentParty() {  return _Party; }

        /// <summary>
        /// Get the current playlist
        /// </summary>
        /// <returns></returns>
        public List<PlaylistItem> GetPlaylist() {return _SessionManifest.items; }

        /// <summary>
        /// Get the downloaded midi data
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public byte[] GetMidiData(string itemId) => _Playlist.GetMidiData(itemId);

        /// <summary>
        /// Set the track for member
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="track"></param>
        public async void SetTrackNumber(string memberId, int track)
        {
            var data = _Party.UpdateTrackForUser(memberId, track);
            await AssignMemberTo(memberId, data.trackNumber, data.instrument);
        }

        /// <summary>
        /// Set the instrument for member
        /// </summary>
        /// <param name="memberId"></param>
        /// <param name="track"></param>
        public async void SetInstrument(string memberId, string instrument)
        {
            var data = _Party.UpdateInstrumentForUser(memberId, instrument);
            await AssignMemberTo(memberId, data.trackNumber, data.instrument);
        }

        #endregion

        // Build POSIX-Ustar-Header (512 Bytes)
        private byte[] CreateTarHeader(string fileName, long fileSize)
        {
            byte[] header = new byte[512];

            byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
            int nameLength = Math.Min(nameBytes.Length, 99);
            Array.Copy(nameBytes, 0, header, 0, nameLength);

            byte[] modeBytes = Encoding.ASCII.GetBytes("0000644\0");
            Array.Copy(modeBytes, 0, header, 100, 8);

            string sizeString = Convert.ToString(fileSize, 8).PadLeft(11, '0') + " ";
            byte[] sizeBytes = Encoding.ASCII.GetBytes(sizeString);
            Array.Copy(sizeBytes, 0, header, 124, 12);

            header[156] = (byte)'0';

            byte[] magicBytes = Encoding.ASCII.GetBytes("ustar\0");
            Array.Copy(magicBytes, 0, header, 257, 6);

            header[263] = (byte)'0';
            header[264] = (byte)'0';

            byte[] spaces = Encoding.ASCII.GetBytes("        ");
            Array.Copy(spaces, 0, header, 148, 8);

            long checksum = 0;
            foreach (byte b in header) checksum += b;

            string checkStr = Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ";
            byte[] checkBytes = Encoding.ASCII.GetBytes(checkStr);
            Array.Copy(checkBytes, 0, header, 148, 8);

            return header;
        }

        /// <summary>
        /// Get Status response if 200 return true else false
        /// </summary>
        /// <param name="StatusCode"></param>
        public bool StatusResponse(int StatusCode)
        {
            switch (StatusCode)
            {
                case 200:
                    return true;
                case 400:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Required field missing or value rejected.\r\n"));
                    return false;
                case 403:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Authenticated but not allowed to perform this action.\r\n"));
                    return false;
                case 404:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("No record with that ID.\r\n"));
                    return false;
                case 409:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Party is full.\r\n"));
                    return false;
                case 429:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Too many requests from this client; retry after the Retry-After header.\r\n"));
                    return false;
                case 503:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Server at session capacity; retry later.\r\n"));
                    return false;
                default:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Sum ting wong.\r\n"));
                    return false;
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            _Heartbeat.Stop();
            _ClientData = null;
            _HostData = null;
            _SessionManifest = null;
            _Party.Dispose();
            _Party = null;
        }
    }
}