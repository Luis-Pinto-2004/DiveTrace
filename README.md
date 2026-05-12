# DriveTrace Core
DRIVOLUTION WP3 â€” WIP Traceability and Monitoring Platform

DriveTrace Core Ã© uma plataforma acadÃ©mica para rastreabilidade e monitorizaÃ§Ã£o WIP em contexto automÃ³vel. A unidade rastreÃ¡vel Ã© a `ProductUnit`, o `Support` Ã© a Ã¢ncora fÃ­sica intra-linha e a `Rack` representa logÃ­stica pÃ³s-linha. A stack combina FIWARE/Orion-LD, ASP.NET Core/.NET 8 API, PostgreSQL/TimescaleDB, MongoDB, QuantumLeap e uma dashboard Vue 3.

## Requisitos

- Windows 10/11
- Docker Desktop
- .NET 8 SDK, apenas para desenvolvimento local
- Node.js 20+ ou 22+, apenas para desenvolvimento local
- PowerShell
- VS Code recomendado

## Primeira execuÃ§Ã£o principal

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

Para testes detalhados, troubleshooting e validaÃ§Ã£o ponta-a-ponta da integraÃ§Ã£o FIWARE, consultar:

- `docs/FIWARE_TESTING.md`

## ExecuÃ§Ãµes seguintes

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

Ambos mantÃªm os volumes Docker.

## Recriar dados do zero

```powershell
docker compose down -v
docker compose up -d --build
```

A opÃ§Ã£o `-v` apaga volumes e recria os dados demo. Use isto se ainda existirem dados antigos nos volumes Docker.

## Grafana Monitoring

URL:

```text
http://localhost:${GRAFANA_PORT:-33010}
```

Credenciais:

```text
admin/admin
```

O Grafana arranca por defeito com `docker compose up -d --build` e e provisionado automaticamente:

- datasource PostgreSQL/TimescaleDB: `DriveTrace TimescaleDB` (UID `drivetrace-timescaledb`)
- dashboards provisionados:
  - `DriveTrace Core - WIP Overview` (UID `drivetrace-wip-overview`)
  - `DriveTrace Core - Executive Overview`
  - `DriveTrace Core - WIP Operations`
  - `DriveTrace Core - Quality & Traceability`
  - `DriveTrace Core - FIWARE / Infrastructure Status`

O Grafana monitoriza dados operacionais e historicos diretamente da base de dados PostgreSQL/TimescaleDB da plataforma (WIP, suportes, qualidade, rastreabilidade e estado de infraestrutura relacional).

Se existir conflito de porta no Windows, crie ou edite `.env` na raiz:

```env
GRAFANA_PORT=33010
```

Pode usar outra porta livre, por exemplo `33011`, `33012` ou `34010`.

Comandos de validacao:

```powershell
docker compose up -d --build
docker compose restart grafana
docker compose ps
docker compose logs --tail=150 grafana
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

## OperaÃ§Ãµes CRUD na interface

O admin pode gerir pela dashboard ordens de fabrico, unidades de produto, suportes, materiais e lotes, qualidade, racks, previsÃµes e parÃ¢metros do sistema. As operaÃ§Ãµes de eliminaÃ§Ã£o tÃªm confirmaÃ§Ã£o na interface e podem ser recusadas pela API quando existirem associaÃ§Ãµes histÃ³ricas ou regras de chave estrangeira.

A autenticaÃ§Ã£o continua a ser demo/local no frontend e o backend ainda nÃ£o aplica RBAC real. O histÃ³rico de localizaÃ§Ã£o de suportes Ã© apresentado apenas para consulta, porque deve ser gerado por eventos/movimentos e nÃ£o editado manualmente.

## AutenticaÃ§Ã£o demo

- `admin/admin`
- AutenticaÃ§Ã£o local/demo no frontend
- SessÃ£o e utilizadores guardados em `localStorage`
- A API ainda nÃ£o estÃ¡ protegida por JWT
- Preparado para evoluÃ§Ã£o futura com autenticaÃ§Ã£o real e RBAC

## Dados demo

O cenÃ¡rio demo representa uma linha automÃ³vel de portas em PT-PT: `ProductUnit` como unidade rastreÃ¡vel, `Support` como Ã¢ncora intra-linha, `Rack` para logÃ­stica pÃ³s-linha, materiais/lotes de matÃ©ria-prima, qualidade, nÃ£o conformidades, retrabalho, sucata e previsÃµes demonstrativas.

Se os dados antigos persistirem em volumes Docker, usar:

```powershell
docker compose down -v
docker compose up -d --build
```

## LimitaÃ§Ãµes conhecidas

- Auth local/demo no frontend
- API ainda nÃ£o protegida por JWT
- Grafana local provisionado para demonstraÃ§Ã£o acadÃ©mica
- FIWARE opcional para demonstraÃ§Ã£o
- PrevisÃµes demonstrativas, sem IA real nesta V1

