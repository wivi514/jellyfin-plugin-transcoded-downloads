using System;
using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Enums;

namespace Jellyfin.Plugin.TranscodedDownloads.Configuration
{
    /// <summary>
    /// A capability profile describes what this server can safely attempt.
    /// </summary>
    public sealed class CapabilityProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilityProfile"/> class.
        /// </summary>
        public CapabilityProfile()
        {
            Id = Guid.NewGuid().ToString("N");
            AllowedVideoCodecs = new List<VideoCodec>();
            AllowedAudioCodecs = new List<AudioCodec>();
            AllowedContainers = new List<ContainerFormat>();
        }

        /// <summary>
        /// Gets or sets the unique identifier for this capability profile.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name for this capability profile.
        /// </summary>
        public string Name { get; set; } = "CPU H.264";

        /// <summary>
        /// Gets or sets the transcoding backend for this capability profile.
        /// </summary>
        public TranscodeBackend Backend { get; set; } = TranscodeBackend.Software;

        /// <summary>
        /// Gets or sets the list of allowed video codecs for this capability profile.
        /// </summary>
        public List<VideoCodec> AllowedVideoCodecs { get; set; }

        /// <summary>
        /// Gets or sets the list of allowed audio codecs for this capability profile.
        /// </summary>
        public List<AudioCodec> AllowedAudioCodecs { get; set; }

        /// <summary>
        /// Gets or sets the list of allowed containers for this capability profile.
        /// </summary>
        public List<ContainerFormat> AllowedContainers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether hardware decode is supported.
        /// </summary>
        public bool SupportsHardwareDecode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether hardware encode is supported.
        /// </summary>
        public bool SupportsHardwareEncode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tone mapping is supported.
        /// </summary>
        public bool SupportsToneMapping { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether subtitle burn-in is supported.
        /// </summary>
        public bool SupportsSubtitleBurnIn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether two-pass encoding is supported.
        /// </summary>
        public bool SupportsTwoPass { get; set; }

        /// <summary>
        /// Gets or sets the device path for this capability profile.
        /// </summary>
        public string DevicePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets notes about this capability profile.
        /// </summary>
        public string Notes { get; set; } = string.Empty;
    }
}
```