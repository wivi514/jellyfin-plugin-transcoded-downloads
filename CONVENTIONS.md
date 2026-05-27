# CONVENTIONS.md

You are running inside Aider, a CLI coding assistant. Files added with /add or /read by the user are visible to you in the conversation as code blocks. When the user asks you to modify code, respond with Aider's SEARCH/REPLACE edit blocks — Aider will apply them to disk. Do NOT claim you lack filesystem access; Aider handles all reads and writes. If you need to see a file that hasn't been added, ask the user to /add it.

## Project

Working name: **Jellyfin Transcoded Downloads**

Goal: add a Jellyfin-integrated way to download a transcoded copy of a movie, episode, or music item directly from Jellyfin, without requiring a separate client app.

This document defines architecture, coding conventions, configuration rules, API conventions, and safety constraints for the plugin.

---

## Core Product Rule

The plugin must never assume that every server can transcode the same way.

Transcoding support depends on:

- CPU speed
- GPU vendor
- GPU generation
- operating system
- driver stack
- Jellyfin FFmpeg build
- available codecs
- installed hardware acceleration backend
- container/Docker device passthrough
- Jellyfin server configuration

Therefore, transcoding must be driven by **explicit configurable presets** and **capability validation**, not hardcoded FFmpeg commands.

---

## Primary Use Cases

### 1. Download transcoded video

A Jellyfin user can choose a movie or episode and download a transcoded version using an allowed preset.

Example presets:

- `Original quality remux-compatible MP4`
- `1080p H.264 AAC MP4`
- `720p H.264 AAC MP4`
- `Mobile low bitrate H.264 AAC MP4`
- `HEVC archive copy`
- `AV1 experimental copy`

### 2. Download transcoded music

A Jellyfin user can choose a music track or album and download a transcoded version using an allowed preset.

Example presets:

- `MP3 320 kbps`
- `MP3 192 kbps`
- `AAC 256 kbps`
- `Opus 160 kbps`
- `FLAC copy if compatible`

### 3. Admin controls available transcode modes

The Jellyfin server admin can configure what transcoding methods are available on that server.

Examples:

- CPU-only H.264
- Intel QSV H.264/HEVC
- NVIDIA NVENC H.264/HEVC/AV1
- AMD VA-API or AMF H.264/HEVC
- Apple VideoToolbox H.264/HEVC
- audio-only CPU transcoding

### 4. Per-user preferences

A Jellyfin user can choose their default download preset from the presets allowed by the admin.

Users must not be allowed to enter arbitrary FFmpeg arguments unless the admin explicitly enables an advanced unsafe mode.

---

## Non-Goals

Do not build these features in the first version:

- full offline sync system
- automatic background sync to devices
- mobile-app-specific downloads
- Plex-style managed downloads
- download scheduling by device
- full media optimizer/replacement system
- automatic permanent replacement of library files
- arbitrary user-provided FFmpeg commands
- remote transcoding cluster support
- complex distributed job orchestration

The first useful version should focus on:

1. select item
2. select preset
3. generate transcoded file
4. download file
5. clean temporary file

---

## Repository Layout

Use this structure unless there is a strong reason to change it.

