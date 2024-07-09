/*
 * Copyright(c) 2024 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.DalamudBridge.Helper.Dalamud
{
    public enum MessageType
    {
        None                    = 0,
        Handshake               = 1,
        Version                 = 2,

        SetGfx                  = 10,   //Get<->Set
        NameAndHomeWorld        = 11,   //Get
        MasterSoundState        = 12,   //Set<->Get
        MasterVolume            = 13,   //Set<->Get
        VoiceSoundState         = 14,   //Set<->Get
        EffectsSoundState       = 15,   //Set<->Get

        Instrument              = 20,
        NoteOn                  = 21,
        NoteOff                 = 22,
        ProgramChange           = 23,

        StartEnsemble           = 30,   //Get<->Set
        AcceptReply             = 31,
        PerformanceModeState    = 32,   //Get

        Chat                    = 40,

        NetworkPacket           = 50,
        ExitGame                = 55,

        PartyInvite             = 60,   //Set           (name";"HomeWorldId)
        PartyInviteAccept       = 61,
        PartyPromote            = 62,   //Set           (name)
        PartyEnterHouse         = 63,
        PartyTeleport           = 64,   //Set           (host?show menu : accept TP)
        PartyFollow             = 65    //Set           (name";"HomeWorldId) | "" unfollow
    }
}
