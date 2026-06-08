/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyChangedEvent : JamboreeEvent
    {
        internal PartyChangedEvent() : base(0, false)
        {
            EventType = GetType();
        }

        public override bool IsValid() => true;
    }
}
