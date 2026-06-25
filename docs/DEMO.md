# Demo de apresentação

Guião para apresentar o DriveTrace Core de forma calma e controlada. A demo mostra a
aplicação como uma **plataforma de rastreabilidade e monitorização de WIP para produção
automóvel**, e não apenas como um CRUD.

## Como arrancar

A forma mais simples (Windows):

```powershell
./scripts/run-demo.ps1
```

O script arranca o ambiente (sem apagar dados), espera pela API e abre o browser já no
**modo de apresentação**, autenticado como **Administrador** (o perfil que dá acesso a
todas as funcionalidades).

Em alternativa, com o ambiente já a correr, basta abrir:

```
http://localhost:8088/login?demo=1
```

> O modo de apresentação é **opcional** e **não destrutivo**: apenas guia a navegação e
> mostra os pontos a explicar. Não altera permissões nem a lógica da aplicação, e
> reutiliza os dados de demonstração existentes. Para sair, carregue em **Sair** no
> rodapé (ou remova `?demo=1` do endereço).

## Como funciona o modo de apresentação

No rodapé aparece uma barra com os passos guiados:

- **Anterior / Seguinte** — avançar manualmente.
- **Reproduzir / Pausar** — avanço automático (cerca de 16 segundos por passo), tempo
  suficiente para explicar cada ecrã.
- **Sair** — termina o modo de apresentação.

Cada passo navega para a página certa e mostra os pontos a destacar.

## Fluxo da apresentação

1. **Visão geral da fábrica** (Painel de operações)
   - KPIs do turno: em curso, concluídas, em análise de qualidade, recondicionamento.
   - Visão rápida da produção: linhas, secções e unidades em curso.
   - Selecionar uma unidade mostra o seu percurso, estado e próxima etapa.

2. **Encomendas e ordens de fabrico**
   - Uma encomenda de cliente dá origem a uma ordem de fabrico.
   - Aceitar a encomenda cria as unidades de produto individuais.
   - Iniciar a produção coloca as unidades na linha.

3. **Análise por linha e suporte**
   - Movimentação das unidades pelas linhas e secções.
   - O **suporte** é o elemento central da rastreabilidade: cada unidade ativa anda
     associada a um suporte.
   - Carga por secção, gargalos e ações operacionais (avançar etapa, transferir).

4. **Controlo de qualidade**
   - Decisões na Linha 4: aprovar, recondicionar ou marcar como sucata.
   - O recondicionamento é um desvio controlado, mantendo a rastreabilidade.

5. **Mapa de rastreabilidade**
   - Grafo técnico com cliente, ordem, unidade, suporte, secção/linha, qualidade,
     recondicionamento, sucata, rack e eventos.
   - Três perspetivas: fábrica, ordem de fabrico e unidade.
   - Selecionar um nó destaca as ligações relacionadas e abre o detalhe lateral.

6. **Analítica e FIWARE** (separador externo)
   - **Grafana** (http://localhost:3000, `admin`/`admin`): histórico e tendências de
     WIP, qualidade e fluxo.
   - **FIWARE / Orion-LD** (http://localhost:1026): contexto da fábrica como entidades
     NGSI-LD. Demonstra rastreabilidade e contexto interoperável.

7. **Área de cliente**
   - O cliente consulta e cria as suas encomendas, sem aceder aos dados internos da
     fábrica. Fecha o ciclo: do pedido do cliente à unidade rastreável na fábrica.

## Pontos diferenciadores a sublinhar

- Produção seguida ao nível da **unidade de produto individual**, não por lotes.
- **Suporte** como elemento essencial da rastreabilidade no chão de fábrica.
- **Rastreabilidade visual por grafo**, com várias perspetivas de análise.
- **Qualidade** com decisões reais (aprovar / recondicionar / sucata).
- Integração **FIWARE** (contexto NGSI-LD) e **Grafana** (monitorização temporal).

## Dicas

- Para uma demonstração mais rápida nas próximas vezes: `./scripts/run-demo.ps1 -NoBuild`.
- Evite `docker compose down -v` antes da apresentação, para manter os dados de demo.
