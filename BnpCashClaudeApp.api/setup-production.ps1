[CmdletBinding()]
param(
    [string]$PublishPath = $PSScriptRoot,
    [string]$DefaultConnection,
    [string]$JwtKey,
    [string]$EncryptionKey,
    [string]$DataExportSigningKey,
    [string]$IntegrityKey,
    [string]$AttachmentEncryptionKey,
    [string]$AttachmentDefenderPath,
    [int]$AttachmentVirusScanTimeoutSeconds = 120,
    [string]$SeedAdminPassword,
    [string]$SeedDatabasePassword,
    [switch]$NoPrompt,
    [switch]$SkipAclHardening,
    [switch]$RunAfterSetup
)

$ErrorActionPreference = "Stop"

function ConvertTo-PlainText {
    param([Security.SecureString]$SecureString)

    if ($null -eq $SecureString) {
        return ""
    }

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function Confirm-YesNo {
    param(
        [string]$Prompt,
        [bool]$DefaultYes = $true
    )

    if ($NoPrompt) {
        return $DefaultYes
    }

    $suffix = if ($DefaultYes) { "[Y/n]" } else { "[y/N]" }
    $answer = Read-Host "$Prompt $suffix"
    if ([string]::IsNullOrWhiteSpace($answer)) {
        return $DefaultYes
    }

    switch ($answer.Trim().ToLowerInvariant()) {
        "y" { return $true }
        "yes" { return $true }
        "n" { return $false }
        "no" { return $false }
        default { return $DefaultYes }
    }
}

function Ensure-Map {
    param(
        [System.Collections.IDictionary]$Parent,
        [string]$Key
    )

    if (-not $Parent.Contains($Key) -or
        $null -eq $Parent[$Key] -or
        -not ($Parent[$Key] -is [System.Collections.IDictionary])) {
        $Parent[$Key] = [ordered]@{}
    }

    return $Parent[$Key]
}

function Get-PathValue {
    param(
        [System.Collections.IDictionary]$Root,
        [string[]]$Path
    )

    $cursor = $Root
    foreach ($segment in $Path) {
        if ($null -eq $cursor -or
            -not ($cursor -is [System.Collections.IDictionary]) -or
            -not $cursor.Contains($segment)) {
            return $null
        }

        $cursor = $cursor[$segment]
    }

    return [string]$cursor
}

function Set-PathValue {
    param(
        [System.Collections.IDictionary]$Root,
        [string[]]$Path,
        [AllowNull()][string]$Value,
        [switch]$SkipIfEmpty
    )

    if ($SkipIfEmpty -and [string]::IsNullOrWhiteSpace($Value)) {
        return
    }

    $cursor = $Root
    for ($i = 0; $i -lt $Path.Length - 1; $i++) {
        $cursor = Ensure-Map -Parent $cursor -Key $Path[$i]
    }

    $cursor[$Path[$Path.Length - 1]] = $Value
}

function Read-Secret {
    param(
        [string]$Prompt,
        [int]$MinimumLength = 1
    )

    while ($true) {
        $secure = Read-Host -AsSecureString -Prompt $Prompt
        $plain = ConvertTo-PlainText -SecureString $secure

        if ([string]::IsNullOrWhiteSpace($plain)) {
            Write-Host "Value is required." -ForegroundColor Yellow
            continue
        }

        if ($plain.Length -lt $MinimumLength) {
            Write-Host "Minimum length is $MinimumLength." -ForegroundColor Yellow
            continue
        }

        return $plain
    }
}

function Resolve-RequiredValue {
    param(
        [string]$Name,
        [AllowNull()][string]$Provided,
        [AllowNull()][string]$Existing,
        [int]$MinimumLength = 1
    )

    if (-not [string]::IsNullOrWhiteSpace($Provided)) {
        if ($Provided.Length -lt $MinimumLength) {
            throw "$Name length must be at least $MinimumLength."
        }

        return $Provided
    }

    if (-not [string]::IsNullOrWhiteSpace($Existing) -and $Existing.Length -ge $MinimumLength) {
        if ($NoPrompt -or (Confirm-YesNo -Prompt "Keep existing value for $Name?" -DefaultYes $true)) {
            return $Existing
        }
    }

    if ($NoPrompt) {
        throw "Missing required value: $Name"
    }

    return Read-Secret -Prompt "Enter $Name" -MinimumLength $MinimumLength
}

function Resolve-OptionalValue {
    param(
        [string]$Name,
        [AllowNull()][string]$Provided,
        [AllowNull()][string]$Existing
    )

    if (-not [string]::IsNullOrWhiteSpace($Provided)) {
        return $Provided
    }

    if (-not [string]::IsNullOrWhiteSpace($Existing)) {
        if ($NoPrompt -or (Confirm-YesNo -Prompt "Keep existing value for $Name?" -DefaultYes $true)) {
            return $Existing
        }
    }

    if ($NoPrompt -or -not (Confirm-YesNo -Prompt "Set value for $Name?" -DefaultYes $false)) {
        return ""
    }

    $secure = Read-Host -AsSecureString -Prompt "Enter $Name (empty to skip)"
    return ConvertTo-PlainText -SecureString $secure
}

function Resolve-DefenderExecutablePath {
    param(
        [AllowNull()][string]$Provided,
        [AllowNull()][string]$Existing
    )

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($Provided)) {
        $candidates += $Provided
    }
    if (-not [string]::IsNullOrWhiteSpace($Existing)) {
        $candidates += $Existing
    }

    $candidates += @(
        "C:\Program Files\Windows Defender\MpCmdRun.exe",
        "C:\Program Files\Microsoft Defender\MpCmdRun.exe"
    )

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }

    if ($NoPrompt) {
        throw "Missing required value: Attachment:VirusScan:DefenderPath (Windows Defender executable not found)."
    }

    while ($true) {
        $manualPath = Read-Host "Enter full path of Windows Defender scanner (MpCmdRun.exe)"
        if (Test-Path -LiteralPath $manualPath -PathType Leaf) {
            return $manualPath
        }

        Write-Host "Invalid path. File not found: $manualPath" -ForegroundColor Yellow
    }
}

