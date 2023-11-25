namespace dotPLC.Mitsubishi
{
    /// <summary>
    /// Specifies the remote control for the server.
    /// </summary>
    public enum RemoteControl
    {

        /// <summary>
        /// Remote RUN.
        /// </summary>
        /// <remarks>Forced execution allowed (Remote RUN can be executed when other device
        /// executes Remote STOP or Remote PAUSE.)</remarks>
        RUN_FORCE,
        /// <summary>
        /// Remote RUN.
        /// </summary>
        /// <remarks>Forced execution not allowed (Remote RUN cannot be executed when other device 
        /// executes Remote STOP or Remote PAUSE.)</remarks>
        RUN,
        /// <summary>
        /// Remote STOP.
        /// </summary>
        /// <remarks>Forced execution allowed (Remote RUN can be executed when other device
        /// executes Remote STOP or Remote PAUSE.)</remarks>
        STOP_FORCE,
        /// <summary>
        /// Remote STOP.
        /// </summary>
        /// <remarks>Forced execution not allowed (Remote RUN cannot be executed when other device
        /// executes Remote STOP or Remote PAUSE.) </remarks>
        STOP,
        /// <summary>
        /// Remote PAUSE.
        /// </summary>
        ///  /// <remarks>Forced execution not allowed (Remote RUN cannot be executed when other device
        /// executes Remote STOP or Remote PAUSE.) </remarks>
        PAUSE,
        /// <summary>
        /// Remote PAUSE.
        /// </summary>
        /// /// <remarks>Forced execution allowed (Remote RUN can be executed when other device
        /// executes Remote STOP or Remote PAUSE.)</remarks>
        PAUSE_FORCE,
        /// <summary>
        /// Remote CLEAR ERROR.
        /// </summary>
        CLEAR_ERROR,
        /// <summary>
        /// Remote RESET.
        /// </summary>
        RESET,
        /// <summary>
        /// Remote CLEAR LATCH.
        /// </summary>
        CLEAR_LATCH,
    }
}


