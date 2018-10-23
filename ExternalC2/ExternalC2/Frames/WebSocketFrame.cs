using System;
using System.Text;

namespace ExternalC2.Frames
{
    /// <summary>
    ///     The frame used for WebSocket C2 communication with the ExternalC2Web server
    /// </summary>
    public class WebSocketFrame
    {
        /// <summary>
        ///     Constructor used to created the frame
        /// </summary>
        /// <param name="type"></param>
        /// <param name="beaconId"></param>
        /// <param name="buffer"></param>
        public WebSocketFrame(FrameType type, Guid beaconId, byte[] buffer)
        {
            Type = type;
            BeaconId = beaconId;
            Buffer = buffer;
        }

        /// <summary>
        ///     The type of frame
        /// </summary>
        public FrameType Type { get; }

        /// <summary>
        ///     The internal beacon identifier
        /// </summary>
        public Guid BeaconId { get; }

        /// <summary>
        ///     The message buffer
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        ///     Creates a WebSocketFrame from a base64 encoded string
        /// </summary>
        /// <param name="b64Str"></param>
        /// <returns>A WebSocketFrame</returns>
        /// <exception cref="Exception"></exception>
        public static WebSocketFrame Decode(string b64Str)
        {
            var str = Encoding.UTF8.GetString(Convert.FromBase64String(b64Str));
            var strParts = str.Split(':');
            if (strParts.Length != 3)
                throw new Exception("Invalid web socket frame");

            var type = (FrameType) Enum.Parse(typeof(FrameType), strParts[0]);
            return new WebSocketFrame(type, Guid.Parse(strParts[1]), Convert.FromBase64String(strParts[2]));
        }

        /// <summary>
        ///     Create a base64 encode string contained the WebSocketFrame
        /// </summary>
        /// <returns>Base64 encoded string: [FrameType]:[BeaconId]:[Base64 Buffer]</returns>
        public string Encode()
        {
            var str = $"{Type}:{BeaconId}:{Convert.ToBase64String(Buffer)}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        ///     Creates an ArraySegment with the buffer bytes
        /// </summary>
        /// <returns>ArraySegment of the buffer</returns>
        public ArraySegment<byte> SegmentBytes()
        {
            var msgBuffer = Encoding.UTF8.GetBytes(Encode());
            return new ArraySegment<byte>(msgBuffer, 0, msgBuffer.Length);
        }

        /// <summary>
        ///     Creates a human readable metadata
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Type: {Type}, BeaconId: {BeaconId}, Buffer Length: {Buffer.Length}";
        }
    }
}