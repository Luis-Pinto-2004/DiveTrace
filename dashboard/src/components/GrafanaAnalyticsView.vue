<template>
  <div class="analytics-page" :data-locale="props.currentLocale">
    <section class="analytics-hero">
      <div class="section-heading">
        <div class="min-w-0">
          <p>{{ t('Operational analytics hub') }}</p>
          <h3>{{ t('Grafana Analytics') }}</h3>
          <p class="section-description">
            {{ t('Executive reading of WIP, quality, supports and industrial context.') }}
          </p>
        </div>
        <div class="flex w-full flex-wrap gap-2 sm:w-auto">
          <button type="button" class="btn-secondary w-full sm:w-auto" @click="refreshAll" :disabled="checkingConnectivity">
            {{ checkingConnectivity ? t('Checking Grafana...') : t('Refresh analysis') }}
          </button>
          <button type="button" class="btn-secondary w-full sm:w-auto" @click="focusTab('fiware')">{{ t('View FIWARE dashboard') }}</button>
          <a class="btn-primary w-full sm:w-auto" :href="grafanaBaseUrl" target="_blank" rel="noopener noreferrer">{{ t('Open in Grafana') }}</a>
        </div>
      </div>

      <div class="analytics-status-grid">
        <article v-for="card in headerCards" :key="card.key" class="analytics-status-card" :class="card.toneClass">
          <span>{{ card.label }}</span>
          <strong>{{ card.value }}</strong>
          <p>{{ card.detail }}</p>
        </article>
      </div>
    </section>

    <section class="analytics-decision-strip" :class="primaryOperationalDecision.toneClass">
      <div>
        <span>{{ t('Estado operacional') }}</span>
        <strong>{{ primaryOperationalDecision.state }}</strong>
      </div>
      <div>
        <span>{{ t('Principal atenção') }}</span>
        <strong>{{ primaryOperationalDecision.attention }}</strong>
      </div>
      <div>
        <span>{{ t('Ação recomendada') }}</span>
        <strong>{{ primaryOperationalDecision.action }}</strong>
      </div>
      <div>
        <span>{{ t('Evidência') }}</span>
        <strong>{{ primaryOperationalDecision.evidence }}</strong>
      </div>
      <button type="button" class="btn-primary" @click="focusTab(primaryOperationalDecision.tab)">{{ t('Abrir detalhe') }}</button>
    </section>

    <section class="analytics-section">
      <div class="section-heading">
        <div class="min-w-0">
          <p>{{ t('Operational data') }}</p>
          <h3>{{ t('Operational snapshot') }}</h3>
          <p class="section-description">{{ t('Operational snapshot description') }}</p>
        </div>
      </div>
      <div class="analytics-kpi-grid">
        <article v-for="metric in operationalMetrics" :key="metric.key" class="analytics-kpi-card">
          <span>{{ metric.label }}</span>
          <strong>{{ metric.value }}</strong>
          <p>{{ metric.detail }}</p>
        </article>
      </div>
    </section>

    <section class="analytics-section">
      <div class="section-heading">
        <div class="min-w-0">
          <p>{{ t('Operational priorities') }}</p>
          <h3>{{ t('Decision panel') }}</h3>
          <p class="section-description">{{ t('Decision panel description') }}</p>
        </div>
      </div>
      <div class="analytics-insight-grid">
        <article v-for="insight in visibleOperationalInsights" :key="insight.key" class="analytics-insight-card" :class="insight.toneClass">
          <span>{{ insight.domain }}</span>
          <strong>{{ insight.title }}</strong>
          <p>{{ insight.description }}</p>
          <p class="mt-3 rounded-lg border border-slate-200 bg-white/70 p-3 text-xs font-bold uppercase tracking-normal text-slate-600 dark:border-slate-700 dark:bg-slate-950/40 dark:text-slate-300">
            {{ t('Recommended action') }}: <span class="normal-case">{{ insight.action }}</span>
          </p>
          <button type="button" class="btn-secondary mt-3 w-full" @click="focusTab(insight.tab)">{{ t('View supporting dashboard') }}</button>
        </article>
      </div>
    </section>

    <section class="analytics-section">
      <div class="section-heading">
        <div class="min-w-0">
          <p>{{ t('Available dashboards') }}</p>
          <h3>{{ t('Grafana dashboard catalogue') }}</h3>
          <p class="section-description">{{ t('Dashboard catalogue description') }}</p>
        </div>
        <button type="button" class="btn-secondary" @click="showAllDashboards = !showAllDashboards">
          {{ showAllDashboards ? t('Mostrar menos') : t('Ver todos') }}
        </button>
      </div>
      <div class="dashboard-compact-list">
        <article
          v-for="dashboard in visibleDashboardCatalog"
          :key="dashboard.uid"
          class="dashboard-card"
          :class="activeTab === dashboard.tab ? 'dashboard-card-active' : ''"
        >
          <div class="flex items-start justify-between gap-3">
            <div class="min-w-0">
              <p class="dashboard-card-domain">{{ t(dashboard.domain) }}</p>
              <h4>{{ t(dashboard.title) }}</h4>
            </div>
            <span :class="dashboardStatusClass">{{ dashboardStatusLabel }}</span>
          </div>
          <p class="mt-3 text-sm text-slate-600 dark:text-slate-300">{{ t(dashboard.objective) }}</p>
          <dl class="dashboard-card-meta">
            <div>
              <dt>{{ t('Analysis type') }}</dt>
              <dd>{{ t(dashboard.analysisType) }}</dd>
            </div>
            <div>
              <dt>{{ t('Data source') }}</dt>
              <dd>{{ datasourceName }}</dd>
            </div>
          </dl>
          <p class="dashboard-card-note">{{ t(dashboard.decision) }}</p>
          <ul class="technical-list">
            <li v-for="question in dashboard.questions" :key="question">{{ t(question) }}</li>
          </ul>
          <div class="mt-4 grid gap-2 sm:grid-cols-2">
            <button type="button" class="btn-secondary" @click="focusEmbeddedDashboard(dashboard)">
              {{ t('Show in application') }}
            </button>
            <button type="button" class="btn-primary" @click="openDashboardInGrafana(dashboard)">
              {{ t('Open in Grafana') }}
            </button>
          </div>
        </article>
      </div>
    </section>

    <section class="analytics-section">
      <div class="section-heading">
        <div class="min-w-0">
          <p>{{ t('Embedded view') }}</p>
          <h3>{{ t(activeDashboard.title) }}</h3>
          <p class="section-description">{{ t(activeDashboard.objective) }}</p>
        </div>
      </div>
      <div class="mt-4 flex flex-wrap gap-2">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          type="button"
          class="mobile-tab"
          :class="activeTab === tab.key ? 'mobile-tab-active' : ''"
          @click="activeTab = tab.key"
        >
          {{ t(tab.label) }}
        </button>
        <button type="button" class="mobile-tab" :class="showEmbeddedPanels ? 'mobile-tab-active' : ''" @click="showEmbeddedPanels = !showEmbeddedPanels">
          {{ showEmbeddedPanels ? t('Ocultar detalhe') : t('Mostrar detalhe') }}
        </button>
      </div>
    </section>

    <section v-if="showEmbeddedPanels" class="grid min-w-0 gap-5 xl:grid-cols-3">
      <GrafanaPanel
        v-for="card in activePanelCards"
        :key="card.key"
        :title="t(card.title)"
        :description="t(card.description)"
        :dashboard-uid="card.dashboardUid"
        :slug="card.slug"
        :panel-id="card.panelId"
        :mode="card.mode"
        :height="card.height"
        :refresh="card.refresh ?? '30s'"
        :from="card.from ?? 'now-30d'"
        :to="card.to ?? 'now'"
        :theme="grafanaTheme"
        :grafana-base-url="grafanaBaseUrl"
        :show-open-button="true"
        @loaded="onEmbedLoaded"
        @failed="onEmbedFailed"
      />
    </section>

    <section class="analytics-section">
      <div class="section-heading">
        <div class="min-w-0">
          <p>{{ t('Full dashboard') }}</p>
          <h3>{{ t(activeDashboard.title) }}</h3>
        </div>
        <div class="flex w-full flex-wrap gap-2 sm:w-auto">
          <button type="button" class="btn-secondary w-full sm:w-auto" @click="showFullDashboard = !showFullDashboard">
            {{ showFullDashboard ? t('Hide full dashboard') : t('Show full dashboard') }}
          </button>
          <button type="button" class="btn-primary w-full sm:w-auto" @click="openDashboardInGrafana(activeDashboard)">
            {{ t('Open in Grafana') }}
          </button>
        </div>
      </div>
      <p class="section-description mt-3">{{ t('Embedded dashboard view may be blocked by browser or Grafana policy. If that happens, use the Open in Grafana button.') }}</p>
      <div v-if="showFullDashboard" class="mt-4">
        <GrafanaPanel
          :key="`full-${activeDashboard.uid}-${props.currentLocale}-${grafanaTheme}`"
          :title="t(activeDashboard.title)"
          :description="t(activeDashboard.objective)"
          :dashboard-uid="activeDashboard.uid"
          :slug="activeDashboard.slug"
          mode="dashboard"
          :height="'clamp(28rem, 70vh, 760px)'"
          refresh="30s"
          from="now-30d"
          to="now"
          :theme="grafanaTheme"
          :grafana-base-url="grafanaBaseUrl"
          :show-open-button="false"
          @loaded="onEmbedLoaded"
          @failed="onEmbedFailed"
        />
      </div>
    </section>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import GrafanaPanel from './GrafanaPanel.vue'
