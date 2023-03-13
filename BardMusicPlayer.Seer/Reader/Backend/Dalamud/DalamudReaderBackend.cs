/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

#region

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BardMusicPlayer.Seer.Events;
using BardMusicPlayer.Seer.Reader.Backend.Machina;
using BardMusicPlayer.Seer.Utilities;

#endregion

namespace BardMusicPlayer.Seer.Reader.Backend.Dalamud
{
    internal sealed class DalamudReaderBackend : IReaderBackend
    {
        public DalamudReaderBackend(int sleepTimeInMs)
        {
            ReaderBackendType = EventSource.DalamudManager;
            SleepTimeInMs = sleepTimeInMs;
        }

        public EventSource ReaderBackendType { get; }

        public ReaderHandler ReaderHandler { get; set; }

        public int SleepTimeInMs { get; set; }

        public async Task Loop(CancellationToken token)
        {
            DalamudManager.Instance.EnsembleStart += OnEnsembleStart;

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(SleepTimeInMs, token);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        internal void OnEnsembleStart(int processId, int code)
        {
            if (ReaderHandler.Game.Pid != processId) 
                return;

            DateTime currentTime = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeMilliseconds();

            if (code == 1)
                ReaderHandler.Game.PublishEvent(new EnsembleStarted(EventSource.Machina, unixTime));
        }
    }
}