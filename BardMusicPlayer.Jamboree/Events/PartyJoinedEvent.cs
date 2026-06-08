/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyJoinedEvent : JamboreeEvent
    {
        internal PartyJoinedEvent(bool connected, string code) : base(0, false)
        {
            EventType = GetType();
            Connected = connected;
            Code = code;
        }

        public bool Connected { get; }
        public string Code { get; }

        public override bool IsValid() => true;
    }
}
