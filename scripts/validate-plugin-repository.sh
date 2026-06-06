#!/usr/bin/env bash
set -euo pipefail

version="${VERSION:-0.3.1.0}"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
package_path="${repo_root}/dist/Jellyfin.Plugin.TranscodedDownloads_${version}.zip"
repository_package_path="${repo_root}/repository/Jellyfin.Plugin.TranscodedDownloads_${version}.zip"
manifest_path="${repo_root}/manifest.json"
project_path="${repo_root}/Jellyfin.Plugin.TranscodedDownloads.csproj"
assembly_info_path="${repo_root}/Properties/AssemblyInfo.cs"

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
manifest_guid="$(jq -r '.[0].guid' "${manifest_path}")"
manifest_checksum="$(jq -r '.[0].versions[0].checksum' "${manifest_path}")"
manifest_source_url="$(jq -r '.[0].versions[0].sourceUrl' "${manifest_path}")"
expected_source_url="https://raw.githubusercontent.com/wivi514/jellyfin-plugin-transcoded-downloads/master/repository/Jellyfin.Plugin.TranscodedDownloads_${version}.zip"
actual_checksum="$(md5sum "${repository_package_path}" | awk '{print tolower($1)}')"
dist_checksum="$(md5sum "${package_path}" | awk '{print tolower($1)}')"
metadata="$(unzip -p "${repository_package_path}" meta.json)"
metadata_version="$(jq -r '.version' <<<"${metadata}")"
metadata_guid="$(jq -r '.guid' <<<"${metadata}")"
project_version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "${project_path}" | head -n 1)"
project_assembly_version="$(sed -n 's:.*<AssemblyVersion>\(.*\)</AssemblyVersion>.*:\1:p' "${project_path}" | head -n 1)"
project_file_version="$(sed -n 's:.*<FileVersion>\(.*\)</FileVersion>.*:\1:p' "${project_path}" | head -n 1)"

if [ "${project_version}" != "${version}" ]; then
    printf 'Project version mismatch: expected %s, found %s\n' "${version}" "${project_version}" >&2
    exit 1
fi

if [ "${project_assembly_version}" != "${version}" ]; then
    printf 'Project AssemblyVersion mismatch: expected %s, found %s\n' "${version}" "${project_assembly_version}" >&2
    exit 1
fi

if [ "${project_file_version}" != "${version}" ]; then
    printf 'Project FileVersion mismatch: expected %s, found %s\n' "${version}" "${project_file_version}" >&2
    exit 1
fi

if ! grep -Fq "[assembly: AssemblyVersion(\"${version}\")]" "${assembly_info_path}"; then
    printf 'AssemblyInfo.cs AssemblyVersion does not match %s.\n' "${version}" >&2
    exit 1
fi

if ! grep -Fq "[assembly: AssemblyFileVersion(\"${version}\")]" "${assembly_info_path}"; then
    printf 'AssemblyInfo.cs AssemblyFileVersion does not match %s.\n' "${version}" >&2
    exit 1
fi

if ! grep -Fq "[assembly: AssemblyInformationalVersion(\"${version}\")]" "${assembly_info_path}"; then
    printf 'AssemblyInfo.cs AssemblyInformationalVersion does not match %s.\n' "${version}" >&2
    exit 1
fi

if [ "${manifest_version}" != "${version}" ]; then
    printf 'Manifest version mismatch: expected %s, found %s\n' "${version}" "${manifest_version}" >&2
    exit 1
fi

if [ "${manifest_source_url}" != "${expected_source_url}" ]; then
    printf 'Manifest sourceUrl mismatch:\nexpected: %s\nfound:    %s\n' "${expected_source_url}" "${manifest_source_url}" >&2
    exit 1
fi

if [ "${metadata_version}" != "${manifest_version}" ]; then
    printf 'Package meta.json version mismatch: expected %s, found %s\n' "${manifest_version}" "${metadata_version}" >&2
    exit 1
fi

if [ "${metadata_guid}" != "${manifest_guid}" ]; then
    printf 'Package meta.json guid mismatch: expected %s, found %s\n' "${manifest_guid}" "${metadata_guid}" >&2
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
