using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Models;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class TranscodeJobServiceTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly FakeTranscodeProcessRunner _processRunner;

        public TranscodeJobServiceTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "jellyfin-transcode-job-service-tests", Guid.NewGuid().ToString("N"));
            _processRunner = new FakeTranscodeProcessRunner();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        [Fact]
        public void CreateJob_WithValidPreset_CreatesQueuedJob()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var request = new CreateDownloadJobRequest
            {
                ItemId = Guid.NewGuid(),
                PresetId = "video-preset",
                StartImmediately = true
            };

            var job = service.CreateJob(request, configuration);

            Assert.NotEqual(Guid.Empty, job.Id);
            Assert.Equal(request.ItemId, job.ItemId);
            Assert.Equal(Guid.Empty, job.UserId);
            Assert.Equal("video-preset", job.PresetId);
            Assert.Equal(JobStatus.Queued, job.Status);
            Assert.Equal(0, job.ProgressPercent);
            Assert.NotNull(job.OutputFileName);
            Assert.NotNull(job.OutputPath);
            Assert.NotNull(job.TempDirectory);
            Assert.EndsWith(".mp4", job.OutputFileName, StringComparison.Ordinal);
            Assert.True(Directory.Exists(job.TempDirectory));
            Assert.StartsWith(job.TempDirectory, job.OutputPath, StringComparison.Ordinal);
            Assert.True(job.CreatedAt <= DateTimeOffset.UtcNow);
            Assert.Null(job.StartedAt);
            Assert.Null(job.CompletedAt);
        }

        [Fact]
        public void CreateJob_WhenPresetDoesNotExist_ThrowsInvalidPresetException()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var request = new CreateDownloadJobRequest
            {
                ItemId = Guid.NewGuid(),
                PresetId = "missing-preset"
            };

            Assert.Throws<InvalidPresetException>(() => service.CreateJob(request, configuration));
        }

        [Fact]
        public void CreateJob_WhenPresetIsInvalid_ThrowsInvalidPresetException()
        {
            var configuration = CreateConfiguration();
            configuration.Presets[0].VideoCodec = VideoCodec.H265;
            var service = CreateService();
            var request = new CreateDownloadJobRequest
            {
                ItemId = Guid.NewGuid(),
                PresetId = "video-preset"
            };

            Assert.Throws<InvalidPresetException>(() => service.CreateJob(request, configuration));
        }

        [Fact]
        public void CreateJob_WhenQueueIsFull_ThrowsTranscodeQueueFullException()
        {
            var configuration = CreateConfiguration();
            configuration.MaxQueueSize = 1;
            var service = CreateService();

            service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);

            Assert.Throws<TranscodeQueueFullException>(
                () => service.CreateJob(
                    new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                    configuration));
            Assert.Single(Directory.GetDirectories(_tempRoot));
        }

        [Fact]
        public void GetJobs_ReturnsCreatedJobs()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var firstRequest = new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" };
            var secondRequest = new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" };

            var firstJob = service.CreateJob(firstRequest, configuration);
            var secondJob = service.CreateJob(secondRequest, configuration);

            var jobs = service.GetJobs();

            Assert.Equal(2, jobs.Count);
            Assert.Contains(jobs, job => job.Id == firstJob.Id);
            Assert.Contains(jobs, job => job.Id == secondJob.Id);
        }

        [Fact]
        public void GetJobs_ReturnsClonedJobs()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);

            var listedJob = service.GetJobs().Single();
            listedJob.Status = JobStatus.Failed;

            var freshJob = service.GetJobs().Single();
            Assert.Equal(JobStatus.Queued, freshJob.Status);
            Assert.Equal(createdJob.Id, freshJob.Id);
        }

        [Fact]
        public void GetJob_WhenJobExists_ReturnsJob()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);

            var job = service.GetJob(createdJob.Id);

            Assert.NotNull(job);
            Assert.Equal(createdJob.Id, job.Id);
        }

        [Fact]
        public void GetJob_WhenJobDoesNotExist_ReturnsNull()
        {
            var service = CreateService();

            var job = service.GetJob(Guid.NewGuid());

            Assert.Null(job);
        }

        [Fact]
        public void DeleteJob_WhenQueuedJobExists_RemovesJob()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);
            var tempDirectory = createdJob.TempDirectory;

            var deleted = service.DeleteJob(createdJob.Id);

            Assert.True(deleted);
            Assert.Null(service.GetJob(createdJob.Id));
            Assert.Empty(service.GetJobs());
            Assert.NotNull(tempDirectory);
            Assert.False(Directory.Exists(tempDirectory));
        }

        [Fact]
        public void DeleteJob_WhenJobDoesNotExist_ReturnsFalse()
        {
            var service = CreateService();

            var deleted = service.DeleteJob(Guid.NewGuid());

            Assert.False(deleted);
        }

        [Fact]
        public void DeleteJob_WhenRunningJobExists_MarksJobCancelled()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);
            service.TryUpdateJobStatus(createdJob.Id, JobStatus.Running);

            var deleted = service.DeleteJob(createdJob.Id);
            var job = service.GetJob(createdJob.Id);

            Assert.True(deleted);
            Assert.NotNull(job);
            Assert.Equal(JobStatus.Cancelled, job.Status);
            Assert.NotNull(job.CompletedAt);
        }

        [Fact]
        public void CreateJob_WhenItemIdIsEmpty_ThrowsArgumentException()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var request = new CreateDownloadJobRequest
            {
                ItemId = Guid.Empty,
                PresetId = "video-preset"
            };

            Assert.Throws<ArgumentException>(() => service.CreateJob(request, configuration));
        }

        [Fact]
        public async Task StartJobAsync_WhenJobCompletes_MarksJobCompleted()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);
            _processRunner.OutputBytes = new byte[] { 1, 2, 3 };

            var started = await service.StartJobAsync(createdJob.Id, "/media/input.mkv", configuration, CancellationToken.None);
            var job = service.GetJob(createdJob.Id);

            Assert.True(started);
            Assert.NotNull(job);
            Assert.Equal(JobStatus.Completed, job.Status);
            Assert.Equal(100, job.ProgressPercent);
            Assert.NotNull(job.StartedAt);
            Assert.NotNull(job.CompletedAt);
            Assert.Equal(3, job.OutputSizeBytes);
            Assert.Null(job.ErrorMessage);
            Assert.Equal("/media/input.mkv", _processRunner.InputPath);
            Assert.Equal(createdJob.OutputPath, _processRunner.OutputPath);
        }

        [Fact]
        public async Task StartJobAsync_WhenProcessFails_MarksJobFailed()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);
            _processRunner.Result = TranscodeProcessResult.Failure(1, "encoder failed");

            var started = await service.StartJobAsync(createdJob.Id, "/media/input.mkv", configuration, CancellationToken.None);
            var job = service.GetJob(createdJob.Id);

            Assert.True(started);
            Assert.NotNull(job);
            Assert.Equal(JobStatus.Failed, job.Status);
            Assert.Equal("encoder failed", job.ErrorMessage);
            Assert.NotNull(job.CompletedAt);
        }

        [Fact]
        public async Task StartJobAsync_WhenJobDoesNotExist_ReturnsFalse()
        {
            var service = CreateService();

            var started = await service.StartJobAsync(Guid.NewGuid(), "/media/input.mkv", CreateConfiguration(), CancellationToken.None);

            Assert.False(started);
            Assert.False(_processRunner.WasCalled);
        }

        [Fact]
        public async Task StartJobAsync_WhenJobIsNotQueued_ReturnsFalse()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);
            service.TryUpdateJobStatus(createdJob.Id, JobStatus.Completed);

            var started = await service.StartJobAsync(createdJob.Id, "/media/input.mkv", configuration, CancellationToken.None);

            Assert.False(started);
            Assert.False(_processRunner.WasCalled);
        }

        [Fact]
        public async Task GetCompletedJobFile_WhenJobCompleted_ReturnsAvailableFile()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);
            _processRunner.OutputBytes = new byte[] { 1, 2, 3 };
            await service.StartJobAsync(createdJob.Id, "/media/input.mkv", configuration, CancellationToken.None);

            var file = service.GetCompletedJobFile(createdJob.Id);

            Assert.Equal(CompletedJobFileStatus.Available, file.Status);
            Assert.Equal(createdJob.OutputPath, file.Path);
            Assert.Equal(createdJob.OutputFileName, file.DownloadFileName);
            Assert.Equal("video/mp4", file.ContentType);
        }

        [Fact]
        public void GetCompletedJobFile_WhenJobIsQueued_ReturnsNotCompleted()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);

            var file = service.GetCompletedJobFile(createdJob.Id);

            Assert.Equal(CompletedJobFileStatus.NotCompleted, file.Status);
        }

        [Fact]
        public void GetCompletedJobFile_WhenJobDoesNotExist_ReturnsNotFound()
        {
            var service = CreateService();

            var file = service.GetCompletedJobFile(Guid.NewGuid());

            Assert.Equal(CompletedJobFileStatus.NotFound, file.Status);
        }

        [Fact]
        public async Task GetCompletedJobFile_WhenCompletedFileIsMissing_ReturnsFileMissing()
        {
            var configuration = CreateConfiguration();
            var service = CreateService();
            var createdJob = service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);
            _processRunner.OutputBytes = new byte[] { 1, 2, 3 };
            await service.StartJobAsync(createdJob.Id, "/media/input.mkv", configuration, CancellationToken.None);
            File.Delete(createdJob.OutputPath!);

            var file = service.GetCompletedJobFile(createdJob.Id);

            Assert.Equal(CompletedJobFileStatus.FileMissing, file.Status);
        }

        private TranscodeJobService CreateService()
        {
            return new TranscodeJobService(new PresetValidator(), new TempFileStore(_tempRoot), _processRunner);
        }

        private static PluginConfiguration CreateConfiguration()
        {
            var profile = new CapabilityProfile
            {
                Id = "cpu-profile",
                Name = "CPU H.264",
                Backend = TranscodeBackend.Software,
                AllowedVideoCodecs = new List<VideoCodec> { VideoCodec.Copy, VideoCodec.H264 },
                AllowedAudioCodecs = new List<AudioCodec> { AudioCodec.Copy, AudioCodec.Aac },
                AllowedContainers = new List<ContainerFormat> { ContainerFormat.Mp4 },
                SupportsSubtitleBurnIn = true,
                SupportsToneMapping = true
            };

            var preset = new AdminTranscodePreset
            {
                Id = "video-preset",
                Name = "1080p H.264 AAC MP4 - CPU Compatible",
                Enabled = true,
                CapabilityProfileId = profile.Id,
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

            return new PluginConfiguration
            {
                MaxQueueSize = 10,
                CapabilityProfiles = new List<CapabilityProfile> { profile },
                Presets = new List<AdminTranscodePreset> { preset }
            };
        }

        private sealed class FakeTranscodeProcessRunner : ITranscodeProcessRunner
        {
            public TranscodeProcessResult Result { get; set; } = TranscodeProcessResult.Success();

            public byte[] OutputBytes { get; set; } = Array.Empty<byte>();

            public bool WasCalled { get; private set; }

            public string? InputPath { get; private set; }

            public string? OutputPath { get; private set; }

            public Task<TranscodeProcessResult> RunAsync(
                AdminTranscodePreset preset,
                CapabilityProfile capabilityProfile,
                string inputPath,
                string outputPath,
                CancellationToken cancellationToken)
            {
                WasCalled = true;
                InputPath = inputPath;
                OutputPath = outputPath;
                if (Result.Succeeded)
                {
                    File.WriteAllBytes(outputPath, OutputBytes);
                }

                return Task.FromResult(Result);
            }
        }
    }
}
