using System;
using Jellyfin.Plugin.TranscodedDownloads.Enums;

namespace Jellyfin.Plugin.TranscodedDownloads.Models
{
    /// <summary>
    /// Transcoded download job details.
    /// </summary>
    public sealed class DownloadJobDto
    {
        /// <summary>
        /// Gets or sets the job ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the Jellyfin item ID.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the user ID that owns the job.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the selected preset ID.
        /// </summary>
        public string PresetId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the job status.
        /// </summary>
        public JobStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the best-effort progress percentage.
        /// </summary>
        public double ProgressPercent { get; set; }

        /// <summary>
        /// Gets or sets the completed output file name.
        /// </summary>
        public string? OutputFileName { get; set; }

        /// <summary>
        /// Gets or sets the completed output size in bytes.
        /// </summary>
        public long? OutputSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the user-facing error message.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the time the job was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the time the job started.
        /// </summary>
        public DateTimeOffset? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the time the job completed.
        /// </summary>
        public DateTimeOffset? CompletedAt { get; set; }
    }
}
