/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
*/

namespace BardMusicPlayer.Dalamud.Events
{
    public sealed class MasterVolumeChangedEvent : DalamudBridgeEvent
    {
        internal MasterVolumeChangedEvent(int pid, short volume) : base(100, true)
        {
            EventType = GetType();
            PId = pid;
            MasterVolume = volume;
        }

        public int PId { get; }
        public short MasterVolume { get; }

        public override bool IsValid()
        {
            return true;
        }
    }
}