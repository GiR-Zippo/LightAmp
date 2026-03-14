/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.DalamudBridge;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Quotidian.Structs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BardMusicPlayer.Maestro.Performance
{
    public partial class Performer
    {
        public void StartLyricsTimer()
        {
            if (LyricsOffsetTime == -1)
                return;
            if (_lyricsTick.Enabled)
                return;

            LyricsOffsetTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - LyricsOffsetTime;
            _lyricsTick.Interval = 50;
            _lyricsTick.Enabled = true;
            _lyricsTick.Start();            
        }

        public void StopLyricsTimer()
        {
            LyricsOffsetTime = -1;
            if (_lyricsTick.Enabled)
                _lyricsTick.Enabled = false;

            while (_lyricsQueue.TryDequeue(out _))
            {
            }
        }

        ConcurrentQueue<KeyValuePair<long, string> > _lyricsQueue = new ConcurrentQueue<KeyValuePair<long, string>>();
        public long LyricsOffsetTime { get; set; } = -1;
        private System.Timers.Timer _lyricsTick { get; set; } = new System.Timers.Timer();
        private void LyricsTick_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_lyricsQueue.Count == 0)
                return;

            var text = _lyricsQueue.First();
            if (text.Key + LyricsOffsetTime <= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            {
                KeyValuePair<long, string> data;
                _lyricsQueue.TryDequeue(out data);
                GameExtensions.SendText(game, ChatMessageChannelType.Say, data.Value);
            }          
        }

        private void InternalLyrics(object sender, Sanford.Multimedia.Midi.MetaMessageEventArgs e)
        {
            if (SingerTrackNr <= 0) //0 mean no singer
                return;

            if (!UsesDalamud)
                return;

            Sanford.Multimedia.Midi.MetaTextBuilder builder = new Sanford.Multimedia.Midi.MetaTextBuilder(e.Message);
            string text = builder.Text;
            if (_sequencer.GetTrackNum(e.MidiTrack) == SingerTrackNr + mainSequencer.LyricStartTrack - 1)
            {
                //if the ensemble is running compensate the latency
                if (BmpPigeonhole.Instance.UseLyricsOffset && (LyricsOffsetTime > -1))
                    _lyricsQueue.Enqueue(new KeyValuePair<long, string>(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), text));
                else
                    GameExtensions.SendText(game, ChatMessageChannelType.Say, text);
            }
        }
    }
}
