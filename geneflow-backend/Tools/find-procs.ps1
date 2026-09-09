Write-Host '--- listening ports ---'
Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
    Where-Object { $_.LocalPort -in 5145,5001,5000,3000 } |
    Select-Object LocalPort, OwningProcess |
    Format-Table -AutoSize

Write-Host '--- dotnet ---'
Get-Process dotnet -ErrorAction SilentlyContinue |
    ForEach-Object {
        $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)").CommandLine
        [pscustomobject]@{ Id = $_.Id; StartTime = $_.StartTime; CmdLine = $cmd }
    } | Format-Table -Wrap

Write-Host '--- python ---'
Get-Process python -ErrorAction SilentlyContinue |
    ForEach-Object {
        $cmd = (Get-CimInstance Win32_Process -Filter "ProcessId=$($_.Id)").CommandLine
        [pscustomobject]@{ Id = $_.Id; CmdLine = $cmd }
    } | Format-Table -Wrap
