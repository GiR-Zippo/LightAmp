/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartySelectSongEvent : JamboreeEvent
    {
        internal PartySelectSongEvent(string songid) : base(0, false)
        {
            EventType = GetType();
            SongId = songid;
        }

        public string SongId { get; }

        public override bool IsValid() => true;
    }
}
