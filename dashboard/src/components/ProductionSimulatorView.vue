<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { api, getApiErrorMessage } from '../services/api'

type SimulationScenario = {
  key: string
  name: string
  description: string
  stepCount: number
  outcome: string
  entities: string[]
  highlights: string[]
  defaultUnitCode: string
  defaultOrderNumber: string
}

type SimulationRunSummary = {
  id: number
  runCode: string
  name: string
  scenarioKey: string
  scenarioName: string
  description: string
  status: string
  executionMode: string
  currentStep: number
  stepCount: number
  progressPercent: number
  startedAt: string
  pausedAt?: string | null
  completedAt?: string | null
  lastExecutedAt?: string | null
  lastStepType?: string | null
  lastStepDescription?: string | null
  lastResult?: string | null
  productUnitId?: number | null
  unitCode?: string | null
  manufacturingOrderId?: number | null
  orderNumber?: string | null
  currentLine?: string | null
  currentSection?: string | null
  supportCode?: string | null
  qualityStatus?: string | null
  qualityDisposition?: string | null
  recoveryStatus?: string | null
  isReconditioned: boolean
}

type SimulationStep = {
  id: number
  stepNumber: number
  stepType: string
  description: string
  executedAt: string
  productUnitId?: number | null
  manufacturingOrderId?: number | null
  fromLineId?: number | null
  toLineId?: number | null
  fromSectionId?: number | null
  toSectionId?: number | null
  result?: string | null
  operationalEventId?: number | null
}

type SimulationRunDetail = SimulationRunSummary & {
  notes?: string | null
  steps: SimulationStep[]
}

type SimulationState = {
  generatedAt: string
  lastAction: string
  unitsInMotion: number
  alerts: string[]
  scenarios: SimulationScenario[]
  activeRuns: SimulationRunSummary[]
  recentRuns: SimulationRunSummary[]
}

type SimulationRunCreateRequest = {
  scenarioKey: string
  name?: string
  speed: 'Manual' | 'Automatic'
  notes?: string
}

const emit = defineEmits<{
  openTraceGraphUnit: [unitId: number]
  openTraceGraphOrder: [orderId: number]
  openAnalytics: []
  openFiware: []
  refresh: []
}>()

const loading = ref(false)
const actionLoading = ref(false)
const errorMessage = ref('')
const actionMessage = ref('')
const state = ref<SimulationState>({
  generatedAt: '',
  lastAction: '',
  unitsInMotion: 0,
  alerts: [],
  scenarios: [],
  activeRuns: [],
  recentRuns: [],
})
const runs = ref<SimulationRunSummary[]>([])
const selectedRun = ref<SimulationRunDetail | null>(null)
const selectedRunId = ref<number | null>(null)
const selectedScenarioKey = ref('')
const runName = ref('Demonstração operacional')
const runSpeed = ref<'Manual' | 'Automatic'>('Manual')
const runNotes = ref('Simulação operacional de demonstração.')
const showAllRuns = ref(false)
let autoTimer: number | undefined

const selectedScenario = computed(() => state.value.scenarios.find((scenario) => scenario.key === selectedScenarioKey.value) ?? state.value.scenarios[0] ?? null)
const statusCards = computed(() => [
  { key: 'runs', label: 'Simulações ativas', value: state.value.activeRuns.length, detail: state.value.lastAction || 'Sem ações recentes.', tone: 'tone-info' },
  { key: 'motion', label: 'Unidades em movimento', value: state.value.unitsInMotion, detail: 'Passos de simulação em execução', tone: state.value.unitsInMotion > 0 ? 'tone-success' : 'tone-muted' },
  { key: 'alerts', label: 'Alertas', value: state.value.alerts.length, detail: state.value.alerts[0] || 'Sem alertas', tone: state.value.alerts.length ? 'tone-warning' : 'tone-success' },
  { key: 'scenarios', label: 'Cenários', value: state.value.scenarios.length, detail: 'Fluxos demo disponíveis', tone: 'tone-muted' },
])

