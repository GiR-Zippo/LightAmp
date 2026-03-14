/*
 * Copyright(c) 2025 GiR-Zippo
 * Licensed under the GPL v3 license. See https://github.com/GiR-Zippo/LightAmp/blob/main/LICENSE for full license information.
 */

using BardMusicPlayer.DalamudBridge.Helper.Dalamud;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Seer;

namespace BardMusicPlayer.DalamudBridge
{
    public struct DalamudBridgeCommandStruct
    {
        public MessageType messageType;
        public ChatMessageChannelType chatType;
        public Game game;
        public int IntData;
        public bool BoolData;
        public string TextData;
    }
}
