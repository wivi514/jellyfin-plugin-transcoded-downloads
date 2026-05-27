using System;
using Jellyfin.Plugin.TranscodedDownloads.Enums;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Maps transcoding backend and codec enums to FFmpeg encoder names.
    /// </summary>
    public sealed class EncoderMap
    {
        /// <summary>
        /// Resolves the video encoder name for a given backend and codec.
        /// </summary>
        /// <param name="backend">The transcoding backend.</param>
        /// <param name="codec">The video codec.</param>
        /// <returns>The FFmpeg video encoder name.</returns>
        public string ResolveVideoEncoder(TranscodeBackend backend, VideoCodec codec)
        {
            return (backend, codec) switch
            {
                (TranscodeBackend.Software, VideoCodec.H264) => "libx264",
                (TranscodeBackend.Software, VideoCodec.H265) => "libx265",
                (TranscodeBackend.Software, VideoCodec.Av1) => "libsvtav1",
                (TranscodeBackend.Software, VideoCodec.Vp9) => "libvpx-vp9",
                (TranscodeBackend.Vaapi, VideoCodec.H264) => "h264_vaapi",
                (TranscodeBackend.Vaapi, VideoCodec.H265) => "hevc_vaapi",
                (TranscodeBackend.Vaapi, VideoCodec.Vp9) => "vp9_vaapi",
                (TranscodeBackend.Qsv, VideoCodec.H264) => "h264_qsv",
                (TranscodeBackend.Qsv, VideoCodec.H265) => "hevc_qsv",
                (TranscodeBackend.Nvenc, VideoCodec.H264) => "h264_nvenc",
                (TranscodeBackend.Nvenc, VideoCodec.H265) => "hevc_nvenc",
                (TranscodeBackend.Nvenc, VideoCodec.Av1) => "av1_nvenc",
                (TranscodeBackend.Amf, VideoCodec.H264) => "h264_amf",
                (TranscodeBackend.Amf, VideoCodec.H265) => "hevc_amf",
                (TranscodeBackend.VideoToolbox, VideoCodec.H264) => "h264_videotoolbox",
                (TranscodeBackend.VideoToolbox, VideoCodec.H265) => "hevc_videotoolbox",
                (_, VideoCodec.Copy) => "copy",
                _ => throw new UnsupportedCapabilityException($"Unsupported encoder mapping: {backend} + {codec}")
            };
        }

        /// <summary>
        /// Resolves the audio encoder name for a given backend and codec.
        /// </summary>
        /// <param name="backend">The transcoding backend.</param>
        /// <param name="codec">The audio codec.</param>
        /// <returns>The FFmpeg audio encoder name.</returns>
        public string ResolveAudioEncoder(TranscodeBackend backend, AudioCodec codec)
        {
            return (backend, codec) switch
            {
                (TranscodeBackend.Software, AudioCodec.Aac) => "aac",
                (TranscodeBackend.Software, AudioCodec.Mp3) => "mp3",
                (TranscodeBackend.Software, AudioCodec.Opus) => "opus",
                (TranscodeBackend.Software, AudioCodec.Flac) => "flac",
                (TranscodeBackend.Vaapi, AudioCodec.Aac) => "aac",
                (TranscodeBackend.Vaapi, AudioCodec.Mp3) => "mp3",
                (TranscodeBackend.Qsv, AudioCodec.Aac) => "aac",
                (TranscodeBackend.Qsv, AudioCodec.Mp3) => "mp3",
                (TranscodeBackend.Nvenc, AudioCodec.Aac) => "aac",
                (TranscodeBackend.Nvenc, AudioCodec.Mp3) => "mp3",
                (TranscodeBackend.Amf, AudioCodec.Aac) => "aac",
                (TranscodeBackend.Amf, AudioCodec.Mp3) => "mp3",
                (TranscodeBackend.VideoToolbox, AudioCodec.Aac) => "aac",
                (TranscodeBackend.VideoToolbox, AudioCodec.Mp3) => "mp3",
                (_, AudioCodec.Copy) => "copy",
                _ => throw new UnsupportedCapabilityException($"Unsupported encoder mapping: {backend} + {codec}")
            };
        }
    }
}
```