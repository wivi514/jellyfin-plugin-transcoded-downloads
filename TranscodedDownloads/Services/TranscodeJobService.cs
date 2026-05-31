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
        private readonly ITempFileStore _tempFileStore;
        private readonly ITranscodeProcessRunner _processRunner;
        private readonly IMediaItemResolver _mediaItemResolver;

        /// <summary>
        /// Initializes the shared job service instance used by the controller until DI is wired.
        /// </summary>
        public static TranscodeJobService Shared { get; } = new TranscodeJobService();

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeJobService"/> class.
        /// </summary>
        public TranscodeJobService()
            : this(new PresetValidator(), new TempFileStore(), new TranscodeProcessRunner(), new UnavailableMediaItemResolver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeJobService"/> class.
        /// </summary>
        /// <param name="presetValidator">The preset validator.</param>
        public TranscodeJobService(IPresetValidator presetValidator)
            : this(presetValidator, new TempFileStore(), new TranscodeProcessRunner(), new UnavailableMediaItemResolver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeJobService"/> class.
        /// </summary>
        /// <param name="presetValidator">The preset validator.</param>
        /// <param name="tempFileStore">The temp file store.</param>
        public TranscodeJobService(IPresetValidator presetValidator, ITempFileStore tempFileStore)
            : this(presetValidator, tempFileStore, new TranscodeProcessRunner(), new UnavailableMediaItemResolver())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeJobService"/> class.
        /// </summary>
        /// <param name="presetValidator">The preset validator.</param>
        /// <param name="tempFileStore">The temp file store.</param>
        /// <param name="processRunner">The transcode process runner.</param>
        /// <param name="mediaItemResolver">The media item resolver.</param>
        public TranscodeJobService(
            IPresetValidator presetValidator,
            ITempFileStore tempFileStore,
            ITranscodeProcessRunner processRunner,
            IMediaItemResolver mediaItemResolver)
        {
            _presetValidator = presetValidator ?? throw new ArgumentNullException(nameof(presetValidator));
            _tempFileStore = tempFileStore ?? throw new ArgumentNullException(nameof(tempFileStore));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _mediaItemResolver = mediaItemResolver ?? throw new ArgumentNullException(nameof(mediaItemResolver));
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

            var mediaItem = _mediaItemResolver.ResolveItem(request.ItemId);
            ValidatePresetMatchesMediaType(preset, mediaItem);

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

                var jobId = Guid.NewGuid();
                var reservation = _tempFileStore.ReserveOutputFile(
                    configuration,
                    jobId,
                    mediaItem.Name,
                    preset);

                var job = new DownloadJobDto
                {
                    Id = jobId,
                    ItemId = request.ItemId,
                    UserId = Guid.Empty,
                    PresetId = preset.Id,
                    Status = JobStatus.Queued,
                    ProgressPercent = 0,
                    OutputFileName = reservation.OutputFileName,
                    OutputPath = reservation.OutputPath,
                    TempDirectory = reservation.JobDirectory,
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
                    _tempFileStore.DeleteJobDirectory(new PluginConfiguration { TempDirectory = GetTempRoot(job) }, job.Id);
                }
                else if (job.Status == JobStatus.Running)
                {
                    job.Status = JobStatus.Cancelled;
                    job.CompletedAt = DateTimeOffset.UtcNow;
                }

                return true;
            }
        }

        /// <inheritdoc />
        public async Task<bool> StartJobAsync(
            Guid jobId,
            PluginConfiguration configuration,
            CancellationToken cancellationToken)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            DownloadJobDto jobSnapshot;
            AdminTranscodePreset preset;
            CapabilityProfile capabilityProfile;
            MediaItemInfo mediaItem;
            lock (_syncRoot)
            {
                var job = _jobs.FirstOrDefault(candidate => candidate.Id == jobId);
                if (job == null || job.Status != JobStatus.Queued)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(job.OutputPath))
                {
                    MarkJobFailed(job, "The job does not have a reserved output path.");
                    return true;
                }

                preset = configuration.Presets.FirstOrDefault(candidate => candidate.Id == job.PresetId)
                    ?? throw new InvalidPresetException("The requested preset does not exist.");
                capabilityProfile = configuration.CapabilityProfiles.FirstOrDefault(candidate => candidate.Id == preset.CapabilityProfileId)
                    ?? throw new InvalidPresetException("The requested preset does not reference a configured capability profile.");
                mediaItem = _mediaItemResolver.ResolveItem(job.ItemId);
                ValidatePresetMatchesMediaType(preset, mediaItem);

                job.Status = JobStatus.Running;
                job.StartedAt = DateTimeOffset.UtcNow;
                job.ProgressPercent = 0;
                jobSnapshot = Clone(job);
            }

            var result = await _processRunner.RunAsync(
                preset,
                capabilityProfile,
                mediaItem.Path,
                jobSnapshot.OutputPath!,
                cancellationToken).ConfigureAwait(false);

            lock (_syncRoot)
            {
                var job = _jobs.FirstOrDefault(candidate => candidate.Id == jobId);
                if (job == null || job.Status == JobStatus.Cancelled)
                {
                    return true;
                }

                if (result.Succeeded)
                {
                    job.Status = JobStatus.Completed;
                    job.ProgressPercent = 100;
                    job.CompletedAt = DateTimeOffset.UtcNow;
                    job.OutputSizeBytes = File.Exists(job.OutputPath) ? new FileInfo(job.OutputPath).Length : null;
                    job.ErrorMessage = null;
                }
                else
                {
                    MarkJobFailed(job, result.ErrorMessage ?? "The transcode failed.");
                }

                return true;
            }
        }

        /// <inheritdoc />
        public CompletedJobFile GetCompletedJobFile(Guid jobId)
        {
            lock (_syncRoot)
            {
                var job = _jobs.FirstOrDefault(candidate => candidate.Id == jobId);
                if (job == null)
                {
                    return new CompletedJobFile { Status = CompletedJobFileStatus.NotFound };
                }

                if (job.Status != JobStatus.Completed)
                {
                    return new CompletedJobFile { Status = CompletedJobFileStatus.NotCompleted };
                }

                if (string.IsNullOrWhiteSpace(job.OutputPath) || !File.Exists(job.OutputPath))
                {
                    return new CompletedJobFile { Status = CompletedJobFileStatus.FileMissing };
                }

                return new CompletedJobFile
                {
                    Status = CompletedJobFileStatus.Available,
                    Path = job.OutputPath,
                    DownloadFileName = job.OutputFileName ?? Path.GetFileName(job.OutputPath),
                    ContentType = GetContentType(job.OutputFileName ?? job.OutputPath)
                };
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

        private static void MarkJobFailed(DownloadJobDto job, string errorMessage)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = errorMessage;
            job.CompletedAt = DateTimeOffset.UtcNow;
        }

        private static void ValidatePresetMatchesMediaType(AdminTranscodePreset preset, MediaItemInfo mediaItem)
        {
            if (mediaItem == null)
            {
                throw new ArgumentNullException(nameof(mediaItem));
            }

            if (preset.IsAudioOnlyPreset && !mediaItem.IsAudio)
            {
                throw new InvalidPresetException("The requested audio-only preset cannot be used with a non-audio item.");
            }

            if (preset.IsVideoPreset && !mediaItem.IsVideo)
            {
                throw new InvalidPresetException("The requested video preset cannot be used with a non-video item.");
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
                OutputPath = job.OutputPath,
                TempDirectory = job.TempDirectory,
                OutputSizeBytes = job.OutputSizeBytes,
                ErrorMessage = job.ErrorMessage,
                CreatedAt = job.CreatedAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt
            };
        }

        private static string GetTempRoot(DownloadJobDto job)
        {
            if (string.IsNullOrWhiteSpace(job.TempDirectory))
            {
                return string.Empty;
            }

            return System.IO.Directory.GetParent(job.TempDirectory)?.FullName ?? string.Empty;
        }

        private static string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".mp4" => "video/mp4",
                ".mkv" => "video/x-matroska",
                ".webm" => "video/webm",
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/mp4",
                ".ogg" => "audio/ogg",
                ".flac" => "audio/flac",
                _ => "application/octet-stream"
            };
        }
    }
}
