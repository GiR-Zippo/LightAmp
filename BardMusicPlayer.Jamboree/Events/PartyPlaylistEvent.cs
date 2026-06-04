namespace BardMusicPlayer.Jamboree.Events
{
    public sealed class PartyPlaylistEvent : JamboreeEvent
    {
        internal PartyPlaylistEvent(PlaylistManifest data) : base(0, false)
        {
            EventType = GetType();
            Data = data;
        }

        public PlaylistManifest Data { get; }

        public override bool IsValid() => true;
    }
}
