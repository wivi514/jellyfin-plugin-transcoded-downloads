using Jellyfin.Plugin.TranscodedDownloads.Enums;

namespace Jellyfin.Plugin.TranscodedDownloads.Models
{
    /// <summary>
    /// Internal completed job file lookup result.
    /// </summary>
    public sealed class CompletedJobFile
    {
        /// <summary>
        /// Gets or sets the lookup status.
        /// </summary>
        public CompletedJobFileStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the internal file path.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets the download file name.
        /// </summary>
        public string? DownloadFileName { get; set; }

        /// <summary>
        /// Gets or sets the response content type.
        /// </summary>
        public string? ContentType { get; set; }
    }
}
