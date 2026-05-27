using System;
using System.Collections.Generic;

namespace Jellyfin.Plugin.TranscodedDownloads.Configuration
{
    /// <summary>
    /// Result of preset validation.
    /// </summary>
    public sealed class PresetValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PresetValidationResult"/> class.
        /// </summary>
        public PresetValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the preset is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the list of validation errors.
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// Gets or sets the list of validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; }
    }
}
```