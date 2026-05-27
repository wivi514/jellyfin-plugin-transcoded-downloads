using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Builds FFmpeg commands for transcoding media.
    /// </summary>
    public sealed class TranscodeCommandBuilder : ITranscodeCommandBuilder
    {
        private readonly EncoderMap _encoderMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeCommandBuilder"/> class.
        /// </summary>
        public TranscodeCommandBuilder()
        {
            _encoderMap = new EncoderMap();
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
            AddSubtitleSettings(args, preset, capabilityProfile);
            AddToneMapping(args, preset, capabilityProfile);
            AddScaling(args, preset);
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

            // Add hardware acceleration flags if needed
            if (capabilityProfile.Backend == TranscodeBackend.Vaapi)
            {
                args.Add("-filter:v");
                args.Add("format=nv12|vaapi");
            }
            else if (capabilityProfile.Backend == TranscodeBackend.Qsv)
            {
                args.Add("-filter:v");
                args.Add("format=nv12|qsv");
            }
            else if (capabilityProfile.Backend == TranscodeBackend.Nvenc)
            {
                args.Add("-filter:v");
                args.Add("format=nv12|cuda");
            }
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

        private void AddSubtitleSettings(List<string> args, AdminTranscodePreset preset, CapabilityProfile capabilityProfile)
        {
            if (preset.BurnSubtitles)
            {
                if (capabilityProfile.SupportsSubtitleBurnIn)
                {
                    args.Add("-vf");
                    args.Add("subtitles");
                }
                else
                {
                    // If burn-in is not supported, we can't burn subtitles
                    // This should be handled by validation before calling this method
                }
            }
            else
            {
                args.Add("-sn"); // Disable subtitles
            }
        }

        private void AddToneMapping(List<string> args, AdminTranscodePreset preset, CapabilityProfile capabilityProfile)
        {
            if (preset.ToneMapHdrToSdr && capabilityProfile.SupportsToneMapping)
            {
                args.Add("-vf");
                args.Add("tonemap=hable");
            }
        }

        private void AddScaling(List<string> args, AdminTranscodePreset preset)
        {
            if (preset.MaxWidth.HasValue && preset.MaxHeight.HasValue)
            {
                args.Add("-vf");
                args.Add($"scale={preset.MaxWidth}:{preset.MaxHeight}");
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
``