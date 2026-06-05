/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyCreatedEvent : JamboreeEvent
    {
        internal PartyCreatedEvent(SessionCreated data) : base(0, false)
        {
            EventType = GetType();
            Data = data;
        }

        public SessionCreated Data { get; }

        public override bool IsValid() => true;
    }
}
