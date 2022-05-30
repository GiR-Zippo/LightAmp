namespace BardMusicPlayer.Seer.Events
{
    public sealed class LatencyUpdate : SeerEvent
    {
        internal LatencyUpdate(EventSource readerBackendType, long milis) : base(readerBackendType, 100, true)
        {
            EventType = GetType();
            LatencyMilis = milis;
        }

        public long LatencyMilis { get; }
        public override bool IsValid() => true;
    }
}