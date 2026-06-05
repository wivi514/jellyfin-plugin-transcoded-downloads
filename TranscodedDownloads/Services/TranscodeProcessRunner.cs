using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Runs FFmpeg using ProcessStartInfo.ArgumentList.
    /// </summary>
    public sealed class TranscodeProcessRunner : ITranscodeProcessRunner
    {
        private readonly ITranscodeCommandBuilder _commandBuilder;
        private readonly string _ffmpegPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeProcessRunner"/> class.
        /// </summary>
        public TranscodeProcessRunner()
            : this(new TranscodeCommandBuilder(), ResolveDefaultFfmpegPath(Environment.GetEnvironmentVariable("JELLYFIN_FFMPEG")))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscodeProcessRunner"/> class.
        /// </summary>
        /// <param name="commandBuilder">The transcode command builder.</param>
        /// <param name="ffmpegPath">The FFmpeg executable path.</param>
        public TranscodeProcessRunner(ITranscodeCommandBuilder commandBuilder, string ffmpegPath)
        {
            _commandBuilder = commandBuilder ?? throw new ArgumentNullException(nameof(commandBuilder));
            if (string.IsNullOrWhiteSpace(ffmpegPath))
            {
                throw new ArgumentException("FFmpeg path cannot be empty.", nameof(ffmpegPath));
            }

            _ffmpegPath = ffmpegPath;
        }

        /// <inheritdoc />
        public async Task<TranscodeProcessResult> RunAsync(
            AdminTranscodePreset preset,
            CapabilityProfile capabilityProfile,
            string inputPath,
            string outputPath,
            CancellationToken cancellationToken)
        {
            var args = _commandBuilder.BuildCommand(preset, capabilityProfile, inputPath, outputPath);
            var startInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = false,
                CreateNoWindow = true
            };

            foreach (var arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using var process = new Process { StartInfo = startInfo };
            try
            {
                if (!process.Start())
                {
                    return TranscodeProcessResult.Failure(-1, "FFmpeg did not start.");
                }

                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                var stderr = await stderrTask.ConfigureAwait(false);

                return process.ExitCode == 0
                    ? TranscodeProcessResult.Success()
                    : TranscodeProcessResult.Failure(process.ExitCode, SummarizeError(stderr));
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return TranscodeProcessResult.Failure(-1, "The transcode was cancelled.");
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
            {
                return TranscodeProcessResult.Failure(-1, ex.Message);
            }
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        internal static string ResolveDefaultFfmpegPath(string? environmentPath)
        {
            return string.IsNullOrWhiteSpace(environmentPath) ? "ffmpeg" : environmentPath;
        }

        private static string SummarizeError(string stderr)
        {
            if (string.IsNullOrWhiteSpace(stderr))
            {
                return "FFmpeg failed without error output.";
            }

            var trimmed = stderr.Trim();
            return trimmed.Length <= 2000 ? trimmed : trimmed[^2000..];
        }
    }
}
