/*
 * Copyright(c) 2024 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Threading;
using System.Threading.Tasks;
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

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendChat(game.Pid, ChatMessageChannelType.Say,
                                       text));
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

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendChat(game.Pid, type, text));
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

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendInstrumentOpen(game.Pid, instrumentID));
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

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendAcceptEnsemble(game.Pid, arg));
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

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendGfxLow(game.Pid, low));
        }

        /// <summary>
        /// Sets the sound
        /// </summary>
        /// <param name="game"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static Task<bool> SetSoundOnOff(this Game game, bool on)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendSoundOnOff(game.Pid, on));
        }

        /// <summary>
        /// Sets the voice mute
        /// </summary>
        /// <param name="game"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static Task<bool> SetVoiceOnOff(this Game game, bool on)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendVoiceOnOff(game.Pid, on));
        }

        /// <summary>
        /// Sets the effect mute
        /// </summary>
        /// <param name="game"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static Task<bool> SetEffectOnOff(this Game game, bool on)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendEffectOnOff(game.Pid, on));
        }
        /// <summary>
        /// Sets the master volume sound
        /// </summary>
        /// <param name="game"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static Task<bool> SetMasterVolume(this Game game, short value)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SetMasterVolume(game.Pid, value));
        }

        /// <summary>
        /// starts the ensemble check
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static Task<bool> StartEnsemble(this Game game)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendStartEnsemble(game.Pid));
        }

        /// <summary>
        /// quits the client
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static Task<bool> TerminateClient(this Game game)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendQuitClient(game.Pid));
        }

        /// <summary>
        /// Send the note and if it's pressed or released
        /// </summary>
        /// <param name="game"></param>
        /// <param name="noteNum"></param>
        /// <param name="pressed"></param>
        /// <returns></returns>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendNote(this Game game, int noteNum, bool pressed)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendNote(game.Pid, noteNum, pressed));
        }

        /// <summary>
        /// Send the program change
        /// </summary>
        /// <param name="game"></param>
        /// <param name="ProgNumber"></param>
        /// <returns></returns>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendProgchange(this Game game, int ProgNumber)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendProgchange(game.Pid, ProgNumber));
        }

        /// <summary>
        /// Send party invite to character
        /// </summary>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendPartyInvite(this Game game, string CharacterName, ushort HomeWorldId)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendPartyInvite(game.Pid, CharacterName, HomeWorldId));
        }

        /// <summary>
        /// Send enable accept party invite
        /// </summary>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendPartyAccept(this Game game)
        {
            if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendPartyAccept(game.Pid));
        }
    }
}
