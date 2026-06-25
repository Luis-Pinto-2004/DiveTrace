<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  useOperationsStore,
  unitStatusLabel,
  unitStatusTone,
  routeView,
  supportState,
  type Unit,
} from '@/stores/operations'
import { formatRelativeTime } from '@/utils/format'
import StatusBadge from '@/components/common/StatusBadge.vue'
import BarChart from '@/components/charts/BarChart.vue'
import type { BarItem } from '@/components/charts/BarChart.vue'
import FlowMap from '@/components/factory/FlowMap.vue'
import TransferDialog from '@/components/factory/TransferDialog.vue'
import RouteStepper from '@/components/factory/RouteStepper.vue'
import QualityDecisions from '@/components/factory/QualityDecisions.vue'
import SupportTag from '@/components/factory/SupportTag.vue'
import { useResourcesStore, RESOURCE_LABEL } from '@/stores/resources'

const ops = useOperationsStore()
const activeLineId = ref(ops.lines[0]?.id ?? 'L1')
const transferOpen = ref(false)

const line = computed(() => ops.lineById(activeLineId.value))
const sectionBars = computed<BarItem[]>(() => {
  const max = ops.maxSectionWip
  return (line.value?.sections ?? []).map((sec) => {
    const used = ops.sectionWip(sec.id)
    const ratio = used / max
    return {
      label: sec.name,
      value: used,
      tone: used >= Math.max(3, max) ? 'critical' : ratio >= 0.6 ? 'warn' : 'info',
      caption: `${used} em curso`,
    }
  })
})
const lineUnits = computed(() =>
  ops.unitsOf(activeLineId.value).filter((u) => u.state !== 'completed' && u.state !== 'scrap'),
)

const selected = computed(() => ops.selectedUnit)

function pick(id: string) {
  ops.selectUnit(id)
}
function advance(id: string) {
  ops.advanceUnit(id)
}
function transfer(id: string) {
  ops.selectUnit(id)
  transferOpen.value = true
}

// Encomenda a que a unidade pertence (nome legível) + código + posição da unidade.
function orderName(unit: Unit): string | null {
  if (!unit.orderId) return null
  return ops.orders.find((o) => o.id === unit.orderId)?.name ?? null
}
function orderCode(unit: Unit): string | null {
  if (!unit.orderId) return null
  return ops.orders.find((o) => o.id === unit.orderId)?.reference ?? null
}
function orderUnitIndex(unit: Unit): { idx: number; total: number } | null {
  if (!unit.orderId) return null
  const sibs = ops.units.filter((u) => u.orderId === unit.orderId)
  const idx = sibs.findIndex((u) => u.id === unit.id)
  return idx >= 0 ? { idx: idx + 1, total: sibs.length } : null
}
function orderRemaining(unit: Unit): number {
  if (!unit.orderId) return 0
  return ops.units.filter(
    (u) => u.orderId === unit.orderId && u.state !== 'completed' && u.state !== 'scrap',
  ).length
}

// Linha 4 = zona de validação final / decisão de qualidade.

// Mostra decisões de qualidade quando a unidade está na zona de validação (Linha 4)
// ou já tem uma disposição de qualidade por resolver.
function canDecide(unit: Unit): boolean {
  if (unit.state === 'completed' || unit.state === 'scrap') return false
  return unit.lineId === 'L4' || unit.quality === 'pending' || unit.quality === 'recoverable' || unit.quality === 'reconditioning'
}
function offRouteReason(unit: Unit): string | null {
  return routeView(unit).offRouteReason
}

// Atribuição de suporte
const assignSupportId = ref('')
function doAssignSupport(unit: Unit) {
  if (!assignSupportId.value) return
  ops.assignSupport(unit.id, assignSupportId.value)
  assignSupportId.value = ''
}
function supportCode(unit: Unit): string | null {
  return ops.supportById(unit.supportId)?.code ?? null
}

