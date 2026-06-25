# Setup e execução

Guia para instalar e executar o DriveTrace Core localmente.

## Requisitos

- **Docker** e **Docker Compose** (forma recomendada — arranca tudo: base de dados,
  API, frontend, FIWARE e Grafana).
- Em alternativa, para correr os componentes à parte: **Node.js 20+** e **.NET 8 SDK**.

## Arranque rápido (Docker)

A partir da raiz do projeto:

```bash
docker compose up -d --build
```

Aguardar a API ficar pronta (semeia a base de dados no primeiro arranque) e abrir o
dashboard. Em Windows pode usar o script de espera:

```powershell
./scripts/wait-api.ps1
```

## Endereços

| Serviço | URL |
|---|---|
| Dashboard (frontend) | http://localhost:8088 |
| API + Swagger | http://localhost:5181/swagger |
| Saúde da API | http://localhost:5181/health |
| Grafana | http://localhost:33010 |
| Orion-LD (FIWARE) | http://localhost:1026/version |
| Base de dados (TimescaleDB) | porta 15432 |

## Utilizadores de demonstração

Autenticação local de demonstração (não é segurança de produção). Utilizador e
palavra-passe iguais:

| Perfil | Utilizador | Palavra-passe |
|---|---|---|
| Administrador | `admin` | `admin` |
| Supervisor | `supervisor` | `supervisor` |
| Operador | `operador` | `operador` |
| Qualidade | `qualidade` | `qualidade` |
| Cliente | `cliente` | `cliente` |

## Gestão do ambiente

```bash
docker compose up -d          # arrancar (sem reconstruir imagens)
docker compose down           # parar, mantendo os dados (volumes)
docker compose down -v        # parar e apagar os dados (reset total)
docker compose config         # validar a configuração do compose
```

> Evite `down -v` se quiser manter os dados de demonstração entre sessões.

## Executar os componentes à parte (sem Docker)

Frontend:

```bash
cd dashboard
npm install
npm run dev          # desenvolvimento em http://localhost:5173
npm run build        # build de produção
npm run test         # testes unitários
```

Backend:

```bash
cd api
dotnet restore
dotnet run           # API em http://localhost:5181
dotnet test ./Tests/DriveTraceCore.Api.Tests.csproj
```

## Demo de apresentação

Para uma apresentação guiada, consulte [DEMO.md](DEMO.md). Em Windows:

```powershell
./scripts/run-demo.ps1
```

## Problemas comuns

| Sintoma | Causa provável | Resolução |
|---|---|---|
| Dashboard sem dados | API ainda a arrancar/semear | Aguardar; verificar `http://localhost:5181/health` |
| Porta ocupada | Outro serviço a usar a porta | Parar o serviço em conflito ou ajustar o `docker-compose.yml` |
| Grafana sem gráficos | QuantumLeap ainda sem histórico | Gerar atividade na aplicação e aguardar a recolha |