import { t, translateSectionName } from '../prefs'

type TabKey = 'executive' | 'wip' | 'quality' | 'fiware' | 'overview'

type DashboardDefinition = {
  tab: TabKey
  uid: string
  slug: string
  title: string
  domain: string
  objective: string
  decision: string
  analysisType: string
  questions: string[]
}

type PanelCard = {
  key: string
  title: string
  description: string
  dashboardUid: string
  slug: string
  panelId?: number
  mode?: 'panel' | 'dashboard'
  height?: number | string
  refresh?: string
  from?: string
  to?: string
}

type SummaryData = {
  generatedAt?: string
  counts?: {
    openOrders?: number
    activeUnits?: number
    activeSupports?: number
    qualityIssues?: number
    rackAssignments?: number
    reconditionedUnits?: number
    recoveryCandidates?: number
    inRecovery?: number
    recoveryRate?: number
  }
  wipBySection?: Array<{ section: string; sectionCode?: string; productUnits: number; activeSupports?: number }>
  recentEvents?: Array<{ supportCode: string; section: string; eventType: string; dateTime: string }>
}

type OperationalData = {
  summary: SummaryData
  flowSummary?: {
    totals?: {
      productionLines?: number
      transferPoints?: number
      transfers?: number
      transfersLast24h?: number
      reconditionedUnits?: number
      recoveryCandidates?: number
      recoveryRate?: number
    }
    lineSummaries?: Array<{ lineCode?: string; name?: string; wipUnits?: number; blockedUnits?: number }>
  }
  orders: Array<{ status?: string }>
  units: Array<{ status?: string; qualityStatus?: string; currentSectionId?: number; isReconditioned?: boolean; recoveryStatus?: string; qualityDisposition?: string }>
  supports: Array<{ status?: string; currentSectionId?: number }>
  racks: Array<{ status?: string }>
  rackSupportAssignments: Array<{ dateTimeOut?: string | null }>
  quality: Array<{ result?: string }>
  nonconformities: Array<{ status?: string; severity?: string }>
  reworkRecords: Array<{ status?: string }>
  supportHistory: Array<{ dateTime?: string }>
}

