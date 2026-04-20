[CmdletBinding()]
param(
    [string]$PublishDirectory = "backend/artifacts/demo-publish",
    [string]$BaseUrl = "http://localhost:5099",
    [string]$ConnectionString = "Server=.;Database=ConventionSystemDemo;Trusted_Connection=True;TrustServerCertificate=True;",
    [string]$JwtKey = "replace-with-a-real-demo-secret-at-least-32-chars",
    [string]$JwtIssuer = "ConventionSystem",
    [string]$JwtAudience = "ConventionSystem",
    [string]$EmailProvider = "Logging",
    [switch]$ForceRestart
)

$ErrorActionPreference = "Stop"

function Test-TcpPortInUse {
    param(
        [Parameter(Mandatory = $true)][string]$HostName,
        [Parameter(Mandatory = $true)][int]$Port
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $asyncResult = $client.BeginConnect($HostName, $Port, $null, $null)
        $connected = $asyncResult.AsyncWaitHandle.WaitOne(500)
        if (-not $connected) {
            return $false
        }

        $client.EndConnect($asyncResult)
        return $true
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Get-ListeningProcessIds {
    param(
        [Parameter(Mandatory = $true)][int]$Port
    )

    try {
        return @(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop |
            Select-Object -ExpandProperty OwningProcess -Unique)
    }
    catch {
        return @()
    }
}

function Wait-ForDemoReady {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -UseBasicParsing $Url
            if ($response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    } while ((Get-Date) -lt $deadline)

    throw "Demo instance did not become ready at '$Url' within $TimeoutSeconds seconds."
}

$publishPath = (Resolve-Path $PublishDirectory).Path
$exePath = Join-Path $publishPath "ConventionSystem.Api.exe"
$dllPath = Join-Path $publishPath "ConventionSystem.Api.dll"

if (Test-Path $exePath) {
    $commandPath = $exePath
    $commandArguments = @()
}
elseif (Test-Path $dllPath) {
    $commandPath = "dotnet"
    $commandArguments = @($dllPath)
}
else {
    throw "Could not find published API executable or DLL in '$publishPath'. Run dotnet publish first."
}

$baseUrlTrimmed = $BaseUrl.TrimEnd('/')
$baseUri = [Uri]$BaseUrl

if (Test-TcpPortInUse -HostName $baseUri.Host -Port $baseUri.Port) {
    if (-not $ForceRestart) {
        throw "Port $($baseUri.Port) on host '$($baseUri.Host)' is already in use. Stop the existing process, use -ForceRestart, or run with -BaseUrl on a different port."
    }

    $owningProcessIds = Get-ListeningProcessIds -Port $baseUri.Port
    if ($owningProcessIds.Count -eq 0) {
        throw "Port $($baseUri.Port) is already in use, but the owning process could not be resolved."
    }

    foreach ($processId in $owningProcessIds) {
        $existingProcess = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($null -eq $existingProcess) {
            continue
        }

        Write-Host "Stopping existing process on port $($baseUri.Port): $($existingProcess.ProcessName) ($processId)"
        Stop-Process -Id $processId -Force
    }

    Start-Sleep -Seconds 1
}

$previousEnvironment = @{
    ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
    ASPNETCORE_URLS = $env:ASPNETCORE_URLS
    ConnectionStrings__DefaultConnection = $env:ConnectionStrings__DefaultConnection
    Jwt__Key = $env:Jwt__Key
    Jwt__Issuer = $env:Jwt__Issuer
    Jwt__Audience = $env:Jwt__Audience
    App__FrontendUrl = $env:App__FrontendUrl
    App__AdminUrlTemplate = $env:App__AdminUrlTemplate
    App__PortalUrl = $env:App__PortalUrl
    Email__Provider = $env:Email__Provider
    UseHttpsRedirect = $env:UseHttpsRedirect
}

try {
    $env:ASPNETCORE_ENVIRONMENT = "Demo"
    $env:ASPNETCORE_URLS = $BaseUrl
    $env:ConnectionStrings__DefaultConnection = $ConnectionString
    $env:Jwt__Key = $JwtKey
    $env:Jwt__Issuer = $JwtIssuer
    $env:Jwt__Audience = $JwtAudience
    $env:App__FrontendUrl = $baseUrlTrimmed
    $env:App__AdminUrlTemplate = "$baseUrlTrimmed/admin"
    $env:App__PortalUrl = "$baseUrlTrimmed/portal"
    $env:Email__Provider = $EmailProvider
    $env:UseHttpsRedirect = "false"

    Write-Host "Starting local demo artifact on $BaseUrl"
    Write-Host "Public : $baseUrlTrimmed/"
    Write-Host "Admin  : $baseUrlTrimmed/admin/"
    Write-Host "Portal : $baseUrlTrimmed/portal/"

    $outputLog = Join-Path $publishPath "run-demo-stdout.log"
    $errorLog = Join-Path $publishPath "run-demo-stderr.log"

    foreach ($logFile in @($outputLog, $errorLog)) {
        if (Test-Path $logFile) {
            Remove-Item -LiteralPath $logFile -Force
        }
    }

    $startProcessParams = @{
        FilePath = $commandPath
        WorkingDirectory = $publishPath
        PassThru = $true
        RedirectStandardOutput = $outputLog
        RedirectStandardError = $errorLog
    }

    if ($commandArguments.Count -gt 0) {
        $startProcessParams.ArgumentList = $commandArguments
    }

    $process = Start-Process @startProcessParams

    try {
        Wait-ForDemoReady -Url "$baseUrlTrimmed/" -TimeoutSeconds 30
        Write-Host "Demo ready at $baseUrlTrimmed/"
        Write-Host "Press Ctrl+C to stop tailing logs and terminate the local demo instance."

        Get-Content -LiteralPath $outputLog -Wait
    }
    finally {
        if ($process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }
    }
}
finally {
    foreach ($entry in $previousEnvironment.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            Remove-Item "Env:$($entry.Key)" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item "Env:$($entry.Key)" $entry.Value
        }
    }
}
