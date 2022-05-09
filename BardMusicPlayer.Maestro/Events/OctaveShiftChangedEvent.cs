using BardMusicPlayer.Seer;

namespace BardMusicPlayer.Maestro.Events
{
    public sealed class OctaveShiftChangedEvent : MaestroEvent
    {
        internal OctaveShiftChangedEvent(Game g, int octaveShift, bool isHost=false) : base(0, false)
        {
            EventType = GetType();
            OctaveShift = octaveShift;
            game = g;
            IsHost = isHost;
        }

        public Game game { get; }
        public int OctaveShift { get; }
        public bool IsHost { get; }
        public override bool IsValid() => true;
    }

}
