namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyPlaylistSendEvent : JamboreeEvent
    {
        internal PartyPlaylistSendEvent(PlaylistResponse data) : base(0, false)
        {
            EventType = GetType();
            Data = data;
        }

        public PlaylistResponse Data { get; }

        public override bool IsValid() => true;
    }
}
