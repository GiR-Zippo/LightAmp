/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using BardMusicPlayer.Seer.Events;
using BardMusicPlayer.Seer.Reader.Backend.Machina;
using BardMusicPlayer.Seer.Utilities;

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
            DalamudManager.Instance.NameAndHomeworld += OnNameAndHomeworld;
            DalamudManager.Instance.EnsembleStart += OnEnsembleStart;
            DalamudManager.Instance.PerformanceMode += OnPerformanceModeUpdate;
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(SleepTimeInMs, token);
            }
        }

        public void Dispose()
        {
            DalamudManager.Instance.NameAndHomeworld -= OnNameAndHomeworld;
            DalamudManager.Instance.EnsembleStart -= OnEnsembleStart;
            DalamudManager.Instance.PerformanceMode -= OnPerformanceModeUpdate;
            GC.SuppressFinalize(this);
        }

        private void OnNameAndHomeworld(int processId, string Name, int WorldId)
        {
            if (ReaderHandler.Game.Pid != processId)
                return;
            
            if (World.Ids.ContainsKey(WorldId))
                ReaderHandler.Game.PublishEvent(new HomeWorldChanged(EventSource.Machina, World.Ids[WorldId]));

            if (!string.IsNullOrEmpty(Name))
                ReaderHandler.Game.PublishEvent(new PlayerNameChanged(EventSource.Machina, Name));
        }

        internal void OnEnsembleStart(int processId, int code)
        {
            if (ReaderHandler.Game.Pid != processId) 
                return;

            DateTime currentTime = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeMilliseconds();

            if (code == 1)
                ReaderHandler.Game.PublishEvent(new EnsembleStarted(EventSource.Dalamud, unixTime));
        }

        internal void OnPerformanceModeUpdate(int processId, int CurrentGroupTone)
        {
            if (ReaderHandler.Game.Pid != processId)
                return;

            DateTime currentTime = DateTime.UtcNow;
            long unixTime = ((DateTimeOffset)currentTime).ToUnixTimeMilliseconds();

            /*if (code == 1)
                ReaderHandler.Game.PublishEvent(new Perf(EventSource.Dalamud, unixTime));*/
        }
    }
}