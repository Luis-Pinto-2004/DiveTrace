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

function Convert-ToArray {
  param([object]$Value)
  if ($null -eq $Value) { return @() }
  return @($Value)
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

function Assert-Graph {
  param(
    [object]$Graph,
    [string]$Name,
    [string[]]$RequiredNodeTypes,
    [string[]]$RequiredEdgeTypes
  )

  Assert-Condition ($null -ne $Graph) "$Name returned a graph object." "$Name did not return a graph object."
  if (-not $Graph) { return }

  $nodes = @(Convert-ToArray $Graph.nodes)
  $edges = @(Convert-ToArray $Graph.edges)
  $legend = @(Convert-ToArray $Graph.legend)
  Assert-Condition ($nodes.Count -gt 0) "$Name has nodes." "$Name returned no nodes."
  Assert-Condition ($edges.Count -gt 0) "$Name has edges." "$Name returned no edges."
  Assert-Condition ($legend.Count -gt 0) "$Name has legend items." "$Name returned no legend items."
  Assert-Condition (-not [string]::IsNullOrWhiteSpace($Graph.summary.recommendation)) "$Name has an operational recommendation." "$Name has no summary recommendation."

  foreach ($type in $RequiredNodeTypes) {
    $match = $nodes | Where-Object { $_.type -eq $type } | Select-Object -First 1
    Assert-Condition ($null -ne $match) "$Name contains node type $type." "$Name is missing node type $type."
  }

  foreach ($type in $RequiredEdgeTypes) {
    $match = $edges | Where-Object { $_.type -eq $type } | Select-Object -First 1
    Assert-Condition ($null -ne $match) "$Name contains edge type $type." "$Name is missing edge type $type."
  }
}

function Get-GrafanaAuthHeader {
  $bytes = [System.Text.Encoding]::ASCII.GetBytes("admin:admin")
  return @{ Authorization = "Basic $([Convert]::ToBase64String($bytes))" }
}

Write-Host "DriveTrace Core - Phase C trace graph smoke test"
Write-Host "------------------------------------------------"

$apiRoot = $ApiBaseUrl -replace "/api/?$", ""
Invoke-HttpHealth -Name "Swagger JSON" -Url "$apiRoot/swagger/v1/swagger.json" | Out-Null
Invoke-HttpHealth -Name "Dashboard" -Url $DashboardBaseUrl | Out-Null

$adminOptions = Invoke-Json -Name "Trace graph options as admin" -Path "/trace-graph/options"
if ($adminOptions) {
  $productUnits = @(Convert-ToArray $adminOptions.productUnits)
  $orders = @(Convert-ToArray $adminOptions.manufacturingOrders)
  $modes = @(Convert-ToArray $adminOptions.supportedModes)
  Assert-Condition ($productUnits.Count -gt 0) "Options expose product units." "Options expose no product units."
  Assert-Condition ($orders.Count -gt 0) "Options expose manufacturing orders." "Options expose no manufacturing orders."
  foreach ($mode in @("factory", "product-unit", "order")) {
    Assert-Condition ($modes -contains $mode) "Options support mode $mode." "Options do not support mode $mode."
  }
}

$factory = Invoke-Json -Name "Factory trace graph as admin" -Path "/trace-graph/factory"
Assert-Graph -Graph $factory -Name "Factory graph" -RequiredNodeTypes @("ProductionLine", "Section", "ProductUnit") -RequiredEdgeTypes @("contains", "route", "current-location")

$firstUnitId = $null
if ($adminOptions) {
  $firstUnit = @(Convert-ToArray $adminOptions.productUnits) | Select-Object -First 1
  if ($firstUnit) { $firstUnitId = [int]$firstUnit.id }
}

if ($firstUnitId) {
  $unitGraph = Invoke-Json -Name "Product-unit trace graph as admin" -Path "/trace-graph/product-unit/$firstUnitId"
  Assert-Graph -Graph $unitGraph -Name "Product-unit graph" -RequiredNodeTypes @("ProductUnit", "Section") -RequiredEdgeTypes @("movement")
} else {
  Register-Failure "Could not select a product unit for product-unit graph validation."
}

$firstOrderId = $null
if ($adminOptions) {
  $firstOrder = @(Convert-ToArray $adminOptions.manufacturingOrders) | Select-Object -First 1
  if ($firstOrder) { $firstOrderId = [int]$firstOrder.id }
}

if ($firstOrderId) {
  $orderGraph = Invoke-Json -Name "Manufacturing-order trace graph as admin" -Path "/trace-graph/manufacturing-order/$firstOrderId"
  Assert-Graph -Graph $orderGraph -Name "Manufacturing-order graph" -RequiredNodeTypes @("ManufacturingOrder", "ProductUnit") -RequiredEdgeTypes @("produces")
} else {
  Register-Failure "Could not select a manufacturing order for order graph validation."
}

Invoke-ExpectedStatus -Name "Customer cannot read factory graph" -Path "/trace-graph/factory" -Role "Customer" -User "cliente" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Customer cannot read product-unit graph" -Path "/trace-graph/product-unit/1" -Role "Customer" -User "cliente" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Operator can read scoped factory graph" -Path "/trace-graph/factory" -Role "Operator" -User "operador" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Supervisor cannot read FIWARE graph status" -Path "/trace-graph/fiware-status" -Role "Supervisor" -User "supervisor" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Demo viewer can read FIWARE graph status" -Path "/trace-graph/fiware-status" -Role "DemoViewer" -User "demo" -ExpectedStatus 200

$operatorOptions = Invoke-Json -Name "Trace graph options as operator" -Path "/trace-graph/options" -Role "Operator" -User "operador"
if ($operatorOptions) {
  $operatorUnits = @(Convert-ToArray $operatorOptions.productUnits)
  Assert-Condition ($operatorUnits.Count -gt 0) "Operator receives scoped product-unit options." "Operator received no scoped product-unit options."
  $operatorUnit = $operatorUnits | Select-Object -First 1
  if ($operatorUnit) {
    Invoke-ExpectedStatus -Name "Operator can read scoped product-unit graph" -Path "/trace-graph/product-unit/$([int]$operatorUnit.id)" -Role "Operator" -User "operador" -ExpectedStatus 200
  }
}

$customerOptions = Invoke-Json -Name "Trace graph options as customer" -Path "/trace-graph/options" -Role "Customer" -User "cliente"
if ($customerOptions) {
  $customerModes = @(Convert-ToArray $customerOptions.supportedModes)
  $customerOrders = @(Convert-ToArray $customerOptions.manufacturingOrders)
  Assert-Condition ($customerModes.Count -eq 1 -and $customerModes[0] -eq "order") "Customer options are limited to order mode." "Customer options expose modes outside order."
  Assert-Condition ($customerOrders.Count -gt 0) "Customer receives own order options." "Customer received no order options."
  $customerOrder = $customerOrders | Select-Object -First 1
  if ($customerOrder) {
    Invoke-ExpectedStatus -Name "Customer can read own order graph" -Path "/trace-graph/manufacturing-order/$([int]$customerOrder.id)" -Role "Customer" -User "cliente" -ExpectedStatus 200
  }
}

$fiwareStatus = Invoke-Json -Name "Trace graph FIWARE status" -Path "/trace-graph/fiware-status" -Role "DemoViewer" -User "demo"
if ($fiwareStatus) {
  Assert-Condition ($fiwareStatus.relationalSnapshotCount -gt 0) "FIWARE graph status exposes relational snapshot count." "FIWARE graph status has no relational snapshot count."
}

if ($PublishFiware) {
  $publish = Invoke-Json -Name "Publish current FIWARE context" -Path "/fiware/publish-current" -Role "Administrator" -User "admin" -Method "POST" -Body @{}
  Assert-Condition ($null -ne $publish) "FIWARE publish-current responded." "FIWARE publish-current did not respond."

  $postPublishStatus = Invoke-Json -Name "Trace graph FIWARE status after publish" -Path "/trace-graph/fiware-status" -Role "Administrator" -User "admin"
  if ($postPublishStatus) {
    Assert-Condition ($postPublishStatus.entityCount -ge 0) "FIWARE status after publish exposes entity count." "FIWARE status after publish does not expose entity count."
    Assert-Condition ($postPublishStatus.relationalSnapshotCount -gt 0) "FIWARE status after publish exposes relational snapshot." "FIWARE status after publish has no relational snapshot."
  }
}

$grafanaHeaders = Get-GrafanaAuthHeader
Invoke-HttpHealth -Name "Grafana health" -Url "$GrafanaBaseUrl/api/health" -Headers $grafanaHeaders | Out-Null
Invoke-HttpHealth -Name "Grafana datasource DriveTrace TimescaleDB" -Url "$GrafanaBaseUrl/api/datasources/uid/drivetrace-timescaledb" -Headers $grafanaHeaders | Out-Null
Invoke-HttpHealth -Name "Grafana WIP Operations dashboard" -Url "$GrafanaBaseUrl/api/dashboards/uid/drivetrace-wip-operations" -Headers $grafanaHeaders | Out-Null
Invoke-HttpHealth -Name "Grafana Quality Traceability dashboard" -Url "$GrafanaBaseUrl/api/dashboards/uid/drivetrace-quality-traceability" -Headers $grafanaHeaders | Out-Null

if ($script:FailedChecks -gt 0) {
  Write-Host ""
  Write-Host "Phase C trace graph smoke test finished with $($script:FailedChecks) failure(s)."
  exit 1
}

Write-Host ""
Write-Host "Phase C trace graph smoke test finished successfully."
