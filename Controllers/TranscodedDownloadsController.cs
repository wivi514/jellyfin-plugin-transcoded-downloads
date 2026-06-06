using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Models;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TranscodedDownloads.Controllers
{
    /// <summary>
    /// API controller for transcoded download metadata.
    /// </summary>
    [ApiController]
    [Authorize(Policy = Policies.Download)]
    [Route("TranscodedDownloads")]
    public sealed class TranscodedDownloadsController : ControllerBase
    {
        private static readonly object RuntimeJobServiceSyncRoot = new object();
        private static ITranscodeJobService? _runtimeJobService;

        private readonly IPresetListingService _presetListingService;
        private readonly ITranscodeJobService _transcodeJobService;
        private readonly ITranscodeJobStarter _transcodeJobStarter;
        private readonly Func<PluginConfiguration?> _configurationProvider;
        private readonly Func<AuthorizationInfo> _authorizationInfoProvider;
        private readonly Func<AuthorizationInfo, bool> _administratorAuthorizationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodedDownloadsController"/> class.
        /// </summary>
        public TranscodedDownloadsController()
            : this(new PresetListingService(), TranscodeJobService.Shared)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodedDownloadsController"/> class.
        /// </summary>
        /// <param name="libraryManager">The Jellyfin library manager.</param>
        public TranscodedDownloadsController(ILibraryManager libraryManager)
            : this(new PresetListingService(), CreateRuntimeJobService(libraryManager))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodedDownloadsController"/> class.
        /// </summary>
        /// <param name="libraryManager">The Jellyfin library manager.</param>
        /// <param name="authorizationContext">The Jellyfin authorization context.</param>
        public TranscodedDownloadsController(ILibraryManager libraryManager, IAuthorizationContext authorizationContext)
            : this(
                new PresetListingService(),
                CreateRuntimeJobService(libraryManager),
                new BackgroundTranscodeJobStarter(CreateRuntimeJobService(libraryManager)),
                () => Plugin.Instance?.Configuration,
                () => new AuthorizationInfo())
        {
            if (authorizationContext == null)
            {
                throw new ArgumentNullException(nameof(authorizationContext));
            }

            _authorizationInfoProvider = () => authorizationContext.GetAuthorizationInfo(Request).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodedDownloadsController"/> class.
        /// </summary>
        /// <param name="presetListingService">The preset listing service.</param>
        /// <param name="transcodeJobService">The transcode job service.</param>
        public TranscodedDownloadsController(
            IPresetListingService presetListingService,
            ITranscodeJobService transcodeJobService)
            : this(
                presetListingService,
                transcodeJobService,
                new BackgroundTranscodeJobStarter(transcodeJobService),
                () => Plugin.Instance?.Configuration,
                () => new AuthorizationInfo())
        {
        }

        internal TranscodedDownloadsController(
            IPresetListingService presetListingService,
            ITranscodeJobService transcodeJobService,
            ITranscodeJobStarter transcodeJobStarter,
            Func<PluginConfiguration?> configurationProvider,
            Func<AuthorizationInfo> authorizationInfoProvider,
            Func<AuthorizationInfo, bool>? administratorAuthorizationProvider = null)
        {
            _presetListingService = presetListingService ?? throw new ArgumentNullException(nameof(presetListingService));
            _transcodeJobService = transcodeJobService ?? throw new ArgumentNullException(nameof(transcodeJobService));
            _transcodeJobStarter = transcodeJobStarter ?? throw new ArgumentNullException(nameof(transcodeJobStarter));
            _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
            _authorizationInfoProvider = authorizationInfoProvider ?? throw new ArgumentNullException(nameof(authorizationInfoProvider));
            _administratorAuthorizationProvider = administratorAuthorizationProvider ?? IsAdministrator;
        }

        /// <summary>
        /// Gets the enabled presets that are valid for this server.
        /// </summary>
        /// <returns>The available transcode presets.</returns>
        [HttpGet("Presets")]
        public ActionResult<IReadOnlyList<TranscodePresetDto>> GetPresets()
        {
            var configuration = _configurationProvider();
            if (configuration == null)
            {
                return StatusCode(503);
            }

            return Ok(_presetListingService.GetAvailablePresets(configuration));
        }

        /// <summary>
        /// Creates a transcoded download job.
        /// </summary>
        /// <param name="request">The create job request.</param>
        /// <returns>The created job.</returns>
        [HttpPost("Jobs")]
        public ActionResult<DownloadJobDto> CreateJob(CreateDownloadJobRequest request)
        {
            var configuration = _configurationProvider();
            if (configuration == null)
            {
                return StatusCode(503);
            }

            try
            {
                var authorizationInfo = GetAuthorizationInfo();
                var job = _transcodeJobService.CreateJob(request, configuration, authorizationInfo.UserId);
                if (request.StartImmediately)
                {
                    _transcodeJobStarter.StartJob(job.Id, configuration);
                }

                return Ok(job);
            }
            catch (InvalidPresetException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (MediaItemResolutionException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (TranscodeQueueFullException ex)
            {
                return StatusCode(429, ex.Message);
            }
        }

        /// <summary>
        /// Gets current transcoded download jobs.
        /// </summary>
        /// <returns>The current jobs.</returns>
        [HttpGet("Jobs")]
        public ActionResult<IReadOnlyList<DownloadJobDto>> GetJobs()
        {
            var authorizationInfo = GetAuthorizationInfo();
            var jobs = _transcodeJobService.GetJobs();
            if (!_administratorAuthorizationProvider(authorizationInfo))
            {
                jobs = jobs.Where(job => job.UserId == authorizationInfo.UserId).ToList();
            }

            return Ok(jobs);
        }

        /// <summary>
        /// Gets a transcoded download job by ID.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>The job.</returns>
        [HttpGet("Jobs/{jobId}")]
        public ActionResult<DownloadJobDto> GetJob(Guid jobId)
        {
            var job = _transcodeJobService.GetJob(jobId);
            if (job == null)
            {
                return NotFound();
            }

            if (!CanAccessJob(job))
            {
                return Forbid();
            }

            return Ok(job);
        }

        /// <summary>
        /// Deletes or cancels a transcoded download job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>No content when the job was found.</returns>
        [HttpDelete("Jobs/{jobId}")]
        public IActionResult DeleteJob(Guid jobId)
        {
            var job = _transcodeJobService.GetJob(jobId);
            if (job == null)
            {
                return NotFound();
            }

            if (!CanAccessJob(job))
            {
                return Forbid();
            }

            if (!_transcodeJobService.DeleteJob(jobId))
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Downloads the completed output file for a transcoded download job.
        /// </summary>
        /// <param name="jobId">The job ID.</param>
        /// <returns>The completed output file.</returns>
        [HttpGet("Jobs/{jobId}/File")]
        public IActionResult GetJobFile(Guid jobId)
        {
            var job = _transcodeJobService.GetJob(jobId);
            if (job == null)
            {
                return NotFound();
            }

            if (!CanAccessJob(job))
            {
                return Forbid();
            }

            var file = _transcodeJobService.GetCompletedJobFile(jobId);
            return file.Status switch
            {
                CompletedJobFileStatus.NotFound => NotFound(),
                CompletedJobFileStatus.NotCompleted => Conflict("The transcode job has not completed."),
                CompletedJobFileStatus.FileMissing => NotFound("The completed transcode file no longer exists."),
                CompletedJobFileStatus.Available => PhysicalFile(file.Path!, file.ContentType!, file.DownloadFileName),
                _ => StatusCode(500)
            };
        }

        private AuthorizationInfo GetAuthorizationInfo()
        {
            var authorizationInfo = _authorizationInfoProvider();
            if (authorizationInfo.UserId == Guid.Empty)
            {
                throw new InvalidOperationException("An authenticated Jellyfin user is required.");
            }

            return authorizationInfo;
        }

        private bool CanAccessJob(DownloadJobDto job)
        {
            var authorizationInfo = GetAuthorizationInfo();
            return job.UserId == authorizationInfo.UserId || _administratorAuthorizationProvider(authorizationInfo);
        }

        private static bool IsAdministrator(AuthorizationInfo authorizationInfo)
        {
            return authorizationInfo.User?.Permissions.Any(permission =>
                permission.Kind == PermissionKind.IsAdministrator && permission.Value) == true;
        }

        private static ITranscodeJobService CreateRuntimeJobService(ILibraryManager libraryManager)
        {
            lock (RuntimeJobServiceSyncRoot)
            {
                _runtimeJobService ??= new TranscodeJobService(
                    new PresetValidator(),
                    new TempFileStore(),
                    new TranscodeProcessRunner(),
                    new MediaItemResolver(libraryManager));

                return _runtimeJobService;
            }
        }
    }
}
