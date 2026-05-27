using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TranscodedDownloads.Configuration
{
    /// <summary>
    /// The plugin configuration class.
    /// </summary>
    public sealed class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            Presets = new List<AdminTranscodePreset>();
            CapabilityProfiles = new List<CapabilityProfile>();
            UserPreferences = new List<UserDownloadPreference>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether video downloads are enabled.
        /// </summary>
        public bool EnableVideoDownloads { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether music downloads are enabled.
        /// </summary>
        public bool EnableMusicDownloads { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether web UI injection is enabled.
        /// </summary>
        public bool EnableWebUiInjection { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether advanced unsafe FFmpeg arguments are enabled.
        /// </summary>
        public bool EnableAdvancedUnsafeFfmpegArguments { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of concurrent jobs.
        /// </summary>
        public int MaxConcurrentJobs { get; set; } = 1;

        /// <summary>
        /// Gets or sets the maximum queue size.
        /// </summary>
        public int MaxQueueSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the job retention hours.
        /// </summary>
        public int JobRetentionHours { get; set; } = 24;

        /// <summary>
        /// Gets or sets the maximum temporary folder size in MB.
        /// </summary>
        public long MaxTempFolderSizeMb { get; set; } = 50_000;

        /// <summary>
        /// Gets or sets the temporary directory path.
        /// </summary>
        public string TempDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of capability profiles.
        /// </summary>
        public List<CapabilityProfile> CapabilityProfiles { get; set; }

        /// <summary>
        /// Gets or sets the list of admin transcode presets.
        /// </summary>
        public List<AdminTranscodePreset> Presets { get; set; }

        /// <summary>
        /// Gets or sets the list of user download preferences.
        /// </summary>
        public List<UserDownloadPreference> UserPreferences { get; set; }
    }
}
```