if (-not (Test-Path $PublishPath)) {
    throw "Publish path does not exist: $PublishPath"
}

$configPath = Join-Path $PublishPath "appsettings.Production.json"
$startScriptPath = Join-Path $PublishPath "start-production.bat"

$config = @{}
if (Test-Path $configPath) {
    try {
        $raw = Get-Content -Path $configPath -Raw
        if (-not [string]::IsNullOrWhiteSpace($raw)) {
            $parsed = ConvertFrom-Json -InputObject $raw -AsHashtable
            if ($parsed -is [System.Collections.IDictionary]) {
                $config = @{} + $parsed
            }
        }
    }
    catch {
        Write-Warning "Existing appsettings.Production.json is invalid JSON. Rebuilding it."
        $config = @{}
    }
}

$existingDefaultConnection = Get-PathValue -Root $config -Path @("ConnectionStrings", "DefaultConnection")
$existingJwtKey = Get-PathValue -Root $config -Path @("Jwt", "Key")
$existingEncryptionKey = Get-PathValue -Root $config -Path @("Encryption", "Key")
$existingDataExportSigningKey = Get-PathValue -Root $config -Path @("DataExport", "SigningKey")
$existingIntegrityKey = Get-PathValue -Root $config -Path @("Security", "IntegrityKey")
$existingAttachmentEncryptionKey = Get-PathValue -Root $config -Path @("Attachment", "EncryptionKey")
$existingAttachmentDefenderPath = Get-PathValue -Root $config -Path @("Attachment", "VirusScan", "DefenderPath")
$existingAttachmentVirusScanTimeoutSeconds = Get-PathValue -Root $config -Path @("Attachment", "VirusScan", "ProcessTimeoutSeconds")
$existingSeedAdminPassword = Get-PathValue -Root $config -Path @("SeedSecrets", "AdminPassword")
$existingSeedDatabasePassword = Get-PathValue -Root $config -Path @("SeedSecrets", "DatabasePassword")

