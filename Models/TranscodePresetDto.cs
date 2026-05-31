using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Enums;

namespace Jellyfin.Plugin.TranscodedDownloads.Models
{
    /// <summary>
    /// User-facing transcode preset information.
    /// </summary>
    public sealed class TranscodePresetDto
    {
        /// <summary>
        /// Gets or sets the preset ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the preset display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the capability profile ID.
        /// </summary>
        public string CapabilityProfileId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the output container.
        /// </summary>
        public ContainerFormat Container { get; set; }

        /// <summary>
        /// Gets or sets the video codec.
        /// </summary>
        public VideoCodec VideoCodec { get; set; }

        /// <summary>
        /// Gets or sets the audio codec.
        /// </summary>
        public AudioCodec AudioCodec { get; set; }

        /// <summary>
        /// Gets or sets the maximum output width.
        /// </summary>
        public int? MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum output height.
        /// </summary>
        public int? MaxHeight { get; set; }

        /// <summary>
        /// Gets or sets the video bitrate in kbps.
        /// </summary>
        public int? VideoBitrateKbps { get; set; }

        /// <summary>
        /// Gets or sets the audio bitrate in kbps.
        /// </summary>
        public int? AudioBitrateKbps { get; set; }

        /// <summary>
        /// Gets or sets the audio channel count.
        /// </summary>
        public int? AudioChannels { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether stream copy may be used when compatible.
        /// </summary>
        public bool AllowStreamCopyWhenCompatible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether subtitles should be burned in.
        /// </summary>
        public bool BurnSubtitles { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether HDR should be tone-mapped to SDR.
        /// </summary>
        public bool ToneMapHdrToSdr { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this preset is for video media.
        /// </summary>
        public bool IsVideoPreset { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this preset is for audio-only media.
        /// </summary>
        public bool IsAudioOnlyPreset { get; set; }

        /// <summary>
        /// Gets or sets validation warnings that are useful to show to users.
        /// </summary>
        public IReadOnlyList<string> Warnings { get; set; } = new List<string>();
    }
}
