using System;
using Jellyfin.Plugin.TranscodedDownloads.Enums;

namespace Jellyfin.Plugin.TranscodedDownloads.Configuration
{
    /// <summary>
    /// A preset that defines how to transcode media for download.
    /// </summary>
    public sealed class AdminTranscodePreset
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdminTranscodePreset"/> class.
        /// </summary>
        public AdminTranscodePreset()
        {
            Id = Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Gets or sets the unique identifier for this preset.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name for this preset.
        /// </summary>
        public string Name { get; set; } = "1080p H.264 AAC MP4";

        /// <summary>
        /// Gets or sets a value indicating whether this preset is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the capability profile ID this preset belongs to.
        /// </summary>
        public string CapabilityProfileId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the container format for the output.
        /// </summary>
        public ContainerFormat Container { get; set; } = ContainerFormat.Mp4;

        /// <summary>
        /// Gets or sets the video codec for the output.
        /// </summary>
        public VideoCodec VideoCodec { get; set; } = VideoCodec.H264;

        /// <summary>
        /// Gets or sets the audio codec for the output.
        /// </summary>
        public AudioCodec AudioCodec { get; set; } = AudioCodec.Aac;

        /// <summary>
        /// Gets or sets the maximum width for the output video.
        /// </summary>
        public int? MaxWidth { get; set; } = 1920;

        /// <summary>
        /// Gets or sets the maximum height for the output video.
        /// </summary>
        public int? MaxHeight { get; set; } = 1080;

        /// <summary>
        /// Gets or sets the video bitrate in kbps.
        /// </summary>
        public int? VideoBitrateKbps { get; set; } = 8000;

        /// <summary>
        /// Gets or sets the audio bitrate in kbps.
        /// </summary>
        public int? AudioBitrateKbps { get; set; } = 192;

        /// <summary>
        /// Gets or sets the number of audio channels.
        /// </summary>
        public int? AudioChannels { get; set; } = 2;

        /// <summary>
        /// Gets or sets a value indicating whether stream copy is allowed when compatible.
        /// </summary>
        public bool AllowStreamCopyWhenCompatible { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether subtitles should be burned in.
        /// </summary>
        public bool BurnSubtitles { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether HDR should be tone-mapped to SDR.
        /// </summary>
        public bool ToneMapHdrToSdr { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether original audio should be preserved if compatible.
        /// </summary>
        public bool PreserveOriginalAudioIfCompatible { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this is a video preset.
        /// </summary>
        public bool IsVideoPreset { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this is an audio-only preset.
        /// </summary>
        public bool IsAudioOnlyPreset { get; set; } = false;
    }
}
