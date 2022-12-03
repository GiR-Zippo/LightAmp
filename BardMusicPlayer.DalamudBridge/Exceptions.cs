using BardMusicPlayer.Quotidian;

namespace BardMusicPlayer.DalamudBridge
{
    public sealed class DalamudBridgeException : BmpException
    {
        internal DalamudBridgeException() : base()
        {
        }
        internal DalamudBridgeException(string message) : base(message)
        {
        }
    }
}
