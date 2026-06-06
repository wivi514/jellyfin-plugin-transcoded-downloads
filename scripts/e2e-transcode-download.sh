#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
container_name="${CONTAINER_NAME:-jtd-e2e}"
host_port="${HOST_PORT:-8099}"
base_url="http://127.0.0.1:${host_port}"
work_dir="${WORK_DIR:-/tmp/jtd-e2e}"
config_dir="${work_dir}/config"
cache_dir="${work_dir}/cache"
media_dir="${work_dir}/media"
download_dir="${work_dir}/download"
plugin_dir="${config_dir}/plugins/TranscodedDownloads_0.2.0.0"
package_path="${repo_root}/dist/Jellyfin.Plugin.TranscodedDownloads_0.2.0.0.zip"
password="JtdE2ePassword123"
username="root"
auth_header='Authorization: MediaBrowser Client="JtdE2E", Device="E2E", DeviceId="jtd-e2e", Version="1.0"'

require_command() {
    if ! command -v "$1" >/dev/null 2>&1; then
        printf 'Required command not found: %s\n' "$1" >&2
        exit 1
    fi
}

cleanup() {
    podman rm -f "${container_name}" >/dev/null 2>&1 || true
}

wait_for_setup_server() {
    local attempt
    for attempt in $(seq 1 90); do
        if curl -fsS "${base_url}/System/Info/Public" >/dev/null 2>&1 \
            && curl -fsS "${base_url}/Startup/Configuration" >/dev/null 2>&1; then
            return 0
        fi

        sleep 1
    done

    podman logs "${container_name}" >&2 || true
    printf 'Jellyfin did not become ready at %s\n' "${base_url}" >&2
    exit 1
}

wait_for_running_server() {
    local attempt
    for attempt in $(seq 1 90); do
        if curl -fsS "${base_url}/System/Info/Public" >/dev/null 2>&1 \
            && curl -fsS "${base_url}/TranscodedDownloads/Presets" >/dev/null 2>&1; then
            return 0
        fi

        sleep 1
    done

    podman logs "${container_name}" >&2 || true
    printf 'Jellyfin did not become ready at %s\n' "${base_url}" >&2
    exit 1
}

wait_for_startup_user() {
    local attempt
    for attempt in $(seq 1 60); do
        if startup_user="$(curl -fsS "${base_url}/Startup/User" 2>/dev/null)" \
            && [[ -n "$(jq -r '.Name // empty' <<<"${startup_user}")" ]]; then
            username="$(jq -r '.Name' <<<"${startup_user}")"
            return 0
        fi

        sleep 1
    done

    podman logs "${container_name}" >&2 || true
    printf 'Jellyfin startup user endpoint did not become ready.\n' >&2
    exit 1
}

start_server() {
    local mode="${1:-running}"
    cleanup
    podman run -d --name "${container_name}" --rm \
        -p "${host_port}:8096" \
        -v "${config_dir}:/config:Z" \
        -v "${cache_dir}:/cache:Z" \
        -v "${media_dir}:/media:Z" \
        -e JELLYFIN_PublishedServerUrl="${base_url}" \
        docker.io/jellyfin/jellyfin:10.11.10 >/dev/null
    if [[ "${mode}" == "setup" ]]; then
        wait_for_setup_server
    else
        wait_for_running_server
    fi
}

api_post() {
    local path="$1"
    local body="$2"
    curl -fsS -X POST "${base_url}${path}" \
        -H "X-Emby-Token: ${token}" \
        -H "Content-Type: application/json" \
        -d "${body}"
}

require_command curl
require_command ffmpeg
require_command ffprobe
require_command jq
require_command podman
require_command unzip

trap cleanup EXIT

"${repo_root}/scripts/package-plugin.sh" >/tmp/jtd-package.log
checksum="$(md5sum "${package_path}" | awk '{print toupper($1)}')"
manifest_checksum="$(jq -r '.[0].versions[0].checksum' "${repo_root}/manifest.json")"
if [[ "${checksum}" != "${manifest_checksum}" ]]; then
    printf 'Package checksum %s does not match manifest checksum %s\n' "${checksum}" "${manifest_checksum}" >&2
    exit 1
fi

