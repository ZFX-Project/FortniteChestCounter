$ErrorActionPreference = "Stop"
$clientId = "178c6fc778ccc68e1d6a"   # публичный client_id GitHub CLI
$resp = Invoke-RestMethod -Method Post -Uri "https://github.com/login/device/code" `
    -Headers @{ "Accept" = "application/json" } `
    -Body @{ client_id = $clientId; scope = "repo read:user" }

$dir = "$env:LOCALAPPDATA\FortniteChestCounter"
New-Item $dir -ItemType Directory -Force | Out-Null
@{
    device_code = $resp.device_code
    user_code = $resp.user_code
    uri = $resp.verification_uri
    interval = [int]$resp.interval
} | ConvertTo-Json | Set-Content "$dir\device_flow.json"

Write-Output ("USER_CODE: " + $resp.user_code)
Write-Output ("OPEN: " + $resp.verification_uri)
