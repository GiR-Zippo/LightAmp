/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    /// <summary>
    /// Called only on host side
    /// </summary>
    public sealed class PartyDebugLogEvent : JamboreeEvent
    {
        /// <summary>
        /// on host, when a party and token was created
        /// </summary>
        /// <param name="token"></param>
        internal PartyDebugLogEvent(string logstring) : base(0, false)
        {
            EventType = GetType();
            LogString = logstring;
        }

        /// <summary>
        /// the base64 token for the clients to join
        /// </summary>
        public string LogString { get; }

        public override bool IsValid() => true;
    }

}
