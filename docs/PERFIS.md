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
| **Supervisor / Gerente** | Acompanhar produção, linhas, qualidade e decisões operacionais | Painel, Encomendas, Análise por linha, Qualidade, Rastreabilidade, **Funcionários** | Aceitar/iniciar encomendas, decisões de qualidade, gerir recursos |
| **Operador** | Executar ações simples na linha | Painel (estado), Análise por linha, Rastreabilidade | Avançar etapa, atribuir suporte, transferir (desvio) |
| **Qualidade** | Validar, aprovar, recondicionar ou marcar sucata | Quadro de qualidade, Análise por linha, Rastreabilidade | Decisões de qualidade na Linha 4 |
| **Logística** | Suportes, racks e movimentações pós-linha | Gestão de dados, Análise por linha, Rastreabilidade | Configurar dados, acompanhar rack/expedição |
| **Cliente** | Acompanhar encomendas | As minhas encomendas | Submeter pedidos e ver estado |

## Porque é que os perfis não são redundantes

- **Operador vs Qualidade** — partilham o painel, a análise por linha e a
  rastreabilidade (contexto de chão de fábrica), mas só a Qualidade tem o **Quadro
  de qualidade** com as decisões (aprovar / análise / recondicionar / sucata). O
  Operador executa ações simples; a Qualidade decide.
- **Operador vs Logística** — a Logística acede à **Gestão de dados** (produtos,
  linhas, secções) e foca-se no pós-linha (rack/expedição); o Operador não.
- **Supervisor vs Administrador** — o Supervisor acompanha produção e gere
  recursos/funcionários, mas **não** acede à configuração global nem ao interface
  clássico, reservados ao Administrador.
- **Cliente** — área completamente separada (apenas as suas encomendas), sem acesso
  a dados internos da fábrica.

## Funcionários (apenas Administrador e Supervisor)

A página *Funcionários e recursos* permite **adicionar, editar, ativar/desativar e
classificar** recursos por tipo: operador humano, técnico de qualidade, logística,
supervisor, braço mecânico, célula automática ou equipamento. Só pessoas podem ter
conta de acesso; os restantes existem apenas como recursos atribuíveis a linhas,
secções ou ações.
