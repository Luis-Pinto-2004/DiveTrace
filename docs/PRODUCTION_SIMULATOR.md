# Production simulator

Fase E adiciona um simulador operacional controlado para demonstrar o fluxo de uma unidade pela fabrica usando o dominio real existente.

## Objetivo

- avancar uma unidade por etapas reais de producao;
- criar eventos operacionais coerentes;
- refletir qualidade, retrabalho, recondicionamento e sucata;
- manter o grafo, FIWARE e Grafana consistentes;
- funcionar em modo manual e em auto-run controlado pela UI.

## Cenarios

| Key | Nome | Resultado |
| --- | --- | --- |
| `normal-flow` | Fluxo normal | Unidade conforme, validada em qualidade e terminada em rack. |
| `minor-recovery` | Falha menor e recondicionamento | Falha menor, retrabalho, recondicionamento e fecho em rack. |
| `critical-scrap` | Falha critica e sucata | Nao conformidade critica e descarte em sucata. |

## Endpoints

Base: `http://localhost:5181/api/simulation`

| Endpoint | Permissao | Objetivo |
| --- | --- | --- |
| `GET /scenarios` | `Simulation.Read` | Lista cenarios disponiveis. |
| `GET /state` | `Simulation.Read` | Resumo da simulacao e runs recentes. |
| `GET /runs` | `Simulation.Read` | Lista runs. |
| `GET /runs/{id}` | `Simulation.Read` | Detalhe de uma run. |
| `POST /runs` | `Simulation.Run` | Cria uma run. |
| `POST /runs/{id}/tick` | `Simulation.Run` | Avanca um passo. |
| `POST /runs/{id}/run-step` | `Simulation.Run` | Alias de tick. |
| `POST /runs/{id}/pause` | `Simulation.Manage` | Pausa a run. |
| `POST /runs/{id}/resume` | `Simulation.Manage` | Retoma a run. |
| `POST /runs/{id}/stop` | `Simulation.Manage` | Termina a run sem apagar dados. |
| `POST /runs/{id}/reset-demo` | `Simulation.Manage` | Marca a run como terminada/cancelada sem reset destrutivo. |

Payload de criacao:

```json
{
  "scenarioKey": "normal-flow",
  "name": "Demonstracao fluxo normal",
  "speed": "Manual"
}
```

## Permissoes

- `Administrator`: leitura e controlo total.
- `Supervisor`: leitura, criacao e gestao.
- `Operator`: leitura e execucao de passos.
- `QualityTechnician`: leitura e execucao de passos.
- `Logistics`: leitura.
- `DemoViewer`: leitura.
- `Customer`: sem acesso ao simulador interno.

## Eventos operacionais

Tipos principais:

- `SimulationRunCreated`
- `SimulationStepExecuted`
- `SimulationPaused`
- `SimulationResumed`
- `SimulationStopped`
- `SimulationCompleted`
- `SimulationProductUnitAdvanced`
- `SimulationQualityFailureInjected`
- `SimulationReconditioningPathExecuted`
- `SimulationScrapPathExecuted`
- `SimulationLineTransferExecuted`

Os passos usam os servicos reais de transferencia, qualidade, recondicionamento, sucata e eventos operacionais.

## Grafo

O simulador nao recria o grafo. Apenas altera o dominio relacional que a Fase C ja consome:

- `GET /api/trace-graph/product-unit/{id}`
- `GET /api/trace-graph/manufacturing-order/{id}`

Na UI existem atalhos para abrir o grafo da unidade e da ordem a partir da run selecionada.

## FIWARE

O simulador nao publica automaticamente para Orion-LD. Depois de passos relevantes, a stack continua publicavel com:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-fiware.ps1 -PublishCurrent
```

Quando publicado, o contexto FIWARE deve refletir o estado relacional da simulacao.

## Grafana

O simulador nao altera UIDs existentes. Os dados gerados aparecem nos dashboards atuais, sobretudo em qualidade, rastreabilidade e operacoes WIP.

## Dados demo

O seed idempotente adiciona:

- `OF-SIM-PORTA-001`, `OF-SIM-PORTA-002`, `OF-SIM-PORTA-003`
- `UP-SIM-001`, `UP-SIM-002`, `UP-SIM-003`
- `SUP-SIM-001`, `SUP-SIM-002`, `SUP-SIM-003`
- `RACK-SIM-001`, `RACK-SIM-002`, `RACK-SIM-003`

## Limitacoes

- sem scheduler permanente no backend;
- sem reset destrutivo;
- sem automacao em segundo plano fora do controlo da UI;
- um scenario ativo por chave de cada vez.

## Como testar

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-e-production-simulator.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-e-production-simulator.ps1 -PublishFiware
```
