namespace ExternalC2.Frames
{
    /// <summary>
    ///     The frame type
    /// </summary>
    public enum FrameType
    {
        /// <summary>
        ///     Used for the initial connection
        /// </summary>
        Connect,
        /// <summary>
        ///     Used for the stager request
        /// </summary>
        Stager,
        /// <summary>
        ///     Used for frames going to the server
        /// </summary>
        ToServer,
        /// <summary>
        ///     Used for frames going to the beacon
        /// </summary>
        ToBeacon
    }
}