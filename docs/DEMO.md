# Demo de apresentação

Guião para apresentar o DriveTrace Core de forma calma e controlada. A demo mostra a
aplicação como uma **plataforma de rastreabilidade e monitorização de WIP para produção
automóvel**, e não apenas como um CRUD.

## Como arrancar

Forma mais simples (Windows):

```powershell
./scripts/run-demo.ps1
```

O script arranca o ambiente (sem apagar dados), espera pela API e abre o browser já no
**modo de apresentação**.

Com o ambiente já a correr, basta abrir:

```
http://localhost:8088/login?demo=1
```

## O que acontece automaticamente

A demo é guiada e avança sozinha pelos passos principais:

1. **Início de sessão** — mostra o ecrã de login e escreve as credenciais de
   Administrador (o perfil que dá acesso a todas as funcionalidades).
2. **Painel de operações** — visão geral da fábrica e prioridades do turno.
3. **Encomendas e ordens de fabrico** — do pedido de cliente às unidades individuais.
4. **Produção e suporte** — o suporte como elemento central da rastreabilidade.
5. **Controlo de qualidade** — aprovar, recondicionar ou marcar sucata.
6. **Mapa de rastreabilidade** — o grafo técnico com todas as relações.
7. **Acompanhamento pelo cliente** — o ciclo fecha do lado do cliente.

Em cada passo, a aplicação **navega para a página certa**, faz **scroll** para a zona
relevante e **destaca** o elemento em foco, enquanto a caixa no rodapé explica, em
português, o que está a ser mostrado.

## Controlos (caixa no rodapé)

- **Anterior / Seguinte** — avançar manualmente.
- **Reproduzir / Pausar** — ligar/desligar o avanço automático (a demo arranca já a
  reproduzir).
- **Recomeçar** — voltar ao primeiro passo.
- **Terminar** — sair do modo de apresentação.

## Repetir e sair

- Para **repetir**, basta voltar a correr o script ou abrir de novo
  `http://localhost:8088/login?demo=1`. A demo é determinística: arranca sempre do
  início, com os controlos visíveis. Não depende de estado guardado.
- Para **sair**, carregue em **Terminar** (ou remova `?demo=1` do endereço). O estado da
  apresentação é limpo.
- A demo **continua a funcionar após um refresh** do browser, retomando a página atual.

## Segurança dos dados

O modo de apresentação é **não destrutivo**: apenas guia a navegação, faz scroll e
destaca elementos. Não cria encomendas novas a cada execução, não altera permissões nem
a lógica da aplicação, e não interfere com FIWARE, Grafana, qualidade, encomendas ou
rastreabilidade. Reutiliza os dados de demonstração existentes.

## Pontos diferenciadores a sublinhar

- Produção seguida ao nível da **unidade de produto individual**, não por lotes.
- **Suporte** como elemento essencial da rastreabilidade no chão de fábrica.
- **Rastreabilidade visual por grafo**, com várias perspetivas de análise.
- **Qualidade** com decisões reais (aprovar / recondicionar / sucata).
- Integração **FIWARE** (contexto NGSI-LD) e **Grafana** (monitorização temporal),
  acessíveis em http://localhost:1026 e http://localhost:3000.

## Dica

Para arranques seguintes mais rápidos: `./scripts/run-demo.ps1 -NoBuild`. Evite
`docker compose down -v` antes da apresentação, para manter os dados de demonstração.
