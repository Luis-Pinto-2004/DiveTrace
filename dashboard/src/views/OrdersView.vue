<script setup lang="ts">
import { computed } from 'vue'
import {
  useOperationsStore,
  orderStatusLabel,
  orderStatusTone,
  unitStatusLabel,
  type Order,
} from '@/stores/operations'
import { formatRelativeTime } from '@/utils/format'
import StatusBadge from '@/components/common/StatusBadge.vue'
import StatStrip from '@/components/common/StatStrip.vue'
import EmptyState from '@/components/common/EmptyState.vue'

const ops = useOperationsStore()

const columns = computed(() => [
  { key: 'submitted', title: 'Aguarda aceitação', orders: ops.orders.filter((o) => o.status === 'submitted') },
  { key: 'accepted', title: 'Aceites (a iniciar)', orders: ops.orders.filter((o) => o.status === 'accepted') },
  { key: 'in_production', title: 'Em produção', orders: ops.orders.filter((o) => o.status === 'in_production') },
  { key: 'ready', title: 'Prontas', orders: ops.orders.filter((o) => o.status === 'ready') },
  { key: 'completed', title: 'Concluídas', orders: ops.orders.filter((o) => o.status === 'completed') },
])

const kpis = computed(() => [
  { label: 'Por aceitar', value: ops.pendingOrders.length, tone: 'warn' as const },
  { label: 'Aceites', value: ops.acceptedOrders.length, tone: 'info' as const },
  { label: 'Em produção', value: ops.orders.filter((o) => o.status === 'in_production').length, tone: 'info' as const },
  { label: 'Prontas', value: ops.orders.filter((o) => o.status === 'ready').length, tone: 'ok' as const },
  { label: 'Concluídas', value: ops.orders.filter((o) => o.status === 'completed').length, tone: 'ok' as const },
])

function lineCode(order: Order) {
  return ops.lineById(order.lineId)?.code ?? order.lineId
}
function orderProgress(order: Order): number {
  if (order.status === 'completed') return 1
  if (order.status === 'ready') return 0.9
  const units = ops.units.filter((u) => u.orderId === order.id)
  if (!units.length) return 0
  const done = units.filter((u) => u.state === 'completed').length
  return done / units.length
}
// Unidades (ProductUnits) associadas à ordem, com a sua localização atual.
function orderUnits(order: Order) {
  return ops.units
    .filter((u) => u.orderId === order.id)
    .map((u) => {
      const line = ops.lineById(u.lineId)
      const sec = line?.sections.find((s) => s.id === u.sectionId)
      return { id: u.id, label: u.label, where: sec ? sec.name : unitStatusLabel(u) }
    })
}
</script>

<template>
  <div
    class="orders"
    data-demo="encomendas"
  >
    <StatStrip :items="kpis" />

    <div class="orders__board">
      <section
        v-for="col in columns"
        :key="col.key"
        class="orders__col"
      >
        <header class="orders__col-head">
          <span>{{ col.title }}</span>
          <span class="orders__count">{{ col.orders.length }}</span>
        </header>

        <EmptyState
          v-if="!col.orders.length"
          title="Vazio"
          icon="-"
        />

        <article
          v-for="order in col.orders"
          :key="order.id"
          :data-demo-order="order.id"
          class="order-card"
          :class="`order-card--${order.status}`"
        >
          <div class="order-card__top">
            <StatusBadge
              :label="orderStatusLabel(order.status)"
              :tone="orderStatusTone(order.status)"
              size="sm"
            />
          </div>
          <h3
            class="order-card__name"
            :title="order.name"
          >
            {{ order.name }}
          </h3>
          <p
            class="order-card__ref"
            :title="order.reference"
          >
            {{ order.reference }}
          </p>
          <p class="order-card__product">
            {{ order.quantity }}× {{ order.product }}
          </p>
          <p class="order-card__meta">
            {{ order.customer }} · {{ lineCode(order) }}
          </p>
          <ul
            v-if="orderUnits(order).length"
            class="order-card__units"
          >
            <li
              v-for="u in orderUnits(order)"
              :key="u.id"
              class="order-card__unit"
            >
              <span
                class="order-card__unit-code"
                :title="u.label"
              >{{ u.label }}</span>
              <span
                class="order-card__unit-loc"
                :title="u.where"
              >{{ u.where }}</span>
            </li>
          </ul>
          <p class="order-card__time">
            Criada {{ formatRelativeTime(order.createdAt) }}
          </p>

          <div
            v-if="order.status === 'in_production' || order.status === 'ready'"
            class="order-card__progress"
          >
            <div
              class="order-card__progress-fill"
              :style="{ width: `${Math.round(orderProgress(order) * 100)}%` }"
            />
          </div>

          <div
            v-if="order.status === 'submitted'"
            class="order-card__actions"
          >
            <button
              type="button"
              class="btn btn--ok"
              @click="ops.acceptOrder(order.id)"
            >
              Aceitar
            </button>
            <button
              type="button"
              class="btn btn--danger"
              @click="ops.rejectOrder(order.id, 'Recusada pelo supervisor')"
            >
              Recusar
            </button>
          </div>
          <div
            v-else-if="order.status === 'accepted'"
            class="order-card__actions"
          >
            <button
              type="button"
              class="btn btn--primary"
              @click="ops.startProduction(order.id)"
            >
              Iniciar produção
            </button>
          </div>
          <div
            v-else-if="order.status === 'ready'"
            class="order-card__actions"
          >
            <button
              type="button"
              class="btn btn--ok"
              @click="ops.deliverOrder(order.id)"
            >
              Concluir e expedir
            </button>
          </div>
        </article>
      </section>
    </div>
  </div>
