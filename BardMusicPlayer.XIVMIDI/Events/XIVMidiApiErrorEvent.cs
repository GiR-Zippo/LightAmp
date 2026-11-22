/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.XIVMIDI.Events
{
    public sealed class XIVMidiApiErrorEvent : XIVMidiEvent
    {
        internal XIVMidiApiErrorEvent(int errorCode, string data, object args, RequestKind requestKind) : base(0, false)
        {
            EventType = GetType();
            ErrorCode = errorCode;
            Message = data;
            Arguments = args;
            RequestKind = requestKind;
        }

        public object Arguments { get; }
        public int ErrorCode { get; }
        public string Message { get; }
        public RequestKind RequestKind { get; }
        public override bool IsValid() => true;
    }
}
