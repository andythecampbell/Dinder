param([string]$BaseUrl = 'http://localhost:8080', [switch]$RestartApi)
$ErrorActionPreference = 'Stop'
$settings = @{}
Get-Content -LiteralPath (Join-Path (Split-Path $PSScriptRoot -Parent) '.env') | ForEach-Object {
    if ($_ -match '^([^#=]+)=(.*)$') { $settings[$matches[1]] = $matches[2] }
}
function Assert-Status($response, [int]$expected, [string]$description) {
    if ([int]$response.StatusCode -ne $expected) { throw "$description expected $expected; got $($response.StatusCode)." }
    Write-Host "PASS: $description"
}
function Get-Token($session) {
    return (Invoke-RestMethod "$BaseUrl/api/auth/csrf" -WebSession $session).token
}
function Sign-In([string]$email, [string]$password) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $body = @{ email = $email; password = $password } | ConvertTo-Json
    $response = Invoke-WebRequest "$BaseUrl/api/auth/login" -Method Post -WebSession $session -Headers @{ 'X-CSRF-TOKEN' = (Get-Token $session) } -ContentType 'application/json' -Body $body -SkipHttpErrorCheck
    Assert-Status $response 204 "Sign in $email"
    return $session
}
Assert-Status (Invoke-WebRequest "$BaseUrl/health/ready") 200 'PostgreSQL readiness'
Assert-Status (Invoke-WebRequest "$BaseUrl/api/auth/me" -SkipHttpErrorCheck) 401 'Anonymous session rejected'
$chef = Sign-In 'chef@dinder.local' $settings.DINDER_CHEF_PASSWORD
$member = Sign-In 'member@dinder.local' $settings.DINDER_MEMBER_PASSWORD
$household = (Invoke-RestMethod "$BaseUrl/api/auth/me" -WebSession $chef).households[0].id
$root = "$BaseUrl/api/households/$household"
$kitchen = Invoke-RestMethod "$root/kitchen" -WebSession $chef
Assert-Status (Invoke-WebRequest "$root/rounds" -Method Post -WebSession $member -Headers @{ 'X-CSRF-TOKEN' = (Get-Token $member) } -SkipHttpErrorCheck) 403 'Member cannot start round'
if (!$kitchen.activeRound) {
    Assert-Status (Invoke-WebRequest "$root/rounds" -Method Post -WebSession $chef -Headers @{ 'X-CSRF-TOKEN' = (Get-Token $chef) }) 200 'Chef starts round'
    $kitchen = Invoke-RestMethod "$root/kitchen" -WebSession $chef
}
Assert-Status (Invoke-WebRequest "$root/rounds" -Method Post -WebSession $chef -Headers @{ 'X-CSRF-TOKEN' = (Get-Token $chef) } -SkipHttpErrorCheck) 409 'Second active round rejected'
$recipeId = $kitchen.recipes[0].id
$voteUrl = "$root/rounds/$($kitchen.activeRound.id)/votes/$recipeId"
# PowerShell retains explicitly supplied headers on WebRequestSession between requests.
$null = $member.Headers.Remove('X-CSRF-TOKEN')
Assert-Status (Invoke-WebRequest $voteUrl -Method Put -WebSession $member -ContentType 'application/json' -Body '{"choice":"Yes"}' -SkipHttpErrorCheck) 400 'Vote without antiforgery token rejected'
foreach ($choice in 'Yes', 'No', 'Skip') {
    $body = @{ choice = $choice } | ConvertTo-Json
    Assert-Status (Invoke-WebRequest $voteUrl -Method Put -WebSession $member -Headers @{ 'X-CSRF-TOKEN' = (Get-Token $member) } -ContentType 'application/json' -Body $body) 200 "Save $choice vote"
    $saved = (Invoke-RestMethod "$root/kitchen" -WebSession $member).myVotes | Where-Object recipeId -eq $recipeId
    if ($saved.Count -ne 1 -or $saved.choice -ne $choice) { throw 'Vote did not persist correctly.' }
}
$foreign = [guid]::NewGuid()
Assert-Status (Invoke-WebRequest "$BaseUrl/api/households/$foreign/kitchen" -WebSession $member -SkipHttpErrorCheck) 404 'Foreign household rejected'
if ($RestartApi) {
    docker compose restart api
    if ($LASTEXITCODE -ne 0) { throw 'API restart failed.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        try { $null = Invoke-RestMethod "$BaseUrl/health/ready"; $ready = $true; break } catch { Start-Sleep -Seconds 1 }
    }
    if (!$ready) { throw 'API did not become ready after restart.' }
    $saved = (Invoke-RestMethod "$root/kitchen" -WebSession $member).myVotes | Where-Object recipeId -eq $recipeId
    if ($saved.choice -ne 'Skip') { throw 'Vote did not survive restart.' }
    Write-Host 'PASS: Session and PostgreSQL vote survive API restart'
}
Write-Host 'Local smoke checks passed. The first sample recipe now has a Member Skip vote.'
