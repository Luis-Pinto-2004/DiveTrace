<script setup lang="ts">
import { computed } from 'vue'
import { useOperationsStore, unitStatusLabel, unitStatusTone } from '@/stores/operations'
import { formatPercent, formatRelativeTime } from '@/utils/format'
import StatStrip from '@/components/common/StatStrip.vue'
import TimelineStep from '@/components/common/TimelineStep.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import DonutChart from '@/components/charts/DonutChart.vue'
import GaugeChart from '@/components/charts/GaugeChart.vue'
import ShopFloorMap from '@/components/factory/ShopFloorMap.vue'
import RouteStepper from '@/components/factory/RouteStepper.vue'

const ops = useOperationsStore()

const kpis = computed(() => [
  { label: 'Unidades em curso', value: ops.wipCount, tone: 'info' as const, hint: 'Unidades ativas na fábrica' },
  { label: 'Concluídas', value: ops.completedCount, tone: 'ok' as const, hint: 'Unidades terminadas' },
  { label: 'Em análise de qualidade', value: ops.pendingQualityCount, tone: 'warn' as const, hint: 'A aguardar decisão na Linha 4' },
  { label: 'Recondicionamento', value: ops.reconditioningCount, tone: 'action' as const, hint: 'Desvio controlado' },
])

const distribution = computed(() =>
  ops.statusDistribution.map((entry) => ({
    label: entry.label,
    value: entry.count,
    tone: entry.tone,
  })),
)

const approval = computed(() => ops.approvalRate)

const selected = computed(() => ops.selectedUnit)
const selectedLine = computed(() => (selected.value ? ops.lineById(selected.value.lineId) : undefined))
const selectedSectionName = computed(() => {
  if (!selected.value || !selectedLine.value) return ''
  return selectedLine.value.sections.find((s) => s.id === selected.value!.sectionId)?.name ?? ''
})
const selectedOrderName = computed(() =>
  selected.value?.orderId ? ops.orders.find((o) => o.id === selected.value!.orderId)?.name ?? '' : '',
)
const selectedQualityNote = computed(() => {
  const q = selected.value?.quality
  if (q === 'pending') return 'Em análise de qualidade.'
  if (q === 'recoverable') return 'Não conformidade recuperável.'
  if (q === 'reconditioning') return 'Em recondicionamento.'
  if (q === 'scrap') return 'Unidade em sucata.'
  return ''
})
const recentEvents = computed(() => (selected.value ? [...selected.value.history].reverse().slice(0, 4) : []))

function onSelect(id: string) {
  ops.selectUnit(id)
}
</script>

<template>
  <div class="cockpit">
    <StatStrip :items="kpis" />

    <div class="cockpit__analytics">
      <section class="card cockpit__split">
        <div>
          <h2 class="card__title">
            Distribuição por estado
          </h2>
          <DonutChart
            :slices="distribution"
            :center-value="ops.wipCount + ops.completedCount"
            center-label="total"
          />
        </div>
        <div class="cockpit__gauge">
          <h2 class="card__title">
            Taxa de aprovação
          </h2>
          <GaugeChart
            :value="approval.rate"
            label="Aprovadas / avaliadas"
          />
          <p class="cockpit__yield">
            {{ approval.approved }} de {{ approval.evaluated }} avaliadas ({{ formatPercent(approval.rate) }})
          </p>
        </div>
      </section>

      <section class="card cockpit__bottlenecks">
        <div class="card__head">
          <h2 class="card__title">
            Concentração de WIP por secção
          </h2>
        </div>
        <ul
          v-if="ops.bottlenecks.length"
          class="bottleneck-list"
        >
          <li
            v-for="b in ops.bottlenecks"
            :key="b.sectionId"
            class="bottleneck"
            :class="b.used >= 3 ? 'is-critical' : 'is-warn'"
          >
            <div>
              <p class="bottleneck__name">
                {{ b.name }}
              </p>
              <p class="bottleneck__detail">
                {{ b.used }} unidades em curso
              </p>
            </div>
            <span class="bottleneck__ratio">{{ b.used }}</span>
          </li>
        </ul>
        <EmptyState
          v-else
          title="Sem concentrações"
          description="Nenhuma secção acima de 2 unidades."
          icon="✓"
        />
      </section>
    </div>

    <div class="cockpit__production">
      <section class="card cockpit__flow">
        <div class="card__head">
          <h2 class="card__title">
            Visão rápida da produção
          </h2>
          <p class="card__sub">
            Linhas, secções e unidades em curso. Toque numa unidade para ver o percurso.
          </p>
        </div>
        <ShopFloorMap @select="onSelect" />
      </section>

      <aside class="card cockpit__detail">
        <div class="card__head">
          <h2 class="card__title">
            Percurso da unidade
          </h2>
        </div>
        <template v-if="selected">
          <div class="cockpit__detail-head">
            <div class="cockpit__detail-id">
              <p class="cockpit__detail-label">
                {{ selected.label }}
              </p>
              <p class="cockpit__detail-product">
                {{ selected.product }}<template v-if="selectedOrderName">
                  · {{ selectedOrderName }}
                </template>
              </p>
            </div>
            <StatusBadge
              :label="unitStatusLabel(selected)"
              :tone="unitStatusTone(selected)"
              size="sm"
            />
          </div>
          <p class="cockpit__detail-loc">
            {{ selectedLine?.code }} · {{ selectedSectionName }}
          </p>

          <RouteStepper :unit="selected" />

          <p
            v-if="selectedQualityNote"
            class="cockpit__detail-quality"
          >
            {{ selectedQualityNote }}
          </p>

          <p class="cockpit__timeline-title">
            Eventos recentes
          </p>
          <ul class="cockpit__timeline">
            <TimelineStep
              v-for="(event, idx) in recentEvents"
              :key="idx"
              :title="event.title"
              :detail="event.detail"
              :timestamp="formatRelativeTime(event.at)"
              :tone="event.tone"
              :last="idx === recentEvents.length - 1"
            />
          </ul>
        </template>
        <EmptyState
          v-else
          title="Selecione uma unidade"
          description="Toque numa unidade para ver o percurso, o suporte e a próxima etapa."
          icon="◎"
        />
      </aside>
    </div>
  </div>
