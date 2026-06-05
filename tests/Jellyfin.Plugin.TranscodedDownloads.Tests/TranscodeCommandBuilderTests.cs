using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Services;
using Xunit;

namespace Jellyfin.Plugin.TranscodedDownloads.Tests
{
    public sealed class TranscodeCommandBuilderTests
    {
        private readonly TranscodeCommandBuilder _builder = new TranscodeCommandBuilder();

        [Fact]
        public void BuildCommand_WhenScalingOnly_AddsSingleScaleFilter()
        {
            var preset = CreatePreset();
            var profile = CreateProfile();

            var args = _builder.BuildCommand(preset, profile, "/media/input.mp4", "/tmp/output.mp4");

            Assert.Equal(new[] { "scale=160:90" }, GetVideoFilterArguments(args));
            Assert.Contains("-sn", args);
        }

        [Fact]
        public void BuildCommand_WhenSubtitleBurnInOnly_AddsSingleSubtitleFilter()
        {
            var preset = CreatePreset();
            preset.BurnSubtitles = true;
            preset.MaxWidth = null;
            preset.MaxHeight = null;
            var profile = CreateProfile();

            var args = _builder.BuildCommand(preset, profile, "/media/input.mp4", "/tmp/output.mp4");

            Assert.Equal(new[] { "subtitles" }, GetVideoFilterArguments(args));
            Assert.DoesNotContain("-sn", args);
        }

        [Fact]
        public void BuildCommand_WhenToneMappingOnly_AddsSingleToneMapFilter()
        {
            var preset = CreatePreset();
            preset.MaxWidth = null;
            preset.MaxHeight = null;
            preset.ToneMapHdrToSdr = true;
            var profile = CreateProfile();
            profile.SupportsToneMapping = true;

            var args = _builder.BuildCommand(preset, profile, "/media/input.mp4", "/tmp/output.mp4");

            Assert.Equal(new[] { "tonemap=hable" }, GetVideoFilterArguments(args));
        }

        [Fact]
        public void BuildCommand_WhenSubtitleBurnInAndScaling_ComposesSingleFilterChain()
        {
            var preset = CreatePreset();
            preset.BurnSubtitles = true;
            var profile = CreateProfile();

            var args = _builder.BuildCommand(preset, profile, "/media/input.mp4", "/tmp/output.mp4");

            Assert.Equal(new[] { "subtitles,scale=160:90" }, GetVideoFilterArguments(args));
            Assert.DoesNotContain("-vf", args);
        }

        [Fact]
        public void BuildCommand_WhenToneMappingAndScaling_ComposesSingleFilterChain()
        {
            var preset = CreatePreset();
            preset.ToneMapHdrToSdr = true;
            var profile = CreateProfile();
            profile.SupportsToneMapping = true;

            var args = _builder.BuildCommand(preset, profile, "/media/input.mp4", "/tmp/output.mp4");

            Assert.Equal(new[] { "tonemap=hable,scale=160:90" }, GetVideoFilterArguments(args));
            Assert.DoesNotContain("-vf", args);
        }

        [Fact]
        public void BuildCommand_WhenAllSoftwareFiltersEnabled_ComposesSingleFilterChain()
        {
            var preset = CreatePreset();
            preset.BurnSubtitles = true;
            preset.ToneMapHdrToSdr = true;
            var profile = CreateProfile();
            profile.SupportsToneMapping = true;

            var args = _builder.BuildCommand(preset, profile, "/media/input.mp4", "/tmp/output.mp4");

            Assert.Equal(new[] { "subtitles,tonemap=hable,scale=160:90" }, GetVideoFilterArguments(args));
            Assert.DoesNotContain("-vf", args);
        }

        [Fact]
        public void BuildCommand_WhenHardwareAndSoftwareFiltersEnabled_ComposesSingleFilterChain()
        {
            var preset = CreatePreset();
            preset.BurnSubtitles = true;
            preset.ToneMapHdrToSdr = true;
            var profile = CreateProfile();
            profile.Backend = TranscodeBackend.Nvenc;
            profile.SupportsToneMapping = true;

            var args = _builder.BuildCommand(preset, profile, "/media/input.mp4", "/tmp/output.mp4");

            Assert.Equal(new[] { "format=nv12|cuda,subtitles,tonemap=hable,scale=160:90" }, GetVideoFilterArguments(args));
            Assert.DoesNotContain("-vf", args);
        }

        private static CapabilityProfile CreateProfile()
        {
            return new CapabilityProfile
            {
                Id = "profile",
                Name = "CPU H.264",
                Backend = TranscodeBackend.Software,
                AllowedVideoCodecs = new List<VideoCodec> { VideoCodec.H264 },
                AllowedAudioCodecs = new List<AudioCodec> { AudioCodec.Aac },
                AllowedContainers = new List<ContainerFormat> { ContainerFormat.Mp4 },
                SupportsSubtitleBurnIn = true
            };
        }

        private static AdminTranscodePreset CreatePreset()
        {
            return new AdminTranscodePreset
            {
                Id = "preset",
                Name = "Preset",
                Enabled = true,
                CapabilityProfileId = "profile",
                Container = ContainerFormat.Mp4,
                VideoCodec = VideoCodec.H264,
                AudioCodec = AudioCodec.Aac,
                MaxWidth = 160,
                MaxHeight = 90,
                VideoBitrateKbps = 256,
                AudioBitrateKbps = 64,
                AudioChannels = 1,
                IsVideoPreset = true,
                IsAudioOnlyPreset = false,
                BurnSubtitles = false,
                ToneMapHdrToSdr = false
            };
        }

        private static IReadOnlyList<string> GetVideoFilterArguments(IReadOnlyList<string> args)
        {
            return args
                .Select((argument, index) => new { argument, index })
                .Where(item => item.argument == "-filter:v" || item.argument == "-vf")
                .Select(item => item.index + 1 < args.Count ? args[item.index + 1] : string.Empty)
                .ToList();
        }
    }
}
