# Nota de Design — DriveTrace Core

Direção visual: **industrial moderna, limpa e funcional** — cruzamento entre
dashboard operacional, produto SaaS B2B e contexto de fabrico. Cada página
responde à pergunta: *"Que decisão ou ação esta página ajuda a tomar?"*.

## Paleta (estados de domínio)

Os tokens estão em `dashboard/src/styles/tokens.css`.

| Estado | Cor | Uso |
|---|---|---|
| OK / aprovado / concluído / disponível | Verde `#10b981` | Sucesso, conclusão |
| Fluxo normal / informação / transferência | Azul `#3b82f6` | Estado em curso |
| Pendente / atenção | Amarelo `#eab308` | Espera, aviso |
| Retrabalho / recondicionamento / decisão | Laranja `#f97316` | Ação requerida |
| Crítico / bloqueado / reprovado / sucata | Vermelho `#ef4444` | Bloqueio |
| Inativo / sem dados | Cinzento `#94a3b8` | Neutro |

Marca: azul Drivolution `#0877d8` (escuro `#0b2948`).

Os estados **nunca dependem apenas da cor**: cada `StatusBadge` combina cor,
ponto e etiqueta textual (acessibilidade WCAG 2.1 AA).

## Tipografia

- Família principal: **Inter** (UI), com *fallback* de sistema.
- Escala: rótulos `0.6875rem` maiúsculas, corpo `0.8125–0.875rem`, valores de
  KPI `1.875rem` peso 900. Pesos intencionais: 700 para ênfase, 900 para números.

## Espaçamento

- Escala base de 4px (`--dt-space-*`). Raio `0.5rem`/`0.75rem`. Elevação suave
  em cartões (`--dt-shadow-card`), elevação forte em diálogos (`--dt-shadow-pop`).

## Movimento

- Transições discretas de **120–220ms** (`--dt-motion-*`) com `ease`
  padronizado. `prefers-reduced-motion` respeitado (animações desligadas).

## Estados

- **Vazio** (`EmptyState`): convite à ação, nunca um beco sem saída.
- **Carregamento** (`LoadingState`): *skeleton* anunciado a leitores de ecrã.
- **Erro**: mensagens na voz da interface, claras sobre o que aconteceu.

## Componentes-chave

`StatusBadge`, `KpiCard`/`StatStrip`, `DecisionCard`, `TimelineStep`,
`ActivityFeed`, `PageHeader`, `EmptyState`, `LoadingState`,
`ConfirmationDialog`, `SmartFormField`. Todos tipados, acessíveis e baseados em
tokens.

## Mockups

- `supervisor-command-center.svg` — centro de comando do supervisor.
- `operator-workbench.svg` — bancada do operador.

Os mockups são de alta fidelidade e derivam diretamente dos tokens acima
(cores, tipografia, espaçamento), servindo de referência para as vistas reais.
