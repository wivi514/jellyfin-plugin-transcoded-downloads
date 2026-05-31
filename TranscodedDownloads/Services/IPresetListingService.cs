using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Builds the user-facing list of available presets.
    /// </summary>
    public interface IPresetListingService
    {
        /// <summary>
        /// Returns enabled presets that are valid for the configured capability profiles.
        /// </summary>
        /// <param name="configuration">The plugin configuration.</param>
        /// <returns>The available preset DTOs.</returns>
        IReadOnlyList<TranscodePresetDto> GetAvailablePresets(PluginConfiguration configuration);
    }
}
