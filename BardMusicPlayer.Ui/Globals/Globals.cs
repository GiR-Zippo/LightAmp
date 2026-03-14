/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;

namespace BardMusicPlayer.Ui.Globals
{
    public static class Globals
    {
        public static string FileFilters = "MIDI file|*.mid;*.midi;*.mmsong;*.mml;*.gp*|All files (*.*)|*.*";
        public static string MusicCatalogFilters = "Amp Catalog file|*.db";
        public static string DataPath;
        public enum Autostart_Types
        {
            NONE = 0,
            VIA_CHAT,
            VIA_METRONOME,
            UNUSED
        }

        public static event EventHandler OnConfigReload;
        public static void ReloadConfig()
        {
            OnConfigReload?.Invoke(null, null);
        }
    }
}
