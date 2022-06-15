namespace BardMusicPlayer.Maestro.Events
{
    public sealed class PerformerUpdate : MaestroEvent
    {

        internal PerformerUpdate() : base(0, false)
        {
            EventType = GetType();
        }

        public override bool IsValid() => true;
    }
}
