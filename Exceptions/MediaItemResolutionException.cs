using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Exceptions
{
    /// <summary>
    /// Exception thrown when a Jellyfin item cannot be resolved to a usable media source.
    /// </summary>
    public sealed class MediaItemResolutionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaItemResolutionException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public MediaItemResolutionException(string message)
            : base(message)
        {
        }
    }
}
