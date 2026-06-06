using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Controllers;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Models;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class TranscodedDownloadsControllerTests
    {
        private static readonly Guid OwnerUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid OtherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        [Fact]
        public void Controller_RequiresDownloadPolicy()
        {
            var attribute = typeof(TranscodedDownloadsController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .Single();

            Assert.Equal(Policies.Download, attribute.Policy);
        }

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
            Assert.Equal(OwnerUserId, service.CreatedJob.UserId);
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

        [Fact]
        public void GetJobs_WhenUserIsNotAdmin_ReturnsOnlyOwnedJobs()
        {
            var service = new FakeTranscodeJobService();
            service.Jobs.Add(new DownloadJobDto { Id = Guid.NewGuid(), UserId = OwnerUserId });
            service.Jobs.Add(new DownloadJobDto { Id = Guid.NewGuid(), UserId = OtherUserId });
            var controller = CreateController(service, new FakeTranscodeJobStarter(), CreateConfiguration());

            var result = controller.GetJobs();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var jobs = Assert.IsAssignableFrom<IReadOnlyList<DownloadJobDto>>(okResult.Value);
            Assert.Single(jobs);
            Assert.Equal(OwnerUserId, jobs[0].UserId);
        }

        [Fact]
        public void GetJobs_WhenUserIsAdmin_ReturnsAllJobs()
        {
            var service = new FakeTranscodeJobService();
            service.Jobs.Add(new DownloadJobDto { Id = Guid.NewGuid(), UserId = OwnerUserId });
            service.Jobs.Add(new DownloadJobDto { Id = Guid.NewGuid(), UserId = OtherUserId });
            var controller = CreateController(
                service,
                new FakeTranscodeJobStarter(),
                CreateConfiguration(),
                isAdministrator: true);

            var result = controller.GetJobs();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var jobs = Assert.IsAssignableFrom<IReadOnlyList<DownloadJobDto>>(okResult.Value);
            Assert.Equal(2, jobs.Count);
        }

        [Fact]
        public void GetJob_WhenJobBelongsToAnotherUser_ReturnsForbidden()
        {
            var service = new FakeTranscodeJobService();
            service.Jobs.Add(new DownloadJobDto { Id = service.CreatedJob.Id, UserId = OtherUserId });
            var controller = CreateController(service, new FakeTranscodeJobStarter(), CreateConfiguration());

            var result = controller.GetJob(service.CreatedJob.Id);

            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public void GetJobFile_WhenJobBelongsToAnotherUser_ReturnsForbidden()
        {
            var service = new FakeTranscodeJobService();
            service.Jobs.Add(new DownloadJobDto { Id = service.CreatedJob.Id, UserId = OtherUserId });
            var controller = CreateController(service, new FakeTranscodeJobStarter(), CreateConfiguration());

            var result = controller.GetJobFile(service.CreatedJob.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public void DeleteJob_WhenJobBelongsToAnotherUser_ReturnsForbidden()
        {
            var service = new FakeTranscodeJobService();
            service.Jobs.Add(new DownloadJobDto { Id = service.CreatedJob.Id, UserId = OtherUserId });
            var controller = CreateController(service, new FakeTranscodeJobStarter(), CreateConfiguration());

            var result = controller.DeleteJob(service.CreatedJob.Id);

            Assert.IsType<ForbidResult>(result);
            Assert.False(service.WasDeleted);
        }

        private static TranscodedDownloadsController CreateController(
            ITranscodeJobService service,
            ITranscodeJobStarter starter,
            PluginConfiguration? configuration,
            bool isAdministrator = false)
        {
            return new TranscodedDownloadsController(
                new PresetListingService(),
                service,
                starter,
                () => configuration,
                () => CreateAuthorizationInfo(OwnerUserId),
                _ => isAdministrator);
        }

        private static AuthorizationInfo CreateAuthorizationInfo(Guid userId)
        {
            return new AuthorizationInfo
            {
                User = new User("test", "auth", "reset") { Id = userId },
                IsAuthenticated = true,
                Token = "test-token"
            };
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
                UserId = OwnerUserId,
                Status = JobStatus.Queued
            };

            public List<DownloadJobDto> Jobs { get; } = new List<DownloadJobDto>();

            public bool WasDeleted { get; private set; }

            public DownloadJobDto CreateJob(CreateDownloadJobRequest request, PluginConfiguration configuration, Guid userId)
            {
                CreatedJob.UserId = userId;
                return CreatedJob;
            }

            public IReadOnlyList<DownloadJobDto> GetJobs()
            {
                return Jobs;
            }

            public DownloadJobDto? GetJob(Guid jobId)
            {
                return Jobs.FirstOrDefault(job => job.Id == jobId);
            }

            public bool DeleteJob(Guid jobId)
            {
                WasDeleted = true;
                return Jobs.RemoveAll(job => job.Id == jobId) > 0;
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
