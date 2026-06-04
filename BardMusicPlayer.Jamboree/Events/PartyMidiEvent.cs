namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyMidiEvent : JamboreeEvent
    {
        internal PartyMidiEvent(byte[] data) : base(0, false)
        {
            EventType = GetType();
            Data = data;
        }

        public byte[] Data { get; }

        public override bool IsValid() => true;
    }
}
