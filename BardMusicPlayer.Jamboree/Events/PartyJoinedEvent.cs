namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyJoinedEvent : JamboreeEvent
    {
        internal PartyJoinedEvent(MemberStateResponse data) : base(0, false)
        {
            EventType = GetType();
            Data = data;
        }

        public MemberStateResponse Data { get; }

        public override bool IsValid() => true;
    }
}
