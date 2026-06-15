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
- Flow summary: http://localhost:5181/api/operations/flow-summary
- Operator workbench: http://localhost:5181/api/operator/workbench
- Mapa de rastreabilidade: http://localhost:5181/api/trace-graph/factory
- Opções do mapa de rastreabilidade: http://localhost:5181/api/trace-graph/options
- Recuperação / recondicionamento: http://localhost:5181/api/reconditioning
- Perfil ativo demo: http://localhost:5181/api/auth/me
- Acompanhamento público do cliente: http://localhost:5181/api/customer/orders/TRC-PORTA-001
- Orion-LD: http://localhost:1026/version
- IoT Agent: http://localhost:4041/iot/about
- QuantumLeap: http://localhost:8668/version
- Grafana: http://localhost:${GRAFANA_PORT:-33010}

## Guia FIWARE

Para testes detalhados, troubleshooting e validação ponta-a-ponta da integração FIWARE, consultar:

- `docs/FIWARE_TESTING.md`
- `docs/PRODUCTION_FLOW.md`
- `docs/TRACE_GRAPH.md`
- `docs/RECONDITIONING.md`
- `docs/USER_ROLES.md`
- `docs/PERMISSIONS.md`

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
  - `DriveTrace Core - Visão executiva` (UID `drivetrace-executive-overview`)
  - `DriveTrace Core - Estado FIWARE / infraestrutura` (UID `drivetrace-fiware-infra-status`)
  - `DriveTrace Core - Qualidade e rastreabilidade` (UID `drivetrace-quality-traceability`)
  - `DriveTrace Core - Operações WIP` (UID `drivetrace-wip-operations`)
  - `DriveTrace Core - Visão geral WIP` (UID `drivetrace-wip-overview`)

Na dashboard Vue existe agora uma secção dedicada no menu lateral: `Analítica operacional` (`AN`), com:

- seleção por dashboard (`Visão executiva`, `Operações WIP`, `Qualidade e rastreabilidade`, `Estado FIWARE / infraestrutura`, `Visão geral WIP`)
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

A autenticação continua a ser demo/local, mas a Fase B aplica guardas funcionais reais no backend através de `X-DriveTrace-Role` e `X-DriveTrace-User`. O histórico de localização de suportes é apresentado apenas para consulta, porque deve ser gerado por eventos/movimentos e não editado manualmente.

## Mapa de rastreabilidade

A Fase C adiciona a vista `Mapa de rastreabilidade` na dashboard. A vista usa um grafo nativo em Vue Flow para chão de fábrica, rota de unidade e ordem de fabrico. O backend expõe `GET /api/trace-graph/factory`, `GET /api/trace-graph/product-unit/{id}`, `GET /api/trace-graph/manufacturing-order/{id}` e `GET /api/trace-graph/options`.

O mapa respeita os perfis da Fase B: perfis internos veem apenas os domínios autorizados, o operador fica limitado à linha/secção atribuída e o cliente continua isolado na consulta pública.

Guia detalhado:

- `docs/TRACE_GRAPH.md`

## Recuperação / Recondicionamento

A Fase D adiciona a vista `Recuperação / Recondicionamento` para gerir recuperação produtiva após não conformidade menor ou recuperável. A funcionalidade cria `ReconditionRecord`, marca unidades recuperáveis, conclui produto recondicionado apenas com validação funcional final e rejeita casos para nova disposição quando necessário.

Endpoints principais:

- `GET /api/reconditioning`
- `GET /api/reconditioning/candidates`
- `GET /api/reconditioning/{id}`
- `POST /api/product-units/{id}/mark-reconditionable`
- `POST /api/product-units/{id}/complete-reconditioning`
- `POST /api/product-units/{id}/reject-reconditioning`

Guia detalhado:

- `docs/RECONDITIONING.md`

## Simulador de producao

A Fase E adiciona a vista `Simulador de produção` para avançar unidades pela linha, gerar eventos operacionais, passar por qualidade, retrabalho, recondicionamento, sucata e rack, e manter o grafo/FIWARE/Grafana coerentes.

Endpoint principal:

- `GET /api/simulation/scenarios`
- `GET /api/simulation/state`
- `POST /api/simulation/runs`
- `POST /api/simulation/runs/{id}/tick`

Guia detalhado:

- `docs/PRODUCTION_SIMULATOR.md`

## Autenticação demo

- `admin/admin`
- `supervisor/supervisor`
- `operador/operador`
- `qualidade/qualidade`
- `logistica/logistica`
- `cliente/cliente`
- `demo/demo`
- Autenticação local/demo no frontend
- Sessão e utilizadores guardados em `localStorage`
- A API ainda não está protegida por JWT
- Guardas backend por permissão para o modo demo/local

## Dados demo

O cenário demo representa um fluxo automóvel de portas em PT-PT com várias linhas: `ProductUnit` como unidade rastreável, `Support` como transporte/âncora física, `Rack` para logística pós-linha, materiais/lotes de matéria-prima, qualidade, não conformidades, retrabalho, recuperação/recondicionamento, sucata e previsões demonstrativas.

## Dados demonstrativos PT-PT

- Idioma predefinido: `pt-PT`; inglês continua disponível no seletor de idioma.
- Ordens de fabrico: `OF-PORTA-001` a `OF-PORTA-004`.
- Unidades de produto: `UP-PORTA-001` a `UP-PORTA-011`.
- Pontos de controlo: `PC-ESTAMP-001`, `PC-SOLD-001`, `PC-PINT-001`, `PC-CQ-001`.
- Linhas demo: `LINHA-01` a `LINHA-04`.
- Secções demo: `SEC-MP`, `SEC-ATRIB-SUP`, `SEC-CORTE-ESTAMP`, `SEC-SOLD`, `SEC-PINT-A`, `SEC-PINT-B`, `SEC-CQ`, `SEC-RETRAB`, `SEC-RACK`.
- Consulta pública de cliente: `TRC-PORTA-001`.
- Utilizadores locais demo: `admin/admin`, `supervisor/supervisor`, `operador/operador`, `qualidade/qualidade`, `logistica/logistica`, `cliente/cliente`, `demo/demo`.
- Cenários de recuperação: unidade candidata, unidade em recuperação, unidade recondicionada e unidade rejeitada para sucata.

Validação rápida do fluxo multi-linha:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-production-flow.ps1 -PublishFiware
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-b-roles.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-c-trace-graph.ps1
```

Se os dados antigos persistirem em volumes Docker, usar:

```powershell
docker compose down -v
docker compose up -d --build
```

## Limitações conhecidas

- Auth local/demo no frontend
- API ainda não protegida por JWT
- RBAC atual baseado em cabeçalhos demo/local, não em identidade criptograficamente autenticada
- Grafana local provisionado para demonstração académica
- FIWARE opcional para demonstração
- Previsões demonstrativas, sem IA real nesta V1
