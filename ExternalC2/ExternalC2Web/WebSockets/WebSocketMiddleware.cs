using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ExternalC2Web.WebSockets
{
    /// <summary>
    /// </summary>
    public class WebSocketManagerMiddleware
    {
        /// <summary>
        ///     The maximum buffer size
        /// </summary>
        private const int MaxBufferSize = 1024 * 4;

        /// <summary>
        ///     The next
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        ///     The web socket handler
        /// </summary>
        private readonly WebSocketHandler _webSocketHandler;

        /// <summary>
        ///     Initializes a new instance of the <see cref="WebSocketManagerMiddleware" /> class.
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="webSocketHandler">The web socket handler.</param>
        public WebSocketManagerMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        /// <summary>
        ///     Invokes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.OnConnected(socket);

            await Receive(socket, async (result, buffer) =>
            {
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        await _webSocketHandler.ReceiveAsync(socket, result, buffer);
                        break;
                    case WebSocketMessageType.Close:
                        await _webSocketHandler.OnDisconnected(socket);
                        break;
                }
            });
        }

        /// <summary>
        ///     Receives the specified socket.
        /// </summary>
        /// <param name="socket">The socket.</param>
        /// <param name="handleMessage">The handle message.</param>
        /// <returns></returns>
        private static async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[MaxBufferSize];

            while (socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                var messageStr = new StringBuilder();
                do
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                            CancellationToken.None);
                    else
                        messageStr.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                } while (!result.EndOfMessage);

                handleMessage(result, Encoding.UTF8.GetBytes(messageStr.ToString()));
            }
        }
    }
}