</template>

<style scoped>
.cockpit {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.cockpit__analytics {
  display: grid;
  grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr);
  gap: 1rem;
  align-items: stretch;
}
.cockpit__production {
  display: grid;
  grid-template-columns: minmax(0, 2.1fr) minmax(0, 1fr);
  gap: 1rem;
  align-items: start;
}
@media (max-width: 1100px) {
  .cockpit__analytics,
  .cockpit__production {
    grid-template-columns: 1fr;
  }
}
.card {
  background: var(--dt-surface);
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  padding: 1rem;
  box-shadow: var(--dt-shadow-card);
}
.card__head {
  margin-bottom: 0.8rem;
}
.card__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.card__sub {
  margin: 0.1rem 0 0;
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
}
.cockpit__detail-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.5rem;
}
.cockpit__detail-id {
  min-width: 0;
}
.cockpit__detail-label {
  margin: 0;
  font-size: 1rem;
  font-weight: 900;
  color: var(--dt-text-strong);
  overflow-wrap: break-word;
}
.cockpit__detail-product {
  margin: 0;
  font-size: 0.75rem;
  color: var(--dt-neutral-text);
  overflow-wrap: break-word;
}
.cockpit__detail-loc {
  margin: 0.4rem 0 0.8rem;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--dt-info-text);
}
.cockpit__detail-quality {
  margin: 0.7rem 0 0;
  font-size: 0.72rem;
  font-weight: 800;
  color: var(--dt-warn-text);
  background: var(--dt-warn-surface);
  border: 1px solid var(--dt-warn-border);
  border-radius: var(--dt-radius);
  padding: 0.4rem 0.55rem;
}
.cockpit__actions {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1rem;
}
.btn {
  border-radius: var(--dt-radius);
  padding: 0.5rem 0.8rem;
  font-size: 0.78rem;
  font-weight: 800;
  cursor: pointer;
  border: 1px solid transparent;
}
.btn--primary {
  background: var(--dt-brand-500);
  color: #fff;
}
.btn--ghost {
  background: transparent;
  border-color: var(--dt-border);
  color: var(--dt-neutral-text);
}
.cockpit__timeline-title {
  margin: 0 0 0.5rem;
  font-size: 0.7rem;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--dt-neutral-text);
}
.cockpit__timeline {
  list-style: none;
  margin: 0;
  padding: 0;
}
.cockpit__split {
  display: flex;
  gap: 1.5rem;
  flex-wrap: wrap;
  align-items: flex-start;
}
.cockpit__split > div {
  flex: 1;
  min-width: 180px;
}
.cockpit__gauge {
  text-align: center;
}
.cockpit__yield {
  margin: 0.3rem 0 0;
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
}
.bottleneck-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.bottleneck {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.75rem;
  border-radius: 10px;
  border: 1px solid var(--dt-warn-border);
  background: var(--dt-warn-surface);
}
.bottleneck.is-critical {
  border-color: var(--dt-critical-border);
  background: var(--dt-critical-surface);
}
.bottleneck__name {
  margin: 0;
  font-size: 0.8rem;
  font-weight: 800;
  color: var(--dt-text-strong);
}
.bottleneck__detail {
  margin: 0;
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
}
.bottleneck__ratio {
  font-size: 1rem;
  font-weight: 900;
  color: var(--dt-warn-text);
}
.is-critical .bottleneck__ratio {
  color: var(--dt-critical-text);
}
.cockpit__history-toggle {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  background: transparent;
  border: none;
  cursor: pointer;
  font-size: 0.9rem;
  font-weight: 900;
  color: var(--dt-text-strong);
  padding: 0;
}
.cockpit__history-chev {
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
}
.cockpit__history-body {
  margin-top: 0.8rem;
}
</style>
