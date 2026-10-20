#!/bin/sh
# Restore only into a fresh private drill directory. Never mount this directory
# into a production container or print environment settings/key contents.
set -eu
umask 077
taskBundle=${1:?private rollback bundle path required}
case "$taskBundle" in /var/backups/envantertr/rollback-????????T??????Z) ;; *) exit 2;; esac
test -d "$taskBundle"
test ! -L "$taskBundle"
test "$(stat -c %a "$taskBundle")" = 700
sha256sum -c "$taskBundle/SHA256SUMS"
gzip -t "$taskBundle/application-images.tar.gz"

taskDrill="${taskBundle}-drill-$(date -u +%Y%m%dT%H%M%SZ)"
mkdir "$taskDrill"
mkdir "$taskDrill/company-media" "$taskDrill/data-protection-keys"
for taskArchive in company-media data-protection-keys; do
    # Bundles are created on this host from verified application mounts.
    # Refuse absolute or parent-relative members even for these local archives.
    if tar -tzf "$taskBundle/$taskArchive.tar.gz" | grep -Eq '(^/|(^|/)\.\.(/|$))'; then
        printf 'Unsafe archive path\n' >&2
        exit 2
    fi
    tar -xzf "$taskBundle/$taskArchive.tar.gz" -C "$taskDrill/$taskArchive"
    tar -dzf "$taskBundle/$taskArchive.tar.gz" -C "$taskDrill/$taskArchive"
done

# Load the exact previously running images. This does not start/replace a
# container, change routing, or apply migrations to any database.
docker image load --input "$taskBundle/application-images.tar.gz"
while read -r taskContainer taskImage taskReference; do
    case "$taskContainer" in /web-*|/public-site-*)
        docker image inspect --format '{{.Id}}' "$taskImage" | grep -Fx "$taskImage" >/dev/null
        ;;
    esac
done < "$taskBundle/images.txt"
printf 'ROLLBACK_FILES_AND_IMAGES_RESTORED\n%s\n' "$taskDrill"
