
using System;

namespace dotPLC.Initial
{
    /// <summary>
    /// Provides data for the Trouble event.
    /// </summary>
    public class TroubleshootingEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a error code of the server.
        /// </summary>
        public int ErrorCode { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="TroubleshootingEventArgs"></see> class.
        /// </summary>
        /// <param name="errorCode">A error code of the server.</param>
        public TroubleshootingEventArgs(int errorCode) => ErrorCode = errorCode;
    }
}
