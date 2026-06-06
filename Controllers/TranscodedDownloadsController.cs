using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Models;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Library;
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
        /// <param name="presetListingService">The preset listing service.</param>
        /// <param name="transcodeJobService">The transcode job service.</param>
        public TranscodedDownloadsController(
            IPresetListingService presetListingService,
            ITranscodeJobService transcodeJobService)
            : this(
                presetListingService,
                transcodeJobService,
                new BackgroundTranscodeJobStarter(transcodeJobService),
                () => Plugin.Instance?.Configuration)
        {
        }

        internal TranscodedDownloadsController(
            IPresetListingService presetListingService,
            ITranscodeJobService transcodeJobService,
            ITranscodeJobStarter transcodeJobStarter,
            Func<PluginConfiguration?> configurationProvider)
        {
            _presetListingService = presetListingService ?? throw new ArgumentNullException(nameof(presetListingService));
            _transcodeJobService = transcodeJobService ?? throw new ArgumentNullException(nameof(transcodeJobService));
            _transcodeJobStarter = transcodeJobStarter ?? throw new ArgumentNullException(nameof(transcodeJobStarter));
            _configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
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
                var job = _transcodeJobService.CreateJob(request, configuration);
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
            return Ok(_transcodeJobService.GetJobs());
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