$resolvedDefaultConnection = Resolve-RequiredValue -Name "ConnectionStrings:DefaultConnection" -Provided $DefaultConnection -Existing $existingDefaultConnection -MinimumLength 10
$resolvedJwtKey = Resolve-RequiredValue -Name "Jwt:Key" -Provided $JwtKey -Existing $existingJwtKey -MinimumLength 32
$resolvedEncryptionKey = Resolve-RequiredValue -Name "Encryption:Key" -Provided $EncryptionKey -Existing $existingEncryptionKey -MinimumLength 32
$resolvedDataExportSigningKey = Resolve-RequiredValue -Name "DataExport:SigningKey" -Provided $DataExportSigningKey -Existing $existingDataExportSigningKey -MinimumLength 32
$resolvedIntegrityKey = Resolve-RequiredValue -Name "Security:IntegrityKey" -Provided $IntegrityKey -Existing $existingIntegrityKey -MinimumLength 32

$resolvedAttachmentEncryptionKey = Resolve-OptionalValue -Name "Attachment:EncryptionKey" -Provided $AttachmentEncryptionKey -Existing $existingAttachmentEncryptionKey
$resolvedAttachmentDefenderPath = Resolve-DefenderExecutablePath -Provided $AttachmentDefenderPath -Existing $existingAttachmentDefenderPath

$resolvedAttachmentVirusScanTimeoutSeconds = $AttachmentVirusScanTimeoutSeconds
if ($resolvedAttachmentVirusScanTimeoutSeconds -le 0) {
    $parsedTimeout = 0
    if ([int]::TryParse([string]$existingAttachmentVirusScanTimeoutSeconds, [ref]$parsedTimeout) -and $parsedTimeout -gt 0) {
        $resolvedAttachmentVirusScanTimeoutSeconds = $parsedTimeout
    }
    else {
        $resolvedAttachmentVirusScanTimeoutSeconds = 120
    }
}
$resolvedAttachmentVirusScanTimeoutSeconds = [Math]::Max(5, [Math]::Min(1800, $resolvedAttachmentVirusScanTimeoutSeconds))

$resolvedSeedAdminPassword = Resolve-OptionalValue -Name "SeedSecrets:AdminPassword" -Provided $SeedAdminPassword -Existing $existingSeedAdminPassword
$resolvedSeedDatabasePassword = Resolve-OptionalValue -Name "SeedSecrets:DatabasePassword" -Provided $SeedDatabasePassword -Existing $existingSeedDatabasePassword

Set-PathValue -Root $config -Path @("ConnectionStrings", "DefaultConnection") -Value $resolvedDefaultConnection
Set-PathValue -Root $config -Path @("Jwt", "Key") -Value $resolvedJwtKey
Set-PathValue -Root $config -Path @("Jwt", "Issuer") -Value (Get-PathValue -Root $config -Path @("Jwt", "Issuer"))
if ([string]::IsNullOrWhiteSpace([string]$config["Jwt"]["Issuer"])) { $config["Jwt"]["Issuer"] = "BnpCashClaudeApp" }
Set-PathValue -Root $config -Path @("Jwt", "Audience") -Value (Get-PathValue -Root $config -Path @("Jwt", "Audience"))
if ([string]::IsNullOrWhiteSpace([string]$config["Jwt"]["Audience"])) { $config["Jwt"]["Audience"] = "BnpCashClaudeAppClient" }
if (-not $config["Jwt"].Contains("ExpiresMinutes")) { $config["Jwt"]["ExpiresMinutes"] = 15 }
if (-not $config["Jwt"].Contains("RefreshTokenExpiryDays")) { $config["Jwt"]["RefreshTokenExpiryDays"] = 7 }