const selectedRunSteps = computed(() => selectedRun.value?.steps ?? [])
const selectedRunTone = computed(() => statusTone(selectedRun.value?.status))
const selectedRunProgressLabel = computed(() => `${selectedRun.value?.progressPercent ?? 0}%`)
const visibleRuns = computed(() => showAllRuns.value ? runs.value : runs.value.slice(0, 6))
const selectedRunSummary = computed(() => {
  if (!selectedRun.value) return 'Selecione uma simulação ou crie um novo cenário.'
  return `${selectedRun.value.scenarioName} · passo ${selectedRun.value.currentStep}/${selectedRun.value.stepCount}`
})
const selectedRunOutcome = computed(() => {
  const run = selectedRun.value
  if (!run) return { label: 'Sem run selecionada', detail: 'Crie ou escolha uma simulação para ver o resultado.', tone: 'warning' }
  if (run.status === 'Running') return { label: 'Em execução', detail: selectedRunSummary.value, tone: 'flow' }
  if (run.status === 'Paused') return { label: 'Pausada', detail: selectedRunSummary.value, tone: 'attention' }
  if (run.qualityDisposition === 'Scrap' || run.recoveryStatus === 'Rejected') return { label: 'Sucata', detail: run.lastStepDescription || 'Cenário terminado com rejeição/sucata.', tone: 'critical' }
  if (run.isReconditioned || run.recoveryStatus === 'Reconditioned') return { label: 'Recondicionada', detail: run.lastStepDescription || 'Unidade recuperada com validação funcional.', tone: 'attention' }
  if (run.status === 'Completed' && run.qualityStatus === 'PASS') return { label: 'Conforme', detail: run.lastStepDescription || 'Fluxo terminado sem desvios críticos.', tone: 'ok' }
  if (run.status === 'Stopped') return { label: 'Terminada', detail: run.lastStepDescription || 'Run encerrada.', tone: 'warning' }
  return { label: formatRunStatus(run.status), detail: selectedRunSummary.value, tone: statusTone(run.status) }
})

onMounted(() => {
  void refreshAll()
})

onBeforeUnmount(() => {
  stopAutoRun()
})

watch(selectedRunId, (value) => {
  if (value) {
    void loadRun(value)
  }
})

async function refreshAll() {
  loading.value = true
  errorMessage.value = ''
  actionMessage.value = ''
  try {
    const [stateResponse, runsResponse] = await Promise.all([
      api.get<SimulationState>('/simulation/state'),
      api.get<SimulationRunSummary[]>('/simulation/runs'),
    ])

    state.value = stateResponse.data
    runs.value = runsResponse.data
    if (!selectedScenarioKey.value) {
      selectedScenarioKey.value = state.value.scenarios[0]?.key ?? ''
    }

    const nextRunId = selectedRunId.value
      ?? state.value.activeRuns[0]?.id
      ?? runs.value[0]?.id
      ?? null
    selectedRunId.value = nextRunId
    if (nextRunId) {
      await loadRun(nextRunId)
    } else {
      selectedRun.value = null
    }
    syncAutoRun()
  } catch (error) {
    errorMessage.value = getApiErrorMessage(error)
  } finally {
    loading.value = false
  }
}

async function loadRun(id: number) {
  try {
    const response = await api.get<SimulationRunDetail>(`/simulation/runs/${id}`)
    selectedRun.value = response.data
    selectedScenarioKey.value = response.data.scenarioKey
    runName.value = response.data.name
    runSpeed.value = response.data.executionMode === 'Automatic' ? 'Automatic' : 'Manual'
    syncAutoRun()
  } catch (error) {
    errorMessage.value = getApiErrorMessage(error)
    selectedRun.value = null
  }
}

function selectScenario(scenario: SimulationScenario) {
  selectedScenarioKey.value = scenario.key
  runName.value = scenario.name
  runSpeed.value = 'Manual'
}

