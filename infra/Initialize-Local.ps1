$ErrorActionPreference = 'Stop'
$destination = Join-Path (Split-Path $PSScriptRoot -Parent) '.env'
if (Test-Path -LiteralPath $destination) {
    Write-Host '.env already exists; no values were changed.'
    exit 0
}
function New-LocalPassword {
    $bytes = New-Object byte[] 24
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return 'Aa1!' + [Convert]::ToBase64String($bytes)
}
@(
    "POSTGRES_PASSWORD=$(New-LocalPassword)"
    "DINDER_DATABASE_PASSWORD=$(New-LocalPassword)"
    "DINDER_CHEF_PASSWORD=$(New-LocalPassword)"
    "DINDER_MEMBER_PASSWORD=$(New-LocalPassword)"
) | Set-Content -LiteralPath $destination -Encoding utf8
Write-Host 'Created ignored .env with random local passwords. Read it locally for the Chef and Member sign-in credentials.'
