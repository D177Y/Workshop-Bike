param(
    [string]$BaseUrl = "http://workshop.local"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$env:WORKSHOP_HTTP_SMOKE_BASE_URL = $BaseUrl
dotnet run --project .\Workshop.Tests\Workshop.Tests.csproj
