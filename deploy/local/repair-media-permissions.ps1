param([string]$Container = 'envantertr-product-dev-web-1')
$ErrorActionPreference = 'Stop'

# Repair only the named media volume's directory ownership. Preserve uploaded files.
$raw = & docker inspect $Container
if ($LASTEXITCODE -ne 0) { throw 'Web container could not be inspected.' }
$details = ($raw | ConvertFrom-Json)[0]
$mediaPath = '/var/lib/envantertr/company-media'
$mount = @($details.Mounts | Where-Object Destination -EQ $mediaPath)
if ($mount.Count -ne 1 -or $mount[0].Type -ne 'volume' -or !$mount[0].RW) {
    throw 'Expected one writable named media volume; no ownership change was made.'
}
$runtimeUid = (& docker exec $Container id -u).Trim()
$runtimeGid = (& docker exec $Container id -g).Trim()
if ($LASTEXITCODE -ne 0 -or $runtimeUid -notmatch '^\d+$' -or $runtimeGid -notmatch '^\d+$' -or $runtimeUid -eq '0') {
    throw 'A non-root application identity could not be verified.'
}
& docker exec --user 0 $Container sh -c 'set -eu; chown "$1:$2" "$3"; if [ -d "$3/accounts" ]; then chown "$1:$2" "$3/accounts"; fi' -- $runtimeUid $runtimeGid $mediaPath
if ($LASTEXITCODE -ne 0) { throw 'Media directory ownership repair failed.' }
& docker exec $Container sh -c 'set -eu; mkdir -p "$1/accounts"; proof=$(mktemp "$1/accounts/write-proof-XXXXXX"); trap ''rm -f -- "$proof"'' EXIT; printf permission-check > "$proof"; test "$(cat "$proof")" = permission-check; echo ACCOUNT_PHOTO_STORAGE_WRITE_PASS' -- $mediaPath
if ($LASTEXITCODE -ne 0) { throw 'Application identity cannot write to photo storage.' }
Write-Output 'Media directories match the non-root application user; existing uploads were preserved.'
