# DriveTrace Core

Plataforma de **rastreabilidade e monitorização de produção (WIP)** para o contexto de
fabrico automóvel, desenvolvida no âmbito do **DRIVOLUTION — WP3**.

## O que é

O DriveTrace Core acompanha a produção ao nível da **unidade de produto individual**,
do pedido do cliente até à expedição. Cada unidade é seguida ao longo das linhas e
secções, sempre associada a um **suporte** (o elemento central da rastreabilidade no
chão de fábrica), com controlo de qualidade, recondicionamento e sucata, e uma vista de
**rastreabilidade por grafo**.

### Problema que resolve

Em produção automóvel é difícil saber, a qualquer momento, **onde está cada unidade, por
onde passou, em que suporte segue e qual o seu estado de qualidade**. O DriveTrace Core
dá essa visibilidade em tempo real e o histórico associado, ligando o pedido do cliente
à unidade física e ao seu percurso.

## Módulos principais

- **Painel de operações** — dashboard do turno: KPIs, visão rápida da produção e
  percurso da unidade.
- **Encomendas e produção** — encomendas de cliente, ordens de fabrico e arranque de
  produção por unidades.
- **Análise por linha** — carga, gargalos e ações operacionais (avançar, transferir,
  atribuir suporte).
- **Quadro de qualidade** — decisões de aprovação, recondicionamento e sucata.
- **Mapa de rastreabilidade** — grafo técnico que relaciona cliente, ordem, unidade,
  suporte, secções/linhas, qualidade, recondicionamento, sucata, rack e eventos.
- **Área de cliente** — submissão e acompanhamento das próprias encomendas.
- **Integração FIWARE e Grafana** — contexto NGSI-LD e monitorização temporal.

## Tecnologias

- **Frontend:** Vue 3, TypeScript, Vite, Pinia.
- **Backend:** ASP.NET Core 8 (.NET 8), Entity Framework Core.
- **Base de dados:** PostgreSQL / TimescaleDB.
- **Contexto e monitorização:** FIWARE (Orion-LD, IoT Agent JSON, QuantumLeap) e Grafana.
- **Orquestração:** Docker Compose.

## Arranque rápido

Requisitos: **Docker** e **Docker Compose**.

```bash
docker compose up -d --build
```

Aguardar a API ficar pronta e abrir o dashboard.

| Serviço | URL |
|---|---|
| Dashboard | http://localhost:8088 |
| API + Swagger | http://localhost:5181/swagger |
| Grafana | http://localhost:33010 |
| Orion-LD (FIWARE) | http://localhost:1026/version |

### Utilizadores de demonstração

| Perfil | Utilizador | Palavra-passe |
|---|---|---|
| Administrador | `admin` | `admin` |
| Supervisor | `supervisor` | `supervisor` |
| Operador | `operador` | `operador` |
| Qualidade | `qualidade` | `qualidade` |
| Cliente | `cliente` | `cliente` |

## Testes

```bash
cd dashboard && npm install && npm run test     # frontend
dotnet test ./api/Tests/DriveTraceCore.Api.Tests.csproj   # backend
```

## Demo de apresentação

```powershell
./scripts/run-demo.ps1
```

Arranca o ambiente e abre a aplicação num **modo de apresentação** guiado (perfil
Administrador). Guião completo em [docs/DEMO.md](docs/DEMO.md).

## Documentação

- [Setup e execução](docs/SETUP_E_EXECUCAO.md)
- [Perfis de utilizador](docs/PERFIS.md)
- [Integração FIWARE](docs/FIWARE.md)
- [Monitorização (Grafana)](docs/GRAFANA.md)
- [Demo de apresentação](docs/DEMO.md)
- [Notas de design](docs/DESIGN_NOTES.md)
- Diagramas em `docs/diagrams/` e coleção Postman em `docs/postman/`.
