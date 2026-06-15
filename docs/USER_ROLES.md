# Perfis de utilizador

Fase B introduz perfis demo, vistas por papel e guardas funcionais no backend. A autenticacao continua a ser local/demo, sem JWT, OAuth ou SSO.

## Contas demo

| Utilizador | Palavra-passe | Papel backend | Vista inicial |
| --- | --- | --- | --- |
| `admin` | `admin` | `Administrator` | Painel operacional |
| `supervisor` | `supervisor` | `Supervisor` | Painel operacional |
| `operador` | `operador` | `Operator` | Bancada do operador |
| `qualidade` | `qualidade` | `QualityTechnician` | Painel de qualidade |
| `logistica` | `logistica` | `Logistics` | Logistica pos-linha / racks |
| `cliente` | `cliente` | `Customer` | Consulta publica de cliente |
| `demo` | `demo` | `DemoViewer` | Painel demo em leitura |

## Regras por papel

- `Administrator`: acesso total, incluindo utilizadores, dados mestre, FIWARE, Grafana e escritas.
- `Supervisor`: acompanha producao, ordens, unidades, qualidade, racks, materiais, Grafana e playback; nao acede ao monitor FIWARE nem gere utilizadores.
- `Operator`: usa a bancada operacional, ve unidades/trace e pode registar transferencias controladas; nao ve racks, materiais, FIWARE, Grafana ou utilizadores.
- `QualityTechnician`: ve unidades e trace, regista qualidade e decide nao conformidades/retrabalho/sucata; nao ve logistica de racks nem FIWARE.
- `Logistics`: gere racks, atribuicoes rack-suporte, suportes e materiais; nao ve qualidade nem FIWARE.
- `Customer`: apenas consulta o portal publico por `publicTrackingCode`; nao ve dados internos, logs tecnicos, FIWARE, Grafana, utilizadores, unidades internas ou ordens CRUD.
- `DemoViewer`: perfil de demonstracao em leitura, com acesso a vistas operacionais, FIWARE/Grafana e portal de cliente, mas sem escritas.

## Backend

O frontend envia:

- `X-DriveTrace-Role`
- `X-DriveTrace-User`

O backend resolve permissoes por `PermissionCatalogService` e devolve `403` quando a operacao exige uma permissao ausente. Isto aplica-se tambem a chamadas diretas por HTTP; esconder botoes no frontend nao e a barreira principal.

Endpoints de contexto:

- `GET /api/auth/me`
- `GET /api/auth/demo-users`
- `GET /api/permissions/catalog?role=Operator`

`GET /api/auth/me` devolve `id`, `name`, `username`, `role`, `roleKey`, `permissions`, idioma/tema preferidos e entidade associada quando aplicavel.

## Frontend

- Login com botoes para todas as contas demo.
- Navegacao filtrada por permissao.
- Vista inicial por papel.
- Mensagem explicita quando uma vista e recusada.
- CRUD em modo so leitura quando o papel pode consultar mas nao escrever.
- Cliente isolado na consulta publica sem dados internos.

## Validacao

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-b-roles.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-b-roles.ps1 -PublishFiware
```