// Secções da linha ativa: responsável + ocupação/capacidade.
const resources = useResourcesStore()
const sectionRows = computed(() =>
  (line.value?.sections ?? []).map((s) => {
    const r = s.responsibleId ? resources.resources.find((x) => x.id === s.responsibleId) : null
    const occ = ops.sectionOccupancy(s.id)
    return {
      id: s.id,
      name: s.name,
      responsible: r?.name ?? null,
      responsibleType: r ? RESOURCE_LABEL[r.type] : null,
      occ,
      cap: s.capacity,
      unlimited: s.capacity >= 99,
      full: s.capacity < 99 && occ >= s.capacity,
    }
  }),
)
</script>

<template>
  <div
    class="line"
    data-demo="linha"
  >
    <div
      class="line__tabs"
      role="tablist"
    >
      <button
        v-for="l in ops.lines"
        :key="l.id"
        type="button"
        class="line__tab"
        :class="{ 'line__tab--active': l.id === activeLineId }"
        role="tab"
        :aria-selected="l.id === activeLineId"
        @click="activeLineId = l.id"
      >
        <span class="line__tab-code">{{ l.code }}</span>
        <span class="line__tab-name">{{ l.name }}</span>
      </button>
    </div>

    <div class="line__grid">
      <section class="card">
        <h2 class="card__title">
          Carga por secção
        </h2>
        <p class="card__sub">
          Unidades em curso por secção
        </p>
        <BarChart
          :items="sectionBars"
          unit="un"
        />
      </section>

      <section class="card line__sections">
        <h2 class="card__title">
          Secções e responsáveis
        </h2>
        <p class="card__sub">
          Responsável e capacidade de cada secção
        </p>
        <ul class="sec-list">
          <li
            v-for="s in sectionRows"
            :key="s.id"
            class="sec-row"
          >
            <div class="sec-row__main">
              <span class="sec-row__name">{{ s.name }}</span>
              <span class="sec-row__resp">
                <template v-if="s.responsible">
                  Responsável: <strong>{{ s.responsible }}</strong>
                  <span class="sec-row__resp-type">· {{ s.responsibleType }}</span>
                </template>
                <template v-else>Sem responsável atribuído</template>
              </span>
            </div>
            <span
              class="sec-row__cap"
              :class="{ 'sec-row__cap--full': s.full, 'sec-row__cap--free': s.unlimited }"
            >
              <template v-if="s.unlimited">{{ s.occ }} · armazém</template>
              <template v-else-if="s.full">{{ s.occ }}/{{ s.cap }} · cheia</template>
              <template v-else>{{ s.occ }}/{{ s.cap }}</template>
            </span>
          </li>
        </ul>
      </section>

      <section class="card line__flow">
        <h2 class="card__title">
          Fluxo da {{ line?.code }}
        </h2>
        <FlowMap
          :line-id="activeLineId"
          :selected-unit-id="ops.selectedUnitId"
          @select="pick"
        />
      </section>

      <section class="card line__units">
        <h2 class="card__title">
          Unidades na linha
        </h2>
        <ul class="unit-list">
          <li
            v-for="unit in lineUnits"
            :key="unit.id"
            class="unit-row"
            :class="{ 'unit-row--sel': unit.id === ops.selectedUnitId }"
          >
            <button
              type="button"
              class="unit-row__main"
              @click="pick(unit.id)"
            >
              <span class="unit-row__label">{{ unit.label }}</span>
              <span class="unit-row__product">{{ unit.product }}</span>
              <span
                v-if="orderName(unit)"
                class="unit-row__order"
              >
                <span class="unit-row__order-ref">{{ orderName(unit) }}</span>
                <span
                  v-if="orderRemaining(unit) === 1"
                  class="unit-row__last"
                >última unidade</span>
                <span
                  v-else
                  class="unit-row__remaining"
                >faltam {{ orderRemaining(unit) }}</span>
              </span>
            </button>
            <SupportTag
              :unit="unit"
              compact
            />
            <StatusBadge
              :label="unitStatusLabel(unit)"
              :tone="unitStatusTone(unit)"
              size="sm"
            />
            <div class="unit-row__actions">
              <button
                type="button"
                class="mini"
                title="Avançar etapa"
                @click="advance(unit.id)"
              >
                ▸
              </button>
              <button
                type="button"
                class="mini"
                title="Transferir"
                @click="transfer(unit.id)"
              >
                ⇄
              </button>
            </div>
          </li>
        </ul>
      </section>
    </div>

    <section
      v-if="selected"
      class="card line__detail"
    >
      <div class="detail__head">
        <div>
          <h2 class="card__title">
            Detalhe da unidade · {{ selected.label }}
          </h2>
          <p class="card__sub">
            {{ selected.product }}
            <template v-if="orderName(selected)">
              · {{ orderName(selected) }}
            </template>
          </p>
          <p
            v-if="orderCode(selected)"
            class="detail__order"
          >
            Ordem de fabrico {{ orderCode(selected) }}
            <template v-if="orderUnitIndex(selected)">
              · unidade {{ orderUnitIndex(selected)!.idx }} de {{ orderUnitIndex(selected)!.total }}
            </template>
          </p>
        </div>
        <StatusBadge
          :label="unitStatusLabel(selected)"
          :tone="unitStatusTone(selected)"
        />
      </div>

      <div class="detail__route">
        <h3 class="detail__h3">
          Rota produtiva
        </h3>
        <RouteStepper :unit="selected" />
      </div>

      <div class="detail__support">
        <h3 class="detail__h3">
          Suporte (rastreabilidade física)
        </h3>
        <div class="support-row">
          <SupportTag :unit="selected" />
          <span class="support-row__hint">
            <template v-if="supportState(selected) === 'assigned'">Suporte ativo. Unidade rastreável no chão de fábrica.</template>
            <template v-else-if="supportState(selected) === 'missing'">Sem suporte ativo. Rastreabilidade em risco.</template>
            <template v-else>Unidade planeada. Atribua o suporte antes de avançar.</template>
          </span>
        </div>
        <div
          v-if="selected.state !== 'completed' && selected.state !== 'scrap'"
          class="support-row support-row--actions"
        >
          <template v-if="selected.supportId">
            <span class="support-row__current">{{ supportCode(selected) }}</span>
            <button
              type="button"
              class="support-btn support-btn--ghost"
              @click="ops.releaseSupport(selected.id)"
            >
              Libertar
            </button>
          </template>
          <template v-else>
            <select
              v-model="assignSupportId"
              class="support-select"
            >
              <option
                value=""
                disabled
              >
                Escolher suporte…
              </option>
              <option
                v-for="s in ops.freeSupports"
                :key="s.id"
                :value="s.id"
              >
                {{ s.code }} · {{ s.type }}
              </option>
            </select>
            <button
              type="button"
              class="support-btn support-btn--primary"
              :disabled="!assignSupportId"
              @click="doAssignSupport(selected)"
            >
              Atribuir suporte
            </button>
          </template>
        </div>
      </div>

      <div
        v-if="canDecide(selected)"
        class="detail__quality"
      >
        <h3 class="detail__h3">
          Decisão de qualidade · Linha 4
        </h3>
        <p
          v-if="offRouteReason(selected)"
          class="detail__why"
        >
          {{ offRouteReason(selected) }}
        </p>
        <QualityDecisions :unit="selected" />
      </div>

      <div class="detail__history">
        <h3 class="detail__h3">
          Histórico
        </h3>
        <ul class="hist">
          <li
            v-for="(ev, i) in [...selected.history].reverse().slice(0, 6)"
            :key="i"
            class="hist__item"
            :class="`hist__item--${ev.tone ?? 'info'}`"
          >
            <span class="hist__title">{{ ev.title }}</span>
            <span
              v-if="ev.detail"
              class="hist__detail"
            >{{ ev.detail }}</span>
            <span class="hist__time">{{ formatRelativeTime(ev.at) }}</span>
          </li>
        </ul>
      </div>
    </section>

    <TransferDialog
      :open="transferOpen"
      :unit="selected"
      @close="transferOpen = false"
    />
  </div>
