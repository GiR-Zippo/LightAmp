using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Linq;
using BardMusicPlayer.XIVMIDI.IO;

namespace BardMusicPlayer.XIVMIDI;


/*
The Api stuff to access the XIVMIDI
-- Init via XivMIDIApi.Initialize();
-- Stop via XivMIDIApi.Stop();

-- The callback event from this library
XivMIDIApi.Instance.OnRequestFinished += Instance_RequestFinished;
private void Instance_RequestFinished(object sender, object e){}

object e can be:
XivMIDIApi.Response.ApiResponse class - if there was an Api JSon request
XivMIDIApi.Response.MidiFile class    - if there was a download request

-- Perform a JSon request to xivmidi
XivMIDIApi.Instance.AddToQueue(new XivMIDIApi.GetRequest()
{
Url = new XivMIDIApi.RequestBuilder() { Performers = PerformerSize_box.SelectedIndex }.BuildRequest(),
Host = "xivmidi.com",
Requester = XivMIDIApi.Requester.JSON
});

This will do a request for 100 Octets


-- Perform a midi download
XivMIDIApi.Instance.AddToQueue(new XivMIDIApi.GetRequest()
{
Url = DownloadUrl + Uri.EscapeUriString(filename),
Host = "xivmidi.com",
Accept = "audio/midi",
Requester = XivMIDIApi.Requester.DOWNLOAD
});

This will requst a midi file (website_file_path is the filename) and returns a XivMIDIApi.Response.MidiFile with filename and byte[] containing the midi binary
*/

public sealed partial class XIVMIDI
{
    private HttpClient httpClient { get; set; } = null;
    private HttpClientHandler httpClientHandler { get; set; } = null;

    private ConcurrentQueue<object> downloadQueue = new ConcurrentQueue<object>();
    private CancellationTokenSource cancelTokenSource;

    /// <summary>
    /// Start the Service
    /// </summary>
    private void StartService()
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
        StartWorkerThread();
    }

    /// <summary>
    /// Start the Workers
    /// </summary>
    private void StartWorkerThread()
    {
        downloadQueue = new ConcurrentQueue<object>();
        cancelTokenSource = new CancellationTokenSource();
        Task.Factory.StartNew(() => RunEventsHandler(cancelTokenSource.Token), TaskCreationOptions.LongRunning);
    }

    /// <summary>
    /// Stop the worker
    /// </summary>
    private void StopWorkerThread()
    {
        cancelTokenSource.Cancel();
        while (downloadQueue.TryDequeue(out _))
        { }
    }

    /// <summary>
    /// Worker runnable task
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task RunEventsHandler(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            while (downloadQueue.TryDequeue(out var request))
            {
                if (token.IsCancellationRequested)
                    break;

                if (request is GetRequest)
                    _ = GetAsync(request as GetRequest);
            }
            await Task.Delay(100, token).ContinueWith(tsk => { });
        }
    }

    /// <summary>
    /// Internal get data from server task
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    private async Task GetAsync(GetRequest request)
    {
        foreach (Cookie co in httpClientHandler.CookieContainer.GetCookies(new Uri(request.Url)))
        {
            co.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(1));
        }

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(request.UserAgent);
        if (request.Accept != "")
            httpClient.DefaultRequestHeaders.Accept.ParseAdd(request.Accept);

        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(request.Url);
            request.ResponseBody = response.Content;
            request.Host = new Uri(request.Url).DnsSafeHost;
            request.ResponseCode = response.StatusCode;
            request.ResponseMsg = response.ReasonPhrase;
        }
        catch (HttpRequestException e)
        {
            request.ResponseCode = HttpStatusCode.ServiceUnavailable;
            request.ResponseMsg = e.InnerException.Message;
        }

        if (request.ResponseCode != HttpStatusCode.OK)
        {
            OnRequestFinished(this, request);
            return;
        }

        if (request.Requester == Requester.JSON)
            GetJSon(request);
        else if (request.Requester == Requester.DOWNLOAD)
            GetMidi(request);
    }

    private void GetJSon(GetRequest request)
    {
        ResponseContainer.ApiResponse resp = JsonConvert.DeserializeObject<ResponseContainer.ApiResponse>(request.ResponseBody.ReadAsStringAsync().Result);
        OnRequestFinished(this, resp);
        return;
    }

    private void GetMidi(GetRequest request)
    {
        var f = Uri.UnescapeDataString(request.Url);
        f = f.Split('/').Last();
        ResponseContainer.MidiFile midi = new()
        {
            Filename = f,
            data = request.ResponseBody.ReadAsByteArrayAsync().Result
        };
        OnRequestFinished(this, midi);
        return;
    }
}