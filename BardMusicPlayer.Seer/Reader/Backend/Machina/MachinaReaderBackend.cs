/*
 * Copyright(c) 2023 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BardMusicPlayer.Seer.Events;
using BardMusicPlayer.Seer.Utilities;

namespace BardMusicPlayer.Seer.Reader.Backend.Machina
{
    internal sealed class MachinaReaderBackend : IReaderBackend
    {
        private ConcurrentQueue<byte[]> _messageQueue;
        private bool _messageQueueOpen;
        private Packet _packet;

        public MachinaReaderBackend(int sleepTimeInMs)
        {
            ReaderBackendType = EventSource.Machina;
            SleepTimeInMs = sleepTimeInMs;
        }

        public EventSource ReaderBackendType { get; }

        public ReaderHandler ReaderHandler { get; set; }

        public int SleepTimeInMs { get; set; }

        public async Task Loop(CancellationToken token)
        {
            _messageQueue = new ConcurrentQueue<byte[]>();
            _messageQueueOpen = true;
            _packet = new Packet(ReaderHandler);

            MachinaManager.Instance.MessageReceived += OnMessageReceived;
            MachinaManager.Instance.AddGame(ReaderHandler.Game.Pid);

            while (!token.IsCancellationRequested)
            {
                while (_messageQueue.TryDequeue(out var message))
                    try
                    {
                        var otherActorId = BitConverter.ToUInt32(message, 4);
                        var myActorId = BitConverter.ToUInt32(message, 8);
                        long timeStamp = BitConverter.ToUInt32(message, 24);
                        timeStamp *= 1000;

                        //string hexString = BitConverter.ToString(message);
                        //System.Diagnostics.Debug.WriteLine("MMMM" + hexString + " " + message.Length.ToString());
                        if (!(ActorIdTools.RangeOkay(myActorId) && ActorIdTools.RangeOkay(otherActorId))) 
                            continue;

                        if (myActorId == otherActorId)
                            ReaderHandler.Game.PublishEvent(new ActorIdChanged(EventSource.Machina, myActorId));

                        var Opcode = BitConverter.ToUInt16(message, 18); //implement if needed
                        switch (message.Length)
                        {
                            case 48:
                                _packet.Size48(timeStamp, otherActorId, myActorId, message); //[6.59]Handles Ensemble Stop
                                break;
                            case 56:
                                _packet.Size56(timeStamp, otherActorId, myActorId, message); //[6.59]Handles Ensemble Request, Ensemble Reject, and Instrument Equip/De-Equip.
                                break;
                            case 88:
                                _packet.Size88(timeStamp, otherActorId, myActorId, message); //[6.59]Handles EnsembleStart --DALAMUD
                                break;
                            case 656:
                                _packet.Size656(timeStamp, otherActorId, myActorId, message); //Handles old Homeworld and Playername --DALAMUD
                                break;
                            case 664:
                                _packet.Size664(timeStamp, otherActorId, myActorId, message); //[6.50]Handles Homeworld and Playername --DALAMUD
                                break;
                            case 672:
                                _packet.Size672(timeStamp, otherActorId, myActorId, message); //[6.59]Handles Homeworld and Playername --DALAMUD
                                break;
                            case 1064:
                                _packet.Size1064(timeStamp, otherActorId, myActorId, message); //[6.59]Handles Ensemble play data
                                break;
                            case 3576:
                                _packet.Size3576(timeStamp, otherActorId, myActorId, message);
                                break;
                            case 3640:
                                _packet.Size3640(timeStamp, otherActorId, myActorId, message); //[6.59]Handles group data
                                break;
                            default:
                                ReaderHandler.Game.PublishEvent(new BackendExceptionEvent(EventSource.Machina,
                                    new BmpSeerMachinaException("Unknown packet size: " + message.Length)));
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        ReaderHandler.Game.PublishEvent(new BackendExceptionEvent(EventSource.Machina, ex));
                    }

                await Task.Delay(SleepTimeInMs, token);
            }
        }

        public void Dispose()
        {
            _messageQueueOpen = false;
            MachinaManager.Instance.MessageReceived -= OnMessageReceived;
            MachinaManager.Instance.RemoveGame(ReaderHandler.Game.Pid);
            while (_messageQueue.TryDequeue(out _))
            {
            }

            _packet?.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnMessageReceived(int processId, byte[] message)
        {
            if (!_messageQueueOpen || ReaderHandler.Game.Pid != processId) return;

            _messageQueue.Enqueue(message);
        }
    }
}