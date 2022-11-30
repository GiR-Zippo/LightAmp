using System.Threading;
using System.Threading.Tasks;
using BardMusicPlayer.DalamudBridge.Helper.Dalamud;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Seer;

namespace BardMusicPlayer.DalamudBridge
{
    public static partial class GameExtensions
    {
        private static readonly SemaphoreSlim LyricSemaphoreSlim = new (1,1);

        public static bool IsConnected(int pid)
        {
            return DalamudBridge.Instance.DalamudServer.IsConnected(pid);
        }

        /// <summary>
        /// Sends a lyric line via say
        /// </summary>
        /// <param name="game"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Task<bool> SendLyricLine(this Game game, string text)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            if (DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid))
                return Task.FromResult(DalamudBridge.Instance.DalamudServer.SendChat(game.Pid, ChatMessageChannelType.Say, text));
            return Task.FromResult(false);
        }

        /// <summary>
        /// sends a text in chat without interrupting playback
        /// </summary>
        /// <param name="game"></param>
        /// <param name="type"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Task<bool> SendText(this Game game, ChatMessageChannelType type, string text)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            if (DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid))
                return Task.FromResult(DalamudBridge.Instance.DalamudServer.SendChat(game.Pid, type, text));
            return Task.FromResult(false);
        }

        /// <summary>
        /// Open or close an instrument
        /// </summary>
        /// <param name="game"></param>
        /// <param name="instrumentID"></param>
        /// <returns></returns>
        public static Task<bool> OpenInstrument(this Game game, int instrumentID)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            if (DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid))
                return Task.FromResult(DalamudBridge.Instance.DalamudServer.SendInstrumentOpen(game.Pid, instrumentID));
            return Task.FromResult(false);
        }

        /// <summary>
        /// Accept the ens request
        /// </summary>
        /// <param name="game"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static Task<bool> AcceptEnsemble(this Game game, bool arg)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            if (DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid))
                return Task.FromResult(DalamudBridge.Instance.DalamudServer.SendAcceptEnsemble(game.Pid, arg));
            return Task.FromResult(false);
        }

        /// <summary>
        /// Sets the objects to low or max
        /// </summary>
        /// <param name="game"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static Task<bool> GfxSetLow(this Game game, bool low)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            if (DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid))
                return Task.FromResult(DalamudBridge.Instance.DalamudServer.SendGfxLow(game.Pid, low));
            return Task.FromResult(false);
        }

        /// <summary>
        /// starts the ensemble check
        /// </summary>
        /// <param name="game"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static Task<bool> StartEnsemble(this Game game)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            if (DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid))
                return Task.FromResult(DalamudBridge.Instance.DalamudServer.SendStartEnsemble(game.Pid));
            return Task.FromResult(false);
        }
    }
}
