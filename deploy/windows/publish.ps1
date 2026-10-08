param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\..\artifacts\windows\publish"),
    [string]$SdkContainerImage = "mcr.microsoft.com/dotnet/sdk:10.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$project = Join-Path $repoRoot "src\EnvanterTR.Web\EnvanterTR.Web.csproj"
$output = [System.IO.Path]::GetFullPath($OutputPath)
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
$docker = Get-Command docker -ErrorAction SilentlyContinue

if (-not $dotnet -and -not $docker) {
    throw ".NET 10 SDK or Docker Desktop with a running engine is required to build the Windows package."
}

New-Item -ItemType Directory -Path $output -Force | Out-Null
$publishArguments = @(
    "publish",
    $(if ($dotnet) { $project } else { "/source/src/EnvanterTR.Web/EnvanterTR.Web.csproj" }),
    "--configuration", "Release", "--runtime", "win-x64", "--self-contained", "true",
    "-p:PublishSingleFile=true", "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "--output", $(if ($dotnet) { $output } else { "/package" })
)
if ($dotnet) {
    & $dotnet.Source @publishArguments
} else {
    $dockerArguments = @(
        "run", "--rm",
        "--mount", "type=bind,source=$repoRoot,target=/source",
        "--mount", "type=bind,source=$output,target=/package",
        "--workdir", "/source", $SdkContainerImage, "dotnet"
    ) + $publishArguments
    & $docker.Source @dockerArguments
}
if ($LASTEXITCODE -ne 0) { throw "Windows x64 dotnet publish failed with exit code $LASTEXITCODE" }

$exe = Join-Path $output "EnvanterTR.Web.exe"
if (-not (Test-Path $exe)) { throw "Publish completed without expected executable: $exe" }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "install.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "migrate.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "disable-bootstrap.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "backup.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "BackupFormat.psm1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "restore.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "run-scheduled-backup.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "send-backup-alert.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "configure-backup-schedule.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "check-backup-health.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "update.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README.md") -Destination $output -Force
Write-Output "Windows x64 package ready: $output"
