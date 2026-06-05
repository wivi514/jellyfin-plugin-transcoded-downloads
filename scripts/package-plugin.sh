#!/usr/bin/env bash
set -euo pipefail

configuration="${CONFIGURATION:-Release}"
version="${VERSION:-0.1.0.0}"
project="Jellyfin.Plugin.TranscodedDownloads.csproj"
plugin_name="Jellyfin.Plugin.TranscodedDownloads"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
publish_dir="${repo_root}/bin/${configuration}/net6.0/publish"
dist_dir="${repo_root}/dist"
package_path="${dist_dir}/${plugin_name}_${version}.zip"

rm -rf "${publish_dir}"
dotnet publish "${repo_root}/${project}" -c "${configuration}" -o "${publish_dir}"

mkdir -p "${dist_dir}"
rm -f "${package_path}"

(
    cd "${publish_dir}"
    zip -9 -q "${package_path}" \
        "${plugin_name}.dll" \
        "${plugin_name}.pdb" \
        "Microsoft.Extensions.Logging.Abstractions.dll"
)

checksum="$(md5sum "${package_path}" | awk '{print toupper($1)}')"
printf 'Package: %s\n' "${package_path}"
printf 'MD5: %s\n' "${checksum}"