async function createRun() {
  if (!selectedScenario.value) return
  actionLoading.value = true
  errorMessage.value = ''
  actionMessage.value = ''
  try {
    const payload: SimulationRunCreateRequest = {
      scenarioKey: selectedScenario.value.key,
      name: runName.value.trim() || selectedScenario.value.name,
      speed: runSpeed.value,
      notes: runNotes.value.trim() || undefined,
    }
    const response = await api.post<SimulationRunDetail>('/simulation/runs', payload)
    selectedRun.value = response.data
    selectedRunId.value = response.data.id
    runs.value = [response.data, ...runs.value.filter((item) => item.id !== response.data.id)]
    actionMessage.value = 'Simulação criada.'
    emit('refresh')
    syncAutoRun()
  } catch (error) {
    errorMessage.value = getApiErrorMessage(error)
  } finally {
    actionLoading.value = false
  }
}

async function tickRun() {
  if (!selectedRunId.value) return
  await runAction(`/simulation/runs/${selectedRunId.value}/tick`, 'Passo avançado.')
}

async function pauseRun() {
  if (!selectedRunId.value) return
  await runAction(`/simulation/runs/${selectedRunId.value}/pause`, 'Simulação pausada.')
}

async function resumeRun() {
  if (!selectedRunId.value) return
  await runAction(`/simulation/runs/${selectedRunId.value}/resume`, 'Simulação retomada.')
}

async function stopRun() {
  if (!selectedRunId.value) return
  await runAction(`/simulation/runs/${selectedRunId.value}/stop`, 'Simulação terminada.')
}

async function resetRun() {
  if (!selectedRunId.value) return
  await runAction(`/simulation/runs/${selectedRunId.value}/reset-demo`, 'Estado preservado e simulação encerrada.')
}

async function runAction(path: string, message: string) {
  actionLoading.value = true
  errorMessage.value = ''
  actionMessage.value = ''
  try {
    const response = await api.post<SimulationRunDetail>(path, {})
    selectedRun.value = response.data
    selectedRunId.value = response.data.id
    runs.value = [response.data, ...runs.value.filter((item) => item.id !== response.data.id)]
    actionMessage.value = message
    emit('refresh')
    syncAutoRun()
  } catch (error) {
    errorMessage.value = getApiErrorMessage(error)
  } finally {
    actionLoading.value = false
  }
}

function selectRun(run: SimulationRunSummary) {
  selectedRunId.value = run.id
}

function syncAutoRun() {
  if (!selectedRun.value || selectedRun.value.status !== 'Running' || selectedRun.value.executionMode !== 'Automatic') {
    stopAutoRun()
    return
  }

  if (autoTimer) return
  autoTimer = window.setInterval(() => {
    void tickRun()
  }, 2200)
}

function stopAutoRun() {
  if (!autoTimer) return
  window.clearInterval(autoTimer)
  autoTimer = undefined
}

function statusTone(status?: string) {
  const value = (status || '').toLowerCase()
  if (value === 'running') return 'ok'
  if (value === 'paused') return 'attention'
  if (value === 'completed') return 'ok'
  if (value === 'stopped') return 'critical'
  return 'warning'
}

function stepTone(result?: string | null) {
  const value = (result || '').toLowerCase()
  if (value.includes('pass') || value.includes('rack') || value.includes('reconditioned')) return 'ok'
  if (value.includes('fail') || value.includes('scrap')) return 'critical'
  if (value.includes('rework') || value.includes('recover')) return 'attention'
  return 'warning'
}

function stepVisualTone(step: SimulationStep) {
  if (step.stepType === 'Transfer') return 'flow'
  if (step.stepType === 'Quality') return (step.result || '').toLowerCase().includes('fail') ? 'critical' : 'ok'
  if (step.stepType === 'Reconditioning') return 'attention'
  if (step.stepType === 'Scrap') return 'critical'
  if (step.stepType === 'Rack') return 'ok'
  return stepTone(step.result)
}

function stepLabel(stepType: string) {
  return {
    Transfer: 'Transferência',
    Quality: 'Qualidade',
    Reconditioning: 'Recuperação / recondicionamento',
    Scrap: 'Sucata',
    Rack: 'Rack',
  }[stepType] ?? stepType
}

