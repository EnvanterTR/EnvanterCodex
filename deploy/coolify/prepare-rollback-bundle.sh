#!/bin/sh
# Run as the existing Docker host administrator. The private output contains
# application secrets; never print its contents or upload it as a public artifact.
set -eu
umask 077
taskResource=${1:?resource UUID required}
taskWeb=${2:?web container required}
taskPublic=${3:?public-site container required}
taskDatabase=${4:?database container required}
taskVerifiedDump=${5:?verified SQL dump path required}
case "$taskResource" in ''|*[!a-z0-9]*) exit 2;; esac
case "$taskWeb" in web-"$taskResource"-*) ;; *) exit 2;; esac
case "$taskPublic" in public-site-"$taskResource"-*) ;; *) exit 2;; esac
case "$taskDatabase" in database-"$taskResource"-*) ;; *) exit 2;; esac
case "$taskVerifiedDump" in /var/lib/postgresql/data/envantertr-backups/pre-web-v1-*.dump) ;; *) exit 2;; esac

taskMedia=$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/lib/envantertr/company-media"}}{{.Source}}{{end}}{{end}}' "$taskWeb")
taskKeys=$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/lib/envantertr/data-protection-keys"}}{{.Source}}{{end}}{{end}}' "$taskWeb")
test "$taskMedia" = "/var/lib/docker/volumes/${taskResource}_company-media-data/_data"
test "$taskKeys" = "/var/lib/docker/volumes/${taskResource}_data-protection-keys-data/_data"
test -d "$taskMedia"
test -d "$taskKeys"
taskWebImage=$(docker inspect --format '{{.Image}}' "$taskWeb")
taskPublicImage=$(docker inspect --format '{{.Image}}' "$taskPublic")

mkdir -p /var/backups/envantertr
chmod 700 /var/backups/envantertr
taskBundle="/var/backups/envantertr/rollback-$(date -u +%Y%m%dT%H%M%SZ)"
mkdir "$taskBundle"
docker exec "$taskDatabase" sha256sum -c "$taskVerifiedDump.sha256"
docker cp "$taskDatabase:$taskVerifiedDump" "$taskBundle/database.dump"
taskExpected=$(docker exec "$taskDatabase" sh -c 'cut -d " " -f 1 "$1.sha256"' sh "$taskVerifiedDump")
taskActual=$(sha256sum "$taskBundle/database.dump" | cut -d ' ' -f 1)
test "$taskExpected" = "$taskActual"
tar -C "$taskMedia" -czf "$taskBundle/company-media.tar.gz" .
tar -C "$taskKeys" -czf "$taskBundle/data-protection-keys.tar.gz" .
tar -tzf "$taskBundle/company-media.tar.gz" >/dev/null
tar -tzf "$taskBundle/data-protection-keys.tar.gz" >/dev/null
docker inspect --format '{{.Name}} {{.Image}} {{.Config.Image}}' "$taskWeb" "$taskPublic" "$taskDatabase" > "$taskBundle/images.txt"
docker inspect --format '{{json .Config.Env}}' "$taskWeb" > "$taskBundle/web-environment.private.json"
docker inspect --format '{{json .Config.Env}}' "$taskDatabase" > "$taskBundle/database-environment.private.json"
docker image save --output "$taskBundle/application-images.tar" "$taskWebImage" "$taskPublicImage"
gzip -1 "$taskBundle/application-images.tar"
sha256sum "$taskBundle/database.dump" "$taskBundle/company-media.tar.gz" \
    "$taskBundle/data-protection-keys.tar.gz" "$taskBundle/application-images.tar.gz" \
    "$taskBundle/images.txt" "$taskBundle/web-environment.private.json" \
    "$taskBundle/database-environment.private.json" > "$taskBundle/SHA256SUMS"
sha256sum -c "$taskBundle/SHA256SUMS"
printf 'ROLLBACK_BUNDLE_READY\n%s\n' "$taskBundle"
du -sh "$taskBundle"