```text
jellyfin-plugin-transcoded-downloads/
├── CONVENTIONS.md
├── README.md
├── LICENSE
├── build.yaml
├── manifest.json
├── src/
│   └── Jellyfin.Plugin.TranscodedDownloads/
│       ├── Jellyfin.Plugin.TranscodedDownloads.csproj
│       ├── Plugin.cs
│       ├── Configuration/
│       │   ├── PluginConfiguration.cs
│       │   ├── AdminTranscodePreset.cs
│       │   ├── CapabilityProfile.cs
│       │   ├── UserDownloadPreference.cs
│       │   └── PresetValidationResult.cs
│       ├── Controllers/
│       │   ├── TranscodedDownloadsController.cs
│       │   ├── DownloadJobsController.cs
│       │   └── UserPreferencesController.cs
│       ├── Services/
│       │   ├── ITranscodeCommandBuilder.cs
│       │   ├── TranscodeCommandBuilder.cs
│       │   ├── ICapabilityDetector.cs
│       │   ├── CapabilityDetector.cs
│       │   ├── ITranscodeJobService.cs
│       │   ├── TranscodeJobService.cs
│       │   ├── ITempFileStore.cs
│       │   ├── TempFileStore.cs
│       │   ├── IProgressTracker.cs
│       │   └── ProgressTracker.cs
│       ├── Models/
│       │   ├── CreateDownloadJobRequest.cs
│       │   ├── DownloadJobDto.cs
│       │   ├── TranscodePresetDto.cs
│       │   ├── CapabilityProfileDto.cs
│       │   └── UserDownloadPreferenceDto.cs
│       ├── Enums/
│       │   ├── TranscodeBackend.cs
│       │   ├── VideoCodec.cs
│       │   ├── AudioCodec.cs
│       │   ├── ContainerFormat.cs
│       │   ├── JobStatus.cs
│       │   └── PermissionMode.cs
│       ├── Exceptions/
│       │   ├── InvalidPresetException.cs
│       │   ├── UnsupportedCapabilityException.cs
│       │   └── TranscodeJobException.cs
│       ├── Pages/
│       │   ├── configPage.html
│       │   └── configPage.js
│       └── Web/
│           └── injected-download-button.js
└── tests/
    └── Jellyfin.Plugin.TranscodedDownloads.Tests/
        ├── TranscodeCommandBuilderTests.cs
        ├── PresetValidationTests.cs
        ├── CapabilityDetectorTests.cs
        └── TempFileStoreTests.cs
```

---

## Naming Conventions

### Plugin identity

Use these names unless renamed deliberately:

```text
Plugin display name: Transcoded Downloads
Plugin namespace: Jellyfin.Plugin.TranscodedDownloads
Assembly name: Jellyfin.Plugin.TranscodedDownloads
Default plugin config file: Jellyfin.Plugin.TranscodedDownloads.xml
```

### C# naming

Use standard C# naming:

- Classes: `PascalCase`
- Interfaces: `IName`
- Public properties: `PascalCase`
- Private fields: `_camelCase`
- Local variables: `camelCase`
- Async methods: suffix with `Async`
- DTOs: suffix with `Dto`
- Request models: suffix with `Request`
- Response models: suffix with `Response`
- Exceptions: suffix with `Exception`

Examples:

```csharp
public sealed class TranscodeJobService : ITranscodeJobService
{
    private readonly ILogger<TranscodeJobService> _logger;

    public Task<DownloadJobDto> CreateJobAsync(CreateDownloadJobRequest request, CancellationToken cancellationToken)
    {
        // ...
    }
}
```

---

## Architecture Rule

Keep these layers separate:

```text
Controller -> Service -> Command Builder -> FFmpeg Process -> Temp File Store
```

Controllers must not build FFmpeg commands directly.

Services must not trust request DTOs directly.

The command builder must only use validated presets.

The FFmpeg execution layer must never receive raw user input as command-line fragments.

---

## Configuration Model

The plugin has two configuration levels:

1. **Admin configuration**
2. **Per-user preferences**

### Admin configuration

Admin configuration controls what the server is allowed to do.

Admin config must include:

```csharp
public sealed class PluginConfiguration : BasePluginConfiguration
{
    public bool EnableVideoDownloads { get; set; } = true;
    public bool EnableMusicDownloads { get; set; } = true;
    public bool EnableWebUiInjection { get; set; } = false;
    public bool EnableAdvancedUnsafeFfmpegArguments { get; set; } = false;

    public int MaxConcurrentJobs { get; set; } = 1;
    public int MaxQueueSize { get; set; } = 10;
    public int JobRetentionHours { get; set; } = 24;
    public long MaxTempFolderSizeMb { get; set; } = 50_000;

    public string TempDirectory { get; set; } = string.Empty;
    public List<CapabilityProfile> CapabilityProfiles { get; set; } = new();
    public List<AdminTranscodePreset> Presets { get; set; } = new();
    public List<UserDownloadPreference> UserPreferences { get; set; } = new();
}
```

