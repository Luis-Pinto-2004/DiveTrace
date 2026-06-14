# Permissions

Fase A adiciona uma base simples e escalavel de permissoes para o modo demo/local. Nao introduz OAuth, SSO ou autenticacao pesada.

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

## API

- `GET /api/permissions/catalog`
- `GET /api/permissions/catalog?role=Operator`
- `GET /api/permissions/profiles/{role}`
- `GET /api/permissions/me`

O backend le os cabecalhos:

- `X-DriveTrace-Role`
- `X-DriveTrace-User`
- `X-DriveTrace-Permissions`

Se nenhum cabecalho for enviado, o ambiente local assume `Administrator` para manter os scripts e o modo demo compativeis. Quando uma funcao e enviada explicitamente, as guardas devolvem `403` se a permissao estiver em falta.

## Guardas iniciais

- Transferencia de unidade: `ProductUnits.Transfer`.
- Trace e eventos por unidade: `ProductUnits.Trace`.
- Eventos operacionais recentes/listagem: `OperationalEvents.View`.
- Escritas CRUD gerais: `MasterData.Manage`.
- Qualidade CRUD: `Quality.Record` ou `Quality.Decide`.
- Racks CRUD especializado: `Racks.Manage`.
- Playback: `Simulation.Manage`.
- FIWARE leitura: `Fiware.View`.
- FIWARE publicacao: `Fiware.Manage`.

## Dashboard

O dashboard envia `X-DriveTrace-Role` e `X-DriveTrace-User` com base no utilizador local ativo e filtra vistas/acesso por permissao. Os perfis demo disponiveis sao `admin`, `supervisor`, `operador`, `qualidade`, `logistica`, `cliente` e `demo`.
