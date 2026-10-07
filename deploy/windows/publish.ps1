param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\..\artifacts\windows\publish")
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$project = Join-Path $repoRoot "src\EnvanterTR.Web\EnvanterTR.Web.csproj"
$output = [System.IO.Path]::GetFullPath($OutputPath)

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 10 SDK is required. Install the SDK on the build machine, then rerun this script."
}

New-Item -ItemType Directory -Path $output -Force | Out-Null
dotnet publish $project --configuration Release --runtime win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true --output $output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$exe = Join-Path $output "EnvanterTR.Web.exe"
if (-not (Test-Path $exe)) { throw "Publish completed without expected executable: $exe" }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "install.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "migrate.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "disable-bootstrap.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "backup.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "update.ps1") -Destination $output -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "README.md") -Destination $output -Force
Write-Output "Windows x64 package ready: $output"
