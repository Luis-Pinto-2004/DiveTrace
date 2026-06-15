# Permissions

Fase B aplica controlo funcional real por permissoes no backend, mantendo autenticacao demo/local. Nao existe ainda JWT, OAuth, SSO ou gestao externa de identidades.

## Perfis

- `Administrator`
- `Supervisor`
- `Operator`
- `QualityTechnician`
- `Logistics`
- `Customer`
- `DemoViewer`

## Permissoes

- `Users.Manage`
- `MasterData.Manage`
- `Orders.View`
- `Orders.Manage`
- `ProductUnits.View`
- `ProductUnits.Transfer`
- `ProductUnits.Trace`
- `Supports.Manage`
- `Quality.View`
- `Quality.Record`
- `Quality.Decide`
- `Reconditioning.Read`
- `Reconditioning.Write`
- `Reconditioning.Decide`
- `Simulation.Read`
- `Simulation.Run`
- `Racks.View`
- `Racks.Manage`
- `Materials.View`
- `Materials.Manage`
- `Grafana.View`
- `Fiware.View`
- `Fiware.Manage`
- `Simulation.Manage`
- `CustomerPortal.View`
- `OperationalEvents.View`

## Matriz resumida

| Perfil | Permissoes principais |
| --- | --- |
| `Administrator` | Todas |
| `Supervisor` | Ordens, unidades, transferencias, trace, suportes, qualidade, recuperacao/recondicionamento, racks, materiais, Grafana, eventos e simulacao |
| `Operator` | Ordens em leitura, unidades, transferencias, trace, simulacao, recuperacao em leitura e eventos |
| `QualityTechnician` | Ordens em leitura, unidades, trace, qualidade, decisao de qualidade, simulacao, recuperacao/recondicionamento e eventos |
| `Logistics` | Unidades, trace, racks, materiais, suportes, simulacao, recuperacao em leitura e eventos |
| `Customer` | Apenas `CustomerPortal.View` |
| `DemoViewer` | Leitura demo: ordens, unidades, trace, qualidade, racks, materiais, FIWARE, Grafana, simulacao, portal cliente e eventos |

## API

- `GET /api/auth/me`
- `GET /api/auth/demo-users`
- `GET /api/permissions/catalog`
- `GET /api/permissions/catalog?role=Operator`
- `GET /api/permissions/profiles/{role}`
- `GET /api/permissions/me`

O backend le os cabecalhos:

- `X-DriveTrace-Role`
- `X-DriveTrace-User`
- `X-DriveTrace-Permissions`

Se nenhum cabecalho for enviado, o ambiente local assume `Administrator` para manter scripts antigos e modo demo compativeis. Quando um papel e enviado explicitamente, as guardas devolvem `403` se a permissao estiver em falta.

## Guardas funcionais

- Dashboard summary, flow summary e operator workbench: `ProductUnits.View`.
- Portal de cliente: `CustomerPortal.View`.
- CRUD de ordens: leitura `Orders.View`, escrita `Orders.Manage`.
- CRUD de unidades: leitura `ProductUnits.View`, escrita geral `MasterData.Manage`.
- Trace e historico de suporte: `ProductUnits.Trace`.
- Transferencia de unidade: `ProductUnits.Transfer`.
- Suportes: leitura `ProductUnits.View`, escrita `Supports.Manage`.
- Qualidade: leitura `Quality.View`, registo `Quality.Record`, decisoes `Quality.Decide`.
- Recuperacao/recondicionamento: leitura `Reconditioning.Read`, marcacao `Reconditioning.Write`, conclusao/rejeicao `Reconditioning.Decide`.
- Racks e atribuicoes rack-suporte: leitura `Racks.View`, escrita `Racks.Manage`.
- Materiais e lotes: leitura `Materials.View`, escrita `Materials.Manage`.
- Playback: `Simulation.Manage`.
- Simulacao leitura: `Simulation.Read`.
- Simulacao execucao: `Simulation.Run`.
- Simulacao gestao: `Simulation.Manage`.
- FIWARE leitura: `Fiware.View`.
- FIWARE publicacao: `Fiware.Manage`.
- Eventos operacionais recentes/listagem: `OperationalEvents.View`.
- Utilizadores: `Users.Manage`.
- Dados mestre/parametros: `MasterData.Manage`.

## Dashboard

O dashboard envia `X-DriveTrace-Role` e `X-DriveTrace-User` com base no utilizador local ativo. A interface:

- filtra navegacao por permissao;
- inicia cada papel na vista funcional adequada;
- mostra mensagem de acesso negado quando uma vista nao e permitida;
- mantem paineis CRUD em modo so leitura quando o papel pode consultar mas nao escrever;
- impede o perfil `Customer` de ver dados internos, logs tecnicos, FIWARE, Grafana e utilizadores.

Perfis demo disponiveis: `admin`, `supervisor`, `operador`, `qualidade`, `logistica`, `cliente` e `demo`.

## Validacao

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-b-roles.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-b-roles.ps1 -PublishFiware
```
