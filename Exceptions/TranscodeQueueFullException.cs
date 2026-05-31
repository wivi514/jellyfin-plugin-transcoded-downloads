using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Exceptions
{
    /// <summary>
    /// Exception thrown when the transcode job queue is full.
    /// </summary>
    public sealed class TranscodeQueueFullException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeQueueFullException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public TranscodeQueueFullException(string message)
            : base(message)
        {
        }
    }
}