</template>

<style scoped>
.orders {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.orders__board {
  display: flex;
  gap: 0.9rem;
  overflow-x: auto;
  padding-bottom: 0.5rem;
  min-width: 0;
  scroll-snap-type: x proximity;
}
.orders__col {
  flex: 1 0 280px;
  min-width: 280px;
  max-width: 380px;
  scroll-snap-align: start;
  background: var(--dt-neutral-surface);
  border: 1px solid var(--dt-neutral-border);
  border-radius: var(--dt-radius-lg);
  padding: 0.65rem;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
  min-height: 120px;
}
.orders__col-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.72rem;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: var(--dt-neutral-text);
  padding: 0 0.2rem;
}
.orders__count {
  background: var(--dt-surface);
  border: 1px solid var(--dt-neutral-border);
  border-radius: 9999px;
  min-width: 1.4rem;
  text-align: center;
  padding: 0 0.4rem;
  color: var(--dt-text-strong);
}
:global(.dark) .orders__count {
  background: var(--dt-surface-2);
  color: #e2e8f0;
}
.order-card {
  background: var(--dt-surface);
  border: 1px solid var(--dt-neutral-border);
  border-left: 3px solid var(--dt-neutral-solid);
  border-radius: 10px;
  padding: 0.7rem;
  box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05);
  min-width: 0;
}
:global(.dark) .order-card {
  background: var(--dt-surface);
}
.order-card--submitted {
  border-left-color: var(--dt-warn-solid);
}
.order-card--accepted {
  border-left-color: var(--dt-info-solid);
}
.order-card--in_production {
  border-left-color: var(--dt-info-solid);
}
.order-card--ready {
  border-left-color: var(--dt-ok-solid);
}
.order-card--completed {
  border-left-color: var(--dt-ok-solid);
}
.order-card__top {
  display: flex;
  margin-bottom: 0.35rem;
}
.order-card__name {
  margin: 0;
  font-size: 0.84rem;
  font-weight: 800;
  color: var(--dt-text-strong);
  line-height: 1.25;
  overflow-wrap: normal;
  word-break: normal;
  white-space: normal;
  hyphens: none;
}
.order-card__ref {
  margin: 0.2rem 0 0;
  font-size: 0.66rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  color: var(--dt-neutral-text);
  font-variant-numeric: tabular-nums;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.order-card__product {
  margin: 0.45rem 0 0;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--dt-text-strong);
  overflow-wrap: normal;
  word-break: normal;
}
.order-card__units {
  list-style: none;
  margin: 0.5rem 0 0;
  padding: 0.45rem 0 0;
  border-top: 1px solid var(--dt-neutral-border);
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.order-card__unit {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.4rem;
  font-size: 0.64rem;
  min-width: 0;
}
.order-card__unit-code {
  font-weight: 800;
  color: var(--dt-text-strong);
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}
.order-card__unit-loc {
  color: var(--dt-neutral-text);
  text-align: right;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.order-card__meta {
  margin: 0.3rem 0 0;
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
}
.order-card__time {
  margin: 0.2rem 0 0;
  font-size: 0.65rem;
  color: var(--dt-neutral-text);
}
.order-card__progress {
  margin-top: 0.5rem;
  height: 6px;
  border-radius: 9999px;
  background: var(--dt-neutral-surface);
  overflow: hidden;
}
.order-card__progress-fill {
  height: 100%;
  background: var(--dt-info-solid);
  transition: width var(--dt-motion-slow) var(--dt-ease);
}
.order-card__actions {
  display: flex;
  gap: 0.4rem;
  margin-top: 0.7rem;
}
.btn {
  flex: 1;
  border-radius: var(--dt-radius);
  padding: 0.45rem 0.6rem;
  font-size: 0.73rem;
  font-weight: 800;
  cursor: pointer;
  border: 1px solid transparent;
}
.btn--primary {
  background: var(--dt-brand-500, #0877d8);
  color: #fff;
}
.btn--ok {
  background: var(--dt-ok-solid);
  color: #fff;
}
.btn--danger {
  background: transparent;
  border-color: var(--dt-critical-border);
  color: var(--dt-critical-text);
}
</style>
