# Mapa de rastreabilidade

Fase C adiciona uma visualizacao grafica operacional dentro da dashboard Vue. O grafo e nativo da aplicacao, usa `@vue-flow/core` no frontend e continua a usar o backend relacional como fonte de verdade. Grafana permanece para analitica e monitorizacao, nao como motor do grafo.

## Endpoints

Base: `http://localhost:5181/api/trace-graph`

| Endpoint | Permissao | Objetivo |
| --- | --- | --- |
| `GET /factory` | `ProductUnits.View` | Mapa atual do chao de fabrica com linhas, seccoes, WIP, suportes e racks visiveis ao perfil. |
| `GET /product-unit/{id}` | `ProductUnits.Trace` | Rota e genealogia operacional de uma ProductUnit. |
| `GET /manufacturing-order/{id}` | `Orders.View` ou `CustomerPortal.View` | Grafo da ordem de fabrico. Cliente so ve ordens proprias e dados sanitizados. |
| `GET /options` | `ProductUnits.View`, `Orders.View` ou `CustomerPortal.View` | Opcoes para seletores de unidade, ordem, linha, estados e modos suportados. |
| `GET /fiware-status` | `Fiware.View` | Estado auxiliar de coerencia FIWARE para validacao operacional. |

DTOs principais:

- `TraceGraphDto`
- `TraceGraphNodeDto`
- `TraceGraphEdgeDto`
- `TraceGraphSummaryDto`
- `TraceGraphWarningDto`
- `TraceGraphLegendItemDto`
- `TraceGraphOptionsDto`

## Tipos de nos

- `ProductionLine`
- `Section`
- `ProductUnit`
- `Support`
- `Rack`
- `ManufacturingOrder`
- `Customer`
- `Product`
- `MaterialLot`
- `Quality`
- `Nonconformity`
- `Rework`
- `ReconditionRecord`
- `Scrap`
- `Event`

Os perfis sem permissao de qualidade, materiais, eventos ou logistica recebem grafos reduzidos. O cliente recebe apenas uma leitura por ordem, limitada ao seu `CustomerCode`.

## Tipos de arestas

- `contains`: linha contem seccao.
- `route`: fluxo nominal entre seccoes.
- `transfer`: transferencia possivel entre linhas.
- `current-location`: localizacao atual de WIP.
- `movement`: historico de movimentos da unidade.
- `product-of`, `produces`, `belongs-to`: relacoes de ordem/produto/cliente.
- `transported-by`, `stored-in`: ancoragem fisica e logistica pos-linha.
- `consumed-lot`: genealogia de lotes.
- `quality-result`, `nonconformity`, `rework`, `reconditioning`, `scrap`: evidencia de qualidade e recuperacao produtiva.
- `generated-event`: eventos operacionais associados.

## Frontend

Menu: `Mapa de rastreabilidade`.

Modos:

- `Chao de fabrica`: visao multi-linha, com WIP atual, seccoes, linhas, suportes e racks.
- `Rota da unidade`: historico e genealogia de uma `ProductUnit`.
- `Ordem de fabrico`: unidades e evidencias por ordem.

Controlos:

- zoom e pan nativos do Vue Flow.
- botoes `Ajustar ao ecra`, `Recentrar` e `Atualizar`.
- filtros por linha, estado, estado de qualidade e tipo de no.
- alternadores de vista compacta, materiais e eventos.
- legenda operacional.
- painel lateral de detalhe com metadados e relacoes visiveis.

Interacoes:

- clicar numa `ProductUnit` no chao de fabrica abre a respetiva rota.
- clicar numa `Section` no chao de fabrica foca essa seccao e o WIP relacionado.
- o painel lateral mostra metadados e arestas ligadas ao no selecionado.
- a vista `Simulador de produção` permite abrir o grafo da unidade ou da ordem ativa sem refazer o componente.

## Permissoes

- `Administrator`: grafo completo.
- `Supervisor`: grafo operacional sem endpoint FIWARE status.
- `Operator`: grafo filtrado pela linha/seccao atribuida.
- `QualityTechnician`: unidades, rota e evidencias de qualidade.
- `Logistics`: unidades, suportes, racks e materiais; sem qualidade.
- `Customer`: sem acesso ao mapa interno; API de ordem apenas para ordens proprias.
- `DemoViewer`: leitura ampla, sem escritas.

## Dados demo

Nao foram adicionados seeds novos na Fase C. O grafo usa os dados ja existentes:

- linhas `LINHA-01` a `LINHA-04`
- seccoes `SEC-MP`, `SEC-ATRIB-SUP`, `SEC-CORTE-ESTAMP`, `SEC-SOLD`, `SEC-PINT-A`, `SEC-PINT-B`, `SEC-CQ`, `SEC-RETRAB`, `SEC-RACK`
- `ProductUnit`, `Support`, `Rack`, lotes, qualidade, nao conformidades, retrabalho, recuperacao/recondicionamento, sucata e `OperationalEvents`

## FIWARE e Grafana

- FIWARE continua a ser publicado por `POST /api/fiware/publish-current`.
- O grafo le dados relacionais, inclui `ReconditionRecord` para perfis internos e mantem o endpoint auxiliar de estado FIWARE.
- Grafana mantem datasource e UIDs; o dashboard de qualidade acrescenta paineis de recuperacao/recondicionamento.

## Validacao

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-c-trace-graph.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-c-trace-graph.ps1 -PublishFiware
```

Validacoes cobertas:

- endpoints da Fase C;
- existencia de nos, arestas, legenda e recomendacao operacional;
- permissoes por perfil;
- isolamento do cliente;
- estado FIWARE;
- saude, datasource e dashboards Grafana provisionados.
