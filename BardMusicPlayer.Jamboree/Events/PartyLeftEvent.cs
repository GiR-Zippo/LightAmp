/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyLeftEvent : JamboreeEvent
    {
        internal PartyLeftEvent(bool wasHost) : base(0, false)
        {
            EventType = GetType();
            WasHost = wasHost;
        }

        public bool WasHost { get; }
        public override bool IsValid() => true;
    }
}
