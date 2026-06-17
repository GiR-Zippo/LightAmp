/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.XIVMIDI.Events;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BardMusicPlayer.XIVMIDI
{
    public partial class XIVMidiApi
    {
        private ConcurrentQueue<XIVMidiEvent> _eventQueue;
        private bool _eventQueueOpen;

        #region Callbacks
        public EventHandler<XIVMidiBMPSongsEvent> OnBMPSongList;
        public EventHandler<XIVMidiXIVSongsEvent> OnXIVSongList;
        public EventHandler<XIVMidiFileEvent> OnXIVMidiFile;
        public EventHandler<XIVMidiApiErrorEvent> OnXIVRequestError;
        public EventHandler<XIVMidiUploadResponseEvent> OnXIVUploadResponse;
        #endregion

        /// <summary>
        /// The event queue
        /// </summary>
        /// <param name="token"></param>
        private async Task RunEventsHandler(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (_eventQueue.TryDequeue(out var xivapiEvent))
                {
                    if (token.IsCancellationRequested)
                        break;

                    try
                    {
                        switch (xivapiEvent)
                        {
                            case XIVMidiBMPSongsEvent songlistEvent:
                                if (OnBMPSongList == null)
                                    break;
                                OnBMPSongList(this, songlistEvent);
                                break;
                            case XIVMidiXIVSongsEvent songlistEvent:
                                if (OnXIVSongList == null)
                                    break;
                                OnXIVSongList(this, songlistEvent);
                                break;
                            case XIVMidiFileEvent midiFileEvent:
                                if (OnXIVMidiFile == null)
                                    break;
                                OnXIVMidiFile(this, midiFileEvent);
                                break;
                            case XIVMidiApiErrorEvent responseError:
                                if (OnXIVRequestError == null)
                                    break;
                                OnXIVRequestError(this, responseError);
                                break;
                            case XIVMidiUploadResponseEvent midiUploadResponse:
                                if (OnXIVUploadResponse == null)
                                    break;
                                OnXIVUploadResponse(this, midiUploadResponse);
                                break;
                        }
                    }
                    catch
                    { }
                }
                await Task.Delay(25, token).ContinueWith(tsk => { });
            }
        }

        private CancellationTokenSource _eventsTokenSource;

        /// <summary>
        /// Start the event queue
        /// </summary>
        private void StartEventsHandler()
        {
            _eventQueue = new ConcurrentQueue<XIVMidiEvent>();
            _eventsTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() => RunEventsHandler(_eventsTokenSource.Token), TaskCreationOptions.LongRunning);
            _eventQueueOpen = true;
        }

        /// <summary>
        /// Stop the event queue
        /// </summary>
        private void StopEventsHandler()
        {
            _eventQueueOpen = false;
            _eventsTokenSource.Cancel();
            while (_eventQueue.TryDequeue(out _)){}
        }

        /// <summary>
        /// Publish a response from Api
        /// </summary>
        /// <param name="xivapiEvent"></param>
        internal void PublishEvent(XIVMidiEvent xivapiEvent)
        {
            if (!_eventQueueOpen)
                return;

            _eventQueue.Enqueue(xivapiEvent);
        }
    }
}
