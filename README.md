# Jellyfin Transcoded Downloads

Download transcoded copies of Jellyfin movies, episodes, and music using administrator-defined presets.

This plugin adds a Jellyfin API and web configuration page for creating downloadable transcode jobs. Jobs are owned by the requesting Jellyfin user, require Jellyfin download permission, and are isolated so non-admin users can only list, inspect, cancel, and download their own jobs.

## Status

- Target Jellyfin ABI: `10.11.0.0`
- Target framework: `.NET 9`
- Plugin package version: `0.3.2.0`
- Plugin ID: `2dff9f1e-7a24-4c58-a1c8-74f4fd5312c8`

## Features

- Admin configuration page under Jellyfin plugin settings.
- Configurable capability profiles for CPU and hardware-oriented FFmpeg backends.
- Configurable video and audio presets.
- Default CPU H.264/AAC MP4 preset for first install.
- Transcode job API with queue limits and retained completed files.
- Jellyfin download-policy authorization.
- Per-user job ownership with admin override.
- Optional Jellyfin web item-page download button injection.

## Install

### Jellyfin Plugin Repository

Add this repository URL in Jellyfin:

```text
https://raw.githubusercontent.com/wivi514/jellyfin-plugin-transcoded-downloads/master/manifest.json
```

In Jellyfin, go to Dashboard -> Plugins -> Repositories, add the URL above, then open Catalog and install `Transcoded Downloads`.

The repository manifest points to the checked-in package for the advertised version:

```text
https://raw.githubusercontent.com/wivi514/jellyfin-plugin-transcoded-downloads/master/repository/Jellyfin.Plugin.TranscodedDownloads_0.3.2.0.zip
```

### Manual Install

Build the plugin package:

```bash
scripts/package-plugin.sh
```

The package is written to:

```text
dist/Jellyfin.Plugin.TranscodedDownloads_0.3.2.0.zip
```

For manual install, extract the package into a Jellyfin plugin folder, then restart Jellyfin. The plugin configuration page should appear in Dashboard -> Plugins as `Transcoded Downloads`.

## Configuration

The admin page manages:

- Video and music download enablement.
- Web UI button injection toggle.
- Maximum concurrent jobs and queue size.
- Job retention and temporary storage limits.
- Temporary output directory.
- Capability profiles.
- Transcode presets.

The default configuration includes:

- Capability profile: `CPU H.264`
- Preset: `1080p H.264 AAC MP4`
- Container: `MP4`
- Video codec: `H.264`
- Audio codec: `AAC`

## API

All plugin API endpoints are rooted at `/TranscodedDownloads` and require Jellyfin download permission.

```text
GET    /TranscodedDownloads/Presets
POST   /TranscodedDownloads/Jobs
GET    /TranscodedDownloads/Jobs
GET    /TranscodedDownloads/Jobs/{jobId}
DELETE /TranscodedDownloads/Jobs/{jobId}
GET    /TranscodedDownloads/Jobs/{jobId}/File
```

Example job request:

```json
{
  "itemId": "00000000-0000-0000-0000-000000000000",
  "presetId": "1080p-h264-aac-mp4",
  "startImmediately": true
}
```

Non-admin users only see and access their own jobs. Jellyfin administrators can access all jobs.

## FFmpeg

The transcode runner uses the `JELLYFIN_FFMPEG` environment variable when set. If it is not set, it falls back to `ffmpeg` on `PATH`.

## Development

Restore, build, and run tests:

```bash
dotnet test Jellyfin_Transcode_Download.sln
```

Run the real Jellyfin smoke test:

```bash
scripts/e2e-transcode-download.sh
```

The E2E script packages the plugin, starts Jellyfin `10.11.10` in Podman, completes first-run setup, configures the plugin through Jellyfin's plugin configuration API, creates a real transcode job, downloads the result, validates it with `ffprobe`, and checks anonymous and cross-user access restrictions.

Required tools for the smoke test:

- `curl`
- `ffmpeg`
- `ffprobe`
- `jq`
- `podman`
- `unzip`

## Packaging Manifest

`manifest.json` advertises the repository package and MD5 checksum. After changing plugin binaries or embedded resources, rebuild the package and update the checksum:

```bash
scripts/package-plugin.sh
```

The script writes the package to `dist/`, copies it into `repository/`, and prints the MD5 to copy into `manifest.json`.

Validate the local package against the Jellyfin repository manifest:

```bash
scripts/validate-plugin-repository.sh
```

To publish an installable repository update, commit the matching `manifest.json` and `repository/` package, then push `master`:

```bash
git push origin master
```

The optional GitHub Actions release workflow can also publish the same package to a versioned release when you push a version tag:

```bash
git tag v0.3.2.0
git push origin master v0.3.2.0
```
