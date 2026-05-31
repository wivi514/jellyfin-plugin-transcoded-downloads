using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Models
{
    /// <summary>
    /// Resolved Jellyfin media item information required to create a transcode job.
    /// </summary>
    public sealed class MediaItemInfo
    {
        /// <summary>
        /// Gets or sets the Jellyfin item ID.
        /// </summary>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the item display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source media path.
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the item is video media.
        /// </summary>
        public bool IsVideo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item is audio media.
        /// </summary>
        public bool IsAudio { get; set; }
    }
}
