param(
  [string]$ApiBaseUrl = "http://localhost:5181/api",
  [switch]$PublishFiware
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$script:FailedChecks = 0

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

function Invoke-Api {
  param(
    [string]$Name,
    [string]$Path,
    [string]$Method = "GET",
    [object]$Body = $null
  )

  $url = "$ApiBaseUrl$Path"
  try {
    if ($Method -eq "GET") {
      $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 25
    } else {
      $jsonBody = if ($null -eq $Body) { "{}" } else { $Body | ConvertTo-Json -Depth 8 }
      $response = Invoke-WebRequest -Uri $url -Method $Method -Body $jsonBody -ContentType "application/json" -UseBasicParsing -TimeoutSec 30
    }

    Write-Host "[OK] $Name -> HTTP $($response.StatusCode)"
    return $response.Content | ConvertFrom-Json
  } catch {
    $detail = $_.Exception.Message
    if ($_.Exception.Response) {
      try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $bodyText = $reader.ReadToEnd()
        if ($bodyText) { $detail = $bodyText }
      } catch {
      }
    }
    Register-Failure "$Name failed: $detail"
    return $null
  }
}

Write-Host "DriveTrace Core - production flow smoke test"
Write-Host "--------------------------------------------"

$flow = Invoke-Api -Name "Flow summary" -Path "/operations/flow-summary"
if ($flow) {
  $lines = @()
  if ($flow.lineSummaries) { $lines = @($flow.lineSummaries) }
  Assert-Condition ($lines.Count -ge 2) "Flow summary exposes multiple production lines." "Flow summary does not expose multiple production lines."
  Assert-Condition ($flow.totals.transferPoints -ge 1) "Flow summary exposes transfer points." "Flow summary does not expose transfer points."
}

$workbench = Invoke-Api -Name "Operator workbench" -Path "/operator/workbench"
if ($workbench) {
  $workbenchUnits = @()
  if ($workbench.units) { $workbenchUnits = @($workbench.units) }
  Assert-Condition ($null -ne $workbench.queues) "Operator workbench exposes queue counters." "Operator workbench does not expose queue counters."
  Assert-Condition ($workbenchUnits.Count -ge 1) "Operator workbench exposes active units." "Operator workbench has no active units."
}

$units = Invoke-Api -Name "Product units" -Path "/product-units"
$sections = Invoke-Api -Name "Production line sections" -Path "/production-line-sections"

$unit = $null
if ($units) {
  $unit = @($units) | Where-Object { $_.unitCode -eq "UP-PORTA-005" } | Select-Object -First 1
  if (-not $unit) { $unit = @($units) | Where-Object { $_.status -ne "Completed" -and $_.status -ne "Scrap" } | Select-Object -First 1 }
}

$target = $null
if ($sections) {
  $target = @($sections) | Where-Object { $_.sectionCode -eq "SEC-RETRAB" } | Select-Object -First 1
  if (-not $target) { $target = @($sections) | Where-Object { $_.allowsLineTransferIn -eq $true } | Select-Object -First 1 }
}

Assert-Condition ($null -ne $unit) "A transferable product unit is available." "No transferable product unit was found."
Assert-Condition ($null -ne $target) "A target section is available." "No target transfer section was found."

if ($unit -and $target) {
  $transfer = Invoke-Api `
    -Name "Product unit transfer" `
    -Path "/product-units/$($unit.id)/transfer" `
    -Method "POST" `
    -Body @{
      toSectionId = [int]$target.id
      reason = "Transferência de validação automática"
      notes = "scripts/test-production-flow.ps1"
      operatorUserId = "script"
      moveCurrentSupport = $true
    }

  Assert-Condition ($transfer -and $transfer.transferred -eq $true) "Transfer endpoint accepted the ProductUnit movement." "Transfer endpoint did not confirm movement."

  $trace = Invoke-Api -Name "Product unit trace" -Path "/product-units/$($unit.id)/trace"
  if ($trace) {
    $timeline = @()
    if ($trace.timeline) { $timeline = @($trace.timeline) }
    $hasTransfer = $timeline | Where-Object { $_.eventType -in @("LineTransfer", "UnitTransfer", "ManualMovement", "PlaybackMovement") } | Select-Object -First 1
    Assert-Condition ($null -ne $hasTransfer) "Trace endpoint exposes movement timeline." "Trace endpoint does not expose a movement timeline."
  }
}

$customer = Invoke-Api -Name "Customer public order lookup" -Path "/customer/orders/TRC-PORTA-001"
if ($customer) {
  Assert-Condition ($customer.publicTrackingCode -eq "TRC-PORTA-001") "Customer lookup returns the PT-PT demo public tracking code." "Customer lookup returned an unexpected tracking code."
}

if ($PublishFiware) {
  $publish = Invoke-Api -Name "Publish FIWARE current context" -Path "/fiware/publish-current" -Method "POST" -Body @{}
  Assert-Condition ($null -ne $publish) "FIWARE publish-current endpoint responded." "FIWARE publish-current endpoint did not respond."
}

$context = Invoke-Api -Name "FIWARE context" -Path "/fiware/context"
if ($context) {
  $entities = @()
  if ($context.entities) { $entities = @($context.entities) }
  $productUnitEntity = $entities | Where-Object { $_.type -eq "ProductUnit" } | Select-Object -First 1
  $hasRouteState = $false
  if ($productUnitEntity -and $productUnitEntity.attributes) {
    $hasRouteState = $null -ne $productUnitEntity.attributes.routeState
  }
  Assert-Condition ($hasRouteState) "FIWARE ProductUnit context includes routeState." "FIWARE ProductUnit context does not include routeState."
  $hasPtProductUnit = $entities | Where-Object { $_.id -eq "urn:ngsi-ld:ProductUnit:UP-PORTA-001" } | Select-Object -First 1
  Assert-Condition ($null -ne $hasPtProductUnit) "FIWARE context includes UP-PORTA-001." "FIWARE context does not include the PT-PT ProductUnit URN."
}

if ($script:FailedChecks -gt 0) {
  Write-Host ""
  Write-Host "Production flow smoke test finished with $($script:FailedChecks) failure(s)."
  exit 1
}

Write-Host ""
Write-Host "Production flow smoke test finished successfully."