### Capability profile

A capability profile describes what this server can safely attempt.

```csharp
public sealed class CapabilityProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "CPU H.264";
    public TranscodeBackend Backend { get; set; } = TranscodeBackend.Software;

    public List<VideoCodec> AllowedVideoCodecs { get; set; } = new();
    public List<AudioCodec> AllowedAudioCodecs { get; set; } = new();
    public List<ContainerFormat> AllowedContainers { get; set; } = new();

    public bool SupportsHardwareDecode { get; set; }
    public bool SupportsHardwareEncode { get; set; }
    public bool SupportsToneMapping { get; set; }
    public bool SupportsSubtitleBurnIn { get; set; }
    public bool SupportsTwoPass { get; set; }

    public string DevicePath { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
```

### Admin preset

A preset is a user-selectable output target approved by the admin.

```csharp
public sealed class AdminTranscodePreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "1080p H.264 AAC MP4";
    public bool Enabled { get; set; } = true;

    public string CapabilityProfileId { get; set; } = string.Empty;

    public ContainerFormat Container { get; set; } = ContainerFormat.Mp4;
    public VideoCodec VideoCodec { get; set; } = VideoCodec.H264;
    public AudioCodec AudioCodec { get; set; } = AudioCodec.Aac;

    public int? MaxWidth { get; set; } = 1920;
    public int? MaxHeight { get; set; } = 1080;
    public int? VideoBitrateKbps { get; set; } = 8000;
    public int? AudioBitrateKbps { get; set; } = 192;
    public int? AudioChannels { get; set; } = 2;

    public bool AllowStreamCopyWhenCompatible { get; set; } = true;
    public bool BurnSubtitles { get; set; } = false;
    public bool ToneMapHdrToSdr { get; set; } = true;
    public bool PreserveOriginalAudioIfCompatible { get; set; } = false;

    public bool IsVideoPreset { get; set; } = true;
    public bool IsAudioOnlyPreset { get; set; } = false;
}
```

### Per-user preference

A user preference chooses defaults from the admin-approved presets.

```csharp
public sealed class UserDownloadPreference
{
    public Guid UserId { get; set; }
    public string DefaultVideoPresetId { get; set; } = string.Empty;
    public string DefaultMusicPresetId { get; set; } = string.Empty;
    public bool AskBeforeStartingLargeJobs { get; set; } = true;
}
```

---

## Enum Conventions

Use enums instead of strings for known transcoding options.

```csharp
public enum TranscodeBackend
{
    Software,
    Vaapi,
    Qsv,
    Nvenc,
    Amf,
    VideoToolbox,
    Rkmpp
}

public enum VideoCodec
{
    Copy,
    H264,
    H265,
    Av1,
    Vp9
}

public enum AudioCodec
{
    Copy,
    Aac,
    Mp3,
    Opus,
    Flac
}

public enum ContainerFormat
{
    Mp4,
    Mkv,
    Webm,
    Mp3,
    M4a,
    Ogg,
    Flac
}
```

Do not expose codec strings like `libx264`, `h264_vaapi`, or `hevc_nvenc` directly to normal users.

Map enum values to FFmpeg arguments internally.

---

## Preset Philosophy

A preset is not only a quality level.

A preset must define:

- container
- video codec
- audio codec
- bitrate or quality strategy
- maximum resolution
- subtitle behavior
- HDR tone-mapping behavior
- hardware backend
- stream-copy policy

Bad preset name:

```text
High quality
```

Good preset name:

```text
1080p H.264 AAC MP4 - CPU Compatible
```

Good preset name:

```text
1080p H.265 AAC MP4 - Intel QSV
```

Good preset name:

```text
Music MP3 320 kbps
```

---

## Default Presets

Ship conservative defaults.

### CPU-safe video presets

```text
720p H.264 AAC MP4 - CPU Compatible
1080p H.264 AAC MP4 - CPU Compatible
```

