param(
    [Parameter(Mandatory = $true)]
    [string]$Database,

    [Parameter(Mandatory = $true)]
    [string]$BackupFile,

    [string]$Host = "localhost",
    [int]$Port = 3306,
    [Parameter(Mandatory = $true)]
    [string]$Username,
    [Parameter(Mandatory = $true)]
    [string]$Password
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command mysql -ErrorAction SilentlyContinue)) {
    throw "mysql client not found on PATH."
}

if (-not (Test-Path $BackupFile)) {
    throw "Backup file not found: $BackupFile"
}

$env:MYSQL_PWD = $Password
try {
    & mysql --host=$Host --port=$Port --user=$Username --execute="CREATE DATABASE IF NOT EXISTS \`$Database\`;"
    Get-Content -Path $BackupFile -Raw | & mysql --host=$Host --port=$Port --user=$Username $Database
}
finally {
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}

Write-Host "Restore complete into database '$Database' from '$BackupFile'."
