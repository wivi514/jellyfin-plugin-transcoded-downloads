using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Builds FFmpeg argument lists for transcoded download jobs.
    /// </summary>
    public interface ITranscodeCommandBuilder
    {
        /// <summary>
        /// Builds FFmpeg arguments for the given preset, capability profile, and file paths.
        /// </summary>
        /// <param name="preset">The validated admin preset.</param>
        /// <param name="capabilityProfile">The capability profile selected by the preset.</param>
        /// <param name="inputPath">The input media path.</param>
        /// <param name="outputPath">The output file path.</param>
        /// <returns>An ordered FFmpeg argument list.</returns>
        List<string> BuildCommand(
            AdminTranscodePreset preset,
            CapabilityProfile capabilityProfile,
            string inputPath,
            string outputPath);
    }
}
