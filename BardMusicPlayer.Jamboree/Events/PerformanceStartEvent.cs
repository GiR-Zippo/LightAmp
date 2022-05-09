using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PerformanceStartEvent : JamboreeEvent
    {
        /// <summary>
        /// Start the performance received
        /// </summary>
        /// <param name="timestampinMillis">in milliseconds</param>
        internal PerformanceStartEvent(long timestampinMillis) : base(0, false)
        {
            EventType = GetType();
            SenderTimestamp_in_millis = timestampinMillis;
        }

        /// <summary>
        /// The host time in milis
        /// </summary>
        public long SenderTimestamp_in_millis { get; }

        public override bool IsValid() => true;
    }

}
