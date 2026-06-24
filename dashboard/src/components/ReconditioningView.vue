<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { api, getApiErrorMessage } from '../services/api'

type ReconditioningSummary = {
  total: number
  candidates: number
  inRecovery: number
  reconditioned: number
  rejected: number
  scrap: number
  recoveryRate: number
  recommendation: string
}

type ReconditioningItem = {
  id?: number
  productUnitId: number
  unitCode: string
  unitStatus: string
  qualityStatus: string
  isReconditioned: boolean
  recoveryStatus: string
  qualityDisposition: string
  reconditionedAt?: string
  reconditionReason?: string
  manufacturingOrderId?: number
  orderNumber?: string
  currentLine?: string
  currentSection?: string
  nonconformityId?: number
  nonconformitySeverity?: string
  nonconformityStatus?: string
  nonconformityDescription?: string
  reworkRecordId?: number
  reworkStatus?: string
  scrapRecordId?: number
  decision?: string
  reason?: string
  notes?: string
  functionalValidation: boolean
  recordedAt?: string
  completedAt?: string
  rejectedAt?: string
  nextDisposition?: string
  canMarkRecoverable: boolean
  canComplete: boolean
  canReject: boolean
}

type ReconditioningList = {
  summary: ReconditioningSummary
  items: ReconditioningItem[]
}

const emit = defineEmits<{
  openTraceGraph: [unitId: number]
}>()

const loading = ref(false)
const actionLoading = ref(false)
const errorMessage = ref('')
const actionMessage = ref('')
const activeFilter = ref<'all' | 'candidates'>('all')
const list = ref<ReconditioningList | null>(null)
const selectedId = ref<number | null>(null)
const reason = ref('Recuperação produtiva validada por qualidade.')
const notes = ref('')
const nextDisposition = ref('Sucata')
const functionalValidation = ref(true)

const items = computed(() => list.value?.items ?? [])
const summary = computed<ReconditioningSummary>(() => list.value?.summary ?? {
  total: 0,
  candidates: 0,
  inRecovery: 0,
  reconditioned: 0,
  rejected: 0,
  scrap: 0,
  recoveryRate: 0,
  recommendation: 'Sem dados de recondicionamento.',
})

const selectedItem = computed(() => {
  return items.value.find((item) => item.productUnitId === selectedId.value || item.id === selectedId.value) ?? items.value[0] ?? null
})

onMounted(() => {
  void loadReconditioning()
})

async function loadReconditioning() {
  loading.value = true
  errorMessage.value = ''
  actionMessage.value = ''
  try {
    const path = activeFilter.value === 'candidates' ? '/reconditioning/candidates' : '/reconditioning'
    const response = await api.get<ReconditioningList>(path)
    list.value = response.data
    if (!selectedId.value && response.data.items[0]) selectedId.value = response.data.items[0].productUnitId
  } catch (error) {
    errorMessage.value = getApiErrorMessage(error)
  } finally {
    loading.value = false
  }
}

function selectFilter(value: 'all' | 'candidates') {
  activeFilter.value = value
  selectedId.value = null
  void loadReconditioning()
}

function selectItem(item: ReconditioningItem) {
  selectedId.value = item.productUnitId
  reason.value = item.reason || item.reconditionReason || 'Recuperação produtiva validada por qualidade.'
  notes.value = item.notes || ''
  nextDisposition.value = item.nextDisposition || 'Sucata'
  functionalValidation.value = item.functionalValidation || item.isReconditioned
}

async function markRecoverable(item = selectedItem.value) {
  if (!item) return
  await runAction(`/product-units/${item.productUnitId}/mark-reconditionable`, {
    nonconformityId: item.nonconformityId,
    reworkRecordId: item.reworkRecordId,
    reason: reason.value,
    notes: notes.value,
    functionalValidation: false,
  })
}

async function completeReconditioning(item = selectedItem.value) {
  if (!item) return
  await runAction(`/product-units/${item.productUnitId}/complete-reconditioning`, {
    nonconformityId: item.nonconformityId,
    reworkRecordId: item.reworkRecordId,
    reason: reason.value,
    notes: notes.value,
    functionalValidation: functionalValidation.value,
  })
}

