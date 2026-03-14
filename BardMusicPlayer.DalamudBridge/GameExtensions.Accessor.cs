/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Numerics;
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

        private static void checkBridge()
        {
           if (!DalamudBridge.Instance.Started) throw new DalamudBridgeException("DalamudBridge not started.");
        }

        /// <summary>
        /// sends a char logout
        /// </summary>
        /// <returns></returns>
        public static Task<bool> SendCharacterLogout(this Game game)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendCharacterLogout(game.Pid));
        }

        /// <summary>
        /// sends the game shutdown
        /// </summary>
        /// <returns></returns>
        public static Task<bool> SendGameShutdown(this Game game)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendGameShutdown(game.Pid));
        }

        /// <summary>
        /// sends a move to an absolute position
        /// </summary>
        /// <returns></returns>
        public static Task<bool> SendMoveToPosition(this Game game, Vector3 position, long rotation)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendMoveToPosition(game.Pid, position, rotation));
        }

        /// <summary>
        /// sends a stop move
        /// </summary>
        /// <returns></returns>
        public static Task<bool> SendMoveStop(this Game game)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SendMoveStop(game.Pid));
        }

        /// <summary>
        /// Sends a lyric line via say
        /// </summary>
        /// <param name="game"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static Task<bool> SendLyricLine(this Game game, string text)
        {
            checkBridge();

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
            checkBridge();

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
            checkBridge();

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
            checkBridge();

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
            checkBridge();

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
            checkBridge();

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
            checkBridge();

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
            checkBridge();

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
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SetMasterVolume(game.Pid, value));
        }


        /// <summary>
        /// send set render size
        /// </summary>
        /// <param name="game"></param>
        /// <param name="low"></param>
        /// <returns></returns>
        public static Task<bool> SetRenderSize(this Game game, uint width, uint height)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                                   DalamudBridge.Instance.DalamudServer.SetRenderSize(game.Pid, width, height));
        }

        /// <summary>
        /// starts the ensemble check
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        public static Task<bool> StartEnsemble(this Game game)
        {
            checkBridge();

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
            checkBridge();

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
            checkBridge();

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
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendProgchange(game.Pid, ProgNumber));
        }

        /// <summary>
        /// Send party invite to character
        /// </summary>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendPartyInvite(this Game game, string CharacterName, ushort HomeWorldId)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendPartyInvite(game.Pid, CharacterName, HomeWorldId));
        }

        /// <summary>
        /// Send enable accept party invite
        /// </summary>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendPartyAccept(this Game game)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendPartyAccept(game.Pid));
        }

        /// <summary>
        /// Send promote character to party lead
        /// </summary>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendPartyPromote(this Game game, string CharacterName)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendPartyPromote(game.Pid, CharacterName));
        }

        /// <summary>
        /// Send enter the house in front
        /// </summary>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendPartyEnterHouse(this Game game)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendPartyEnterHouse(game.Pid));
        }

        /// <summary>
        /// Send party teleport
        /// </summary>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendPartyTeleport(this Game game, bool partyLead)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendPartyTeleport(game.Pid, partyLead));
        }

        /// <summary>
        /// Send party teleport
        /// </summary>
        /// <exception cref="DalamudBridgeException"></exception>
        public static Task<bool> SendPartyFollowMe(this Game game, string Character, uint homeWorldId)
        {
            checkBridge();

            return Task.FromResult(DalamudBridge.Instance.DalamudServer.IsConnected(game.Pid) &&
                       DalamudBridge.Instance.DalamudServer.SendPartyFollowMe(game.Pid, Character, homeWorldId));
        }
    }
}
