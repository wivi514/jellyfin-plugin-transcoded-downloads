using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Starts transcode jobs on the thread pool.
    /// </summary>
    public sealed class BackgroundTranscodeJobStarter : ITranscodeJobStarter
    {
        private readonly ITranscodeJobService _transcodeJobService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundTranscodeJobStarter"/> class.
        /// </summary>
        /// <param name="transcodeJobService">The transcode job service.</param>
        public BackgroundTranscodeJobStarter(ITranscodeJobService transcodeJobService)
        {
            _transcodeJobService = transcodeJobService ?? throw new ArgumentNullException(nameof(transcodeJobService));
        }

        /// <inheritdoc />
        public void StartJob(Guid jobId, PluginConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _ = Task.Run(() => _transcodeJobService.StartJobAsync(jobId, configuration, CancellationToken.None));
        }
    }
}
