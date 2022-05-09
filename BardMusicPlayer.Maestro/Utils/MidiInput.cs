using Sanford.Multimedia.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BardMusicPlayer.Maestro.Utils
{
    public static class MidiInput
    {
        public class MidiInputDescription
        {
            public string name = string.Empty;
            public int id = 0;
            public MidiInputDescription(string n, int i)
            {
                name = n;
                id = i;
            }
        }       

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
