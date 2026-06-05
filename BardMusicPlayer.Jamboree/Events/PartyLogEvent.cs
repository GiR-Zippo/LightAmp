/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyLogEvent : JamboreeEvent
    {
        internal PartyLogEvent(string logstring) : base(0, false)
        {
            EventType = GetType();
            LogString = logstring;
        }

        public string LogString { get; }

        public override bool IsValid() => true;
    }

}
