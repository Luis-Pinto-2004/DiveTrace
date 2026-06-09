# DriveTrace Core
DRIVOLUTION WP3 — WIP Traceability and Monitoring Platform

DriveTrace Core é uma plataforma académica para rastreabilidade e monitorização WIP em contexto automóvel. A unidade rastreável é a `ProductUnit`, o `Support` é a âncora física intra-linha e a `Rack` representa logística pós-linha. A stack combina FIWARE/Orion-LD, ASP.NET Core/.NET 8 API, PostgreSQL/TimescaleDB, MongoDB, QuantumLeap e uma dashboard Vue 3.

## Requisitos

- Windows 10/11
- Docker Desktop
- .NET 8 SDK, apenas para desenvolvimento local
- Node.js 20+ ou 22+, apenas para desenvolvimento local
- PowerShell
- VS Code recomendado

## Primeira execução principal

```powershell
docker compose config
docker compose up -d --build
docker compose ps
```

Abrir:

```text
http://localhost:8088
```

Login:

```text
admin/admin
```

Este comando arranca a stack principal completa, incluindo `db`, `mongo-db`, `orion`, `iot-agent`, `quantumleap`, `api`, `dashboard` e `grafana`.

## URLs principais

- Dashboard: http://localhost:8088
- Swagger: http://localhost:5181/swagger
- Dashboard summary: http://localhost:5181/api/dashboard/summary
- Orion-LD: http://localhost:1026/version
- IoT Agent: http://localhost:4041/iot/about
- QuantumLeap: http://localhost:8668/version
- Grafana: http://localhost:${GRAFANA_PORT:-33010}

## Guia FIWARE

Para testes detalhados, troubleshooting e validação ponta-a-ponta da integração FIWARE, consultar:

- `docs/FIWARE_TESTING.md`

## Execuções seguintes

```powershell
docker compose up -d
docker compose ps
```

## Parar sem perder dados

```powershell
docker compose stop
```

ou:

```powershell
docker compose down
```

Ambos mantêm os volumes Docker.

## Recriar dados do zero

```powershell
docker compose down -v
docker compose up -d --build
```

A opção `-v` apaga volumes e recria os dados demo. Use isto se ainda existirem dados antigos nos volumes Docker.

## Grafana Monitoring

URL:

```text
http://localhost:${GRAFANA_PORT:-33010}
```

Credenciais:

```text
admin/admin
```

O Grafana arranca por defeito com `docker compose up -d --build` e é provisionado automaticamente:

- datasource PostgreSQL/TimescaleDB: `DriveTrace TimescaleDB` (UID `drivetrace-timescaledb`)
- dashboards provisionados:
  - `DriveTrace Core - Executive Overview` (UID `drivetrace-executive-overview`)
  - `DriveTrace Core - FIWARE / Infrastructure Status` (UID `drivetrace-fiware-infra-status`)
  - `DriveTrace Core - Quality & Traceability` (UID `drivetrace-quality-traceability`)
  - `DriveTrace Core - WIP Operations` (UID `drivetrace-wip-operations`)
  - `DriveTrace Core - WIP Overview` (UID `drivetrace-wip-overview`)

Na dashboard Vue existe agora uma secção dedicada no menu lateral: `Grafana Analytics` (`AN`), com:

- seleção por dashboard (`Executive Overview`, `WIP Operations`, `Quality & Traceability`, `FIWARE / Infrastructure Status`, `WIP Overview`)
- cartões de objetivo/apoio à decisão para cada dashboard
- cartões de insight operacional (visão executiva, WIP, qualidade e FIWARE)
- embeds reais via iframe e opção `Open in Grafana`

Variáveis de ambiente relevantes:

```env
GRAFANA_PORT=33010
VITE_GRAFANA_BASE_URL=http://localhost:33010
```

- `GRAFANA_PORT` controla a porta externa do Grafana no Docker Compose.
- `VITE_GRAFANA_BASE_URL` define a base URL usada pelo frontend para embedding (com fallback para `http://localhost:33010`).

Para desenvolvimento/demo local, o serviço Grafana está configurado com:

- `GF_SECURITY_ALLOW_EMBEDDING=true`
- `GF_AUTH_ANONYMOUS_ENABLED=true`
- `GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer`

Isto é adequado para demonstração académica local, mas não é uma configuração segura para produção.
Em produção, use autenticação, reverse proxy/SSO e controlo de acesso de rede.

Comandos de validação:

```powershell
docker compose up -d --build
docker compose restart grafana
docker compose ps
docker compose logs --tail=200 grafana
```

Guia detalhado:

- `docs/GRAFANA_MONITORING.md`

## Desenvolvimento local

API:

```powershell
docker compose up -d db
cd api
dotnet restore
dotnet run
```

A API local exige a base de dados ativa em `127.0.0.1:15432`.

Frontend:

```powershell
cd dashboard
npm install
npm run dev
```

## Operações CRUD na interface

O admin pode gerir pela dashboard ordens de fabrico, unidades de produto, suportes, materiais e lotes, qualidade, racks, previsões e parâmetros do sistema. As operações de eliminação têm confirmação na interface e podem ser recusadas pela API quando existirem associações históricas ou regras de chave estrangeira.

A autenticação continua a ser demo/local no frontend e o backend ainda não aplica RBAC real. O histórico de localização de suportes é apresentado apenas para consulta, porque deve ser gerado por eventos/movimentos e não editado manualmente.

## Autenticação demo

- `admin/admin`
- Autenticação local/demo no frontend
- Sessão e utilizadores guardados em `localStorage`
- A API ainda não está protegida por JWT
- Preparado para evolução futura com autenticação real e RBAC

## Dados demo

O cenário demo representa uma linha automóvel de portas em PT-PT: `ProductUnit` como unidade rastreável, `Support` como âncora intra-linha, `Rack` para logística pós-linha, materiais/lotes de matéria-prima, qualidade, não conformidades, retrabalho, sucata e previsões demonstrativas.

Se os dados antigos persistirem em volumes Docker, usar:

```powershell
docker compose down -v
docker compose up -d --build
```

## Limitações conhecidas

- Auth local/demo no frontend
- API ainda não protegida por JWT
- Grafana local provisionado para demonstração académica
- FIWARE opcional para demonstração
- Previsões demonstrativas, sem IA real nesta V1
