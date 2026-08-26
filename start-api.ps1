# Stops any process on port 5126, then starts the API
$port = 5126
$apiPath = Join-Path $PSScriptRoot "src\LexVersusEmailMarketing.Api"

Write-Host "Checking port $port..."

$pids = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess -Unique

foreach ($pid in $pids) {
    Write-Host "Stopping process $pid that is using port $port..."
    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Seconds 1

Set-Location $apiPath
Write-Host "Starting API at http://localhost:$port ..."
dotnet run --launch-profile http
