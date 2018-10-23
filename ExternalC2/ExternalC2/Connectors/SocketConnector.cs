using ExternalC2.Channels;
using ExternalC2.Interfaces;

namespace ExternalC2.Connectors
{
    /// <summary>
    ///     Base Socket C2 Connector for integrating the ServerChannel with another IC2Channel implementation
    /// </summary>
    public abstract class SocketConnector : BaseConnector
    {
        /// <summary>
        ///     Public default constructor for DotNetToJScript and Unit Tests
        /// </summary>
        protected SocketConnector()
        {
        }

        /// <summary>
        ///     Creates a new SocketConnector using the ipAddr and port for the ServerChannel
        ///     and the supplied IC2Channel for the BeaconChannel
        /// </summary>
        /// <param name="beaconChannel"></param>
        /// <param name="ipAddr"></param>
        /// <param name="port"></param>
        protected SocketConnector(IC2Channel beaconChannel, string ipAddr, string port)
            : base(beaconChannel, new SocketChannel(ipAddr, port))
        {
        }
    }
}