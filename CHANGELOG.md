# Changelog — DriveTrace Core

Formato baseado em *Keep a Changelog*. Datas em ISO 8601.

## [2.20.1] — 2026-06-25

### Corrigido — conflito de porta do IoT Agent e portas na documentação
- `docker-compose.yml`: o **IoT Agent** passou a publicar a API no host em **`14041`**
  (`14041:4041`), evitando o erro de arranque em Windows quando a porta `4041` cai numa
  gama reservada pelo sistema. **A porta interna mantém-se `4041`**: `IOTA_NORTH_PORT` e
  `IOTA_PROVIDER_URL` (`http://iot-agent:4041`) não foram alterados, pelo que a
  comunicação dentro da rede Docker continua igual. O acesso externo passa a ser
  `http://localhost:14041`.
- `scripts/test-fiware.ps1`: a verificação externa do IoT Agent passou a usar
  `http://localhost:14041/iot/about` (continua a validar Orion-LD, IoT Agent,
  QuantumLeap, API e o contexto FIWARE).
- `docs/FIWARE.md`: tabela de portas atualizada (host → Docker), clarificando a porta
  interna `4041`, a externa `14041` e o endpoint de payload/dispositivos `7896`.
- Correção de coerência: as referências ao **Grafana** na documentação e no
  `run-demo.ps1` passaram de `localhost:3000` para **`localhost:33010`**, que é a porta
  publicada no host por defeito (`${GRAFANA_PORT:-33010}:3000`).


## [2.20.0] — 2026-06-25

### Alterado — demo como narrativa visual de uma unidade concreta
- A demo deixou de ser uma sequência de saltos entre páginas e passou a **acompanhar a
  mesma unidade do início ao fim**: seleção no painel, suporte, avanço pela rota
  (respeitando a capacidade), controlo de qualidade e encaminhamento para rack,
  acompanhamento pelo cliente e grafo de rastreabilidade da ordem.
- A caixa de apresentação foi **redesenhada** (formatação estável em todas as páginas) e
  passou a mostrar sempre **qual a unidade acompanhada e onde está**, com uma
  **confirmação visível da ação** de cada passo (por exemplo “✓ Suporte ativo”,
  “✓ Avançou para Controlo de qualidade”, “✓ Qualidade validada”).
- Cada passo **destaca o elemento afetado** (encomenda, unidade, secção, cartão de
  qualidade, nó do grafo) e dá **tempo para observar** a alteração antes e depois da
  ação, em vez de mudanças instantâneas.
- A análise por linha passa a **focar automaticamente a linha da unidade selecionada**,
  para a unidade acompanhada estar sempre visível.
- Mantém-se **determinística, repetível e não destrutiva**: repõe o cenário e a unidade
  no arranque, a caixa de controlo aparece sempre, e nada é escrito na base de dados.
- Controlos: Anterior / Pausar / Retomar / Seguinte / Recomeçar / Terminar.


## [2.19.0] — 2026-06-25

### Alterado — regras de capacidade consolidadas e visíveis
- A regra de capacidade das secções passou a estar **aplicada de forma consistente** em
  todos os pontos de movimento: avanço da rota, transferência entre linhas, decisões de
  qualidade, recondicionamento e armazenamento/rack. Se a secção de destino estiver
  cheia, a unidade **não avança nem se sobrepõe** e fica em espera com a mensagem
  **“Secção cheia, aguarda disponibilidade”**. As exceções de capacidade alargada
  (matérias-primas, buffers, rack e expedição) mantêm-se.
- **Interface reflete a indisponibilidade**: o botão de avançar fica desativado e avisa
  quando a próxima secção está cheia; o diálogo de transferência marca as secções cheias
  e bloqueia a confirmação; o botão de recondicionar fica desativado quando o
  recondicionamento está cheio; o detalhe da unidade mostra o estado da próxima etapa.
- Novos getters de apoio (`advanceInfo`, `decisionTarget`, `sectionName`) e **3 testes**
  que confirmam que uma unidade não avança/transfere/decide para uma secção cheia
  (total: 63 testes).

### Alterado — demo do ciclo completo, não destrutiva e repetível
- A demo passou a **demonstrar o ciclo completo com ações reais**: aceitar e iniciar uma
  ordem (gerando as unidades individuais), atribuir suporte e avançar pela rota
  (respeitando a capacidade), aplicar uma decisão de qualidade e abrir o **grafo de uma
  ordem de fabrico**, terminando na área de cliente.
- O cenário é **reposto a um estado conhecido no arranque** (e no **Recomeçar**), pelo
  que **várias execuções dão sempre o mesmo resultado**. As ações operam apenas sobre o
  estado de demonstração do cliente: **não criam encomendas novas nem escrevem na base
  de dados**.
- Mantém a navegação automática, o scroll, o destaque dos elementos e a caixa de
  controlos (Anterior / Reproduzir / Pausar / Seguinte / Recomeçar / Terminar).


## [2.18.0] — 2026-06-25

### Corrigido — modo de apresentação determinístico
- O modo de demo passou a ser **orientado pelo URL** (`?demo=1`), sem qualquer estado
  guardado em memória, `localStorage` ou `sessionStorage`. A **caixa de controlo aparece
  sempre** que a aplicação é aberta em modo demo, em execuções repetidas e **após
  refresh** do browser.
- O guard de autenticação passou a **autenticar como Administrador** quando há `?demo=1`
  (sem perder o modo ao redirecionar) e a permitir mostrar o ecrã de login. Ao **sair**,
  o estado da apresentação é limpo; ao **recomeçar**, volta ao primeiro passo.

### Alterado — demo mais automática e visual
- A demo **avança sozinha** pelas páginas, faz **scroll automático** para a zona
  relevante e **destaca** visualmente o elemento em foco, mantendo a caixa de
  controlos no rodapé (**Anterior / Reproduzir / Pausar / Seguinte / Recomeçar /
  Terminar**).
- No passo de login, **simula a escrita** das credenciais de Administrador. O fluxo
  cobre login, painel de operações, encomendas/produção, suporte, qualidade,
  rastreabilidade e área de cliente, com mensagens curtas em PT-PT por passo.
- **Não destrutivo**: não cria encomendas novas a cada execução nem altera dados,
  permissões ou a lógica da aplicação; reutiliza os dados de demonstração existentes.

### Alterado — documentação da demo
- `docs/DEMO.md` reescrito para refletir o funcionamento real: arranque, o que acontece
  automaticamente, controlos, como repetir, como sair e pontos a destacar.


## [2.17.0] — 2026-06-25

