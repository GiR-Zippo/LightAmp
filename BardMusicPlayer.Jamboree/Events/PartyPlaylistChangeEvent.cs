/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Collections.Generic;

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyPlaylistChangeEvent : JamboreeEvent
    {
        internal PartyPlaylistChangeEvent(List<PlaylistItem> playlist) : base(0, false)
        {
            EventType = GetType();
            Playlist = playlist;
        }

        public List<PlaylistItem> Playlist { get; }

        public override bool IsValid() => true;
    }
}
