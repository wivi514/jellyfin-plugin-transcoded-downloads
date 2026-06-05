using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Controllers;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Models;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class TranscodedDownloadsControllerTests
    {
        [Fact]
        public void CreateJob_WhenStartImmediatelyIsTrue_StartsCreatedJob()
        {
            var configuration = CreateConfiguration();
            var service = new FakeTranscodeJobService();
            var starter = new FakeTranscodeJobStarter();
            var controller = CreateController(service, starter, configuration);
            var request = new CreateDownloadJobRequest
            {
                ItemId = Guid.NewGuid(),
                PresetId = "video-preset",
                StartImmediately = true
            };

            var result = controller.CreateJob(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var job = Assert.IsType<DownloadJobDto>(okResult.Value);
            Assert.Equal(service.CreatedJob.Id, job.Id);
            Assert.Equal(job.Id, starter.StartedJobId);
            Assert.Same(configuration, starter.Configuration);
        }

        [Fact]
        public void CreateJob_WhenStartImmediatelyIsFalse_DoesNotStartCreatedJob()
        {
            var configuration = CreateConfiguration();
            var service = new FakeTranscodeJobService();
            var starter = new FakeTranscodeJobStarter();
            var controller = CreateController(service, starter, configuration);
            var request = new CreateDownloadJobRequest
            {
                ItemId = Guid.NewGuid(),
                PresetId = "video-preset",
                StartImmediately = false
            };

            var result = controller.CreateJob(request);

            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(starter.StartedJobId);
        }

        [Fact]
        public void CreateJob_WhenConfigurationIsUnavailable_ReturnsServiceUnavailable()
        {
            var controller = CreateController(
                new FakeTranscodeJobService(),
                new FakeTranscodeJobStarter(),
                configuration: null);

            var result = controller.CreateJob(new CreateDownloadJobRequest());

            var statusResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(503, statusResult.StatusCode);
        }

        private static TranscodedDownloadsController CreateController(
            ITranscodeJobService service,
            ITranscodeJobStarter starter,
            PluginConfiguration? configuration)
        {
            return new TranscodedDownloadsController(
                new PresetListingService(),
                service,
                starter,
                () => configuration);
        }

        private static PluginConfiguration CreateConfiguration()
        {
            return new PluginConfiguration
            {
                Presets = new List<AdminTranscodePreset>
                {
                    new AdminTranscodePreset
                    {
                        Id = "video-preset",
                        Name = "Video Preset",
                        Enabled = true
                    }
                }
            };
        }

        private sealed class FakeTranscodeJobService : ITranscodeJobService
        {
            public DownloadJobDto CreatedJob { get; } = new DownloadJobDto
            {
                Id = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                PresetId = "video-preset",
                Status = JobStatus.Queued
            };

            public DownloadJobDto CreateJob(CreateDownloadJobRequest request, PluginConfiguration configuration)
            {
                return CreatedJob;
            }

            public IReadOnlyList<DownloadJobDto> GetJobs()
            {
                return Array.Empty<DownloadJobDto>();
            }

            public DownloadJobDto? GetJob(Guid jobId)
            {
                return null;
            }

            public bool DeleteJob(Guid jobId)
            {
                return false;
            }

            public Task<bool> StartJobAsync(Guid jobId, PluginConfiguration configuration, CancellationToken cancellationToken)
            {
                return Task.FromResult(false);
            }

            public CompletedJobFile GetCompletedJobFile(Guid jobId)
            {
                return new CompletedJobFile { Status = CompletedJobFileStatus.NotFound };
            }
        }

        private sealed class FakeTranscodeJobStarter : ITranscodeJobStarter
        {
            public Guid? StartedJobId { get; private set; }

            public PluginConfiguration? Configuration { get; private set; }

            public void StartJob(Guid jobId, PluginConfiguration configuration)
            {
                StartedJobId = jobId;
                Configuration = configuration;
            }
        }
    }
}
