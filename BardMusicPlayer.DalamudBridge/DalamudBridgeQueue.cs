/*
 * Copyright(c) 2023 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.DalamudBridge
{
    public sealed partial class DalamudBridge
    {
        private ConcurrentQueue<DalamudBridgeCommandStruct> _eventQueue;
        private bool _eventQueueOpen;

        private async Task RunEventsHandler(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (_eventQueue.TryDequeue(out var d_event))
                {
                    if (token.IsCancellationRequested)
                        break;

                    try
                    {
                        switch (d_event.messageType)
                        {
                            case Helper.Dalamud.MessageType.Chat:
                                await GameExtensions.SendText(d_event.game, d_event.chatType, d_event.TextData);
                                break;
                            case Helper.Dalamud.MessageType.Instrument:
                                await GameExtensions.OpenInstrument(d_event.game, d_event.IntData);
                                break;
                            case Helper.Dalamud.MessageType.AcceptReply:
                                await GameExtensions.AcceptEnsemble(d_event.game, d_event.BoolData);
                                break;
                            case Helper.Dalamud.MessageType.NoteOn:
                            case Helper.Dalamud.MessageType.NoteOff:
                                _ = GameExtensions.SendNote(d_event.game, d_event.IntData, d_event.BoolData);
                                break;
                            case Helper.Dalamud.MessageType.ProgramChange:
                                _ = GameExtensions.SendProgchange(d_event.game, d_event.IntData);
                                break;
                        };
                    }
                    catch
                    { }
                }
                await Task.Delay(1, token).ContinueWith(static tsk => { }, token);
            }
        }

        private CancellationTokenSource _eventsTokenSource;

        private void StartEventsHandler()
        {
            _eventQueue = new ConcurrentQueue<DalamudBridgeCommandStruct>();
            _eventsTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() => RunEventsHandler(_eventsTokenSource.Token), TaskCreationOptions.LongRunning);
            _eventQueueOpen = true;
        }

        private void StopEventsHandler()
        {
            _eventQueueOpen = false;
            _eventsTokenSource.Cancel();
            while (_eventQueue.TryDequeue(out _))
            {
            }
        }

        internal void PublishEvent(DalamudBridgeCommandStruct meastroEvent)
        {
            if (!_eventQueueOpen)
                return;

            _eventQueue.Enqueue(meastroEvent);
        }
    }
}
