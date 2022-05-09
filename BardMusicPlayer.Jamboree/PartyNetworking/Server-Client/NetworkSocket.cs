/*
 * Copyright(c) 2021 MoogleTroupe
 * Licensed under the GPL v3 license. See https://github.com/BardMusicPlayer/BardMusicPlayer/blob/develop/LICENSE for full license information.
 */

using BardMusicPlayer.Jamboree.Events;
using BardMusicPlayer.Jamboree.PartyManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using ZeroTier.Sockets;

namespace BardMusicPlayer.Jamboree.PartyNetworking
{
    public class NetworkSocket
    {
        private bool _close = false;
        private PartyClientInfo _clientInfo = new PartyClientInfo();

        public PartyClientInfo PartyClient { get { return _clientInfo; } }

        public Socket ListenSocket { get; set; } = null;
        public Socket ConnectorSocket { get; set; } = null;
        private string _remoteIP = "";

        public NetworkSocket(string IP)
        {
            _ = ConnectTo(IP).ConfigureAwait(false);
        }

        public async Task<bool> ConnectTo(string IP)
        {
            await Task.Delay(1500); //Just wait a while
            _remoteIP = IP;
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(IP), 12345);
            byte[] bytes = new byte[1024];
            ConnectorSocket = new Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            //Connect to the server
            ConnectorSocket.Connect(localEndPoint);
            //Wait til connected
            while (!ConnectorSocket.Connected)
            { await Task.Delay(1); }
            //Inform we are connected
            BmpJamboree.Instance.PublishEvent(new PartyConnectionChangedEvent(PartyConnectionChangedEvent.ResponseCode.OK, "Connected"));

            BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[NetworkSocket]: Send handshake\r\n"));
            NetworkPacket buffer = new NetworkPacket(NetworkOpcodes.OpcodeEnum.CMSG_JOIN_PARTY);
            buffer.WriteUInt8(FoundClients.Instance.Type);
            buffer.WriteCString(FoundClients.Instance.OwnName);
            SendPacket(buffer.GetData());
                
            return false;
        }

        internal NetworkSocket(Socket socket)
        {
            ListenSocket = socket;
            PartyManager.Instance.Add(_clientInfo);
        }

        public bool Update()
        {
            byte[] bytes = new byte[60000];
            if (_close)
            {
                CloseConnection();
                return false;
            }
            if (ListenSocket.Poll(0, System.Net.Sockets.SelectMode.SelectError))
            {
                CloseConnection();
                return false;
            }

            if (ListenSocket.Available == -1)
                return false;

            if (ListenSocket.Poll(100, System.Net.Sockets.SelectMode.SelectRead))
            {
                int bytesRec;
                try
                {
                    bytesRec = ListenSocket.Receive(bytes);
                    if (bytesRec == -1)
                    {
                        CloseConnection();
                        return false;
                    }
                    else
                    {
                            serverOpcodeHandling(bytes, bytesRec);
                            clientOpcodeHandling(bytes, bytesRec);
                    }
                }
                catch (SocketException err)
                {
                    Console.WriteLine(
                            "ServiceErrorCode={0} SocketErrorCode={1}",
                            err.ServiceErrorCode,
                            err.SocketErrorCode);
                    return false;
                }
            }

            return true;
        }

        public void SendPacket(byte[] pck)
        {
            if (ConnectorSocket.Available == -1)
                _close = true;

            if (!ConnectorSocket.Connected)
                _close = true;

            try 
            { 
                if(ConnectorSocket.Send(pck) == -1 )
                    _close = true;
            }
            catch { _close = true; }
            _close = false;
        }

        private void serverOpcodeHandling(byte[] bytes, int bytesRec)
        {
            NetworkPacket packet = new NetworkPacket(bytes);
            switch (packet.Opcode)
            {
                case NetworkOpcodes.OpcodeEnum.CMSG_JOIN_PARTY:
                    _clientInfo.Performer_Type = packet.ReadUInt8();
                    _clientInfo.Performer_Name = packet.ReadCString();
                    BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[SocketServer]: Received handshake from "+_clientInfo.Performer_Name+"\r\n"));
                    break;
                default:
                    break;
            };
        }

        private void clientOpcodeHandling(byte[] bytes, int bytesRec)
        {
            NetworkPacket packet = new NetworkPacket(bytes);
            switch (packet.Opcode)
            {
                case NetworkOpcodes.OpcodeEnum.SMSG_PERFORMANCE_START:
                    BmpJamboree.Instance.PublishEvent(new PerformanceStartEvent(packet.ReadInt64()));
                    break;
                case NetworkOpcodes.OpcodeEnum.SMSG_JOIN_PARTY:

                    break;
                case NetworkOpcodes.OpcodeEnum.SMSG_PARTY_MEMBERS:
                    int count = packet.ReadInt32();
                    for (int index = 0; index != count; index ++)
                    {
                        PartyClientInfo clientInfo = new PartyClientInfo();
                        clientInfo.Performer_Type = packet.ReadUInt8();
                        clientInfo.Performer_Name = packet.ReadCString();
                        PartyManager.Instance.Add(clientInfo);
                    }
                    BmpJamboree.Instance.PublishEvent(new PartyChangedEvent());
                    break;
                default:
                    break;
            };
        }

        public void CloseConnection()
        {
            ListenSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            ListenSocket.Close();
            ConnectorSocket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
            ConnectorSocket.Close();
            FoundClients.Instance.Remove(_remoteIP);
        }

        private void sendPartyMemberList()
        {
            List<PartyClientInfo> members = PartyManager.Instance.GetPartyMembers();
            if (members.Count == 0)
                return;
            SendPacket(ZeroTierPacketBuilder.SMSG_PARTY_MEMBERS(members));
        }
    }
}
