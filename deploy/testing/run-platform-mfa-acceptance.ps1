$ErrorActionPreference = "Stop"
$composeFile = Join-Path $PSScriptRoot "compose.security-acceptance.yaml"
$projectName = "envantertr-platform-mfa-acceptance-$([Guid]::NewGuid().ToString('N').Substring(0,10))"
$exitCode = 1
try {
    & docker compose --project-name $projectName --file $composeFile run --build --rm acceptance --filter "FullyQualifiedName~Authenticator_two_factor_requires_second_step_and_recovery_codes_are_single_use|FullyQualifiedName~Platform_admin_first_login_accepts_password_then_requires_mfa_setup_before_panel"
    $exitCode = $LASTEXITCODE
} finally {
    & docker compose --project-name $projectName --file $composeFile down --volumes --remove-orphans
}
exit $exitCode
