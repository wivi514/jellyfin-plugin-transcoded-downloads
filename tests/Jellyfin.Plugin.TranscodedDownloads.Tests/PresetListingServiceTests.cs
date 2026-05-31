using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class PresetListingServiceTests
    {
        private readonly PresetListingService _service = new PresetListingService();

        [Fact]
        public void GetAvailablePresets_ReturnsOnlyEnabledValidPresets()
        {
            var profile = CreateCpuProfile();
            var validPreset = CreateVideoPreset(profile.Id, "valid-preset");
            var disabledPreset = CreateVideoPreset(profile.Id, "disabled-preset");
            disabledPreset.Enabled = false;
            var invalidPreset = CreateVideoPreset(profile.Id, "invalid-preset");
            invalidPreset.VideoCodec = VideoCodec.H265;

            var configuration = new PluginConfiguration
            {
                CapabilityProfiles = new List<CapabilityProfile> { profile },
                Presets = new List<AdminTranscodePreset> { validPreset, disabledPreset, invalidPreset }
            };

            var presets = _service.GetAvailablePresets(configuration);

            var preset = Assert.Single(presets);
            Assert.Equal("valid-preset", preset.Id);
        }

        [Fact]
        public void GetAvailablePresets_IncludesValidationWarnings()
        {
            var profile = CreateCpuProfile();
            profile.SupportsToneMapping = false;
            var preset = CreateVideoPreset(profile.Id, "warning-preset");
            preset.ToneMapHdrToSdr = true;

            var configuration = new PluginConfiguration
            {
                CapabilityProfiles = new List<CapabilityProfile> { profile },
                Presets = new List<AdminTranscodePreset> { preset }
            };

            var presets = _service.GetAvailablePresets(configuration);

            var dto = Assert.Single(presets);
            Assert.Equal("warning-preset", dto.Id);
            Assert.Contains(dto.Warnings, warning => warning.Contains("HDR tone mapping"));
        }

        [Fact]
        public void GetAvailablePresets_MapsPresetFields()
        {
            var profile = CreateCpuProfile();
            var preset = CreateVideoPreset(profile.Id, "mapped-preset");

            var configuration = new PluginConfiguration
            {
                CapabilityProfiles = new List<CapabilityProfile> { profile },
                Presets = new List<AdminTranscodePreset> { preset }
            };

            var dto = _service.GetAvailablePresets(configuration).Single();

            Assert.Equal(preset.Name, dto.Name);
            Assert.Equal(preset.CapabilityProfileId, dto.CapabilityProfileId);
            Assert.Equal(preset.Container, dto.Container);
            Assert.Equal(preset.VideoCodec, dto.VideoCodec);
            Assert.Equal(preset.AudioCodec, dto.AudioCodec);
            Assert.Equal(preset.MaxWidth, dto.MaxWidth);
            Assert.Equal(preset.MaxHeight, dto.MaxHeight);
            Assert.Equal(preset.VideoBitrateKbps, dto.VideoBitrateKbps);
            Assert.Equal(preset.AudioBitrateKbps, dto.AudioBitrateKbps);
            Assert.Equal(preset.AudioChannels, dto.AudioChannels);
            Assert.Equal(preset.AllowStreamCopyWhenCompatible, dto.AllowStreamCopyWhenCompatible);
            Assert.Equal(preset.BurnSubtitles, dto.BurnSubtitles);
            Assert.Equal(preset.ToneMapHdrToSdr, dto.ToneMapHdrToSdr);
            Assert.Equal(preset.IsVideoPreset, dto.IsVideoPreset);
            Assert.Equal(preset.IsAudioOnlyPreset, dto.IsAudioOnlyPreset);
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

        private static AdminTranscodePreset CreateVideoPreset(string capabilityProfileId, string id)
        {
            return new AdminTranscodePreset
            {
                Id = id,
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
    }
}
