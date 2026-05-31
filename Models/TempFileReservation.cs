using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Models
{
    /// <summary>
    /// Temporary output file reservation for a transcode job.
    /// </summary>
    public sealed class TempFileReservation
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public Guid JobId { get; set; }

        /// <summary>
        /// Gets or sets the job-specific temporary directory.
        /// </summary>
        public string JobDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the safe output file name.
        /// </summary>
        public string OutputFileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the full output path.
        /// </summary>
        public string OutputPath { get; set; } = string.Empty;
    }
}