const props = defineProps<{
  theme: string
  currentLocale: string
  apiStatus: string
  apiBaseUrl: string
  operationalData: OperationalData
}>()

const emit = defineEmits<{
  refresh: []
}>()

const datasourceName = 'DriveTrace TimescaleDB'
const datasourceDetail = 'PostgreSQL / TimescaleDB'

const tabs: Array<{ key: TabKey; label: string }> = [
  { key: 'executive', label: 'Visão executiva' },
  { key: 'wip', label: 'Operações WIP' },
  { key: 'quality', label: 'Qualidade e rastreabilidade' },
  { key: 'fiware', label: 'FIWARE / infraestrutura' },
  { key: 'overview', label: 'Visão geral WIP' },
]

const dashboardCatalog: DashboardDefinition[] = [
  {
    tab: 'executive',
    uid: 'drivetrace-executive-overview',
    slug: 'drivetrace-executive-overview',
    title: 'DriveTrace Core - Visão executiva',
    domain: 'Monitorização executiva',
    objective: 'Objetivo do dashboard executivo',
    decision: 'Suporte à decisão executiva',
    analysisType: 'Visão geral de produção, WIP e qualidade',
    questions: ['Quantas ordens estão abertas?', 'Quantas unidades estão em fluxo?', 'Existem problemas de qualidade?', 'Onde está o maior WIP?'],
  },
  {
    tab: 'wip',
    uid: 'drivetrace-wip-operations',
    slug: 'drivetrace-wip-operations',
    title: 'DriveTrace Core - Operações WIP',
    domain: 'Fluxo operacional WIP',
    objective: 'Objetivo do dashboard de operações WIP',
    decision: 'Suporte à decisão de operações WIP',
    analysisType: 'Fluxo, suportes e gargalos',
    questions: ['Onde existe acumulação?', 'Que suportes estão carregados?', 'Que movimentos ocorreram recentemente?'],
  },
  {
    tab: 'quality',
    uid: 'drivetrace-quality-traceability',
    slug: 'drivetrace-quality-traceability',
    title: 'DriveTrace Core - Qualidade e rastreabilidade',
    domain: 'Qualidade e rastreabilidade',
    objective: 'Objetivo do dashboard de qualidade',
    decision: 'Suporte à decisão de qualidade',
    analysisType: 'Evidência de qualidade e genealogia',
    questions: ['Qual é a taxa aprovado/reprovado?', 'Que unidades falharam?', 'Existe retrabalho ou não conformidade?'],
  },
  {
    tab: 'fiware',
    uid: 'drivetrace-fiware-infra-status',
    slug: 'drivetrace-fiware-infrastructure-status',
    title: 'DriveTrace Core - Estado FIWARE / infraestrutura',
    domain: 'FIWARE e infraestrutura',
    objective: 'Objetivo do dashboard FIWARE',
    decision: 'Suporte à decisão FIWARE',
    analysisType: 'Prontidão de publicação de contexto',
    questions: ['Quantas entidades são publicáveis?', 'Que tipos estão sincronizados?', 'O contexto está coerente?'],
  },
  {
    tab: 'overview',
    uid: 'drivetrace-wip-overview',
    slug: 'drivetrace-wip-overview',
    title: 'DriveTrace Core - Visão geral WIP',
    domain: 'Visão base WIP',
    objective: 'Objetivo do dashboard de visão geral WIP',
    decision: 'Suporte à decisão da visão geral WIP',
    analysisType: 'Estado WIP compacto de referência',
    questions: ['Qual é o estado global do WIP?', 'Que indicadores requerem atenção?', 'O que mudou recentemente?'],
  },
]

