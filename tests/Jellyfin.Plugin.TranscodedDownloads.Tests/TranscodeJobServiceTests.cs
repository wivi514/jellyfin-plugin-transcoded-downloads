using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Models;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class TranscodeJobServiceTests
    {
        [Fact]
        public void CreateJob_WithValidPreset_CreatesQueuedJob()
        {
            var configuration = CreateConfiguration();
            var service = new TranscodeJobService();
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
            Assert.True(job.CreatedAt <= DateTimeOffset.UtcNow);
            Assert.Null(job.StartedAt);
            Assert.Null(job.CompletedAt);
        }

        [Fact]
        public void CreateJob_WhenPresetDoesNotExist_ThrowsInvalidPresetException()
        {
            var configuration = CreateConfiguration();
            var service = new TranscodeJobService();
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
            var service = new TranscodeJobService();
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
            var service = new TranscodeJobService();

            service.CreateJob(
                new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                configuration);

            Assert.Throws<TranscodeQueueFullException>(
                () => service.CreateJob(
                    new CreateDownloadJobRequest { ItemId = Guid.NewGuid(), PresetId = "video-preset" },
                    configuration));
        }

        [Fact]
        public void GetJobs_ReturnsCreatedJobs()
        {
            var configuration = CreateConfiguration();
            var service = new TranscodeJobService();
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
            var service = new TranscodeJobService();
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
        public void CreateJob_WhenItemIdIsEmpty_ThrowsArgumentException()
        {
            var configuration = CreateConfiguration();
            var service = new TranscodeJobService();
            var request = new CreateDownloadJobRequest
            {
                ItemId = Guid.Empty,
                PresetId = "video-preset"
            };

            Assert.Throws<ArgumentException>(() => service.CreateJob(request, configuration));
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
    }
}
