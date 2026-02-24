param(
    [Parameter(Mandatory = $true)]
    [string]$Database,

    [string]$Host = "localhost",
    [int]$Port = 3306,
    [Parameter(Mandatory = $true)]
    [string]$Username,
    [Parameter(Mandatory = $true)]
    [string]$Password,
    [string]$OutputDir = ".\backups"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Get-Command mysqldump -ErrorAction SilentlyContinue)) {
    throw "mysqldump not found on PATH."
}

if (-not (Test-Path $OutputDir)) {
    New-Item -Path $OutputDir -ItemType Directory | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$fileName = "$Database-$timestamp.sql"
$outputPath = Join-Path $OutputDir $fileName

$env:MYSQL_PWD = $Password
try {
    & mysqldump --host=$Host --port=$Port --user=$Username --single-transaction --routines --events --triggers $Database > $outputPath
}
finally {
    Remove-Item Env:\MYSQL_PWD -ErrorAction SilentlyContinue
}

if (-not (Test-Path $outputPath)) {
    throw "Backup failed. Output file not found: $outputPath"
}

$sizeBytes = (Get-Item $outputPath).Length
Write-Host "Backup complete: $outputPath ($sizeBytes bytes)"
