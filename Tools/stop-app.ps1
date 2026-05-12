$ErrorActionPreference = 'Continue'
foreach ($p in 5000, 3000) {
    $listeners = Get-NetTCPConnection -State Listen -LocalPort $p -ErrorAction SilentlyContinue
    if ($listeners) {
        foreach ($l in $listeners) {
            Write-Host "Killing PID $($l.OwningProcess) on port $p"
            taskkill /F /T /PID $l.OwningProcess 2>&1 | Out-Null
        }
    } else {
        Write-Host "Nothing on port $p"
    }
}
Start-Sleep -Seconds 2
foreach ($p in 5000, 3000) {
    $still = Get-NetTCPConnection -State Listen -LocalPort $p -ErrorAction SilentlyContinue
    if ($still) {
        Write-Host "PORT $p STILL UP -> PIDs $($still.OwningProcess -join ',')" -ForegroundColor Red
    } else {
        Write-Host "Port $p free" -ForegroundColor Green
    }
}
