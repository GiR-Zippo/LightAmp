﻿/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Seer;

namespace BardMusicPlayer.Maestro.Events
{
    public sealed class TrackNumberChangedEvent : MaestroEvent
    {
        internal TrackNumberChangedEvent(Game g, int trackNumber, bool isHost=false) : base(0, false)
        {
            EventType = GetType();
            TrackNumber = trackNumber;
            game = g;
            IsHost = isHost;
        }

        public Game game { get; }
        public int TrackNumber { get; }
        public bool IsHost { get; }
        public override bool IsValid() => true;
    }

}