Use CPU-compatible H.264/AAC/MP4 as the baseline because it is broadly compatible.

### CPU-safe music presets

```text
MP3 320 kbps
MP3 192 kbps
AAC 256 kbps
Opus 160 kbps
```

### Hardware presets

Do not enable hardware presets automatically unless capability detection is implemented and passes validation.

Hardware presets should be generated or enabled only after the admin confirms the detected backend.

Examples:

```text
1080p H.264 AAC MP4 - Intel QSV
1080p H.265 AAC MP4 - Intel QSV
1080p H.264 AAC MP4 - NVIDIA NVENC
1080p H.265 AAC MP4 - NVIDIA NVENC
1080p H.264 AAC MP4 - AMD VA-API
1080p H.265 AAC MP4 - AMD VA-API
```

---

## Capability Detection

Capability detection is helpful, but admin configuration remains the source of truth.

Detection should check:

- FFmpeg path
- FFmpeg version
- available hardware acceleration methods
- available encoders
- available decoders
- OS platform
- Docker/container device access
- known device paths, such as `/dev/dri` on Linux

Detection should not guarantee that a preset will work perfectly.

Detection should return:

```csharp
public sealed class PresetValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
```

### Validation rules

A preset is invalid when:

- selected video codec is not allowed by its capability profile
- selected audio codec is not allowed by its capability profile
- selected container is not allowed by its capability profile
- bitrate is zero or negative
- max resolution is zero or negative
- subtitle burn-in is enabled but unsupported
- HDR tone-mapping is enabled but unsupported
- hardware backend requires a missing device path
- audio-only preset has video-only settings that would affect command generation

A preset may be valid with warnings when:

- tone mapping may fall back to CPU
- subtitle burn-in may force CPU work
- selected bitrate is very high
- selected resolution is higher than the source
- output format has limited player compatibility

---

## FFmpeg Command Rules

### Never concatenate raw command strings

Bad:

```csharp
var args = "-i " + inputPath + " " + userArgs + " " + outputPath;
```

Good:

```csharp
var args = new List<string>
{
    "-hide_banner",
    "-y",
    "-i", inputPath,
    "-map", "0:v:0",
    "-map", "0:a:0?",
    "-c:v", videoEncoder,
    "-b:v", $"{preset.VideoBitrateKbps}k",
    "-c:a", audioEncoder,
    "-b:a", $"{preset.AudioBitrateKbps}k",
    outputPath
};
```

### Always quote paths safely

Use `ProcessStartInfo.ArgumentList` instead of a single argument string when possible.

### No arbitrary user FFmpeg args

Normal users must never be allowed to provide raw FFmpeg arguments.

Admin-only advanced arguments may exist later, but must be:

- disabled by default
- clearly marked unsafe
- logged when used
- excluded from bug reports unless reproduced without them

### Command builder responsibilities

`TranscodeCommandBuilder` must:

- validate preset
- resolve encoder names
- resolve container extension
- resolve hardware backend flags
- resolve subtitle behavior
- resolve audio behavior
- resolve scaling behavior
- resolve stream-copy behavior
- output an argument list, not a shell string

---

## Backend Mapping Conventions

Keep backend mapping isolated in one place.

Example class:

```csharp
public sealed class EncoderMap
{
    public string ResolveVideoEncoder(TranscodeBackend backend, VideoCodec codec)
    {
        return (backend, codec) switch
        {
            (TranscodeBackend.Software, VideoCodec.H264) => "libx264",
            (TranscodeBackend.Software, VideoCodec.H265) => "libx265",
            (TranscodeBackend.Software, VideoCodec.Av1) => "libsvtav1",
            (TranscodeBackend.Vaapi, VideoCodec.H264) => "h264_vaapi",
            (TranscodeBackend.Vaapi, VideoCodec.H265) => "hevc_vaapi",
            (TranscodeBackend.Qsv, VideoCodec.H264) => "h264_qsv",
            (TranscodeBackend.Qsv, VideoCodec.H265) => "hevc_qsv",
            (TranscodeBackend.Nvenc, VideoCodec.H264) => "h264_nvenc",
            (TranscodeBackend.Nvenc, VideoCodec.H265) => "hevc_nvenc",
            (TranscodeBackend.Amf, VideoCodec.H264) => "h264_amf",
            (TranscodeBackend.Amf, VideoCodec.H265) => "hevc_amf",
            (TranscodeBackend.VideoToolbox, VideoCodec.H264) => "h264_videotoolbox",
            (TranscodeBackend.VideoToolbox, VideoCodec.H265) => "hevc_videotoolbox",
            (_, VideoCodec.Copy) => "copy",
            _ => throw new UnsupportedCapabilityException($"Unsupported encoder mapping: {backend} + {codec}")
        };
    }
}
```

