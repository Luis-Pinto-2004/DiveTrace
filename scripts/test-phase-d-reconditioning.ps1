param(
  [string]$ApiBaseUrl = "http://localhost:5181/api",
  [string]$DashboardBaseUrl = "http://localhost:8088",
  [string]$GrafanaBaseUrl = "http://localhost:33010",
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

function Convert-ToArray {
  param([object]$Value)
  if ($null -eq $Value) { return @() }
  return @($Value)
}

function New-Headers {
  param(
    [string]$Role,
    [string]$User
  )

  return @{
    "X-DriveTrace-Role" = $Role
    "X-DriveTrace-User" = $User
  }
}

function Invoke-Json {
  param(
    [string]$Name,
    [string]$Path,
    [string]$Role = "Administrator",
    [string]$User = "admin",
    [string]$Method = "GET",
    [object]$Body = $null
  )

  $url = "$ApiBaseUrl$Path"
  $headers = New-Headers -Role $Role -User $User
  try {
    if ($Method -eq "GET") {
      $response = Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -TimeoutSec 30
    } else {
      $jsonBody = if ($null -eq $Body) { "{}" } else { $Body | ConvertTo-Json -Depth 12 }
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
    [int]$ExpectedStatus,
    [string]$Role = "Administrator",
    [string]$User = "admin",
    [string]$Method = "GET",
    [object]$Body = $null
  )

  $url = "$ApiBaseUrl$Path"
  $headers = New-Headers -Role $Role -User $User
  try {
    if ($Method -eq "GET") {
      $response = Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -TimeoutSec 30
    } else {
      $jsonBody = if ($null -eq $Body) { "{}" } else { $Body | ConvertTo-Json -Depth 12 }
      $response = Invoke-WebRequest -Uri $url -Method $Method -Headers $headers -Body $jsonBody -ContentType "application/json" -UseBasicParsing -TimeoutSec 30
    }
    Assert-Condition ($response.StatusCode -eq $ExpectedStatus) "$Name returned HTTP $ExpectedStatus." "$Name returned HTTP $($response.StatusCode), expected $ExpectedStatus."
  } catch {
    $statusCode = $null
    if ($_.Exception.Response) {
      try { $statusCode = [int]$_.Exception.Response.StatusCode } catch { }
    }
    Assert-Condition ($statusCode -eq $ExpectedStatus) "$Name returned HTTP $ExpectedStatus." "$Name returned HTTP $statusCode, expected $ExpectedStatus."
  }
}

function Invoke-HttpHealth {
  param(
    [string]$Name,
    [string]$Url,
    [hashtable]$Headers = @{}
  )

  try {
    $response = Invoke-WebRequest -Uri $Url -Headers $Headers -UseBasicParsing -TimeoutSec 20
    Assert-Condition ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) "$Name is reachable." "$Name returned HTTP $($response.StatusCode)."
    return $response
  } catch {
    Register-Failure "$Name is not reachable: $($_.Exception.Message)"
    return $null
  }
}

function Has-JsonProperty {
  param(
    [object]$Object,
    [string]$Name
  )

  if ($null -eq $Object) { return $false }
  return @($Object.PSObject.Properties.Name) -contains $Name
}

function Get-GrafanaAuthHeader {
  $bytes = [System.Text.Encoding]::ASCII.GetBytes("admin:admin")
  return @{ Authorization = "Basic $([Convert]::ToBase64String($bytes))" }
}

Write-Host "DriveTrace Core - Phase D reconditioning smoke test"
Write-Host "---------------------------------------------------"

$apiRoot = $ApiBaseUrl -replace "/api/?$", ""
Invoke-HttpHealth -Name "Swagger JSON" -Url "$apiRoot/swagger/v1/swagger.json" | Out-Null
Invoke-HttpHealth -Name "Dashboard" -Url $DashboardBaseUrl | Out-Null

$profileAdmin = Invoke-Json -Name "auth/me administrator" -Path "/auth/me" -Role "Administrator" -User "admin"
$profileOperator = Invoke-Json -Name "auth/me operator" -Path "/auth/me" -Role "Operator" -User "operador"
$profileCustomer = Invoke-Json -Name "auth/me customer" -Path "/auth/me" -Role "Customer" -User "cliente"

if ($profileAdmin) {
  $permissions = @(Convert-ToArray $profileAdmin.permissions)
  Assert-Condition ($permissions -contains "Reconditioning.Read" -and $permissions -contains "Reconditioning.Write" -and $permissions -contains "Reconditioning.Decide") "Administrator has all reconditioning permissions." "Administrator is missing reconditioning permissions."
}

if ($profileOperator) {
  $permissions = @(Convert-ToArray $profileOperator.permissions)
  Assert-Condition ($permissions -contains "Reconditioning.Read") "Operator can read reconditioning." "Operator lacks Reconditioning.Read."
  Assert-Condition (-not ($permissions -contains "Reconditioning.Decide")) "Operator cannot decide reconditioning." "Operator should not have Reconditioning.Decide."
}

if ($profileCustomer) {
  $permissions = @(Convert-ToArray $profileCustomer.permissions)
  Assert-Condition (-not ($permissions -contains "Reconditioning.Read")) "Customer has no internal reconditioning permission." "Customer should not have Reconditioning.Read."
}

$all = Invoke-Json -Name "Reconditioning list" -Path "/reconditioning" -Role "QualityTechnician" -User "qualidade"
if ($all) {
  $items = @(Convert-ToArray $all.items)
  Assert-Condition ($items.Count -gt 0) "Reconditioning list has items." "Reconditioning list returned no items."
  Assert-Condition ($all.summary.total -ge $items.Count) "Summary total is coherent." "Summary total is lower than returned items."
  Assert-Condition ($all.summary.candidates -ge 1) "Summary exposes recovery candidates." "Summary has no recovery candidates."
  Assert-Condition ($all.summary.reconditioned -ge 1) "Summary exposes reconditioned units." "Summary has no reconditioned units."
  Assert-Condition ($all.summary.scrap -ge 1) "Summary exposes rejected/scrap decisions." "Summary has no scrap decisions."
} else {
  $items = @()
}

$candidatesResponse = Invoke-Json -Name "Reconditioning candidates" -Path "/reconditioning/candidates" -Role "QualityTechnician" -User "qualidade"
if ($candidatesResponse) {
  $candidateItems = @(Convert-ToArray $candidatesResponse.items)
  Assert-Condition ($candidateItems.Count -gt 0) "Candidate endpoint has candidates." "Candidate endpoint returned no candidates."
  $unexpected = $candidateItems | Where-Object { $_.recoveryStatus -eq "Reconditioned" -or $_.qualityDisposition -eq "Scrap" } | Select-Object -First 1
  Assert-Condition ($null -eq $unexpected) "Candidate endpoint excludes closed reconditioned/scrap decisions." "Candidate endpoint returned a closed decision."
}

$candidate = $items | Where-Object { $_.canMarkRecoverable -eq $true -or $_.recoveryStatus -in @("Candidate", "Recoverable", "InRecovery") } | Select-Object -First 1
$reconditioned = $items | Where-Object { $_.isReconditioned -eq $true -or $_.recoveryStatus -eq "Reconditioned" } | Select-Object -First 1
$scrap = $items | Where-Object { $_.scrapRecordId -or $_.qualityDisposition -eq "Scrap" -or $_.unitStatus -eq "Scrap" } | Select-Object -First 1
$critical = $items | Where-Object { ($_.nonconformitySeverity -match "Critical") -or ($_.nonconformitySeverity -match "Crit") } | Select-Object -First 1

Assert-Condition ($null -ne $candidate) "A recoverable candidate exists." "No recoverable candidate was found."
Assert-Condition ($null -ne $reconditioned) "An already reconditioned unit exists." "No already reconditioned unit was found."
Assert-Condition ($null -ne $scrap) "A rejected/scrap recovery decision exists." "No rejected/scrap recovery decision was found."

if ($reconditioned) {
  $detailId = if ($reconditioned.id) { [int]$reconditioned.id } else { [int]$reconditioned.productUnitId }
  $detail = Invoke-Json -Name "Reconditioning detail" -Path "/reconditioning/$detailId" -Role "QualityTechnician" -User "qualidade"
  if ($detail) {
    Assert-Condition ($detail.productUnitId -eq $reconditioned.productUnitId) "Detail returns the selected product unit." "Detail returned an unexpected product unit."
    Assert-Condition ($detail.isReconditioned -eq $true -or $detail.recoveryStatus -eq "Reconditioned") "Detail identifies the unit as reconditioned." "Detail does not identify the unit as reconditioned."
  }
}

$decisionBody = @{
  reason = "Validacao automatica da Fase D"
  notes = "scripts/test-phase-d-reconditioning.ps1"
  functionalValidation = $true
}

$rejectBody = @{
  reason = "Validacao automatica da Fase D"
  nextDisposition = "Scrap"
  notes = "scripts/test-phase-d-reconditioning.ps1"
}

if ($reconditioned) {
  Invoke-ExpectedStatus -Name "Operator cannot complete reconditioning" -Path "/product-units/$([int]$reconditioned.productUnitId)/complete-reconditioning" -Role "Operator" -User "operador" -Method "POST" -ExpectedStatus 403 -Body $decisionBody
  Invoke-ExpectedStatus -Name "Quality cannot complete already reconditioned unit" -Path "/product-units/$([int]$reconditioned.productUnitId)/complete-reconditioning" -Role "QualityTechnician" -User "qualidade" -Method "POST" -ExpectedStatus 400 -Body $decisionBody
  Invoke-ExpectedStatus -Name "Quality cannot reject already reconditioned unit" -Path "/product-units/$([int]$reconditioned.productUnitId)/reject-reconditioning" -Role "QualityTechnician" -User "qualidade" -Method "POST" -ExpectedStatus 400 -Body $rejectBody
}

if ($critical) {
  Invoke-ExpectedStatus -Name "Critical nonconformity cannot be marked reconditionable" -Path "/product-units/$([int]$critical.productUnitId)/mark-reconditionable" -Role "QualityTechnician" -User "qualidade" -Method "POST" -ExpectedStatus 400 -Body $decisionBody
}

Invoke-ExpectedStatus -Name "Customer cannot read reconditioning list" -Path "/reconditioning" -Role "Customer" -User "cliente" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Customer can read public tracking portal" -Path "/customer/orders/TRC-PORTA-001" -Role "Customer" -User "cliente" -ExpectedStatus 200

if ($reconditioned) {
  Invoke-ExpectedStatus -Name "Customer cannot read internal product-unit graph" -Path "/trace-graph/product-unit/$([int]$reconditioned.productUnitId)" -Role "Customer" -User "cliente" -ExpectedStatus 403
  $graph = Invoke-Json -Name "Reconditioned unit trace graph" -Path "/trace-graph/product-unit/$([int]$reconditioned.productUnitId)" -Role "QualityTechnician" -User "qualidade"
  if ($graph) {
    $nodes = @(Convert-ToArray $graph.nodes)
    $edges = @(Convert-ToArray $graph.edges)
    $hasRecord = $nodes | Where-Object { $_.type -eq "ReconditionRecord" } | Select-Object -First 1
    $hasEdge = $edges | Where-Object { $_.type -eq "reconditioning" } | Select-Object -First 1
    Assert-Condition ($null -ne $hasRecord) "Trace graph contains ReconditionRecord node." "Trace graph is missing ReconditionRecord node."
    Assert-Condition ($null -ne $hasEdge) "Trace graph contains reconditioning edge." "Trace graph is missing reconditioning edge."
  }

  $unitEvents = Invoke-Json -Name "Product unit reconditioning events" -Path "/product-units/$([int]$reconditioned.productUnitId)/events?limit=100" -Role "QualityTechnician" -User "qualidade"
  if ($unitEvents) {
    $events = @(Convert-ToArray $unitEvents)
    $hasCompleted = $events | Where-Object { $_.eventType -in @("ReconditioningCompleted", "ProductUnitMarkedReconditioned") } | Select-Object -First 1
    Assert-Condition ($null -ne $hasCompleted) "Product unit events include reconditioning completion." "Product unit events are missing reconditioning completion."
  }
}

$reconditioningEvents = Invoke-Json -Name "Operational reconditioning events" -Path "/operational-events?eventType=ProductUnitMarkedReconditioned&limit=50" -Role "QualityTechnician" -User "qualidade"
if ($reconditioningEvents) {
  $events = @(Convert-ToArray $reconditioningEvents)
  Assert-Condition ($events.Count -ge 1) "Operational events include ProductUnitMarkedReconditioned." "No ProductUnitMarkedReconditioned event found."
  $withRecord = $events | Where-Object { $null -ne $_.reconditionRecord } | Select-Object -First 1
  Assert-Condition ($null -ne $withRecord) "Reconditioning event links to ReconditionRecord." "Reconditioning event has no ReconditionRecord link."
}

if ($PublishFiware) {
  $publish = Invoke-Json -Name "Publish current FIWARE context" -Path "/fiware/publish-current" -Role "Administrator" -User "admin" -Method "POST" -Body @{}
  Assert-Condition ($null -ne $publish) "FIWARE publish-current responded." "FIWARE publish-current did not respond."
}

$fiwareContext = Invoke-Json -Name "FIWARE context" -Path "/fiware/context" -Role "DemoViewer" -User "demo"
if ($fiwareContext) {
  Assert-Condition ($fiwareContext.relationalSnapshotCount -gt 0) "FIWARE context exposes relational snapshot count." "FIWARE context has no relational snapshot count."
  $entities = @(Convert-ToArray $fiwareContext.entities)
  $productUnitEntity = $entities | Where-Object { $_.type -eq "ProductUnit" -and $_.id -eq "urn:ngsi-ld:ProductUnit:UP-PORTA-011" } | Select-Object -First 1
  if (-not $productUnitEntity) {
    $productUnitEntity = $entities | Where-Object { $_.type -eq "ProductUnit" -and (Has-JsonProperty -Object $_.attributes -Name "isReconditioned") } | Select-Object -First 1
  }
  Assert-Condition ($null -ne $productUnitEntity) "FIWARE context contains ProductUnit entities." "FIWARE context is missing ProductUnit entities."
  if ($PublishFiware -and $productUnitEntity) {
    Assert-Condition (Has-JsonProperty -Object $productUnitEntity.attributes -Name "isReconditioned") "FIWARE ProductUnit includes isReconditioned." "FIWARE ProductUnit is missing isReconditioned."
    Assert-Condition (Has-JsonProperty -Object $productUnitEntity.attributes -Name "qualityDisposition") "FIWARE ProductUnit includes qualityDisposition." "FIWARE ProductUnit is missing qualityDisposition."
    Assert-Condition (Has-JsonProperty -Object $productUnitEntity.attributes -Name "recoveryStatus") "FIWARE ProductUnit includes recoveryStatus." "FIWARE ProductUnit is missing recoveryStatus."
  }
}

$grafanaHeaders = Get-GrafanaAuthHeader
Invoke-HttpHealth -Name "Grafana health" -Url "$GrafanaBaseUrl/api/health" -Headers $grafanaHeaders | Out-Null
Invoke-HttpHealth -Name "Grafana datasource DriveTrace TimescaleDB" -Url "$GrafanaBaseUrl/api/datasources/uid/drivetrace-timescaledb" -Headers $grafanaHeaders | Out-Null
$qualityDashboardResponse = Invoke-HttpHealth -Name "Grafana Quality Traceability dashboard" -Url "$GrafanaBaseUrl/api/dashboards/uid/drivetrace-quality-traceability" -Headers $grafanaHeaders
if ($qualityDashboardResponse) {
  try {
    $qualityDashboard = $qualityDashboardResponse.Content | ConvertFrom-Json
    $panels = @(Convert-ToArray $qualityDashboard.dashboard.panels)
    $hasPanel12 = $panels | Where-Object { $_.id -eq 12 } | Select-Object -First 1
    $hasPanel13 = $panels | Where-Object { $_.id -eq 13 } | Select-Object -First 1
    Assert-Condition ($null -ne $hasPanel12) "Grafana dashboard includes recovery status panel." "Grafana dashboard is missing panel 12."
    Assert-Condition ($null -ne $hasPanel13) "Grafana dashboard includes recovered units table." "Grafana dashboard is missing panel 13."
  } catch {
    Register-Failure "Could not parse Grafana quality dashboard JSON."
  }
}

if ($script:FailedChecks -gt 0) {
  Write-Host ""
  Write-Host "Phase D reconditioning smoke test finished with $($script:FailedChecks) failure(s)."
  exit 1
}

Write-Host ""
Write-Host "Phase D reconditioning smoke test finished successfully."
