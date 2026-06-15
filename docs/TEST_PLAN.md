# Test plan

## Static checks

```bash
docker compose config
```

## Backend

```bash
cd api
dotnet restore
dotnet build
dotnet run
```

Open:

```text
http://localhost:5181/swagger
```

Validate:

1. `GET /api/dashboard/summary`
2. `GET /api/product-units`
3. `GET /api/supports`
4. `GET /api/traceability/product-units/1`
5. `POST /api/events/playback`
6. `POST /api/events/manual`
7. `GET /api/fiware/context`

## Frontend

```bash
cd dashboard
npm install
npm run build
npm run dev
```

Validate:

1. Dashboard loads with DRIVOLUTION branding.
2. Sidebar has automotive WIP pages.
3. No legacy non-core mobile module appears in UI.
4. Product units show unitary traceability.
5. Supports show the intra-line anchor.
6. Racks appear as post-line logistics.
7. Quality page shows PASS/FAIL and deviations.
8. Event playback button calls the API when available.

## Docker

```bash
docker compose up -d --build
```

Antes de correr smoke tests, esperar pela API:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\wait-api.ps1
```

Validate:

- Dashboard: `http://localhost:8088`
- Swagger: `http://localhost:5181/swagger`
- Orion-LD: `http://localhost:1026/version`
- Grafana: `http://localhost:33010`

## Fase F0 - clareza visual e analitica operacional

```powershell
dotnet build .\api\DriveTraceCore.Api.csproj
cd dashboard
npm run build
cd ..
docker compose config
docker compose up -d --build
powershell -ExecutionPolicy Bypass -File .\scripts\wait-api.ps1
```

Validar:

1. O menu nao apresenta a area de previsoes.
2. O mapa de rastreabilidade tem modo simples e modo tecnico.
3. O modo simples do mapa esconde nos tecnicos e mantem linhas, seccoes, unidade, qualidade e rota.
4. A analitica apresenta estado operacional, principal atencao, acao recomendada e evidencia.
5. O catalogo Grafana mostra tres dashboards inicialmente e permite expandir.
6. O simulador mostra resultado do cenario no topo, detalhes tecnicos colapsados e runs limitadas.
7. Os scripts de smoke test usam readiness da API para evitar falsos negativos apos `docker compose up`.

## Fase B - perfis e permissoes

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-b-roles.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-b-roles.ps1 -PublishFiware
```

Validar:

1. `GET /api/auth/me` devolve `roleKey` e permissoes do perfil ativo.
2. Cliente so acede a `GET /api/customer/orders/{publicTrackingCode}`.
3. Operador acede a `/api/operator/workbench`, mas nao a FIWARE, racks ou materiais.
4. Qualidade acede a resultados/nao conformidades, mas nao a racks.
5. Logistica acede a racks e materiais, mas nao a qualidade.
6. Demo viewer fica em leitura; escritas protegidas devolvem `403`.

## Fase C - mapa de rastreabilidade

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-c-trace-graph.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-c-trace-graph.ps1 -PublishFiware
```

Validar:

1. `GET /api/trace-graph/options` devolve unidades, ordens, linhas e modos `factory`, `product-unit`, `order`.
2. `GET /api/trace-graph/factory` devolve nos `ProductionLine`, `Section`, `ProductUnit` e arestas `contains`, `route`, `current-location`.
3. `GET /api/trace-graph/product-unit/{id}` devolve rota e movimentos da unidade.
4. `GET /api/trace-graph/manufacturing-order/{id}` devolve ordem e unidades associadas.
5. Cliente nao acede ao grafo interno, mas acede ao grafo da propria ordem.
6. Operador recebe grafo filtrado pela linha/seccao atribuida.
7. `GET /api/trace-graph/fiware-status` respeita `Fiware.View`.
8. Dashboard, FIWARE opcional e Grafana continuam acessiveis.

## Fase D - recuperacao e recondicionamento

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-d-reconditioning.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-d-reconditioning.ps1 -PublishFiware
```

Validar:

1. `GET /api/reconditioning` devolve resumo, candidatos, em recuperacao, recondicionados, rejeitados e sucata.
2. `GET /api/reconditioning/candidates` devolve apenas unidades candidatas/recuperaveis/em recuperacao.
3. `GET /api/reconditioning/{id}` devolve detalhe com unidade, nao conformidade, retrabalho, historico e permissoes de acao.
4. `POST /api/product-units/{id}/mark-reconditionable` cria ou atualiza decisao recuperavel e evento operacional.
5. `POST /api/product-units/{id}/complete-reconditioning` exige validacao funcional final e marca a unidade como recondicionada.
6. `POST /api/product-units/{id}/reject-reconditioning` rejeita recuperacao e impede recondicionar sucata.
7. Perfis sem `Reconditioning.Decide` nao conseguem concluir/rejeitar; cliente nao acede ao dominio interno.
8. O grafo inclui `ReconditionRecord` para perfis internos e mantem o cliente sanitizado.
9. FIWARE expõe `isReconditioned`, `qualityDisposition`, `recoveryStatus` e `reconditionedAt`.
10. Grafana tem paineis de estado de recuperacao e tabela de unidades recondicionadas.

## Fase E - simulador de producao

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-e-production-simulator.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-e-production-simulator.ps1 -PublishFiware
```

Validar:

1. `GET /api/simulation/scenarios` devolve pelo menos tres cenarios.
2. `GET /api/simulation/state` expõe runs ativas, estado recente e alertas.
3. `POST /api/simulation/runs` cria uma run para `normal-flow`, `minor-recovery` e `critical-scrap`.
4. `POST /api/simulation/runs/{id}/tick` avanca exatamente um passo.
5. O fluxo normal termina em unidade conforme e rack.
6. O fluxo com falha menor passa por retrabalho e recondicionamento.
7. O fluxo com falha critica termina em sucata.
8. Eventos operacionais de simulacao sao criados.
9. O grafo da unidade e da ordem responde apos a simulacao.
10. Cliente continua sem acesso ao simulador interno.
11. FIWARE continua publicavel e consistente com `publish-current`.
12. Grafana continua acessivel e com os mesmos UIDs.