const dashboardsByTab: Record<TabKey, PanelCard[]> = {
  executive: [
    { key: 'executive-open-orders', title: 'Ordens de fabrico abertas', description: 'KPI de alto nível para ordens atuais.', dashboardUid: 'drivetrace-executive-overview', slug: 'drivetrace-executive-overview', panelId: 1, height: 260 },
    { key: 'executive-active-units', title: 'Unidades de produto ativas', description: 'Monitoriza as unidades atualmente ativas em produção.', dashboardUid: 'drivetrace-executive-overview', slug: 'drivetrace-executive-overview', panelId: 2, height: 260 },
    { key: 'executive-quality-issues', title: 'Problemas de qualidade em aberto', description: 'Destaca desvios de qualidade ativos.', dashboardUid: 'drivetrace-executive-overview', slug: 'drivetrace-executive-overview', panelId: 4, height: 280 },
    { key: 'executive-operations', title: 'Unidades de produto por estado', description: 'Visão operacional dos estados das unidades.', dashboardUid: 'drivetrace-executive-overview', slug: 'drivetrace-executive-overview', panelId: 6, height: 320 },
  ],
  wip: [
    { key: 'wip-by-section', title: 'Unidades por secção atual', description: 'Distribuição do WIP pelas secções de produção.', dashboardUid: 'drivetrace-wip-operations', slug: 'drivetrace-wip-operations', panelId: 1, height: 320 },
    { key: 'wip-supports-by-section', title: 'Suportes por secção atual', description: 'Ocupação de suportes por secção atual.', dashboardUid: 'drivetrace-wip-operations', slug: 'drivetrace-wip-operations', panelId: 2, height: 320 },
    { key: 'wip-units-status', title: 'Unidades de produto por estado', description: 'Distribuição operacional por estado das unidades.', dashboardUid: 'drivetrace-wip-operations', slug: 'drivetrace-wip-operations', panelId: 3, height: 320 },
    { key: 'wip-recent-movements', title: 'Histórico recente de localização de suportes', description: 'Eventos recentes de movimento e atribuições a racks.', dashboardUid: 'drivetrace-wip-operations', slug: 'drivetrace-wip-operations', panelId: 8, height: 380 },
  ],
  quality: [
    { key: 'quality-pass-fail', title: 'Resultados de qualidade aprovado/reprovado', description: 'Resultados dos pontos de controlo e equilíbrio aprovado/reprovado.', dashboardUid: 'drivetrace-quality-traceability', slug: 'drivetrace-quality-traceability', panelId: 1, height: 320 },
    { key: 'quality-nc-severity', title: 'Não conformidades por severidade', description: 'Distribuição de não conformidades por severidade.', dashboardUid: 'drivetrace-quality-traceability', slug: 'drivetrace-quality-traceability', panelId: 2, height: 320 },
    { key: 'quality-rework', title: 'Registos de retrabalho em aberto', description: 'Unidades atuais em estado de retrabalho.', dashboardUid: 'drivetrace-quality-traceability', slug: 'drivetrace-quality-traceability', panelId: 4, height: 260 },
    { key: 'quality-reconditioning-status', title: 'Estado de recuperação / recondicionamento', description: 'Candidatas, em recuperação, rejeitadas e recondicionadas.', dashboardUid: 'drivetrace-quality-traceability', slug: 'drivetrace-quality-traceability', panelId: 12, height: 320 },
    { key: 'quality-recent-events', title: 'Tabela de resultados de qualidade recentes', description: 'Evidência recente de qualidade e eventos de rastreabilidade.', dashboardUid: 'drivetrace-quality-traceability', slug: 'drivetrace-quality-traceability', panelId: 7, height: 380 },
    { key: 'quality-reconditioned-table', title: 'Unidades recuperadas e recondicionadas', description: 'Histórico operacional das decisões de recuperação produtiva.', dashboardUid: 'drivetrace-quality-traceability', slug: 'drivetrace-quality-traceability', panelId: 13, height: 380 },
  ],
  fiware: [
    { key: 'fiware-publishable-entities', title: 'Entidades de contexto publicáveis estimadas', description: 'Quantidade estimada de entidades prontas para publicação.', dashboardUid: 'drivetrace-fiware-infra-status', slug: 'drivetrace-fiware-infrastructure-status', panelId: 1, height: 260 },
    { key: 'fiware-current-supports', title: 'Número atual de suportes', description: 'Entidades de suporte atualmente disponíveis.', dashboardUid: 'drivetrace-fiware-infra-status', slug: 'drivetrace-fiware-infrastructure-status', panelId: 2, height: 260 },
    { key: 'fiware-candidates-type', title: 'Candidatos a entidades de contexto por tipo', description: 'Candidatos a entidades segmentados por tipo FIWARE.', dashboardUid: 'drivetrace-fiware-infra-status', slug: 'drivetrace-fiware-infrastructure-status', panelId: 5, height: 390 },
    { key: 'fiware-recent-movements', title: 'Eventos recentes de movimento de suportes', description: 'Fluxo de eventos operacional associado a atualizações de contexto.', dashboardUid: 'drivetrace-fiware-infra-status', slug: 'drivetrace-fiware-infrastructure-status', panelId: 6, height: 390 },
  ],
  overview: [
    { key: 'overview-open-orders', title: 'Ordens de fabrico abertas', description: 'KPI de alto nível para ordens atuais.', dashboardUid: 'drivetrace-wip-overview', slug: 'drivetrace-wip-overview', panelId: 1, height: 250 },
    { key: 'overview-active-units', title: 'Unidades de produto ativas', description: 'Monitoriza as unidades atualmente ativas em produção.', dashboardUid: 'drivetrace-wip-overview', slug: 'drivetrace-wip-overview', panelId: 2, height: 250 },
    { key: 'overview-wip-by-section', title: 'WIP por secção de produção', description: 'Distribuição do WIP pelas secções de produção.', dashboardUid: 'drivetrace-wip-overview', slug: 'drivetrace-wip-overview', panelId: 8, height: 320 },
    { key: 'overview-quality', title: 'Resultados de qualidade aprovado/reprovado', description: 'Resultados dos pontos de controlo e equilíbrio aprovado/reprovado.', dashboardUid: 'drivetrace-wip-overview', slug: 'drivetrace-wip-overview', panelId: 9, height: 320 },
  ],
}

