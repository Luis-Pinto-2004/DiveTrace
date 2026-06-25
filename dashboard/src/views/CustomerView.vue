<script setup lang="ts">
import { computed, ref } from 'vue'
import { useOperationsStore, orderStatusLabel, orderStatusTone, type Order } from '@/stores/operations'
import { formatRelativeTime, formatDate } from '@/utils/format'
import StatusBadge from '@/components/common/StatusBadge.vue'
import StatStrip from '@/components/common/StatStrip.vue'
import EmptyState from '@/components/common/EmptyState.vue'

// Identidade do cliente (demo). O cliente só vê as SUAS encomendas.
const CUSTOMER = 'Auto Lisboa'
const ops = useOperationsStore()

const products = computed(() => ops.products.map((p) => p.name))
const product = ref(ops.products[0]?.name ?? '')
const quantity = ref(4)
const desiredDate = ref('')
const note = ref('')
const feedback = ref('')
const expandedId = ref<string | null>(null)

function toggleDetails(id: string) {
  expandedId.value = expandedId.value === id ? null : id
}

// Resumo de unidades (apropriado ao cliente): concluídas / total.
function unitsSummary(order: Order): { done: number; total: number } {
  const units = ops.units.filter((u) => u.orderId === order.id)
  return { done: units.filter((u) => u.state === 'completed').length, total: units.length }
}

const myOrders = computed<Order[]>(() =>
  ops.orders.filter((o) => o.customer === CUSTOMER).sort((a, b) => b.createdAt - a.createdAt),
)

const kpis = computed(() => {
  const mine = ops.orders.filter((o) => o.customer === CUSTOMER)
  return [
    { label: 'Em aberto', value: mine.filter((o) => o.status === 'submitted' || o.status === 'accepted').length, tone: 'warn' as const },
    { label: 'Em produção', value: mine.filter((o) => o.status === 'in_production').length, tone: 'info' as const },
    { label: 'Prontas', value: mine.filter((o) => o.status === 'ready').length, tone: 'ok' as const },
    { label: 'Concluídas', value: mine.filter((o) => o.status === 'completed').length, tone: 'ok' as const },
  ]
})

// Passos visíveis ao cliente (sem detalhes internos da fábrica).
const STEPS: Array<{ key: Order['status']; label: string }> = [
  { key: 'submitted', label: 'Submetida' },
  { key: 'accepted', label: 'Aceite' },
  { key: 'in_production', label: 'Em produção' },
  { key: 'ready', label: 'Pronta' },
  { key: 'completed', label: 'Concluída' },
]

function stepIndex(order: Order): number {
  const idx = STEPS.findIndex((s) => s.key === order.status)
  return idx < 0 ? 0 : idx
}

// Progresso real: percentagem de unidades concluídas da encomenda.
// Sem unidades ainda criadas, não há progresso a mostrar.
function progress(order: Order): number {
  if (order.status === 'completed') return 1
  if (order.status === 'ready') return 0.9
  const units = ops.units.filter((u) => u.orderId === order.id)
  if (!units.length) return 0
  return units.filter((u) => u.state === 'completed').length / units.length
}

function placeOrder() {
  if (quantity.value < 1) return
  ops.placeOrder({
    customer: CUSTOMER,
    product: product.value,
    quantity: quantity.value,
    lineId: 'L1', // entrada na fábrica; o encaminhamento é decidido internamente
    note: note.value || undefined,
    desiredDate: desiredDate.value || undefined,
  })
  feedback.value = 'Pedido submetido. Vai receber atualização quando for aceite.'
  note.value = ''
  window.setTimeout(() => (feedback.value = ''), 3000)
}

function cancel(order: Order) {
  ops.cancelOrder(order.id)
  feedback.value = `${order.name} cancelada.`
  window.setTimeout(() => (feedback.value = ''), 3000)
}
</script>

