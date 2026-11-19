param([Parameter(Mandatory = $true)][string]$Thumbprint)
$ErrorActionPreference = 'Stop'
# Export only a public certificate that the workstation already trusts. Never export a private key.
$certificate = Get-Item -LiteralPath "Cert:\LocalMachine\Root\$Thumbprint"
$workspace = Split-Path $PSScriptRoot -Parent
$toolsDirectory = Join-Path $workspace '.tools'
New-Item -ItemType Directory -Force -Path $toolsDirectory | Out-Null
[System.IO.File]::WriteAllText((Join-Path $toolsDirectory 'build-ca.crt'), $certificate.ExportCertificatePem())
@'
services:
  api:
    build:
      secrets:
        - build_ca
  web:
    build:
      secrets:
        - build_ca
secrets:
  build_ca:
    file: ./.tools/build-ca.crt
'@ | Set-Content -LiteralPath (Join-Path $toolsDirectory 'compose.trust.yaml') -Encoding utf8
Write-Host 'Created ignored build-only certificate and Compose override. Workstation and runtime trust are unchanged.'
