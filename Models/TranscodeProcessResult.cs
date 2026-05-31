namespace Jellyfin.Plugin.TranscodedDownloads.Models
{
    /// <summary>
    /// Result of an FFmpeg transcode process.
    /// </summary>
    public sealed class TranscodeProcessResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether FFmpeg completed successfully.
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Gets or sets the FFmpeg process exit code.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Gets or sets a concise error message or stderr summary.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful result.
        /// </summary>
        /// <returns>The successful result.</returns>
        public static TranscodeProcessResult Success()
        {
            return new TranscodeProcessResult
            {
                Succeeded = true,
                ExitCode = 0
            };
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        /// <param name="exitCode">The process exit code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>The failed result.</returns>
        public static TranscodeProcessResult Failure(int exitCode, string errorMessage)
        {
            return new TranscodeProcessResult
            {
                Succeeded = false,
                ExitCode = exitCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
