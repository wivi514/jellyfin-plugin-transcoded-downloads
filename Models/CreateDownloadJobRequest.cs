using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Models
{
    /// <summary>
    /// Request to create a transcoded download job.
    /// </summary>
    public sealed class CreateDownloadJobRequest
    {
        /// <summary>
        /// Gets or sets the Jellyfin item ID to transcode.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the preset ID to use.
        /// </summary>
        public string PresetId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the job should start immediately.
        /// </summary>
        public bool StartImmediately { get; set; } = true;
    }
}