Set-PathValue -Root $config -Path @("Encryption", "Key") -Value $resolvedEncryptionKey
Set-PathValue -Root $config -Path @("DataExport", "SigningKey") -Value $resolvedDataExportSigningKey
Set-PathValue -Root $config -Path @("Security", "IntegrityKey") -Value $resolvedIntegrityKey
Set-PathValue -Root $config -Path @("Attachment", "EncryptionKey") -Value $resolvedAttachmentEncryptionKey -SkipIfEmpty
Set-PathValue -Root $config -Path @("SeedSecrets", "AdminPassword") -Value $resolvedSeedAdminPassword -SkipIfEmpty
Set-PathValue -Root $config -Path @("SeedSecrets", "DatabasePassword") -Value $resolvedSeedDatabasePassword -SkipIfEmpty

$attachment = Ensure-Map -Parent $config -Key "Attachment"
$attachment["EnableVirusScan"] = $true
if (-not $attachment.Contains("EnableEncryption")) { $attachment["EnableEncryption"] = $false }

$virusScan = Ensure-Map -Parent $attachment -Key "VirusScan"
$virusScan["DefenderPath"] = $resolvedAttachmentDefenderPath
$virusScan["ProcessTimeoutSeconds"] = $resolvedAttachmentVirusScanTimeoutSeconds
$virusScan["EnforceInProduction"] = $true
$virusScan["RequireOperationalScannerInProduction"] = $true
$virusScan["RequireConfiguredDefenderPathInProduction"] = $true
$virusScan["AllowBuiltInFallbackInProduction"] = $false

$logging = Ensure-Map -Parent $config -Key "Logging"
$logLevel = Ensure-Map -Parent $logging -Key "LogLevel"
if (-not $logLevel.Contains("Default")) { $logLevel["Default"] = "Warning" }
if (-not $logLevel.Contains("Microsoft.AspNetCore")) { $logLevel["Microsoft.AspNetCore"] = "Warning" }
if (-not $logLevel.Contains("BnpCashClaudeApp")) { $logLevel["BnpCashClaudeApp"] = "Information" }

$failSecure = Ensure-Map -Parent $config -Key "FailSecure"
if (-not $failSecure.Contains("DenyAllInSecureMode")) { $failSecure["DenyAllInSecureMode"] = $true }

$json = $config | ConvertTo-Json -Depth 25
Set-Content -Path $configPath -Value $json -Encoding UTF8

$startScriptContent = @"
@echo off
setlocal
set ASPNETCORE_ENVIRONMENT=Production
if exist "%~dp0BnpCashClaudeApp.api.exe" (
  "%~dp0BnpCashClaudeApp.api.exe"
) else (
  dotnet "%~dp0BnpCashClaudeApp.api.dll"
)
endlocal
"@
Set-Content -Path $startScriptPath -Value $startScriptContent -Encoding ASCII

$shouldHardenAcl = -not $SkipAclHardening
if ($shouldHardenAcl -and (Confirm-YesNo -Prompt "Apply restricted ACL to appsettings.Production.json?" -DefaultYes $true)) {
    & icacls $configPath /inheritance:r /grant:r "Administrators:(F)" /grant:r "SYSTEM:(F)" /grant:r "Users:(R)" /grant:r "IIS_IUSRS:(R)" | Out-Null
}

Write-Host "[OK] Generated: $configPath" -ForegroundColor Green
Write-Host "[OK] Generated: $startScriptPath" -ForegroundColor Green
Write-Host "[OK] Attachment malware scan policy enforced for Production." -ForegroundColor Green

$shouldRun = $RunAfterSetup -or (Confirm-YesNo -Prompt "Run application now?" -DefaultYes $false)
if ($shouldRun) {
    & $startScriptPath
}

exit 0