Do not scatter encoder names across controllers or UI code.

---

## API Conventions

All plugin routes should live under:

```text
/TranscodedDownloads
```

### Routes

```text
GET    /TranscodedDownloads/Presets
GET    /TranscodedDownloads/Capabilities
POST   /TranscodedDownloads/Capabilities/Detect
GET    /TranscodedDownloads/UserPreferences
PUT    /TranscodedDownloads/UserPreferences
POST   /TranscodedDownloads/Jobs
GET    /TranscodedDownloads/Jobs
GET    /TranscodedDownloads/Jobs/{jobId}
DELETE /TranscodedDownloads/Jobs/{jobId}
GET    /TranscodedDownloads/Jobs/{jobId}/File
```

### Create job request

```csharp
public sealed class CreateDownloadJobRequest
{
    public Guid ItemId { get; set; }
    public string PresetId { get; set; } = string.Empty;
    public bool StartImmediately { get; set; } = true;
}
```

### Job DTO

```csharp
public sealed class DownloadJobDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid UserId { get; set; }
    public string PresetId { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public double ProgressPercent { get; set; }
    public string? OutputFileName { get; set; }
    public long? OutputSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
```

### Job statuses

```csharp
public enum JobStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
    Expired
}
```

---

## Permission Rules

Respect Jellyfin permissions.

A user must only download transcoded media when:

- the user is authenticated
- the user has access to the item
- the item belongs to a library visible to the user
- downloading is allowed by plugin settings
- the preset is enabled
- the preset is valid for this server

Admin-only endpoints:

```text
POST /TranscodedDownloads/Capabilities/Detect
PUT  /TranscodedDownloads/AdminConfiguration
```

User endpoints:

```text
GET  /TranscodedDownloads/Presets
GET  /TranscodedDownloads/UserPreferences
PUT  /TranscodedDownloads/UserPreferences
POST /TranscodedDownloads/Jobs
GET  /TranscodedDownloads/Jobs
GET  /TranscodedDownloads/Jobs/{jobId}
GET  /TranscodedDownloads/Jobs/{jobId}/File
```

A user must not be able to fetch another user's generated file unless they are an admin.

---

## Temporary File Rules

Generated transcodes are temporary by default.

The plugin must:

- store files outside the media library by default
- avoid modifying original media
- avoid writing beside source files
- use unique job IDs in file paths
- clean old files automatically
- expose retention settings to the admin
- enforce temp folder size limits

Recommended default path strategy:

```text
{JellyfinDataPath}/transcoded-downloads/{jobId}/output.{extension}
```

Do not store temporary downloads inside:

```text
/mnt/media
/media
/library
/movies
/tv
/music
```

unless the admin explicitly configures that path.

---

## File Naming Rules

Generated file names should be human-readable but safe.

Format:

```text
{CleanItemName} - {PresetName}.{extension}
```

Example:

```text
Blade Runner 2049 - 1080p H264 AAC MP4.mp4
```

Sanitize:

- slashes
- backslashes
- control characters
- colons on Windows
- reserved Windows names
- trailing dots
- trailing spaces

---

## UI Conventions

### Admin UI

The admin configuration page should include:

- global enable/disable
- temp folder path
- max concurrent jobs
- job retention hours
- max temp folder size
- capability profiles
- preset editor
- detect capabilities button
- preset validation warnings

