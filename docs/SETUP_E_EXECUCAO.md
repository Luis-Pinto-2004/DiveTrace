# Configuração e Execução — DriveTrace Core

Guia prático para colocar a plataforma a correr, em ambiente de demonstração,
e para validar rapidamente que tudo está operacional.

> A autenticação é **demo/local** — não representa segurança de produção.
> Ver `docs/NOTAS_TECNICAS.md`, secção "Permissões e autenticação".

## 1. Requisitos

| Ferramenta | Versão alvo | Notas |
|---|---|---|
| Docker + Docker Compose | recente | Caminho recomendado para a demo completa |
| .NET SDK | 8.0.x | Apenas para correr/testar a API fora do Docker |
| Node.js | 22 LTS | Apenas para desenvolvimento do frontend |
| npm | 10.x | Incluído no Node 22 |

## 2. Primeira execução (Docker — recomendado)

```bash
# A partir da raiz do repositório
cp .env.example .env        # ajustar segredos se necessário
docker compose up -d --build
```

O arranque levanta a base de dados, a API, o frontend, o FIWARE (Orion-LD,
MongoDB, IoT Agent, QuantumLeap) e o Grafana. A API semeia automaticamente os
dados demo no primeiro arranque.

### URLs

| Serviço | URL |
|---|---|
| Dashboard (frontend) | http://localhost:8088 |
| API + Swagger | http://localhost:5181/swagger |
| Saúde da API | http://localhost:5181/health |
| Grafana | http://localhost:3000 |
| Orion-LD (FIWARE) | http://localhost:1026/version |

### Credenciais demo

| Perfil | Utilizador | Palavra-passe |
|---|---|---|
| Administrador | `admin` | `admin` |
| Supervisor | `supervisor` | `supervisor` |
| Operador | `operador` | `operador` |
| Qualidade | `qualidade` | `qualidade` |
| Logística | `logistica` | `logistica` |
| Cliente | `cliente` | `cliente` |

No ecrã de início de sessão existem botões de acesso rápido para cada perfil.

## 3. Execuções seguintes

```bash
docker compose up -d          # sem reconstruir imagens
```

## 4. Parar serviços

```bash
docker compose down           # mantém volumes/dados
docker compose down -v        # remove volumes (reset total)
```

## 5. Repor a demo (reset de dados)

```bash
docker compose down -v
docker compose up -d --build
```

## 6. Desenvolvimento local (sem Docker)

### Frontend

```bash
cd dashboard
npm ci
npm run dev          # servidor de desenvolvimento em http://localhost:5173
```

### Backend

```bash
cd api
dotnet restore
dotnet run           # API em http://localhost:5181
```

A cadeia de ligação por defeito aponta para `127.0.0.1:15432` (ver
`api/appsettings.json`); ajustar conforme a base de dados disponível.

## 7. Validação rápida (smoke)

```bash
# Frontend: type-check, build e testes unitários
cd dashboard
npm run build
npm run test

# Backend: build e testes unitários
cd ..
dotnet build ./DriveTrace-Core.sln --configuration Release
dotnet test ./api/Tests/DriveTraceCore.Api.Tests.csproj

# Configuração do Docker Compose
docker compose config
```

### Testes end-to-end e Lighthouse

```bash
cd dashboard
npx playwright install --with-deps chromium
npm run test:e2e          # fluxos críticos por perfil
npm run lighthouse        # metas de performance/acessibilidade
```

## 8. Resolução de problemas

| Sintoma | Causa provável | Ação |
|---|---|---|
| Dashboard não carrega dados | API ainda a arrancar/semear | Aguardar; verificar `http://localhost:5181/health` |
| Porta ocupada | Conflito com serviço local | Ajustar portas no `docker-compose.yml` |
| Grafana sem dados | Base ainda vazia | Confirmar que a API semeou os dados demo |
| Erro de ligação à BD (local) | Porta/credenciais | Rever `appsettings.json` e a BD ativa |
| E2E falha por browser | Browsers Playwright em falta | `npx playwright install --with-deps chromium` |