rm -rf "${work_dir}"
mkdir -p "${plugin_dir}" "${cache_dir}" "${media_dir}" "${download_dir}"
unzip -q "${package_path}" -d "${plugin_dir}"

ffmpeg -hide_banner -y \
    -f lavfi -i testsrc=size=160x90:rate=15 \
    -f lavfi -i sine=frequency=1000:sample_rate=48000 \
    -t 2 \
    -c:v libx264 -pix_fmt yuv420p \
    -c:a aac \
    -shortest "${media_dir}/TestMovie.mp4" >/tmp/jtd-e2e-create-media.log 2>&1

start_server setup

curl -fsS -X POST "${base_url}/Startup/Configuration" \
    -H "Content-Type: application/json" \
    -d '{"UICulture":"en-US","MetadataCountryCode":"US","PreferredMetadataLanguage":"en","ServerName":"JTD E2E"}' >/dev/null

wait_for_startup_user
curl -fsS -X POST "${base_url}/Startup/User" \
    -H "Content-Type: application/json" \
    -d "{\"Name\":\"${username}\",\"Password\":\"${password}\"}" >/dev/null

curl -fsS -X POST "${base_url}/Startup/Complete" >/dev/null

auth_response="$(curl -fsS -X POST "${base_url}/Users/AuthenticateByName" \
    -H "Content-Type: application/json" \
    -H "${auth_header}" \
    -d "{\"Username\":\"${username}\",\"Pw\":\"${password}\"}")"
token="$(jq -r '.AccessToken' <<<"${auth_response}")"
user_id="$(jq -r '.User.Id' <<<"${auth_response}")"

api_post "/Library/VirtualFolders?name=E2E%20Movies&collectionType=movies&refreshLibrary=true" \
    '{"EnableRealtimeMonitor":false}' >/dev/null
api_post "/Library/VirtualFolders/Paths?refreshLibrary=true" \
    '{"Name":"E2E Movies","Path":"/media"}' >/dev/null

item_id=""
for _ in $(seq 1 60); do
    items="$(curl -fsS "${base_url}/Users/${user_id}/Items?Recursive=true&IncludeItemTypes=Movie&Fields=Path,MediaSources" \
        -H "X-Emby-Token: ${token}")"
    item_id="$(jq -r '.Items[0].Id // empty' <<<"${items}")"
    if [[ -n "${item_id}" ]]; then
        break
    fi

    sleep 2
done

if [[ -z "${item_id}" ]]; then
    podman logs "${container_name}" >&2 || true
    printf 'Jellyfin did not scan the generated test movie.\n' >&2
    exit 1
fi

cleanup

cat >"${config_dir}/plugins/configurations/Jellyfin.Plugin.TranscodedDownloads.xml" <<'XML'
<?xml version="1.0" encoding="utf-8"?>
<PluginConfiguration xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <EnableVideoDownloads>true</EnableVideoDownloads>
  <EnableMusicDownloads>true</EnableMusicDownloads>
  <EnableWebUiInjection>false</EnableWebUiInjection>
  <EnableAdvancedUnsafeFfmpegArguments>false</EnableAdvancedUnsafeFfmpegArguments>
  <MaxConcurrentJobs>1</MaxConcurrentJobs>
  <MaxQueueSize>10</MaxQueueSize>
  <JobRetentionHours>24</JobRetentionHours>
  <MaxTempFolderSizeMb>50000</MaxTempFolderSizeMb>
  <TempDirectory>/cache/transcoded-downloads</TempDirectory>
  <CapabilityProfiles>
    <CapabilityProfile>
      <Id>cpu-e2e</Id>
      <Name>CPU E2E</Name>
      <Backend>Software</Backend>
      <AllowedVideoCodecs>
        <VideoCodec>H264</VideoCodec>
      </AllowedVideoCodecs>
      <AllowedAudioCodecs>
        <AudioCodec>Aac</AudioCodec>
      </AllowedAudioCodecs>
      <AllowedContainers>
        <ContainerFormat>Mp4</ContainerFormat>
      </AllowedContainers>
      <SupportsHardwareDecode>false</SupportsHardwareDecode>
      <SupportsHardwareEncode>false</SupportsHardwareEncode>
      <SupportsToneMapping>false</SupportsToneMapping>
      <SupportsSubtitleBurnIn>true</SupportsSubtitleBurnIn>
      <SupportsTwoPass>false</SupportsTwoPass>
      <DevicePath />
      <Notes />
    </CapabilityProfile>
  </CapabilityProfiles>
  <Presets>
    <AdminTranscodePreset>
      <Id>e2e-h264-aac</Id>
      <Name>E2E H264 AAC MP4</Name>
      <Enabled>true</Enabled>
      <CapabilityProfileId>cpu-e2e</CapabilityProfileId>
      <Container>Mp4</Container>
      <VideoCodec>H264</VideoCodec>
      <AudioCodec>Aac</AudioCodec>
      <MaxWidth>160</MaxWidth>
      <MaxHeight>90</MaxHeight>
      <VideoBitrateKbps>256</VideoBitrateKbps>
      <AudioBitrateKbps>64</AudioBitrateKbps>
      <AudioChannels>1</AudioChannels>
      <AllowStreamCopyWhenCompatible>false</AllowStreamCopyWhenCompatible>
      <BurnSubtitles>false</BurnSubtitles>
      <ToneMapHdrToSdr>false</ToneMapHdrToSdr>
      <PreserveOriginalAudioIfCompatible>false</PreserveOriginalAudioIfCompatible>
      <IsVideoPreset>true</IsVideoPreset>
      <IsAudioOnlyPreset>false</IsAudioOnlyPreset>
    </AdminTranscodePreset>
  </Presets>
  <UserPreferences />
