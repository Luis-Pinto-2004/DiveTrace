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

A demo é guiada, avança sozinha e **demonstra o ciclo completo** com ações reais sobre
um cenário de demonstração controlado:

1. **Início de sessão** — mostra o login e escreve as credenciais de Administrador.
2. **Painel de operações** — visão geral da fábrica; seleciona uma unidade para mostrar
   o seu percurso.
3. **Encomendas e ordens de fabrico** — aceita uma encomenda submetida e inicia a
   produção, **gerando as unidades de produto individuais**.
4. **Produção e suporte** — **atribui o suporte** e faz a unidade **avançar pela rota**,
   respeitando a capacidade da secção.
5. **Controlo de qualidade** — aplica uma **decisão de qualidade** (aprovação) e
   encaminha a unidade para o rack.
6. **Mapa de rastreabilidade** — abre o **grafo de uma ordem de fabrico**, com unidades,
   suporte, secções, qualidade e eventos.
7. **Acompanhamento pelo cliente** — o ciclo fecha do lado do cliente.

Em cada passo, a aplicação navega para a página certa, faz **scroll** para a zona
relevante e **destaca** o elemento em foco, enquanto a caixa no rodapé explica, em
português, o que está a acontecer.

## Controlos (caixa no rodapé)

- **Anterior / Seguinte** — avançar manualmente.
- **Reproduzir / Pausar** — ligar/desligar o avanço automático (arranca a reproduzir).
- **Recomeçar** — repor o cenário e voltar ao primeiro passo.
- **Terminar** — sair do modo de apresentação.

## Repetir e sair

- A demo é **determinística e repetível**: ao arrancar (ou ao **Recomeçar**), o cenário
  é **reposto a um estado conhecido**, pelo que **várias execuções dão sempre o mesmo
  resultado**. Não depende de estado guardado.
- Pode correr a demo **várias vezes seguidas** sem reiniciar o projeto: a caixa de
  controlo aparece sempre.
- A demo **continua a funcionar após um refresh** do browser.
- Para **sair**, carregue em **Terminar** (ou remova `?demo=1` do endereço).

## Segurança dos dados

O modo de apresentação é **não destrutivo**. As ações demonstradas (aceitar/iniciar
ordem, gerar unidades, atribuir suporte, avançar, decidir qualidade) operam apenas sobre
o **estado de demonstração do cliente**, que é **reposto no arranque** — **não criam
encomendas novas a cada execução nem escrevem na base de dados**, e não alteram
permissões nem a lógica da aplicação. Não interferem com FIWARE, Grafana, qualidade,
encomendas ou rastreabilidade reais.

## Pontos diferenciadores a sublinhar

- Produção seguida ao nível da **unidade de produto individual**, não por lotes.
- **Suporte** como elemento essencial da rastreabilidade no chão de fábrica.
- **Regras de capacidade**: uma unidade só muda de secção se houver espaço.
- **Rastreabilidade visual por grafo**, com várias perspetivas de análise.
- **Qualidade** com decisões reais (aprovar / recondicionar / sucata).
- Integração **FIWARE** (contexto NGSI-LD) e **Grafana** (monitorização temporal),
  acessíveis em http://localhost:1026 e http://localhost:3000.

## Dica

Para arranques seguintes mais rápidos: `./scripts/run-demo.ps1 -NoBuild`. Evite
`docker compose down -v` antes da apresentação, para manter os dados.
