/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.DalamudBridge
{
    public sealed partial class DalamudBridge
    {
        private ConcurrentQueue<DalamudBridgeCommandStruct> _commandQueue;
        private bool _commandQueueOpen;

        private async Task RunCommandEventsHandler(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (_commandQueue.TryDequeue(out var d_command))
                {
                    if (token.IsCancellationRequested)
                        break;

                    try
                    {
                        switch (d_command.messageType)
                        {
                            case Helper.Dalamud.MessageType.Chat:
                                await GameExtensions.SendText(d_command.game, d_command.chatType, d_command.TextData);
                                break;
                            case Helper.Dalamud.MessageType.Instrument:
                                await GameExtensions.OpenInstrument(d_command.game, d_command.IntData);
                                break;
                            case Helper.Dalamud.MessageType.AcceptReply:
                                await GameExtensions.AcceptEnsemble(d_command.game, d_command.BoolData);
                                break;
                            case Helper.Dalamud.MessageType.NoteOn:
                            case Helper.Dalamud.MessageType.NoteOff:
                                _ = GameExtensions.SendNote(d_command.game, d_command.IntData, d_command.BoolData);
                                break;
                            case Helper.Dalamud.MessageType.ProgramChange:
                                _ = GameExtensions.SendProgchange(d_command.game, d_command.IntData);
                                break;
                        };
                    }
                    catch
                    { }
                }
                await Task.Delay(1, token).ContinueWith(static tsk => { }, token);
            }
        }

        private CancellationTokenSource _commandTokenSource;

        private void StartCommandEventsHandler()
        {
            _commandQueue = new ConcurrentQueue<DalamudBridgeCommandStruct>();
            _commandTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() => RunCommandEventsHandler(_commandTokenSource.Token), TaskCreationOptions.LongRunning);
            _commandQueueOpen = true;
        }

        private void StopCommandEventsHandler()
        {
            _commandQueueOpen = false;
            _commandTokenSource.Cancel();
            while (_commandQueue.TryDequeue(out _))
            {
            }
        }

        internal void PublishCommandEvent(DalamudBridgeCommandStruct commandEvent)
        {
            if (!_commandQueueOpen)
                return;

            _commandQueue.Enqueue(commandEvent);
        }
    }
}
