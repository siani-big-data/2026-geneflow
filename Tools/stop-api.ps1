$ErrorActionPreference = 'Continue'

# Kill anything currently bound to :5145 (the API).
$listeners = Get-NetTCPConnection -State Listen -LocalPort 5145 -ErrorAction SilentlyContinue
if ($listeners) {
    foreach ($l in $listeners) {
        $procId = $l.OwningProcess
        Write-Host "Killing PID $procId (port 5145)"
        taskkill /F /T /PID $procId | Out-Null
    }
} else {
    Write-Host 'No process listening on :5145.'
}

# Also kill any `dotnet run` against GeneFlow.ApiNet2.API in case the
# wrapper survived but its child died earlier.
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
    Where-Object { $_.CommandLine -and $_.CommandLine -match 'GeneFlow\.ApiNet2\.API' } |
    ForEach-Object {
        Write-Host "Killing dotnet run wrapper PID $($_.ProcessId)"
        taskkill /F /T /PID $_.ProcessId | Out-Null
    }

Start-Sleep -Seconds 2

$still = Get-NetTCPConnection -State Listen -LocalPort 5145 -ErrorAction SilentlyContinue
if ($still) {
    Write-Host "WARNING: something is still listening on :5145 -> $($still.OwningProcess -join ', ')" -ForegroundColor Red
    exit 1
} else {
    Write-Host 'Port 5145 is free.' -ForegroundColor Green
}