### User UI

The user-facing UI should include:

- selected item title
- preset dropdown
- estimated output information when possible
- start download/transcode button
- queue status
- progress indicator
- download completed file button
- delete generated file button

### Jellyfin Web integration

Direct integration into the Jellyfin item menu may require JavaScript injection or a future Jellyfin Web contribution.

Treat web UI injection as optional and configurable:

```csharp
public bool EnableWebUiInjection { get; set; } = false;
```

Do not make the core plugin depend on injected JavaScript.

The backend API must work even if the web UI injection is disabled.

---

## Logging Conventions

Use structured logging.

Good:

```csharp
_logger.LogInformation("Created transcoded download job {JobId} for item {ItemId} using preset {PresetId}", jobId, itemId, presetId);
```

Bad:

```csharp
_logger.LogInformation("Created job " + jobId + " for " + itemId);
```

Log levels:

- `Trace`: detailed FFmpeg progress parsing
- `Debug`: command-building decisions, capability detection details
- `Information`: job created, job started, job completed, cleanup summary
- `Warning`: preset fallback, partial acceleration, temp storage nearing limit
- `Error`: failed transcode, file write failure, permission failure
- `Critical`: plugin cannot initialize safely

Never log:

- API keys
- auth tokens
- full private network details unless needed for debugging
- full media paths at Information level

Full media paths may be logged at Debug level only.

---

## Error Message Conventions

User-facing errors should explain what to do next.

Bad:

```text
FFmpeg failed with code 1.
```

Good:

```text
The transcode failed. This preset may not be supported by your server hardware or FFmpeg build. Try a CPU-compatible H.264 preset or ask the server admin to check the plugin logs.
```

Admin-facing logs may include:

- FFmpeg exit code
- stderr summary
- preset ID
- backend
- encoder
- output path

---

## Job Queue Rules

The plugin must use a bounded queue.

Required settings:

```csharp
public int MaxConcurrentJobs { get; set; } = 1;
public int MaxQueueSize { get; set; } = 10;
```

Rules:

- reject new jobs when queue is full
- allow users to cancel their own queued/running jobs
- allow admins to cancel any job
- clean partial files when a job fails or is cancelled
- do not start unlimited FFmpeg processes
- do not allow one user to starve the queue indefinitely

Future improvement:

- per-user queue limit
- priority for admins
- daily download limits

---

## Progress Tracking

Progress should be best-effort.

Preferred approach:

- parse FFmpeg progress output
- estimate percent from source duration
- report status even when exact percentage is unknown

Do not block file download forever waiting for perfect progress data.

Status should be reliable even if progress percentage is approximate.

---

## Stream Copy Rules

Stream copy is allowed only when compatible with the selected output container.

Example:

- H.264 video + AAC audio into MP4 may be copied.
- DTS audio into MP4 may require transcoding.
- Subtitle formats may not be compatible with all containers.

If stream copy is unsafe or incompatible, transcode instead.

The preset flag:

```csharp
public bool AllowStreamCopyWhenCompatible { get; set; } = true;
```

means:

```text
Copy only when valid. Never force invalid copy.
```

---

## Music Transcoding Rules

Music transcoding must be treated separately from video transcoding.

Audio-only jobs should not pass video arguments.

Music presets should define:

- audio codec
- bitrate or quality
- container
- album/track metadata behavior
- cover art behavior if supported

Recommended first-version behavior:

- support single track downloads first
- support album ZIP downloads later
- preserve basic metadata when possible
- avoid destructive metadata changes

---

## Subtitle Rules

Subtitle behavior must be explicit.

Allowed behaviors:

```text
None
Copy compatible subtitle streams
Burn selected subtitle stream
```

Do not burn subtitles by default.

Subtitle burn-in may force CPU-heavy processing, even when hardware video encoding is enabled.

If subtitle burn-in is selected, validate that the capability profile allows it.

---

## HDR / Tone-Mapping Rules

HDR to SDR tone-mapping must be explicit.

Default:

