using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.TranscodedDownloads.Configuration;
using Jellyfin.Plugin.TranscodedDownloads.Enums;
using Jellyfin.Plugin.TranscodedDownloads.Models;

namespace Jellyfin.Plugin.TranscodedDownloads.Services
{
    /// <summary>
    /// Manages temporary files for transcoded downloads.
    /// </summary>
    public sealed class TempFileStore : ITempFileStore
    {
        private static readonly HashSet<string> ReservedWindowsFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };

        private static readonly string[] DisallowedTempRoots =
        {
            "/mnt/media",
            "/media",
            "/library",
            "/movies",
            "/tv",
            "/music"
        };

        private readonly string _defaultTempRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFileStore"/> class.
        /// </summary>
        public TempFileStore()
            : this(Path.Combine(Path.GetTempPath(), "jellyfin-transcoded-downloads"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFileStore"/> class.
        /// </summary>
        /// <param name="defaultTempRoot">The default temp root used when plugin configuration is empty.</param>
        public TempFileStore(string defaultTempRoot)
        {
            if (string.IsNullOrWhiteSpace(defaultTempRoot))
            {
                throw new ArgumentException("Default temp root cannot be empty.", nameof(defaultTempRoot));
            }

            _defaultTempRoot = defaultTempRoot;
        }

        /// <inheritdoc />
        public TempFileReservation ReserveOutputFile(
            PluginConfiguration configuration,
            Guid jobId,
            string itemName,
            AdminTranscodePreset preset)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (jobId == Guid.Empty)
            {
                throw new ArgumentException("Job ID cannot be empty.", nameof(jobId));
            }

            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            var tempRoot = ResolveTempRoot(configuration);
            var jobDirectory = EnsureInsideRoot(tempRoot, Path.Combine(tempRoot, jobId.ToString("N")));
            Directory.CreateDirectory(jobDirectory);

            var outputFileName = BuildOutputFileName(itemName, preset);
            var outputPath = EnsureInsideRoot(tempRoot, Path.Combine(jobDirectory, outputFileName));

            return new TempFileReservation
            {
                JobId = jobId,
                JobDirectory = jobDirectory,
                OutputFileName = outputFileName,
                OutputPath = outputPath
            };
        }

        /// <inheritdoc />
        public bool DeleteJobDirectory(PluginConfiguration configuration, Guid jobId)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (jobId == Guid.Empty)
            {
                throw new ArgumentException("Job ID cannot be empty.", nameof(jobId));
            }

            var tempRoot = ResolveTempRoot(configuration);
            var jobDirectory = EnsureInsideRoot(tempRoot, Path.Combine(tempRoot, jobId.ToString("N")));
            if (!Directory.Exists(jobDirectory))
            {
                return false;
            }

            Directory.Delete(jobDirectory, recursive: true);
            return true;
        }

        /// <summary>
        /// Gets the file extension for a container format.
        /// </summary>
        /// <param name="container">The container format.</param>
        /// <returns>The file extension, including the leading dot.</returns>
        public static string GetExtension(ContainerFormat container)
        {
            return container switch
            {
                ContainerFormat.Mp4 => ".mp4",
                ContainerFormat.Mkv => ".mkv",
                ContainerFormat.Webm => ".webm",
                ContainerFormat.Mp3 => ".mp3",
                ContainerFormat.M4a => ".m4a",
                ContainerFormat.Ogg => ".ogg",
                ContainerFormat.Flac => ".flac",
                _ => throw new ArgumentOutOfRangeException(nameof(container), container, "Unsupported container format.")
            };
        }

        /// <summary>
        /// Builds a safe output file name for a media item and preset.
        /// </summary>
        /// <param name="itemName">The item display name.</param>
        /// <param name="preset">The output preset.</param>
        /// <returns>The safe output file name.</returns>
        public static string BuildOutputFileName(string itemName, AdminTranscodePreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            var safeItemName = SanitizeFileName(itemName);
            var safePresetName = SanitizeFileName(preset.Name);
            return $"{safeItemName} - {safePresetName}{GetExtension(preset.Container)}";
        }

        /// <summary>
        /// Sanitizes a file name segment for cross-platform temporary output use.
        /// </summary>
        /// <param name="value">The candidate file name segment.</param>
        /// <returns>The sanitized file name segment.</returns>
        public static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Untitled";
            }

            var invalidChars = Path.GetInvalidFileNameChars()
                .Concat(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' })
                .ToHashSet();
            var chars = value
                .Select(character => char.IsControl(character) || invalidChars.Contains(character) ? ' ' : character)
                .ToArray();
            var sanitized = CollapseWhitespace(new string(chars)).Trim().TrimEnd('.', ' ');
            sanitized = RemoveDotOnlySegments(sanitized);

            if (sanitized.Length == 0)
            {
                sanitized = "Untitled";
            }

            var baseName = Path.GetFileNameWithoutExtension(sanitized);
            if (ReservedWindowsFileNames.Contains(baseName))
            {
                sanitized = $"{sanitized}_";
            }

            return sanitized;
        }

        private string ResolveTempRoot(PluginConfiguration configuration)
        {
            var configuredRoot = string.IsNullOrWhiteSpace(configuration.TempDirectory)
                ? _defaultTempRoot
                : configuration.TempDirectory;
            var tempRoot = Path.GetFullPath(configuredRoot);

            if (IsDisallowedTempRoot(tempRoot))
            {
                throw new InvalidOperationException("Temporary download files must not be stored inside a media library path.");
            }

            Directory.CreateDirectory(tempRoot);
            return tempRoot;
        }

        private static bool IsDisallowedTempRoot(string tempRoot)
        {
            if (!Path.IsPathRooted(tempRoot) || Path.DirectorySeparatorChar != '/')
            {
                return false;
            }

            var normalizedRoot = NormalizePathForComparison(tempRoot);
            return DisallowedTempRoots.Any(disallowedRoot =>
                normalizedRoot == disallowedRoot || normalizedRoot.StartsWith(disallowedRoot + "/", StringComparison.Ordinal));
        }

        private static string EnsureInsideRoot(string root, string candidatePath)
        {
            var fullRoot = Path.GetFullPath(root);
            var fullCandidate = Path.GetFullPath(candidatePath);
            var rootWithSeparator = fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? fullRoot
                : fullRoot + Path.DirectorySeparatorChar;

            if (!fullCandidate.Equals(fullRoot, StringComparison.Ordinal)
                && !fullCandidate.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Resolved temporary path escaped the configured temporary root.");
            }

            return fullCandidate;
        }

        private static string NormalizePathForComparison(string path)
        {
            var normalized = path.Replace('\\', '/').TrimEnd('/');
            return normalized.Length == 0 ? "/" : normalized;
        }

        private static string CollapseWhitespace(string value)
        {
            return string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        private static string RemoveDotOnlySegments(string value)
        {
            var segments = value
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(segment => segment.Any(character => character != '.'));

            return string.Join(" ", segments);
        }
    }
}
