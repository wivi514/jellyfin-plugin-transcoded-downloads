using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Manages transcoded download jobs.
    /// </summary>
    public interface ITranscodeJobService
    {
        /// <summary>
        /// Creates a queued transcode job.
        /// </summary>
        /// <param name="request">The create job request.</param>
        /// <param name="configuration">The plugin configuration.</param>
        /// <returns>The created job.</returns>
        DownloadJobDto CreateJob(CreateDownloadJobRequest request, PluginConfiguration configuration);

        /// <summary>
        /// Gets the current in-memory jobs.
        /// </summary>
        /// <returns>The known jobs.</returns>
        IReadOnlyList<DownloadJobDto> GetJobs();
    }
}
