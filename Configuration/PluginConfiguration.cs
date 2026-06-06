using System.Collections.Generic;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.TranscodedDownloads.Configuration
{
    /// <summary>
    /// The plugin configuration class.
    /// </summary>
    public sealed class PluginConfiguration : BasePluginConfiguration
    {
        private const string DefaultCpuProfileId = "cpu-h264-aac";

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            CapabilityProfiles = new List<CapabilityProfile>
            {
                CreateDefaultCpuProfile()
            };
            Presets = new List<AdminTranscodePreset>
            {
                CreateVideoPreset("480p-h264-aac-mp4", "480p H.264 AAC MP4", VideoCodec.H264, ContainerFormat.Mp4, 854, 480, 2000, 128),
                CreateVideoPreset("720p-h264-aac-mp4", "720p H.264 AAC MP4", VideoCodec.H264, ContainerFormat.Mp4, 1280, 720, 4000, 160),
                CreateVideoPreset("1080p-h264-aac-mp4", "1080p H.264 AAC MP4", VideoCodec.H264, ContainerFormat.Mp4, 1920, 1080, 8000, 192),
                CreateVideoPreset("1440p-h264-aac-mp4", "1440p H.264 AAC MP4", VideoCodec.H264, ContainerFormat.Mp4, 2560, 1440, 16000, 192),
                CreateVideoPreset("2160p-h264-aac-mp4", "4K H.264 AAC MP4", VideoCodec.H264, ContainerFormat.Mp4, 3840, 2160, 35000, 256),
                CreateVideoPreset("1080p-h265-aac-mp4", "1080p H.265 AAC MP4", VideoCodec.H265, ContainerFormat.Mp4, 1920, 1080, 5000, 192),
                CreateVideoPreset("2160p-h265-aac-mp4", "4K H.265 AAC MP4", VideoCodec.H265, ContainerFormat.Mp4, 3840, 2160, 20000, 256),
                CreateVideoPreset("1080p-av1-opus-webm", "1080p AV1 Opus WebM", VideoCodec.Av1, ContainerFormat.Webm, 1920, 1080, 3500, 160, AudioCodec.Opus)
            };
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

        private static CapabilityProfile CreateDefaultCpuProfile()
        {
            return new CapabilityProfile
            {
                Id = DefaultCpuProfileId,
                Name = "CPU common codecs",
                Backend = TranscodeBackend.Software,
                AllowedVideoCodecs = new List<VideoCodec> { VideoCodec.H264, VideoCodec.H265, VideoCodec.Av1, VideoCodec.Vp9 },
                AllowedAudioCodecs = new List<AudioCodec> { AudioCodec.Aac, AudioCodec.Opus },
                AllowedContainers = new List<ContainerFormat> { ContainerFormat.Mp4, ContainerFormat.Mkv, ContainerFormat.Webm },
                SupportsSubtitleBurnIn = true,
                SupportsToneMapping = true
            };
        }

        private static AdminTranscodePreset CreateVideoPreset(
            string id,
            string name,
            VideoCodec videoCodec,
            ContainerFormat container,
            int maxWidth,
            int maxHeight,
            int videoBitrateKbps,
            int audioBitrateKbps,
            AudioCodec audioCodec = AudioCodec.Aac)
        {
            return new AdminTranscodePreset
            {
                Id = id,
                Name = name,
                CapabilityProfileId = DefaultCpuProfileId,
                Container = container,
                VideoCodec = videoCodec,
                AudioCodec = audioCodec,
                MaxWidth = maxWidth,
                MaxHeight = maxHeight,
                VideoBitrateKbps = videoBitrateKbps,
                AudioBitrateKbps = audioBitrateKbps,
                AudioChannels = 2,
                IsVideoPreset = true,
                IsAudioOnlyPreset = false
            };
        }
    }
}
