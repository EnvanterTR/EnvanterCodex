param(
    [Parameter(Mandatory = $true)][string]$PackagePath,
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\..\artifacts\windows\installer"),
    [string]$SdkContainerImage = "mcr.microsoft.com/dotnet/sdk:10.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$package = (Resolve-Path -LiteralPath $PackagePath).Path
$output = [System.IO.Path]::GetFullPath($OutputDirectory)
$directorySeparator = [System.IO.Path]::DirectorySeparatorChar
$repoPrefix = $repoRoot.TrimEnd('\','/') + $directorySeparator

if (-not (Test-Path -LiteralPath $package -PathType Container)) { throw "Windows publish directory does not exist: $package" }
if ($package -eq $output -or $output.StartsWith($package.TrimEnd('\','/') + $directorySeparator, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Installer output cannot be inside the published application package."
}
if (-not $output.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Installer output must be inside the repository so Docker can publish it safely."
}

$requiredFiles = @(
    "EnvanterTR.Web.exe", "Setup.cmd", "Setup-Personal.cmd", "setup.ps1", "install.ps1", "preflight.ps1",
    "ensure-localhost-certificate.ps1", "install-postgresql.ps1", "protect-connection-string.ps1", "configure-email.ps1",
    "prepare-postgresql.ps1", "migrate.ps1", "disable-bootstrap.ps1", "backup.ps1", "BackupFormat.psm1",
    "WindowsSecrets.psm1", "restore.ps1", "run-scheduled-backup.ps1", "send-backup-alert.ps1",
    "configure-backup-schedule.ps1", "check-backup-health.ps1", "update.ps1", "README.md"
)
foreach ($name in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $package $name) -PathType Leaf)) {
        throw "Windows installer package is missing required file: $name"
    }
}

$forbiddenFiles = Get-ChildItem -LiteralPath $package -File -Force -Recurse | Where-Object {
    $_.Name -match '^(\.env($|\.)|appsettings\.Production\.json$|server\.env$|backup\.pass$)' -or
    $_.Extension -eq ".dpapi"
}
if ($forbiddenFiles) { throw "Windows installer package contains a private configuration or secret file: $($forbiddenFiles[0].FullName)" }

New-Item -ItemType Directory -Path $output -Force | Out-Null
$output = (Resolve-Path -LiteralPath $output).Path
if (-not $output.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved installer output escapes the repository."
}
if ($package -eq $output -or $output.StartsWith($package.TrimEnd('\','/') + $directorySeparator, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Installer output cannot be inside the published application package."
}

$workRoot = Join-Path $output (".setup-build-" + [guid]::NewGuid().ToString("N"))
$workPrefix = $output.TrimEnd('\','/') + $directorySeparator
if (-not $workRoot.StartsWith($workPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Temporary installer build path escaped the output directory." }
$payloadZip = Join-Path $workRoot "EnvanterTR.SetupPayload.zip"
$publishOutput = Join-Path $workRoot "publish"
$intermediateOutput = (Join-Path $workRoot "obj") + $directorySeparator
$baseOutput = (Join-Path $workRoot "bin") + $directorySeparator
$project = Join-Path $PSScriptRoot "setup-bootstrapper\Setup.csproj"
$outputExe = Join-Path $publishOutput "Setup.exe"

try {
    New-Item -ItemType Directory -Path $publishOutput -Force | Out-Null
    Compress-Archive -Path (Join-Path $package "*") -DestinationPath $payloadZip -CompressionLevel Optimal
    if (-not (Test-Path -LiteralPath $payloadZip -PathType Leaf) -or (Get-Item -LiteralPath $payloadZip).Length -le 0) {
        throw "Installer payload archive was not created."
    }

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    $docker = Get-Command docker -ErrorAction SilentlyContinue
    if ($dotnet) {
        & $dotnet.Source publish $project --configuration Release --runtime win-x64 --self-contained true `
            "-p:SetupPayloadZipPath=$payloadZip" "-p:PublishSingleFile=true" "-p:IncludeNativeLibrariesForSelfExtract=true" `
            "-p:EnableCompressionInSingleFile=true" "-p:DebugType=None" "-p:DebugSymbols=false" `
            "-p:BaseIntermediateOutputPath=$intermediateOutput" "-p:BaseOutputPath=$baseOutput" --output $publishOutput
        if ($LASTEXITCODE -ne 0) { throw "Windows Setup.exe publish failed with exit code $LASTEXITCODE" }
    } elseif ($docker) {
        $dockerRepoRoot = $repoRoot.Replace('\','/')
        $dockerWork = $workRoot.Replace('\','/')
        $dockerArguments = @(
            "run", "--rm",
            "--mount", "type=bind,source=$dockerRepoRoot,target=/source,readonly",
            "--mount", "type=bind,source=$dockerWork,target=/installer-work",
            "--workdir", "/source", $SdkContainerImage, "dotnet", "publish", "/source/deploy/windows/setup-bootstrapper/Setup.csproj",
            "--configuration", "Release", "--runtime", "win-x64", "--self-contained", "true",
            "-p:SetupPayloadZipPath=/installer-work/EnvanterTR.SetupPayload.zip", "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true",
            "-p:EnableCompressionInSingleFile=true", "-p:DebugType=None", "-p:DebugSymbols=false",
            "-p:BaseIntermediateOutputPath=/installer-work/obj/", "-p:BaseOutputPath=/installer-work/bin/",
            "--output", "/installer-work/publish"
        )
        & $docker.Source @dockerArguments
        if ($LASTEXITCODE -ne 0) { throw "Windows Setup.exe Docker publish failed with exit code $LASTEXITCODE" }
    } else {
        throw ".NET 10 SDK or Docker Desktop with a running engine is required to build Setup.exe."
    }

    if (-not (Test-Path -LiteralPath $outputExe -PathType Leaf)) { throw "Publish completed without the expected single-file installer: $outputExe" }
    $extraFiles = Get-ChildItem -LiteralPath $publishOutput -File -Force | Where-Object { $_.Name -ne "Setup.exe" }
    if ($extraFiles) { throw "Setup.exe publish emitted loose files beside the single-file installer: $($extraFiles[0].Name)" }

    $verificationOutput = @(& $outputExe --verify 2>&1)
    if ($LASTEXITCODE -ne 0) {
        $verificationOutput | ForEach-Object { Write-Error $_ }
        throw "Embedded Windows installer payload verification failed with exit code $LASTEXITCODE."
    }
    $verificationOutput | ForEach-Object { Write-Output $_ }

    $finalExe = Join-Path $output "Setup.exe"
    $temporaryExe = Join-Path $output ("Setup.exe.tmp-" + [guid]::NewGuid().ToString("N"))
    $checksumPath = "$finalExe.sha256"
    $temporaryChecksum = "$checksumPath.tmp-$([guid]::NewGuid().ToString('N'))"
    try {
        Copy-Item -LiteralPath $outputExe -Destination $temporaryExe
        $hash = (Get-FileHash -LiteralPath $temporaryExe -Algorithm SHA256).Hash.ToLowerInvariant()
        [System.IO.File]::WriteAllText($temporaryChecksum, "$hash  Setup.exe`n", [System.Text.UTF8Encoding]::new($false))
        Move-Item -LiteralPath $temporaryExe -Destination $finalExe -Force
        Move-Item -LiteralPath $temporaryChecksum -Destination $checksumPath -Force
        if ((Get-FileHash -LiteralPath $finalExe -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash) {
            throw "Published Setup.exe did not match its computed SHA-256."
        }
        Write-Output "Windows single-file installer: $finalExe"
        Write-Output "SHA-256: $hash"
    } finally {
        Remove-Item -LiteralPath $temporaryExe,$temporaryChecksum -Force -ErrorAction SilentlyContinue
    }
} finally {
    if (Test-Path -LiteralPath $workRoot -PathType Container) {
        $resolvedWorkRoot = (Resolve-Path -LiteralPath $workRoot).Path
        if (-not $resolvedWorkRoot.StartsWith($workPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove a temporary build path outside installer output."
        }
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force
    }
}
