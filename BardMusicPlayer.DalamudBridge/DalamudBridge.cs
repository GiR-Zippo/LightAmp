/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using BardMusicPlayer.DalamudBridge.Helper.Dalamud;
using BardMusicPlayer.Seer;

namespace BardMusicPlayer.DalamudBridge
{
    public sealed partial class DalamudBridge
    {
        private static readonly Lazy<DalamudBridge> LazyInstance = new(static () => new DalamudBridge());

        /// <summary>
        /// 
        /// </summary>
        public bool Started { get; private set; }

        internal DalamudServer DalamudServer;

        private DalamudBridge()
        {
        }

        public static DalamudBridge Instance => LazyInstance.Value;

        /// <summary>
        /// Start DalamudBridge.
        /// </summary>
        public void Start()
        {
            if (Started) 
                return;
            if (!BmpSeer.Instance.Started) throw new DalamudBridgeException("DalamudBridge requires Seer to be running.");
            DalamudServer = new DalamudServer();
            StartResponeEventsHandler();
            StartCommandEventsHandler();
            Started = true;
        }

        /// <summary>
        /// Stop DalamudBridge.
        /// </summary>
        public void Stop()
        {
            if (!Started) return;
            StopResponseEventsHandler();
            StopCommandEventsHandler();
            DalamudServer?.Dispose();
            DalamudServer = null;
            Started = false;
        }

        public void ActionToQueue(DalamudBridgeCommandStruct data)
        {
            if (!Started) return;
            PublishCommandEvent(data);
        }


        ~DalamudBridge() => Dispose();
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
