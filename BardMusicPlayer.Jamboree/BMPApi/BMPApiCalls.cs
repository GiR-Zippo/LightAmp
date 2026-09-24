/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.Jamboree
{
    /// <summary>
    /// Implements:
    /// <code>POST   /api/party/sessions</code>
    /// <code>DELETE /api/party/sessions/by-code/{code}</code>
    /// <code>POST   /api/party/sessions/by-code/{code}/playlist</code>
    /// <code>GET    /api/party/sessions/by-code/{code}/manifest</code>
    /// <code>GET    /api/party/sessions/by-code/{code}/items/{itemId}/file</code>
    /// <code>POST   /api/party/sessions/by-code/{code}/members</code>
    /// <code>DELETE /api/party/sessions/by-code/{code}/members/{id}</code>
    /// <code>POST   /api/party/sessions/by-code/{code}/members/{id}/heartbeat</code>
    /// <code>POST   /api/party/sessions/by-code/{code}/host-heartbeat</code>
    /// <code>PATCH  /api/party/sessions/by-code/{code}/members/{id}/assignment</code>
    /// <code>PATCH  /api/party/sessions/by-code/{code}/now-playing</code>
    /// missing:
    /// <code>/api/party/sessions/by-code/{code}/archive</code>
    /// </summary>
    public partial class BMPApi : IDisposable
    {
        private CancellationTokenSource _heartbeatCts;

        /// <summary>
        /// Create a session
        /// <code>POST: /api/party/sessions </code>
        /// </summary>
        public async Task CreateSession(List<KeyValuePair<string, string>> localCharacters)
        {
            if (_HostData != null || _ClientData != null)
                return;

            using (var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl))
            {
                request.Headers.Accept.ParseAdd("application/json");
                var additionalChars = localCharacters.Select(c => new
                {
                    displayName = c.Key,
                    world = c.Value
                }).ToList();

                var payload = new Dictionary<string, object>();
                if (additionalChars.Count > 0)
                    payload.Add("characters", additionalChars);

                string jsonString = JsonConvert.SerializeObject(payload);
                request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    BmpJamboree.Instance.PublishEvent(new PartyCreatedEvent(false, response.Content.ToString()));
                    return;
                }

                var data = await response.Content.ReadAsStringAsync();
                _HostData = JsonConvert.DeserializeObject<SessionCreated>(data);
                BmpJamboree.Instance.PublishEvent(new PartyCreatedEvent(true, _HostData.code));
                BmpJamboree.Instance.PublishEvent(new PartyLogEvent("New Session created: " + _HostData.code));

                // create party
                _Party = new Party();

                // create Playlist
                _Playlist = new PartySongs(this);

                // set the _Heartbeat
                _ = StartHostHeartBeatLoop();

                // get the SessionManifest
                await GetSessionManifest();
            }
        }

        /// <summary>
        /// Deletes the session
        /// <code>DELETE: /api/party/sessions/by-code/{code} </code>
        /// </summary>
        /// <returns></returns>
        public async Task DeleteSession()
        {
            if (_HostData == null)
                return;

            string url = ApiUrl + "/by-code/" + GetCode();
            using (var request = new HttpRequestMessage(new HttpMethod("DELETE"), url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.TryAddWithoutValidation("X-Party-Host-Token", _HostData.hostToken);

                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                BmpJamboree.Instance.PublishEvent(new PartyLeftEvent(true));
            }
        }

        /// <summary>
        /// Send Midis file(s) for playlist
        /// <code>POST: /api/party/sessions/by-code/{code}/playlist </code>
        /// </summary>
        /// <returns></returns>
        public async Task SendPlaylist(List<string> files)
        {
            if (_HostData == null)
                return;
            if (files == null || files.Count == 0)
                return;

            string url = ApiUrl + "/by-code/" + GetCode() + "/playlist";
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.TryAddWithoutValidation("X-Party-Host-Token", _HostData.hostToken);

                //build tar.gz
                if (files.Count > 1)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, leaveOpen: true))
                        {
                            foreach (var filePath in files)
                            {
                                if (!File.Exists(filePath)) continue;

                                byte[] fileBytes = File.ReadAllBytes(filePath);
                                string fileName = Path.GetFileName(filePath);

                                byte[] header = CreateTarHeader(fileName, fileBytes.Length);
                                await gzipStream.WriteAsync(header, 0, header.Length);
                                await gzipStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                                int padding = (512 - (fileBytes.Length % 512)) % 512;
                                if (padding > 0)
                                    await gzipStream.WriteAsync(new byte[padding], 0, padding);
                            }
                            await gzipStream.WriteAsync(new byte[1024], 0, 1024);
                        }

                        var fileContent = new ByteArrayContent(memoryStream.ToArray());
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
                        request.Content = fileContent;
                    }
                }
                // one file
                else
                {
                    string filename = files[0];
                    string pureFileName = Path.GetFileName(filename);
                    bool shouldZipSingleFile = filename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);

                    if (shouldZipSingleFile)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                            using (var fileStream = File.OpenRead(filename))
                            {
                                await fileStream.CopyToAsync(gzipStream);
                            }
                            var fileContent = new ByteArrayContent(memoryStream.ToArray());
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
                            request.Content = fileContent;
                        }
                    }
                    else
                    {
                        var fileContent = new ByteArrayContent(File.ReadAllBytes(filename));
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/midi");
                        request.Content = fileContent;
                        request.Headers.TryAddWithoutValidation("X-Party-Filename", pureFileName);
                    }
                }

                using (HttpResponseMessage response = await _HttpClient.SendAsync(request))
                {
                    var data = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        StatusResponse((int)response.StatusCode);
                        return;
                    }
                    PlaylistResponse resp = JsonConvert.DeserializeObject<PlaylistResponse>(data);
                    BmpJamboree.Instance.PublishEvent(new PartyPlaylistSendEvent(resp));
                    BmpJamboree.Instance.PublishEvent(new PartyLogEvent("Files uploaded..."));
                }
            }
        }

        /// <summary>
        /// Sends a midi by raw bytes
        /// </summary>
        /// <param name="name"></param>
        /// <param name="data"></param>
        public async Task SendSong(string name, byte[] data)
        {
            if (_HostData == null)
                return;
            if (data == null || data.Length == 0)
                return;

            string url = ApiUrl + "/by-code/" + GetCode() + "/playlist";
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.TryAddWithoutValidation("X-Party-Host-Token", _HostData.hostToken);

                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                        gzipStream.Write(data, 0, data.Length);

                    var fileContent = new ByteArrayContent(memoryStream.ToArray());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
                    request.Content = fileContent;
                    request.Headers.TryAddWithoutValidation("X-Party-Filename", name);


                    using (HttpResponseMessage response = await _HttpClient.SendAsync(request))
                    {
                        var responseData = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            StatusResponse((int)response.StatusCode);
                            return;
                        }
                        PlaylistResponse resp = JsonConvert.DeserializeObject<PlaylistResponse>(responseData);
                        BmpJamboree.Instance.PublishEvent(new PartyPlaylistSendEvent(resp));
                        BmpJamboree.Instance.PublishEvent(new PartyLogEvent("Files uploaded..."));
                    }
                }
            }
        }

        /// <summary>
        /// gets the playlist
        /// <code>POST: /api/party/sessions/by-code/{code}/manifest </code>
        /// </summary>
        /// <returns></returns>
        public async Task GetSessionManifest()
        {
            if (!IsConnected())
                return;

            string url = ApiUrl + "/by-code/" + GetCode() + "/manifest";
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                var data = await response.Content.ReadAsStringAsync();
                var newManifest = JsonConvert.DeserializeObject<SessionManifest>(data);

                // New stateVersion: update the Party-Members
                if (_SessionManifest == null || _SessionManifest.stateVersion != newManifest.stateVersion)
                {
                    _SessionManifest = newManifest;
                    _Party.UpdateMembers(_SessionManifest);
                }

                // New playlistVersion: update the existing playlist
                if (_Playlist.GetPlaylistVersion() != newManifest.playlistVersion)
                {
                    _SessionManifest = newManifest;
                    _Playlist.UpdateSongs(newManifest);
                }

                // Current playing song changed? Set the new one to clients
                if (_SessionManifest.nowPlaying != null && _HostData == null)
                    BmpJamboree.Instance.PublishEvent(new PartySelectSongEvent(_SessionManifest.nowPlaying));

                BmpJamboree.Instance.PublishEvent(new PartyLogEvent("New Manifest received..."));
            }
        }

        /// <summary>
        /// get midi file
        /// <code>POST: /api/party/sessions/by-code/{code}/items/{itemId}/file </code>
        /// </summary>
        /// <returns></returns>
        public async Task DownloadMidiFile(string itemId)
        {
            if (_ClientData == null)
                return;
            string url = ApiUrl + "/by-code/" + GetCode() + "/items/" + itemId + "/file";
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Accept.ParseAdd("application/gzip");
                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                var data = await response.Content.ReadAsStreamAsync();
                using (var gzipStream = new GZipStream(data, CompressionMode.Decompress))
                using (var resultStream = new MemoryStream())
                {
                    await gzipStream.CopyToAsync(resultStream);
                    // add to _Playlist
                    _Playlist.AddMidiFile(itemId, resultStream.ToArray());
                }
            }
        }

        /// <summary>
        /// Join a session by {code}
        /// <code>POST: /api/party/sessions/by-code/{code}/members </code>
        /// </summary>
        /// <param name="code"></param>
        public async Task JoinParty(string code, List<KeyValuePair<string, string>> localCharacters)
        {
            if (_ClientData != null || localCharacters == null || localCharacters.Count == 0)
                return;

            string url = ApiUrl + "/by-code/" + code + "/members";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                var additionalChars = localCharacters.Select(c => new
                {
                    displayName = c.Key,
                    world = c.Value
                }).ToList();

                var payload = new Dictionary<string, object>();
                if (additionalChars.Count > 0)
                    payload.Add("characters", additionalChars);

                string jsonString = JsonConvert.SerializeObject(payload);
                request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _HttpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    BmpJamboree.Instance.PublishEvent(new PartyCreatedEvent(false, errorContent));
                    return;
                }

                var data = await response.Content.ReadAsStringAsync();
                _ClientData = JsonConvert.DeserializeObject<MemberStateResponse>(data);
                _ClientData.code = code;

                BmpJamboree.Instance.PublishEvent(new PartyJoinedEvent(true, _ClientData.code));
                BmpJamboree.Instance.PublishEvent(new PartyLogEvent($"Session joined with {localCharacters.Count} characters."));

                // create party
                _Party = new Party();

                // create playlist
                _Playlist = new(this);
        
                // set the _Heartbeat
                _ = StartHeartBeatLoop();

                // get the SessionManifest
                await GetSessionManifest();
            }
        }

        /// <summary>
        /// /api/party/sessions/by-code/{code}/members/{id}
        /// </summary>
        /// <param name="code"></param>
        /// <param name="localCharacters"></param>
        /// <returns></returns>
        public async Task UpdateSessionMembers(List<KeyValuePair<string, string>> localCharacters)
        {
            if (localCharacters == null || localCharacters.Count == 0)
                return;

            string memberId = "";
            string memberToken = "";
            if (_ClientData != null)
            {
                memberId = _ClientData.memberId;
                memberToken = _ClientData.memberToken;
            }
            else
            {
                memberId = _HostData.sessionId;
                memberToken = _HostData.hostToken;
            }

            string url = ApiUrl + "/by-code/" + GetCode() + "/members/" + GetMemberId();
            using (var request = new HttpRequestMessage(HttpMethod.Put, url))
            {
                request.Headers.TryAddWithoutValidation("X-Party-Member-Token", memberToken);
                request.Headers.Accept.ParseAdd("application/json");

                var existingChars = _Party.FindByCharacterByMemberId(GetMemberId()).Select(c => new Dictionary<string, object>
                {
                    ["charId"] = (string)c.charId,
                    ["displayName"] = c.displayName,
                    ["world"] = c.world
                }).ToList();

                var additionalChars = localCharacters
                    .Where(lc => !existingChars.Any(ec => (string)ec["displayName"] == lc.Key && (string)ec["world"] == lc.Value))
                    .Select(lc => new Dictionary<string, object>
                    {
                        ["displayName"] = lc.Key,
                        ["world"] = lc.Value
                    }).ToList();

                var combinedChars = existingChars.Concat(additionalChars).ToList();
                var payload = new Dictionary<string, object>
                {
                    ["characters"] = combinedChars
                };
                string jsonString = JsonConvert.SerializeObject(payload);
                request.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    BmpJamboree.Instance.PublishEvent(new PartyCreatedEvent(false, errorContent));
                    return;
                }
                // ignore payload we'll get the partylist via heartbeat
                //var f = JsonConvert.DeserializeObject<UpdateSessionMembers>(await response.Content.ReadAsStringAsync());
            }

        }

        /// <summary>
        /// Leaves the session
        /// <code>DELETE: /api/party/sessions/by-code/{code} </code>
        /// </summary>
        /// <returns></returns>
        public async Task LeaveSession()
        {
            if (_ClientData != null)
                return;

            string url = ApiUrl + "/by-code/" + GetCode() + "/members/" + _ClientData.memberId;
            using (var request = new HttpRequestMessage(new HttpMethod("DELETE"), url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                BmpJamboree.Instance.PublishEvent(new PartyLeftEvent(false));
            }
        }

        /// <summary>
        /// Send the client heartbeat in a loop
        /// <code>POST: /api/party/sessions/by-code/{code}/members/{id}/heartbeat </code>
        /// </summary>
        private async Task StartHeartBeatLoop()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts = new CancellationTokenSource();
            var token = _heartbeatCts.Token;

            string memberId = "";
            string memberToken = "";
            if (_ClientData != null)
            {
                memberId = _ClientData.memberId;
                memberToken = _ClientData.memberToken;
            }
            else
            {
                memberId = _HostData.sessionId;
                memberToken = _HostData.hostToken;
            }

            while (!token.IsCancellationRequested && (memberId != "" || memberToken!=""))
            {
                string url = ApiUrl + "/by-code/" + GetCode() + "/members/" + memberId + "/heartbeat";
                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        request.Headers.TryAddWithoutValidation("X-Party-Member-Token", memberToken);

                        Heartbeat hb = new Heartbeat
                        {
                            knownPlaylistVersion = _SessionManifest   == null ? 0 : _SessionManifest.playlistVersion,
                            since                = _ClientHeartbeatResponse == null ? 0 : _ClientHeartbeatResponse.stateVersion,
                            wait                 = true // Long Polling
                        };

                        request.Content = new StringContent(JsonConvert.SerializeObject(hb), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await _HttpClient.SendAsync(request, token);
                        if (!StatusResponse((int)response.StatusCode))
                            break;

                        var data = await response.Content.ReadAsStringAsync();
                        _ClientHeartbeatResponse = JsonConvert.DeserializeObject<ClientHeartbeatResponse>(data);
                        if (_SessionManifest == null)
                            continue;

                        if ((_SessionManifest.playlistVersion != _ClientHeartbeatResponse.playlistVersion) ||
                            (_SessionManifest.stateVersion != _ClientHeartbeatResponse.stateVersion))
                        {
                            BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Heartbeat] Playlistversion: " + _ClientHeartbeatResponse.playlistVersion + "\r\n"));
                            await GetSessionManifest();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Heartbeat] Disconnected.\r\n"));
                    break;
                }
                catch (Exception ex)
                {
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Heartbeat] Error: " + ex.Message + "\r\n"));
                    try { await Task.Delay(2000, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        /// <summary>
        /// Send the host heartbeat in a loop
        /// <code>POST: /api/party/sessions/by-code/{code}/host-heartbeat </code>
        /// </summary>
        private async Task StartHostHeartBeatLoop()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts = new CancellationTokenSource();
            var token = _heartbeatCts.Token;

            string memberId = "";
            string memberToken = "";
            if (_HostData != null)
            {
                memberId = _HostData.sessionId;
                memberToken = _HostData.hostToken;
            }

            while (!token.IsCancellationRequested && (memberId != "" || memberToken != ""))
            {
                string url = ApiUrl + "/by-code/" + GetCode() + "/host-heartbeat";
                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        request.Headers.TryAddWithoutValidation("X-Party-Host-Token", _HostData.hostToken);

                        Heartbeat hb = new Heartbeat
                        {
                            knownPlaylistVersion = _SessionManifest == null ? 0 : _SessionManifest.playlistVersion,
                            since = _HostHeartbeatResponse == null ? 0 : _HostHeartbeatResponse.stateVersion,
                            wait = true // Long Polling
                        };

                        request.Content = new StringContent(JsonConvert.SerializeObject(hb), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await _HttpClient.SendAsync(request, token);
                        if (!StatusResponse((int)response.StatusCode))
                            break;

                        var data = await response.Content.ReadAsStringAsync();
                        _HostHeartbeatResponse = JsonConvert.DeserializeObject<HostHeartbeatResponse>(data);
                        if (_SessionManifest == null)
                            continue;

                        if ((_SessionManifest.playlistVersion != _HostHeartbeatResponse.playlistVersion) ||
                            (_SessionManifest.stateVersion != _HostHeartbeatResponse.stateVersion))
                        {
                            BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Heartbeat] Playlistversion: " + _HostHeartbeatResponse.playlistVersion + "\r\n"));
                            await GetSessionManifest();
                        }

                    }
                }
                catch (OperationCanceledException)
                {
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Heartbeat] Disconnected.\r\n"));
                    break;
                }
                catch (Exception ex)
                {
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Heartbeat] Error: " + ex.Message + "\r\n"));
                    try { await Task.Delay(2000, token); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        /// <summary>
        /// Set the song by downloadId
        /// <code>PATCH: /api/party/sessions/by-code/{code}/now-playing</code>
        /// </summary>
        public async Task SelectSong(string track)
        {
            if (_HostData == null)
                return;

            string url = ApiUrl + "/by-code/" + GetCode() + "/now-playing";
            using (var request = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.TryAddWithoutValidation("X-Party-Host-Token", _HostData.hostToken);

                NowPlayingRequest ta = new NowPlayingRequest
                {
                    itemId = track,
                    playbackState = "stopped"
                };
                request.Content = new StringContent(JsonConvert.SerializeObject(ta), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                var data = await response.Content.ReadAsStringAsync();
                var taResponse = JsonConvert.DeserializeObject<NowPlayingResponse>(data);
            }
        }

        /// <summary>
        /// Set track and instrument
        /// <code>PATCH: /api/party/sessions/by-code/{code}/members/{id}/assignment</code>
        /// </summary>
        public async Task AssignMemberTo(string memberId, TrackAssignment ta)
        {
            if (_HostData == null)
                return;

            string url = ApiUrl + "/by-code/" + GetCode() + "/assignments";
            using (var request = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.TryAddWithoutValidation("X-Party-Host-Token", _HostData.hostToken);
                string d = JsonConvert.SerializeObject(new
                {
                    assignments = new[] { new {
                            charId = ta.charId,
                            trackNumber = ta.trackNumber,
                            instrument = ta.instrument
                        }}});
                request.Content = new StringContent(d, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                var data = await response.Content.ReadAsStringAsync();
                var taResponse = JsonConvert.DeserializeObject<TrackAssignmentResponse>(data);
            }
        }
    }
}
