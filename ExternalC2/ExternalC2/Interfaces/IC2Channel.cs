using System;

namespace ExternalC2.Interfaces
{
    /// <summary>
    ///     An IC2Channel is used to communicate with the beacon and server protocols
    /// </summary>
    public interface IC2Channel : IDisposable
    {
        /// <summary>
        ///     Determines if the channel is connected
        /// </summary>
        bool Connected { get; }

        /// <summary>
        ///     Connects to the channel
        /// </summary>
        /// <returns>If the channel connected</returns>
        bool Connect();

        /// <summary>
        ///     Close the channel connection
        /// </summary>
        void Close();

        /// <summary>
        ///     Reads a frame from the channel
        /// </summary>
        /// <returns>The frame bytes</returns>
        byte[] ReadFrame();

        /// <summary>
        ///     Sends a frame to the channel
        /// </summary>
        /// <param name="buffer"></param>
        void SendFrame(byte[] buffer);

        /// <summary>
        ///     Reads a frame and sends it to the other channel
        /// </summary>
        /// <param name="c2"></param>
        /// <returns>If it was successful</returns>
        bool ReadAndSendTo(IC2Channel c2);

        /// <summary>
        ///     Requests a stager from the channel
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="is64Bit"></param>
        /// <param name="taskWaitTime"></param>
        /// <returns>The stager bytes</returns>
        byte[] GetStager(string pipeName, bool is64Bit, int taskWaitTime = 100);
    }
}