/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
*/

namespace BardMusicPlayer.Dalamud.Events
{
    public sealed class VoiceVolumeMuteEvent : DalamudBridgeEvent
    {
        internal VoiceVolumeMuteEvent(int pid, bool state) : base(100, true)
        {
            EventType = GetType();
            PId = pid;
            State = state;
        }

        public int PId { get; }
        public bool State { get; }

        public override bool IsValid()
        {
            return true;
        }
    }
}