using Jellyfin.Plugin.TranscodedDownloads.Services;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class TranscodeProcessRunnerTests
    {
        [Fact]
        public void ResolveDefaultFfmpegPath_WhenConfiguredPathIsPresent_ReturnsConfiguredPath()
        {
            var path = TranscodeProcessRunner.ResolveDefaultFfmpegPath("/usr/lib/jellyfin-ffmpeg/ffmpeg");

            Assert.Equal("/usr/lib/jellyfin-ffmpeg/ffmpeg", path);
        }

        [Fact]
        public void ResolveDefaultFfmpegPath_WhenConfiguredPathIsEmpty_ReturnsFallback()
        {
            var path = TranscodeProcessRunner.ResolveDefaultFfmpegPath(string.Empty);

            Assert.Equal("ffmpeg", path);
        }
    }
}
