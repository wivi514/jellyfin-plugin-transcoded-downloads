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
            Assert.Single(configuration.Presets);
            Assert.Single(presets);
            Assert.Equal("1080p-h264-aac-mp4", presets[0].Id);
            Assert.Equal(VideoCodec.H264, presets[0].VideoCodec);
            Assert.Equal(AudioCodec.Aac, presets[0].AudioCodec);
            Assert.Equal(ContainerFormat.Mp4, presets[0].Container);
            Assert.Empty(presets[0].Warnings);
        }
    }
}
