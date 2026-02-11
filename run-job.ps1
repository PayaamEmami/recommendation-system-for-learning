param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("ingestion", "x-ingestion", "feed")]
    [string]$JobName
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot

function Write-Log($message) {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Output "[$timestamp] $message"
}

# 1. Ensure Docker Desktop is running
Write-Log "Checking Docker Desktop..."
$dockerRunning = $false
try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -eq 0) { $dockerRunning = $true }
} catch {}

if (-not $dockerRunning) {
    Write-Log "Docker Desktop is not running. Starting it..."
    Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"

    $timeout = 120
    $elapsed = 0
    while ($elapsed -lt $timeout) {
        Start-Sleep -Seconds 5
        $elapsed += 5
        try {
            $null = docker info 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Log "Docker Desktop is ready."
                $dockerRunning = $true
                break
            }
        } catch {}
        Write-Log "Waiting for Docker Desktop... ($elapsed/$timeout seconds)"
    }

    if (-not $dockerRunning) {
        Write-Log "ERROR: Docker Desktop did not start within $timeout seconds."
        exit 1
    }
}

# 2. Ensure OpenSearch container is running
Write-Log "Checking OpenSearch container..."
$containerStatus = docker ps --filter "name=rsl-opensearch" --format "{{.Status}}" 2>&1
if (-not $containerStatus -or $containerStatus -notlike "Up*") {
    Write-Log "OpenSearch container is not running. Starting it..."
    docker compose -f "$repoRoot\docker-compose.yml" up -d opensearch
    if ($LASTEXITCODE -ne 0) {
        Write-Log "ERROR: Failed to start OpenSearch container."
        exit 1
    }
} else {
    Write-Log "OpenSearch container is already running."
}

# 3. Wait for OpenSearch to be healthy
Write-Log "Waiting for OpenSearch to be healthy..."
$timeout = 120
$elapsed = 0
$healthy = $false
while ($elapsed -lt $timeout) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:9200/_cluster/health" -TimeoutSec 5 -ErrorAction SilentlyContinue
        if ($response.status -eq "green" -or $response.status -eq "yellow") {
            Write-Log "OpenSearch is healthy (status: $($response.status))."
            $healthy = $true
            break
        }
    } catch {}
    Start-Sleep -Seconds 5
    $elapsed += 5
    Write-Log "Waiting for OpenSearch... ($elapsed/$timeout seconds)"
}

if (-not $healthy) {
    Write-Log "ERROR: OpenSearch did not become healthy within $timeout seconds."
    exit 1
}

# 4. Run the job
Write-Log "Running job: $JobName"
Set-Location $repoRoot
dotnet run --project src/Rsl.Jobs -- $JobName
$exitCode = $LASTEXITCODE
Write-Log "Job '$JobName' finished with exit code: $exitCode"
exit $exitCode
