/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Net;

namespace BardMusicPlayer.XIVMIDI.Events
{
    public sealed class XIVMidiUploadResponseEvent : XIVMidiEvent
    {
        internal XIVMidiUploadResponseEvent(HttpStatusCode statusCode) : base(0, false)
        {
            EventType = GetType();
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
        public override bool IsValid() => true;
    }
}
