/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;

namespace BardMusicPlayer.XIVMIDI;

public sealed partial class XIVMIDI : IDisposable
{
    /// <summary>
    /// If request has finished
    /// </summary>
    public EventHandler<object> OnRequestFinished;

    private static readonly Lazy<XIVMIDI> LazyInstance = new(static () => new XIVMIDI());

    /// <summary>
    ///
    /// </summary>
    public bool Started { get; private set; }

    public bool IsRequestRunning { get; private set; }

    private XIVMIDI()
    {
    }

    public static XIVMIDI Instance => LazyInstance.Value;


    /// <summary>
    /// Start XIVMIDI.
    /// </summary>
    public void Start()
    {
        if (Started)
            return;

        StartService();
        Started = true;
    }

    /// <summary>
    /// Stop XIVMIDI
    /// </summary>
    public void Stop()
    {
        if (!Started) return;

        Started = false;
        this.StopWorkerThread();
        this.httpClient.Dispose();
        Dispose();
    }

    ~XIVMIDI() => Dispose();
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Add a Request-Object to the queue
    /// </summary>
    /// <param name="data"></param>
    public void AddToQueue(object data)
    {
        downloadQueue.Enqueue(data);
    }

    public void CancelDownloads()
    {
        this.CancelDownloadQueue();
    }
}

