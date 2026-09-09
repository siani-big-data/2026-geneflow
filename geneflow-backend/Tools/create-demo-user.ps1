#requires -Version 5.1
<#
    Crea un usuario demo en GeneFlow:
      1) POST /api/v1/auth/register      (email, username, password)
      1b) UPDATE identity.users          (email_verified=true, demo bypass)
      2) POST /api/v1/auth/login         (recupera JWT)
      3) POST /api/v1/profiles/          (firstName, lastName)
      4) UPDATE subscriptions            (asigna plan ilimitado / Enterprise)

    Uso por defecto: eduardo.marrero@alu.ulpgc.es / Eduardo.Marrero1
#>
param(
    [string] $ApiBase  = "http://localhost:5000",
    [string] $Email    = "eduardo.marrero@alu.ulpgc.es",
    [string] $Username = "eduardomarrero",
    [string] $Password = "Eduardo.Marrero1",
    [string] $FirstName = "Eduardo",
    [string] $LastName  = "Marrero",
    [string] $PgContainer = "geneflow-core-postgres",
    [string] $PgUser      = "geneflow",
    [string] $PgDatabase  = "geneflow",
    [string] $UnlimitedPlanName = "Enterprise"
)

$ErrorActionPreference = "Stop"

function Invoke-Json {
    param(
        [string] $Method,
        [string] $Url,
        [hashtable] $Body = $null,
        [string] $Token = $null
    )
    $headers = @{ "Accept" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{
        Method      = $Method
        Uri         = $Url
        Headers     = $headers
        ContentType = "application/json"
    }
    if ($Body) {
        $params["Body"] = ($Body | ConvertTo-Json -Depth 10 -Compress)
    }
    return Invoke-RestMethod @params
}

Write-Host ""
Write-Host "Crear usuario demo" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host "API       : $ApiBase"
Write-Host "Email     : $Email"
Write-Host "Username  : $Username"
Write-Host "Profile   : $FirstName $LastName"
Write-Host ""

# --- 1. Register --------------------------------------------------------
Write-Host "[1/3] Register" -ForegroundColor Green
try {
    $registerBody = @{
        email    = $Email
        username = $Username
        password = $Password
    }
    $reg = Invoke-Json -Method "POST" -Url "$ApiBase/api/v1/auth/register" -Body $registerBody
    Write-Host "  - usuario creado"
} catch {
    $resp = $_.Exception.Response
    $code = if ($resp) { $resp.StatusCode.value__ } else { 0 }
    $body = ""
    try {
        $stream = $resp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $body = $reader.ReadToEnd()
    } catch {}
    if ($code -eq 409) {
        Write-Host "  - ya existe (409), continuo" -ForegroundColor Yellow
    } else {
        Write-Host "  - register fallo HTTP=$code body=$body" -ForegroundColor Red
        throw
    }
}

# --- 1b. Marcar email como verificado (bypass para demo) ---------------
Write-Host "[1b] Verificar email en DB" -ForegroundColor Green
$sql = "UPDATE identity.users SET email_verified=true, email_verification_token=NULL, email_verification_token_expiry=NULL WHERE email='$Email';"
docker exec $PgContainer psql -U $PgUser -d $PgDatabase -v ON_ERROR_STOP=1 -c $sql | Out-Null
if ($LASTEXITCODE -ne 0) { throw "No pude marcar email_verified" }

# --- 2. Login -----------------------------------------------------------
Write-Host "[2/3] Login" -ForegroundColor Green
$loginBody = @{
    identifier = $Email
    password   = $Password
}
$login = Invoke-Json -Method "POST" -Url "$ApiBase/api/v1/auth/login" -Body $loginBody

$token = $null
if ($login.tokens -and $login.tokens.accessToken) {
    $token = $login.tokens.accessToken
} elseif ($login.accessToken) {
    $token = $login.accessToken
}
if (-not $token) {
    Write-Host ($login | ConvertTo-Json -Depth 6) -ForegroundColor DarkGray
    throw "No pude extraer el JWT de la respuesta de login"
}
Write-Host "  - JWT obtenido (len=$($token.Length))"

# --- 3. Crear perfil ----------------------------------------------------
Write-Host "[3/3] Crear profile" -ForegroundColor Green
try {
    $profileBody = @{
        firstName = $FirstName
        lastName  = $LastName
    }
    $profile = Invoke-Json -Method "POST" -Url "$ApiBase/api/v1/profiles/" -Body $profileBody -Token $token
    Write-Host "  - perfil creado"
} catch {
    $resp = $_.Exception.Response
    if ($resp -and $resp.StatusCode.value__ -eq 409) {
        Write-Host "  - perfil ya existe" -ForegroundColor Yellow
    } else {
        throw
    }
}

# --- 4. Upgrade subscription al plan ilimitado --------------------------
Write-Host "[4/4] Asignar plan $UnlimitedPlanName" -ForegroundColor Green
$upgradeSql = @"
UPDATE subscriptions.subscriptions s
SET plan_id   = p.id,
    plan_name = p.name,
    modified_at = NOW()
FROM plans.plans p
WHERE p.name = '$UnlimitedPlanName'
  AND s.user_id = (SELECT id FROM identity.users WHERE email = '$Email');
"@
docker exec $PgContainer psql -U $PgUser -d $PgDatabase -v ON_ERROR_STOP=1 -c $upgradeSql | Out-Null
if ($LASTEXITCODE -ne 0) { throw "No pude asignar el plan $UnlimitedPlanName" }

$check = docker exec $PgContainer psql -U $PgUser -d $PgDatabase -t -A -F "|" -c "SELECT s.plan_name, p.max_studies, p.max_traces_per_month FROM subscriptions.subscriptions s JOIN plans.plans p ON p.id = s.plan_id JOIN identity.users u ON u.id = s.user_id WHERE u.email='$Email';"
Write-Host "  - subscription -> $check"

Write-Host ""
Write-Host "OK -> $Email / $Password (plan: $UnlimitedPlanName)" -ForegroundColor Cyan
