/*
 * Copyright(c) 2024 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.DalamudBridge;
using BardMusicPlayer.Quotidian.Structs;

namespace BardMusicPlayer.Maestro.Performance
{
    public partial class Performer
    {
        private void InternalLyrics(object sender, Sanford.Multimedia.Midi.MetaMessageEventArgs e)
        {
            if (SingerTrackNr <= 0) //0 mean no singer
                return;

            if (!UsesDalamud)
                return;

            Sanford.Multimedia.Midi.MetaTextBuilder builder = new Sanford.Multimedia.Midi.MetaTextBuilder(e.Message);
            string text = builder.Text;
            if (_sequencer.GetTrackNum(e.MidiTrack) == SingerTrackNr + mainSequencer.LyricStartTrack - 1)
                GameExtensions.SendText(game, ChatMessageChannelType.Say, text);
        }
    }
}
