# Fase F0 - Clareza visual, mapa de chao de fabrica e analitica operacional

Esta fase melhora a leitura operacional da aplicacao sem alterar o dominio, sem introduzir previsoes e sem usar IA generativa.

## Removido

- Area de previsoes no menu e na UI.
- Antigo endpoint ativo da area de previsoes.
- Entidade/modelo EF associada a essa area.
- Dados demo de previsoes no seeder e no frontend.

A analitica passa a ser apresentada como interpretacao deterministica baseada nos dados existentes.

## Simplificado

- Os smoke tests passam a usar `scripts/wait-api.ps1` antes de chamar a API.
- O catalogo Grafana mostra inicialmente apenas tres dashboards e permite `Ver todos`.
- Os paineis embebidos de Grafana ficam limitados aos tres mais relevantes da area ativa.
- O simulador destaca primeiro o resultado do cenario e deixa detalhes tecnicos colapsados.
- A lista de runs do simulador mostra inicialmente as ultimas seis execucoes.

## Mapa de rastreabilidade

O mapa continua a usar Vue Flow e os endpoints existentes de trace graph.

- **Modo simples**: mostra a leitura operacional principal, escondendo nos tecnicos como eventos, lotes, cliente, produto e ordem.
- **Modo tecnico**: mostra materiais, eventos e restantes nos de suporte a demonstracao tecnica.
- O topo do mapa apresenta estado operacional, principal atencao, acao recomendada e evidencia.
- O painel lateral mostra detalhes do no selecionado sem expandir a pagina toda.
- O canvas usa altura controlada para funcionar melhor em 1366x768 e 1920x1080.

## Analitica operacional

A analitica principal e uma camada de regras deterministicas:

- unidades bloqueadas, FAIL ou em retrabalho indicam prioridade de qualidade;
- WIP concentrado numa seccao sugere validacao de capacidade;
- racks ocupadas sugerem verificacao de disponibilidade pos-linha;
- recondicionamento pendente sugere decisao funcional antes de libertar;
- sem sinais criticos, o estado apresentado e fluxo operacional estavel.

Esta area nao e chamada previsao e nao representa modelo estatistico.

## Validacao

```powershell
dotnet build .\api\DriveTraceCore.Api.csproj
cd dashboard
npm run build
cd ..
docker compose config
docker compose up -d --build
powershell -ExecutionPolicy Bypass -File .\scripts\wait-api.ps1
```

Smoke tests principais:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-a-foundation.ps1 -PublishFiware
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-b-roles.ps1 -PublishFiware
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-c-trace-graph.ps1 -PublishFiware
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-d-reconditioning.ps1 -PublishFiware
powershell -ExecutionPolicy Bypass -File .\scripts\test-phase-e-production-simulator.ps1 -PublishFiware
powershell -ExecutionPolicy Bypass -File .\scripts\test-fiware.ps1 -PublishCurrent
```
