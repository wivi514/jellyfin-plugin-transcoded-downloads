using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        /// <summary>
        /// Gets a single job by ID.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>The job, or null when it does not exist.</returns>
        DownloadJobDto? GetJob(Guid jobId);

        /// <summary>
        /// Deletes or cancels a job by ID.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>True when the job exists; otherwise, false.</returns>
        bool DeleteJob(Guid jobId);

        /// <summary>
        /// Starts a queued transcode job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="configuration">The plugin configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>True when a queued job was found and processed; otherwise, false.</returns>
        Task<bool> StartJobAsync(
            Guid jobId,
            PluginConfiguration configuration,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the completed output file for a job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>The completed file lookup result.</returns>
        CompletedJobFile GetCompletedJobFile(Guid jobId);
    }
}
