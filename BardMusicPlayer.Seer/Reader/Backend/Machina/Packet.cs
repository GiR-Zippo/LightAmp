/*
 * Copyright(c) 2022 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

#region

using System;
using System.Collections.Generic;

#endregion

namespace BardMusicPlayer.Seer.Reader.Backend.Machina
{
    internal sealed partial class Packet : IDisposable
    {
        private readonly Dictionary<ulong, uint> _contentId2ActorId = new();
        private readonly MachinaReaderBackend _machinaReader;

        internal Packet(MachinaReaderBackend machinaReader)
        {
            _machinaReader = machinaReader;
        }

        public void Dispose()
        {
            _contentId2ActorId.Clear();
        }

        private static bool ValidTimeSig(byte timeSig)
        {
            return timeSig is > 1 and < 8;
        }

        private static bool ValidTempo(byte tempo)
        {
            return tempo is > 29 and < 201;
        }

        ~Packet()
        {
            Dispose();
        }
    }
}