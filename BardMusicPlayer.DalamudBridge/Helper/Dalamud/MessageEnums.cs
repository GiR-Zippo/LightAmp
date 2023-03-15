using System;

namespace BardMusicPlayer.DalamudBridge.Helper.Dalamud
{
    public enum MessageType
    {
        None                    = 0,
        Handshake               = 1,
        Version                 = 2,

        SetGfx                  = 10,   //Get<->Set
        NameAndHomeWorld        = 11,   //Get

        Instrument              = 20,
        NoteOn                  = 21,
        NoteOff                 = 22,
        ProgramChange           = 23,

        StartEnsemble           = 30,   //Get<->Set
        AcceptReply             = 31,
        PerformanceModeState    = 32,   //Get

        Chat                    = 40,

        NetworkPacket           = 50
    }
}