function formatDate(value?: string | null) {
  if (!value) return 'n/d'
  return new Intl.DateTimeFormat('pt-PT', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}

function formatRunStatus(status?: string) {
  return {
    Running: 'Em execução',
    Paused: 'Em pausa',
    Completed: 'Concluída',
    Stopped: 'Terminada',
    Draft: 'Rascunho',
  }[status || ''] ?? (status || 'n/d')
}

function scenarioStateTone(scenario: SimulationScenario) {
  return selectedScenarioKey.value === scenario.key ? 'ok' : 'warning'
}

function openSelectedRunUnitGraph() {
  if (!selectedRun.value?.productUnitId) return
  emit('openTraceGraphUnit', selectedRun.value.productUnitId)
}

function openSelectedRunOrderGraph() {
  if (!selectedRun.value?.manufacturingOrderId) return
  emit('openTraceGraphOrder', selectedRun.value.manufacturingOrderId)
}
</script>

<template>
  <div class="production-simulator-view">
    <section class="ops-hero">
      <div class="section-heading">
        <div class="min-w-0">
          <p>Operação demo</p>
          <h3>Simulador de produção</h3>
          <p class="section-description">Avance unidades pela linha para demonstrar fluxo, qualidade, retrabalho, recondicionamento e logística.</p>
        </div>
        <div class="flex flex-wrap gap-2">
          <button class="btn-secondary" @click="refreshAll" :disabled="loading">Atualizar</button>
          <button class="btn-secondary" @click="emit('openAnalytics')">Abrir analítica</button>
          <button class="btn-secondary" @click="emit('openFiware')">Abrir FIWARE</button>
        </div>
      </div>

      <div class="kpi-grid mt-5">
        <article v-for="card in statusCards" :key="card.key" class="kpi-card" :class="card.tone">
          <span>{{ card.label }}</span>
          <strong>{{ card.value }}</strong>
          <p>{{ card.detail }}</p>
        </article>
      </div>

      <div class="scenario-result-strip mt-4" :class="selectedRunOutcome.tone">
        <span>Resultado do cenário</span>
        <strong>{{ selectedRunOutcome.label }}</strong>
        <p>{{ selectedRunOutcome.detail }}</p>
      </div>
    </section>

    <p v-if="errorMessage" class="alert-error">{{ errorMessage }}</p>
    <p v-if="actionMessage" class="alert-ok">{{ actionMessage }}</p>

    <section class="grid gap-4 xl:grid-cols-3">
      <article
        v-for="scenario in state.scenarios"
        :key="scenario.key"
        class="industrial-panel scenario-card"
        :class="{ selected: selectedScenarioKey === scenario.key }"
        @click="selectScenario(scenario)"
      >
        <div class="section-heading">
          <div>
            <p>{{ scenario.key }}</p>
            <h3>{{ scenario.name }}</h3>
          </div>
          <span class="state-pill" :class="scenarioStateTone(scenario)">{{ scenario.stepCount }} passos</span>
        </div>
        <p class="mt-3 text-sm text-slate-600 dark:text-slate-300">{{ scenario.description }}</p>
        <dl class="detail-grid mt-4">
          <div><dt>Resultado</dt><dd>{{ scenario.outcome }}</dd></div>
          <div><dt>Ordem</dt><dd>{{ scenario.defaultOrderNumber }}</dd></div>
          <div><dt>Unidade</dt><dd>{{ scenario.defaultUnitCode }}</dd></div>
          <div><dt>Entidades</dt><dd>{{ scenario.entities.length }}</dd></div>
        </dl>
        <ul class="scenario-highlights mt-4">
          <li v-for="item in scenario.highlights" :key="item">{{ item }}</li>
        </ul>
      </article>
    </section>

    <section class="sim-grid">
      <div class="industrial-panel">
        <div class="section-heading">
          <div>
            <p>Criar simulação</p>
            <h3>Controlo operacional</h3>
          </div>
        </div>

        <div class="form-grid mt-4">
          <label class="form-label">
            Cenário
            <select v-model="selectedScenarioKey" class="form-input">
              <option v-for="scenario in state.scenarios" :key="scenario.key" :value="scenario.key">
                {{ scenario.name }}
              </option>
            </select>
          </label>

          <label class="form-label">
            Nome da simulação
            <input v-model="runName" class="form-input" />
          </label>

          <label class="form-label">
            Velocidade
            <select v-model="runSpeed" class="form-input">
              <option value="Manual">Manual</option>
              <option value="Automatic">Automática</option>
            </select>
          </label>

          <label class="form-label">
            Notas
            <textarea v-model="runNotes" class="form-input min-h-[92px]"></textarea>
          </label>
        </div>

        <div class="button-row mt-4">
          <button class="btn-primary" :disabled="actionLoading || !selectedScenario" @click="createRun">Criar simulação</button>
          <button class="btn-secondary" :disabled="actionLoading || !selectedRunId" @click="tickRun">Avançar passo</button>
          <button class="btn-secondary" :disabled="actionLoading || !selectedRunId || selectedRun?.status !== 'Running'" @click="syncAutoRun">Automático</button>
          <button class="btn-secondary" :disabled="actionLoading || !selectedRunId || selectedRun?.status !== 'Running'" @click="pauseRun">Pausar</button>
          <button class="btn-secondary" :disabled="actionLoading || !selectedRunId || selectedRun?.status !== 'Paused'" @click="resumeRun">Retomar</button>
          <button class="btn-danger" :disabled="actionLoading || !selectedRunId" @click="stopRun">Parar</button>
          <button class="btn-secondary" :disabled="actionLoading || !selectedRunId" @click="resetRun">Preparar demo limpa</button>
        </div>

        <div class="mt-5 rounded-lg border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-900/50">
          <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">Estado atual</p>
          <p class="mt-2 text-sm font-semibold text-slate-700 dark:text-slate-200">{{ selectedRunSummary }}</p>
          <div v-if="selectedRun" class="mt-4 grid gap-3 sm:grid-cols-2">
            <div>
              <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">Unidade</p>
              <p class="font-bold">{{ selectedRun.unitCode || 'n/d' }}</p>
            </div>
            <div>
              <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">Ordem</p>
              <p class="font-bold">{{ selectedRun.orderNumber || 'n/d' }}</p>
            </div>
            <div>
              <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">Linha</p>
              <p class="font-bold">{{ selectedRun.currentLine || 'n/d' }}</p>
            </div>
            <div>
              <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">Secção</p>
              <p class="font-bold">{{ selectedRun.currentSection || 'n/d' }}</p>
            </div>
            <div>
              <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">Qualidade</p>
              <p class="font-bold">{{ selectedRun.qualityStatus || 'n/d' }}</p>
            </div>
            <div>
              <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">Recondicionamento</p>
              <p class="font-bold">{{ selectedRun.isReconditioned ? 'Recondicionada' : (selectedRun.recoveryStatus || 'n/d') }}</p>
            </div>
          </div>
        </div>
      </div>

      <aside class="industrial-panel">
        <div class="section-heading">
          <div>
            <p>Run selecionada</p>
            <h3>{{ selectedRun?.name || 'Sem simulação selecionada' }}</h3>
          </div>
          <span class="state-pill" :class="selectedRunTone">{{ formatRunStatus(selectedRun?.status) }}</span>
        </div>

        <details v-if="selectedRun" class="technical-details mt-4">
          <summary>Detalhes técnicos</summary>
          <div class="detail-grid mt-3">
            <div><dt>Progresso</dt><dd>{{ selectedRunProgressLabel }}</dd></div>
            <div><dt>Velocidade</dt><dd>{{ selectedRun.executionMode }}</dd></div>
            <div><dt>Passo atual</dt><dd>{{ selectedRun.currentStep }}</dd></div>
            <div><dt>Último passo</dt><dd>{{ selectedRun.lastStepType || 'n/d' }}</dd></div>
            <div><dt>Iniciada</dt><dd>{{ formatDate(selectedRun.startedAt) }}</dd></div>
            <div><dt>Fechada</dt><dd>{{ formatDate(selectedRun.completedAt || selectedRun.pausedAt) }}</dd></div>
          </div>
        </details>

        <div class="mt-4 flex flex-wrap gap-2">
          <button class="btn-secondary" :disabled="!selectedRun?.productUnitId" @click="openSelectedRunUnitGraph">Abrir grafo da unidade</button>
          <button class="btn-secondary" :disabled="!selectedRun?.manufacturingOrderId" @click="openSelectedRunOrderGraph">Abrir grafo da ordem</button>
          <button class="btn-secondary" :disabled="!selectedRun?.productUnitId" @click="emit('openAnalytics')">Ver analítica</button>
          <button class="btn-secondary" @click="emit('openFiware')">Ver monitor FIWARE</button>
        </div>

        <div class="mt-5">
          <div class="section-heading">
            <div>
              <p>Histórico</p>
              <h3>Linha temporal de passos</h3>
            </div>
          </div>

          <div class="timeline-shell">
            <article v-for="step in selectedRunSteps" :key="step.id" class="timeline-card" :class="stepVisualTone(step)">
              <div class="flex items-start justify-between gap-3">
                <div>
                  <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">Passo {{ step.stepNumber }} · {{ stepLabel(step.stepType) }}</p>
                  <h4 class="text-base font-black">{{ step.description }}</h4>
                </div>
                <span class="state-pill" :class="stepVisualTone(step)">{{ step.result || 'n/d' }}</span>
              </div>
              <p class="mt-2 text-xs font-semibold text-slate-500 dark:text-slate-400">{{ formatDate(step.executedAt) }}</p>
            </article>
            <div v-if="!selectedRunSteps.length" class="empty-state">
              <strong>Sem passos executados ainda.</strong>
            </div>
          </div>
        </div>
      </aside>
    </section>

    <section class="industrial-panel">
      <div class="section-heading">
        <div>
          <p>Execuções recentes</p>
          <h3>Runs disponíveis</h3>
        </div>
        <button v-if="runs.length > 6" type="button" class="btn-secondary" @click="showAllRuns = !showAllRuns">
          {{ showAllRuns ? 'Mostrar últimas 6' : 'Ver todos' }}
        </button>
      </div>
      <div class="runs-grid mt-4">
        <button
          v-for="run in visibleRuns"
          :key="run.id"
          type="button"
          class="run-card"
          :class="{ active: selectedRunId === run.id }"
          @click="selectRun(run)"
        >
          <div class="flex items-start justify-between gap-2">
            <div class="min-w-0 text-left">
              <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">{{ run.scenarioName }}</p>
              <h4 class="truncate font-black">{{ run.name }}</h4>
            </div>
            <span class="state-pill" :class="statusTone(run.status)">{{ formatRunStatus(run.status) }}</span>
          </div>
          <p class="mt-2 text-left text-xs font-semibold text-slate-500 dark:text-slate-400">{{ run.runCode }} · {{ run.currentStep }}/{{ run.stepCount }} · {{ run.progressPercent }}%</p>
        </button>
      </div>
    </section>
  </div>
</template>

<style scoped>
.production-simulator-view {
  display: grid;
  gap: 1.25rem;
}

.scenario-card {
  cursor: pointer;
  transition: border-color 0.15s ease, transform 0.15s ease;
}

.scenario-card.selected {
  border-color: rgb(20 184 166);
  box-shadow: 0 0 0 1px rgba(20, 184, 166, 0.18);
  transform: translateY(-1px);
}

.scenario-highlights {
  display: grid;
  gap: 0.35rem;
  padding-left: 1rem;
  list-style: disc;
}

.sim-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: minmax(0, 1.05fr) minmax(320px, 0.95fr);
}

