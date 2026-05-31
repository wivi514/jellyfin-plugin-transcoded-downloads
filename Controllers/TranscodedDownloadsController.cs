using System.Collections.Generic;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodedDownloadsController"/> class.
        /// </summary>
        public TranscodedDownloadsController()
            : this(new PresetListingService())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodedDownloadsController"/> class.
        /// </summary>
        /// <param name="presetListingService">The preset listing service.</param>
        public TranscodedDownloadsController(IPresetListingService presetListingService)
        {
            _presetListingService = presetListingService;
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
    }
}
