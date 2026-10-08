#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd -P)"
OUTPUT_DIR="${1:-$REPO_ROOT/artifacts/linux}"
OUTPUT_DIR="$(mkdir -p -- "$OUTPUT_DIR" && cd -- "$OUTPUT_DIR" && pwd -P)"

command -v git >/dev/null 2>&1 || { echo "git is required to package a source release." >&2; exit 1; }
command -v tar >/dev/null 2>&1 || { echo "tar is required to package a source release." >&2; exit 1; }
command -v gzip >/dev/null 2>&1 || { echo "gzip is required to package a source release." >&2; exit 1; }
command -v sha256sum >/dev/null 2>&1 || { echo "sha256sum is required to verify a source release." >&2; exit 1; }

commit="$(git -C "$REPO_ROOT" rev-parse --verify HEAD)"
short_commit="${commit:0:12}"
package_name="envantertr-linux-server-${short_commit}.tar.gz"
package_path="$OUTPUT_DIR/$package_name"
checksum_path="$package_path.sha256"
temporary_path="$package_path.tmp.$$"
checksum_temporary_path="$checksum_path.tmp.$$"
trap 'rm -f -- "$temporary_path" "$checksum_temporary_path"' EXIT

git -C "$REPO_ROOT" archive --format=tar --prefix="envantertr-linux-server-${short_commit}/" "$commit" \
  | gzip -n -9 > "$temporary_path"
test -s "$temporary_path"
mv -- "$temporary_path" "$package_path"
(cd -- "$OUTPUT_DIR" && sha256sum "$package_name") > "$checksum_temporary_path"
mv -- "$checksum_temporary_path" "$checksum_path"
(cd -- "$OUTPUT_DIR" && sha256sum --check --status "$(basename -- "$checksum_path")")

archive_prefix="envantertr-linux-server-${short_commit}"
archive_listing="$(tar -tzf "$package_path")"
for required_path in Dockerfile deploy/server/compose.yaml deploy/server/install.sh deploy/server/update.sh deploy/server/restore.sh; do
  if ! grep -Fqx -- "$archive_prefix/$required_path" <<< "$archive_listing"; then
    echo "Source package is missing required path: $required_path" >&2
    exit 1
  fi
done

echo "Source package: $package_path"
echo "SHA-256: $(cut -d ' ' -f 1 "$checksum_path")"
