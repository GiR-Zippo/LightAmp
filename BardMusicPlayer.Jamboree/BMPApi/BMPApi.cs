using BardMusicPlayer.Jamboree.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace BardMusicPlayer.Jamboree
{
    public class BMPApi
    {
        private static readonly Lazy<BMPApi> LazyInstance = new(() => new BMPApi());
        public static BMPApi Instance => LazyInstance.Value;
        private BMPApi() {}


        public static readonly string ApiUrl = "https://bardmusicplayer.com/api/party/sessions";
        public static readonly string UserAgent = "XIVMIDI CLIENT V2 (LightAmp)";

        private HttpClient httpClient { get; set; } = null;
        private HttpClientHandler httpClientHandler { get; set; } = null;

        /// <summary>
        /// The timer for the heartbeat, only when we are client
        /// </summary>
        private Timer heartbeat { get; set; } = new Timer();
        /// <summary>
        /// The hostData, if we are the host
        /// </summary>
        private SessionCreated hostData { get; set; } = null;
        /// <summary>
        /// The client data, only if we are a client
        /// </summary>
        private MemberStateResponse clientData { get; set; } = null;
        /// <summary>
        /// The playlist we got
        /// </summary>
        private PlaylistManifest playlist { get; set; } = null;

        /// <summary>
        /// Starts the http client
        /// </summary>
        public void StartService()
        {
            httpClientHandler = new HttpClientHandler
            {
                UseCookies = true,
                UseProxy = true,
                MaxAutomaticRedirections = 2,
                MaxConnectionsPerServer = 2
            };

            httpClient = new HttpClient(handler: httpClientHandler);
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);

            heartbeat.Stop();
            heartbeat.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SendHeartBeat().ConfigureAwait(true);
        }

        /// <summary>
        /// Stops and dispose the http stuff
        /// </summary>
        public void StopService()
        {
            heartbeat.Elapsed -= Timer_Elapsed;
            heartbeat.Dispose();
            httpClient.Dispose();
            httpClientHandler.Dispose();
        }


        /// <summary>
        /// Create a session
        /// <code>POST: /api/party/sessions </code>
        /// </summary>
        public async Task CreateSession()
        {
            if (hostData != null || clientData != null)
                return;
            using (var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl))
            {
                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }

                var data = await response.Content.ReadAsStringAsync();
                hostData = JsonConvert.DeserializeObject<SessionCreated>(data);
                BmpJamboree.Instance.PublishEvent(new PartyCreatedEvent(hostData));
                BmpJamboree.Instance.PublishEvent(new PartyLogEvent("New Session created: " + hostData.code));
            }
        }

        /// <summary>
        /// Send Midis file(s) for playlist
        /// <code>POST: /api/party/sessions/by-code/{code}/playlist </code>
        /// </summary>
        /// <returns></returns>
        public async Task SendPlaylist(List<string> files)
        {
            if (hostData == null)
                return;
            if (files == null || files.Count == 0)
                return;

            string url = ApiUrl + "/by-code/" + hostData.code + "/playlist";

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                request.Headers.TryAddWithoutValidation("X-Party-Host-Token", hostData.hostToken);

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

                using (HttpResponseMessage response = await httpClient.SendAsync(request))
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
        public async Task GetPlaylist()
        {
            if (clientData == null)
                return;
            string url = ApiUrl + "/by-code/" + clientData.code + "/manifest";
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                var data = await response.Content.ReadAsStringAsync();
                playlist = JsonConvert.DeserializeObject<PlaylistManifest>(data);
                BmpJamboree.Instance.PublishEvent(new PartyPlaylistEvent(playlist));
                BmpJamboree.Instance.PublishEvent(new PartyLogEvent("New playlist received..."));
            }
        }

        /// <summary>
        /// get midi file
        /// <code>POST: /api/party/sessions/by-code/{code}/items/{itemId}/file </code>
        /// </summary>
        /// <returns></returns>
        public async Task GetMidiFile(string itemId)
        {
            if (clientData == null)
                return;
            string url = ApiUrl + "/by-code/" + clientData.code + "/items/" + itemId + "/file";
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.Accept.ParseAdd("application/gzip");
                HttpResponseMessage response = await httpClient.SendAsync(request);
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
                    BmpJamboree.Instance.PublishEvent(new PartyMidiEvent(resultStream.ToArray()));
                }
            }
        }

        /// <summary>
        /// Join a session by {code}
        /// <code>POST: /api/party/sessions/by-code/{code}/members </code>
        /// </summary>
        /// <param name="code"></param>
        public async Task JoinParty(string code)
        {
            if (clientData != null)
                return;
            string url = ApiUrl + "/by-code/" + code + "/members";
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Accept.ParseAdd("application/json");
                var jsonContent = new StringContent("{\r\n  \"displayName\": \"string\"\r\n}", Encoding.UTF8, "application/json");
                request.Content = jsonContent;

                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                var data = await response.Content.ReadAsStringAsync();
                clientData = JsonConvert.DeserializeObject<MemberStateResponse>(data);
                clientData.code = code;
                BmpJamboree.Instance.PublishEvent(new PartyJoinedEvent(clientData));
                // set the heartbeat
                heartbeat.Interval = 10000;
                heartbeat.Start();

                BmpJamboree.Instance.PublishEvent(new PartyLogEvent("Session joined"));
                // get the playlist
                await GetPlaylist();
            }
        }

        /// <summary>
        /// Send the heartbeat
        /// <code>POST: /api/party/sessions/by-code/{code}/members/{id}/heartbeat </code>
        /// </summary>
        /// <param name="code"></param>
        public async Task SendHeartBeat()
        {
            if (clientData == null)
            {
                heartbeat.Stop();
                return;
            }
            string url = ApiUrl + "/by-code/" + clientData.code + "/members/" + clientData.memberId + "/heartbeat";
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.TryAddWithoutValidation("X-Party-Member-Token", clientData.memberToken);

                // heartbeat package
                Heartbeat hb = new Heartbeat();
                hb.knownPlaylistVersion = playlist == null ? 0 : playlist.playlistVersion;
                hb.since = 0;
                hb.wait = false;
                var jsonContent = new StringContent(JsonConvert.SerializeObject(hb), Encoding.UTF8, "application/json");
                request.Content = jsonContent;
                HttpResponseMessage response = await httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    StatusResponse((int)response.StatusCode);
                    return;
                }
                // do something with our heartbeat response
                var data = await response.Content.ReadAsStringAsync();
                var heartbeatResponse = JsonConvert.DeserializeObject<HeartbeatResponse>(data);
                BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Heartbeat] Playlistversion: "+heartbeatResponse.playlistVersion +"\r\n"));

                // no playlist? gtfo
                if (playlist == null)
                    return;

                // different playlist versions, grab the current one
                if (playlist.playlistVersion != heartbeatResponse.playlistVersion)
                    await GetPlaylist();
            }
        }


        public void LeaveParty()
        {
            if (clientData != null)
            {
                heartbeat.Stop();
                clientData = null;
            }

            if (hostData != null)
                hostData = null;

            playlist = null;
            BmpJamboree.Instance.PublishEvent(new PartyLogEvent("Party left..."));
        }


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
        // /api/party/sessions/by-code/{code}/now-playing
        // /api/party/sessions/by-code/{code}/members/{id}/assignment

        /// <summary>
        /// Get Status response if not 200
        /// </summary>
        /// <param name="StatusCode"></param>
        public void StatusResponse(int StatusCode)
        {
            switch (StatusCode)
            {
                case 400:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Required field missing or value rejected.\r\n"));
                    break;
                case 403:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Authenticated but not allowed to perform this action.\r\n"));
                    break;
                case 404:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("No record with that ID.\r\n"));
                    break;
                case 409:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Party is full.\r\n"));
                    break;
                case 429:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Too many requests from this client; retry after the Retry-After header.\r\n"));
                    break;
                case 503:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Server at session capacity; retry later.\r\n"));
                    break;
                default:
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("Sum ting wong.\r\n"));
                    break;
            }
        }
    }
}