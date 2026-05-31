using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class PresetValidatorTests
    {
        private readonly PresetValidator _validator = new PresetValidator();

        [Fact]
        public void Validate_WithCpuH264Preset_ReturnsValidResult()
        {
            var profile = CreateCpuProfile();
            var preset = CreateVideoPreset(profile.Id);

            var result = _validator.Validate(preset, new[] { profile });

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WhenCapabilityProfileIsMissing_ReturnsInvalidResult()
        {
            var preset = CreateVideoPreset("missing-profile");

            var result = _validator.Validate(preset, System.Array.Empty<CapabilityProfile>());

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("capability profile"));
        }

        [Fact]
        public void Validate_WhenPresetIsDisabled_ReturnsInvalidResult()
        {
            var profile = CreateCpuProfile();
            var preset = CreateVideoPreset(profile.Id);
            preset.Enabled = false;

            var result = _validator.Validate(preset, new[] { profile });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("disabled"));
        }

        [Fact]
        public void Validate_WhenVideoCodecIsNotAllowed_ReturnsInvalidResult()
        {
            var profile = CreateCpuProfile();
            profile.AllowedVideoCodecs = new List<VideoCodec> { VideoCodec.H265 };
            var preset = CreateVideoPreset(profile.Id);

            var result = _validator.Validate(preset, new[] { profile });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("video codec"));
        }

        [Fact]
        public void Validate_WhenAudioCodecIsNotAllowed_ReturnsInvalidResult()
        {
            var profile = CreateCpuProfile();
            profile.AllowedAudioCodecs = new List<AudioCodec> { AudioCodec.Opus };
            var preset = CreateVideoPreset(profile.Id);

            var result = _validator.Validate(preset, new[] { profile });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("audio codec"));
        }

        [Fact]
        public void Validate_WhenContainerIsNotAllowed_ReturnsInvalidResult()
        {
            var profile = CreateCpuProfile();
            profile.AllowedContainers = new List<ContainerFormat> { ContainerFormat.Mkv };
            var preset = CreateVideoPreset(profile.Id);

            var result = _validator.Validate(preset, new[] { profile });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("container"));
        }

        [Fact]
        public void Validate_WhenBitrateOrResolutionIsNotPositive_ReturnsInvalidResult()
        {
            var profile = CreateCpuProfile();
            var preset = CreateVideoPreset(profile.Id);
            preset.MaxWidth = 0;
            preset.VideoBitrateKbps = -1;
            preset.AudioBitrateKbps = 0;

            var result = _validator.Validate(preset, new[] { profile });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("Maximum width"));
            Assert.Contains(result.Errors, error => error.Contains("Video bitrate"));
            Assert.Contains(result.Errors, error => error.Contains("Audio bitrate"));
        }

        [Fact]
        public void Validate_WhenSubtitleBurnInIsUnsupported_ReturnsInvalidResult()
        {
            var profile = CreateCpuProfile();
            profile.SupportsSubtitleBurnIn = false;
            var preset = CreateVideoPreset(profile.Id);
            preset.BurnSubtitles = true;

            var result = _validator.Validate(preset, new[] { profile });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("Subtitle burn-in"));
        }

        [Fact]
        public void Validate_WhenToneMappingIsUnsupported_ReturnsValidResultWithWarning()
        {
            var profile = CreateCpuProfile();
            profile.SupportsToneMapping = false;
            var preset = CreateVideoPreset(profile.Id);
            preset.ToneMapHdrToSdr = true;

            var result = _validator.Validate(preset, new[] { profile });

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.Contains(result.Warnings, warning => warning.Contains("HDR tone mapping"));
        }

        [Fact]
        public void Validate_WhenAudioOnlyPresetHasVideoSettings_ReturnsInvalidResult()
        {
            var profile = CreateCpuProfile();
            var preset = CreateAudioOnlyPreset(profile.Id);
            preset.VideoBitrateKbps = 8000;

            var result = _validator.Validate(preset, new[] { profile });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("video resolution or bitrate"));
        }

        [Fact]
        public void Validate_WithAudioOnlyMp3Preset_ReturnsValidResult()
        {
            var profile = CreateCpuProfile();
            var preset = CreateAudioOnlyPreset(profile.Id);

            var result = _validator.Validate(preset, new[] { profile });

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WhenHardwareProfileRequiresMissingDevicePath_ReturnsInvalidResult()
        {
            var profile = CreateCpuProfile();
            profile.Backend = TranscodeBackend.Vaapi;
            profile.DevicePath = string.Empty;
            var preset = CreateVideoPreset(profile.Id);

            var result = _validator.Validate(preset, new[] { profile });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("device path"));
        }

        [Fact]
        public void BuildCommand_WhenPresetIsInvalid_ThrowsUnsupportedCapabilityException()
        {
            var profile = CreateCpuProfile();
            profile.AllowedVideoCodecs = new List<VideoCodec> { VideoCodec.H265 };
            var preset = CreateVideoPreset(profile.Id);
            var commandBuilder = new TranscodeCommandBuilder();

            Assert.Throws<UnsupportedCapabilityException>(
                () => commandBuilder.BuildCommand(preset, profile, "/media/input.mkv", "/tmp/output.mp4"));
        }

        private static CapabilityProfile CreateCpuProfile()
        {
            return new CapabilityProfile
            {
                Id = "cpu-profile",
                Name = "CPU H.264",
                Backend = TranscodeBackend.Software,
                AllowedVideoCodecs = new List<VideoCodec> { VideoCodec.Copy, VideoCodec.H264 },
                AllowedAudioCodecs = new List<AudioCodec> { AudioCodec.Copy, AudioCodec.Aac, AudioCodec.Mp3 },
                AllowedContainers = new List<ContainerFormat> { ContainerFormat.Mp4, ContainerFormat.Mp3 },
                SupportsSubtitleBurnIn = true,
                SupportsToneMapping = true
            };
        }

        private static AdminTranscodePreset CreateVideoPreset(string capabilityProfileId)
        {
            return new AdminTranscodePreset
            {
                Id = "video-preset",
                Name = "1080p H.264 AAC MP4 - CPU Compatible",
                Enabled = true,
                CapabilityProfileId = capabilityProfileId,
                Container = ContainerFormat.Mp4,
                VideoCodec = VideoCodec.H264,
                AudioCodec = AudioCodec.Aac,
                MaxWidth = 1920,
                MaxHeight = 1080,
                VideoBitrateKbps = 8000,
                AudioBitrateKbps = 192,
                AudioChannels = 2,
                IsVideoPreset = true,
                IsAudioOnlyPreset = false,
                ToneMapHdrToSdr = false
            };
        }

        private static AdminTranscodePreset CreateAudioOnlyPreset(string capabilityProfileId)
        {
            return new AdminTranscodePreset
            {
                Id = "audio-preset",
                Name = "Music MP3 320 kbps",
                Enabled = true,
                CapabilityProfileId = capabilityProfileId,
                Container = ContainerFormat.Mp3,
                VideoCodec = VideoCodec.Copy,
                AudioCodec = AudioCodec.Mp3,
                MaxWidth = null,
                MaxHeight = null,
                VideoBitrateKbps = null,
                AudioBitrateKbps = 320,
                AudioChannels = 2,
                IsVideoPreset = false,
                IsAudioOnlyPreset = true,
                BurnSubtitles = false,
                ToneMapHdrToSdr = false
            };
        }
    }
}
