# Production Flow

DriveTrace Core Fase 3/4 models a realistic multi-line route without replacing the traceability roots established in earlier phases.

- `ProductUnit` remains the business traceability root.
- `Support` remains the physical transport anchor for intra-line movement.
- `Rack` remains post-line logistics and storage, not the primary WIP identity.

## Data Model

New or extended relational data:

- `Customers`: customer master data for public order lookup.
- `ManufacturingOrders.CustomerId`, `CustomerReference`, `PublicTrackingCode`: links orders to customers and a public tracking code.
- `ProductionLines.DisplayOrder`, `VisualGroup`: stable ordering/grouping for line summaries.
- `ProductionLineSections.DisplayOrder`, layout metadata and transfer flags: route-aware section metadata.
- `ProductUnitLocationHistory`: auditable unit movement history across sections, supports and production lines.

The current operational location remains on `ProductUnits.CurrentSectionId` and `ProductUnits.CurrentSupportId`. Historical route evidence is stored in `ProductUnitLocationHistory`.

Existing databases are evolved non-destructively at API startup through `SchemaEvolution.EnsurePhase34Async`. This avoids volume resets and only adds missing tables, columns and indexes.

## API Endpoints

Production flow endpoints:

- `POST /api/product-units/{id}/transfer`
- `GET /api/product-units/{id}/trace`
- `GET /api/operations/flow-summary`
- `GET /api/operator/workbench`
- `GET /api/customer/orders/{publicTrackingCode}`

Useful CRUD endpoints added for admin/data inspection:

- `GET /api/customers`
- `GET /api/product-unit-location-history`

## Transfer Semantics

`POST /api/product-units/{id}/transfer` updates the product unit current section and writes a `ProductUnitLocationHistory` row.

If `moveCurrentSupport` is true, the current support moves with the unit and a `SupportLocalizationHistory` row is also written. If `toSupportId` is supplied, the unit can transfer to a different support; open unit-support assignment records are closed and a new one is created.

Completed or scrapped units are rejected by the transfer endpoint.

## Dashboard

New UI surfaces:

- `Dashboard / Line Overview`: compact multi-line WIP summary from `/api/operations/flow-summary`.
- `Product Units`: trace and transfer panel above the existing CRUD table.
- `Operator Workbench`: transfer-ready, blocked, rework and no-support queues.
- `Customer Lookup`: consulta pública usando o código demo `TRC-PORTA-001`.
- `Grafana Analytics`: receives flow summary data for line and transfer metrics.

No factory-floor graph is introduced in this phase.

## FIWARE

`ProductUnit` context now includes:

- `currentProductionLine`
- `lastMovementAt`
- `routeState`

Production line context entities are also published as `ProductionLine`.

## Grafana

`DriveTrace Core - Operações WIP` foi atualizado de forma conservadora:

- WIP by section now includes production line grouping.
- Supports by section now includes production line grouping.
- Recent movement history includes `ProductUnitLocationHistory` and `SupportLocalizationHistory`.

## Validation

Run the production-flow smoke test:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-production-flow.ps1 -PublishFiware
```

This checks flow summary, operator workbench, a controlled product-unit transfer, product-unit trace, customer lookup and FIWARE ProductUnit route state.

## Dados demonstrativos PT-PT

A seed demo é idempotente e atualiza códigos antigos conhecidos sem apagar dados reais desconhecidos.

- Ordens de fabrico: `OF-PORTA-001` a `OF-PORTA-004`.
- Unidades: `UP-PORTA-001` a `UP-PORTA-010`.
- Consulta pública: `TRC-PORTA-001`.
- Linhas: `LINHA-01` montagem/soldadura, `LINHA-02` pintura A, `LINHA-03` pintura B, `LINHA-04` qualidade/retrabalho.
- Secções principais: `SEC-MP`, `SEC-ATRIB-SUP`, `SEC-CORTE-ESTAMP`, `SEC-SOLD`, `SEC-PINT-A`, `SEC-PINT-B`, `SEC-CQ`, `SEC-RETRAB`, `SEC-RACK`.
- Pontos de controlo: `PC-ESTAMP-001`, `PC-SOLD-001`, `PC-PINT-001`, `PC-CQ-001`.

Os estados internos como `Active`, `Blocked`, `Rework`, `PASS` e `FAIL` continuam técnicos por segurança. A aplicação, FIWARE e Grafana apresentam esses valores traduzidos em PT-PT.
