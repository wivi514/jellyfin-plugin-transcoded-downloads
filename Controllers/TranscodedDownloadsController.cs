using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Exceptions;
using Jellyfin.Plugin.TranscodedDownloads.Models;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.TranscodedDownloads.Controllers
{
    /// <summary>
    /// API controller for transcoded download metadata.
    /// </summary>
    [ApiController]
    [Route("TranscodedDownloads")]
    public sealed class TranscodedDownloadsController : ControllerBase
    {
        private readonly IPresetListingService _presetListingService;
        private readonly ITranscodeJobService _transcodeJobService;

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
        /// <param name="presetListingService">The preset listing service.</param>
        /// <param name="transcodeJobService">The transcode job service.</param>
        public TranscodedDownloadsController(
            IPresetListingService presetListingService,
            ITranscodeJobService transcodeJobService)
        {
            _presetListingService = presetListingService ?? throw new ArgumentNullException(nameof(presetListingService));
            _transcodeJobService = transcodeJobService ?? throw new ArgumentNullException(nameof(transcodeJobService));
        }

        /// <summary>
        /// Gets the enabled presets that are valid for this server.
        /// </summary>
        /// <returns>The available transcode presets.</returns>
        [HttpGet("Presets")]
        public ActionResult<IReadOnlyList<TranscodePresetDto>> GetPresets()
        {
            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                return StatusCode(503);
            }

            return Ok(_presetListingService.GetAvailablePresets(plugin.Configuration));
        }

        /// <summary>
        /// Creates a transcoded download job.
        /// </summary>
        /// <param name="request">The create job request.</param>
        /// <returns>The created job.</returns>
        [HttpPost("Jobs")]
        public ActionResult<DownloadJobDto> CreateJob(CreateDownloadJobRequest request)
        {
            var plugin = Plugin.Instance;
            if (plugin == null)
            {
                return StatusCode(503);
            }

            try
            {
                return Ok(_transcodeJobService.CreateJob(request, plugin.Configuration));
            }
            catch (InvalidPresetException ex)
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
    }
}
