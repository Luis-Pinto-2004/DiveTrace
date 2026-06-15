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

Validate:

- Dashboard: `http://localhost:8088`
- Swagger: `http://localhost:5181/swagger`
- Orion-LD: `http://localhost:1026/version`
- Grafana: `http://localhost:3004`

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
