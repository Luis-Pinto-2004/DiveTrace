# Validation report

## Environment limits during packaging

The project was prepared in an execution environment where the following developer tools were not available:

- .NET SDK: `dotnet` command not found.
- Docker CLI: `docker` command not found.
- NPM dependency installation could not complete because external registry access was unavailable from the packaging environment.

Because of this, backend compilation and full Docker execution were not executed here. The project has been structured with standard .NET 8, Vue 3, Vite and Docker Compose conventions so it can be validated on a normal Windows + VS Code + Docker Desktop environment.

## Commands prepared for local validation

```bash
docker compose config
docker compose up -d --build
```

```bash
cd api
dotnet restore
dotnet build
dotnet run
```

```bash
cd dashboard
npm install
npm run build
npm run dev
```

## Manual review performed

- Legacy source folders were not carried into the active core.
- The active backend namespace is `DriveTraceCore.Api`.
- Runtime branding uses DriveTrace Core and DRIVOLUTION.
- The frontend source was rebuilt with automotive WIP pages.
- The domain model contains ProductUnit, Support, Rack, raw material lots, quality, nonconformity, rework and scrap records.
- The Docker Compose file exposes API on `5181`, dashboard on `8088`, PostgreSQL/TimescaleDB on `15432`, Orion-LD on `1026` and Grafana on `3004`.
