using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class PluginConfigurationTests
    {
        [Fact]
        public void Constructor_CreatesValidDefaultPreset()
        {
            var configuration = new PluginConfiguration();
            var listingService = new PresetListingService();

            var presets = listingService.GetAvailablePresets(configuration);

            Assert.Single(configuration.CapabilityProfiles);
            Assert.Equal(8, configuration.Presets.Count);
            Assert.Equal(8, presets.Count);

            var compatibilityPreset = presets.Single(preset => preset.Id == "1080p-h264-aac-mp4");
            Assert.Equal(VideoCodec.H264, compatibilityPreset.VideoCodec);
            Assert.Equal(AudioCodec.Aac, compatibilityPreset.AudioCodec);
            Assert.Equal(ContainerFormat.Mp4, compatibilityPreset.Container);
            Assert.Equal(8000, compatibilityPreset.VideoBitrateKbps);
            Assert.Empty(compatibilityPreset.Warnings);

            Assert.Contains(presets, preset => preset.Id == "2160p-h264-aac-mp4");
            Assert.Contains(presets, preset => preset.Id == "1080p-h265-aac-mp4");
            Assert.Contains(presets, preset => preset.Id == "1080p-av1-opus-webm");
        }
    }
}
