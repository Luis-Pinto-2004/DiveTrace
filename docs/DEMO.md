# Demo de apresentação

Guião para apresentar o DriveTrace Core como uma **narrativa visual**: a demo acompanha
**uma unidade concreta do início ao fim**, para mostrar a rastreabilidade WIP no chão de
fábrica de forma clara, mesmo para quem vê a aplicação pela primeira vez.

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

## O que a demo mostra

A demo segue **a mesma unidade** ao longo de todo o percurso. Em cada passo, a caixa no
rodapé indica sempre **qual é a unidade acompanhada e onde está agora**, e cada ação fica
**destacada no ecrã** com uma confirmação (por exemplo “✓ Suporte ativo”):

1. **Início de sessão** — entra como Administrador (escrita simulada das credenciais).
2. **Painel de operações** — seleciona a unidade a acompanhar e mostra-a no fluxo.
3. **Encomenda em produção** — destaca a encomenda dessa unidade.
4. **Suporte e rastreabilidade** — mostra o suporte que torna a unidade rastreável.
5. **Avanço pela rota** — a unidade avança para a etapa seguinte, respeitando a
   capacidade da secção; vê-se a mudança de secção.
6. **Controlo de qualidade** — aplica a decisão (aprovação) e encaminha para o rack.
7. **Acompanhamento pelo cliente** — destaca a encomenda na área de cliente.
8. **Mapa de rastreabilidade** — abre o grafo dessa ordem, com unidade, suporte, secções,
   qualidade e eventos.

Em cada passo a aplicação navega para a página certa, faz **scroll** para a zona
relevante e **destaca** o elemento afetado, com tempo suficiente para observar a
alteração antes e depois da ação.

## Controlos (caixa no rodapé)

- **Anterior / Seguinte** — avançar manualmente.
- **Pausar / Retomar** — ligar/desligar o avanço automático (arranca a reproduzir).
- **Recomeçar** — repor o cenário e voltar ao primeiro passo.
- **Terminar** — sair do modo de apresentação.

## Repetir e sair

- A demo é **determinística e repetível**: ao arrancar (ou ao **Recomeçar**), o cenário
  e a unidade acompanhada são **repostos ao mesmo estado inicial**. **Várias execuções
  dão sempre o mesmo resultado** e a caixa de controlo **aparece sempre**.
- Pode correr a demo **várias vezes seguidas** sem reiniciar o projeto nem limpar volumes.
- A demo **continua a funcionar após um refresh** do browser.
- Para **sair**, carregue em **Terminar** (ou remova `?demo=1` do endereço).

## Segurança dos dados

O modo de apresentação é **não destrutivo**. As ações demonstradas (selecionar, suporte,
avançar, decidir qualidade) operam apenas sobre o **estado de demonstração do cliente**,
que é **reposto no arranque** — **não criam encomendas novas nem escrevem na base de
dados**, e não interferem com FIWARE, Grafana, qualidade, encomendas ou rastreabilidade
reais.

## Pontos diferenciadores a sublinhar

- Produção seguida ao nível da **unidade de produto individual**, não por lotes.
- **Suporte** como elemento essencial da rastreabilidade no chão de fábrica.
- **Regras de capacidade**: uma unidade só muda de secção se houver espaço.
- **Rastreabilidade visual por grafo**, com várias perspetivas de análise.
- **Qualidade** com decisões reais (aprovar / recondicionar / sucata).
- Integração **FIWARE** (contexto NGSI-LD) e **Grafana** (monitorização temporal),
  acessíveis em http://localhost:1026 e http://localhost:33010.

## Dica

Para arranques seguintes mais rápidos: `./scripts/run-demo.ps1 -NoBuild`. Evite
`docker compose down -v` antes da apresentação, para manter os dados.
