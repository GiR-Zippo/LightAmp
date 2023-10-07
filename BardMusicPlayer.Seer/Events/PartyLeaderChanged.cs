/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

#region

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BardMusicPlayer.Seer.Utilities;

#endregion

namespace BardMusicPlayer.Seer.Events
{
    public sealed class PartyLeaderChanged : SeerEvent
    {
        internal PartyLeaderChanged(EventSource readerBackendType, KeyValuePair<uint, string> partyLeader) : base(
            readerBackendType)
        {
            EventType = GetType();
            PartyLeader = partyLeader;
        }

        public KeyValuePair<uint, string> PartyLeader { get; set; }

        public override bool IsValid()
        {
            return ActorIdTools.RangeOkay(PartyLeader.Key);
        }
    }
}