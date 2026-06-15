param(
  [string]$ApiBaseUrl = "http://localhost:5181/api",
  [string]$DashboardBaseUrl = "http://localhost:8088",
  [string]$GrafanaBaseUrl = "http://localhost:33010",
  [switch]$PublishFiware
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$script:FailedChecks = 0

$apiRoot = $ApiBaseUrl -replace "/api/?$", ""
& "$PSScriptRoot\wait-api.ps1" -Url "$apiRoot/swagger/v1/swagger.json"

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

function New-Headers {
  param([string]$Role = "Administrator")
  return @{
    "X-DriveTrace-Role" = $Role
    "X-DriveTrace-User" = "phase-a-script"
  }
}

function Invoke-Json {
  param(
    [string]$Name,
    [string]$Path,
    [string]$Method = "GET",
    [object]$Body = $null,
    [string]$Role = "Administrator"
  )

  $url = "$ApiBaseUrl$Path"
  try {
    $headers = New-Headers -Role $Role
    if ($Method -eq "GET") {
      $response = Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -TimeoutSec 30
    } else {
      $jsonBody = if ($null -eq $Body) { "{}" } else { $Body | ConvertTo-Json -Depth 10 }
      $response = Invoke-WebRequest -Uri $url -Method $Method -Headers $headers -Body $jsonBody -ContentType "application/json" -UseBasicParsing -TimeoutSec 30
    }

    Write-Host "[OK] $Name -> HTTP $($response.StatusCode)"
    if ([string]::IsNullOrWhiteSpace($response.Content)) { return $null }
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

function Invoke-ExpectedStatus {
  param(
    [string]$Name,
    [string]$Path,
    [string]$Method,
    [object]$Body,
    [string]$Role,
    [int]$ExpectedStatus
  )

  $url = "$ApiBaseUrl$Path"
  $headers = New-Headers -Role $Role
  try {
    $jsonBody = if ($null -eq $Body) { "{}" } else { $Body | ConvertTo-Json -Depth 10 }
    $response = Invoke-WebRequest -Uri $url -Method $Method -Headers $headers -Body $jsonBody -ContentType "application/json" -UseBasicParsing -TimeoutSec 30
    Assert-Condition ($response.StatusCode -eq $ExpectedStatus) "$Name returned HTTP $ExpectedStatus." "$Name returned HTTP $($response.StatusCode), expected $ExpectedStatus."
  } catch {
    $statusCode = $null
    if ($_.Exception.Response) {
      $statusCode = [int]$_.Exception.Response.StatusCode
    }
    Assert-Condition ($statusCode -eq $ExpectedStatus) "$Name returned HTTP $ExpectedStatus." "$Name returned HTTP $statusCode, expected $ExpectedStatus."
  }
}

function Invoke-HttpHealth {
  param(
    [string]$Name,
    [string]$Url
  )

  try {
    $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 20
    Assert-Condition ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) "$Name is reachable." "$Name returned HTTP $($response.StatusCode)."
    return $response
  } catch {
    Register-Failure "$Name is not reachable: $($_.Exception.Message)"
    return $null
  }
}

Write-Host "DriveTrace Core - Phase A foundation smoke test"
Write-Host "------------------------------------------------"

$apiRoot = $ApiBaseUrl -replace "/api/?$", ""
Invoke-HttpHealth -Name "Swagger JSON" -Url "$apiRoot/swagger/v1/swagger.json" | Out-Null
Invoke-HttpHealth -Name "Dashboard" -Url $DashboardBaseUrl | Out-Null

$permissions = Invoke-Json -Name "Permission catalog" -Path "/permissions/catalog?role=Operator" -Role "Operator"
if ($permissions) {
  $roles = @($permissions.roles)
  $activePermissions = @($permissions.activePermissions)
  Assert-Condition (($roles | Where-Object { $_.key -eq "Operator" } | Select-Object -First 1) -ne $null) "Permission catalog exposes Operator." "Permission catalog does not expose Operator."
  Assert-Condition ($activePermissions -contains "ProductUnits.Transfer") "Operator can transfer product units." "Operator profile lacks ProductUnits.Transfer."
  Assert-Condition (-not ($activePermissions -contains "Fiware.Manage")) "Operator is not allowed to manage FIWARE." "Operator should not have Fiware.Manage."
}

$events = Invoke-Json -Name "Recent operational events" -Path "/operational-events/recent?limit=20" -Role "DemoViewer"
if ($events) {
  $eventRows = @($events)
  Assert-Condition ($eventRows.Count -gt 0) "Recent operational events endpoint returns rows." "Recent operational events endpoint returned no rows."
}

$seedEvents = Invoke-Json -Name "Seed operational events" -Path "/operational-events?eventType=DemoSeeded&limit=5" -Role "Administrator"
if ($seedEvents) {
  Assert-Condition (@($seedEvents).Count -gt 0) "Seed operational events are present." "Seed operational events were not found."
}

$flow = Invoke-Json -Name "Flow summary" -Path "/operations/flow-summary"
if ($flow) {
  Assert-Condition ($flow.totals.operationalEvents -gt 0) "Flow summary exposes operational event totals." "Flow summary does not expose operational event totals."
  Assert-Condition (@($flow.recentOperationalEvents).Count -gt 0) "Flow summary exposes recent operational events." "Flow summary recentOperationalEvents is empty."
}

$units = Invoke-Json -Name "Product units" -Path "/product-units"
$sections = Invoke-Json -Name "Production line sections" -Path "/production-line-sections"
$unit = $null
$target = $null
if ($units) {
  $unit = @($units) | Where-Object { $_.unitCode -eq "UP-PORTA-005" } | Select-Object -First 1
  if (-not $unit) { $unit = @($units) | Where-Object { $_.status -ne "Completed" -and $_.status -ne "Scrap" } | Select-Object -First 1 }
}
if ($sections) {
  $target = @($sections) | Where-Object { $_.sectionCode -eq "SEC-RETRAB" } | Select-Object -First 1
  if (-not $target) { $target = @($sections) | Where-Object { $_.allowsLineTransferIn -eq $true } | Select-Object -First 1 }
}

Assert-Condition ($null -ne $unit) "A transferable product unit is available." "No transferable product unit found."
Assert-Condition ($null -ne $target) "A target section is available." "No target section found."

if ($unit -and $target) {
  Invoke-ExpectedStatus `
    -Name "Customer denied transfer" `
    -Path "/product-units/$($unit.id)/transfer" `
    -Method "POST" `
    -Role "Customer" `
    -ExpectedStatus 403 `
    -Body @{
      toSectionId = [int]$target.id
      reason = "Tentativa sem permissao"
      notes = "scripts/test-phase-a-foundation.ps1"
      operatorUserId = "phase-a-script"
      moveCurrentSupport = $true
    }

  $transfer = Invoke-Json `
    -Name "Operator product-unit transfer" `
    -Path "/product-units/$($unit.id)/transfer" `
    -Method "POST" `
    -Role "Operator" `
    -Body @{
      toSectionId = [int]$target.id
      reason = "Transferencia de validacao Fase A"
      notes = "scripts/test-phase-a-foundation.ps1"
      operatorUserId = "phase-a-script"
      moveCurrentSupport = $true
    }

  Assert-Condition ($transfer -and $transfer.transferred -eq $true) "Operator transfer created a movement." "Operator transfer did not confirm movement."

  $trace = Invoke-Json -Name "Product-unit trace with operational events" -Path "/product-units/$($unit.id)/trace" -Role "DemoViewer"
  if ($trace) {
    $traceEvents = @($trace.operationalEvents)
    $timeline = @($trace.timeline)
    Assert-Condition ($traceEvents.Count -gt 0) "Trace includes operationalEvents." "Trace does not include operationalEvents."
    Assert-Condition (($timeline | Where-Object { $_.eventType -eq "LineTransfer" -or $_.eventType -eq "SectionMovement" } | Select-Object -First 1) -ne $null) "Trace timeline includes operational movement events." "Trace timeline lacks operational movement events."
  }

  $unitEvents = Invoke-Json -Name "Product-unit events endpoint" -Path "/product-units/$($unit.id)/events" -Role "DemoViewer"
  if ($unitEvents) {
    Assert-Condition (@($unitEvents).Count -gt 0) "Product-unit events endpoint returns rows." "Product-unit events endpoint returned no rows."
  }
}

$fiware = Invoke-Json -Name "FIWARE context" -Path "/fiware/context" -Role "DemoViewer"
if ($fiware) {
  Assert-Condition ($fiware.relationalSnapshotCount -gt 0) "FIWARE context exposes relational snapshot." "FIWARE context relational snapshot is empty."
}

if ($PublishFiware) {
  $publish = Invoke-Json -Name "FIWARE publish-current" -Path "/fiware/publish-current" -Method "POST" -Body @{} -Role "Administrator"
  Assert-Condition ($null -ne $publish) "FIWARE publish-current responded." "FIWARE publish-current did not respond."

  $publishedEvents = Invoke-Json -Name "FIWARE published events" -Path "/operational-events?eventType=FiwarePublished&limit=5" -Role "Administrator"
  if ($publishedEvents) {
    Assert-Condition (@($publishedEvents).Count -gt 0) "FiwarePublished operational event was recorded." "FiwarePublished operational event was not recorded."
  }
}

Invoke-HttpHealth -Name "Grafana API" -Url "$GrafanaBaseUrl/api/health" | Out-Null

if ($script:FailedChecks -gt 0) {
  Write-Host ""
  Write-Host "Phase A foundation smoke test finished with $($script:FailedChecks) failure(s)."
  exit 1
}

Write-Host ""
Write-Host "Phase A foundation smoke test finished successfully."
