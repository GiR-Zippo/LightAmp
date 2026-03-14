/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.DalamudBridge;
using BardMusicPlayer.Maestro.Utils;
using BardMusicPlayer.Quotidian.Structs;
using System;
using System.Threading.Tasks;

namespace BardMusicPlayer.Maestro.Performance
{
    public partial class Performer
    {
        /// <summary>
        /// If the sequencer has a note to play, it will be processed here 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void InternalNote(Object o, Sanford.Multimedia.Midi.ChannelMessageEventArgs args)
        {
            Sanford.Multimedia.Midi.ChannelMessageBuilder builder = new Sanford.Multimedia.Midi.ChannelMessageBuilder(args.Message);
            //build the note event
            NoteEvent noteEvent = new NoteEvent
            {
                note = builder.Data1,
                origNote = builder.Data1,
                trackNum = _sequencer.GetTrackNum(args.MidiTrack),
                track = args.MidiTrack,
            };

            //check if the note was for the performer and if the sequencer is running
            if ((_sequencer.GetTrackNum(noteEvent.track) == this._trackNumber) || !_sequencer.IsPlaying)
            {
                noteEvent.note = NoteHelper.ApplyOctaveShift(noteEvent.note, this.OctaveShift);

                Sanford.Multimedia.Midi.ChannelCommand cmd = args.Message.Command;
                int vel = builder.Data2;
                if ((cmd == Sanford.Multimedia.Midi.ChannelCommand.NoteOff) || (cmd == Sanford.Multimedia.Midi.ChannelCommand.NoteOn && vel == 0))
                {
                    this.processOffNote(noteEvent);
                }
                if ((cmd == Sanford.Multimedia.Midi.ChannelCommand.NoteOn) && vel > 0)
                {
                    if (_livePlayDelay)
                        this.processOnNoteLive(noteEvent);
                    else
                        this.processOnNote(noteEvent);
                }
            }
        }

        /// <summary>
        /// Play the note via dalamud or legacy output
        /// </summary>
        /// <param name="note"></param>
        private void processOnNote(NoteEvent note)
        {
            if (!_forcePlayback)
            {
                if (!this.PerformerEnabled)
                    return;

                if (game.InstrumentHeld.Equals(Instrument.None))
                    return;
            }

            //Check if note is in range
            if (note.note < 0 || note.note > 36)
                return;

            //if dalamud is active use it
            if (UsesDalamudForKeys)
            {
                GameExtensions.SendNote(game, note.note, true);
                return;
            }

            //else use the lecagy output
            if (game.NoteKeys[(Quotidian.Enums.NoteKey)note.note] is Quotidian.Enums.Keys keybind)
            {
                if (game.ChatStatus && !_forcePlayback)
                    return;

                if (_holdNotes)
                    _hook.SendKeybindDown(keybind);
                else
                    _hook.SendAsyncKeybind(keybind);
            }
        }

        /// <summary>
        /// Play the note via dalamud or legacy output with compensation for live play
        /// </summary>
        /// <param name="note"></param>
        private void processOnNoteLive(NoteEvent note)
        {
            if (!_forcePlayback)
            {
                if (!this.PerformerEnabled)
                    return;

                if (game.InstrumentHeld.Equals(Instrument.None))
                    return;
            }

            if (note.note < 0 || note.note > 36)
                return;

            if (game.NoteKeys[(Quotidian.Enums.NoteKey)note.note] is Quotidian.Enums.Keys keybind)
            {

                long diff = System.Diagnostics.Stopwatch.GetTimestamp() / 10000 - _lastNoteTimestamp;
                if (diff < 15)
                {
                    int sleepDuration = (int)(15 - diff);
                    Task.Delay(sleepDuration).Wait();
                }

                if (game.ChatStatus && !_forcePlayback)
                    return;

                //if dalamud is active use it
                if (UsesDalamudForKeys)
                    GameExtensions.SendNote(game, note.note, true);
                else
                {
                    if (_holdNotes)
                        _hook.SendKeybindDown(keybind);
                    else
                        _hook.SendAsyncKeybind(keybind);
                }
                _lastNoteTimestamp = System.Diagnostics.Stopwatch.GetTimestamp() / 10000;
            }
        }

        /// <summary>
        /// Stop the note via dalamud or legacy output
        /// </summary>
        /// <param name="note"></param>
        private void processOffNote(NoteEvent note)
        {
            //Check if the performer is enabled
            if (!this.PerformerEnabled)
                return;

            //Check if note is in range
            if (note.note < 0 || note.note > 36)
                return;

            //if dalamud is active use it
            if (UsesDalamudForKeys)
            {
                GameExtensions.SendNote(game, note.note, false);
                return;
            }

            //else use the lecagy output
            if (game.NoteKeys[(Quotidian.Enums.NoteKey)note.note] is Quotidian.Enums.Keys keybind)
            {
                if (game.ChatStatus && !_forcePlayback)
                    return;

                if (_holdNotes)
                    _hook.SendKeybindUp(keybind);
            }
        }
    }
}