const activeTab = ref<TabKey>('executive')
const grafanaAvailable = ref<boolean | null>(null)
const checkingConnectivity = ref(false)
const statusTimestampRaw = ref('')
const showAllDashboards = ref(false)
const showEmbeddedPanels = ref(true)
const showFullDashboard = ref(false)

let availabilityTimer: ReturnType<typeof setInterval> | undefined

const grafanaBaseUrl = computed(() => {
  const envUrl = import.meta.env.VITE_GRAFANA_BASE_URL || 'http://localhost:33010'
  return envUrl.replace(/\/+$/, '')
})

const grafanaTheme = computed<'dark' | 'light'>(() => (props.theme === 'dark' ? 'dark' : 'light'))
const activeDashboard = computed(() => dashboardCatalog.find((item) => item.tab === activeTab.value) ?? dashboardCatalog[0])
const visibleDashboardCatalog = computed(() => showAllDashboards.value ? dashboardCatalog : dashboardCatalog.slice(0, 3))
const activePanelCards = computed(() => dashboardsByTab[activeTab.value].slice(0, 3))

const statusTimestamp = computed(() => {
  if (!statusTimestampRaw.value) return '-'
  return new Date(statusTimestampRaw.value).toLocaleString(props.currentLocale)
})

const summaryTimestamp = computed(() => {
  const value = props.operationalData.summary.generatedAt
  return value ? new Date(value).toLocaleString(props.currentLocale) : t('No data available')
})

const apiStatusLabel = computed(() => t(props.apiStatus))
const apiStatusTone = computed(() => props.apiStatus.includes('Offline') ? 'tone-warning' : 'tone-success')
const dashboardStatusLabel = computed(() => {
  if (checkingConnectivity.value) return t('Checking')
  if (grafanaAvailable.value === true) return t('Available')
  if (grafanaAvailable.value === false) return t('Unavailable')
  return t('Not checked')
})
const dashboardStatusClass = computed(() => {
  if (grafanaAvailable.value === true) return 'badge-green'
  if (grafanaAvailable.value === false) return 'badge-red'
  return 'badge-gray'
})