async function rejectReconditioning(item = selectedItem.value) {
  if (!item) return
  await runAction(`/product-units/${item.productUnitId}/reject-reconditioning`, {
    nonconformityId: item.nonconformityId,
    reason: reason.value,
    nextDisposition: nextDisposition.value,
    notes: notes.value,
  })
}

async function runAction(path: string, payload: Record<string, unknown>) {
  actionLoading.value = true
  errorMessage.value = ''
  actionMessage.value = ''
  try {
    await api.post<ReconditioningItem>(path, payload)
    actionMessage.value = 'Decisão registada.'
    await loadReconditioning()
  } catch (error) {
    errorMessage.value = getApiErrorMessage(error)
  } finally {
    actionLoading.value = false
  }
}

function statusLabel(value?: string) {
  return {
    Candidate: 'Candidata',
    Recoverable: 'Recuperável',
    InRecovery: 'Em recuperação',
    Reconditioned: 'Recondicionada',
    Rejected: 'Rejeitada',
    Scrap: 'Sucata',
    Normal: 'Normal',
    Failed: 'Reprovada',
    Blocked: 'Bloqueada',
    Pending: 'Pendente',
    PASS: 'Aprovado',
    FAIL: 'Reprovado',
    Active: 'Ativa',
    Rework: 'Retrabalho',
    Completed: 'Concluída',
  }[value || ''] ?? value ?? 'n/d'
}

function statusTone(item: ReconditioningItem) {
  if (item.isReconditioned || item.recoveryStatus === 'Reconditioned') return 'ok'
  if (item.scrapRecordId || item.qualityDisposition === 'Scrap' || item.recoveryStatus === 'Rejected') return 'critical'
  if (item.recoveryStatus === 'InRecovery') return 'attention'
  return 'warning'
}

