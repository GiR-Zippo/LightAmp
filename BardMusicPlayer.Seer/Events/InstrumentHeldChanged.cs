/*
 * Copyright(c) 2023 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

#region

using BardMusicPlayer.Quotidian.Structs;

#endregion

namespace BardMusicPlayer.Seer.Events
{
    public sealed class InstrumentHeldChanged : SeerEvent
    {
        internal InstrumentHeldChanged(EventSource readerBackendType, Instrument instrumentHeld) : base(
            readerBackendType)
        {
            EventType = GetType();
            InstrumentHeld = instrumentHeld;
        }

        public Instrument InstrumentHeld { get; }

        public override bool IsValid()
        {
            return true;
        }
    }
}