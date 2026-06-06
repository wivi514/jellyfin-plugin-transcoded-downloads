#!/usr/bin/env bash
set -euo pipefail

version="${VERSION:-0.2.0.0}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
package_path="${repo_root}/dist/Jellyfin.Plugin.TranscodedDownloads_${version}.zip"
repository_package_path="${repo_root}/repository/Jellyfin.Plugin.TranscodedDownloads_${version}.zip"
manifest_path="${repo_root}/manifest.json"

if ! command -v jq >/dev/null 2>&1; then
    printf 'jq is required to validate %s\n' "${manifest_path}" >&2
    exit 1
fi

if [ ! -f "${package_path}" ]; then
    printf 'Package does not exist: %s\n' "${package_path}" >&2
    printf 'Run scripts/package-plugin.sh first.\n' >&2
    exit 1
fi

if [ ! -f "${repository_package_path}" ]; then
    printf 'Repository package does not exist: %s\n' "${repository_package_path}" >&2
    printf 'Run scripts/package-plugin.sh first.\n' >&2
    exit 1
fi

manifest_version="$(jq -r '.[0].versions[0].version' "${manifest_path}")"
manifest_checksum="$(jq -r '.[0].versions[0].checksum' "${manifest_path}")"
manifest_source_url="$(jq -r '.[0].versions[0].sourceUrl' "${manifest_path}")"
expected_source_url="https://raw.githubusercontent.com/wivi514/jellyfin-plugin-transcoded-downloads/master/repository/Jellyfin.Plugin.TranscodedDownloads_${version}.zip"
actual_checksum="$(md5sum "${repository_package_path}" | awk '{print tolower($1)}')"
dist_checksum="$(md5sum "${package_path}" | awk '{print tolower($1)}')"

if [ "${manifest_version}" != "${version}" ]; then
    printf 'Manifest version mismatch: expected %s, found %s\n' "${version}" "${manifest_version}" >&2
    exit 1
fi

if [ "${manifest_source_url}" != "${expected_source_url}" ]; then
    printf 'Manifest sourceUrl mismatch:\nexpected: %s\nfound:    %s\n' "${expected_source_url}" "${manifest_source_url}" >&2
    exit 1
fi

if [ "${manifest_checksum}" != "${actual_checksum}" ]; then
    printf 'Manifest checksum mismatch:\nexpected: %s\nfound:    %s\n' "${actual_checksum}" "${manifest_checksum}" >&2
    exit 1
fi

if [ "${dist_checksum}" != "${actual_checksum}" ]; then
    printf 'Dist package and repository package checksums differ:\ndist:       %s\nrepository: %s\n' "${dist_checksum}" "${actual_checksum}" >&2
    exit 1
fi

printf 'Repository manifest is valid for %s.\n' "${repository_package_path}"
