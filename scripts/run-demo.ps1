<#
.SYNOPSIS
  Arranca o DriveTrace Core e abre a aplicacao em modo de apresentacao (demo).

.DESCRIPTION
  Arranca o ambiente Docker (sem apagar dados), espera pela API e abre o browser
  diretamente no modo de apresentacao, autenticado como Administrador. Os passos
  guiados aparecem no rodape da aplicacao (Anterior / Reproduzir / Seguinte / Sair).

.PARAMETER NoBuild
  Arranca os servicos sem reconstruir as imagens (mais rapido).

.EXAMPLE
  ./scripts/run-demo.ps1
  ./scripts/run-demo.ps1 -NoBuild
#>
param(
  [switch]$NoBuild,
  [string]$DashboardUrl = "http://localhost:8088/login?demo=1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Raiz do projeto (uma pasta acima de /scripts)
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
  Write-Host "== DriveTrace Core - Demo de apresentacao ==" -ForegroundColor Cyan

  # 1) Arrancar o ambiente. Nao destrutivo: mantem volumes e dados.
  if ($NoBuild) {
    Write-Host "A arrancar os servicos (sem reconstruir imagens)..."
    docker compose up -d
  } else {
    Write-Host "A arrancar e reconstruir os servicos..."
    docker compose up -d --build
  }

  # 2) Esperar que a API fique pronta (semeia a base de dados no 1.o arranque).
  & "$PSScriptRoot/wait-api.ps1"

  # 3) Abrir o browser em modo de apresentacao (perfil Administrador, autologin).
  Write-Host "A abrir a aplicacao em modo de apresentacao..."
  Write-Host "  $DashboardUrl"
  Start-Process $DashboardUrl

  Write-Host ""
  Write-Host "Apresentacao pronta." -ForegroundColor Green
  Write-Host "Use os botoes no rodape: Anterior / Reproduzir / Seguinte / Sair."
  Write-Host ""
  Write-Host "Enderecos uteis:"
  Write-Host "  Dashboard : http://localhost:8088"
  Write-Host "  Grafana   : http://localhost:3000  (admin/admin)"
  Write-Host "  FIWARE    : http://localhost:1026/version"
  Write-Host "  Guiao     : docs/DEMO.md"
}
finally {
  Pop-Location
}
