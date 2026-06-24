<script setup lang="ts">
import { computed, ref } from 'vue'
import { useOperationsStore, unitStatusLabel, unitStatusTone, routeView, type Unit } from '@/stores/operations'
import { formatRelativeTime, formatPercent } from '@/utils/format'
import StatStrip from '@/components/common/StatStrip.vue'
import DecisionCard from '@/components/common/DecisionCard.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import GaugeChart from '@/components/charts/GaugeChart.vue'
import ActivityFeed from '@/components/common/ActivityFeed.vue'
import QualityDecisions from '@/components/factory/QualityDecisions.vue'

const ops = useOperationsStore()

type Filter = 'all' | 'pending' | 'recoverable' | 'reconditioning'
const filter = ref<Filter>('all')

// Fila de qualidade: tudo o que está fora do fluxo normal por decisão de qualidade.
const queue = computed(() =>
  ops.units.filter(
    (u) => u.quality === 'pending' || u.quality === 'recoverable' || u.quality === 'reconditioning',
  ),
)
const filtered = computed(() => {
  if (filter.value === 'all') return queue.value
  return queue.value.filter((u) => u.quality === filter.value)
})

const counts = computed(() => ({
  pending: ops.units.filter((u) => u.quality === 'pending').length,
  recoverable: ops.units.filter((u) => u.quality === 'recoverable').length,
  reconditioning: ops.units.filter((u) => u.quality === 'reconditioning').length,
  approved: ops.approvedCount,
  scrap: ops.scrapCount,
}))

const kpis = computed(() => [
  { label: 'Em análise de qualidade', value: counts.value.pending, tone: 'warn' as const, hint: 'A aguardar decisão' },
  { label: 'Recondicionável', value: counts.value.recoverable, tone: 'action' as const, hint: 'NC recuperável' },
  { label: 'Recondicionamento', value: counts.value.reconditioning, tone: 'action' as const, hint: 'Desvio controlado' },
  { label: 'Sucata', value: counts.value.scrap, tone: 'critical' as const },
])

const approval = computed(() => ops.approvalRate)

const filters: Array<{ key: Filter; label: string }> = [
  { key: 'all', label: 'Todas' },
  { key: 'pending', label: 'Em análise' },
  { key: 'recoverable', label: 'Recondicionável' },
  { key: 'reconditioning', label: 'Recondicionamento' },
]

const qualityActivity = computed(() =>
  ops.recentActivity
    .filter((r) => r.tone === 'ok' || r.tone === 'action' || r.tone === 'warn' || r.tone === 'critical')
    .slice(0, 8)
    .map((r) => ({ id: r.id, title: r.title, detail: r.detail, timestamp: formatRelativeTime(r.at), tone: r.tone })),
)
// Histórico de qualidade: compacto e colapsável (não compete com a decisão).
const showHistory = ref(false)
const latestQuality = computed(() => qualityActivity.value[0] ?? null)
const recentQuality = computed(() => qualityActivity.value.slice(0, 5))

function locationOf(unit: Unit): string {
  const line = ops.lineById(unit.lineId)
  const section = line?.sections.find((s) => s.id === unit.sectionId)?.name ?? ''
  return `${line?.code ?? ''} · ${section}`
}
function orderName(unit: Unit): string {
  return ops.orders.find((o) => o.id === unit.orderId)?.name ?? 'sem encomenda'
}
function whyOffFlow(unit: Unit): string | null {
  return routeView(unit).offRouteReason
}
</script>

