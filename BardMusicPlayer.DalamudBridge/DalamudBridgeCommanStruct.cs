using BardMusicPlayer.DalamudBridge.Helper.Dalamud;
using BardMusicPlayer.Quotidian.Structs;
using BardMusicPlayer.Seer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
