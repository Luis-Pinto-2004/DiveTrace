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

function Invoke-Health {
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

function Get-GrafanaAuthHeader {
  $bytes = [System.Text.Encoding]::ASCII.GetBytes("admin:admin")
  return @{ Authorization = "Basic $([Convert]::ToBase64String($bytes))" }
}

function Has-JsonProperty {
  param(
    [object]$Object,
    [string]$Name
  )

  if ($null -eq $Object) { return $false }
  return @($Object.PSObject.Properties.Name) -contains $Name
}

function Complete-ScenarioRun {
  param(
    [string]$ScenarioKey,
    [string]$RunName,
    [string]$Role,
    [string]$User,
    [bool]$PauseResume = $false
  )

  $scenarios = Invoke-Json -Name "Scenario list for $ScenarioKey" -Path "/simulation/scenarios" -Role $Role -User $User
  if (-not $scenarios) { return $null }
  $scenario = @($scenarios) | Where-Object { $_.key -eq $ScenarioKey } | Select-Object -First 1
  Assert-Condition ($null -ne $scenario) "Scenario '$ScenarioKey' exists." "Scenario '$ScenarioKey' not found."
  if (-not $scenario) { return $null }

  $run = Invoke-Json -Name "Create run for $ScenarioKey" -Path "/simulation/runs" -Role $Role -User $User -Method "POST" -Body @{
    scenarioKey = $ScenarioKey
    name = $RunName
    speed = "Manual"
  }
  if (-not $run) { return $null }

  Assert-Condition ($run.scenarioKey -eq $ScenarioKey) "Run created for $ScenarioKey." "Run was created for an unexpected scenario."
  Assert-Condition ($run.status -eq "Running") "Run started in Running state." "Run did not start as Running."

  $firstTick = Invoke-Json -Name "First tick for $ScenarioKey" -Path "/simulation/runs/$($run.id)/tick" -Role $Role -User $User -Method "POST" -Body @{}
  if (-not $firstTick) { return $null }
  Assert-Condition ($firstTick.currentStep -ge 1) "Tick advanced scenario '$ScenarioKey'." "Tick did not advance scenario '$ScenarioKey'."

  if ($PauseResume) {
    Invoke-ExpectedStatus -Name "Operator cannot pause simulation" -Path "/simulation/runs/$($run.id)/pause" -Role "Operator" -User "operador" -Method "POST" -ExpectedStatus 403

    $paused = Invoke-Json -Name "Pause scenario $ScenarioKey" -Path "/simulation/runs/$($run.id)/pause" -Role "Supervisor" -User "supervisor" -Method "POST" -Body @{}
    if (-not $paused) { return $null }
    Assert-Condition ($paused.status -eq "Paused") "Pause endpoint paused the run." "Pause endpoint did not pause the run."

    $resumed = Invoke-Json -Name "Resume scenario $ScenarioKey" -Path "/simulation/runs/$($run.id)/resume" -Role "Supervisor" -User "supervisor" -Method "POST" -Body @{}
    if (-not $resumed) { return $null }
    Assert-Condition ($resumed.status -eq "Running") "Resume endpoint resumed the run." "Resume endpoint did not resume the run."
  }

  $current = $firstTick
  while ($current.status -eq "Running" -and $current.currentStep -lt $current.stepCount) {
    $current = Invoke-Json -Name "Advance scenario $ScenarioKey" -Path "/simulation/runs/$($run.id)/tick" -Role $Role -User $User -Method "POST" -Body @{}
    if (-not $current) { return $null }
  }

  Assert-Condition ($current.status -eq "Completed") "Scenario '$ScenarioKey' completed." "Scenario '$ScenarioKey' did not complete."
  Assert-Condition ($current.currentStep -eq $current.stepCount) "Scenario '$ScenarioKey' reached all steps." "Scenario '$ScenarioKey' did not reach the expected step count."
  return $current
}

Write-Host "DriveTrace Core - Phase E production simulator smoke test"
Write-Host "---------------------------------------------------------"

$apiRoot = $ApiBaseUrl -replace "/api/?$", ""
Invoke-Health -Name "Swagger JSON" -Url "$apiRoot/swagger/v1/swagger.json" | Out-Null
Invoke-Health -Name "Dashboard" -Url $DashboardBaseUrl | Out-Null

$adminProfile = Invoke-Json -Name "auth/me administrator" -Path "/auth/me" -Role "Administrator" -User "admin"
$operatorProfile = Invoke-Json -Name "auth/me operator" -Path "/auth/me" -Role "Operator" -User "operador"
$customerProfile = Invoke-Json -Name "auth/me customer" -Path "/auth/me" -Role "Customer" -User "cliente"

if ($adminProfile) {
  $adminPermissions = @(Convert-ToArray $adminProfile.permissions)
  Assert-Condition ($adminPermissions -contains "Simulation.Read" -and $adminPermissions -contains "Simulation.Run" -and $adminPermissions -contains "Simulation.Manage") "Administrator has all simulation permissions." "Administrator is missing simulation permissions."
}

if ($operatorProfile) {
  $operatorPermissions = @(Convert-ToArray $operatorProfile.permissions)
  Assert-Condition ($operatorPermissions -contains "Simulation.Read") "Operator can read simulation." "Operator lacks Simulation.Read."
  Assert-Condition ($operatorPermissions -contains "Simulation.Run") "Operator can run simulation steps." "Operator lacks Simulation.Run."
}

if ($customerProfile) {
  $customerPermissions = @(Convert-ToArray $customerProfile.permissions)
  Assert-Condition (-not ($customerPermissions -contains "Simulation.Read")) "Customer has no internal simulation permission." "Customer should not have Simulation.Read."
}

Invoke-ExpectedStatus -Name "Customer cannot access scenarios" -Path "/simulation/scenarios" -Role "Customer" -User "cliente" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Customer cannot access state" -Path "/simulation/state" -Role "Customer" -User "cliente" -ExpectedStatus 403

$scenarioState = Invoke-Json -Name "Simulation state" -Path "/simulation/state" -Role "Operator" -User "operador"
if ($scenarioState) {
  $stateScenarios = @(Convert-ToArray $scenarioState.scenarios)
  Assert-Condition ($stateScenarios.Count -ge 3) "Simulation state exposes at least three scenarios." "Simulation state exposes fewer than three scenarios."
}

$scenarioList = Invoke-Json -Name "Scenario catalog" -Path "/simulation/scenarios" -Role "Supervisor" -User "supervisor"
if ($scenarioList) {
  $scenarioItems = @(Convert-ToArray $scenarioList)
  Assert-Condition ($scenarioItems.Count -ge 3) "Scenarios endpoint returns at least three scenarios." "Scenarios endpoint returned fewer than three scenarios."
}

$normalRun = Complete-ScenarioRun -ScenarioKey "normal-flow" -RunName "Fase E - fluxo normal" -Role "Supervisor" -User "supervisor" -PauseResume $true
$minorRun = Complete-ScenarioRun -ScenarioKey "minor-recovery" -RunName "Fase E - falha menor" -Role "Supervisor" -User "supervisor"
$criticalRun = Complete-ScenarioRun -ScenarioKey "critical-scrap" -RunName "Fase E - falha critica" -Role "Operator" -User "operador"

if ($normalRun) {
  $normalSteps = @(Convert-ToArray $normalRun.steps)
  Assert-Condition ($normalSteps.Count -ge 1) "Normal-flow run produced step history." "Normal-flow run produced no step history."
  Assert-Condition ($normalRun.qualityDisposition -eq "Normal") "Normal-flow finished without quality disposition change." "Normal-flow finished with an unexpected quality disposition."
  Assert-Condition ($normalRun.recoveryStatus -eq "None") "Normal-flow finished without recovery state." "Normal-flow finished with an unexpected recovery state."
}

if ($minorRun) {
  Assert-Condition ($minorRun.qualityDisposition -eq "Reconditioned") "Minor scenario ended reconditioned." "Minor scenario did not end as reconditioned."
  Assert-Condition ($minorRun.recoveryStatus -eq "Reconditioned") "Minor scenario recovery state is reconditioned." "Minor scenario recovery state is unexpected."
  Assert-Condition ($minorRun.isReconditioned -eq $true) "Minor scenario flagged the unit as reconditioned." "Minor scenario did not flag the unit as reconditioned."
}

if ($criticalRun) {
  Assert-Condition ($criticalRun.qualityDisposition -eq "Scrap") "Critical scenario ended in scrap." "Critical scenario did not end in scrap."
  Assert-Condition ($criticalRun.recoveryStatus -eq "Rejected") "Critical scenario recovery state is rejected." "Critical scenario recovery state is unexpected."
}

$runs = Invoke-Json -Name "Simulation runs" -Path "/simulation/runs" -Role "Supervisor" -User "supervisor"
if ($runs) {
  $runItems = @(Convert-ToArray $runs)
  Assert-Condition ($runItems.Count -ge 3) "Simulation runs list includes created runs." "Simulation runs list does not include the created runs."
}

$stateAfter = Invoke-Json -Name "Simulation state after runs" -Path "/simulation/state" -Role "Supervisor" -User "supervisor"
if ($stateAfter) {
  $activeRuns = @(Convert-ToArray $stateAfter.activeRuns)
  Assert-Condition ($activeRuns.Count -eq 0) "No simulation run remains active after completion." "There are still active simulation runs."
  Assert-Condition ($stateAfter.unitsInMotion -eq 0) "No units remain in motion after completion." "Units are still in motion after completion."
  $recentRuns = @(Convert-ToArray $stateAfter.recentRuns)
  Assert-Condition ($recentRuns.Count -ge 3) "Recent runs include all simulator scenarios." "Recent runs do not include all simulator scenarios."
}

if ($normalRun) {
  $graphUnit = Invoke-Json -Name "Trace graph for normal unit" -Path "/trace-graph/product-unit/$($normalRun.productUnitId)" -Role "Supervisor" -User "supervisor"
  if ($graphUnit) {
    $nodes = @(Convert-ToArray $graphUnit.nodes)
    $edges = @(Convert-ToArray $graphUnit.edges)
    Assert-Condition ($nodes.Count -gt 0) "Product-unit graph returns nodes." "Product-unit graph returned no nodes."
    Assert-Condition ($edges.Count -gt 0) "Product-unit graph returns edges." "Product-unit graph returned no edges."
  }

  $graphOrder = Invoke-Json -Name "Trace graph for normal order" -Path "/trace-graph/manufacturing-order/$($normalRun.manufacturingOrderId)" -Role "Supervisor" -User "supervisor"
  if ($graphOrder) {
    $nodes = @(Convert-ToArray $graphOrder.nodes)
    $edges = @(Convert-ToArray $graphOrder.edges)
    Assert-Condition ($nodes.Count -gt 0) "Order graph returns nodes." "Order graph returned no nodes."
    Assert-Condition ($edges.Count -gt 0) "Order graph returns edges." "Order graph returned no edges."
  }
}

Invoke-ExpectedStatus -Name "Customer cannot access simulator list" -Path "/simulation/runs" -Role "Customer" -User "cliente" -ExpectedStatus 403

if ($PublishFiware) {
  $publish = Invoke-Json -Name "Publish current FIWARE context" -Path "/fiware/publish-current" -Role "Administrator" -User "admin" -Method "POST" -Body @{}
  Assert-Condition ($null -ne $publish) "FIWARE publish-current responded." "FIWARE publish-current did not respond."

  $fiwareContext = Invoke-Json -Name "FIWARE context" -Path "/fiware/context" -Role "DemoViewer" -User "admin"
  if ($fiwareContext) {
    Assert-Condition ($fiwareContext.entityCount -eq $fiwareContext.relationalSnapshotCount) "FIWARE entity count matches relational snapshot count." "FIWARE entity count does not match relational snapshot count."
    $entities = @(Convert-ToArray $fiwareContext.entities)

    if ($normalRun) {
      $unitEntity = $entities | Where-Object { $_.type -eq "ProductUnit" -and $_.id -eq "urn:ngsi-ld:ProductUnit:$($normalRun.unitCode)" } | Select-Object -First 1
      Assert-Condition ($null -ne $unitEntity) "FIWARE includes the normal-flow ProductUnit." "FIWARE is missing the normal-flow ProductUnit."
    }

    if ($minorRun) {
      $recondEntity = $entities | Where-Object { $_.type -eq "ProductUnit" -and $_.id -eq "urn:ngsi-ld:ProductUnit:$($minorRun.unitCode)" } | Select-Object -First 1
      if ($recondEntity -and $recondEntity.attributes) {
        Assert-Condition (Has-JsonProperty -Object $recondEntity.attributes -Name "isReconditioned") "FIWARE marks the minor-flow unit as reconditioned." "FIWARE does not mark the minor-flow unit as reconditioned."
      } else {
        Register-Failure "FIWARE is missing the minor-flow ProductUnit."
      }
    }

    if ($criticalRun) {
      $scrapEntity = $entities | Where-Object { $_.type -eq "ProductUnit" -and $_.id -eq "urn:ngsi-ld:ProductUnit:$($criticalRun.unitCode)" } | Select-Object -First 1
      if ($scrapEntity -and $scrapEntity.attributes) {
        Assert-Condition (Has-JsonProperty -Object $scrapEntity.attributes -Name "qualityDisposition") "FIWARE marks the critical-flow unit as scrap." "FIWARE does not mark the critical-flow unit as scrap."
      } else {
        Register-Failure "FIWARE is missing the critical-flow ProductUnit."
      }
    }
  }
}

$grafanaHeaders = Get-GrafanaAuthHeader
Invoke-Health -Name "Grafana health" -Url "$GrafanaBaseUrl/api/health" -Headers $grafanaHeaders | Out-Null
Invoke-Health -Name "Grafana datasource DriveTrace TimescaleDB" -Url "$GrafanaBaseUrl/api/datasources/uid/drivetrace-timescaledb" -Headers $grafanaHeaders | Out-Null
Invoke-Health -Name "Grafana quality dashboard" -Url "$GrafanaBaseUrl/api/dashboards/uid/drivetrace-quality-traceability" -Headers $grafanaHeaders | Out-Null

if ($script:FailedChecks -gt 0) {
  Write-Host ""
  Write-Host "Phase E production simulator smoke test finished with $($script:FailedChecks) failure(s)."
  exit 1
}

Write-Host ""
Write-Host "Phase E production simulator smoke test finished successfully."
