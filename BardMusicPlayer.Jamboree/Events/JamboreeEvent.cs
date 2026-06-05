/*
 * Copyright(c) 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using BardMusicPlayer.Quotidian.UtcMilliTime;

namespace BardMusicPlayer.Jamboree.Events
{
    public abstract class JamboreeEvent
    {
        internal JamboreeEvent(byte dedupeThreshold = 0, bool highPriority = false)
        {
            DedupeThreshold = dedupeThreshold;
            HighPriority = highPriority;
            TimeStamp = Clock.Time.Now;
        }

        public long TimeStamp { get; }

        internal byte DedupeThreshold { get; }

        internal bool HighPriority { get; }

        public Type EventType { get; protected set; }

        /// <summary>
        /// Used to determine if the Reader was able to successfully obtain the
        /// data it was expecting to grab, and the Event is safe to use.
        /// </summary>
        /// <returns>True, if the Event should be used to update data.</returns>
        public abstract bool IsValid();
    }
}