</PluginConfiguration>
XML

start_server running

presets="$(curl -fsS "${base_url}/TranscodedDownloads/Presets" -H "X-Emby-Token: ${token}")"
if ! jq -e 'any(.[]; (.id // .Id) == "e2e-h264-aac")' <<<"${presets}" >/dev/null; then
    printf 'Expected e2e preset was not available: %s\n' "${presets}" >&2
    exit 1
fi

job_response="$(api_post "/TranscodedDownloads/Jobs" \
    "{\"itemId\":\"${item_id}\",\"presetId\":\"e2e-h264-aac\",\"startImmediately\":true}")"
job_id="$(jq -r '.id // .Id' <<<"${job_response}")"

status=""
for _ in $(seq 1 60); do
    job="$(curl -fsS "${base_url}/TranscodedDownloads/Jobs/${job_id}" -H "X-Emby-Token: ${token}")"
    status="$(jq -r '.status // .Status' <<<"${job}")"
    if [[ "${status}" == "2" || "${status}" == "Completed" ]]; then
        break
    fi

    if [[ "${status}" == "3" || "${status}" == "Failed" ]]; then
        printf 'Transcode job failed: %s\n' "${job}" >&2
        podman logs "${container_name}" >&2 || true
        exit 1
    fi

    sleep 2
done

if [[ "${status}" != "2" && "${status}" != "Completed" ]]; then
    printf 'Transcode job did not complete. Last job: %s\n' "${job}" >&2
    podman logs "${container_name}" >&2 || true
    exit 1
fi

curl -fsS "${base_url}/TranscodedDownloads/Jobs/${job_id}/File" \
    -H "X-Emby-Token: ${token}" \
    -o "${download_dir}/transcoded.mp4"

if [[ ! -s "${download_dir}/transcoded.mp4" ]]; then
    printf 'Downloaded transcode file is empty.\n' >&2
    exit 1
fi

ffprobe -hide_banner -loglevel error \
    -select_streams v:0 \
    -show_entries stream=codec_name,width,height \
    -of json "${download_dir}/transcoded.mp4" >/tmp/jtd-e2e-ffprobe.json

codec="$(jq -r '.streams[0].codec_name' /tmp/jtd-e2e-ffprobe.json)"
width="$(jq -r '.streams[0].width' /tmp/jtd-e2e-ffprobe.json)"
height="$(jq -r '.streams[0].height' /tmp/jtd-e2e-ffprobe.json)"

if [[ "${codec}" != "h264" || "${width}" != "160" || "${height}" != "90" ]]; then
    printf 'Unexpected output stream: codec=%s width=%s height=%s\n' "${codec}" "${width}" "${height}" >&2
    exit 1
fi

printf 'E2E transcode download passed: item=%s job=%s output=%s\n' \
    "${item_id}" "${job_id}" "${download_dir}/transcoded.mp4"
