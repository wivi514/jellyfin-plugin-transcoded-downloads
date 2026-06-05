using System;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Starts queued transcode jobs outside the request path.
    /// </summary>
    public interface ITranscodeJobStarter
    {
        /// <summary>
        /// Starts the queued job in the background.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <param name="configuration">The plugin configuration.</param>
        void StartJob(Guid jobId, PluginConfiguration configuration);
    }
}
