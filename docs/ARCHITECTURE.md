# Architecture summary

DriveTrace Core follows a pragmatic three-layer split.

## Business truth

The ASP.NET Core API and PostgreSQL/TimescaleDB database hold the source of truth: product units, supports, movements, quality decisions, material lot usage, racks and manufacturing orders.

## Current context

FIWARE/Orion-LD represents hot/current context entities such as `Support`, `ProductUnit` and `Rack`. It is useful for IIoT-style integration and dashboard subscriptions, but it does not replace the relational domain model.

## Presentation

The Vue dashboard provides a clean DRIVOLUTION interface with line overview, WIP by section, support tracking, product units, material lots, quality, rack logistics, event playback and current context inspection.

## Domain poles

1. Manufacturing Order: planning and production intent.
2. Product Unit: unitary product/subproduct traceability.
3. Event: movement, quality and decision audit trail.
4. Support: physical intra-line tracking anchor.
