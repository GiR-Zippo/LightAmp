/*
 * Copyright(c) 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

using System;

namespace BardMusicPlayer.Jamboree
{
    public partial class BmpJamboree : IDisposable
    {

#region Instance Constructor/Destructor
        private static readonly Lazy<BmpJamboree> LazyInstance = new(() => new BmpJamboree());

        /// <summary>
        /// 
        /// </summary>
        public bool Started { get; private set; }

        private BmpJamboree(){}

        public static BmpJamboree Instance => LazyInstance.Value;

        /// <summary>
        /// Start the eventhandler
        /// </summary>
        /// <returns></returns>
        public void Start()
        {
            if (Started) return;
            StartEventsHandler();
            BMPApi.Instance.StartService();
            Started = true;
        }

        /// <summary>
        /// Stop the eventhandler
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            if (!Started) return;
            StopEventsHandler();
            BMPApi.Instance.StopService();
            Started = false;
        }

        ~BmpJamboree() { Dispose(); }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
#endregion

        public void JoinParty()
        {
        }

        public void LeaveParty()
        {
            BMPApi.Instance.LeaveParty();
        }

        public void SendPerformanceStart()
        {
        }

        /// <summary>
        /// Send we joined the party
        /// | type 0 = bard
        /// | type 1 = dancer
        /// </summary>
        /// <param name="type"></param>
        /// <param name="performer_name"></param>
        public void SendPerformerJoin(byte type, string performer_name)
        {
        }

        public void SendClientPacket(byte[] packet)
        {
        }

        public void SendServerPacket(byte [] packet)
        {
        }

    }
}
