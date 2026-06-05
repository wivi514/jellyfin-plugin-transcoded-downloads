using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Builds FFmpeg commands for transcoding media.
    /// </summary>
    public sealed class TranscodeCommandBuilder : ITranscodeCommandBuilder
    {
        private readonly EncoderMap _encoderMap;
        private readonly IPresetValidator _presetValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeCommandBuilder"/> class.
        /// </summary>
        public TranscodeCommandBuilder()
        {
            _encoderMap = new EncoderMap();
            _presetValidator = new PresetValidator();
        }

        /// <inheritdoc />
        public List<string> BuildCommand(
            AdminTranscodePreset preset,
            CapabilityProfile capabilityProfile,
            string inputPath,
            string outputPath)
        {
            if (preset == null)
                throw new ArgumentNullException(nameof(preset));
            if (capabilityProfile == null)
                throw new ArgumentNullException(nameof(capabilityProfile));
            if (string.IsNullOrEmpty(inputPath))
                throw new ArgumentException("Input path cannot be null or empty", nameof(inputPath));
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            var validationResult = _presetValidator.Validate(preset, new[] { capabilityProfile });
            if (!validationResult.IsValid)
            {
                throw new UnsupportedCapabilityException(
                    $"Preset '{preset.Id}' is not valid for capability profile '{capabilityProfile.Id}': {string.Join(" ", validationResult.Errors)}");
            }

            var args = new List<string>
            {
                "-hide_banner",
                "-y",
                "-i", inputPath
            };

            // Map the preset to FFmpeg arguments
            AddStreamSelection(args);
            AddVideoEncoding(args, preset, capabilityProfile);
            AddAudioEncoding(args, preset, capabilityProfile);
            AddContainerSettings(args, preset);
            AddSubtitleSelection(args, preset);
            AddVideoFilters(args, preset, capabilityProfile);
            AddBitrateSettings(args, preset);

            args.Add(outputPath);

            return args;
        }

        private void AddStreamSelection(List<string> args)
        {
            // Select video stream
            args.Add("-map");
            args.Add("0:v:0");

            // Select audio stream
            args.Add("-map");
            args.Add("0:a:0?");
        }

        private void AddVideoEncoding(List<string> args, AdminTranscodePreset preset, CapabilityProfile capabilityProfile)
        {
            if (preset.IsAudioOnlyPreset)
            {
                args.Add("-vn");
                return;
            }

            var videoEncoder = _encoderMap.ResolveVideoEncoder(capabilityProfile.Backend, preset.VideoCodec);
            args.Add("-c:v");
            args.Add(videoEncoder);
        }

        private void AddAudioEncoding(List<string> args, AdminTranscodePreset preset, CapabilityProfile capabilityProfile)
        {
            if (preset.IsVideoPreset && !preset.IsAudioOnlyPreset)
            {
                var audioEncoder = _encoderMap.ResolveAudioEncoder(capabilityProfile.Backend, preset.AudioCodec);
                args.Add("-c:a");
                args.Add(audioEncoder);
            }
            else
            {
                args.Add("-c:a");
                args.Add("copy");
            }
        }

        private void AddContainerSettings(List<string> args, AdminTranscodePreset preset)
        {
            // Container settings are handled by the output format
            // The output path extension determines the container
        }

        private void AddSubtitleSelection(List<string> args, AdminTranscodePreset preset)
        {
            if (!preset.BurnSubtitles)
            {
                args.Add("-sn");
            }
        }

        private void AddVideoFilters(List<string> args, AdminTranscodePreset preset, CapabilityProfile capabilityProfile)
        {
            if (preset.IsAudioOnlyPreset)
            {
                return;
            }

            var filters = new List<string>();

            AddHardwareFormatFilter(filters, capabilityProfile);

            if (preset.BurnSubtitles)
            {
                filters.Add("subtitles");
            }

            if (preset.ToneMapHdrToSdr && capabilityProfile.SupportsToneMapping)
            {
                filters.Add("tonemap=hable");
            }

            if (preset.MaxWidth.HasValue && preset.MaxHeight.HasValue)
            {
                filters.Add($"scale={preset.MaxWidth}:{preset.MaxHeight}");
            }

            if (filters.Count == 0)
            {
                return;
            }

            args.Add("-filter:v");
            args.Add(string.Join(",", filters));
        }

        private static void AddHardwareFormatFilter(List<string> filters, CapabilityProfile capabilityProfile)
        {
            var filter = capabilityProfile.Backend switch
            {
                TranscodeBackend.Vaapi => "format=nv12|vaapi",
                TranscodeBackend.Qsv => "format=nv12|qsv",
                TranscodeBackend.Nvenc => "format=nv12|cuda",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                filters.Add(filter);
            }
        }

        private void AddBitrateSettings(List<string> args, AdminTranscodePreset preset)
        {
            if (preset.VideoBitrateKbps.HasValue)
            {
                args.Add("-b:v");
                args.Add($"{preset.VideoBitrateKbps}k");
            }

            if (preset.AudioBitrateKbps.HasValue)
            {
                args.Add("-b:a");
                args.Add($"{preset.AudioBitrateKbps}k");
            }

            if (preset.AudioChannels.HasValue)
            {
                args.Add("-ac");
                args.Add($"{preset.AudioChannels}");
            }
        }
    }
}
