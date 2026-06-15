# Domain model

## Products

- Product
- Variant

## Manufacturing

- ManufacturingOrder
- ProductionLine
- ProductionLineSection
- Resource
- ManufacturingProcess
- ManufacturingSectionPhase
- ManufacturingProcessPhase
- Checkpoint

## Core traceability

- ProductUnit
- Support
- UnitSupportAssignment
- SupportLocalizationHistory

## Post-line logistics

- Rack
- RackSupportAssignment

## Materials and genealogy

- RawMaterial
- LotRawMaterial
- UnitMaterialLotUsage

## Quality

- QualityResult
- Nonconformity
- ReworkRecord
- ScrapRecord

## Naming decision

The implementation uses `ManufacturingSectionPhase`, aligned with the current class/database modelling direction and clearer than a generic phase name because each phase is associated with a production-line section.
