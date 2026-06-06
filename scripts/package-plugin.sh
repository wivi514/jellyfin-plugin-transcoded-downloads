#!/usr/bin/env bash
set -euo pipefail

configuration="${CONFIGURATION:-Release}"
version="${VERSION:-0.3.0.0}"
project="Jellyfin.Plugin.TranscodedDownloads.csproj"
plugin_name="Jellyfin.Plugin.TranscodedDownloads"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
publish_dir="${repo_root}/bin/${configuration}/net9.0/publish"
dist_dir="${repo_root}/dist"
repository_dir="${repo_root}/repository"
package_path="${dist_dir}/${plugin_name}_${version}.zip"
repository_package_path="${repository_dir}/${plugin_name}_${version}.zip"
artifact_timestamp="${PACKAGE_TIMESTAMP:-2026-06-06 00:00:00 UTC}"
metadata_path="${publish_dir}/meta.json"

rm -rf "${publish_dir}"
dotnet publish "${repo_root}/${project}" -c "${configuration}" -o "${publish_dir}"

mkdir -p "${dist_dir}"
mkdir -p "${repository_dir}"
rm -f "${package_path}"
touch -d "${artifact_timestamp}" \
    "${publish_dir}/${plugin_name}.dll" \
    "${publish_dir}/${plugin_name}.pdb"

cat >"${metadata_path}" <<EOF
{
  "category": "General",
  "changelog": "Added popular transcode presets and a simple H.264 bitrate preset builder in the admin configuration UI.",
  "description": "Adds Jellyfin API and Web UI support for downloading transcoded copies of movies, episodes, and music items using administrator-defined presets.",
  "guid": "2dff9f1e-7a24-4c58-a1c8-74f4fd5312c8",
  "name": "Transcoded Downloads",
  "overview": "Download transcoded copies of Jellyfin media.",
  "owner": "wivi514",
  "targetAbi": "10.11.0.0",
  "timestamp": "2026-06-06T00:00:00Z",
  "version": "${version}"
}
EOF
touch -d "${artifact_timestamp}" "${metadata_path}"

(
    cd "${publish_dir}"
    zip -X -9 -q "${package_path}" \
        "meta.json" \
        "${plugin_name}.dll" \
        "${plugin_name}.pdb"
)

cp "${package_path}" "${repository_package_path}"

checksum="$(md5sum "${package_path}" | awk '{print toupper($1)}')"
printf 'Package: %s\n' "${package_path}"
printf 'Repository package: %s\n' "${repository_package_path}"
printf 'MD5: %s\n' "${checksum}"