const headerCards = computed(() => [
  {
    key: 'grafana',
    label: t('Grafana status'),
    value: dashboardStatusLabel.value,
    detail: grafanaBaseUrl.value,
    toneClass: grafanaAvailable.value === true ? 'tone-success' : grafanaAvailable.value === false ? 'tone-danger' : 'tone-muted',
  },
  {
    key: 'api',
    label: t('API status'),
    value: apiStatusLabel.value,
    detail: props.apiBaseUrl,
    toneClass: apiStatusTone.value,
  },
  {
    key: 'datasource',
    label: t('Data source'),
    value: datasourceName,
    detail: datasourceDetail,
    toneClass: 'tone-info',
  },
  {
    key: 'dashboards',
    label: t('Dashboards'),
    value: String(dashboardCatalog.length),
    detail: t('Provisioned in Grafana'),
    toneClass: 'tone-info',
  },
  {
    key: 'updated',
    label: t('Last update'),
    value: statusTimestamp.value,
    detail: `${t('API data')}: ${summaryTimestamp.value}`,
    toneClass: 'tone-muted',
  },
])

function normalized(value: string | undefined) {
  return (value || '').toLowerCase()
}

function countBy<T>(items: T[], predicate: (item: T) => boolean) {
  return items.filter(predicate).length
}

const activeUnits = computed(() => countBy(props.operationalData.units, (unit) => ['active', 'in progress', 'blocked', 'rework'].includes(normalized(unit.status))))
const blockedUnits = computed(() => countBy(props.operationalData.units, (unit) => ['blocked', 'rework', 'scrap'].includes(normalized(unit.status)) || normalized(unit.qualityStatus) === 'fail'))
const reworkUnits = computed(() => countBy(props.operationalData.units, (unit) => normalized(unit.status) === 'rework'))
const loadedSupports = computed(() => countBy(props.operationalData.supports, (support) => normalized(support.status) === 'loaded'))
const blockedSupports = computed(() => countBy(props.operationalData.supports, (support) => ['blocked', 'rework'].includes(normalized(support.status))))
const openOrders = computed(() => props.operationalData.summary.counts?.openOrders ?? countBy(props.operationalData.orders, (order) => !['completed', 'closed', 'cancelled'].includes(normalized(order.status))))
const activeRackAssignments = computed(() => props.operationalData.summary.counts?.rackAssignments ?? countBy(props.operationalData.rackSupportAssignments, (item) => !item.dateTimeOut))
const openNonconformities = computed(() => props.operationalData.summary.counts?.qualityIssues ?? countBy(props.operationalData.nonconformities, (item) => !['closed', 'completed'].includes(normalized(item.status))))
const openRework = computed(() => countBy(props.operationalData.reworkRecords, (item) => !['closed', 'completed'].includes(normalized(item.status))))
const reconditionedUnits = computed(() => props.operationalData.summary.counts?.reconditionedUnits
  ?? props.operationalData.flowSummary?.totals?.reconditionedUnits
  ?? countBy(props.operationalData.units, (unit) => unit.isReconditioned === true || normalized(unit.recoveryStatus) === 'reconditioned' || normalized(unit.qualityDisposition) === 'reconditioned'))
const recoveryCandidates = computed(() => props.operationalData.summary.counts?.recoveryCandidates
  ?? props.operationalData.flowSummary?.totals?.recoveryCandidates
  ?? countBy(props.operationalData.units, (unit) => ['candidate', 'recoverable'].includes(normalized(unit.recoveryStatus))))
const inRecoveryUnits = computed(() => props.operationalData.summary.counts?.inRecovery
  ?? countBy(props.operationalData.units, (unit) => normalized(unit.recoveryStatus) === 'inrecovery'))
const recoveryRate = computed(() => props.operationalData.summary.counts?.recoveryRate ?? props.operationalData.flowSummary?.totals?.recoveryRate ?? null)
const productionLineCount = computed(() => props.operationalData.flowSummary?.totals?.productionLines ?? 0)
const transferPoints = computed(() => props.operationalData.flowSummary?.totals?.transferPoints ?? 0)
const transfersLast24h = computed(() => props.operationalData.flowSummary?.totals?.transfersLast24h ?? 0)
const passResults = computed(() => countBy(props.operationalData.quality, (item) => normalized(item.result) === 'pass'))
const failResults = computed(() => countBy(props.operationalData.quality, (item) => normalized(item.result) === 'fail'))
const passRate = computed(() => {
  const total = passResults.value + failResults.value
  return total > 0 ? Math.round((passResults.value / total) * 100) : null
})
const topWipSection = computed(() => {
  const sections = props.operationalData.summary.wipBySection ?? []
  if (!sections.length) return null
  return [...sections].sort((a, b) => b.productUnits - a.productUnits)[0]
})
const emptyWipSections = computed(() => (props.operationalData.summary.wipBySection ?? []).filter((section) => section.productUnits === 0))
const rackUtilization = computed(() => {
  const total = props.operationalData.racks.length
  if (!total) return null
  return Math.round((activeRackAssignments.value / total) * 100)
})

