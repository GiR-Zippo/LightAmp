/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.XIVMIDI.Events
{
    public sealed class XIVMidiApiErrorEvent : XIVMidiEvent
    {
        internal XIVMidiApiErrorEvent(int errorCode, string data) : base(0, false)
        {
            EventType = GetType();
            ErrorCode = errorCode;
            Message = data;
        }

        public int ErrorCode { get; }
        public string Message { get; }
        public override bool IsValid() => true;
    }
}
