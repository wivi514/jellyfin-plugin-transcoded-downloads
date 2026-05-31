using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Exceptions
{
    /// <summary>
    /// Exception thrown when a preset cannot be used for a download job.
    /// </summary>
    public sealed class InvalidPresetException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidPresetException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public InvalidPresetException(string message)
            : base(message)
        {
        }
    }
}
