using BardMusicPlayer.Jamboree.PartyManagement;
using System;
using System.Collections.Generic;

namespace BardMusicPlayer.Jamboree.PartyNetworking
{
    public static class NetworkOpcodes
    {
        public enum OpcodeEnum : byte
        {
            NULL_OPCODE = 0x00,
            SMSG_PERFORMANCE_START = 0x01,
            CMSG_TERM_SESSION = 0x02,
            CMSG_JOIN_PARTY = 0x03,
            SMSG_JOIN_PARTY = 0x04,
            SMSG_PARTY_MEMBERS = 0x05,
            SMSG_LEAVE_PARTY = 0x06
        }
    }

    public static class ZeroTierPacketBuilder
    {
        public static byte[] PerformanceStart()
        {
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.SMSG_PERFORMANCE_START);
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
        public static byte[] CMSG_JOIN_PARTY(byte type, string performer_name)
        {
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.CMSG_JOIN_PARTY);
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
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.SMSG_PARTY_MEMBERS);
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
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.SMSG_LEAVE_PARTY);
            buffer.WriteUInt8(type);
            buffer.WriteCString(performer_name);
            return buffer.GetData();
        }
    }
}
