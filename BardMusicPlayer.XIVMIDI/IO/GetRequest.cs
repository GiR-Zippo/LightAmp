using System;
using System.Net;
using System.Net.Http;

namespace BardMusicPlayer.XIVMIDI.IO
{
    public class GetRequest : IDisposable
    {
        public string Url { get; set; } = "";
        public Requester Requester { get; set; } = Requester.NONE;
        public object Parameters { get; set; } = null;
        public string UserAgent { get; set; } = "XIVMIDI CLIENT V2";
        public string Accept { get; set; } = "application/json;q=0.8"; //Default its json

        public HttpContent ResponseBody { get; set; } = null;
        public HttpStatusCode ResponseCode { get; set; } = HttpStatusCode.Unused;
        public string ResponseMsg { get; set; } = "";

        /// <summary>
        /// The API we are using: 0 = XIVMIDI 1 = BMP
        /// </summary>
        public int RequestSource { get; set; } = 0;
        public string Host { get; set; } = "";
        public string Referrer { get; set; } = "";

        public void Dispose()
        {
            ResponseBody.Dispose();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
