﻿/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe, trotlinebeercan
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using BardMusicPlayer.Quotidian;
using BardMusicPlayer.Seer.Events;

namespace BardMusicPlayer.Seer
{
    public sealed partial class Game
    {
        private void OnEventReceived(SeerEvent seerEvent)
        {
            try
            {
                // make sure it's valid to begin with and from a backend.
                if (!seerEvent.IsValid() || 10 > (int)seerEvent.EventSource) return;

                seerEvent.Game = this;

                // pass exceptions up immediately
                if (seerEvent is BackendExceptionEvent backendExceptionEvent)
                {
                    BmpSeer.Instance.PublishEvent(backendExceptionEvent);
                    return;
                }

                // deduplicate if needed
                if (seerEvent.DedupeThreshold > 0)
                {
                    if (_eventDedupeHistory.ContainsKey(seerEvent.EventType) &&
                        _eventDedupeHistory[seerEvent.EventType] + seerEvent.DedupeThreshold >=
                        seerEvent.TimeStamp) return;

                    _eventDedupeHistory[seerEvent.EventType] = seerEvent.TimeStamp;
                }

                switch (seerEvent)
                {
                    case ActorIdChanged actorId:
                        if (ActorId != actorId.ActorId)
                        {
                            ActorId = actorId.ActorId;
                            BmpSeer.Instance.PublishEvent(actorId);
                        }

                        break;

                    case ChatLog chatLogEvent:
                        BmpSeer.Instance.PublishEvent(chatLogEvent);
                        break;

                    case ChatStatusChanged chatStatus:
                        if (ChatStatus != chatStatus.ChatStatus)
                        {
                            ChatStatus = chatStatus.ChatStatus;
                            BmpSeer.Instance.PublishEvent(chatStatus);
                        }

                        break;

                    case ConfigIdChanged configId:
                        if (!ConfigId.Equals(configId.ConfigId))
                        {
                            ConfigId = configId.ConfigId;
                            BmpSeer.Instance.PublishEvent(configId);
                        }

                        break;

                    case EnsembleRejected ensembleRejected:
                        BmpSeer.Instance.PublishEvent(ensembleRejected);
                        break;

                    case EnsembleRequested ensembleRequested:
                        BmpSeer.Instance.PublishEvent(ensembleRequested);
                        break;

                    case EnsembleStarted ensembleStarted:
                        BmpSeer.Instance.PublishEvent(ensembleStarted);
                        break;

                    case EnsembleStopped ensembleStopped:
                        BmpSeer.Instance.PublishEvent(ensembleStopped);
                        break;

                    case EnsembleStreamdata ensembleStreamdata:
                        BmpSeer.Instance.PublishEvent(ensembleStreamdata);
                        break;

                    case InstrumentHeldChanged instrumentHeld:
                        if (!InstrumentHeld.Equals(instrumentHeld.InstrumentHeld))
                        {
                            InstrumentHeld = instrumentHeld.InstrumentHeld;
                            BmpSeer.Instance.PublishEvent(instrumentHeld);
                        }
                        break;
                    case IsLoggedInChanged isLoggedIn:
                        if (IsLoggedIn != isLoggedIn.IsLoggedIn)
                        {
                            IsLoggedIn = isLoggedIn.IsLoggedIn;
                            BmpSeer.Instance.PublishEvent(isLoggedIn);
                        }

                        break;
                    case IsBardChanged isBard:
                        if (IsBard != isBard.IsBard)
                        {
                            IsBard = isBard.IsBard;
                            BmpSeer.Instance.PublishEvent(isBard);
                        }

                        break;

                    case KeyMapChanged keyMap:
                        if (!NavigationMenuKeys.Equals(keyMap.NavigationMenuKeys) ||
                            !InstrumentToneMenuKeys.Equals(keyMap.InstrumentToneMenuKeys) ||
                            !InstrumentKeys.Equals(keyMap.InstrumentKeys) ||
                            !InstrumentToneKeys.Equals(keyMap.InstrumentToneKeys) ||
                            !NoteKeys.Equals(keyMap.NoteKeys))
                        {
                            NavigationMenuKeys = keyMap.NavigationMenuKeys;
                            InstrumentToneMenuKeys = keyMap.InstrumentToneMenuKeys;
                            InstrumentKeys = keyMap.InstrumentKeys;
                            InstrumentToneKeys = keyMap.InstrumentToneKeys;
                            NoteKeys = keyMap.NoteKeys;
                            BmpSeer.Instance.PublishEvent(keyMap);
                        }

                        break;

                    case LatencyUpdate latencyUpdate:
                        ServerLatency = latencyUpdate.LatencyMilis;
                        break;

                    //Midibard things
                    case MidibardPlaylistEvent midibardPlaylistEvent:
                        BmpSeer.Instance.PublishEvent(midibardPlaylistEvent);
                        break;

                    case PartyMembersChanged partyMembers:
                        if (!PartyMembers.KeysEquals(partyMembers.PartyMembers))
                        {
                            PartyMembers = partyMembers.PartyMembers;
                            BmpSeer.Instance.PublishEvent(partyMembers);
                        }
                        break;

                    case PartyLeaderChanged partyLeader:
                        if (PartyLeader.Key != partyLeader.PartyLeader.Key)
                        {
                            PartyLeader = partyLeader.PartyLeader;
                            BmpSeer.Instance.PublishEvent(partyLeader);
                        }
                        break;
                    case PartyInvite partyInvite:
                        BmpSeer.Instance.PublishEvent(partyInvite);
                        break;
                    case PlayerNameChanged playerName:
                        if (!PlayerName.Equals(playerName.PlayerName))
                        {
                            PlayerName = playerName.PlayerName;
                            if (Pigeonhole.BmpPigeonhole.Instance.EnableMultibox)
                                SetClientWindowName(PlayerName+"@"+HomeWorld);
                            BmpSeer.Instance.PublishEvent(playerName);
                        }

                        break;

                    case HomeWorldChanged homeWorld:
                        if (!HomeWorld.Equals(homeWorld.HomeWorld))
                        {
                            HomeWorld = homeWorld.HomeWorld;
                            if (Pigeonhole.BmpPigeonhole.Instance.EnableMultibox)
                                SetClientWindowName(PlayerName + "@" + HomeWorld);
                            BmpSeer.Instance.PublishEvent(homeWorld);
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                BmpSeer.Instance.PublishEvent(new GameExceptionEvent(this, Pid, ex));
            }
        }
    }
}