/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree.Events;
using BardMusicPlayer.Quotidian;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.Jamboree
{
    public partial class BMPApi : IDisposable
    {
        private CancellationTokenSource _heartbeatCts;

        /// <summary>
        /// Create a session
        /// <code>POST: /api/party/sessions </code>
        /// </summary>
        public async Task CreateSession()
        {
            if (_HostData != null || _ClientData != null)
                return;

            using (var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl))
            {
                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }

                var data = await response.Content.ReadAsStringAsync();
                _HostData = JsonConvert.DeserializeObject<SessionCreated>(data);
                BmpJamboree.Instance.PublishEvent(new PartyCreatedEvent(_HostData));
                BmpJamboree.Instance.PublishEvent(new PartyLogEvent("New Session created: " + _HostData.code));

                // create party
                _Party = new Party();

                // set the _Heartbeat
                _Heartbeat.Interval = 10000;
                _Heartbeat.Start();
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

            string url = ApiUrl + "/by-code/" + _HostData.code + "/playlist";

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
        /// gets the playlist
        /// <code>POST: /api/party/sessions/by-code/{code}/manifest </code>
        /// </summary>
        /// <returns></returns>
        public async Task GetSessionManifest()
        {
            if (!IsConnected())
                return;

            string code = _ClientData == null ? code = _HostData.code : code = _ClientData.code;
            string url = ApiUrl + "/by-code/" + code + "/manifest";
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
                _SessionManifest = JsonConvert.DeserializeObject<SessionManifest>(data);
                _Party.UpdateMembers(_SessionManifest);

                BmpJamboree.Instance.PublishEvent(new SessionManifestEvent(_SessionManifest));
                BmpJamboree.Instance.PublishEvent(new PartyLogEvent("New Manifest received..."));
            }
        }

        /// <summary>
        /// get midi file
        /// <code>POST: /api/party/sessions/by-code/{code}/items/{itemId}/file </code>
        /// </summary>
        /// <returns></returns>
        public async Task GetMidiFile(string itemId)
        {
            if (_ClientData == null)
                return;
            string url = ApiUrl + "/by-code/" + _ClientData.code + "/items/" + itemId + "/file";
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
                    BmpJamboree.Instance.PublishEvent(new PartyMidiEvent(itemId, resultStream.ToArray()));
                }
            }
        }

        /// <summary>
        /// Join a session by {code}
        /// <code>POST: /api/party/sessions/by-code/{code}/members </code>
        /// </summary>
        /// <param name="code"></param>
        public async Task JoinParty(string code, string name)
        {
            if (_ClientData != null)
                return;
            string url = ApiUrl + "/by-code/" + code + "/members";
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                var jsonContent = new StringContent("{\r\n  \"displayName\": \"" + name + "\"\r\n}", Encoding.UTF8, "application/json");
                request.Content = jsonContent;

                HttpResponseMessage response = await _HttpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                var data = await response.Content.ReadAsStringAsync();
                _ClientData = JsonConvert.DeserializeObject<MemberStateResponse>(data);
                _ClientData.code = code;
                BmpJamboree.Instance.PublishEvent(new PartyJoinedEvent(_ClientData));
                BmpJamboree.Instance.PublishEvent(new PartyLogEvent("Session joined"));

                // create party
                _Party = new Party();

                // set the _Heartbeat
                _ = StartHeartBeatLoop();

                // get the SessionManifest
                await GetSessionManifest();
            }
        }

        /// <summary>
        /// Send the heartbeat in a loop
        /// <code>POST: /api/party/sessions/by-code/{code}/members/{id}/heartbeat </code>
        /// </summary>
        private async Task StartHeartBeatLoop()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts = new CancellationTokenSource();
            var token = _heartbeatCts.Token;

            while (!token.IsCancellationRequested && _ClientData != null)
            {
                string url = ApiUrl + "/by-code/" + _ClientData.code + "/members/" + _ClientData.memberId + "/heartbeat";

                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        request.Headers.TryAddWithoutValidation("X-Party-Member-Token", _ClientData.memberToken);

                        Heartbeat hb = new Heartbeat
                        {
                            knownPlaylistVersion = _SessionManifest   == null ? 0 : _SessionManifest.playlistVersion,
                            since                = _HeartbeatResponse == null ? 0 : _HeartbeatResponse.stateVersion,
                            wait                 = true // Long Polling
                        };

                        request.Content = new StringContent(JsonConvert.SerializeObject(hb), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = await _HttpClient.SendAsync(request, token);
                        if (!StatusResponse((int)response.StatusCode))
                            break;

                        var data = await response.Content.ReadAsStringAsync();
                        _HeartbeatResponse = JsonConvert.DeserializeObject<HeartbeatResponse>(data);
                        if (_SessionManifest == null)
                            continue;

                        if ((_SessionManifest.playlistVersion != _HeartbeatResponse.playlistVersion) ||
                            (_SessionManifest.stateVersion != _HeartbeatResponse.stateVersion))
                        {
                            BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Heartbeat] Playlistversion: " + _HeartbeatResponse.playlistVersion + "\r\n"));
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
    }
}
