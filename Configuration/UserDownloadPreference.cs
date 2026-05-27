using System;

namespace Jellyfin.Plugin.TranscodedDownloads.Configuration
{
    /// <summary>
    /// A user's download preference for default presets.
    /// </summary>
    public sealed class UserDownloadPreference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserDownloadPreference"/> class.
        /// </summary>
        public UserDownloadPreference()
        {
            UserId = Guid.NewGuid();
        }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the default video preset ID.
        /// </summary>
        public string DefaultVideoPresetId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default music preset ID.
        /// </summary>
        public string DefaultMusicPresetId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to ask before starting large jobs.
        /// </summary>
        public bool AskBeforeStartingLargeJobs { get; set; } = true;
    }
}
```