### Corrigido — grafo do Mapa de rastreabilidade (sem caixas sobrepostas)
- Adicionada uma **compactação por coluna que garante um espaçamento mínimo** entre
  caixas, eliminando sobreposições, sobretudo na zona "Secção / Linha" e onde várias
  unidades passam pela mesma etapa. O alinhamento por faixas mantém-se; só afasta os
  nós onde colidiriam.
- **Mais espaço vertical** por faixa, **colunas e nós mais largos** (texto cabe melhor).
  Estrutura, filtros, painel lateral e realce de ligações ao selecionar mantêm-se.

### Adicionado — modo de apresentação (demo) e script
- Novo **modo de apresentação opcional** ativado por `?demo=1` (ex.:
  `http://localhost:8088/login?demo=1`). Mostra passos guiados no rodapé (Anterior /
  Reproduzir / Seguinte / Sair), com avanço manual ou automático, autentica como
  **Administrador** e percorre: visão geral, encomendas/produção, análise por linha e
  suporte, qualidade, rastreabilidade, analítica/FIWARE e área de cliente.
- O modo demo é **não destrutivo**: não altera permissões nem a lógica da aplicação,
  reutiliza os dados existentes e não interfere com FIWARE, Grafana, qualidade,
  encomendas ou rastreabilidade.
- Novo script **`scripts/run-demo.ps1`**: arranca o ambiente (sem apagar dados), espera
  pela API e abre o browser já em modo de apresentação.

### Alterado — documentação final limpa
- **`README.md` reescrito** como porta de entrada clara: o que é, problema que resolve no
  DRIVOLUTION WP3, módulos, tecnologias, arranque, utilizadores demo, testes e demo.
- Pasta `docs` organizada: mantidas `diagrams/` e `postman/`, **`DESIGN_NOTES.md` movido**
  de `docs/mockups/` para `docs/`, e **`mockups/` removida**. Removidos os ficheiros de
  fases antigas/intermédias.
- Documentação final reescrita e coerente: **Setup e execução**, **FIWARE**, **Grafana**,
  **Demo de apresentação** e **Perfis**.


## [2.16.0] — 2026-06-24

### Alterado — perfis de utilizador (remoção do perfil "Logística")
- O perfil de acesso **Logística** foi **removido** como tipo de utilizador. A sua
  função (racks, armazenamento e movimentações pós-linha, além da **Gestão de dados**)
  passou para o **Supervisor**, que assume a visão de gestão operacional e logística.
- Ficam apenas os perfis com diferenças reais: **Administrador, Supervisor, Operador,
  Qualidade e Cliente**.
- Atualizadas todas as referências de **acesso/login/permissões**: utilizador demo e
  botão de login rápido de logística removidos, `RoleKey` e mapa de perfis sem
  logística, **Gestão de dados** acessível a Administrador e Supervisor, navegação e
  router atualizados, documentação (`PERFIS.md`) e testes ajustados.
- **Mantidos** os conceitos de **logística do domínio** (racks, suportes, expedição,
  logística pós-linha) e o **tipo de recurso "Logística"** na página de Funcionários.

### Alterado — Mapa de rastreabilidade: legibilidade das ligações do grafo
- **Alinhamento por faixas:** cada unidade passa a ter a sua linha, com o **suporte,
  secção/linha, qualidade, recondicionamento, sucata e rack alinhados** horizontalmente
  com a unidade; as **ordens ficam centradas** nas suas unidades e os **clientes**
  centrados nas suas ordens. O percurso de cada encomenda fica visualmente mais lógico.
- **Mais espaço vertical** entre nós dentro de cada coluna (clientes, ordens, unidades,
  suportes, secções, qualidade, etc.), para seguir as ligações sem cruzamentos.
- **Ligações discretas por omissão** e com **realce na interação:** ao passar o rato ou
  selecionar um nó (cliente, ordem, unidade, suporte…), só as ligações e nós
  relacionados ganham destaque, enquanto os restantes ficam apagados. Mantém-se a visão
  completa da fábrica, mas a análise de um percurso específico fica muito mais fácil.
- Alterações concentradas no grafo (`TraceGraph`/`TraceMapView`); sem mexer na lógica de
  produção, FIWARE, qualidade, cliente, encomendas ou análise por linha.


## [2.15.0] — 2026-06-24

### Alterado — Painel de operações mais limpo e orientado a análise
- Reorganização da página: **KPIs no topo** (Unidades em curso, Concluídas, Em análise
  de qualidade, Recondicionamento), depois a faixa de **análise** (Distribuição por
  estado / Taxa de aprovação + Concentração de WIP por secção) e, por fim, a faixa de
  **produção** (Visão rápida + Percurso da unidade).
- Removidas as caixas **"WIP por linha"** (repetia informação) e **"Histórico recente"**
  (ocupava espaço sem ser essencial à leitura rápida).
- A **Visão rápida da produção** passou a ocupar mais largura e maior destaque (é o
  elemento visual principal); o **Percurso da unidade** fica ao lado ou abaixo conforme
  o ecrã.
- O **Percurso da unidade** é agora **apenas informativo** (percurso, estado, próxima
  etapa, alertas de qualidade e eventos recentes). As ações **Avançar etapa** e
  **Transferir** ficam exclusivamente na **Análise por linha**, onde faz sentido executar
  operações.

### Alterado — Mapa de rastreabilidade: grafo técnico real (TexPact, adaptado)
- A página voltou ao conceito de **grafo técnico de rastreabilidade**: permite analisar
  as relações entre **Cliente, Encomenda, Ordem de fabrico, Materiais/lotes, Unidades de
  produto, Suporte, Secções/Linhas, Qualidade, Recondicionamento/Sucata, Racks e
  Eventos** (não apenas um fluxo por linhas).
- **Três perspetivas** com seletor claro: **Fábrica** (chão de fábrica como um todo),
  **Unidade de produto** (rota, suporte, qualidade, materiais e eventos de uma unidade)
  e **Ordem de fabrico** (todas as unidades e materiais de uma ordem). Nas perspetivas
  de unidade/ordem há uma lista para escolher o elemento.
- **Layout agrupado por categoria** (colunas por grupo) com espaçamento adequado e
  ligações limpas, com direção de leitura da esquerda para a direita, para deixar de
  parecer um amontoado de caixas mesmo com muitos nós.
- **Nós com o essencial** (tipo, código e estado, com uma marca visual); os **dados
  completos** (rota, suporte, qualidade, eventos, materiais, metadados) aparecem no
  **painel lateral** ao selecionar o nó. **Zoom, pan e seleção** funcionais.
