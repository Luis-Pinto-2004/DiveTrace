import type { Component } from 'vue'
import type { RoleKey, ViewKey } from '@/utils/access'

/** Agrupamento da navegação na sidebar. */
export type NavGroup = 'operacao' | 'qualidade' | 'logistica' | 'cliente' | 'analitica' | 'admin'

export interface ViewRoute {
  /** Chave estável da vista (corresponde a `activeView` em App.vue). */
  key: ViewKey
  /** Caminho de rota associado. */
  path: string
  /** Etiqueta PT-PT apresentada na navegação. */
  label: string
  /** Subtítulo orientado à decisão. */
  subtitle?: string
  /** Grupo na sidebar. */
  group: NavGroup
  /** Permissão necessária (vazio = acessível a qualquer autenticado). */
  permission?: string
  /** Perfis com acesso direto (vazio = qualquer perfil com a permissão). */
  roles?: RoleKey[]
  /** Componente carregado de forma assíncrona (code-splitting). */
  component?: () => Promise<Component>
  /** Vista pesada que deve ser sempre lazy. */
  heavy?: boolean
}

/**
 * Tabela central de vistas. Fonte única consumida pela navegação,
 * pelos guards e pelos testes. Os componentes pesados são declarados
 * como import dinâmico para permitir divisão de código por rota.
 */
export const VIEW_ROUTES: ViewRoute[] = [
  {
    key: 'overview',
    path: '/app/overview',
    label: 'Centro de comando',
    subtitle: 'Visão global da fábrica e prioridades do turno',
    group: 'operacao',
  },
  {
    key: 'operator',
    path: '/app/operador',
    label: 'Bancada do operador',
    subtitle: 'Unidades a atuar e próxima ação',
    group: 'operacao',
    permission: 'ProductUnits.View',
  },
  {
    key: 'orders',
    path: '/app/ordens',
    label: 'Ordens de produção',
    subtitle: 'Acompanhamento de ordens e unidades',
    group: 'operacao',
    permission: 'Orders.View',
  },
  {
    key: 'units',
    path: '/app/unidades',
    label: 'Unidades WIP',
    subtitle: 'Estado e localização das unidades',
    group: 'operacao',
    permission: 'ProductUnits.View',
  },
  {
    key: 'reconditioning',
    path: '/app/qualidade',
    label: 'Qualidade e recondicionamento',
    subtitle: 'Decisões pendentes, NC, retrabalho e sucata',
    group: 'qualidade',
    permission: 'Quality.View',
    heavy: true,
    component: () => import('@/components/ReconditioningView.vue'),
  },
  {
    key: 'quality',
    path: '/app/registos-qualidade',
    label: 'Registos de qualidade',
    subtitle: 'Resultados e não conformidades',
    group: 'qualidade',
    permission: 'Quality.View',
  },
  {
    key: 'racks',
    path: '/app/racks',
    label: 'Racks e suportes',
    subtitle: 'Ocupação, buffers e expedição',
    group: 'logistica',
    permission: 'Racks.View',
  },
  {
    key: 'supports',
    path: '/app/suportes',
    label: 'Suportes',
    subtitle: 'Disponibilidade e atribuição',
    group: 'logistica',
    permission: 'Supports.Manage',
  },
  {
    key: 'materials',
    path: '/app/materiais',
    label: 'Materiais e lotes',
    subtitle: 'Rastreabilidade de matéria-prima',
    group: 'logistica',
    permission: 'Materials.View',
  },
  {
    key: 'traceGraph',
    path: '/app/rastreabilidade',
    label: 'Mapa de rastreabilidade',
    subtitle: 'Fluxo por linha, transferências e bloqueios',
    group: 'analitica',
    permission: 'ProductUnits.Trace',
    heavy: true,
    component: () => import('@/components/TraceGraphView.vue'),
  },
  {
    key: 'analytics',
    path: '/app/analitica',
    label: 'Analítica operacional',
    subtitle: 'Indicadores e dashboards',
    group: 'analitica',
    permission: 'Grafana.View',
    heavy: true,
    component: () => import('@/components/GrafanaAnalyticsView.vue'),
  },
  {
    key: 'simulation',
    path: '/app/simulador',
    label: 'Simulador de produção',
    subtitle: 'Cenários determinísticos de fluxo',
    group: 'analitica',
    permission: 'Simulation.Read',
    heavy: true,
    component: () => import('@/components/ProductionSimulatorView.vue'),
  },
  {
    key: 'customerOrders',
    path: '/app/cliente/encomendas',
    label: 'As minhas encomendas',
    subtitle: 'Progresso e seguimento',
    group: 'cliente',
    roles: ['client'],
    permission: 'Orders.View',
  },
  {
    key: 'customerNewOrder',
    path: '/app/cliente/novo-pedido',
    label: 'Novo pedido',
    subtitle: 'Submeter uma nova encomenda',
    group: 'cliente',
    roles: ['client'],
    permission: 'Orders.Manage',
  },
  {
    key: 'parameters',
    path: '/app/administracao',
    label: 'Administração',
    subtitle: 'Perfis, dados mestre e estado de serviços',
    group: 'admin',
    permission: 'MasterData.Manage',
  },
]

export function findViewByKey(key: ViewKey): ViewRoute | undefined {
  return VIEW_ROUTES.find((route) => route.key === key)
}

export function findViewByPath(path: string): ViewRoute | undefined {
  return VIEW_ROUTES.find((route) => route.path === path)
}
