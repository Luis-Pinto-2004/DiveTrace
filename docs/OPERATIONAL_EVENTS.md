# Operational Events

Fase A introduz um log operacional central para consolidar movimentos, qualidade, rack, seed demo e sincronizacao FIWARE sem substituir os modelos existentes.

## Modelo

Tabela: `OperationalEvents`

Campos principais:

- `EventCode`: codigo unico e idempotente do evento.
- `EventType`: tipo funcional do evento.
- referencias opcionais: `ProductUnitId`, `SupportId`, `ManufacturingOrderId`, linhas, secoes, `CheckpointId`, `QualityResultId`, `NonconformityId`, `ReworkRecordId`, `ScrapRecordId`, `RackId`.
- `ReasonCode`, `Severity`, `Source`, `PerformedByUserId`, `OccurredAt`, `Notes`, `IsDemo`, `MetadataJson`.

Tipos suportados:

- `ProductUnitCreated`
- `SupportAssigned`
- `SectionMovement`
- `LineTransfer`
- `QualityRecorded`
- `NonconformityOpened`
- `ReworkStarted`
- `ReworkCompleted`
- `ScrapRecorded`
- `RackAssigned`
- `RackReleased`
- `FiwarePublished`
- `DemoSeeded`

## API

- `GET /api/operational-events`
- `GET /api/operational-events/recent?limit=20`
- `GET /api/product-units/{id}/events`
- `GET /api/product-units/{id}/trace`
- `GET /api/operations/flow-summary`

`/api/product-units/{id}/trace` inclui agora `operationalEvents` e tambem adiciona os eventos operacionais na `timeline`.

`/api/operations/flow-summary` mantem a forma anterior e acrescenta totais de eventos e `recentOperationalEvents`.

## Integracoes

- Seed demo cria eventos idempotentes para unidades, suportes, movimentos, qualidade, nao conformidades, retrabalho, sucata e `DemoSeeded`.
- Transferencias `POST /api/product-units/{id}/transfer` criam `LineTransfer` ou `SectionMovement`.
- Eventos manuais/playback criam eventos operacionais com `Source` `Manual` ou `Playback`.
- CRUD direto de qualidade, nao conformidades, retrabalho, sucata e racks cria eventos de dominio correspondentes.
- Publicacao FIWARE bem-sucedida cria `FiwarePublished`.

## Dashboard

A vista `events` mostra KPIs e stream recente do log central. O overview mostra uma lista compacta de eventos recentes. O trace de produto continua baseado em `ProductUnit`, mas recebe eventos operacionais como evidencia adicional.