- **Cores e estilos por tipo/estado**: unidade em produção, concluída, com problema de
  qualidade, em recondicionamento, em sucata, suporte, material/lote, rack, evento e
  marca de **fora da rota**; ligações distintas para rota, suporte, qualidade,
  recondicionamento, material e armazenamento. **Legenda discreta**. Os grupos
  secundários (materiais e eventos) começam **ocultos** e ativam-se nos filtros.
- **Sem duplicação de título**: o título fica no cabeçalho da página; o conteúdo tem
  apenas o seletor/filtros e o grafo.
- A implementação **respeita o modelo existente** (mesmos tipos de nó, ligações e as 3
  perspetivas do serviço de rastreabilidade do backend), mas o grafo é agora desenhado a
  partir dos dados da aplicação, pelo que é legível e funcional **mesmo sem o backend
  ligado**. Não foram alterados o Painel de operações, a Análise por linha, a área de
  cliente, o FIWARE, os dados demo, as permissões nem os testes.


## [2.14.0] — 2026-06-23

### Alterado — Painel de operações como dashboard de leitura rápida
- O bloco "Fluxo da fábrica" deu lugar à **Visão rápida da produção** (a vista
  simplificada do chão de fábrica): linhas, secções e unidades em curso, com estados e
  prioridades à vista, sem ter de interpretar o grafo técnico.
- O bloco "Detalhe da unidade" passou a **Percurso da unidade**: ao selecionar uma
  unidade mostra, de forma simples e alinhada, o estado atual, a **próxima etapa**, o
  **suporte associado**, eventuais **problemas de qualidade**, a **rota prevista** e os
  eventos recentes (reutilizando a lógica visual da vista simplificada).
- Layout equilibrado e responsivo: a vista principal ocupa o espaço maior e o percurso
  fica ao lado ou abaixo conforme o ecrã, sem scroll horizontal desnecessário. O resto
  do painel (indicadores, qualidade, gargalos) mantém-se.

### Alterado — Mapa de rastreabilidade dedicado ao grafo técnico (reconstruído)
- A página deixou de ter o seletor de vistas; **abre diretamente no grafo técnico**
  (a vista simplificada vive agora no Painel de operações).
- **Grafo reconstruído de raiz** com leitura clara em vez de nós amontoados:
  organização em **swimlanes por linha**, **secções em colunas** e **unidades dentro
  da secção**, com **espaçamento generoso** e sem caixas sobrepostas.
- **Cores por estado** (em produção, em análise, recuperação/recondicionamento,
  aprovado/concluído, sucata), **indicador de suporte** (ativo/planeada/sem suporte) e
  marca de **fora da rota**, além das **ligações de rota** entre secções.
- **Detalhes secundários só por interação:** ao selecionar um nó, o **painel lateral**
  (compacto) mostra a encomenda, o percurso completo, o suporte, a qualidade e os
  eventos. **Filtros** por linha e por tipo de problema, **zoom/pan** funcionais e
  **legenda discreta**. O grafo passou a funcionar de forma autónoma (a partir dos
  dados da aplicação), pelo que já é legível mesmo sem o serviço de backend ligado.


## [2.13.0] — 2026-06-23

### Alterado — Mapa de rastreabilidade (grafo técnico como vista principal)
- Ao abrir a página, passa a aparecer logo o **Grafo técnico** como vista
  principal/predefinida. A **Vista simplificada** continua disponível como vista
  secundária para leitura rápida.
- **Layout do grafo reorganizado para deixar de parecer amontoado:** espaçamentos
  muito mais generosos entre linhas (lanes), e cada secção passou a ter as suas
  **unidades e suportes empilhados na própria coluna** (deixaram de se sobrepor à
  coluna seguinte, um dos principais focos de confusão).
- **Separação clara por tipo:** os **racks** ficam numa coluna dedicada e os
  resultados de **qualidade, recondicionamento e sucata** passam para colunas próprias
  à direita, afastados do fluxo de produção, para se seguir melhor o percurso da
  unidade, a rota e os desvios.
- **Legenda e painel lateral mais compactos:** a legenda ficou mais pequena (descrição
  passa a estar disponível no tooltip) e o painel lateral ficou mais estreito, deixando
  mais espaço para o grafo.


## [2.12.0] — 2026-06-22

### Alterado — linguagem mais natural e limpa (PT-PT)
- Removidos todos os **travessões (—)** do texto visível, substituídos por vírgula,
  dois pontos, ponto final ou hífen simples conforme o contexto. Abrange menu, cartões,
  tooltips, mensagens de estado, formulários e dados demo.