const operationalMetrics = computed(() => [
  {
    key: 'open-orders',
    label: t('Ordens de fabrico abertas'),
    value: String(openOrders.value),
    detail: t('Orders requiring operational follow-up'),
  },
  {
    key: 'active-units',
    label: t('Unidades de produto ativas'),
    value: String(activeUnits.value),
    detail: `${blockedUnits.value} ${t('units requiring attention')}`,
  },
  {
    key: 'flow-lines',
    label: t('Production lines'),
    value: String(productionLineCount.value),
    detail: `${transferPoints.value} ${t('transfer points')} / ${transfersLast24h.value} ${t('transfers in last 24h')}`,
  },
  {
    key: 'attention-units',
    label: t('Units requiring attention'),
    value: String(blockedUnits.value),
    detail: t('Blocked, rework or failed quality units.'),
  },
  {
    key: 'supports',
    label: t('Supports in flow'),
    value: String(loadedSupports.value),
    detail: `${blockedSupports.value} ${t('supports blocked or in rework')}`,
  },
  {
    key: 'quality',
    label: t('Quality PASS rate'),
    value: passRate.value === null ? '-' : `${passRate.value}%`,
    detail: `${passResults.value} PASS / ${failResults.value} FAIL`,
  },
  {
    key: 'reconditioning',
    label: t('Recuperação / Recondicionamento'),
    value: String(reconditionedUnits.value),
    detail: `${recoveryCandidates.value} ${t('candidatas')} / ${inRecoveryUnits.value} ${t('em recuperação')} / ${recoveryRate.value === null ? '-' : `${recoveryRate.value}%`}`,
  },
  {
    key: 'racks',
    label: t('Rack utilization'),
    value: rackUtilization.value === null ? '-' : `${rackUtilization.value}%`,
    detail: `${activeRackAssignments.value} ${t('active rack assignments')}`,
  },
])

const operationalInsights = computed(() => {
  const topSection = topWipSection.value
  const topSectionName = topSection ? translateSectionName(topSection.section) : t('No data available')
  const emptySectionNames = emptyWipSections.value.slice(0, 3).map((section) => translateSectionName(section.section)).join(', ')

  return [
    {
      key: 'wip-concentration',
      domain: t('Operations WIP'),
      title: topSection && topSection.productUnits > 0 ? `${topSectionName}: ${topSection.productUnits}` : t('No data available'),
      description: topSection && topSection.productUnits > 0
        ? t('Highest WIP concentration insight')
        : t('No WIP concentration data available.'),
      action: topSection && topSection.productUnits > 0
        ? t('Validate capacity and exit rhythm before releasing new flow.')
        : t('No automatic recommendation available.'),
      toneClass: 'tone-info',
      tab: 'wip' as TabKey,
    },
    {
      key: 'blocked-units',
      domain: t('Quality and traceability'),
      title: blockedUnits.value > 0 ? `${blockedUnits.value} ${t('units requiring attention')}` : t('No blocked units right now.'),
      description: blockedUnits.value > 0 ? t('Blocked units require attention insight') : t('No blocked units insight'),
      action: blockedUnits.value > 0 ? t('Prioritize containment and quality decision.') : t('Keep monitoring quality gates.'),
      toneClass: blockedUnits.value > 0 ? 'tone-danger' : 'tone-success',
      tab: 'quality' as TabKey,
    },
    {
      key: 'nonconformities',
      domain: t('Quality evidence'),
      title: openNonconformities.value > 0 ? `${openNonconformities.value} ${t('open nonconformities')}` : t('No open nonconformities'),
      description: openNonconformities.value > 0 ? t('Open nonconformities insight') : t('No open nonconformities insight'),
      action: openNonconformities.value > 0 ? t('Review nonconformity owner and release criteria.') : t('No automatic recommendation available.'),
      toneClass: openNonconformities.value > 0 ? 'tone-warning' : 'tone-success',
      tab: 'quality' as TabKey,
    },
    {
      key: 'reconditioning',
      domain: t('Recuperação / Recondicionamento'),
      title: `${reconditionedUnits.value} ${t('unidades recondicionadas')}`,
      description: recoveryCandidates.value + inRecoveryUnits.value > 0
        ? t('Há unidades recuperáveis a aguardar decisão ou validação final.')
        : t('Sem pendências críticas de recondicionamento.'),
      action: recoveryCandidates.value + inRecoveryUnits.value > 0
        ? t('Validar severidade, retrabalho e aprovação funcional antes de libertar.')
        : t('Manter monitorização das decisões de recuperação.'),
      toneClass: recoveryCandidates.value + inRecoveryUnits.value > 0 ? 'tone-warning' : 'tone-success',
      tab: 'quality' as TabKey,
    },
    {
      key: 'racks',
      domain: t('Post-line logistics'),
      title: `${activeRackAssignments.value}/${props.operationalData.racks.length || 0}`,
      description: activeRackAssignments.value > 0 ? t('Rack assignments active insight') : t('Racks available insight'),
      action: activeRackAssignments.value > 0 ? t('Confirm rack exit timing and physical availability.') : t('Keep post-line capacity ready.'),
      toneClass: activeRackAssignments.value > 0 ? 'tone-info' : 'tone-success',
      tab: 'wip' as TabKey,
    },
    {
      key: 'empty-sections',
      domain: t('Line state'),
      title: emptyWipSections.value.length ? `${emptyWipSections.value.length} ${t('sections without WIP')}` : t('All monitored sections contain WIP'),
      description: emptySectionNames || t('No data available'),
      action: emptyWipSections.value.length ? t('Validate whether empty sections are expected for this scenario.') : t('No automatic recommendation available.'),
      toneClass: 'tone-muted',
      tab: 'overview' as TabKey,
    },
    {
      key: 'rework',
      domain: t('Rework records'),
      title: `${reworkUnits.value} ${t('units in rework')}`,
      description: openRework.value > 0 ? t('Open rework insight') : t('No open rework insight'),
      action: openRework.value > 0 ? t('Track cause, owner and closure of rework records.') : t('Keep monitoring rework queue.'),
      toneClass: openRework.value > 0 ? 'tone-warning' : 'tone-success',
      tab: 'quality' as TabKey,
    },
  ]
})

