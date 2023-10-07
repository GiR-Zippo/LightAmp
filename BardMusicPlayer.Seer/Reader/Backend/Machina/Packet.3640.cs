/*
 * Copyright(c) 2023 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BardMusicPlayer.Seer.Events;

#endregion

namespace BardMusicPlayer.Seer.Reader.Backend.Machina
{
    internal sealed partial class Packet
    {
        /// <summary>
        ///     Contains contentId -> PlayerName.
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <param name="otherActorId"></param>
        /// <param name="myActorId"></param>
        /// <param name="message"></param>
        internal void Size3640(long timeStamp, uint otherActorId, uint myActorId, byte[] message)
        {
            try
            {
                if (otherActorId != myActorId) return;

                var myZoneId = uint.MaxValue;
                var myWorldId = uint.MaxValue;
                var partyMembers = new SortedDictionary<uint, string>();
                var currentPartyLead = new KeyValuePair<uint, string>();

                byte currentLeader = (byte)BitConverter.ToInt16(message, 3632);
                for (var i = 0; i <= 3136; i += 448)
                {
                    var actorId = BitConverter.ToUInt32(message, 80 + i);
                    if (actorId == 0)
                        continue; // This player is too far away for us to consider them "in party."

                    var playerName = Encoding.UTF8.GetString(message, 32 + i, 32).Trim((char)0);
                    uint currentZoneId = BitConverter.ToUInt16(message, 102 + i);
                    uint currentWorldId = BitConverter.ToUInt16(message, 104 + i);
                    switch (i)
                    {
                        case 0 when actorId != myActorId: // The first ActorId should always be this Game's ActorId.
                            return;
                        case 0 when actorId == myActorId: // Store location of this Game for lookup later.
                            myZoneId = currentZoneId;
                            myWorldId = currentWorldId;
                            break;
                        default:
                            if (myZoneId != currentZoneId || myWorldId != currentWorldId)
                                continue; // The player is in a different location.

                            break;
                    }

                    if (i/ 448 == currentLeader)
                        currentPartyLead = new KeyValuePair<uint, string>(actorId, playerName);

                    partyMembers.Add(actorId, playerName);
                }

                if (partyMembers.Count == 1)
                    // No party members nearby. Seer only accepts an empty collection for this case.
                    partyMembers.Clear();
                else
                    _machinaReader.Game.PublishEvent(new PartyLeaderChanged(EventSource.Machina, currentPartyLead));

                _machinaReader.Game.PublishEvent(new PartyMembersChanged(EventSource.Machina, partyMembers));
            }
            catch (Exception ex)
            {
                _machinaReader.Game.PublishEvent(new BackendExceptionEvent(EventSource.Machina,
                    new BmpSeerMachinaException("Exception in Packet.Size928 (party): " + ex.Message)));
            }
        }
    }
}