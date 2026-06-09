# Grafana Monitoring

## Scope

DriveTrace Core uses Grafana as the analytical and monitoring layer on top of PostgreSQL/TimescaleDB operational data.

- Source of truth: ASP.NET Core API + PostgreSQL/TimescaleDB
- Context layer: FIWARE stack (Orion-LD, IoT Agent, QuantumLeap)
- Monitoring layer: Grafana
- Frontend integration: Vue section `Grafana Analytics` with real iframe embeds (`/d` and `/d-solo`)

## Runtime Endpoints

- Frontend: `http://localhost:8088`
- API Swagger: `http://localhost:5181/swagger`
- Grafana: `http://localhost:${GRAFANA_PORT:-33010}`
- Grafana login: `admin/admin`

## Datasource and Provisioning

- Datasource name: `DriveTrace TimescaleDB`
- Datasource UID: `drivetrace-timescaledb`
- Provisioning folder: `grafana/provisioning/`
- Dashboard JSON folder: `grafana/dashboards/`

Provisioned dashboards:

1. `DriveTrace Core - Executive Overview` (`drivetrace-executive-overview`)
2. `DriveTrace Core - FIWARE / Infrastructure Status` (`drivetrace-fiware-infra-status`)
3. `DriveTrace Core - Quality & Traceability` (`drivetrace-quality-traceability`)
4. `DriveTrace Core - WIP Operations` (`drivetrace-wip-operations`)
5. `DriveTrace Core - WIP Overview` (`drivetrace-wip-overview`)

## Analytical Coverage

- `DriveTrace Core - Executive Overview`
  - Open manufacturing orders, active units/supports, open quality issues, active rack assignments
  - Product/support status distributions, WIP by section, recent support movements, recent nonconformities
- `DriveTrace Core - WIP Operations`
  - WIP by section, supports by section and by status, manufacturing orders by status
  - Current unit-support assignments, bottleneck indication, throughput proxy by day, recent movement history
- `DriveTrace Core - Quality & Traceability`
  - PASS/FAIL distribution, nonconformities by severity/status, rework and scrap metrics
  - Recent quality events, product-unit traceability table, genealogy/material lot usage context
- `DriveTrace Core - FIWARE / Infrastructure Status`
  - Estimated publishable entities, supports/units/racks counts, candidate entities by type
  - Recent movement events and explicit note about datasource scope versus FIWARE API validation
- `DriveTrace Core - WIP Overview`
  - Simplified baseline view with core stats, status distributions, WIP section distribution and recent events

## Phase 2 Operational Analytics Hub

The frontend analytics area now works as an operational hub, not only as a list of embedded dashboards.

Access path:

1. Open `http://localhost:8088`
2. Use the sidebar entry `Grafana Analytics` / `Analítica operacional`
3. Review the operational snapshot, insight cards and dashboard catalogue
4. Use `Show in application` to focus an embedded dashboard family
5. Use `Open in Grafana` for direct troubleshooting or full-screen analysis

The hub combines two data surfaces:

- current operational values already loaded by the Vue application from the DriveTrace Core API
- provisioned Grafana dashboards backed by the `DriveTrace TimescaleDB` datasource

No new production-line transfer logic, FIWARE publication logic or relational model changes are required by this layer. The hub is a presentation and observability improvement over the existing data model.

Iframe notes:

- Local demo embedding is enabled through Docker Compose Grafana settings.
- Anonymous Viewer access is intended only for local academic/demo execution.
- If a browser, proxy or Grafana policy blocks the iframe, use `Open in Grafana` and authenticate directly.

## Phase 2B Visual and UX Redesign

Phase 2B keeps the same technical boundaries as Phase 2 and focuses on presentation quality.

Changed at application level:

- Industrial visual shell with grouped sidebar navigation, system status topbar and a softer operational background
- Decision-oriented `Dashboard / Line Overview` with WIP, quality, FIWARE coherence and recent movements
- Improved `Racks / Post-line Logistics` page with post-line KPIs and rack-support interpretation
- Improved `FIWARE Context Monitor` with context coherence cards and clearer entity table
- Improved local `Users` administration with profile metrics, clearer form/list separation and consistent actions
- `Grafana Analytics` now prioritizes snapshot, decision panel, dashboard catalogue questions, selected embedded panels and an optional full-dashboard embed

Grafana provisioning was intentionally kept stable:

- Datasource UID remains `drivetrace-timescaledb`
- Dashboard UIDs remain unchanged
- Existing dashboard JSON files remain the source of provisioned dashboards
- The frontend improves the integration and interpretation layer around those dashboards

Manual visual checks recommended:

- `http://localhost:8088` at 1366x768 and 1920x1080
- Sidebar grouping and topbar wrapping
- `Analítica operacional` dashboard catalogue and embedded panels
- `Racks / Logística pós-linha` KPIs and CRUD tables
- `Monitor de contexto FIWARE` coherence panel and entity table

