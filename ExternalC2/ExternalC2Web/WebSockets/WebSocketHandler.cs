using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExternalC2;

namespace ExternalC2Web.WebSockets
{
    /// <summary>
    ///     Handles the WebSocket connections
    /// </summary>
    public abstract class WebSocketHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSocketHandler" /> class.
        /// </summary>
        /// <param name="webSocketManager">The web socket manager.</param>
        protected WebSocketHandler(ChannelManager<WebSocket> webSocketManager)
        {
            WebSocketManager = webSocketManager;
        }

        /// <summary>
        ///     Gets or sets the web socket manager.
        /// </summary>
        /// <value>
        ///     The web socket manager.
        /// </value>
        protected ChannelManager<WebSocket> WebSocketManager { get; set; }

        /// <summary>
        ///     Called when [connected].
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <returns></returns>
        public virtual async Task OnConnected(WebSocket socket)
        {
            await Task.Run(() => WebSocketManager.AddChannel(new BeaconId(), socket));
        }

        /// <summary>
        ///     Called when [disconnected].
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <returns></returns>
        public virtual async Task OnDisconnected(WebSocket socket)
        {
            await Task.Run(() => WebSocketManager.RemoveChannel(WebSocketManager.GetId(socket)));
        }

        /// <summary>
        ///     Sends the message.
        /// </summary>
        /// <param name="socketId">The socket identifier.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public async Task SendMessageAsync(BeaconId socketId, string message)
        {
            await SendMessageAsync(WebSocketManager.GetChannelById(socketId), message);
        }

        /// <summary>
        ///     Sends the message.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public async Task SendMessageAsync(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open)
                return;

            var messageBuffer = new ArraySegment<byte>(Encoding.ASCII.GetBytes(message), 0, message.Length);
            await socket.SendAsync(messageBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        ///     Sends the message to all.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public async Task SendMessageToAllAsync(string message)
        {
            foreach (var pair in WebSocketManager.GetAll())
                if (pair.Value.State == WebSocketState.Open)
                    await SendMessageAsync(pair.Value, message);
        }

        /// <summary>
        ///     Receives a buffer from the websocket.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="result">The result.</param>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    }
}