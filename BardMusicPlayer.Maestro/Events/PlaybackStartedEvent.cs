namespace BardMusicPlayer.Maestro.Events
{
    public sealed class PlaybackStartedEvent : MaestroEvent
    {

        internal PlaybackStartedEvent() : base(0, false)
        {
            EventType = GetType();
            Started = true;
        }

        public bool Started;
        public override bool IsValid() => true;
    }
}
