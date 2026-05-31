using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Validates admin-defined transcode presets against configured server capabilities.
    /// </summary>
    public sealed class PresetValidator : IPresetValidator
    {
        /// <inheritdoc />
        public PresetValidationResult Validate(
            AdminTranscodePreset preset,
            IReadOnlyCollection<CapabilityProfile> capabilityProfiles)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (capabilityProfiles == null)
            {
                throw new ArgumentNullException(nameof(capabilityProfiles));
            }

            var result = new PresetValidationResult();

            ValidatePresetState(preset, result);
            ValidatePositiveNumbers(preset, result);
            ValidateAudioOnlyPreset(preset, result);

            var capabilityProfile = capabilityProfiles.FirstOrDefault(profile => profile.Id == preset.CapabilityProfileId);
            if (capabilityProfile == null)
            {
                result.Errors.Add("The preset does not reference a configured capability profile.");
                Complete(result);
                return result;
            }

            ValidateCapabilityProfile(preset, capabilityProfile, result);
            Complete(result);
            return result;
        }

        private static void ValidatePresetState(AdminTranscodePreset preset, PresetValidationResult result)
        {
            if (!preset.Enabled)
            {
                result.Errors.Add("The preset is disabled.");
            }

            if (string.IsNullOrWhiteSpace(preset.CapabilityProfileId))
            {
                result.Errors.Add("The preset must reference a capability profile.");
            }
        }

        private static void ValidatePositiveNumbers(AdminTranscodePreset preset, PresetValidationResult result)
        {
            AddPositiveValueError(result, preset.MaxWidth, "Maximum width");
            AddPositiveValueError(result, preset.MaxHeight, "Maximum height");
            AddPositiveValueError(result, preset.VideoBitrateKbps, "Video bitrate");
            AddPositiveValueError(result, preset.AudioBitrateKbps, "Audio bitrate");
            AddPositiveValueError(result, preset.AudioChannels, "Audio channels");
        }

        private static void AddPositiveValueError(PresetValidationResult result, int? value, string label)
        {
            if (value.HasValue && value.Value <= 0)
            {
                result.Errors.Add($"{label} must be greater than zero when set.");
            }
        }

        private static void ValidateAudioOnlyPreset(AdminTranscodePreset preset, PresetValidationResult result)
        {
            if (!preset.IsAudioOnlyPreset)
            {
                return;
            }

            if (preset.IsVideoPreset)
            {
                result.Errors.Add("Audio-only presets must not also be marked as video presets.");
            }

            if (preset.VideoCodec != VideoCodec.Copy)
            {
                result.Errors.Add("Audio-only presets must not select a video codec.");
            }

            if (preset.MaxWidth.HasValue || preset.MaxHeight.HasValue || preset.VideoBitrateKbps.HasValue)
            {
                result.Errors.Add("Audio-only presets must not define video resolution or bitrate settings.");
            }

            if (preset.BurnSubtitles)
            {
                result.Errors.Add("Audio-only presets must not enable subtitle burn-in.");
            }

            if (preset.ToneMapHdrToSdr)
            {
                result.Errors.Add("Audio-only presets must not enable HDR tone mapping.");
            }
        }

        private static void ValidateCapabilityProfile(
            AdminTranscodePreset preset,
            CapabilityProfile capabilityProfile,
            PresetValidationResult result)
        {
            if (!preset.IsAudioOnlyPreset && !capabilityProfile.AllowedVideoCodecs.Contains(preset.VideoCodec))
            {
                result.Errors.Add("The selected video codec is not allowed by the capability profile.");
            }

            if (!capabilityProfile.AllowedAudioCodecs.Contains(preset.AudioCodec))
            {
                result.Errors.Add("The selected audio codec is not allowed by the capability profile.");
            }

            if (!capabilityProfile.AllowedContainers.Contains(preset.Container))
            {
                result.Errors.Add("The selected container is not allowed by the capability profile.");
            }

            if (preset.BurnSubtitles && !capabilityProfile.SupportsSubtitleBurnIn)
            {
                result.Errors.Add("Subtitle burn-in is enabled but not supported by the capability profile.");
            }

            if (preset.ToneMapHdrToSdr && !capabilityProfile.SupportsToneMapping)
            {
                result.Warnings.Add("HDR tone mapping is enabled but not marked as supported by the capability profile.");
            }

            if (RequiresHardwareDevice(capabilityProfile) && string.IsNullOrWhiteSpace(capabilityProfile.DevicePath))
            {
                result.Errors.Add("The selected hardware backend requires a configured device path.");
            }
        }

        private static bool RequiresHardwareDevice(CapabilityProfile capabilityProfile)
        {
            return capabilityProfile.Backend == TranscodeBackend.Vaapi
                || capabilityProfile.Backend == TranscodeBackend.Qsv
                || capabilityProfile.Backend == TranscodeBackend.Rkmpp;
        }

        private static void Complete(PresetValidationResult result)
        {
            result.IsValid = result.Errors.Count == 0;
        }
    }
}
