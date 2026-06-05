/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class SessionManifestEvent : JamboreeEvent
    {
        internal SessionManifestEvent(SessionManifest data) : base(0, false)
        {
            EventType = GetType();
            Data = data;
        }

        public SessionManifest Data { get; }

        public override bool IsValid() => true;
    }
}
