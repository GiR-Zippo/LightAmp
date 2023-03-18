/*
 * Copyright(c) 2023 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

#region

using BardMusicPlayer.Seer.Utilities;

#endregion

namespace BardMusicPlayer.Seer.Events
{
    public sealed class ActorIdChanged : SeerEvent
    {
        internal ActorIdChanged(EventSource readerBackendType, uint actorId) : base(readerBackendType)
        {
            EventType = GetType();
            ActorId = actorId;
        }

        public uint ActorId { get; }

        public override bool IsValid()
        {
            return ActorIdTools.RangeOkay(ActorId);
        }
    }
}