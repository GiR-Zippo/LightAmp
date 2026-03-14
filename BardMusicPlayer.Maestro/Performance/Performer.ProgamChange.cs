/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.DalamudBridge;
using BardMusicPlayer.Maestro.Utils;
using BardMusicPlayer.Quotidian.Structs;

namespace BardMusicPlayer.Maestro.Performance
{
    public partial class Performer
    {
        /// <summary>
        /// The program changes, atm used for guitars
        /// </summary>
        private void InternalProg(object sender, Sanford.Multimedia.Midi.ChannelMessageEventArgs args)
        {
            if (!_forcePlayback)
            {
                if (!this.PerformerEnabled)
                    return;

                if (game.InstrumentHeld.Equals(Instrument.None))
                    return;
            }

            var programEvent = new ProgChangeEvent
            {
                track = args.MidiTrack,
                trackNum = _sequencer.GetTrackNum(args.MidiTrack),
                voice = args.Message.Data1,
            };
            if (programEvent.voice < 27 || programEvent.voice > 31)
                return;

            if (_sequencer.GetTrackNum(programEvent.track) == this._trackNumber)
            {
                if (game.ChatStatus && !_forcePlayback)
                    return;

                int tone = -1;
                switch (programEvent.voice)
                {
                    case 29: // overdriven guitar
                        tone = 0;
                        break;
                    case 27: // clean guitar
                        tone = 1;
                        break;
                    case 28: // muted guitar
                        tone = 2;
                        break;
                    case 30: // power chords
                        tone = 3;
                        break;
                    case 31: // special guitar
                        tone = 4;
                        break;
                }

                if (UsesDalamudForKeys)
                {
                    GameExtensions.SendProgchange(game, tone);
                    return;
                }

                if (tone > -1 && tone < 5 && game.InstrumentToneMenuKeys[(Quotidian.Enums.InstrumentToneMenuKey)tone] is Quotidian.Enums.Keys keybind)
                    _hook.SendSyncKey(keybind);
            }
        }
    }
}
