using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Builds the user-facing list of available presets.
    /// </summary>
    public sealed class PresetListingService : IPresetListingService
    {
        private readonly IPresetValidator _presetValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="PresetListingService"/> class.
        /// </summary>
        public PresetListingService()
            : this(new PresetValidator())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PresetListingService"/> class.
        /// </summary>
        /// <param name="presetValidator">The preset validator.</param>
        public PresetListingService(IPresetValidator presetValidator)
        {
            _presetValidator = presetValidator ?? throw new ArgumentNullException(nameof(presetValidator));
        }

        /// <inheritdoc />
        public IReadOnlyList<TranscodePresetDto> GetAvailablePresets(PluginConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var presets = new List<TranscodePresetDto>();

            foreach (var preset in configuration.Presets)
            {
                var validationResult = _presetValidator.Validate(preset, configuration.CapabilityProfiles);
                if (!validationResult.IsValid)
                {
                    continue;
                }

                presets.Add(ToDto(preset, validationResult.Warnings));
            }

            return presets;
        }

        private static TranscodePresetDto ToDto(AdminTranscodePreset preset, IReadOnlyList<string> warnings)
        {
            return new TranscodePresetDto
            {
                Id = preset.Id,
                Name = preset.Name,
                CapabilityProfileId = preset.CapabilityProfileId,
                Container = preset.Container,
                VideoCodec = preset.VideoCodec,
                AudioCodec = preset.AudioCodec,
                MaxWidth = preset.MaxWidth,
                MaxHeight = preset.MaxHeight,
                VideoBitrateKbps = preset.VideoBitrateKbps,
                AudioBitrateKbps = preset.AudioBitrateKbps,
                AudioChannels = preset.AudioChannels,
                AllowStreamCopyWhenCompatible = preset.AllowStreamCopyWhenCompatible,
                BurnSubtitles = preset.BurnSubtitles,
                ToneMapHdrToSdr = preset.ToneMapHdrToSdr,
                IsVideoPreset = preset.IsVideoPreset,
                IsAudioOnlyPreset = preset.IsAudioOnlyPreset,
                Warnings = new List<string>(warnings)
            };
        }
    }
}
