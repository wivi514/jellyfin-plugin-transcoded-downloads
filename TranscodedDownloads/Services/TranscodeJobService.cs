using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// In-memory transcode job service.
    /// </summary>
    public sealed class TranscodeJobService : ITranscodeJobService
    {
        private readonly object _syncRoot = new object();
        private readonly List<DownloadJobDto> _jobs = new List<DownloadJobDto>();
        private readonly IPresetValidator _presetValidator;

        /// <summary>
        /// Initializes the shared job service instance used by the controller until DI is wired.
        /// </summary>
        public static TranscodeJobService Shared { get; } = new TranscodeJobService();

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeJobService"/> class.
        /// </summary>
        public TranscodeJobService()
            : this(new PresetValidator())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeJobService"/> class.
        /// </summary>
        /// <param name="presetValidator">The preset validator.</param>
        public TranscodeJobService(IPresetValidator presetValidator)
        {
            _presetValidator = presetValidator ?? throw new ArgumentNullException(nameof(presetValidator));
        }

        /// <inheritdoc />
        public DownloadJobDto CreateJob(CreateDownloadJobRequest request, PluginConfiguration configuration)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (request.ItemId == Guid.Empty)
            {
                throw new ArgumentException("ItemId is required.", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.PresetId))
            {
                throw new InvalidPresetException("PresetId is required.");
            }

            var preset = configuration.Presets.FirstOrDefault(candidate => candidate.Id == request.PresetId);
            if (preset == null)
            {
                throw new InvalidPresetException("The requested preset does not exist.");
            }

            var validationResult = _presetValidator.Validate(preset, configuration.CapabilityProfiles);
            if (!validationResult.IsValid)
            {
                throw new InvalidPresetException($"The requested preset is not valid: {string.Join(" ", validationResult.Errors)}");
            }

            lock (_syncRoot)
            {
                var queuedOrRunningCount = _jobs.Count(job => job.Status == JobStatus.Queued || job.Status == JobStatus.Running);
                if (queuedOrRunningCount >= configuration.MaxQueueSize)
                {
                    throw new TranscodeQueueFullException("The transcode download queue is full.");
                }

                var job = new DownloadJobDto
                {
                    Id = Guid.NewGuid(),
                    ItemId = request.ItemId,
                    UserId = Guid.Empty,
                    PresetId = preset.Id,
                    Status = JobStatus.Queued,
                    ProgressPercent = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _jobs.Add(job);
                return Clone(job);
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<DownloadJobDto> GetJobs()
        {
            lock (_syncRoot)
            {
                return _jobs.Select(Clone).ToList();
            }
        }

        /// <inheritdoc />
        public DownloadJobDto? GetJob(Guid jobId)
        {
            lock (_syncRoot)
            {
                var job = _jobs.FirstOrDefault(candidate => candidate.Id == jobId);
                return job == null ? null : Clone(job);
            }
        }

        /// <inheritdoc />
        public bool DeleteJob(Guid jobId)
        {
            lock (_syncRoot)
            {
                var job = _jobs.FirstOrDefault(candidate => candidate.Id == jobId);
                if (job == null)
                {
                    return false;
                }

                if (job.Status == JobStatus.Queued)
                {
                    _jobs.Remove(job);
                }
                else if (job.Status == JobStatus.Running)
                {
                    job.Status = JobStatus.Cancelled;
                    job.CompletedAt = DateTimeOffset.UtcNow;
                }

                return true;
            }
        }

        internal bool TryUpdateJobStatus(Guid jobId, JobStatus status)
        {
            lock (_syncRoot)
            {
                var job = _jobs.FirstOrDefault(candidate => candidate.Id == jobId);
                if (job == null)
                {
                    return false;
                }

                job.Status = status;
                if (status == JobStatus.Running)
                {
                    job.StartedAt = DateTimeOffset.UtcNow;
                }
                else if (status == JobStatus.Completed || status == JobStatus.Failed || status == JobStatus.Cancelled)
                {
                    job.CompletedAt = DateTimeOffset.UtcNow;
                }

                return true;
            }
        }

        private static DownloadJobDto Clone(DownloadJobDto job)
        {
            return new DownloadJobDto
            {
                Id = job.Id,
                ItemId = job.ItemId,
                UserId = job.UserId,
                PresetId = job.PresetId,
                Status = job.Status,
                ProgressPercent = job.ProgressPercent,
                OutputFileName = job.OutputFileName,
                OutputSizeBytes = job.OutputSizeBytes,
                ErrorMessage = job.ErrorMessage,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt
            };
        }
    }
}