.button-row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.scenario-result-strip {
  border: 1px solid rgb(226 232 240);
  border-left: 0.35rem solid rgb(14 165 233);
  border-radius: 0.6rem;
  background: rgb(255 255 255 / 0.9);
  padding: 0.85rem 1rem;
}

.scenario-result-strip span {
  display: block;
  color: rgb(100 116 139);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.scenario-result-strip strong {
  display: block;
  margin-top: 0.25rem;
  color: rgb(15 23 42);
  font-size: 1.35rem;
  font-weight: 950;
  line-height: 1.1;
}

.scenario-result-strip p {
  margin-top: 0.3rem;
  color: rgb(71 85 105);
  font-size: 0.9rem;
  font-weight: 700;
}

.scenario-result-strip.ok { border-left-color: rgb(5 150 105); background: rgb(236 253 245 / 0.86); }
.scenario-result-strip.flow { border-left-color: rgb(37 99 235); background: rgb(239 246 255 / 0.9); }
.scenario-result-strip.attention { border-left-color: rgb(168 85 247); background: rgb(250 245 255 / 0.9); }
.scenario-result-strip.warning { border-left-color: rgb(245 158 11); background: rgb(255 251 235 / 0.9); }
.scenario-result-strip.critical { border-left-color: rgb(220 38 38); background: rgb(254 242 242 / 0.9); }

.technical-details {
  border: 1px solid rgb(226 232 240);
  border-radius: 0.65rem;
  background: rgb(248 250 252 / 0.86);
  padding: 0.75rem;
}

.technical-details summary {
  cursor: pointer;
  font-size: 0.78rem;
  font-weight: 950;
  text-transform: uppercase;
  color: rgb(71 85 105);
}

.timeline-shell {
  display: grid;
  gap: 0.75rem;
  max-height: 32rem;
  overflow: auto;
}

.timeline-card {
  border: 1px solid rgb(226 232 240);
  border-radius: 0.75rem;
  padding: 0.85rem 0.95rem;
}

.timeline-card.ok {
  background: rgba(16, 185, 129, 0.08);
}

.timeline-card.attention {
  background: rgba(168, 85, 247, 0.10);
}

.timeline-card.flow {
  background: rgba(37, 99, 235, 0.08);
  border-color: rgba(37, 99, 235, 0.32);
}

.timeline-card.warning {
  background: rgba(245, 158, 11, 0.08);
}

.timeline-card.critical {
  background: rgba(239, 68, 68, 0.08);
}

.runs-grid {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
}

.run-card {
  border: 1px solid rgb(226 232 240);
  border-radius: 0.75rem;
  padding: 0.9rem 1rem;
  text-align: left;
}

.run-card.active {
  border-color: rgb(20 184 166);
  background: rgba(20, 184, 166, 0.08);
}

.state-pill {
  border-radius: 999px;
  display: inline-flex;
  font-size: 0.75rem;
  font-weight: 900;
  padding: 0.25rem 0.65rem;
}

.state-pill.ok {
  background: rgba(16, 185, 129, 0.12);
  color: rgb(4 120 87);
}

.state-pill.warning {
  background: rgba(245, 158, 11, 0.14);
  color: rgb(146 64 14);
}

.state-pill.attention {
  background: rgba(168, 85, 247, 0.14);
  color: rgb(107 33 168);
}

.state-pill.flow {
  background: rgba(37, 99, 235, 0.14);
  color: rgb(29 78 216);
}

.state-pill.critical {
  background: rgba(239, 68, 68, 0.12);
  color: rgb(185 28 28);
}

.detail-grid {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.detail-grid div {
  border: 1px solid rgb(226 232 240);
  border-radius: 0.6rem;
  padding: 0.75rem;
}

.detail-grid dt,
.timeline-card p,
.run-card p {
  margin: 0;
}

.detail-grid dt {
  color: rgb(100 116 139);
  font-size: 0.7rem;
  font-weight: 900;
  text-transform: uppercase;
}

.detail-grid dd {
  color: rgb(15 23 42);
  font-weight: 800;
  margin-top: 0.2rem;
}

:global(.dark) .detail-grid div,
:global(.dark) .timeline-card,
:global(.dark) .run-card,
:global(.dark) .technical-details {
  border-color: rgb(51 65 85);
}

:global(.dark) .detail-grid dd,
:global(.dark) .timeline-card h4,
:global(.dark) .run-card h4,
:global(.dark) .scenario-result-strip strong {
  color: rgb(248 250 252);
}

:global(.dark) .scenario-result-strip,
:global(.dark) .technical-details {
  background: rgb(15 23 42 / 0.82);
}

@media (max-width: 1024px) {
  .sim-grid {
    grid-template-columns: 1fr;
  }
}
</style>