- Encurtadas frases longas/formais e retirado jargão sem utilidade direta ("dados
  reais", redundância "WIP em curso", etc.), deixando textos curtos e operacionais.

### Alterado — identidade no menu lateral
- Bloco da marca **centrado e mais trabalhado**: logótipo um pouco maior e nome a duas
  cores (**DriveTrace** + **Core** em destaque), com melhor hierarquia e espaçamento,
  sem aumentar a altura do bloco.

### Alterado — página de login mais limpa
- Retirados os textos "Rastreabilidade e monitorização de produção · DRIVOLUTION WP3"
  e "Autenticação demo/local — não é segurança de produção".
- Logótipo e nome **DriveTrace Core centrados** no cartão, que mantém apenas o
  essencial: marca, nome, utilizador/palavra-passe, botão de entrada e acesso rápido
  demo.

### Alterado — dados demo com tempos realistas
- As datas/horas dos dados demo passaram a ter **variedade temporal**: encomendas
  recentes (minutos), outras de há algumas horas, do dia anterior e concluídas há mais
  tempo (até ~3 dias). Os eventos das unidades em produção passaram a refletir
  movimentos recentes, tornando a análise mais credível.


## [2.11.1] — 2026-06-22

### Corrigido — cartões de encomenda deixam de deformar o texto (definitivo)
- **Causa de raiz:** no cabeçalho do cartão, a etiqueta de estado (que não encolhe)
  competia com o nome na mesma linha; em colunas estreitas o nome era esmagado e as
  palavras partiam-se **letra a letra**.
- **Interior do cartão reorganizado** com separação clara: etiqueta de estado numa
  **linha própria** no topo, depois **nome da encomenda** (a ocupar a largura toda),
  **código da ordem** (linha própria, mais pequeno, truncado com reticências),
  **quantidade × produto**, **cliente · linha** e, por fim, a **lista de unidades**
  (separada por um filete).
- **Nome da encomenda** passou a usar `overflow-wrap: normal` + `word-break: normal`
  (quebra só nos espaços, nunca a meio da palavra).
- **Colunas do Kanban** com **largura mínima real de 280 px** e **scroll horizontal
  controlado** quando o ecrã não tem espaço — cada coluna mantém-se legível em vez de
  encolher até deformar.


## [2.11.0] — 2026-06-22

### Corrigido — texto a ultrapassar limites (cartões, colunas, listas)
- **Quadro Kanban de encomendas:** deixou de espremer 5 colunas no ecrã. Passou a
  ter **scroll horizontal controlado** com **largura mínima por coluna** (≈250 px),
  evitando que as palavras se partam letra a letra em ecrãs de portátil.
- **Fluxo da fábrica (Painel de operações):** as secções da linha passaram a usar
  **scroll horizontal com cartões de largura fixa** (em vez de grelha comprimida).
- Quebra de texto **mais suave e profissional**: troca de `overflow-wrap: anywhere`
  por `break-word` nos nomes (quebra em espaços, mantém as palavras inteiras), com
  **truncagem por reticências + tooltip** nos códigos de rastreio, códigos de unidade
  e localizações. As correções foram aplicadas de forma **global e reutilizável** aos
  componentes de cartão, lista e etiqueta (`StatusBadge`, `UnitChip`, cartões de
  encomenda e de decisão, lista de eventos), não apenas a casos isolados.
- Corrigido um conjunto de casos em que **títulos ficavam invisíveis em modo claro**
  (texto branco sobre fundo branco) nos componentes reutilizáveis de **estado vazio**,
  **cronologia** e **lista de eventos**.

### Alterado — Quadro de qualidade focado na decisão
- A secção **"Atividade de qualidade"** deixou de ser um bloco grande sempre visível.
  Passou a ser um **histórico compacto e colapsável** (fechado por omissão, com um
  resumo de uma linha do último evento; ao abrir, mostra os últimos eventos). A
  prioridade visual da página é agora: estado da qualidade, fila de unidades a decidir,
  ações e só depois o histórico.


## [2.10.0] — 2026-06-22

### Corrigido — área do cliente com dados suficientes
- O cliente demo (*Auto Lisboa*) passou a estar ligado a **quatro encomendas em
  marcos diferentes**, para se poder validar a listagem, o detalhe, o progresso e a
  leitura visual: **duas em produção** (uma em fabrico, outra com unidade em controlo
  de qualidade), **uma pronta** e **uma concluída**. (Na v2.9.0 a área do cliente
  tinha ficado sem dados por incompatibilidade do nome do cliente.)
- O acompanhamento do cliente passou a reconhecer o estado **Pronta** (passo próprio
  no seguimento, KPI e progresso ~90% até à entrega).

### Adicionado — concluir/expedir uma encomenda pronta
- Nova ação **"Concluir e expedir"** (admin/supervisor, na página *Encomendas e
  produção*) que passa uma encomenda de **Pronta → Concluída**: conclui as unidades,
  liberta os suportes, regista o evento operacional, reflete-se na área do cliente e
  leva o progresso a **100%**.
- A ação só está disponível para encomendas **prontas**; é bloqueada se houver
  unidades por validar, em recuperação, em recondicionamento ou em controlo de
  qualidade.

### Corrigido — overflow de texto em tabelas, listas e cartões
- Proteções aplicadas de forma **global aos componentes reutilizáveis**
  (etiquetas de estado e cartões de decisão) e às páginas *Fluxo de fábrica* e
  *Encomendas e produção*: nomes de encomenda, códigos de rastreio, produtos,
  estados e secções passam a **quebrar de forma controlada** ou a ser **truncados
  com reticências** (mantendo o texto completo acessível), sem sair dos limites nem
  sobrepor botões. As etiquetas de estado deixaram de empurrar o conteúdo adjacente.
- Corrigido um caso em que o **título do cartão de decisão ficava invisível em modo
  claro** (texto branco sobre fundo branco).


## [2.9.0] — 2026-06-22

### Alterado — dados demo normalizados (nomes legíveis + códigos previsíveis)
- **Cada encomenda passou a ter um nome legível** (ex.: *Encomenda Porta Direita
  Standard*, *Encomenda Capô Premium*, *Encomenda Painel Exterior Desportivo*). O
  **nome é o que aparece em primeiro plano** no frontend; o código técnico passou a
  **informação secundária / de rastreabilidade**.
- **Nova convenção de códigos, simples e previsível:** ordem de fabrico
  **`OF-<FAMÍLIA>-NNN`** (ex.: `OF-PORTA-D-001`) e unidade
  **`UP-<FAMÍLIA>-NNN-UU`** (ex.: `UP-PORTA-D-001-01`, `-02`, `-03`). Deixaram de
  existir os números de 4 dígitos e as misturas pouco claras.
- **Conjunto demo pequeno mas completo**, a cobrir todos os estados: duas em
  **planeamento** (uma aceite com unidades em fila no armazém; uma a aguardar
  aceitação, sem unidades), três **em produção**, uma **pronta**, uma **concluída** e
  uma com **recondicionamento + não conformidade recuperável + sucata** em simultâneo.
  Todos os dados visíveis em **português de Portugal**, sem termos em inglês.

### Adicionado — estado "Pronta para expedição"
- Novo estado de encomenda **`ready`** ("Pronta para expedição"), distinto de
  *Em produção* e de *Concluída*. O quadro de encomendas ganhou a coluna **"Prontas"**
  e um indicador correspondente.

### Alterado — relação encomenda → ordem de fabrico → unidades mais clara
- No **quadro de encomendas**, cada cartão mostra o **nome**, o **código** (secundário)
  e a **lista das unidades** associadas com a sua localização atual.
- Na **Análise por linha**, cada unidade mostra o **nome da encomenda**; o detalhe
  indica a **ordem de fabrico** e **"unidade X de N"** dessa ordem.
- **Acompanhamento do cliente** e **quadro de qualidade** passaram a privilegiar o
  nome da encomenda, com o código como referência de rastreio.


## [2.8.0] — 2026-06-22

### Alterado — suporte obrigatório (regra de rastreabilidade)
- **Uma unidade já não avança no fluxo sem suporte ativo.** A atribuição de suporte
  é uma etapa **obrigatória e crítica**: a unidade pode existir como **planeada**
  (antes da fase "Atribuição de suporte"), mas ao entrar nessa fase fica
  **automaticamente associada** a um suporte disponível.
- A partir daí, **qualquer avanço, transferência entre secções, desvio entre linhas
  ou decisão de qualidade valida o suporte ativo**; se faltar, é feita uma
  **atribuição automática controlada** antes de continuar (usa um suporte livre ou
  cria um novo). O suporte **acompanha a unidade** até uma libertação/troca explícita
  ou à conclusão.

### Adicionado — produção unitária e regra de capacidade
- **Encomendas com várias unidades produzem unidades independentes.** Uma encomenda
  de 3 unidades cria 3 ProductUnits com códigos próprios (**UP-CAPO-3305-001/-002/-003**),
  todas ligadas à mesma ordem **OF-CAPO-3305**, cada uma com a **sua rota, suporte,
  posição, histórico, qualidade e eventos**. Deixou de haver "bloco produtivo".
- Na **Análise por linha** é visível que a mesma encomenda pode ter unidades em
  **fases diferentes** da linha.
- **Regra de capacidade por secção:** por defeito **1 unidade**; **2** apenas onde
  configurado (soldadura, pintura A, controlo de qualidade); armazéns/buffers
  ilimitados. Secção cheia → a unidade fica **em espera** em vez de se sobreporem
  unidades na mesma secção.

### Adicionado — responsáveis por secção
- Cada secção pode ter um **responsável** (operador, técnico de qualidade, logística,
  supervisor, robô, célula ou equipamento). A **Análise por linha** mostra, por secção,
  **"Responsável: …"** com o tipo, além da **ocupação/capacidade**.

### Alterado — Linha 4 e Mapa de rastreabilidade
- Removido o texto explicativo longo da Linha 4. A linha passou a chamar-se
  **"Qualidade e recuperação"** e a secção de retrabalho passou a
  **"Recondicionamento"**; a informação operacional é dada pelos próprios cartões.
- **Novo "Mapa do chão de fábrica" por omissão:** vista simplificada com faixas por
  linha, secções com **ocupação/capacidade**, unidades **coloridas por estado** com
  **indicador de suporte** (ativo/planeada/sem suporte) e marca de **fora da rota**,
  além de **filtros** (linha e tipo de problema) e legenda. O **grafo técnico**
  detalhado (vue-flow) fica agora atrás do **"Modo técnico"**. O painel lateral
  mostra os detalhes apenas da unidade selecionada.


## [2.7.0] — 2026-06-22

### Alterado — Painel de operações mais executivo
- **Removida a "Atividade recente" como bloco permanente.** Passou a ficar numa
  zona **colapsável** ("Histórico recente"), escondida por omissão, para o painel ser
  uma vista rápida e operacional sem listas extensas.

### Adicionado — atribuição de suporte como marco de rastreabilidade
- **A "atribuição de suporte" é agora um marco visível** do fluxo WIP. Antes do
  suporte, a unidade é **planeada/preparada**; depois, qualquer movimento está
  ligado a um **suporte ativo**.
- A aplicação mostra claramente: **sem suporte (planeada)**, **suporte atribuído**
  (qual é o suporte atual) e **problema de rastreabilidade** (passou a atribuição
  sem suporte). Há um indicador de suporte na **Análise por linha**, no **detalhe da
  unidade** (com atribuir/libertar) e no **Mapa de rastreabilidade**, e a etapa
  aparece marcada como **marco** no stepper da rota.
- Novo registo de **suportes** (palete/berço/skid) com atribuição/libertação; o
  suporte é libertado automaticamente quando a unidade conclui.

### Adicionado — página "Funcionários e recursos" (Administrador e Supervisor)
- Gere **pessoas e recursos produtivos** (operador humano, técnico de qualidade,
  logística, supervisor, braço mecânico, célula automática, equipamento).
- Adicionar, editar, **ativar/desativar**, classificar por tipo e atribuir a linhas.
- **Funcionário ≠ utilizador da aplicação**: só pessoas podem ter conta de acesso;
  os restantes existem apenas como recursos atribuíveis.

### Alterado — perfis de utilizador
- Revisão e **documentação** do modelo de perfis (`docs/PERFIS.md`), com a função
  real, páginas e ações de cada perfil e a justificação de por que não são
  redundantes (Administrador, Supervisor/Gerente, Operador, Qualidade, Logística,
  Cliente).

### Pendente (próximo)
- **Redesenho do Mapa de rastreabilidade / grafo** (vista limpa por faixas, estilos
  de ligação distintos, filtros, menos sobreposição, detalhes no painel lateral) —
  fica para um turno dedicado por ser a peça mais extensa.

## [2.6.0] — 2026-06-22

### Adicionado — rota produtiva nominal
- **Rota nominal bem definida** entre linhas e secções: matéria-prima → atribuição
  de suporte → fabrico e soldadura → pintura → controlo de qualidade (Linha 4) →
  rack/armazém final → expedição.
- **Stepper visual da rota** no detalhe da unidade (Análise por linha), a mostrar de
  forma simples: etapas **concluídas**, **etapa atual**, **próxima etapa esperada** e
  se a unidade está **fora da rota prevista** (com o motivo).
- As **transferências entre linhas** continuam possíveis mas passam a ser tratadas
  como **desvios/controlos operacionais explícitos**: exigem **motivo** e ficam
  registadas no histórico da unidade com **origem → destino + motivo** (tom de
  desvio). O diálogo de transferência só confirma um desvio interlinha com motivo
  preenchido.

### Alterado — modelo de qualidade (Linha 4)
- **A Linha 4 passa a ser claramente a zona de validação final / decisão de
  qualidade / recondicionamento** (deixa de parecer uma linha genérica). Um aviso
  identifica-a como tal na Análise por linha.
- **Removido o conceito de "retrabalho" como fluxo principal.** O estado da unidade
  passa a combinar fluxo + decisão de qualidade com vocabulário claro:
  **Aprovado**, **Em análise de qualidade**, **Recondicionável**,
  **Recondicionamento** e **Sucata**.
- **Decisões de qualidade** disponíveis no Quadro de qualidade **e** no detalhe da
  unidade quando esta está na Linha 4: Aprovar · Em análise · Recondicionar
  (ou "Recond. concluído") · Sucata. Ao chegar ao controlo de qualidade, a unidade
  fica automaticamente "Em análise de qualidade".
- O **detalhe da unidade** e a **Análise por linha** mostram agora **porque é que um
  produto saiu do fluxo normal** e **qual a decisão de qualidade tomada** (histórico
  + indicador de desvio).
- **Quadro de qualidade reescrito** com filtros (Todas / Em análise / Recondicionável
  / Recondicionamento), indicadores de conformidade e a ligação à encomenda.
- **Cockpit** e indicadores migrados para o novo vocabulário (Em análise de
  qualidade / Recondicionamento em vez de Retrabalho / Bloqueadas).

### Notas técnicas
- `ProductUnit` ganha o campo `quality` (none / pending / approved / recoverable /
  reconditioning / scrap), alinhado com `QualityDisposition` do backend. `UnitState`
  fica reduzido ao ciclo de fluxo (queued / active / transfer / completed / scrap).
- A taxa de aprovação passa a ser *aprovadas / avaliadas* com base na disposição de
  qualidade.

## [2.5.8] — 2026-06-21

### Corrigido (grave)
- **Já não é possível "concluir" uma encomenda com unidades ainda nas linhas.**
  Removido o botão "Concluir produção" que forçava o fecho (deixava produtos
  perdidos). A conclusão passa a ser **automática** e só acontece quando **todas**
  as unidades da encomenda estão concluídas (ou sucata) — o comportamento já
  existente e correto.
- **Unidades de demonstração agora pertencem a encomendas.** Antes tinham
  `orderId` nulo, pelo que nenhuma encomenda em produção tinha unidades para
  concluir (incluindo a `OF-TEJAD-3304`). Cada unidade semeada passa a estar
  ligada à respetiva encomenda em produção (agrupadas por produto), mantendo os
  estados originais. Para terminar a `OF-TEJAD-3304`, basta avançar as suas 3
  unidades até ao fim da linha — a encomenda fecha-se sozinha.

### Adicionado
- **"Análise por linha" mostra a encomenda de cada unidade.** Cada unidade
  apresenta a referência da encomenda a que pertence e, quando é a **última**
  que falta concluir nessa encomenda, uma etiqueta "última unidade" (caso
  contrário, "faltam N") — para se poder dar prioridade a fechar encomendas.

## [2.5.7] — 2026-06-20

### Adicionado
- **Concluir produção a partir de "Encomendas e produção".** As encomendas em
  produção passam a ter o botão **"Concluir produção"**, que conclui as unidades
  restantes (exceto sucata) e marca a encomenda como concluída. Antes só era
  possível concluir indiretamente, avançando cada unidade até ao fim da linha —
  o que não funcionava para encomendas semeadas sem unidades associadas (ex.:
  `OF-TEJAD`). Adicionada a ação `completeOrder` e o campo `completedAt`, agora
  também visível em "As minhas encomendas" › detalhes.

## [2.5.6] — 2026-06-20

### Corrigido
- **Modo escuro — tira "Estado operacional / Principal atenção / Ação
  recomendada / Evidência" no Mapa de rastreabilidade.** O painel
  (`.trace-decision-panel`) e os seus gradientes de estado desvaneciam para
  branco à direita, o que estava correto no tema claro mas surgia como uma
  faixa clara/ilegível no tema escuro. Adicionadas regras `.dark` para o painel
  e para os gradientes (ok/danger) desvanecerem para o fundo escuro. **O tema
  claro não foi alterado.**

## [2.5.5] — 2026-06-20

### Corrigido (crítico)
- **Modo escuro deixou de mostrar um ecrã branco.** Uma regra de estilo do
  logótipo do login (`filter: brightness(0) invert(1)`) estava, devido à forma
  como o CSS scoped compilava `:global(.dark) ...`, a ser aplicada ao elemento
  raiz `<html>` — invertendo a página inteira para branco sempre que o tema
  escuro estava ativo. Regra removida; o logótipo da barra lateral continua a
  ser recolorido (corretamente isolado à própria imagem).
- **Cartões brancos no modo escuro.** Vários elementos (cartões KPI, barra
  superior, diálogos, cartões de encomenda, campos, etc.) tinham fundo fixo
  `#fff` cujo "override" de tema escuro nunca era aplicado, ficando brancos no
  escuro. Passaram todos a usar o token adaptativo `--dt-surface`. **O tema claro
  fica idêntico** (nesse tema o token é exatamente branco).

### Alterado
- **"Quadro de qualidade" remodelado.** Deixou de parecer perdido: agora tem
  filtros (Todas / Bloqueadas / Retrabalho / Recondicionar), um painel de
  **conformidade** com a taxa de aprovação e a repartição por categoria, a fila
  de decisões e um registo de **atividade de qualidade recente**.

### Adicionado
- **"Painel de operações" — atividade recente preenchida.** Antes estava vazia
  porque o estado inicial não gerava eventos; passou a ter um histórico inicial
  coerente com o estado de arranque (conclusões, transferências, bloqueios,
  retrabalho, aceitação de encomendas).

## [2.5.4] — 2026-06-20

### Alterado
- **Modo escuro: barra lateral volta a ser azul-marinho da marca** (tema claro
  intocado). A causa do aspeto "chapado" era a barra lateral escura fundir-se
  com o conteúdo escuro; agora a barra mantém o azul-marinho `#0b2948` nos dois
  temas, dando estrutura ao ecrá, com o conteúdo em fundo profundo, cartões
  elevados e contornos visíveis.
- **Logótipo Drivolution maior e mais visível** na barra lateral e no ecrã de
  entrada: removida a "caixa" pequena; o logótipo é apresentado a toda a largura
  e recolorido a branco sobre os fundos escuros (azul sobre o painel claro do
  login). No login mantém o azul no tema claro e fica branco no escuro.
- **Texto da marca** na barra lateral simplificado para apenas **"DriveTrace
  Core"** (removido "DRIVOLUTION · WP3").

### Adicionado
- **"As minhas encomendas": ver detalhes da encomenda.** Cada encomenda no
  acompanhamento tem agora um botão "Ver detalhes" que revela referência,
  produto, quantidade, estado, datas (submissão/aceitação/início), data desejada,
  progresso (unidades concluídas) e observações — sem expor dados internos.

### Removido
- Texto "Conta local de demonstração — a alteração é válida durante a sessão."
  na página "O meu perfil".
- Texto "A organização da produção é definida pela fábrica." na página
  "As minhas encomendas".

## [2.5.3] — 2026-06-20

### Alterado
- **Modo escuro afinado de novo** (tema claro intocado): paleta de inspiração
  GitHub-dark com **contornos visíveis** e elevação clara — os cartões deixam de
  se fundir num bloco escuro. Fundo `#0d1117`, cartões `#181e27`, áreas
  recolhidas `#11161e`, contornos `rgba(205,217,229,.18–.22)` e acentos
  calibrados para escuro. Contornos codificados nos componentes passaram a usar
  o token, para a delimitação ser coerente em toda a aplicação.
- **Cabeçalhos de página deixaram de ser repetidos.** O título e a descrição de
  cada página aparecem agora **uma só vez** (na barra superior). Removido o
  cabeçalho duplicado dentro de cada página; no Mapa de rastreabilidade o
  alternador Grafo/Mapa passou para uma pequena barra de ferramentas.

### Removido
- **Etiquetas técnicas na "Gestão de dados"** (ex.: `POST /api/products`).
  Não faziam sentido para o utilizador final.

## [2.5.2] — 2026-06-19

### Alterado
- **Paleta do modo escuro reformulada** (sem qualquer alteração ao tema claro):
  passou de azul-ardósia saturado para um carvão frio e neutro, com degraus de
  elevação nítidos (fundo `#0c0f15`, superfícies `#1a1f27` / `#141821`, barra
  lateral `#10141c`) e contornos de acento mais suaves. Texto e números mantêm
  alto contraste. Avatar da barra superior harmonizado para o tema escuro.

### Adicionado
- **Área de perfil do utilizador** (`/perfil`, acessível a todos os perfis, e a
  partir do avatar na barra superior). Cada utilizador pode editar **apenas** o
  que faz sentido: nome a apresentar, email e telefone (guardados localmente),
  escolher o **tema** (claro/escuro) e **alterar a palavra-passe** (validando a
  atual). Nome de utilizador e perfil são apresentados como só de leitura
  (o perfil é definido pela administração).

## [2.5.1] — 2026-06-19

### Corrigido
- **Contraste no tema claro.** Os títulos de página e os números grandes
  (cartões KPI, mostrador de aprovação e centro do gráfico circular) usavam
  `--dt-app-bg` como cor de texto — quase branco sobre fundo branco, ilegível.
  Passaram a usar `--dt-text-strong` (escuro no tema claro, claro no escuro),
  garantindo leitura nos dois temas. Ajustado também o texto secundário de
  data/hora para maior contraste.

### Removido
- **Estimativas de tempo inventadas.** O portal do cliente já não mostra uma
  estimativa de conclusão fictícia. Mantém-se apenas informação verídica: estado
  por passos, progresso real (percentagem de unidades concluídas), data desejada
  (indicada pelo cliente) e data da última atualização. Removida também a
  fração de progresso fabricada para encomendas ainda sem unidades.

## [2.5.0] — 2026-06-19

### Corrigido
- **Taxa de aprovação** (cartão "Distribuição por estado") deixou de mostrar
  sempre 100%. Passa a contar `aprovadas / avaliadas`, onde *avaliadas* =
  concluídas + bloqueadas + retrabalho + recondicionamento + sucata. O cartão
  mostra agora "X de Y avaliadas (Z%)". (Coberto por teste.)
- **Modo noturno** harmonizado numa paleta coesa de ardósia-azulada
  (fundo, barras laterais, superfícies, contornos e **texto**), substituindo o
  contraste anterior. Introduzidos os tokens `--dt-surface`, `--dt-surface-2`,
  `--dt-border` e `--dt-sidebar-bg` (claro + escuro); todas as superfícies das
  vistas migradas para tokens adaptativos.

### Alterado
- **Grafo de rastreabilidade restaurado ao original do projeto.** O separador
  "Grafo" passa a usar o componente `TraceGraphView` (vue-flow) com as duas
  vistas originais — fábrica (linhas/secções) e **rota da unidade** — em vez do
  grafo SVG provisório (removido). O "Mapa de fluxo" mantém-se inalterado.
  O grafo lê dados em direto do backend.
- **Dados alinhados com a base de dados real.** O modelo de operações passa a
  espelhar o esquema (`ProductionLine` / `ProductionLineSection` com os códigos
  reais LINHA-01..04 e SEC-MP, SEC-SOLD, SEC-PINT-A, SEC-CQ, SEC-RETRAB,
  SEC-EXPED, ...), incluindo tipo de secção, zona e pontos de transferência.
  Removido o gráfico fictício **"Produção por hora"**; substituído por **WIP por
  linha** (contagens reais). Eliminada a "capacidade" inventada — as métricas
  derivam de contagens reais de `ProductUnit` por secção (concentração de WIP).

### Adicionado
- **Gestão de dados mestre** (admin + logística) em `/gestao`: criação de
  **produtos**, **matérias-primas**, **linhas** e **secções**, com indicação do
  *endpoint* REST correspondente (`POST /api/products`, `/api/raw-materials`,
  `/api/production-lines`, `/api/production-line-sections`). Os produtos criados
  ficam disponíveis no portal do cliente.

### Qualidade
- 58 testes unitários (inclui verificação da taxa de aprovação). Build de
  produção com *code-splitting*; ESLint com 0 erros.

## [2.4.0] — 2026-06-18

### Alterado
- **Removido o botão para o interface clássico.** Já não aparece na navegação;
  a rota `/classico` mantém-se apenas para administração (restrita por perfil),
  preservando funcionalidades legadas sem a expor aos restantes utilizadores.
- **Funções por perfil afinadas e segregação de acesso.** O **cliente** deixa de
  ver qualquer informação interna da fábrica — só acede ao seu portal. As vistas
  internas (cockpit, análise por linha, rastreabilidade) passam a ter
  *gating* por perfil no router e na barra lateral.
- **Portal do cliente repensado** (`CustomerView.vue`): formulário sem detalhes
  internos (produto, quantidade, data desejada, observações — o encaminhamento
  de linha é decidido pela fábrica); **indicador de estado por passos**
  (Submetida → Aceite → Em produção → Concluída), **estimativa de conclusão** e
  ação de **cancelar** pedidos ainda não aceites. Mostra apenas as encomendas do
  próprio cliente. Novo estado `cancelled` e ação `cancelOrder` no store.

### Adicionado
- **Vista em grafo no Mapa de rastreabilidade** (`FlowGraph.vue`): alternador
  **Grafo / Mapa de fluxo**. O grafo mostra as secções como nós (cor por
  ocupação), setas de fluxo dentro da linha e ligações curvas de encaminhamento
  entre linhas (montagem → pintura → qualidade), com animação subtil. O mapa de
  fluxo anterior mantém-se disponível.

### Corrigido
- **Modo claro/escuro.** O texto de títulos deixava de ter contraste num dos
  temas. Introduzidos tokens adaptativos `--dt-text-strong` e `--dt-app-bg` e
  migrados todos os textos das novas vistas para esses tokens — ambos os temas
  ficam legíveis e consistentes.

### Marca
- **Logótipo DRIVOLUTION** aplicado na barra lateral e no ecrã de entrada
  (sobre fundo claro para visibilidade em qualquer tema).

### Qualidade
- 57 testes unitários (inclui cancelamento de encomenda pelo cliente). Build de
  produção com *code-splitting*; ESLint com 0 erros.

## [2.3.0] — 2026-06-18

### Adicionado — Novo interface visual (orientado a operação de linha)
- **Novo interface como cara principal da aplicação**, focado em clareza visual,
  cor e gráficos em vez de tabelas densas. Conduzido por router
  (`dashboard/src/router/index.ts`) com *shell* próprio (barra lateral + topo).
  O **interface clássico completo mantém-se acessível em `/classico`** — nenhuma
  funcionalidade existente foi removida (sessão partilhada via `localStorage`).
- **Modelo de operações reativo** (`dashboard/src/stores/operations.ts`):
  domínio em memória (4 linhas, secções com capacidade, unidades e encomendas)
  que torna a app totalmente interativa e demonstrável sem backend, com pontos
  de ligação à API documentados.
- **Ciclo de vida da encomenda** (pedido do cliente → **aceitar** → **iniciar
  produção** → conclusão automática): vista `OrdersView.vue` (quadro com
  aceitar/recusar e iniciar produção) e portal do cliente `CustomerView.vue`
  (submeter pedido + acompanhamento com barra de progresso).
- **Transferências de produtos**: dentro da mesma linha e **entre linhas**
  (interlinha), via `TransferDialog.vue` e ações do store (`transferUnit`,
  `transferUnitToLine`). Disponíveis no cockpit e na análise por linha.
- **Gráficos SVG próprios** (sem dependências novas): `AreaChart` (produção por
  hora), `BarChart` (carga por secção), `DonutChart` (distribuição por estado),
  `GaugeChart` (taxa de aprovação). Helpers puros em `utils/charts.ts`.
- **Mapa de fluxo da fábrica** (`FlowMap.vue`): *swimlanes* por linha, secções
  com medidor de capacidade, chips de unidade com **cor por estado**, realce de
  **gargalos** e seleção de unidade com detalhe/percurso.
- **Vistas novas**: `CockpitView` (visão global), `LineAnalysisView` (análise
  por linha), `QualityBoardView` (decisões de qualidade), `TraceMapView`
  (rastreabilidade visual), `LoginView` (acesso demo).

### Qualidade
- **+12 testes unitários** (56 no total, antes 44): `charts.spec.ts` (helpers de
  gráficos) e `operations.spec.ts` (ciclo de encomenda e transferências).
- **Build de produção** com *code-splitting*: arranque reduzido para ~16,5 kB
  (`index`), com o interface clássico isolado num *chunk* próprio carregado
  apenas em `/classico`.
- ESLint: 0 erros.

## [2.1.0] — 2026-06-18

### Corrigido
- **Build Docker da API** falhava com `CS0246: 'Xunit' could not be found` e
  `Duplicate AssemblyAttribute`. Causa: o `api/Dockerfile` faz `COPY . ./` e o
  *globbing* implícito do `.csproj` da API apanhava os ficheiros de
  `api/Tests/`. Correção: `<Compile Remove="Tests/**" />` no `.csproj` da API e
  `Tests` adicionado a `api/.dockerignore`. Os testes continuam a correr como
  projeto autónomo (`dotnet test ./api/Tests/...`).
- **`npm run lint`** falhava (parser sem suporte TypeScript). Adicionado
  `typescript-eslint` e reconfigurado `eslint.config.js` (flat config) com o
  *parser* correto para `.ts` e `<script lang="ts">`. Resultado: 0 erros.

## [2.0.0] — 2026-06-17

### Adicionado
- **Design tokens** (`dashboard/src/styles/tokens.css`): paleta semântica de
  estados, tema escuro com contraste real, foco acessível e suporte a
  `prefers-reduced-motion`.
- **Camada de utilitários** (`dashboard/src/utils/`): `status.ts` (tons/classes
  de estado), `access.ts` (mapa de perfis/permissões/vista inicial), `format.ts`
  (formatadores PT-PT).
- **Biblioteca de componentes acessíveis** (`dashboard/src/components/common/`):
  StatusBadge, KpiCard, StatStrip, EmptyState, LoadingState, PageHeader,
  ConfirmationDialog, SmartFormField, DecisionCard, TimelineStep, ActivityFeed.
- **Estado global com Pinia** (`dashboard/src/stores/`): `auth`, `preferences`,
  `ui` — partilhando as chaves de `localStorage` existentes (coexistência sem
  regressões).
- **Vue Router** (`dashboard/src/router/`): tabela de rotas como fonte única de
  navegação, guards como função pura testada, *lazy-loading* das vistas pesadas.
- **Composables** (`dashboard/src/composables/`): `useBreakpoint`, `useViewSync`.
- **Testes**: 44 testes unitários (Vitest) sobre utils, stores, guards e
  componentes; E2E (Playwright) por perfil; testes unitários de backend (xUnit).
- **Configuração de qualidade**: `vitest.config.ts`, `playwright.config.ts`,
  `lighthouserc.json`, `eslint.config.js`, `.prettierrc.json`, `.editorconfig`.
- **CI/CD**: `.github/workflows/ci.yml` (backend, frontend, E2E, Lighthouse,
  validação de Docker Compose, artefacto ZIP).
- **Hardening do backend**: ProblemDetails, *exception handler*,
  `SecurityHeadersMiddleware`, endpoint `/health`, CORS configurável
  (`Cors:AllowedOrigins`).
- **Documentação**: `docs/SETUP_E_EXECUCAO.md`, `docs/NOTAS_TECNICAS.md`,
  `docs/mockups/` (2 mockups SVG + nota de design), README atualizado.

### Alterado
- **Performance**: *code-splitting* por rota e divisão de *vendors*
  (`manualChunks`) reduziram o *chunk* inicial JS de **~601 kB / 182 kB gzip**
  para **~262 kB / 68 kB gzip** — o motor `@vue-flow` e as vistas pesadas
  deixaram de pesar no arranque.
- `main.ts` instala Pinia + Vue Router e hidrata o tema antes do primeiro
  render (evita *flash* de tema).

### Mantido (sem regressões)
- FIWARE, Grafana, Docker Compose, simulador, perfis por papel, autenticação
  demo/local e `App.vue` como autoridade de renderização.

### Por melhorar (próximos incrementos)
- Migrar a renderização de cada vista para `<router-view>` (infra já pronta).
- Migração do *schema* físico para nomes PT-PT via Fluent API.
- Integração real controlada de backend (WebApplicationFactory + Testcontainers).
- Confirmar metas Lighthouse no ambiente representativo.
