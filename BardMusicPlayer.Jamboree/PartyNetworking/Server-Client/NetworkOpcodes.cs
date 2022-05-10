using BardMusicPlayer.Jamboree.PartyManagement;
using System;
using System.Collections.Generic;

namespace BardMusicPlayer.Jamboree.PartyNetworking
{
    public static class NetworkOpcodes
    {
        public enum OpcodeEnum : byte
        {
            NULL_OPCODE             = 0x00,
            PING                    = 0x01,
            PONG                    = 0x02,
            MSG_JOIN_PARTY          = 0x03
        }
    }

    public static class ZeroTierPacketBuilder
    {
        public static byte[] PerformanceStart()
        {
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.NULL_OPCODE);
            buffer.WriteInt64(DateTimeOffset.Now.ToUnixTimeMilliseconds());
            return buffer.GetData();
        }

        /// <summary>
        /// Send we joined the party
        /// | type 0 = bard
        /// | type 1 = dancer
        /// </summary>
        /// <param name="type"></param>
        /// <param name="performer_name"></param>
        /// <returns>data as byte[]</returns>
        public static byte[] MSG_JOIN_PARTY(byte type, string performer_name)
        {
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.MSG_JOIN_PARTY);
            buffer.WriteUInt8(type);
            buffer.WriteCString(performer_name);
            return buffer.GetData();
        }

        /// <summary>
        /// pack the party members in our party
        /// </summary>
        /// <param name="clients"></param>
        public static byte[] SMSG_PARTY_MEMBERS(List<PartyClientInfo> clients)
        {
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.NULL_OPCODE);
            buffer.WriteInt32(clients.Count);
            foreach (var member in clients)
            {
                buffer.WriteUInt8(member.Performer_Type);
                buffer.WriteCString(member.Performer_Name);
            }
            return buffer.GetData();
        }

        public static byte[] SMSG_LEAVE_PARTY(byte type, string performer_name)
        {
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.NULL_OPCODE);
            buffer.WriteUInt8(type);
            buffer.WriteCString(performer_name);
            return buffer.GetData();
        }
    }
}
