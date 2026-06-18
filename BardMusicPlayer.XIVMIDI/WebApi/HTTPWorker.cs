/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.XIVMIDI.Events;
using BardMusicPlayer.XIVMIDI.IO;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.XIVMIDI.WebApi
{
    public class HTTPWorker : IDisposable
    {
        private string UserAgent { get; } = "XIVMIDI CLIENT V2 (LightAmp)";
        private HttpClient _HttpClient { get; set; } = null;
        private HttpClientHandler _HttpClientHandler { get; set; } = null;
        public HTTPWorker()
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
            _HttpClient.DefaultRequestHeaders.Clear();
            _HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        public void Dispose()
        {
            _HttpClient.Dispose();
            _HttpClientHandler.Dispose();
        }

        /// <summary>
        /// Get the song list
        /// </summary>
        /// <param name="request"></param>
        public async Task RequestSongList(object request)
        {
            string url = "";
            if (request is BMPAPIRequestBuilder)
                url = ((BMPAPIRequestBuilder)request).BuildRequest();
            else if (request is XIVMIDIRequestBuilder)
                url = ((XIVMIDIRequestBuilder)request).BuildRequest();

            if (url.Length < 1)
                return;

            foreach (Cookie co in _HttpClientHandler.CookieContainer.GetCookies(new Uri(url)))
                co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));

            _HttpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json;q=0.8");
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync(url);
                HttpContent ResponseBody = response.Content;
                if (response.IsSuccessStatusCode)
                {
                    if (request is BMPAPIRequestBuilder)
                        XIVMidiApi.Instance.PublishEvent(new XIVMidiBMPSongsEvent(JsonConvert.DeserializeObject<BMPResponseContainer.Root>(ResponseBody.ReadAsStringAsync().Result)));
                    else if (request is XIVMIDIRequestBuilder)
                        XIVMidiApi.Instance.PublishEvent(new XIVMidiXIVSongsEvent(JsonConvert.DeserializeObject<XIVMIDIResponseContainer.ApiResponse>(ResponseBody.ReadAsStringAsync().Result)));
                    return;
                }
                else
                    XIVMidiApi.Instance.PublishEvent(new XIVMidiApiErrorEvent((int)response.StatusCode, response.StatusCode.ToString()));
            }
            catch (HttpRequestException e)
            {
                XIVMidiApi.Instance.PublishEvent(new XIVMidiApiErrorEvent((int)HttpStatusCode.ServiceUnavailable, e.InnerException.Message));
            }
        }

        /// <summary>
        /// Download a song
        /// </summary>
        /// <param name="url"></param>
        /// <param name="args"></param>
        /// <param name="fromBMP"></param>
        public async Task DownloadSong(string url, object args, bool fromBMP)
        {
            if (url.Length < 1)
                return;

            if (!fromBMP)
                url = "https://xivmidi.com" + url;

            foreach (Cookie co in _HttpClientHandler.CookieContainer.GetCookies(new Uri(url)))
                co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));

            _HttpClient.DefaultRequestHeaders.Accept.ParseAdd("audio/mid");
            try
            {
                HttpResponseMessage response = await _HttpClient.GetAsync(url);
                HttpContent ResponseBody = response.Content;
                if (response.IsSuccessStatusCode)
                {
                    XIVMIDIResponseContainer.MidiFile midi = new()
                    {
                        Filename = WebUtility.UrlDecode(Uri.UnescapeDataString(url)).Split('/').Last(),
                        data = ResponseBody.ReadAsByteArrayAsync().Result
                    };
                    XIVMidiApi.Instance.PublishEvent(new XIVMidiFileEvent(midi, args));
                }
                else
                    XIVMidiApi.Instance.PublishEvent(new XIVMidiApiErrorEvent((int)response.StatusCode, response.StatusCode.ToString()));
            }
            catch (HttpRequestException e)
            {
                XIVMidiApi.Instance.PublishEvent(new XIVMidiApiErrorEvent((int)HttpStatusCode.ServiceUnavailable, e.InnerException.Message));
            }
        }

        /// <summary>
        /// Upload a song
        /// </summary>
        /// <param name="upload"></param>
        public async Task UploadSong(BMPUploadBuilder upload)
        {
            string url = upload.ApiBaseUrl;
            using (var multipartContent = new MultipartFormDataContent())
            {
                var fileContent = new ByteArrayContent(upload.MidiFile);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mid");
                multipartContent.Add(fileContent, "file", Path.GetFileName(upload.FileName));

                string jsonString = JsonConvert.SerializeObject(upload);
                var jsonContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                multipartContent.Add(jsonContent, "_payload");
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Content = multipartContent;
                    request.Headers.Authorization = new AuthenticationHeaderValue("users", "API-Key " + upload.ApiKey);
                    try
                    {
                        HttpResponseMessage response = await _HttpClient.SendAsync(request);
                        XIVMidiApi.Instance.PublishEvent(new XIVMidiUploadResponseEvent(response.StatusCode));
                    }
                    catch (Exception ex)
                    {
                        XIVMidiApi.Instance.PublishEvent(new XIVMidiUploadResponseEvent(HttpStatusCode.ServiceUnavailable));
                    }
                }
            }
        }
    }
}
