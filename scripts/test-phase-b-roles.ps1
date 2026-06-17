param(
  [string]$ApiBaseUrl = "http://localhost:5181/api",
  [string]$DashboardBaseUrl = "http://localhost:8088",
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
    [string]$Role,
    [string]$User,
    [string]$Method = "GET",
    [object]$Body = $null
  )

  $url = "$ApiBaseUrl$Path"
  $headers = New-Headers -Role $Role -User $User
  try {
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
    [string]$Role,
    [string]$User,
    [int]$ExpectedStatus,
    [string]$Method = "GET",
    [object]$Body = $null
  )

  $url = "$ApiBaseUrl$Path"
  $headers = New-Headers -Role $Role -User $User
  try {
    if ($Method -eq "GET") {
      $response = Invoke-WebRequest -Uri $url -Headers $headers -UseBasicParsing -TimeoutSec 30
    } else {
      $jsonBody = if ($null -eq $Body) { "{}" } else { $Body | ConvertTo-Json -Depth 10 }
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

function Assert-Permissions {
  param(
    [object]$Profile,
    [string[]]$Required,
    [string[]]$Forbidden
  )

  $permissions = @($Profile.permissions)
  foreach ($permission in $Required) {
    Assert-Condition ($permissions -contains $permission) "$($Profile.username) has $permission." "$($Profile.username) lacks $permission."
  }
  foreach ($permission in $Forbidden) {
    Assert-Condition (-not ($permissions -contains $permission)) "$($Profile.username) does not have $permission." "$($Profile.username) should not have $permission."
  }
}

Write-Host "DriveTrace Core - Phase B role and permission smoke test"
Write-Host "--------------------------------------------------------"

try {
  $dashboardResponse = Invoke-WebRequest -Uri $DashboardBaseUrl -UseBasicParsing -TimeoutSec 20
  Assert-Condition ($dashboardResponse.StatusCode -ge 200 -and $dashboardResponse.StatusCode -lt 400) "Dashboard is reachable." "Dashboard returned HTTP $($dashboardResponse.StatusCode)."
} catch {
  Register-Failure "Dashboard is not reachable: $($_.Exception.Message)"
}

$demoUsers = Invoke-Json -Name "Demo users" -Path "/auth/demo-users" -Role "Administrator" -User "admin"
if ($demoUsers) {
  $usernames = @($demoUsers | ForEach-Object { $_.username })
  foreach ($username in @("admin", "supervisor", "operador", "qualidade", "logistica", "cliente")) {
    Assert-Condition ($usernames -contains $username) "Demo user $username exists." "Demo user $username is missing."
  }
  Assert-Condition (-not ($usernames -contains "demo")) "Demo user is no longer exposed." "Demo user should not be exposed by /auth/demo-users."
}

$profiles = @(
  @{ User = "admin"; Role = "Administrator"; RoleKey = "admin"; Required = @("Users.Manage", "Fiware.Manage", "Racks.Manage"); Forbidden = @() },
  @{ User = "supervisor"; Role = "Supervisor"; RoleKey = "supervisor"; Required = @("Orders.Manage", "ProductUnits.Transfer", "Grafana.View"); Forbidden = @("Fiware.View", "Users.Manage") },
  @{ User = "operador"; Role = "Operator"; RoleKey = "operator"; Required = @("ProductUnits.Transfer", "ProductUnits.Trace"); Forbidden = @("Fiware.View", "Racks.View", "Materials.View") },
  @{ User = "qualidade"; Role = "QualityTechnician"; RoleKey = "quality"; Required = @("Quality.View", "Quality.Record", "Quality.Decide"); Forbidden = @("Racks.View", "Fiware.View") },
  @{ User = "logistica"; Role = "Logistics"; RoleKey = "logistics"; Required = @("Racks.View", "Racks.Manage", "Materials.Manage"); Forbidden = @("Quality.View", "Fiware.View") },
  @{ User = "cliente"; Role = "Customer"; RoleKey = "client"; Required = @("CustomerPortal.View"); Forbidden = @("ProductUnits.View", "ProductUnits.Trace", "Fiware.View", "Grafana.View") }
)

foreach ($profileCase in $profiles) {
  $me = Invoke-Json -Name "auth/me $($profileCase.User)" -Path "/auth/me" -Role $profileCase.Role -User $profileCase.User
  if ($me) {
    Assert-Condition ($me.roleKey -eq $profileCase.RoleKey) "$($profileCase.User) resolves roleKey $($profileCase.RoleKey)." "$($profileCase.User) roleKey was $($me.roleKey), expected $($profileCase.RoleKey)."
    Assert-Permissions -Profile $me -Required $profileCase.Required -Forbidden $profileCase.Forbidden
  }
}

Invoke-ExpectedStatus -Name "Admin can read FIWARE" -Path "/fiware/context" -Role "Administrator" -User "admin" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Supervisor can read dashboard summary" -Path "/dashboard/summary" -Role "Supervisor" -User "supervisor" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Supervisor cannot read FIWARE" -Path "/fiware/context" -Role "Supervisor" -User "supervisor" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Operator can read workbench" -Path "/operator/workbench" -Role "Operator" -User "operador" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Operator cannot read racks" -Path "/racks" -Role "Operator" -User "operador" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Quality can read quality results" -Path "/quality-results" -Role "QualityTechnician" -User "qualidade" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Quality cannot read racks" -Path "/racks" -Role "QualityTechnician" -User "qualidade" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Logistics can read racks" -Path "/racks" -Role "Logistics" -User "logistica" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Logistics can read materials" -Path "/raw-materials" -Role "Logistics" -User "logistica" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Logistics cannot read quality" -Path "/quality-results" -Role "Logistics" -User "logistica" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Customer can read public order" -Path "/customer/orders/TRC-PORTA-001" -Role "Customer" -User "cliente" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Customer can list own orders" -Path "/customer/orders" -Role "Customer" -User "cliente" -ExpectedStatus 200
Invoke-ExpectedStatus -Name "Customer cannot read another customer order" -Path "/customer/orders/TRC-SIM-002" -Role "Customer" -User "cliente" -ExpectedStatus 404
Invoke-ExpectedStatus -Name "Customer cannot read dashboard summary" -Path "/dashboard/summary" -Role "Customer" -User "cliente" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Customer cannot read product units" -Path "/product-units" -Role "Customer" -User "cliente" -ExpectedStatus 403
Invoke-ExpectedStatus -Name "Customer cannot read FIWARE" -Path "/fiware/context" -Role "Customer" -User "cliente" -ExpectedStatus 403

$createdCustomerOrder = Invoke-Json -Name "Customer can create demo order" -Path "/customer/orders" -Role "Customer" -User "cliente" -Method "POST" -Body @{
  quantity = 1
  observations = "Pedido criado pelo smoke test de cliente."
}
if ($createdCustomerOrder) {
  Assert-Condition (-not [string]::IsNullOrWhiteSpace($createdCustomerOrder.publicTrackingCode)) "Customer order creation returns a public tracking code." "Customer order creation did not return a public tracking code."
}

if ($PublishFiware) {
  Invoke-ExpectedStatus -Name "Admin can publish FIWARE current context" -Path "/fiware/publish-current" -Role "Administrator" -User "admin" -Method "POST" -ExpectedStatus 200 -Body @{}
  Invoke-ExpectedStatus -Name "Operator cannot publish FIWARE current context" -Path "/fiware/publish-current" -Role "Operator" -User "operador" -Method "POST" -ExpectedStatus 403 -Body @{}
}

if ($script:FailedChecks -gt 0) {
  Write-Host ""
  Write-Host "Phase B role smoke test finished with $($script:FailedChecks) failure(s)."
  exit 1
}

Write-Host ""
Write-Host "Phase B role smoke test finished successfully."