<template>
  <div class="quality">
    <StatStrip :items="kpis" />

    <div class="quality__layout">
      <section class="card quality__main">
        <div class="card__head">
          <div>
            <h2 class="card__title">
              Decisões de qualidade · Linha 4
            </h2>
            <p class="card__sub">
              {{ filtered.length }} unidade(s) na zona de validação final
            </p>
          </div>
          <div
            class="seg"
            role="tablist"
            aria-label="Filtrar decisões"
          >
            <button
              v-for="f in filters"
              :key="f.key"
              type="button"
              class="seg__btn"
              :class="{ 'seg__btn--active': filter === f.key }"
              role="tab"
              :aria-selected="filter === f.key"
              @click="filter = f.key"
            >
              {{ f.label }}
            </button>
          </div>
        </div>

        <div
          v-if="filtered.length"
          class="quality__grid"
        >
          <DecisionCard
            v-for="unit in filtered"
            :key="unit.id"
            :title="unit.label"
            :context="`${unit.product} · ${orderName(unit)} · ${locationOf(unit)}`"
            :status-label="unitStatusLabel(unit)"
            :tone="unitStatusTone(unit)"
            :timestamp="formatRelativeTime(unit.updatedAt)"
          >
            <template #body>
              <p
                v-if="whyOffFlow(unit)"
                class="quality__why"
              >
                {{ whyOffFlow(unit) }}
              </p>
            </template>
            <template #actions>
              <QualityDecisions :unit="unit" />
            </template>
          </DecisionCard>
        </div>
        <EmptyState
          v-else
          title="Sem decisões pendentes"
          :description="filter === 'all' ? 'Nenhuma unidade a aguardar decisão de qualidade.' : 'Nenhuma unidade nesta categoria.'"
          icon="✓"
        />
      </section>

      <aside class="quality__side">
        <section class="card quality__summary">
          <h2 class="card__title">
            Conformidade
          </h2>
          <GaugeChart
            :value="approval.rate"
            label="Aprovadas / avaliadas"
          />
          <p class="quality__rate">
            {{ approval.approved }} de {{ approval.evaluated }} avaliadas ({{ formatPercent(approval.rate) }})
          </p>
          <ul class="breakdown">
            <li>
              <span class="breakdown__dot breakdown__dot--ok" />
              Aprovadas <strong>{{ counts.approved }}</strong>
            </li>
            <li>
              <span class="breakdown__dot breakdown__dot--warn" />
              Em análise <strong>{{ counts.pending }}</strong>
            </li>
            <li>
              <span class="breakdown__dot breakdown__dot--action" />
              Recondicionável <strong>{{ counts.recoverable }}</strong>
            </li>
            <li>
              <span class="breakdown__dot breakdown__dot--action" />
              Recondicionamento <strong>{{ counts.reconditioning }}</strong>
            </li>
            <li>
              <span class="breakdown__dot breakdown__dot--critical" />
              Sucata <strong>{{ counts.scrap }}</strong>
            </li>
          </ul>
        </section>

        <section class="card quality__history">
          <button
            type="button"
            class="quality__history-toggle"
            :aria-expanded="showHistory"
            @click="showHistory = !showHistory"
          >
            <span class="quality__history-title">Histórico de qualidade</span>
            <span
              class="quality__history-chevron"
              :class="{ 'is-open': showHistory }"
              aria-hidden="true"
            >▸</span>
          </button>
          <p
            v-if="!showHistory && latestQuality"
            class="quality__history-latest"
            :title="latestQuality.title"
          >
            Último: {{ latestQuality.title }}
          </p>
          <ActivityFeed
            v-if="showHistory"
            :items="recentQuality"
            empty-title="Sem eventos de qualidade"
          />
        </section>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.quality {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.quality__layout {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 1rem;
  align-items: start;
}
@media (max-width: 1000px) {
  .quality__layout {
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
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
  flex-wrap: wrap;
  margin-bottom: 0.9rem;
}
.card__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.card__sub {
  margin: 0.15rem 0 0;
  font-size: 0.72rem;
  color: var(--dt-neutral-text);
}
.seg {
  display: inline-flex;
  flex-wrap: wrap;
  background: var(--dt-neutral-surface);
  border: 1px solid var(--dt-border);
  border-radius: 9999px;
  padding: 3px;
}
.seg__btn {
  border: none;
  background: transparent;
  color: var(--dt-neutral-text);
  border-radius: 9999px;
  padding: 0.32rem 0.7rem;
  font-size: 0.72rem;
  font-weight: 800;
  cursor: pointer;
}
.seg__btn--active {
  background: var(--dt-brand-500);
  color: #fff;
}
.quality__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 0.9rem;
}
.quality__why {
  margin: 0.2rem 0 0.5rem;
  font-size: 0.72rem;
  color: var(--dt-action-text);
}
.quality__side {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.quality__summary {
  text-align: center;
}
.quality__rate {
  margin: 0.3rem 0 0.8rem;
  font-size: 0.74rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
}
.breakdown {
  list-style: none;
  margin: 0;
  padding: 0.6rem 0 0;
  border-top: 1px solid var(--dt-border);
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  text-align: left;
}
.breakdown li {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.78rem;
  color: var(--dt-neutral-text);
}
.breakdown li strong {
  margin-left: auto;
  color: var(--dt-text-strong);
}
.breakdown__dot {
  width: 9px;
  height: 9px;
  border-radius: 9999px;
  flex-shrink: 0;
}
.breakdown__dot--ok {
  background: var(--dt-ok-solid);
}
.breakdown__dot--warn {
  background: var(--dt-warn-solid);
}
.breakdown__dot--action {
  background: var(--dt-action-solid);
}
.breakdown__dot--critical {
  background: var(--dt-critical-solid);
}
.quality__history-toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  width: 100%;
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 0;
  color: var(--dt-text-strong);
}
.quality__history-title {
  font-size: 0.95rem;
  font-weight: 800;
}
.quality__history-chevron {
  font-size: 0.8rem;
  color: var(--dt-neutral-text);
  transition: transform var(--dt-motion-fast) var(--dt-ease);
}
.quality__history-chevron.is-open {
  transform: rotate(90deg);
}
.quality__history-latest {
  margin: 0.4rem 0 0;
  font-size: 0.72rem;
  color: var(--dt-neutral-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.quality__history .dt-feed {
  margin-top: 0.5rem;
}
</style>