</template>

<style scoped>
.line {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.line__tabs {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.6rem;
}
@media (max-width: 800px) {
  .line__tabs {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
.line__tab {
  text-align: left;
  border: 1px solid var(--dt-neutral-border);
  background: var(--dt-surface);
  border-radius: 10px;
  padding: 0.6rem 0.75rem;
  cursor: pointer;
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}
:global(.dark) .line__tab {
  background: var(--dt-surface);
}
.line__tab--active {
  border-color: #0877d8;
  box-shadow: inset 0 0 0 1px #0877d8;
}
.line__tab-code {
  font-size: 0.78rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
:global(.dark) .line__tab-code {
  color: #f1f5f9;
}
.line__tab-name {
  font-size: 0.66rem;
  color: var(--dt-neutral-text);
}
.line__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}
@media (max-width: 1100px) {
  .line__grid {
    grid-template-columns: 1fr;
  }
}
.line__flow {
  grid-column: 1 / -1;
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
  margin: 0;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
:global(.dark) .card__title {
  color: #f1f5f9;
}
.card__sub {
  margin: 0.1rem 0 0.8rem;
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
}
.unit-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
}
.unit-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  padding: 0.5rem 0.3rem;
  border-bottom: 1px solid var(--dt-neutral-border);
}
.unit-row--sel {
  background: var(--dt-info-surface);
  border-radius: 8px;
}
.unit-row__main {
  flex: 1;
  display: flex;
  flex-direction: column;
  text-align: left;
  background: transparent;
  border: none;
  cursor: pointer;
  min-width: 0;
}
.unit-row__label {
  font-size: 0.78rem;
  font-weight: 800;
  color: var(--dt-text-strong);
  overflow-wrap: break-word;
}
:global(.dark) .unit-row__label {
  color: #f1f5f9;
}
.unit-row__product {
  font-size: 0.68rem;
  color: var(--dt-neutral-text);
  overflow-wrap: break-word;
}
.unit-row__order {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  margin-top: 0.25rem;
  flex-wrap: wrap;
  min-width: 0;
  max-width: 100%;
}
.unit-row__order-ref {
  font-size: 0.62rem;
  font-weight: 800;
  color: var(--dt-info-text);
  background: var(--dt-info-surface);
  border: 1px solid var(--dt-info-border);
  border-radius: 9999px;
  padding: 0.06rem 0.42rem;
  max-width: 100%;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.unit-row__last {
  font-size: 0.58rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.02em;
  color: var(--dt-action-text);
  background: var(--dt-action-surface);
  border: 1px solid var(--dt-action-border);
  border-radius: 9999px;
  padding: 0.06rem 0.42rem;
}
.unit-row__remaining {
  font-size: 0.6rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
}
.unit-row__actions {
  display: flex;
  gap: 0.3rem;
}
.mini {
  width: 28px;
  height: 28px;
  border-radius: 7px;
  border: 1px solid var(--dt-neutral-border);
  background: transparent;
  color: var(--dt-neutral-text);
  cursor: pointer;
  font-size: 0.85rem;
}
.mini:hover {
  border-color: #0877d8;
  color: #0877d8;
}
.line__zone-note {
  margin: 0;
  font-size: 0.74rem;
  font-weight: 700;
  color: var(--dt-info-text);
  background: var(--dt-info-surface);
  border: 1px solid var(--dt-info-border);
  border-radius: var(--dt-radius);
  padding: 0.55rem 0.75rem;
}
.line__detail {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.detail__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
  flex-wrap: wrap;
}
.detail__h3 {
  margin: 0 0 0.55rem;
  font-size: 0.78rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.detail__route,
.detail__support,
.detail__quality,
.detail__history {
  border-top: 1px solid var(--dt-border);
  padding-top: 0.85rem;
}
.support-row {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  flex-wrap: wrap;
}
.support-row--actions {
  margin-top: 0.6rem;
}
.support-row__hint {
  font-size: 0.72rem;
  color: var(--dt-neutral-text);
}
.support-row__current {
  font-size: 0.78rem;
  font-weight: 800;
  color: var(--dt-ok-text);
}
.support-select {
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius);
  padding: 0.4rem 0.5rem;
  background: var(--dt-surface);
  color: var(--dt-text-strong);
  font-size: 0.78rem;
}
.support-btn {
  border-radius: var(--dt-radius);
  padding: 0.4rem 0.75rem;
  font-size: 0.74rem;
  font-weight: 800;
  cursor: pointer;
  border: 1px solid transparent;
}
.support-btn--primary {
  background: var(--dt-brand-500);
  color: #fff;
}
.support-btn--primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.support-btn--ghost {
  background: transparent;
  border-color: var(--dt-border);
  color: var(--dt-neutral-text);
}
.detail__why {
  margin: 0 0 0.55rem;
  font-size: 0.74rem;
  font-weight: 700;
  color: var(--dt-action-text);
  background: var(--dt-action-surface);
  border: 1px solid var(--dt-action-border);
  border-radius: var(--dt-radius);
  padding: 0.45rem 0.6rem;
}
.hist {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.hist__item {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 0 0.6rem;
  font-size: 0.74rem;
  padding-left: 0.6rem;
  border-left: 3px solid var(--dt-border);
}
.hist__item--ok {
  border-left-color: var(--dt-ok-solid);
}
.hist__item--warn {
  border-left-color: var(--dt-warn-solid);
}
.hist__item--action {
  border-left-color: var(--dt-action-solid);
}
.hist__item--critical {
  border-left-color: var(--dt-critical-solid);
}
.hist__item--info {
  border-left-color: var(--dt-info-solid);
}
.hist__title {
  font-weight: 700;
  color: var(--dt-text-strong);
}
.hist__detail {
  grid-column: 1 / 2;
  color: var(--dt-neutral-text);
}
.hist__time {
  grid-row: 1 / 2;
  grid-column: 2 / 3;
  color: var(--dt-neutral-text);
  white-space: nowrap;
}
.line__sections {
  display: flex;
  flex-direction: column;
}
.sec-list {
  list-style: none;
  margin: 0.4rem 0 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}
.sec-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.6rem;
  padding: 0.45rem 0.55rem;
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius);
  background: var(--dt-surface-2);
}
.sec-row__main {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  min-width: 0;
}
.sec-row__name {
  font-size: 0.78rem;
  font-weight: 800;
  color: var(--dt-text-strong);
  overflow-wrap: break-word;
}
.sec-row__resp {
  font-size: 0.68rem;
  color: var(--dt-neutral-text);
  overflow-wrap: break-word;
}
.sec-row__resp strong {
  color: var(--dt-text-strong);
}
.sec-row__resp-type {
  opacity: 0.85;
}
.sec-row__cap {
  font-size: 0.68rem;
  font-weight: 800;
  white-space: nowrap;
  flex-shrink: 0;
  padding: 0.1rem 0.45rem;
  border-radius: 9999px;
  color: var(--dt-info-text);
  background: var(--dt-info-surface);
  border: 1px solid var(--dt-info-border);
}
.sec-row__cap--full {
  color: var(--dt-critical-text);
  background: var(--dt-critical-surface);
  border-color: var(--dt-critical-border);
}
.sec-row__cap--free {
  color: var(--dt-neutral-text);
  background: var(--dt-neutral-surface);
  border-color: var(--dt-neutral-border);
}
.detail__order {
  margin: 0.15rem 0 0;
  font-size: 0.66rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
  font-variant-numeric: tabular-nums;
}
</style>
