/*
 * Copyright(c) 2025 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Seer;
using H.Formatters;
using H.Pipes;
using H.Pipes.AccessControl;
using H.Pipes.Args;
using BardMusicPlayer.Seer.Utilities;
using BardMusicPlayer.Dalamud.Events;
using System.Numerics;

namespace BardMusicPlayer.DalamudBridge.Helper.Dalamud
{
    public sealed class Message
    {
        public MessageType msgType { get; set; } = MessageType.None;
        public int  msgChannel { get; set; } = 0;
        public string message { get; set; } = "";
    }

    internal sealed class DalamudServer : IDisposable
    {
        private readonly PipeServer<Message> _pipe;
        private readonly ConcurrentDictionary<int, string> _clients;
        private readonly Version minVersion = new Version("0.0.2.5");

        /// <summary>
        /// 
        /// </summary>
        internal DalamudServer()
        {
            _clients = new ConcurrentDictionary<int, string>();
            _pipe = new PipeServer<Message>("Hypnotoad", new NewtonsoftJsonFormatter());
            _pipe.ClientConnected += OnConnected;
            _pipe.ClientDisconnected += OnDisconnected;
            _pipe.MessageReceived += OnMessage;
            _pipe.AllowUsersReadWrite();
            Start();
        }

        internal async void Start()
        {
            if (_pipe.IsStarted) return;
            await _pipe.StartAsync();
        }

        internal async void Stop()
        {
            if (!_pipe.IsStarted) return;
            await _pipe.StopAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        internal bool IsConnected(int pid) => _clients.ContainsKey(pid) && _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected) is not null;

        /// <summary>
        /// sends a char logout
        /// </summary>
        /// <returns></returns>
        internal bool SendCharacterLogout(int pid)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.ClientLogout
            });
            return true;
        }

        /// <summary>
        /// sends the game shutdown
        /// </summary>
        /// <returns></returns>
        internal bool SendGameShutdown(int pid)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.GameShutdown
            });
            return true;
        }

        /// <summary>
        /// sends a move to an absolute position
        /// </summary>
        /// <returns></returns>
        internal bool SendMoveToPosition(int pid, Vector3 position, long rotation)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.MoveCharToPosition,
                message = position.ToString() + ";" + rotation
            });
            return true;
        }

        /// <summary>
        /// sends a stop move
        /// </summary>
        /// <returns></returns>
        internal bool SendMoveStop(int pid)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.StopMovement
            });
            return true;
        }

        /// <summary>
        /// send a text to the toad, to type during playback
        /// </summary>
        /// <param name="pid">proc Id</param>
        /// <param name="chanType">channel type</param>
        /// <param name="text">the message</param>
        /// <returns></returns>
        internal bool SendChat(int pid, ChatMessageChannelType chanType, string text)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.Chat,
                msgChannel = chanType.ChannelCode,
                message = text
            });
            return true;
        }

        /// <summary>
        /// Send intrument open action to the toad
        /// </summary>
        /// <param name="pid">proc Id</param>
        /// <param name="instrumentID">XIV instrument numer</param>
        /// <returns></returns>
        internal bool SendInstrumentOpen(int pid, int instrumentID)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.Instrument,
                message = instrumentID.ToString()
            });
            return true;
        }

        /// <summary>
        /// accept ensemble request action
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool SendAcceptEnsemble(int pid, bool arg)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.AcceptReply,
                message = ""
            });
            return true;
        }

        /// <summary>
        /// switch gfx to low and back
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool SendGfxLow(int pid, bool arg)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.SetGfx,
                message = arg.ToString()
            });
            return true;
        }

        /// <summary>
        /// switch sound on/off
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool SendSoundOnOff(int pid, bool arg)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.MasterSoundState,
                message = arg.ToString()
            });
            return true;
        }

        /// <summary>
        /// switch voice on/off
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool SendVoiceOnOff(int pid, bool arg)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.VoiceSoundState,
                message = arg.ToString()
            });
            return true;
        }

        /// <summary>
        /// switch effect on/off
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool SendEffectOnOff(int pid, bool arg)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.EffectsSoundState,
                message = arg.ToString()
            });
            return true;
        }

        /// <summary>
        /// send master volume
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool SetMasterVolume(int pid, short arg)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.MasterVolume,
                message = arg.ToString()
            });
            return true;
        }

        /// <summary>
        /// send set render size
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        internal bool SetRenderSize(int pid, uint width, uint height)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.SetWindowRenderSize,
                message = width.ToString() + ";" + height.ToString()
            });
            return true;
        }

        /// <summary>
        /// Send ensemble start
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        internal bool SendStartEnsemble(int pid)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.StartEnsemble,
                message = ""
            });
            return true;
        }

        /// <summary>
        /// Send the note and if it's pressed or released
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="note"></param>
        /// <param name="pressed"></param>
        /// <returns></returns>
        internal bool SendNote(int pid, int note, bool pressed)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = pressed ? MessageType.NoteOn : MessageType.NoteOff,
                message = note.ToString()
            });
            return true;
        }

        /// <summary>
        /// Send the progchange
        /// </summary>
        /// <param name="pid"></param>
        /// <param name="ProgNumber"></param>
        /// <returns></returns>
        internal bool SendProgchange(int pid, int ProgNumber)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.ProgramChange,
                message = ProgNumber.ToString()
            });
            return true;
        }

        /// <summary>
        /// Send ensemble start
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        internal bool SendQuitClient(int pid)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.ExitGame,
                message = ""
            });
            return true;
        }

        /// <summary>
        /// Send party invite to character
        /// </summary>
        internal bool SendPartyInvite(int pid, string CharacterName, ushort HomeWorldId)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.PartyInvite,
                message = CharacterName +";"+ HomeWorldId.ToString()
            });
            return true;
        }

        /// <summary>
        /// Send party invite to character
        /// </summary>
        internal bool SendPartyAccept(int pid)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.PartyInviteAccept
            });
            return true;
        }

        /// <summary>
        /// Send promote char to lead
        /// </summary>
        internal bool SendPartyPromote(int pid, string CharacterName)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.PartyPromote,
                message = CharacterName
            });
            return true;
        }

        /// <summary>
        /// Send party invite to character
        /// </summary>
        internal bool SendPartyEnterHouse(int pid)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.PartyEnterHouse
            });
            return true;
        }

        /// <summary>
        /// Send party teleport
        /// </summary>
        internal bool SendPartyTeleport(int pid, bool partyLead)
        {
            if (!IsConnected(pid))
                return false;

            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
            {
                msgType = MessageType.PartyTeleport,
                message = partyLead.ToString()
            });
            return true;
        }

        /// <summary>
        /// Send party follow
        /// </summary>
        internal bool SendPartyFollowMe(int pid, string Character, uint homeWorldId)
        {
            if (!IsConnected(pid))
                return false;

            if (Character == "")
                _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
                {
                    msgType = MessageType.PartyFollow
                });
            else
                _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(new Message
                {
                    msgType = MessageType.PartyFollow,
                    message = Character+";"+homeWorldId.ToString()
                });
            return true;
        }

        

        /// <summary>
        /// If message from client rec
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessage(object? sender, ConnectionMessageEventArgs<Message?> e)
        {
            Message? inMsg = e.Message as Message;
            if (inMsg == null)
                return;

            switch (inMsg.msgType)
            {
                case MessageType.Handshake:
                {
                    var t = Convert.ToInt32(inMsg.message);
                    _clients.TryAdd(t, e.Connection.PipeName);
                    Debug.WriteLine($"Dalamud client Id {e.Connection.PipeName} {t} connected");
                    break;
                }
                case MessageType.Version:
                    try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        var version = new Version(t.Split(':')[1]);
                        if (version < minVersion)
                        {
                            _pipe.ConnectedClients.FirstOrDefault(x => x.PipeName == _clients[pid] && x.IsConnected)?.WriteAsync(
                                new Message
                                {
                                    msgType = MessageType.Version,
                                    message = minVersion.ToString()
                                });
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    break;
                case MessageType.SetGfx:
                   try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        var lowsettings = Convert.ToBoolean(t.Split(':')[1]);
                        if (BmpSeer.Instance.Games.ContainsKey(pid))
                            BmpSeer.Instance.Games[pid].GfxSettingsLow = lowsettings;
                    }
                    catch { }
                    break;
                case MessageType.MasterSoundState:
                    try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        if (BmpSeer.Instance.Games.ContainsKey(pid))
                            DalamudBridge.Instance.PublishResponseEvent(new MasterVolumeMuteEvent(pid, Convert.ToBoolean(t.Split(':')[1])));
                    }
                    catch { }
                    break;
                case MessageType.VoiceSoundState:
                    try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        if (BmpSeer.Instance.Games.ContainsKey(pid))
                            DalamudBridge.Instance.PublishResponseEvent(new VoiceVolumeMuteEvent(pid, Convert.ToBoolean(t.Split(':')[1])));
                    }
                    catch { }
                    break;
                case MessageType.EffectsSoundState:
                    try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        if (BmpSeer.Instance.Games.ContainsKey(pid))
                            DalamudBridge.Instance.PublishResponseEvent(new EffectVolumeMuteEvent(pid, Convert.ToBoolean(t.Split(':')[1])));
                    }
                    catch { }
                    break;
                case MessageType.MasterVolume:
                    try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        if (BmpSeer.Instance.Games.ContainsKey(pid))
                            DalamudBridge.Instance.PublishResponseEvent(new MasterVolumeChangedEvent(pid, Convert.ToInt16(t.Split(':')[1])));
                    }
                    catch { }
                    break;
                case MessageType.NameAndHomeWorld:
                    try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        var Name = t.Split(':')[1];
                        var HomeWorld = Convert.ToInt32(t.Split(':')[2]);
                        if (BmpSeer.Instance.Games.ContainsKey(pid))
                            DalamudManager.Instance.NameAndHomeworldModeEventHandler(pid, Name, HomeWorld);
                    }
                    catch { }
                    break;
                case MessageType.StartEnsemble:
                    try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        var action = Convert.ToInt32(t.Split(':')[0]);
                        if (BmpSeer.Instance.Games.ContainsKey(pid))
                            DalamudManager.Instance.EnsembleStartEventHandler(pid, action);

                    }
                    catch { }
                    break;
                case MessageType.PerformanceModeState:
                    try
                    {
                        var t = inMsg.message;
                        var pid = Convert.ToInt32(t.Split(':')[0]);
                        var action = Convert.ToInt32(t.Split(':')[0]);
                        if (BmpSeer.Instance.Games.ContainsKey(pid))
                            DalamudManager.Instance.PerformanceModeEventHandler(pid, action);
                    }
                    catch { }
                    break;
            }

        }

        /// <summary>
        /// If client disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDisconnected(object? sender, ConnectionEventArgs<Message> e)
        {
            if (_clients.Values.Contains(e.Connection.PipeName)) 
                _clients.TryRemove(_clients.FirstOrDefault(x => x.Value == e.Connection.PipeName).Key, out _);
            Debug.WriteLine($"Dalamud client Id {e.Connection.PipeName} disconnected");
        }

        /// <summary>
        /// If connected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnConnected(object? sender, ConnectionEventArgs<Message> e)
        {
            Debug.WriteLine($"Dalamud client Id {e.Connection.PipeName} connected");
        }

        /// <summary>
        /// Disposing
        /// </summary>
        public void Dispose()
        {
            try
            {
                Stop();
                _pipe.MessageReceived -= OnMessage;
                _pipe.ClientConnected -= OnDisconnected;
                _pipe.ClientDisconnected -= OnConnected;
                _pipe.DisposeAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Dalamud error: {ex.Message}");
            }
            _clients.Clear();
        }
    }
}
