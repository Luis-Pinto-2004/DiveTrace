# DriveTrace Core
DRIVOLUTION WP3 — WIP Traceability and Monitoring Platform

DriveTrace Core é uma plataforma académica para rastreabilidade e monitorização do Work-in-Progress (WIP) em contexto automóvel. A V1 centra-se em `ProductUnit`, no `Support` como âncora física intra-linha, na `Rack` como logística pós-linha, em FIWARE/Orion-LD como contexto atual, numa API ASP.NET Core e numa dashboard Vue 3.

## Requisitos

- Windows 10/11.
- Docker Desktop.
- PowerShell.
- VS Code recomendado.
- .NET 8 SDK apenas para desenvolvimento local da API.
- Node.js 20+ ou 22+ apenas para desenvolvimento local do frontend.

## Primeira execução principal

Na raiz do projeto:

```powershell
docker compose config
docker compose up -d --build
docker compose ps
```

Abrir:

```text
http://localhost:8088
```

Login:

```text
admin
admin
```

Este comando arranca o core principal SEM Grafana por defeito.

## URLs principais

| Serviço | URL |
|---|---|
| Dashboard Docker | `http://localhost:8088` |
| Dashboard dev | `http://localhost:5173` |
| Swagger | `http://localhost:5181/swagger` |
| Dashboard summary | `http://localhost:5181/api/dashboard/summary` |
| Orion-LD | `http://localhost:1026/version` |
| IoT Agent | `http://localhost:4041/iot/about` |
| QuantumLeap | `http://localhost:8668/version` |
| Grafana (opcional) | `http://localhost:33010` (ou `http://localhost:${GRAFANA_PORT}`) |

## Execuções seguintes

```powershell
docker compose up -d
docker compose ps
```

## Parar sem perder dados

```powershell
docker compose stop
```

Ou:

```powershell
docker compose down
```

Estes comandos mantêm os volumes Docker.

## Recomeçar do zero

```powershell
docker compose down -v
docker compose up -d --build
```

A opção `-v` apaga volumes e recria os dados demo.

## Grafana opcional

Para arrancar Grafana:

```powershell
docker compose --profile monitoring up -d grafana
```

Ou toda a stack com monitorização:

```powershell
docker compose --profile monitoring up -d
```

URL por defeito:

```text
http://localhost:33010
```

Login:

```text
admin
admin
```

Se a porta falhar no Windows, crie/edite `.env` na raiz com outra porta:

```env
GRAFANA_PORT=33011
```

Depois correr novamente:

```powershell
docker compose --profile monitoring up -d grafana
```

## Portas bloqueadas no Windows

Se surgir erro “ports are not available” ou “forbidden by its access permissions”, a porta está ocupada/reservada pelo Windows, Hyper-V, WSL ou Docker. Troque `GRAFANA_PORT` para outra porta alta, por exemplo 33011, 33012 ou 34010.

## Desenvolvimento local

Frontend:

```powershell
cd dashboard
npm install
npm run dev
```

API local:

```powershell
docker compose up -d db
cd api
dotnet restore
dotnet run
```

A API local precisa da base de dados ativa em `127.0.0.1:15432`.

## Testes rápidos

1. Abrir `http://localhost:8088`.
2. Entrar com `admin/admin`.
3. Confirmar PT-PT por defeito.
4. Mudar idioma para inglês e voltar para PT-PT.
5. Testar tema claro/escuro.
6. Abrir Perfil.
7. Abrir Definições.
8. Abrir Utilizadores como admin.
9. Criar utilizador.
10. Terminar sessão.
11. Entrar com o novo utilizador.
12. Confirmar que o novo utilizador não vê Gestão de Utilizadores.
13. Abrir Previsões.
14. Testar Event Playback.
15. Abrir Monitor de Contexto FIWARE.
16. Validar `http://localhost:5181/api/dashboard/summary`.

## Autenticação demo

A autenticação é local/demo no frontend. O admin existe sempre. Novos utilizadores são guardados em `localStorage`. A API ainda não está protegida por JWT. A estrutura foi preparada para evolução futura com autenticação real e RBAC.

## Dados demo

Os dados demo representam uma linha automóvel de portas, com unidades `DU`, suportes `SUP`, racks, materiais/lotes, resultados de qualidade, retrabalho, sucata e uma base simples de previsões. Os seeds principais foram traduzidos para PT-PT.

Se já existirem volumes Docker antigos, execute:

```powershell
docker compose down -v
docker compose up -d --build
```

## Limitações conhecidas

- Auth local/demo.
- API não protegida.
- Grafana opcional.
- FIWARE opcional para demonstração.
- Previsões apenas como base/futuro.
- Roles ainda não aplicam permissões funcionais profundas.
