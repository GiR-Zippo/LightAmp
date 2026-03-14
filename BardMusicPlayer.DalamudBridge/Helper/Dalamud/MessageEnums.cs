/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

namespace BardMusicPlayer.DalamudBridge.Helper.Dalamud
{
    public enum MessageType
    {
        None                    = 0,
        Handshake               = 1,
        Version                 = 2,
        MoveCharToPosition      = 3,
        StopMovement            = 4,

        ClientLogout            = 5,
        GameShutdown            = 6,

        SetGfx                  = 10,   //Get<->Set
        NameAndHomeWorld        = 11,   //Get
        MasterSoundState        = 12,   //Set<->Get
        MasterVolume            = 13,   //Set<->Get
        VoiceSoundState         = 14,   //Set<->Get
        EffectsSoundState       = 15,   //Set<->Get
        SetWindowRenderSize     = 16,

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
