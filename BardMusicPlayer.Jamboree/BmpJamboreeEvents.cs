/*
 * Copyright(c) 2026 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree.Events;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.Jamboree
{
    public partial class BmpJamboree
    {
        public EventHandler<PartyChangedEvent> OnPartyChanged;
        public EventHandler<PartyCreatedEvent> OnPartyCreated;
        public EventHandler<PartyDebugLogEvent> OnPartyDebugLog;
        public EventHandler<PartyJoinedEvent> OnPartyJoined;
        public EventHandler<PartyLogEvent> OnPartyLog;
        public EventHandler<PartyPlaylistChangeEvent> OnPlaylistChangedEvent;
        public EventHandler<PartyPlaylistSendEvent> OnPartyPlaylistSend;
        public EventHandler<PartySelectSongEvent> OnPartySelectSong;

        public EventHandler<PerformanceStartEvent> OnPerformanceStart;

        private ConcurrentQueue<JamboreeEvent> _eventQueue;
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
                            case PartyChangedEvent partyChanged:
                                if (OnPartyChanged == null)
                                    break;
                                OnPartyChanged(this, partyChanged);
                                break;
                            case PartyCreatedEvent partyCreated:
                                if (OnPartyCreated == null)
                                    break;
                                OnPartyCreated(this, partyCreated);
                                break;
                            case PartyDebugLogEvent partyDebugLog:
                                if (OnPartyDebugLog == null)
                                    break;
                                OnPartyDebugLog(this, partyDebugLog);
                                break;
                            case PartyJoinedEvent partyJoined:
                                if (OnPartyJoined == null)
                                    break;
                                OnPartyJoined(this, partyJoined);
                                break;
                            case PartyLogEvent partyLog:
                                if (OnPartyLog == null)
                                    break;
                                OnPartyLog(this, partyLog);
                                break;
                            case PartyPlaylistChangeEvent ev:
                                if (OnPlaylistChangedEvent == null)
                                    break;
                                OnPlaylistChangedEvent(this, ev);
                                break;
                            case PartyPlaylistSendEvent partyPlaylist:
                                if (OnPartyPlaylistSend == null)
                                    break;
                                OnPartyPlaylistSend(this, partyPlaylist);
                                break;
                            case PartySelectSongEvent ev:
                                if (OnPartySelectSong == null)
                                    break;
                                OnPartySelectSong(this, ev);
                                break;

                            case PerformanceStartEvent performanceStart:
                                if (OnPerformanceStart == null)
                                    break;
                                OnPerformanceStart(this, performanceStart);
                                break;
                        };
                    }
                    catch
                    { }
                }
                await Task.Delay(25, token).ContinueWith(tsk => { });
            }
        }

        private CancellationTokenSource _eventsTokenSource;

        private void StartEventsHandler()
        {
            _eventQueue = new ConcurrentQueue<JamboreeEvent>();
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

        internal void PublishEvent(JamboreeEvent jamboreeEvent)
        {
            if (!_eventQueueOpen)
                return;

            _eventQueue.Enqueue(jamboreeEvent);
        }
    }
}
