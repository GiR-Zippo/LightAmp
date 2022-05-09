namespace BardMusicPlayer.Maestro.Events
{
    public sealed class PerformersChangedEvent : MaestroEvent
    {

        internal PerformersChangedEvent() : base(0, false)
        {
            EventType = GetType();
            Changed = true;
        }

        public bool Changed;
        public override bool IsValid() => true;
    }
}
