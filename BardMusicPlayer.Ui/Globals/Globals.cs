/*
 * Copyright(c) 2026 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BardMusicPlayer.Ui.Globals
{
    public static class Globals
    {
        public static string FileFilters = "MIDI file|*.mid;*.midi;*.mmsong;*.mml;*.ms2mml;*.gp*;*.flp*|All files (*.*)|*.*";
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

        public static void SetTheme(object control, string theme)
        {
            if (!File.Exists(theme))
                return;

            var file = File.OpenText(theme);
            if (!file.ReadLine().StartsWith("<ResourceDictionary xmlns="))
            {
                file.Close();
                return;
            }

            Collection<ResourceDictionary> dictionaries = new();
            if (control is UserControl)
                dictionaries = (control as UserControl).Resources.MergedDictionaries;
            else if (control is Window)
                dictionaries = (control as Window).Resources.MergedDictionaries;
            else
                return;

            var existing = dictionaries.FirstOrDefault(d =>
                d.Source != null && d.Source.ToString().Contains("Theme.xaml"));
            if (existing != null)
                dictionaries.Remove(existing);

            dictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(theme, UriKind.Absolute)
            });
        }
    }
}