function formatDate(value?: string) {
  if (!value) return 'n/d'
  return new Intl.DateTimeFormat('pt-PT', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
}
</script>

<template>
  <div class="reconditioning-view">
    <section class="ops-hero">
      <div class="section-heading">
        <div class="min-w-0">
          <p>Qualidade operacional</p>
          <h3>Recuperação / Recondicionamento</h3>
          <p class="section-description">
            {{ summary.recommendation }}
          </p>
        </div>
        <div class="button-row">
          <button
            class="btn-secondary"
            :class="{ active: activeFilter === 'all' }"
            @click="selectFilter('all')"
          >
            Todos
          </button>
          <button
            class="btn-secondary"
            :class="{ active: activeFilter === 'candidates' }"
            @click="selectFilter('candidates')"
          >
            Candidatos
          </button>
          <button
            class="btn-primary"
            :disabled="loading"
            @click="loadReconditioning"
          >
            Atualizar
          </button>
        </div>
      </div>
      <div class="kpi-grid mt-5">
        <article class="kpi-card">
          <span>Total</span>
          <strong>{{ summary.total }}</strong>
          <p>Unidades com sinal de recuperação</p>
        </article>
        <article class="kpi-card warning">
          <span>Candidatas</span>
          <strong>{{ summary.candidates }}</strong>
          <p>Requerem decisão de qualidade</p>
        </article>
        <article class="kpi-card attention">
          <span>Em recuperação</span>
          <strong>{{ summary.inRecovery }}</strong>
          <p>Retrabalho ou validação pendente</p>
        </article>
        <article class="kpi-card ok">
          <span>Recondicionadas</span>
          <strong>{{ summary.reconditioned }}</strong>
          <p>{{ summary.recoveryRate }}% das decisões fechadas</p>
        </article>
      </div>
    </section>

    <p
      v-if="errorMessage"
      class="alert-error"
    >
      {{ errorMessage }}
    </p>
    <p
      v-if="actionMessage"
      class="alert-ok"
    >
      {{ actionMessage }}
    </p>

    <section class="reconditioning-grid">
      <div class="industrial-panel">
        <div class="section-heading">
          <div>
            <p>Lista de unidades</p>
            <h3>Estados de recuperação</h3>
          </div>
        </div>
        <div class="table-shell reconditioning-table-shell">
          <table class="data-table">
            <thead>
              <tr>
                <th>Unidade</th>
                <th>NC</th>
                <th>Estado</th>
                <th>Local</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="item in items"
                :key="`${item.productUnitId}-${item.id ?? 'candidate'}`"
                :class="{ selected: selectedItem?.productUnitId === item.productUnitId }"
                @click="selectItem(item)"
              >
                <td>
                  <strong>{{ item.unitCode }}</strong>
                  <span>{{ item.orderNumber || 'n/d' }}</span>
                </td>
                <td>
                  <strong>{{ statusLabel(item.nonconformitySeverity) }}</strong>
                  <span>{{ item.nonconformityStatus || 'n/d' }}</span>
                </td>
                <td>
                  <span
                    class="state-pill"
                    :class="statusTone(item)"
                  >{{ statusLabel(item.recoveryStatus) }}</span>
                </td>
                <td>
                  <strong>{{ item.currentSection || 'n/d' }}</strong>
                  <span>{{ item.currentLine || 'n/d' }}</span>
                </td>
                <td class="row-actions">
                  <button
                    class="icon-button"
                    title="Abrir grafo"
                    @click.stop="emit('openTraceGraph', item.productUnitId)"
                  >
                    MAP
                  </button>
                  <button
                    class="icon-button"
                    title="Marcar recuperável"
                    :disabled="!item.canMarkRecoverable || actionLoading"
                    @click.stop="markRecoverable(item)"
                  >
                    REC
                  </button>
                  <button
                    class="icon-button"
                    title="Concluir recondicionamento"
                    :disabled="!item.canComplete || actionLoading"
                    @click.stop="completeReconditioning(item)"
                  >
                    OK
                  </button>
                  <button
                    class="icon-button danger"
                    title="Rejeitar recuperação"
                    :disabled="!item.canReject || actionLoading"
                    @click.stop="rejectReconditioning(item)"
                  >
                    RJ
                  </button>
                </td>
              </tr>
              <tr v-if="!items.length">
                <td colspan="5">
                  <div class="empty-state">
                    <strong>Sem unidades para apresentar.</strong>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <aside class="industrial-panel detail-panel">
        <template v-if="selectedItem">
          <div class="section-heading">
            <div>
              <p>{{ selectedItem.unitCode }}</p>
              <h3>{{ statusLabel(selectedItem.recoveryStatus) }}</h3>
            </div>
            <span
              class="state-pill"
              :class="statusTone(selectedItem)"
            >{{ statusLabel(selectedItem.qualityDisposition) }}</span>
          </div>

          <dl class="detail-grid">
            <div><dt>Ordem</dt><dd>{{ selectedItem.orderNumber || 'n/d' }}</dd></div>
            <div><dt>Secção</dt><dd>{{ selectedItem.currentSection || 'n/d' }}</dd></div>
            <div><dt>Severidade</dt><dd>{{ statusLabel(selectedItem.nonconformitySeverity) }}</dd></div>
            <div><dt>Retrabalho</dt><dd>{{ selectedItem.reworkStatus || 'n/d' }}</dd></div>
            <div><dt>Registado</dt><dd>{{ formatDate(selectedItem.recordedAt) }}</dd></div>
            <div><dt>Fechado</dt><dd>{{ formatDate(selectedItem.completedAt || selectedItem.rejectedAt) }}</dd></div>
          </dl>

          <div class="detail-block">
            <span>Não conformidade</span>
            <p>{{ selectedItem.nonconformityDescription || 'n/d' }}</p>
          </div>

          <label class="form-label">
            Justificação
            <textarea
              v-model="reason"
              class="form-input min-h-[92px]"
            />
          </label>

          <label class="form-label">
            Notas
            <textarea
              v-model="notes"
              class="form-input min-h-[76px]"
            />
          </label>

          <div class="form-grid">
            <label class="form-label">
              Disposição se rejeitada
              <select
                v-model="nextDisposition"
                class="form-input"
              >
                <option value="Sucata">Sucata</option>
                <option value="Retrabalho">Retrabalho</option>
                <option value="Bloqueado">Bloqueado</option>
              </select>
            </label>
            <label class="check-row">
              <input
                v-model="functionalValidation"
                type="checkbox"
              >
              <span>Validação funcional final</span>
            </label>
          </div>

          <div class="button-row">
            <button
              class="btn-secondary"
              @click="emit('openTraceGraph', selectedItem.productUnitId)"
            >
              Abrir grafo
            </button>
            <button
              class="btn-secondary"
              :disabled="!selectedItem.canMarkRecoverable || actionLoading"
              @click="markRecoverable()"
            >
              Marcar recuperável
            </button>
            <button
              class="btn-primary"
              :disabled="!selectedItem.canComplete || actionLoading"
              @click="completeReconditioning()"
            >
              Concluir
            </button>
            <button
              class="btn-danger"
              :disabled="!selectedItem.canReject || actionLoading"
              @click="rejectReconditioning()"
            >
              Rejeitar
            </button>
          </div>
        </template>
      </aside>
    </section>
  </div>
</template>

<style scoped>
.reconditioning-view {
  display: grid;
  gap: 1.25rem;
}

.button-row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.button-row .active {
  border-color: rgb(20 184 166);
  color: rgb(15 118 110);
}

.reconditioning-grid {
  display: grid;
  gap: 1rem;
  grid-template-columns: minmax(0, 1.25fr) minmax(320px, 0.75fr);
}

.reconditioning-table-shell {
  max-height: 34rem;
  overflow: auto;
}

.data-table tr.selected {
  background: rgba(20, 184, 166, 0.08);
}

.data-table td span {
  display: block;
  color: rgb(100 116 139);
  font-size: 0.78rem;
  margin-top: 0.15rem;
}

.row-actions {
  display: flex;
  gap: 0.35rem;
}

.icon-button {
  border: 1px solid rgb(203 213 225);
  border-radius: 0.45rem;
  font-size: 0.7rem;
  font-weight: 900;
  min-width: 2.35rem;
  padding: 0.35rem 0.45rem;
}

.icon-button:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.icon-button.danger {
  color: rgb(185 28 28);
}

.state-pill {
  border-radius: 999px;
  display: inline-flex;
  font-size: 0.75rem;
  font-weight: 900;
  padding: 0.25rem 0.6rem;
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
  background: rgba(14, 165, 233, 0.14);
  color: rgb(3 105 161);
}

.state-pill.critical {
  background: rgba(239, 68, 68, 0.12);
  color: rgb(185 28 28);
}

.detail-panel {
  align-self: start;
}

.detail-grid {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin: 1rem 0;
}

.detail-grid div,
.detail-block {
  border: 1px solid rgb(226 232 240);
  border-radius: 0.5rem;
  padding: 0.7rem;
}

.detail-grid dt,
.detail-block span {
  color: rgb(100 116 139);
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
}

.detail-grid dd,
.detail-block p {
  color: rgb(15 23 42);
  font-weight: 800;
  margin: 0.2rem 0 0;
}

.form-grid {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: 1fr;
  margin: 0.75rem 0 1rem;
}

.check-row {
  align-items: center;
  display: flex;
  gap: 0.5rem;
  font-weight: 800;
}

.btn-danger {
  align-items: center;
  background: rgb(185 28 28);
  border-radius: 0.55rem;
  color: white;
  display: inline-flex;
  font-weight: 900;
  justify-content: center;
  padding: 0.65rem 0.95rem;
}

.alert-error,
.alert-ok {
  border-radius: 0.6rem;
  font-weight: 800;
  padding: 0.8rem 1rem;
}

.alert-error {
  background: rgba(254, 226, 226, 0.8);
  color: rgb(153 27 27);
}

.alert-ok {
  background: rgba(209, 250, 229, 0.8);
  color: rgb(6 95 70);
}

:global(.dark) .detail-grid div,
:global(.dark) .detail-block {
  border-color: rgb(51 65 85);
}

:global(.dark) .detail-grid dd,
:global(.dark) .detail-block p {
  color: rgb(248 250 252);
}

@media (max-width: 1024px) {
  .reconditioning-grid {
    grid-template-columns: 1fr;
  }
}
</style>
