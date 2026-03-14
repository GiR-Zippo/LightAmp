/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using Sanford.Multimedia.Midi;
using System.Collections.Generic;

namespace BardMusicPlayer.Maestro.Utils
{
    public static class MidiInput
    {
        /// <summary>
        /// Helper struct for the Midi-device and Id
        /// </summary>
        public struct MidiInputDescription
        {
            public string name;
            public int id;
            public MidiInputDescription(string n, int i)
            {
                name = n;
                id = i;
            }
        }       

        /// <summary>
        /// Reload and get all midi input devices
        /// </summary>
        /// <returns><see cref="Dictionary{TKey, TValue}"/></returns>
        public static Dictionary<int, string> ReloadMidiInputDevices()
        {
            Dictionary<int, string> midiInputs = new Dictionary<int, string>();
            midiInputs.Add(-1, "None");
            for (int i = 0; i < InputDevice.DeviceCount; i++)
            {
                MidiInCaps cap = InputDevice.GetDeviceCapabilities(i);
                midiInputs.Add(i, cap.name);
            }
            return midiInputs;
        }
    }
}
