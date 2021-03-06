using BardMusicPlayer.Jamboree.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroTier.Sockets;

namespace BardMusicPlayer.Jamboree.PartyNetworking
{
    /// <summary>
    /// The autodiscover, to get the client IP and version
    /// </summary>
    internal class Autodiscover : IDisposable
    {
        private static readonly Lazy<Autodiscover> lazy = new Lazy<Autodiscover>(() => new Autodiscover());
        public static Autodiscover Instance { get { return lazy.Value; } }
        private Autodiscover() { }
        ~Autodiscover()
        {
            svcRx.Stop();
#if DEBUG
            Console.WriteLine("Destructor Called.");
#endif
        }

        void IDisposable.Dispose()
        {
            svcRx.Stop();
#if DEBUG
            Console.WriteLine("Dispose Called.");
#endif
            //GC.SuppressFinalize(this);
        }

        private SocketRx svcRx { get; set; } = null;

        public void StartAutodiscover(string address, string version)
        {
            BackgroundWorker objWorkerServerDiscoveryRx = new BackgroundWorker();
            objWorkerServerDiscoveryRx.WorkerReportsProgress = true;
            objWorkerServerDiscoveryRx.WorkerSupportsCancellation = true;

            svcRx = new SocketRx(ref objWorkerServerDiscoveryRx, address, version);

            objWorkerServerDiscoveryRx.DoWork += new DoWorkEventHandler(svcRx.Start);
            objWorkerServerDiscoveryRx.ProgressChanged += new ProgressChangedEventHandler(logWorkers_ProgressChanged);
            objWorkerServerDiscoveryRx.RunWorkerAsync();
        }

        private void logWorkers_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Console.WriteLine(e.UserState.ToString());
        }

        public void Stop()
        {
            svcRx.Stop();
        }

    }

    public class SocketRx
    {
        public bool disposing = false;
        public System.Net.IPEndPoint iPEndPoint;
        public string BCAddress = "";
        public string Address = "";
        public string version = "";
        public int ServerPort = 0;
        byte[] bytes = new byte[255];

        private BackgroundWorker worker = null;

        public SocketRx(ref BackgroundWorker w, string address, string ver)
        {
            Address = address;
            BCAddress = address.Split('.')[0] + "." + address.Split('.')[1] + "." + address.Split('.')[2] + ".255";
            version = ver;
            worker = w;
            worker.ReportProgress(1, "Server");
        }

        public void Start(object sender, DoWorkEventArgs e)
        {
            ZeroTierExtendedSocket listener = new ZeroTierExtendedSocket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            ZeroTierExtendedSocket transmitter = new ZeroTierExtendedSocket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
            int r = listener.SetBroadcast();
            r = transmitter.SetBroadcast();
            iPEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(BCAddress), 5555);
            listener.ReceiveTimeout = 10;
            listener.BSD_Bind(iPEndPoint);
            BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Autodiscover]: Started\r\n"));
            
            while (this.disposing == false)
            {
                int bytesRec = listener.ReceiveFrom(bytes);
                if (bytesRec > 0)
                {
                    string all = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    string f = all.Split(' ')[0];               //Get the init
                    if (f.Equals("XIVAmp"))
                    {
                        string ip = all.Split(' ')[1];          //the IP
                        string version = all.Split(' ')[2];     //the version number
                        //Add the client
                        FoundClients.Instance.Add(ip, version);
                    }
                }
                if (!this.disposing)
                {
                    string t = "XIVAmp " + Address + " " + version; //Send the init ip and version
                    int p = transmitter.SendTo(iPEndPoint, Encoding.ASCII.GetBytes(t));
                    System.Threading.Thread.Sleep(3000);
                }
            }

            try { transmitter.Shutdown(System.Net.Sockets.SocketShutdown.Both); }
            finally { transmitter.Close(); }
            try { listener.Shutdown(System.Net.Sockets.SocketShutdown.Both); }
            finally { listener.Close(); }
            BmpJamboree.Instance.PublishEvent(new PartyDebugLogEvent("[Autodiscover]: Stopped\r\n"));
            return;
        }

        public void Stop()
        {
            this.disposing = true;
        }
    }
}
