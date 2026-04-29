[CmdletBinding()]
param(
    [string]$PublishDirectory = "backend/artifacts/demo-publish",
    [string]$BaseUrl = "http://127.0.0.1:5099",
    [string]$EnvironmentName = "Development",
    [switch]$DisableMultitenancy = $true,
    [switch]$DisableHttpsRedirect = $true,
    [int]$StartupTimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"

function Get-ExpectedPageMetadata {
    return @(
        @{ Path = "/";            Title = "Conclave"; BaseHref = "/" },
        @{ Path = "/program";     Title = "Conclave"; BaseHref = "/" },
        @{ Path = "/admin/";      Title = "Admin";    BaseHref = "/admin/" },
        @{ Path = "/admin/login"; Title = "Admin";    BaseHref = "/admin/" },
        @{ Path = "/portal/";     Title = "Portal";   BaseHref = "/portal/" },
        @{ Path = "/portal/login";Title = "Portal";   BaseHref = "/portal/" },
        @{ Path = "/reception/";  Title = "Conclave Receptionen"; BaseHref = "/reception/" },
        @{ Path = "/reception/checkin"; Title = "Conclave Receptionen"; BaseHref = "/reception/" }
    )
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)]$Expected,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Actual -ne $Expected) {
        throw "$Message Expected '$Expected' but got '$Actual'."
    }
}

function Get-PageMetadata {
    param(
        [Parameter(Mandatory = $true)][string]$Url
    )

    $response = Invoke-WebRequest -UseBasicParsing $Url
    $title = [regex]::Match($response.Content, "<title>(.*?)</title>").Groups[1].Value
    $baseHref = [regex]::Match($response.Content, '<base href="([^"]+)"').Groups[1].Value
    $scriptSrc = [regex]::Match($response.Content, '<script src="([^"]+)"').Groups[1].Value

    return [PSCustomObject]@{
        StatusCode = [int]$response.StatusCode
        Title = $title
        BaseHref = $baseHref
        ScriptSrc = $scriptSrc
    }
}

function Wait-ForUrl {
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

    throw "Timed out waiting for $Url to respond successfully."
}

$publishPath = (Resolve-Path $PublishDirectory).Path
$executablePath = Join-Path $publishPath "ConventionSystem.Api.exe"
$dllPath = Join-Path $publishPath "ConventionSystem.Api.dll"

$commandPath = $null
$commandArguments = @()

if (Test-Path $executablePath) {
    $commandPath = $executablePath
}
elseif (Test-Path $dllPath) {
    $commandPath = "dotnet"
    $commandArguments = @($dllPath)
}
else {
    throw "Could not find published API executable or DLL in '$publishPath'."
}

$stdoutLog = Join-Path $publishPath "smoke-stdout.log"
$stderrLog = Join-Path $publishPath "smoke-stderr.log"

foreach ($logFile in @($stdoutLog, $stderrLog)) {
    if (Test-Path $logFile) {
        Remove-Item -LiteralPath $logFile -Force
    }
}

$previousEnvironment = @{
    ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
    ASPNETCORE_URLS = $env:ASPNETCORE_URLS
    UseHttpsRedirect = $env:UseHttpsRedirect
    Multitenancy__Enabled = $env:Multitenancy__Enabled
}

$process = $null

try {
    $env:ASPNETCORE_ENVIRONMENT = $EnvironmentName
    $env:ASPNETCORE_URLS = $BaseUrl

    if ($DisableHttpsRedirect) {
        $env:UseHttpsRedirect = "false"
    }

    if ($DisableMultitenancy) {
        $env:Multitenancy__Enabled = "false"
    }

    $startProcessParams = @{
        FilePath = $commandPath
        WorkingDirectory = $publishPath
        PassThru = $true
        RedirectStandardOutput = $stdoutLog
        RedirectStandardError = $stderrLog
    }

    if ($commandArguments.Count -gt 0) {
        $startProcessParams.ArgumentList = $commandArguments
    }

    $process = Start-Process @startProcessParams

    Wait-ForUrl -Url "$BaseUrl/" -TimeoutSeconds $StartupTimeoutSeconds

    foreach ($page in Get-ExpectedPageMetadata) {
        $metadata = Get-PageMetadata -Url ($BaseUrl.TrimEnd("/") + $page.Path)
        Assert-Equal -Actual $metadata.StatusCode -Expected 200 -Message "Unexpected status code for '$($page.Path)'."
        Assert-Equal -Actual $metadata.Title -Expected $page.Title -Message "Unexpected page title for '$($page.Path)'."
        Assert-Equal -Actual $metadata.BaseHref -Expected $page.BaseHref -Message "Unexpected base href for '$($page.Path)'."
    }

    $adminMetadata = Get-PageMetadata -Url ($BaseUrl.TrimEnd("/") + "/admin/")
    $scriptUrl = if ($adminMetadata.ScriptSrc.StartsWith("/")) {
        $BaseUrl.TrimEnd("/") + $adminMetadata.ScriptSrc
    }
    else {
        $BaseUrl.TrimEnd("/") + "/admin/" + $adminMetadata.ScriptSrc
    }

    $assetResponse = Invoke-WebRequest -UseBasicParsing $scriptUrl
    Assert-Equal -Actual ([int]$assetResponse.StatusCode) -Expected 200 -Message "Admin asset smoke check failed."

    try {
        $apiResponse = Invoke-WebRequest -UseBasicParsing ($BaseUrl.TrimEnd("/") + "/system/tenants")
        Assert-Equal -Actual ([int]$apiResponse.StatusCode) -Expected 401 -Message "Expected system API route to stay protected."
    }
    catch {
        $response = $_.Exception.Response
        if ($null -eq $response) {
            throw
        }

        Assert-Equal -Actual ([int]$response.StatusCode) -Expected 401 -Message "Expected system API route to stay protected."
    }

    Write-Host "Demo artifact smoke passed for $BaseUrl"
}
finally {
    if ($process -and -not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
    }

    foreach ($entry in $previousEnvironment.GetEnumerator()) {
        if ($null -eq $entry.Value) {
            Remove-Item "Env:$($entry.Key)" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item "Env:$($entry.Key)" $entry.Value
        }
    }
}
