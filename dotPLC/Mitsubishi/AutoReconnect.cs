namespace dotPLC.Mitsubishi
{
    /// <summary>
    /// Specifies auto-reconnect mode when connection is lost.
    /// </summary>
    public enum AutoReconnect
    {
        /// <summary>
        /// Disable auto-reconnect to the server.
        /// </summary>
        None,
        /// <summary>
        /// Enable auto-reconnect to the server, with a limit interval for the attempt to reconnect to the server.
        /// </summary>
        Limit,
        /// <summary>
        /// Enable auto-reconnect to the server.
        /// </summary>
        Always,
        /// <summary>
        /// Just enable detect disconnections
        /// </summary>
        JustDetect
    }
}