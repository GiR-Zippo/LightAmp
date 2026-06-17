/*
 * Copyright(c) 2026 GiR-Zippo, 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.XIVMIDI.IO;
using BardMusicPlayer.XIVMIDI.WebApi;
using System;

namespace BardMusicPlayer.XIVMIDI
{
    public partial class XIVMidiApi : IDisposable
    {
        #region Instance Constructor/Destructor
        private static readonly Lazy<XIVMidiApi> LazyInstance = new(() => new XIVMidiApi());

        private HTTPWorker _HttpWorker { get; set; } = null;
        /// <summary>
        /// 
        /// </summary>
        public bool Started { get; private set; }

        private XIVMidiApi()
        {
            _HttpWorker = new HTTPWorker();
        }

        public static XIVMidiApi Instance => LazyInstance.Value;

        /// <summary>
        /// Start the eventhandler
        /// </summary>
        /// <returns></returns>
        public void Start()
        {
            if (Started) return;
            StartEventsHandler();
            Started = true;
        }

        /// <summary>
        /// Stop the eventhandler
        /// </summary>
        /// <returns></returns>
        public void Stop()
        {
            if (!Started) return;
            StopEventsHandler();
            Started = false;
            Dispose();
        }

        public void Dispose()
        {
            _HttpWorker.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Getters
        /// <summary>
        /// Get the songlist from BMPApi
        /// </summary>
        public void GetSonglist(BMPAPIRequestBuilder request)
        {
            _HttpWorker.RequestSongList(request).ConfigureAwait(true);
        }

        /// <summary>
        /// Get the song list from XIVMidi
        /// </summary>
        public void GetSonglist(XIVMIDIRequestBuilder request)
        {
            _HttpWorker.RequestSongList(request).ConfigureAwait(true);
        }

        /// <summary>
        /// Download a midi file
        /// </summary>
        public void GetMidiFile(string url, object args, bool fromBMP)
        {
            _HttpWorker.DownloadSong(url, args, fromBMP).ConfigureAwait(true);
        }

        /// <summary>
        /// Upload Midi
        /// </summary>
        /// <param name="upload"></param>
        public void UploadMidi(BMPUploadBuilder upload)
        {
            _HttpWorker.UploadSong(upload).ConfigureAwait(true);
        }
        #endregion
    }
}
