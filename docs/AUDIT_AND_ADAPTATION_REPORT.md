# Audit and adaptation report

## Source audit

The source ZIP contained these main areas:

- ASP.NET Core API with controllers, DTOs, services, repositories, EF Core DbContext and historical migrations.
- Vue 3 dashboard with a sidebar, dashboard views, table components and domain-specific pages.
- Docker Compose stack with API, context broker, IoT agent, MongoDB, time-series database, QuantumLeap and Grafana.
- Auxiliary configuration folders and a legacy mobile-side module unrelated to the new DRIVOLUTION WP3 core.

## Main adaptation decisions

1. A clean backend was created under `api/` to avoid accidental retention of legacy domain concepts.
2. The new API keeps ASP.NET Core, EF Core, Swagger and PostgreSQL/TimescaleDB.
3. The frontend was rebuilt under `dashboard/src/` with DriveTrace Core branding and DRIVOLUTION assets.
4. The active domain is now automotive WIP traceability.
5. The model is unitary: `ProductUnit` replaces final output batch thinking.
6. `Support` is the intra-line physical tracking anchor.
7. `Rack` is restricted to post-line logistics.
8. Material genealogy is retained through raw material lots.
9. Quality, nonconformity, rework and scrap records are explicitly modelled.
10. FIWARE is retained as optional current-context infrastructure.

## Files retained from the source approach

- General ASP.NET Core API pattern.
- EF Core relational persistence concept.
- Swagger/OpenAPI exposure.
- Docker Compose infrastructure idea.
- Vue + TypeScript + Tailwind frontend approach.
- Dashboard and history visualisation concept.

## Files/areas intentionally not carried into the active core

- Legacy mobile-side module.
- Legacy human monitoring functionality.
- Legacy conversational AI and advanced out-of-scope analytics services.
- Legacy final output batch model.
- Legacy branding and old visual identity.

## Delivered V1 outcome

The delivered ZIP contains a coherent academic V1 that can be opened, inspected and extended. It prioritises domain correctness and demonstrability over industrial-scale completeness.