<template>
  <div
    class="cust"
    data-demo="cliente"
  >
    <StatStrip :items="kpis" />

    <div class="cust__grid">
      <section class="card cust__form">
        <h2 class="card__title">
          Novo pedido
        </h2>
        <label class="cust__field">
          <span>Produto</span>
          <select v-model="product">
            <option
              v-for="p in products"
              :key="p"
              :value="p"
            >{{ p }}</option>
          </select>
        </label>
        <label class="cust__field">
          <span>Quantidade</span>
          <input
            v-model.number="quantity"
            type="number"
            min="1"
            max="50"
          >
        </label>
        <label class="cust__field">
          <span>Data desejada (opcional)</span>
          <input
            v-model="desiredDate"
            type="date"
          >
        </label>
        <label class="cust__field">
          <span>Observações (opcional)</span>
          <textarea
            v-model="note"
            rows="2"
            placeholder="Ex.: requisitos de acabamento"
          />
        </label>
        <button
          type="button"
          class="cust__submit"
          @click="placeOrder"
        >
          Submeter pedido
        </button>
        <p
          v-if="feedback"
          class="cust__ok"
          role="status"
        >
          {{ feedback }}
        </p>
      </section>

      <section class="card cust__list">
        <h2 class="card__title">
          Acompanhamento
        </h2>
        <EmptyState
          v-if="!myOrders.length"
          title="Sem encomendas"
          description="Os seus pedidos aparecem aqui."
          icon="-"
        />

        <article
          v-for="order in myOrders"
          :key="order.id"
          :data-demo-order="order.id"
          class="track"
        >
          <div class="track__head">
            <div>
              <span class="track__name">{{ order.name }}</span>
              <span class="track__product">{{ order.quantity }}× {{ order.product }}</span>
              <span class="track__ref">{{ order.reference }}</span>
            </div>
            <StatusBadge
              :label="orderStatusLabel(order.status)"
              :tone="orderStatusTone(order.status)"
              size="sm"
            />
          </div>

          <!-- Stepper amigável -->
          <div
            v-if="order.status !== 'rejected' && order.status !== 'cancelled'"
            class="stepper"
            :style="{ '--steps': STEPS.length }"
          >
            <div
              v-for="(step, i) in STEPS"
              :key="step.key"
              class="stepper__node"
              :class="{ 'is-done': i <= stepIndex(order), 'is-current': i === stepIndex(order) }"
            >
              <span class="stepper__dot" />
              <span class="stepper__label">{{ step.label }}</span>
            </div>
          </div>

          <div
            v-if="order.status === 'in_production'"
            class="track__bar"
          >
            <div
              class="track__bar-fill"
              :style="{ width: `${Math.round(progress(order) * 100)}%` }"
            />
          </div>

          <div class="track__meta">
            <span
              v-if="order.desiredDate"
              class="track__date"
            >Data desejada: {{ formatDate(order.desiredDate) }}</span>
            <span class="track__time">Atualizada {{ formatRelativeTime(order.startedAt ?? order.acceptedAt ?? order.createdAt) }}</span>
          </div>

          <div class="track__footer">
            <button
              type="button"
              class="track__details-btn"
              :aria-expanded="expandedId === order.id"
              @click="toggleDetails(order.id)"
            >
              {{ expandedId === order.id ? 'Ocultar detalhes' : 'Ver detalhes' }}
            </button>
            <button
              v-if="order.status === 'submitted'"
              type="button"
              class="track__cancel"
              @click="cancel(order)"
            >
              Cancelar pedido
            </button>
          </div>

          <dl
            v-if="expandedId === order.id"
            class="track__details"
          >
            <div class="track__detail">
              <dt>Encomenda</dt>
              <dd>{{ order.name }}</dd>
            </div>
            <div class="track__detail">
              <dt>Referência (rastreio)</dt>
              <dd>{{ order.reference }}</dd>
            </div>
            <div class="track__detail">
              <dt>Produto</dt>
              <dd>{{ order.product }}</dd>
            </div>
            <div class="track__detail">
              <dt>Quantidade</dt>
              <dd>{{ order.quantity }} unidades</dd>
            </div>
            <div class="track__detail">
              <dt>Estado</dt>
              <dd>{{ orderStatusLabel(order.status) }}</dd>
            </div>
            <div class="track__detail">
              <dt>Submetida</dt>
              <dd>{{ formatRelativeTime(order.createdAt) }}</dd>
            </div>
            <div
              v-if="order.acceptedAt"
              class="track__detail"
            >
              <dt>Aceite</dt>
              <dd>{{ formatRelativeTime(order.acceptedAt) }}</dd>
            </div>
            <div
              v-if="order.startedAt"
              class="track__detail"
            >
              <dt>Em produção desde</dt>
              <dd>{{ formatRelativeTime(order.startedAt) }}</dd>
            </div>
            <div
              v-if="order.completedAt"
              class="track__detail"
            >
              <dt>Concluída</dt>
              <dd>{{ formatRelativeTime(order.completedAt) }}</dd>
            </div>
            <div
              v-if="order.desiredDate"
              class="track__detail"
            >
              <dt>Data desejada</dt>
              <dd>{{ formatDate(order.desiredDate) }}</dd>
            </div>
            <div
              v-if="unitsSummary(order).total"
              class="track__detail"
            >
              <dt>Progresso</dt>
              <dd>{{ unitsSummary(order).done }} de {{ unitsSummary(order).total }} concluídas</dd>
            </div>
            <div
              v-if="order.note"
              class="track__detail track__detail--full"
            >
              <dt>Observações</dt>
              <dd>{{ order.note }}</dd>
            </div>
          </dl>
        </article>
      </section>
    </div>
  </div>
</template>

