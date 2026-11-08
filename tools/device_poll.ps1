$ErrorActionPreference = "Stop"
$clientId = "178c6fc778ccc68e1d6a"
$dir = "$env:LOCALAPPDATA\FortniteChestCounter"
$flow = Get-Content "$dir\device_flow.json" -Raw | ConvertFrom-Json
$interval = [int]$flow.interval; if ($interval -lt 5) { $interval = 5 }
$deadline = (Get-Date).AddMinutes(9)
$token = $null

Write-Output ("polling for user code " + $flow.user_code + " ...")
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds $interval
    try {
        $t = Invoke-RestMethod -Method Post -Uri "https://github.com/login/oauth/access_token" `
            -Headers @{ "Accept" = "application/json" } `
            -Body @{ client_id = $clientId; device_code = $flow.device_code; grant_type = "urn:ietf:params:oauth:grant-type:device_code" }
        if ($t.access_token) { $token = $t.access_token; break }
        if ($t.error -eq "authorization_pending") { Write-Output "waiting for user..."; continue }
        if ($t.error -eq "slow_down") { $interval += 5; Write-Output "slowing down poll..."; continue }
        if ($t.error) { Write-Output ("FLOW ERROR: " + $t.error); exit 1 }
    } catch {
        Write-Output "network retry..."
    }
}
if (-not $token) { Write-Output "TIMEOUT: code expired (10 min) — перезапусти процедуру"; exit 1 }

[System.IO.File]::WriteAllText("$dir\.gh_token", $token)
Write-Output "TOKEN SAVED — авторизация успешна"
