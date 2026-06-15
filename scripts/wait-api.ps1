param(
  [string]$Url = "http://localhost:5181/swagger/v1/swagger.json",
  [int]$TimeoutSeconds = 90,
  [int]$DelaySeconds = 2,
  [string]$ContainerName = "drivetrace-api"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$attempt = 0

Write-Host "Waiting for API readiness at $Url (timeout ${TimeoutSeconds}s)..."

while ((Get-Date) -lt $deadline) {
  $attempt++
  try {
    $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
    if ($response.StatusCode -eq 200) {
      Write-Host "[OK] API ready after $attempt attempt(s)."
      return
    }
  } catch {
    if ($attempt -eq 1 -or $attempt % 5 -eq 0) {
      Write-Host "[WAIT] API not ready yet: $($_.Exception.Message)"
    }
  }

  Start-Sleep -Seconds $DelaySeconds
}

Write-Host "[FAIL] API did not become ready within ${TimeoutSeconds}s."
Write-Host "Recent logs from ${ContainerName}:"
try {
  docker logs --tail 120 $ContainerName
} catch {
  Write-Host "Could not read Docker logs: $($_.Exception.Message)"
}

exit 1
