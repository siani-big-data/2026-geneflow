foreach ($t in @('psql','pg_dump','redis-cli','dotnet')) {
    $cmd = Get-Command $t -ErrorAction SilentlyContinue
    if ($cmd) {
        Write-Host "$t : $($cmd.Source)"
    } else {
        Write-Host "$t : MISSING" -ForegroundColor Red
    }
}
Write-Host '---'
$ef = & dotnet ef --version 2>&1
Write-Host "dotnet ef: $ef"
