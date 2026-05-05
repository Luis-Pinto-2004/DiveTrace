# Delivery notes

## Implemented

- Rebuilt backend domain model for automotive WIP traceability.
- Rebuilt frontend as DriveTrace Core with DRIVOLUTION branding.
- Added demo seed data for the automotive door line.
- Added support movement history, material lot genealogy, quality failures, rework and scrap records.
- Added dashboard summary and traceability endpoints.
- Added deterministic event playback and manual event injection endpoints.
- Added NGSI-LD-style current context export and optional Orion-LD publishing.
- Added Docker Compose stack with API, dashboard, TimescaleDB/PostgreSQL, MongoDB, Orion-LD, IoT Agent, QuantumLeap and Grafana.

## Removed from the active core

- Legacy non-core mobile subsystem.
- Legacy human-monitoring concepts.
- Legacy conversational AI and advanced prediction services.
- Product-lot-as-output modelling.

## Validation note

The frontend can be built in a normal Node environment. Backend build requires .NET 8 SDK. In the packaged project, backend validation commands are documented in `docs/TEST_PLAN.md`.
