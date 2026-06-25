param(
  [switch]$PublishCurrent
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$script:FailedChecks = 0
$script:StaleEntityId = "urn:ngsi-ld:Support:teste"

& "$PSScriptRoot\wait-api.ps1"

function Register-Failure {
  param([string]$Message)
  Write-Host "[FAIL] $Message"
  $script:FailedChecks++
}

function Assert-Condition {
  param(
    [bool]$Condition,
    [string]$SuccessMessage,
    [string]$FailureMessage
  )

  if ($Condition) {
    Write-Host "[OK] $SuccessMessage"
  } else {
    Register-Failure $FailureMessage
  }
}

function Show-Result {
  param(
    [string]$Name,
    [string]$Url,
    [string]$Method = "GET",
    [string]$Body = "",
    [string]$ContentType = "application/json"
  )

  try {
    if ($Method -eq "GET") {
      $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 20
    } else {
      $response = Invoke-WebRequest -Uri $Url -Method $Method -Body $Body -ContentType $ContentType -UseBasicParsing -TimeoutSec 25
    }

    Write-Host "[OK] $Name -> HTTP $($response.StatusCode)"
    if ($response.Content) {
      $preview = $response.Content
      if ($preview.Length -gt 320) { $preview = $preview.Substring(0, 320) + " ..." }
      Write-Host "     $preview"
    }
    return $response
  } catch {
    $status = "N/A"
    $detail = $_.Exception.Message
    if ($_.Exception.Response) {
      try {
        $status = [int]$_.Exception.Response.StatusCode
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $bodyText = $reader.ReadToEnd()
        if ($bodyText) { $detail = $bodyText }
      } catch {
      }
    }
    Register-Failure "$Name -> HTTP $status"
    Write-Host "      $detail"
    return $null
  }
}

function Get-FiwareContextObject {
  $contextResponse = Show-Result -Name "API FIWARE context" -Url "http://localhost:5181/api/fiware/context"
  if (-not $contextResponse) { return $null }

  try {
    return $contextResponse.Content | ConvertFrom-Json
  } catch {
    Register-Failure "API FIWARE context returned non-JSON payload"
    return $null
  }
}

function Remove-EntityBestEffort {
  param([string]$EntityId)

  $payload = "[`"$EntityId`"]"
  try {
    Invoke-WebRequest `
      -Method Post `
      -Uri "http://localhost:1026/ngsi-ld/v1/entityOperations/delete" `
      -Body $payload `
      -ContentType "application/json" `
      -UseBasicParsing `
      -TimeoutSec 15 | Out-Null
  } catch {
    if ($_.Exception.Response) {
      try {
        $status = [int]$_.Exception.Response.StatusCode
        if ($status -eq 404) { return }
      } catch {
      }
    }
  }
}

function Ensure-StaleEntityForSyncTest {
  $staleId = $script:StaleEntityId
  Remove-EntityBestEffort -EntityId $staleId

  $payload = @"
[
  {
    "id": "urn:ngsi-ld:Support:teste",
    "type": "Support",
    "supportCode": { "type": "Property", "value": "teste" },
    "status": { "type": "Property", "value": "\u00d3rf\u00e3" },
    "@context": [
      "https://uri.etsi.org/ngsi-ld/v1/ngsi-ld-core-context-v1.8.jsonld",
      {
        "Support": "https://uri.drivolution.local/ns/Support",
        "supportCode": "https://uri.drivolution.local/ns/supportCode",
        "status": "https://uri.drivolution.local/ns/status"
      }
    ]
  }
]
"@

  $createResponse = Show-Result `
    -Name "Upsert stale test entity (Support:teste)" `
    -Url "http://localhost:1026/ngsi-ld/v1/entityOperations/upsert" `
    -Method "POST" `
    -Body $payload `
    -ContentType "application/ld+json"

  if (-not $createResponse) { return $false }
  return $true
}

Write-Host "DriveTrace Core - FIWARE smoke test"
Write-Host "-----------------------------------"

Show-Result -Name "Orion-LD version" -Url "http://localhost:1026/version" | Out-Null
Show-Result -Name "IoT Agent about" -Url "http://localhost:14041/iot/about" | Out-Null
Show-Result -Name "QuantumLeap version" -Url "http://localhost:8668/version" | Out-Null
Show-Result -Name "API Swagger" -Url "http://localhost:5181/swagger" | Out-Null
Get-FiwareContextObject | Out-Null

if ($PublishCurrent) {
  Write-Host ""
  Write-Host "Stale synchronization test"
  Write-Host "--------------------------"

  $createdStale = Ensure-StaleEntityForSyncTest
  if (-not $createdStale) {
    Register-Failure "Could not create stale FIWARE entity for sync test."
  } else {
    $before = Get-FiwareContextObject
    if ($before) {
      $beforeEntities = @()
      if ($before.entities) { $beforeEntities = @($before.entities) }
      $hasStaleBefore = $beforeEntities | Where-Object { $_.id -eq $script:StaleEntityId } | Select-Object -First 1
      Assert-Condition ($null -ne $hasStaleBefore) "Stale test entity is visible before publish-current." "Stale test entity is missing before publish-current."
    }

    $publishResponseRaw = Show-Result `
      -Name "API publish current FIWARE context" `
      -Url "http://localhost:5181/api/fiware/publish-current" `
      -Method "POST" `
      -Body "{}" `
      -ContentType "application/json"

    if ($publishResponseRaw) {
      try {
        $publishResponse = $publishResponseRaw.Content | ConvertFrom-Json
        Assert-Condition ($publishResponse.staleDeletedCount -ge 1) "staleDeletedCount >= 1" "staleDeletedCount is less than 1"

        $staleIds = @()
        if ($publishResponse.staleEntityIds) { $staleIds = @($publishResponse.staleEntityIds) }
        $containsStale = $staleIds -contains $script:StaleEntityId
        Assert-Condition $containsStale "staleEntityIds includes $($script:StaleEntityId)" "staleEntityIds does not include $($script:StaleEntityId)"
      } catch {
        Register-Failure "Could not parse publish-current response JSON."
      }
    }

    $after = Show-Result -Name "API FIWARE context (after publish)" -Url "http://localhost:5181/api/fiware/context"
    if ($after) {
      try {
        $afterJson = $after.Content | ConvertFrom-Json
        Assert-Condition ($afterJson.entityCount -eq $afterJson.relationalSnapshotCount) "entityCount equals relationalSnapshotCount after publish-current." "entityCount differs from relationalSnapshotCount after publish-current."
        $afterEntities = @()
        if ($afterJson.entities) { $afterEntities = @($afterJson.entities) }
        $stillHasStale = $afterEntities | Where-Object { $_.id -eq $script:StaleEntityId } | Select-Object -First 1
        Assert-Condition ($null -eq $stillHasStale) "Stale test entity was removed from Orion-LD." "Stale test entity is still present in Orion-LD."
        $hasPtUnit = $afterEntities | Where-Object { $_.id -eq "urn:ngsi-ld:ProductUnit:UP-PORTA-001" } | Select-Object -First 1
        Assert-Condition ($null -ne $hasPtUnit) "Context includes urn:ngsi-ld:ProductUnit:UP-PORTA-001." "Context does not include the PT-PT ProductUnit URN."
      } catch {
        Register-Failure "Could not parse API FIWARE context (after publish) JSON."
      }
    }
  }
}

if ($script:FailedChecks -gt 0) {
  Write-Host ""
  Write-Host "FIWARE smoke test finished with $($script:FailedChecks) failure(s)."
  exit 1
}

Write-Host ""
Write-Host "FIWARE smoke test finished successfully."
