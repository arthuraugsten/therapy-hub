# Configures the local Keycloak realm (therapy-hub) using the passwords in .env.
# Usage (from the repo root, after `docker compose up -d`):  ./infra/keycloak/setup.ps1
$ErrorActionPreference = 'Stop'
$root = Resolve-Path "$PSScriptRoot/../.."

$envVars = @{}
Get-Content "$root/.env" | Where-Object { $_ -match '^\w+=' } | ForEach-Object {
    $key, $value = $_ -split '=', 2
    $envVars[$key] = $value
}

foreach ($key in 'KEYCLOAK_ADMIN_PASSWORD', 'KEYCLOAK_DEV_USER_PASSWORD') {
    if (-not $envVars[$key]) { throw "Set $key in .env first." }
}

$container = docker compose -f "$root/compose.yaml" ps -q keycloak
if (-not $container) { throw 'Keycloak is not running. Run: docker compose up -d' }

docker cp "$PSScriptRoot/setup.sh" "${container}:/tmp/keycloak-setup.sh"
docker exec -e KC_ADMIN_PW=$($envVars['KEYCLOAK_ADMIN_PASSWORD']) -e DEV_USER_PW=$($envVars['KEYCLOAK_DEV_USER_PASSWORD']) `
    $container bash /tmp/keycloak-setup.sh
