namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyLogEvent : JamboreeEvent
    {
        internal PartyLogEvent(string logstring) : base(0, false)
        {
            EventType = GetType();
            LogString = logstring;
        }

        public string LogString { get; }

        public override bool IsValid() => true;
    }

}
