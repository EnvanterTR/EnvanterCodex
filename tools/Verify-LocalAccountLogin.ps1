param([switch]$PlatformAdminOnly)

$ErrorActionPreference = 'Stop'

# One attempt only. A rejected password increments the local account's failed-login count.
# This script is intentionally bound to localhost and never prints or persists the password.
$baseUrl = 'http://localhost:8080'
$email = 'admin@envantertr.com'
$secret = Read-Host 'Enter the authorized local account password (hidden)' -AsSecureString
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
$plainPassword = $null
$requestBody = $null

try {
    $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    $webSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new()

    $csrfResponse = Invoke-WebRequest -Uri "$baseUrl/api/session/csrf" -WebSession $webSession -UseBasicParsing -TimeoutSec 10
    $csrfToken = ($csrfResponse.Content | ConvertFrom-Json).requestToken
    $requestBody = @{ email = $email; password = $plainPassword; rememberMe = $false; platformAdminOnly = [bool]$PlatformAdminOnly } | ConvertTo-Json -Compress

    try {
        $loginResponse = Invoke-WebRequest -Uri "$baseUrl/api/session/login" -Method Post -WebSession $webSession `
            -UseBasicParsing -TimeoutSec 10 -Headers @{ 'X-CSRF-TOKEN' = $csrfToken } `
            -ContentType 'application/json' -Body $requestBody -ErrorAction Stop
    }
    catch {
        $statusCode = 0
        $errorBody = $null
        $response = $_.Exception.Response
        if ($response) {
            try { $statusCode = [int]$response.StatusCode } catch { }
            try {
                if ($response -is [System.Net.HttpWebResponse]) {
                    $reader = [System.IO.StreamReader]::new($response.GetResponseStream())
                    try { $errorBody = $reader.ReadToEnd() } finally { $reader.Dispose() }
                }
                elseif ($response.Content) {
                    $errorBody = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                }
            } catch { }
        }
        Write-Output "LOGIN_FAILED_HTTP=$statusCode"
        try {
            $errorData = $errorBody | ConvertFrom-Json
            if ($errorData.code) { Write-Output "LOGIN_CODE=$($errorData.code)" }
            if ($errorData.twoFactorRequired -eq $true) { Write-Output 'TWO_FACTOR_REQUIRED=true' }
            if ($errorData.message) { Write-Output "LOGIN_MESSAGE=$($errorData.message)" }
        } catch { }
        exit 1
    }

    if ($loginResponse.StatusCode -ne 200) {
        $errorBody = $null
        try { $errorBody = $loginResponse.Content | ConvertFrom-Json } catch { }
        Write-Output "LOGIN_FAILED_HTTP=$($loginResponse.StatusCode)"
        if ($errorBody.code) { Write-Output "LOGIN_CODE=$($errorBody.code)" }
        if ($errorBody.twoFactorRequired -eq $true) { Write-Output 'TWO_FACTOR_REQUIRED=true' }
        if ($errorBody.message) { Write-Output "LOGIN_MESSAGE=$($errorBody.message)" }
        exit 1
    }

    $loginBody = $loginResponse.Content | ConvertFrom-Json
    if ($loginBody.authenticated -ne $true) {
        Write-Output 'LOGIN_FAILED: unexpected success response.'
        exit 1
    }

    $contextLabel = if ($PlatformAdminOnly) { 'platform-admin' } else { 'company' }
    if ($PlatformAdminOnly -and $loginBody.platformAdmin -ne $true) {
        Write-Output 'LOGIN_FAILED: platform administrator claim missing.'
        exit 1
    }
    Write-Output "LOGIN_OK context=$contextLabel role=$($loginBody.role) company=$($loginBody.companyId)"
    $logoutCsrf = Invoke-WebRequest -Uri "$baseUrl/api/session/csrf" -WebSession $webSession -UseBasicParsing -TimeoutSec 10
    $logoutToken = ($logoutCsrf.Content | ConvertFrom-Json).requestToken
    $logoutResponse = Invoke-WebRequest -Uri "$baseUrl/api/session/logout" -Method Post -WebSession $webSession `
        -UseBasicParsing -TimeoutSec 10 -Headers @{ 'X-CSRF-TOKEN' = $logoutToken } `
        -ContentType 'application/json' -Body '{}'
    if ($logoutResponse.StatusCode -ne 204) {
        Write-Output "LOGOUT_FAILED_HTTP=$($logoutResponse.StatusCode)"
        exit 2
    }

    Write-Output 'LOGOUT_OK'
}
finally {
    if ($passwordPointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    }
    $plainPassword = $null
    $requestBody = $null
    $secret = $null
}
