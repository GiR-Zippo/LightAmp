/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.DalamudBridge;
using BardMusicPlayer.DalamudBridge.Helper.Dalamud;
using BardMusicPlayer.Pigeonhole;
using BardMusicPlayer.Quotidian.Structs;
using System.Threading.Tasks;

namespace BardMusicPlayer.Maestro.Performance
{
    public partial class Performer
    {
        #region Instrument_Actions
        /// <summary>
        /// Open an instrument
        /// </summary>
        public void OpenInstrument()
        {
            //Check if we already have an instrument
            if (!game.InstrumentHeld.Equals(Instrument.None))
                return;

            //Check if the track and channel are okay
            if (!trackAndChannelOk())
                return;

            //if dalamud is active use it
            if (UsesDalamud)
                DalamudBridge.DalamudBridge.Instance.ActionToQueue(new DalamudBridgeCommandStruct { messageType = MessageType.Instrument, game = game, IntData = Instrument.Parse(TrackInstrument).Index });
            else
            {
                var key = game.InstrumentKeys[Instrument.Parse(TrackInstrument)];
                if (key != Quotidian.Enums.Keys.None)
                    _hook.SendSyncKeybind(key);
            }
        }

        /// <summary>
        /// Replace the instrument
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReplaceInstrument()
        {
            //Check if the track and channel are okay
            if (!trackAndChannelOk())
                return 0;

            if (!game.InstrumentHeld.Equals(Instrument.None))
            {
                if (game.InstrumentHeld.Equals(Instrument.Parse(TrackInstrument)))
                    return 0;
                else
                {
                    _hook.ClearLastPerformanceKeybinds();

                    if (UsesDalamud)
                        DalamudBridge.DalamudBridge.Instance.ActionToQueue(new DalamudBridgeCommandStruct { messageType = MessageType.Instrument, game = game, IntData = 0 });
                    else
                        _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.ESC]);
                    await Task.Delay(BmpPigeonhole.Instance.EnsembleReadyDelay).ConfigureAwait(false);
                }
            }

            if (Instrument.Parse(TrackInstrument).Equals(Instrument.None))
                return 0;

            //if dalamud is active use it
            if (UsesDalamud)
                DalamudBridge.DalamudBridge.Instance.ActionToQueue(new DalamudBridgeCommandStruct { messageType = MessageType.Instrument, game = game, IntData = Instrument.Parse(TrackInstrument).Index });
            else
            {
                var key = game.InstrumentKeys[Instrument.Parse(TrackInstrument)];
                if (key != Quotidian.Enums.Keys.None)
                    _hook.SendSyncKeybind(key);
            }

            return 0;
        }

        /// <summary>
        /// Close the instrument
        /// </summary>
        public void CloseInstrument()
        {
            //If we don't have an instrument, nothing to do
            if (game.InstrumentHeld.Equals(Instrument.None))
                return;

            _hook.ClearLastPerformanceKeybinds();

            //if dalamud is active use it
            if (UsesDalamud)
                DalamudBridge.DalamudBridge.Instance.ActionToQueue(new DalamudBridgeCommandStruct { messageType = MessageType.Instrument, game = game, IntData = 0 });
            else
                _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.ESC]);
        }
        #endregion

        #region Ensmble_Actions
        /// <summary>
        /// Do the ready check
        /// </summary>
        public void DoReadyCheck()
        {
            if (!_forcePlayback)
            {
                if (!this.PerformerEnabled)
                    return;

                if (game.InstrumentHeld.Equals(Instrument.None))
                    return;
            }

            //if dalamud is active use it
            if (UsesDalamud)
            {
                GameExtensions.StartEnsemble(game);
                return;
            }

            //do it the lecagy way
            Task task = Task.Run(() =>
            {
                _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.VIRTUAL_PAD_SELECT]);
                Task.Delay(100).Wait();
                _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.LEFT]);
                Task.Delay(100).Wait();
                _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
                Task.Delay(400).Wait();
                _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
            });
        }

        /// <summary>
        /// Accept the ready check
        /// </summary>
        public void EnsembleAccept()
        {
            if (!_forcePlayback)
            {
                if (!this.PerformerEnabled)
                    return;

                if (game.InstrumentHeld.Equals(Instrument.None))
                    return;
            }

            //if dalamud is active use it
            if (UsesDalamud)
            {
                DalamudBridge.DalamudBridge.Instance.ActionToQueue(new DalamudBridgeCommandStruct { messageType = MessageType.AcceptReply, game = game, BoolData = true });
                return;
            }

            //do it the lecagy way
            _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
            Task.Delay(200);
            _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
        }

        /// <summary>
        /// Press Yes on a Yes / No box
        /// </summary>
        public void YesNoBoxAccept()
        {
            _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
            Task.Delay(100).Wait();
            _hook.SendSyncKeybind(Quotidian.Enums.Keys.NumPad4);
            Task.Delay(100).Wait();
            _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
        }

        public void EnterHouse()
        {
            _hook.SendSyncKeybind(Quotidian.Enums.Keys.NumPad0);
            Task.Delay(1000).Wait();
            _hook.SendSyncKeybind(Quotidian.Enums.Keys.NumPad4);
            Task.Delay(100).Wait();
            _hook.SendSyncKeybind(game.NavigationMenuKeys[Quotidian.Enums.NavigationMenuKey.OK]);
        }
        #endregion

        #region Textoutput

        /// <summary>
        /// Send a text in game; During playback set this into a task
        /// </summary>
        /// <param name="text"></param>
        public void SendTextCopyPasta(string text)
        {
            if (!game.ChatStatus)
            {
                _hook.SendSyncKeybind(Quotidian.Enums.Keys.Enter);
                Task.Delay(BmpPigeonhole.Instance.EnsembleReadyDelay).Wait();
            }
            _hook.CopyToClipboard(text);
            Task.Delay(BmpPigeonhole.Instance.EnsembleReadyDelay).Wait();
            _hook.SendSyncKeybind(Quotidian.Enums.Keys.Enter);
        }

        public void SendText(string text)
        {
            if (!game.ChatStatus)
            {
                _hook.SendSyncKeybind(Quotidian.Enums.Keys.Enter);
                Task.Delay(BmpPigeonhole.Instance.EnsembleReadyDelay).Wait();
            }
            _hook.SendString(text);
            Task.Delay((text.Length * 8) + 20).Wait();
            _hook.SendSyncKeybind(Quotidian.Enums.Keys.Enter);
        }

        public void SendText(ChatMessageChannelType type, string text)
        {
            GameExtensions.SendText(game, type, text);
        }

        public void TapKey(string modifier, string character)
        {
            try
            {
                Quotidian.Enums.Keys key = Quotidian.Enums.KeyTranslation.ASCIIToGame[character];

                if (modifier.ToLower().Contains("shift"))
                    key = (int)Quotidian.Enums.Keys.Shift + key;
                else if (modifier.ToLower().Contains("ctrl"))
                    key = (int)Quotidian.Enums.Keys.Control + key;
                else if (modifier.ToLower().Contains("alt"))
                    key = (int)Quotidian.Enums.Keys.Alt + key;
                _hook.SendSyncKeybind(key);
            }
            catch
            {

            }
        }
        #endregion

    }
}
