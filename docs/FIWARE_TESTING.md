# FIWARE Testing Guide (DriveTrace Core)

## 1) Objetivo

Este guia explica como validar a integração FIWARE no DriveTrace Core, mantendo o princípio arquitetural do projeto:

- PostgreSQL/TimescaleDB é a fonte principal de verdade.
- FIWARE (Orion-LD + IoT Agent + QuantumLeap) é camada opcional de contexto vivo e histórico temporal.
- Se FIWARE falhar, a aplicação principal continua funcional.

## 2) Papel de cada componente

- `orion` (Orion-LD): broker NGSI-LD para estado atual/digital twin.
- `mongo-db`: persistência interna de `orion` e `iot-agent`.
- `iot-agent` (IoT Agent JSON): entrada IIoT/simulação e encaminhamento para Orion-LD.
- `quantumleap`: histórico temporal FIWARE (se houver dados/subscrições).
- `api` (.NET 8): publica snapshot relacional em FIWARE e consulta contexto.
- `dashboard` (Vue): mostra estado FIWARE de forma legível, sem bloquear o resto da UI.

## 3) Pré-requisitos

- Docker Desktop ativo.
- Portas livres: `1026`, `4041`, `7896`, `8668`, `5181`, `8088`.

## 4) Arranque da stack

```powershell
docker compose config
docker compose up -d --build
docker compose ps
```

## 5) Testes rápidos de saúde

```powershell
Invoke-WebRequest http://localhost:1026/version | Select-Object -ExpandProperty Content
Invoke-WebRequest http://localhost:4041/iot/about | Select-Object -ExpandProperty Content
Invoke-WebRequest http://localhost:8668/version | Select-Object -ExpandProperty Content
Invoke-WebRequest http://localhost:5181/swagger | Select-Object -ExpandProperty StatusCode
Invoke-WebRequest http://localhost:5181/api/fiware/context | Select-Object -ExpandProperty Content
```

Publicar contexto atual:

```powershell
Invoke-WebRequest `
  -Uri http://localhost:5181/api/fiware/publish-current `
  -Method POST `
  -ContentType 'application/json' `
  -Body '{}' | Select-Object -ExpandProperty Content
```

Esperado no payload de resposta:

- `attemptedCount`
- `publishedCount`
- `failedCount`
- `staleDeletedCount`
- `staleEntityIds`
- `entityIds`
- `errors`
- `timestamp`
- `brokerReachable`
- `orionLdBaseUrl`

Consultar novamente contexto:

```powershell
Invoke-WebRequest http://localhost:5181/api/fiware/context | Select-Object -ExpandProperty Content
```

## 6) Teste no frontend

1. Abrir `http://localhost:8088`
2. Login: `admin/admin`
3. Navegar para `Monitor de Contexto FIWARE`
4. Clicar `Atualizar contexto`
5. Clicar `Publicar contexto atual no Orion-LD`
6. Confirmar:
   - estado de ligação;
   - origem (`Orion-LD` ou fallback relacional);
   - contagem de entidades;
   - última atualização;
   - resumo de publicação (tentadas/publicadas/falhadas/órfãs removidas);
   - IDs órfãos removidos (quando existirem);
   - erros de sincronização (se existirem);
   - lista de entidades e atributos.

## 7) Endpoints FIWARE da API

- `GET /api/fiware/context`
  - tenta ler Orion-LD;
  - se Orion indisponível, devolve fallback relacional com mensagem clara;
  - inclui estado da ligação, origem, contagem, entidades, timestamp e erros.

- `POST /api/fiware/publish-current`
  - publica snapshot atual (supports, product units, racks, production line sections, checkpoints);
  - mantém upsert das entidades atuais;
  - remove do Orion-LD entidades órfãs dos tipos geridos (Support/ProductUnit/Rack/ProductionLineSection/Checkpoint);
  - usa `DELETE /ngsi-ld/v1/entities/{entityId}` para órfãs;
  - trata `404` no delete como não crítico;
  - usa payload NGSI-LD com `@context` embutido (sem dependência externa para resolver contexto);
  - devolve `attemptedCount`, `publishedCount`, `failedCount`, `staleDeletedCount`, `staleEntityIds`, `entityIds`, `errors`, `timestamp`.

## 8) Teste funcional de sincronização stale

Objetivo: garantir que o Orion-LD não mantém entidades antigas que já não existem na BD relacional.

1. Confirmar estado inicial:
   - `GET http://localhost:5181/api/fiware/context`
2. Criar (ou confirmar) entidade stale de teste:
   - `urn:ngsi-ld:Support:teste`
3. Executar:
   - `POST http://localhost:5181/api/fiware/publish-current`
4. Confirmar no payload:
   - `staleDeletedCount >= 1`
   - `staleEntityIds` inclui `urn:ngsi-ld:Support:teste`
5. Executar:
   - `GET http://localhost:5181/api/fiware/context`
6. Confirmar:
   - `entityCount == relationalSnapshotCount`
   - `urn:ngsi-ld:Support:teste` desapareceu
   - entidades válidas continuam presentes

## 9) Logs úteis

```powershell
docker compose logs --tail=120 orion
docker compose logs --tail=120 mongo-db
docker compose logs --tail=120 iot-agent
docker compose logs --tail=120 quantumleap
docker compose logs --tail=120 api
```

## 10) Problemas comuns

### 10.1 Orion devolve 503 ao publicar

Sintoma típico em logs do Orion:

- `Unable to download context: https://uri.drivolution.local/context.jsonld`

Mitigação aplicada no projeto:

- a API publica com `@context` embutido no payload;
- evita dependência de resolver URL externa de contexto.

### 10.2 IoT Agent falha com erro Mongo URI

Variável obrigatória no `docker-compose.yml`:

- `IOTA_MONGO_URI=mongodb://mongo-db:27017/iotagentjson`

Esta variável deve ser preservada.

### 10.3 `GET /ngsi-ld/v1/entities` devolve `400 Too broad query`

No Orion-LD desta stack, consultas demasiado genéricas são rejeitadas.
O serviço da API já consulta por `idPattern` para evitar esse erro.

### 10.4 Entidades stale continuam no Orion-LD após publish

`publish-current` agora executa upsert + limpeza stale para os tipos geridos.
Se as stale persistirem:

- confirmar que `staleDeletedCount` e `staleEntityIds` vêm preenchidos na resposta;
- confirmar permissões HTTP `DELETE` no endpoint Orion-LD;
- verificar logs da API para erros de delete/sincronização.

### 10.5 QuantumLeap com `WORKER TIMEOUT`

Pode acontecer em ambiente demo sem carga FIWARE estável/subscrições completas.
Se o endpoint `http://localhost:8668/version` responder e a app principal estiver funcional, tratar como limitação conhecida da demo.

## 11) Limitações atuais

- Integração IoT Agent/QuantumLeap está orientada a demo.
- Não há pipeline IIoT completo com dispositivos reais nesta V1.
- A aplicação de negócio não depende de FIWARE para CRUD e dashboard base.

## 12) Script opcional

Existe um script auxiliar:

- `scripts/test-fiware.ps1`

Executar:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-fiware.ps1
```

Com publish:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-fiware.ps1 -PublishCurrent
```

O modo `-PublishCurrent` inclui teste automático do cenário stale (`urn:ngsi-ld:Support:teste`) e valida:

- remoção da entidade stale;
- `staleDeletedCount >= 1`;
- presença do ID stale em `staleEntityIds`;
- igualdade `entityCount == relationalSnapshotCount` após sincronização.