```csharp
public bool ToneMapHdrToSdr { get; set; } = true;
```

But the preset must warn if the selected backend may not fully support it.

Do not silently output washed-out SDR files from HDR sources.

If tone-mapping is disabled, the UI should show a warning for HDR sources:

```text
This source appears to be HDR. Disabling tone-mapping may create an output that looks incorrect on SDR screens.
```

---

## Security Rules

The plugin must treat media paths, output paths, and FFmpeg arguments as sensitive.

Required protections:

- never accept raw output paths from normal users
- never accept raw FFmpeg args from normal users
- validate preset ID against admin config
- validate item access through Jellyfin services
- sanitize output file names
- prevent path traversal
- prevent downloading files outside the job temp folder
- restrict job file access to owner or admin
- avoid shell execution
- use `ProcessStartInfo.ArgumentList`

Never do this:

```csharp
Process.Start("bash", $"-c \"ffmpeg {args}\"");
```

---

## Compatibility Rules

The plugin should compile against the targeted Jellyfin server version.

If Jellyfin server APIs change, update:

- package references
- plugin manifest
- controller attributes
- service injection patterns
- CI build matrix

Use a clear compatibility statement in `README.md`:

```text
Compatible with Jellyfin Server: 10.x.y
Tested with jellyfin-ffmpeg: x.y.z-Jellyfin
```

---

## Testing Rules

Unit tests must cover command generation.

Required tests:

- CPU H.264 MP4 command generation
- QSV H.264 MP4 command generation
- NVENC H.265 MP4 command generation
- audio-only MP3 command generation
- invalid codec rejected
- invalid container rejected
- missing capability profile rejected
- subtitle burn-in rejected when unsupported
- tone-mapping warning generated when partial support exists
- unsafe filename sanitized
- path traversal rejected

Integration tests may be added later.

Do not require a real GPU for normal unit tests.

Hardware-specific tests should be optional/manual.

---

## Build and Packaging Rules

The plugin should be buildable from command line.

Expected commands:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Package output should be easy to install manually into Jellyfin's plugin directory.

The repository should eventually support a Jellyfin plugin repository manifest so users can install/update through Jellyfin's plugin catalog workflow.

---

## README Requirements

`README.md` must eventually include:

- what the plugin does
- what it does not do
- screenshots or mockups
- Jellyfin version compatibility
- install instructions
- manual install instructions
- Docker notes
- hardware acceleration notes
- preset setup guide
- security warning about advanced FFmpeg args
- troubleshooting section
- known limitations

---

## First Milestone

Milestone 1 should not try to solve every UI problem.

Implement:

1. plugin loads in Jellyfin
2. admin config page exists
3. admin can define CPU-safe presets
4. API lists presets
5. API creates a transcode job
6. FFmpeg creates an output file
7. API downloads completed output file
8. cleanup deletes expired output files

Skip until later:

- direct item-menu integration
- album ZIP downloads
- hardware auto-detection perfection
- advanced queue UI
- remote transcoding
- arbitrary FFmpeg args

---

## Milestone 2

Implement:

1. capability detection
2. hardware preset validation
3. per-user default presets
4. progress tracking
5. cancel job
6. better error reporting

---

## Milestone 3

Implement:

1. optional Jellyfin Web button injection
2. music album ZIP download
3. subtitle selection
4. HDR warning UI
5. preset import/export
6. admin dashboard statistics

---

## Design Principle

When uncertain, choose the safer and more compatible option.

Prefer:

- explicit presets
- admin approval
- validation before execution
- conservative defaults
- CPU fallback
- clear warnings
- temporary outputs
- no media library modification

Avoid:

- hidden assumptions
- automatic destructive behavior
- raw command injection
- unbounded queues
- hardcoded GPU behavior
- UI-only features without backend APIs

---

## One-Sentence Summary

This plugin should behave like a controlled Jellyfin download transcoder: admin-defined capabilities and presets, user-friendly download choices, safe FFmpeg command generation, bounded jobs, temporary files, and no assumption that every server can encode the same way.
