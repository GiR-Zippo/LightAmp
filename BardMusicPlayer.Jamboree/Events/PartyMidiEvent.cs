/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyMidiEvent : JamboreeEvent
    {
        internal PartyMidiEvent(string id, byte[] data) : base(0, false)
        {
            EventType = GetType();
            fileId = id;
            Data = data;
        }

        public string fileId { get; }
        public byte[] Data { get; }

        public override bool IsValid() => true;
    }
}
