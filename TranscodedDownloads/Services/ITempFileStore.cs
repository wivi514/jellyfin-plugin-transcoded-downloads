using System;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Manages temporary files for transcoded downloads.
    /// </summary>
    public interface ITempFileStore
    {
        /// <summary>
        /// Creates a job output reservation under the configured temporary root.
        /// </summary>
        /// <param name="configuration">The plugin configuration.</param>
        /// <param name="jobId">The job ID.</param>
        /// <param name="itemName">The media item display name.</param>
        /// <param name="preset">The preset used for the output.</param>
        /// <returns>The reserved output path information.</returns>
        TempFileReservation ReserveOutputFile(
            PluginConfiguration configuration,
            Guid jobId,
            string itemName,
            AdminTranscodePreset preset);

        /// <summary>
        /// Deletes a job temporary directory if it exists.
        /// </summary>
        /// <param name="configuration">The plugin configuration.</param>
        /// <param name="jobId">The job ID.</param>
        /// <returns>True when a directory was deleted; otherwise, false.</returns>
        bool DeleteJobDirectory(PluginConfiguration configuration, Guid jobId);
    }
}
