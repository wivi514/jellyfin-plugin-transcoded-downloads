using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Runs FFmpeg for a transcoded download job.
    /// </summary>
    public interface ITranscodeProcessRunner
    {
        /// <summary>
        /// Runs FFmpeg for the selected preset and capability profile.
        /// </summary>
        /// <param name="preset">The validated preset.</param>
        /// <param name="capabilityProfile">The capability profile.</param>
        /// <param name="inputPath">The input media path.</param>
        /// <param name="outputPath">The output media path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The process result.</returns>
        Task<TranscodeProcessResult> RunAsync(
            AdminTranscodePreset preset,
            CapabilityProfile capabilityProfile,
            string inputPath,
            string outputPath,
            CancellationToken cancellationToken);
    }
}
