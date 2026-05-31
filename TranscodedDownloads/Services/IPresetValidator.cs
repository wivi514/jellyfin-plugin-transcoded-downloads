using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Validates admin-defined transcode presets against configured server capabilities.
    /// </summary>
    public interface IPresetValidator
    {
        /// <summary>
        /// Validates a preset against the available capability profiles.
        /// </summary>
        /// <param name="preset">The preset to validate.</param>
        /// <param name="capabilityProfiles">The server capability profiles.</param>
        /// <returns>The validation result.</returns>
        PresetValidationResult Validate(
            AdminTranscodePreset preset,
            IReadOnlyCollection<CapabilityProfile> capabilityProfiles);
    }
}
