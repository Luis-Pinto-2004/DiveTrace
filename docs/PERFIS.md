# Perfis de utilizador — DriveTrace Core

Análise funcional dos perfis e justificação das páginas/ações de cada um. O objetivo
é que cada tipo de utilizador tenha uma área de trabalho clara, sem perfis
praticamente iguais.

> Nota importante: **perfil de utilizador ≠ funcionário/recurso**. A página
> *Funcionários e recursos* gere pessoas **e** recursos produtivos (braços
> mecânicos, células, equipamento); apenas alguns têm conta de acesso. A conta de
> acesso é que determina o perfil abaixo.

## Perfis

| Perfil | Função real | Páginas | Ações-chave |
|---|---|---|---|
| **Administrador** | Configuração e gestão global | Todas | Tudo: dados, funcionários, encomendas, qualidade, interface clássico |
| **Supervisor / Gerente** | Gestão operacional e logística: produção, linhas, racks/pós-linha, qualidade e dados | Painel, Encomendas, Análise por linha, Qualidade, Rastreabilidade, **Gestão de dados**, **Funcionários** | Aceitar/iniciar encomendas, decisões de qualidade, gerir dados e recursos, acompanhar rack/expedição |
| **Operador** | Executar ações simples na linha | Painel (estado), Análise por linha, Rastreabilidade | Avançar etapa, atribuir suporte, transferir (desvio) |
| **Qualidade** | Validar, aprovar, recondicionar ou marcar sucata | Quadro de qualidade, Análise por linha, Rastreabilidade | Decisões de qualidade na Linha 4 |
| **Cliente** | Acompanhar encomendas | As minhas encomendas | Submeter pedidos e ver estado |

> O perfil **Logística** deixou de existir como tipo de acesso. A sua função (racks,
> armazenamento e movimentações pós-linha, além da gestão de dados) passou para o
> **Supervisor**. Os conceitos de logística da fábrica (racks, suportes, expedição) e o
> tipo de recurso *Logística* nos funcionários mantêm-se.

## Porque é que os perfis não são redundantes

- **Operador vs Qualidade** — partilham o painel, a análise por linha e a
  rastreabilidade (contexto de chão de fábrica), mas só a Qualidade tem o **Quadro
  de qualidade** com as decisões (aprovar / análise / recondicionar / sucata). O
  Operador executa ações simples; a Qualidade decide.
- **Gestão de dados** — a **Gestão de dados** (produtos, linhas, secções) é agora
  acessível ao **Administrador** e ao **Supervisor**.
- **Supervisor vs Administrador** — o Supervisor faz a gestão operacional e logística
  (produção, racks/pós-linha, qualidade, dados) e gere recursos/funcionários, mas
  **não** acede à configuração global nem ao interface clássico, reservados ao
  Administrador.
- **Cliente** — área completamente separada (apenas as suas encomendas), sem acesso
  a dados internos da fábrica.

## Funcionários (apenas Administrador e Supervisor)

A página *Funcionários e recursos* permite **adicionar, editar, ativar/desativar e
classificar** recursos por tipo: operador humano, técnico de qualidade, logística,
supervisor, braço mecânico, célula automática ou equipamento. Só pessoas podem ter
conta de acesso; os restantes existem apenas como recursos atribuíveis a linhas,
secções ou ações.