const visibleOperationalInsights = computed(() => operationalInsights.value.slice(0, 4))

const primaryOperationalDecision = computed(() => {
  const attentionInsight = operationalInsights.value.find((insight) => insight.toneClass === 'tone-danger')
    ?? operationalInsights.value.find((insight) => insight.toneClass === 'tone-warning')

  if (attentionInsight) {
    return {
      state: 'Atenção necessária',
      attention: attentionInsight.title,
      action: attentionInsight.action,
      evidence: attentionInsight.description,
      tab: attentionInsight.tab,
      toneClass: attentionInsight.toneClass,
    }
  }

  return {
    state: t('Fluxo operacional estável'),
    attention: t('No blocked units right now.'),
    action: t('Keep monitoring quality gates.'),
    evidence: `${activeUnits.value} unidades ativas / ${passRate.value === null ? '-' : `${passRate.value}%`} PASS`,
    tab: 'executive' as TabKey,
    toneClass: 'tone-success',
  }
})

function focusTab(tab: TabKey) {
  activeTab.value = tab
}

function focusEmbeddedDashboard(dashboard: DashboardDefinition) {
  activeTab.value = dashboard.tab
}

function buildDashboardUrl(dashboard: Pick<DashboardDefinition, 'uid' | 'slug'>) {
  const query = new URLSearchParams({
    orgId: '1',
    from: 'now-30d',
    to: 'now',
    timezone: 'browser',
    refresh: '30s',
    theme: grafanaTheme.value,
  })
  return `${grafanaBaseUrl.value}/d/${dashboard.uid}/${dashboard.slug}?${query.toString()}&kiosk`
}

async function checkGrafanaAvailability() {
  checkingConnectivity.value = true
  try {
    await fetch(`${grafanaBaseUrl.value}/api/health`, {
      mode: 'no-cors',
      cache: 'no-store',
    })
    grafanaAvailable.value = true
  } catch {
    grafanaAvailable.value = false
  } finally {
    statusTimestampRaw.value = new Date().toISOString()
    checkingConnectivity.value = false
  }
}

function refreshAll() {
  emit('refresh')
  void checkGrafanaAvailability()
}

function onEmbedLoaded() {
  grafanaAvailable.value = true
  statusTimestampRaw.value = new Date().toISOString()
}

function onEmbedFailed() {
  if (grafanaAvailable.value !== true) {
    grafanaAvailable.value = false
  }
  statusTimestampRaw.value = new Date().toISOString()
}

function openDashboardInGrafana(dashboard: Pick<DashboardDefinition, 'uid' | 'slug'>) {
  window.open(buildDashboardUrl(dashboard), '_blank', 'noopener,noreferrer')
}

onMounted(() => {
  void checkGrafanaAvailability()
  availabilityTimer = setInterval(() => {
    void checkGrafanaAvailability()
  }, 45000)
})

onBeforeUnmount(() => {
  if (availabilityTimer) clearInterval(availabilityTimer)
})
</script>