## Frontend Embedding Configuration

Frontend env var (build-time):

- `VITE_GRAFANA_BASE_URL=http://localhost:33010`

Location:

- `dashboard/.env.example`
- root `.env.example`

Used in Vue with fallback to local default:

- `import.meta.env.VITE_GRAFANA_BASE_URL || 'http://localhost:33010'`

Reusable component:

- `dashboard/src/components/GrafanaPanel.vue`

Main analytics page:

- `dashboard/src/components/GrafanaAnalyticsView.vue`
- Integrated in menu/sidebar through `dashboard/src/App.vue` as `Grafana Analytics` (`AN`)
- Includes dashboard cards with purpose/decision text, insight cards and embedded full-dashboard preview

## Grafana Docker Settings for Local Embed

Configured in `docker-compose.yml` under `grafana.environment`:

- `GF_SECURITY_ALLOW_EMBEDDING=true`
- `GF_AUTH_ANONYMOUS_ENABLED=true`
- `GF_AUTH_ANONYMOUS_ORG_ROLE=Viewer`
- `GF_USERS_ALLOW_SIGN_UP=false`
- `GF_ANALYTICS_REPORTING_ENABLED=false`
- `GF_ANALYTICS_CHECK_FOR_UPDATES=false`
- `GF_PLUGINS_PREINSTALL_DISABLED=true`

## Security Note

These settings are suitable for local demo/academic environments only.

Do not expose Grafana anonymously in production. Production should use authentication and access controls, such as:

- reverse proxy with auth
- private/internal network exposure
- SSO / OAuth / enterprise auth
- least-privilege roles and org permissions

## Validation Commands

Run from repository root:

```powershell
docker compose config
docker compose up -d --build
docker compose restart grafana
docker compose ps
docker compose logs --tail=200 grafana
```

Validate endpoints:

- `http://localhost:8088`
- `http://localhost:5181/swagger`
- `http://localhost:33010/login`

Grafana API checks (Basic auth admin/admin):

```powershell
$pair = 'admin:admin'
$auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
$headers = @{ Authorization = "Basic $auth" }

Invoke-WebRequest -Uri 'http://localhost:33010/api/search?type=dash-db' -Headers $headers -UseBasicParsing
Invoke-WebRequest -Uri 'http://localhost:33010/api/search?tag=drivetrace' -Headers $headers -UseBasicParsing
Invoke-WebRequest -Uri 'http://localhost:33010/api/datasources/uid/drivetrace-timescaledb' -Headers $headers -UseBasicParsing
Invoke-WebRequest -Uri 'http://localhost:33010/api/dashboards/uid/drivetrace-executive-overview' -Headers $headers -UseBasicParsing
Invoke-WebRequest -Uri 'http://localhost:33010/api/dashboards/uid/drivetrace-fiware-infra-status' -Headers $headers -UseBasicParsing
Invoke-WebRequest -Uri 'http://localhost:33010/api/dashboards/uid/drivetrace-quality-traceability' -Headers $headers -UseBasicParsing
Invoke-WebRequest -Uri 'http://localhost:33010/api/dashboards/uid/drivetrace-wip-operations' -Headers $headers -UseBasicParsing
Invoke-WebRequest -Uri 'http://localhost:33010/api/dashboards/uid/drivetrace-wip-overview' -Headers $headers -UseBasicParsing
```

Optional datasource query check (example payload):

```powershell
$body = @{
  queries = @(
    @{
      refId = 'A'
      datasource = @{ uid = 'drivetrace-timescaledb' }
      rawSql = 'SELECT COUNT(*)::bigint AS value FROM \"ProductUnits\";'
      format = 'table'
    }
  )
} | ConvertTo-Json -Depth 8

Invoke-RestMethod -Method Post -Uri 'http://localhost:33010/api/ds/query' -Headers $headers -ContentType 'application/json' -Body $body
```

Iframe URL checks (expected HTTP 200):

```text
http://localhost:33010/d-solo/drivetrace-executive-overview/drivetrace-executive-overview?orgId=1&from=now-30d&to=now&timezone=browser&refresh=30s&panelId=1&theme=dark
http://localhost:33010/d/drivetrace-wip-operations/drivetrace-wip-operations?orgId=1&from=now-30d&to=now&timezone=browser&refresh=30s&theme=dark&kiosk
```

Frontend build validation:

```powershell
cd dashboard
npm run build
```

Backend build validation:

```powershell
cd api
dotnet build
```

FIWARE validation:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-fiware.ps1 -PublishCurrent
```

## Realistic Limitations

- Dashboards depend on currently available demo data; some KPIs may show `0`.
- Grafana reads PostgreSQL/TimescaleDB operational data only.
- FIWARE connectivity and publish flow are validated through API/script checks, not a direct Grafana Orion-LD datasource.
