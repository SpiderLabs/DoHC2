namespace ExternalC2Web
{
    /// <summary>
    ///     Settings for connecting to the External C2 server
    /// </summary>
    public class SocketSettings
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SocketSettings" /> class.
        /// </summary>
        public SocketSettings()
        {
            IpAddress = "127.0.0.1";
            Port = "2222";
        }

        /// <summary>
        ///     Gets or sets the ip address.
        /// </summary>
        /// <value>
        ///     The ip address.
        /// </value>
        public string IpAddress { get; set; }

        /// <summary>
        ///     Gets or sets the port.
        /// </summary>
        /// <value>
        ///     The port.
        /// </value>
        public string Port { get; set; }
    }
}