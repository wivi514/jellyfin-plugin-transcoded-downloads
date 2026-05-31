using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Exceptions
{
    /// <summary>
    /// Exception thrown when a preset requests a capability that is not supported.
    /// </summary>
    public sealed class UnsupportedCapabilityException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedCapabilityException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public UnsupportedCapabilityException(string message)
            : base(message)
        {
        }
    }
}
