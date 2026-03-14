/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Dalamud.Events;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.DalamudBridge
{
    public sealed partial class DalamudBridge
    {
        public EventHandler<MasterVolumeChangedEvent> OnMasterVolumeChangedEvent;
        public EventHandler<MasterVolumeMuteEvent> OnMasterVolumeMuteEvent;
        public EventHandler<VoiceVolumeMuteEvent> OnVoiceVolumeMuteEvent;
        public EventHandler<EffectVolumeMuteEvent> OnEffectVolumeMuteEvent;

        private ConcurrentQueue<DalamudBridgeEvent> _eventQueue;
        private bool _eventQueueOpen;

        private async Task RunEventsHandler(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (_eventQueue.TryDequeue(out var meastroEvent))
                {
                    if (token.IsCancellationRequested)
                        break;

                    try
                    {
                        switch (meastroEvent)
                        {
                            case MasterVolumeChangedEvent masterVolumeChangedEvent:
                                if (OnMasterVolumeChangedEvent != null)
                                    OnMasterVolumeChangedEvent(this, masterVolumeChangedEvent);
                                break;
                            case MasterVolumeMuteEvent masterVolumeMuteEvent:
                                if (OnMasterVolumeMuteEvent != null)
                                    OnMasterVolumeMuteEvent(this, masterVolumeMuteEvent);
                                break;
                            case VoiceVolumeMuteEvent voiceVolumeMuteEvent:
                                if (OnVoiceVolumeMuteEvent != null)
                                    OnVoiceVolumeMuteEvent(this, voiceVolumeMuteEvent);
                                break;
                            case EffectVolumeMuteEvent effectVolumeMuteEvent:
                                if (OnEffectVolumeMuteEvent != null)
                                    OnEffectVolumeMuteEvent(this, effectVolumeMuteEvent);
                                break;
                        };
                    }
                    catch
                    { }
                }
                await Task.Delay(25, token).ContinueWith(tsk=> { });
            }
        }

        private CancellationTokenSource _eventsTokenSource;

        private void StartResponeEventsHandler()
        {
            _eventQueue = new ConcurrentQueue<DalamudBridgeEvent>();
            _eventsTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() => RunEventsHandler(_eventsTokenSource.Token), TaskCreationOptions.LongRunning);
            _eventQueueOpen = true;
        }

        private void StopResponseEventsHandler()
        {
            _eventQueueOpen = false;
            _eventsTokenSource.Cancel();
            while (_eventQueue.TryDequeue(out _))
            {
            }
        }

        internal void PublishResponseEvent(DalamudBridgeEvent dalamudEvent)
        {
            if (!_eventQueueOpen)
                return;

            _eventQueue.Enqueue(dalamudEvent);
        }
    }
}
