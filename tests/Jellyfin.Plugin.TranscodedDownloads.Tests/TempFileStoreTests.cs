using System;
using System.IO;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class TempFileStoreTests : IDisposable
    {
        private readonly string _tempRoot;

        public TempFileStoreTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "jellyfin-transcoded-downloads-tests", Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        [Fact]
        public void BuildOutputFileName_WithSafeNames_ReturnsHumanReadableFileName()
        {
            var preset = CreatePreset(ContainerFormat.Mp4);

            var fileName = TempFileStore.BuildOutputFileName("Blade Runner 2049", preset);

            Assert.Equal("Blade Runner 2049 - 1080p H.264 AAC MP4.mp4", fileName);
        }

        [Fact]
        public void SanitizeFileName_RemovesPathTraversalAndInvalidCharacters()
        {
            var fileName = TempFileStore.SanitizeFileName("../Bad:Name/With\\Separators?. ");

            Assert.Equal("Bad Name With Separators", fileName);
            Assert.DoesNotContain("/", fileName);
            Assert.DoesNotContain("\\", fileName);
            Assert.DoesNotContain(":", fileName);
            Assert.DoesNotContain("?", fileName);
            Assert.False(fileName.EndsWith(".", StringComparison.Ordinal));
            Assert.False(fileName.EndsWith(" ", StringComparison.Ordinal));
        }

        [Fact]
        public void SanitizeFileName_WhenReservedWindowsName_AppendsSuffix()
        {
            var fileName = TempFileStore.SanitizeFileName("CON");

            Assert.Equal("CON_", fileName);
        }

        [Theory]
        [InlineData(ContainerFormat.Mp4, ".mp4")]
        [InlineData(ContainerFormat.Mkv, ".mkv")]
        [InlineData(ContainerFormat.Webm, ".webm")]
        [InlineData(ContainerFormat.Mp3, ".mp3")]
        [InlineData(ContainerFormat.M4a, ".m4a")]
        [InlineData(ContainerFormat.Ogg, ".ogg")]
        [InlineData(ContainerFormat.Flac, ".flac")]
        public void GetExtension_ReturnsExpectedExtension(ContainerFormat container, string expectedExtension)
        {
            Assert.Equal(expectedExtension, TempFileStore.GetExtension(container));
        }

        [Fact]
        public void ReserveOutputFile_CreatesJobDirectoryAndKeepsOutputUnderTempRoot()
        {
            var store = new TempFileStore(_tempRoot);
            var configuration = new PluginConfiguration();
            var jobId = Guid.NewGuid();

            var reservation = store.ReserveOutputFile(configuration, jobId, "../Movie", CreatePreset(ContainerFormat.Mkv));

            Assert.Equal(jobId, reservation.JobId);
            Assert.True(Directory.Exists(reservation.JobDirectory));
            Assert.StartsWith(Path.GetFullPath(_tempRoot), reservation.JobDirectory, StringComparison.Ordinal);
            Assert.StartsWith(reservation.JobDirectory, reservation.OutputPath, StringComparison.Ordinal);
            Assert.EndsWith(".mkv", reservation.OutputPath, StringComparison.Ordinal);
            Assert.DoesNotContain("..", Path.GetFileName(reservation.OutputPath));
        }

        [Fact]
        public void ReserveOutputFile_UsesConfiguredTempDirectory()
        {
            var store = new TempFileStore(Path.Combine(_tempRoot, "default"));
            var configuredRoot = Path.Combine(_tempRoot, "configured");
            var configuration = new PluginConfiguration { TempDirectory = configuredRoot };

            var reservation = store.ReserveOutputFile(configuration, Guid.NewGuid(), "Movie", CreatePreset(ContainerFormat.Mp4));

            Assert.StartsWith(Path.GetFullPath(configuredRoot), reservation.OutputPath, StringComparison.Ordinal);
        }

        [Fact]
        public void ReserveOutputFile_WhenTempDirectoryIsMediaRoot_ThrowsInvalidOperationException()
        {
            var store = new TempFileStore(_tempRoot);
            var configuration = new PluginConfiguration { TempDirectory = "/media/transcoded-downloads" };

            Assert.Throws<InvalidOperationException>(
                () => store.ReserveOutputFile(configuration, Guid.NewGuid(), "Movie", CreatePreset(ContainerFormat.Mp4)));
        }

        [Fact]
        public void DeleteJobDirectory_WhenDirectoryExists_DeletesDirectory()
        {
            var store = new TempFileStore(_tempRoot);
            var configuration = new PluginConfiguration();
            var jobId = Guid.NewGuid();
            var reservation = store.ReserveOutputFile(configuration, jobId, "Movie", CreatePreset(ContainerFormat.Mp4));
            File.WriteAllText(reservation.OutputPath, "placeholder");

            var deleted = store.DeleteJobDirectory(configuration, jobId);

            Assert.True(deleted);
            Assert.False(Directory.Exists(reservation.JobDirectory));
        }

        [Fact]
        public void DeleteJobDirectory_WhenDirectoryDoesNotExist_ReturnsFalse()
        {
            var store = new TempFileStore(_tempRoot);

            var deleted = store.DeleteJobDirectory(new PluginConfiguration(), Guid.NewGuid());

            Assert.False(deleted);
        }

        private static AdminTranscodePreset CreatePreset(ContainerFormat container)
        {
            return new AdminTranscodePreset
            {
                Id = "preset",
                Name = "1080p H.264 AAC MP4",
                Container = container
            };
        }
    }
}