<style scoped>
.cust {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.cust__grid {
  display: grid;
  grid-template-columns: 320px 1fr;
  gap: 1rem;
}
@media (max-width: 900px) {
  .cust__grid {
    grid-template-columns: 1fr;
  }
}
.card {
  background: var(--dt-surface);
  border: 1px solid var(--dt-neutral-border);
  border-radius: var(--dt-radius-lg);
  padding: 1rem;
  box-shadow: var(--dt-shadow-card);
}
:global(.dark) .card {
  background: var(--dt-surface);
  border-color: var(--dt-border);
}
.card__title {
  margin: 0 0 0.8rem;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.cust__field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin-bottom: 0.7rem;
}
.cust__field span {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--dt-text-strong);
}
.cust__field select,
.cust__field input,
.cust__field textarea {
  border: 1px solid var(--dt-neutral-border);
  border-radius: var(--dt-radius);
  padding: 0.5rem;
  font-size: 0.82rem;
  background: var(--dt-surface);
  color: var(--dt-text-strong);
  font-family: inherit;
}
:global(.dark) .cust__field select,
:global(.dark) .cust__field input,
:global(.dark) .cust__field textarea {
  background: var(--dt-surface-2);
  color: #e2e8f0;
}
.cust__submit {
  width: 100%;
  border: none;
  border-radius: var(--dt-radius);
  padding: 0.65rem;
  background: linear-gradient(135deg, #0877d8, #065aa7);
  color: #fff;
  font-weight: 900;
  font-size: 0.85rem;
  cursor: pointer;
}
.cust__ok {
  margin: 0.6rem 0 0;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--dt-ok-text);
}
.cust__list {
  display: flex;
  flex-direction: column;
}
.track {
  border-bottom: 1px solid var(--dt-neutral-border);
  padding: 0.85rem 0;
}
.track:last-child {
  border-bottom: none;
}
.track__head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.5rem;
}
.track__name {
  display: block;
  font-size: 0.86rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.track__product {
  display: block;
  font-size: 0.76rem;
  color: var(--dt-text-strong);
  margin-top: 0.1rem;
}
.track__ref {
  display: block;
  font-size: 0.66rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
  font-variant-numeric: tabular-nums;
  margin-top: 0.05rem;
}
.stepper {
  display: grid;
  grid-template-columns: repeat(var(--steps), 1fr);
  gap: 0;
  margin: 0.8rem 0 0.6rem;
}
.stepper__node {
  position: relative;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.3rem;
}
.stepper__node::before {
  content: '';
  position: absolute;
  top: 7px;
  left: -50%;
  width: 100%;
  height: 2px;
  background: var(--dt-neutral-border);
  z-index: 0;
}
.stepper__node:first-child::before {
  display: none;
}
.stepper__node.is-done::before {
  background: var(--dt-info-solid);
}
.stepper__dot {
  position: relative;
  z-index: 1;
  width: 16px;
  height: 16px;
  border-radius: 9999px;
  background: var(--dt-surface);
  border: 2px solid var(--dt-neutral-border);
}
:global(.dark) .stepper__dot {
  background: var(--dt-surface);
}
.stepper__node.is-done .stepper__dot {
  background: var(--dt-info-solid);
  border-color: var(--dt-info-solid);
}
.stepper__node.is-current .stepper__dot {
  box-shadow: 0 0 0 4px var(--dt-info-surface);
}
.stepper__label {
  font-size: 0.6rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
  text-align: center;
}
.stepper__node.is-current .stepper__label {
  color: var(--dt-info-text);
}
.track__bar {
  height: 8px;
  border-radius: 9999px;
  background: var(--dt-neutral-surface);
  overflow: hidden;
  margin-bottom: 0.5rem;
}
.track__bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #3b82f6, #10b981);
  transition: width var(--dt-motion-slow) var(--dt-ease);
}
.track__meta {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem 1rem;
  font-size: 0.68rem;
  color: var(--dt-neutral-text);
}
.track__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: 0.6rem;
}
.track__details-btn {
  border: none;
  background: transparent;
  color: var(--dt-info-text);
  padding: 0.15rem 0;
  font-size: 0.72rem;
  font-weight: 800;
  cursor: pointer;
}
.track__details-btn:hover {
  text-decoration: underline;
}
.track__cancel {
  border: 1px solid var(--dt-critical-border);
  background: transparent;
  color: var(--dt-critical-text);
  border-radius: var(--dt-radius);
  padding: 0.35rem 0.7rem;
  font-size: 0.72rem;
  font-weight: 800;
  cursor: pointer;
}
.track__cancel:hover {
  background: var(--dt-critical-surface);
}
.track__details {
  margin: 0.6rem 0 0;
  padding: 0.7rem 0.8rem;
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.5rem 1rem;
  background: var(--dt-surface-2);
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius);
}
.track__detail {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  min-width: 0;
}
.track__detail--full {
  grid-column: 1 / -1;
}
.track__detail dt {
  font-size: 0.62rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: var(--dt-neutral-solid);
}
.track__detail dd {
  margin: 0;
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--dt-text-strong);
  word-break: break-word;
}
@media (max-width: 480px) {
  .track__details {
    grid-template-columns: 1fr;
  }
}
</style>
