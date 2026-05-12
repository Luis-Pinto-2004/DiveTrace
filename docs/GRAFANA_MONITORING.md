# Grafana Monitoring

## Purpose

This project uses Grafana as a local academic monitoring layer over the operational PostgreSQL/TimescaleDB data.

- Primary source of truth: PostgreSQL/TimescaleDB (`db` service)
- Domain backend: ASP.NET Core API (`api` service)
- Industrial context layer (optional): FIWARE stack (Orion-LD, IoT Agent, QuantumLeap)
- Monitoring UI: Grafana (`grafana` service)

Grafana is intentionally focused on stable, demonstrable SQL dashboards for WIP, quality and traceability.

## Architecture

- Grafana URL: `http://localhost:${GRAFANA_PORT:-33010}`
- Credentials: `admin/admin`
- Datasource: `DriveTrace TimescaleDB`
- Datasource UID: `drivetrace-timescaledb`
- Container datasource target: `db:5432`
- Database: `drivetrace`

Provisioning paths (mounted read-only into container):

- `grafana/provisioning`
- `grafana/dashboards`

## Provisioned Dashboards

1. `DriveTrace Core - WIP Overview` (UID `drivetrace-wip-overview`)
2. `DriveTrace Core - Executive Overview` (UID `drivetrace-executive-overview`)
3. `DriveTrace Core - WIP Operations` (UID `drivetrace-wip-operations`)
4. `DriveTrace Core - Quality & Traceability` (UID `drivetrace-quality-traceability`)
5. `DriveTrace Core - FIWARE / Infrastructure Status` (UID `drivetrace-fiware-infra-status`)

## Key SQL Queries

All queries use quoted PascalCase table/column names to match EF Core generated schema.

Open manufacturing orders:

```sql
SELECT COUNT(*) AS value
FROM "ManufacturingOrders"
WHERE "Status" <> 'Completed';
```

Active product units:

```sql
SELECT COUNT(*) AS value
FROM "ProductUnits"
WHERE "Status" IN ('Active', 'Rework', 'Blocked');
```

Product units by status:

```sql
SELECT "Status" AS status, COUNT(*) AS total
FROM "ProductUnits"
GROUP BY "Status"
ORDER BY "Status";
```

Supports by current section:

```sql
SELECT pls."Name" AS section, COUNT(s."Id") AS total
FROM "ProductionLineSections" pls
LEFT JOIN "Supports" s ON s."CurrentSectionId" = pls."Id"
GROUP BY pls."Name", pls."Id"
ORDER BY pls."Id";
```

Quality results PASS/FAIL:

```sql
SELECT "Result" AS result, COUNT(*) AS total
FROM "QualityResults"
GROUP BY "Result"
ORDER BY "Result";
```

Recent support movements:

```sql
SELECT slh."DateTime", s."SupportCode", pls."Name" AS section, slh."EventType"
FROM "SupportLocalizationHistory" slh
LEFT JOIN "Supports" s ON s."Id" = slh."SupportId"
LEFT JOIN "ProductionLineSections" pls ON pls."Id" = slh."SectionId"
ORDER BY slh."DateTime" DESC
LIMIT 30;
```

Material lot usage:

```sql
SELECT pu."UnitCode", lrm."LotNumber", rm."Name" AS material, umlu."Quantity", umlu."AssociationType"
FROM "UnitMaterialLotUsages" umlu
LEFT JOIN "ProductUnits" pu ON pu."Id" = umlu."ProductUnitId"
LEFT JOIN "LotRawMaterials" lrm ON lrm."Id" = umlu."LotId"
LEFT JOIN "RawMaterials" rm ON rm."Id" = lrm."RawMaterialId"
ORDER BY pu."UnitCode", lrm."LotNumber";
```

## Validation Steps

Run from repository root:

```powershell
docker compose config
docker compose up -d --build
docker compose restart grafana
docker compose ps
docker compose logs --tail=150 grafana
```

Validate endpoints:

- Grafana login: `http://localhost:${GRAFANA_PORT:-33010}/login`
- Frontend: `http://localhost:8088`
- API Swagger: `http://localhost:5181/swagger`

Validate FIWARE remains healthy:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-fiware.ps1 -PublishCurrent
```

## Grafana API Checks (optional but recommended)

Check datasource exists:

- `GET /api/datasources/uid/drivetrace-timescaledb`

Check dashboard exists:

- `GET /api/dashboards/uid/drivetrace-wip-overview`
- `GET /api/dashboards/uid/drivetrace-executive-overview`
- `GET /api/dashboards/uid/drivetrace-wip-operations`
- `GET /api/dashboards/uid/drivetrace-quality-traceability`
- `GET /api/dashboards/uid/drivetrace-fiware-infra-status`

Query execution check:

- `POST /api/ds/query` with dashboard SQL to confirm frames/rows.

## Known Limitations

- Grafana runs local demo credentials (`admin/admin`) for V1 demonstration.
- Some KPIs can legitimately show zero if demo data does not include active rows for that scenario.
- In some Grafana image versions, a non-blocking plugin background installer message may appear in logs (for bundled `elasticsearch` plugin permissions). This does not block datasource provisioning or dashboard rendering.
- FIWARE data is not queried directly from Grafana in this V1; FIWARE health is validated through API endpoints and `scripts/test-fiware.ps1`.
