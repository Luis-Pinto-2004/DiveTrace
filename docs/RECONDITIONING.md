# Recuperacao e recondicionamento

Fase D adiciona o fluxo controlado de recuperacao produtiva apos uma nao conformidade menor ou recuperavel. A logica estende qualidade/retrabalho/sucata e nao substitui os modelos existentes.

## Regras funcionais

Uma `ProductUnit` so pode ser marcada como recuperavel/recondicionada quando:

- existe uma nao conformidade associada;
- a severidade e recuperavel (`Minor`, `Medium`, `Recoverable` ou equivalentes PT-PT);
- existe ou e criado um registo de retrabalho/recuperacao;
- a validacao funcional final e aprovada;
- a unidade nao esta em sucata;
- a unidade nao foi concluida normalmente sem falha de qualidade;
- a decisao cria evidencia em `OperationalEvents`.

Nao e permitido recondicionar sucata, nao conformidade critica ou nao recuperavel, unidade sem justificacao de qualidade, utilizador sem permissao ou dados fora do ambito do perfil.

## Modelo

Entidade nova:

- `ReconditionRecord`

Campos principais:

- `ProductUnitId`
- `NonconformityId`
- `ReworkRecordId`
- `Status`: `Candidate`, `Recoverable`, `InRecovery`, `Reconditioned`, `Rejected`
- `Decision`
- `Reason`
- `FunctionalValidation`
- `NextDisposition`
- `RecordedAt`, `CompletedAt`, `RejectedAt`
- `RecordedByResourceId`
- `PerformedByUserId`

Campos adicionados a `ProductUnit`:

- `IsReconditioned`
- `ReconditionedAt`
- `ReconditionReason`
- `ReconditionedFromNonconformityId`
- `ReconditionedByResourceId`
- `RecoveryStatus`
- `QualityDisposition`

Campo adicionado a `OperationalEvent`:

- `ReconditionRecordId`

## Endpoints

Base de consulta:

| Endpoint | Permissao | Objetivo |
| --- | --- | --- |
| `GET /api/reconditioning` | `Reconditioning.Read` | Lista decisoes e candidatos de recuperacao/recondicionamento. |
| `GET /api/reconditioning/candidates` | `Reconditioning.Read` | Lista apenas unidades candidatas/recuperaveis/em recuperacao. |
| `GET /api/reconditioning/{id}` | `Reconditioning.Read` | Detalhe de uma decisao de recondicionamento. |

Acoes por unidade:

| Endpoint | Permissao | Objetivo |
| --- | --- | --- |
| `POST /api/product-units/{id}/mark-reconditionable` | `Reconditioning.Write` | Marca a unidade como recuperavel e inicia recuperacao produtiva. |
| `POST /api/product-units/{id}/complete-reconditioning` | `Reconditioning.Decide` | Fecha a recuperacao com validacao funcional e marca produto recondicionado. |
| `POST /api/product-units/{id}/reject-reconditioning` | `Reconditioning.Decide` | Rejeita a recuperacao e define a disposicao seguinte (`Scrap`, `Rework`, `Hold`). |

## Permissoes

- `Administrator`: leitura, escrita e decisao.
- `Supervisor`: leitura, escrita e decisao.
- `QualityTechnician`: leitura, escrita e decisao.
- `Operator`: leitura.
- `Logistics`: leitura.
- `DemoViewer`: leitura.
- `Customer`: sem acesso interno.

## Eventos operacionais

Tipos adicionados:

- `ReconditioningCandidateMarked`
- `ReconditioningStarted`
- `ReconditioningCompleted`
- `ReconditioningRejected`
- `ProductUnitMarkedReconditioned`

Estes eventos ficam ligados a `ProductUnit`, `Nonconformity`, `ReworkRecord` e `ReconditionRecord` quando aplicavel.

## Frontend

Vista: `Recuperacao / Recondicionamento`.

Funcionalidades:

- lista de candidatas com scroll interno quando existem muitas linhas;
- resumo de candidatas, em recuperacao, recondicionadas, rejeitadas e sucata;
- detalhe com severidade, motivo, retrabalho, validacao funcional e historico;
- acoes `Marcar recuperavel`, `Concluir`, `Rejeitar`;
- abertura direta do mapa de rastreabilidade da unidade.

## Grafo de rastreabilidade

O grafo passa a incluir nos `ReconditionRecord` e arestas de recuperacao/recondicionamento. Perfis de cliente continuam a receber vista sanitizada sem campos internos de qualidade.

## FIWARE

A entidade FIWARE `ProductUnit` passa a expor:

- `isReconditioned`
- `qualityDisposition`
- `recoveryStatus`
- `reconditionedAt`, quando existente

`routeState` apresenta `Reconditioned` para unidades recondicionadas.

## Grafana

O dashboard `DriveTrace Core - Qualidade e rastreabilidade` acrescenta:

- estado de recuperacao/recondicionamento por decisao;
- tabela de unidades recuperadas e recondicionadas.

A vista Vue `Grafana Analytics` tambem inclui KPI e insight de recuperacao/recondicionamento.

## Dados demo

O seed idempotente cobre:

- unidade PASS normal;
- unidade bloqueada por qualidade;
- unidade em retrabalho/recuperacao;
- candidata recuperavel;
- unidade ja recondicionada;
- unidade rejeitada para sucata;
- nao conformidade menor/media recuperavel;
- nao conformidade critica nao recuperavel;
- eventos operacionais e dados para grafo/Grafana/FIWARE.

## Validacao

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-d-reconditioning.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-d-reconditioning.ps1 -PublishFiware
```

Validacoes cobertas:

- API e Swagger;
- frontend;
- candidatos, detalhe, recondicionado e sucata;
- permissoes por perfil;
- isolamento do cliente;
- grafo;
- eventos operacionais;
- FIWARE;
- Grafana.
