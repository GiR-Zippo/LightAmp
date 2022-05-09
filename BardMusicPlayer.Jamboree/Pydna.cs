using BardMusicPlayer.Jamboree.Events;
using BardMusicPlayer.Jamboree.PartyNetworking;
using ZeroTier;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BardMusicPlayer.Jamboree
{
    /// <summary>
    /// The manager class for the festival
    /// </summary>
    /// We are creating a mesh network
    /// - fire up the ZeroTier
    /// - start the autodiscover to tell we are here
    /// - start the TCP listener
    /// - add the clients we found to the manager
    /// - create a Session and connect to the listenserver
    /// - add the listensocket from the listenserver to our session
    /// - handshake complete
    public class Pydna
    {
        private bool _servermode { get; set; } = false;

        private ZeroTierConnector zeroTierConnector = null;

        public void JoinParty(string networkId, byte type, string name)
        {
            FoundClients.Instance.OwnName = name;
            FoundClients.Instance.Type = type;

            zeroTierConnector = new ZeroTierConnector();
            string data = zeroTierConnector.ZeroTierConnect(networkId).Result;
            Autodiscover.Instance.StartAutodiscover(data, "0.1.0");
            NetworkPartyServer.Instance.StartServer(new IPEndPoint(IPAddress.Parse(data), 12345), type, name);
            return;
        }

        public void LeaveParty()
        {
            //Stop the autodiscover
            Autodiscover.Instance.Stop();
            NetworkPartyServer.Instance.Stop();
            FoundClients.Instance.Clear();
            /*if (!_servermode)
            {
                if (client == null)
                    return;
                client.Close();
                client = null;
            }
            else*/
            {
                ((IDisposable)NetworkPartyServer.Instance).Dispose();
                GC.Collect();
            }
            zeroTierConnector.ZeroTierDisconnect();
        }

#region NetworkSendFunctions
        public void SendPerformanceStart()
        {
            FoundClients.Instance.SendToAll(ZeroTierPacketBuilder.PerformanceStart());
        }

        /// <summary>
        /// Send we joined the party
        /// | type 0 = bard
        /// | type 1 = dancer
        /// </summary>
        /// <param name="type"></param>
        /// <param name="performer_name"></param>
        public void SendPerformerJoin(byte type, string performername)
        {
            /*if (!_servermode)
            {
                client.SetPlayerData(type, performername);
                client.SendPacket(ZeroTierPacketBuilder.CMSG_JOIN_PARTY(type, performername));
            }*/
        }

        public void SendClientPacket(byte [] packet)
        {
            //if (!_servermode)
            //    client.SendPacket(packet);
        }

        public void SendServerPacket(byte[] packet)
        {
            FoundClients.Instance.SendToAll(packet);
        }

        #endregion

    }
}
