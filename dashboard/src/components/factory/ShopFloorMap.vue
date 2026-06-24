<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  useOperationsStore,
  unitStatusTone,
  unitStatusLabel,
  supportState,
  routeView,
  type Unit,
} from '@/stores/operations'

const ops = useOperationsStore()
const emit = defineEmits<{ (e: 'select', id: string): void }>()

// Filtros: linha visível + destaque por tipo de problema.
const lineFilter = ref<string>('all')
type Highlight = 'all' | 'quality' | 'support' | 'offroute'
const highlight = ref<Highlight>('all')

const lines = computed(() =>
  lineFilter.value === 'all' ? ops.lines : ops.lines.filter((l) => l.id === lineFilter.value),
)

const highlights: Array<{ key: Highlight; label: string }> = [
  { key: 'all', label: 'Tudo' },
  { key: 'quality', label: 'Problemas de qualidade' },
  { key: 'support', label: 'Sem suporte' },
  { key: 'offroute', label: 'Fora da rota' },
]

function unitsIn(sectionId: string): Unit[] {
  return ops.units.filter((u) => u.sectionId === sectionId && u.state !== 'completed' && u.state !== 'scrap')
}
function doneIn(sectionId: string): Unit[] {
  return ops.units.filter((u) => u.sectionId === sectionId && (u.state === 'completed' || u.state === 'scrap'))
}
function matches(u: Unit): boolean {
  switch (highlight.value) {
    case 'quality':
      return ['pending', 'recoverable', 'reconditioning', 'scrap'].includes(u.quality)
    case 'support':
      return u.state !== 'completed' && supportState(u) !== 'assigned'
    case 'offroute':
      return routeView(u).offRoute
    default:
      return true
  }
}
function supDot(u: Unit): string {
  const s = supportState(u)
  return s === 'assigned' ? 'ok' : s === 'missing' ? 'critical' : 'neutral'
}
function capInfo(sectionId: string, capacity: number) {
  const occ = unitsIn(sectionId).length
  return { occ, cap: capacity, unlimited: capacity >= 99, full: capacity < 99 && occ >= capacity }
}
function pick(u: Unit) {
  ops.selectUnit(u.id)
  emit('select', u.id)
}
</script>

<template>
  <div class="sf">
    <div class="sf__controls">
      <div class="sf__filter">
        <button
          type="button"
          class="chip"
          :class="{ 'chip--on': lineFilter === 'all' }"
          @click="lineFilter = 'all'"
        >
          Todas as linhas
        </button>
        <button
          v-for="l in ops.lines"
          :key="l.id"
          type="button"
          class="chip"
          :class="{ 'chip--on': lineFilter === l.id }"
          @click="lineFilter = l.id"
        >
          {{ l.code }}
        </button>
      </div>
      <div class="sf__filter">
        <button
          v-for="h in highlights"
          :key="h.key"
          type="button"
          class="chip chip--ghost"
          :class="{ 'chip--on': highlight === h.key }"
          @click="highlight = h.key"
        >
          {{ h.label }}
        </button>
      </div>
    </div>

    <div class="sf__lanes">
      <section
        v-for="l in lines"
        :key="l.id"
        class="lane"
      >
        <header class="lane__head">
          <span class="lane__code">{{ l.code }}</span>
          <span class="lane__name">{{ l.name }}</span>
        </header>
        <div class="lane__track">
          <template
            v-for="(s, i) in l.sections"
            :key="s.id"
          >
            <div
              class="sec"
              :class="{ 'sec--full': capInfo(s.id, s.capacity).full }"
            >
              <div class="sec__head">
                <span class="sec__name">{{ s.name }}</span>
                <span
                  class="sec__cap"
                  :class="{
                    'sec__cap--full': capInfo(s.id, s.capacity).full,
                    'sec__cap--free': capInfo(s.id, s.capacity).unlimited,
                  }"
                >
                  <template v-if="capInfo(s.id, s.capacity).unlimited">armazém</template>
                  <template v-else>{{ capInfo(s.id, s.capacity).occ }}/{{ s.capacity }}</template>
                </span>
              </div>
              <div class="sec__units">
                <button
                  v-for="u in unitsIn(s.id)"
                  :key="u.id"
                  type="button"
                  class="uchip"
                  :class="[
                    `uchip--${unitStatusTone(u)}`,
                    { 'uchip--sel': u.id === ops.selectedUnitId, 'uchip--dim': !matches(u) },
                  ]"
                  :title="`${u.label} · ${u.product} · ${unitStatusLabel(u)}`"
                  @click="pick(u)"
                >
                  <span
                    class="uchip__sup"
                    :class="`uchip__sup--${supDot(u)}`"
                    aria-hidden="true"
                  />
                  <span class="uchip__lbl">{{ u.label }}</span>
                  <span
                    v-if="routeView(u).offRoute"
                    class="uchip__off"
                    aria-hidden="true"
                  >!</span>
                </button>
                <span
                  v-for="u in doneIn(s.id)"
                  :key="u.id"
                  class="uchip uchip--done"
                  :title="`${u.label} · ${unitStatusLabel(u)}`"
                >{{ u.label }}</span>
                <span
                  v-if="!unitsIn(s.id).length && !doneIn(s.id).length"
                  class="sec__empty"
                >-</span>
              </div>
            </div>
            <span
              v-if="i < l.sections.length - 1"
              class="lane__arrow"
              aria-hidden="true"
            >→</span>
          </template>
        </div>
      </section>
    </div>

    <div class="sf__legend">
      <span class="legend__item"><span class="legend__dot legend__dot--info" />Em produção</span>
      <span class="legend__item"><span class="legend__dot legend__dot--warn" />Em análise</span>
      <span class="legend__item"><span class="legend__dot legend__dot--action" />Recondicionamento</span>
      <span class="legend__item"><span class="legend__dot legend__dot--ok" />Aprovado/Concluído</span>
      <span class="legend__item"><span class="legend__dot legend__dot--critical" />Sucata</span>
      <span class="legend__sep" />
      <span class="legend__item"><span class="legend__sup legend__sup--ok" />Suporte ativo</span>
      <span class="legend__item"><span class="legend__sup legend__sup--neutral" />Planeada</span>
      <span class="legend__item"><span class="legend__sup legend__sup--critical" />Sem suporte</span>
      <span class="legend__item"><span class="legend__off">!</span>Fora da rota</span>
    </div>
  </div>
</template>

<style scoped>
.sf {
  display: flex;
  flex-direction: column;
  gap: 0.8rem;
}
.sf__controls {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.sf__filter {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
}
.chip {
  border: 1px solid var(--dt-border);
  background: var(--dt-surface);
  color: var(--dt-neutral-text);
  border-radius: 9999px;
  padding: 0.25rem 0.6rem;
  font-size: 0.7rem;
  font-weight: 800;
  cursor: pointer;
}
.chip--ghost {
  background: transparent;
}
.chip--on {
  background: var(--dt-brand-500);
  color: #fff;
  border-color: var(--dt-brand-500);
}
.sf__lanes {
  display: flex;
  flex-direction: column;
  gap: 0.7rem;
}
.lane {
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  background: var(--dt-surface);
  padding: 0.6rem;
}
.lane__head {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
  min-width: 0;
  flex-wrap: wrap;
}
.lane__code {
  font-size: 0.8rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.lane__name {
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
  min-width: 0;
  overflow-wrap: break-word;
}
.lane__track {
  display: flex;
  align-items: stretch;
  gap: 0.2rem;
  overflow-x: auto;
  padding-bottom: 0.3rem;
}
.lane__arrow {
  align-self: center;
  color: var(--dt-neutral-text);
  font-weight: 900;
  flex-shrink: 0;
}
.sec {
  flex: 0 0 auto;
  width: 150px;
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius);
  background: var(--dt-surface-2);
  padding: 0.45rem;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.sec--full {
  border-color: var(--dt-critical-border);
}
.sec__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.3rem;
  min-width: 0;
}
.sec__name {
  font-size: 0.66rem;
  font-weight: 800;
  color: var(--dt-text-strong);
  line-height: 1.1;
  min-width: 0;
  overflow-wrap: break-word;
}
.sec__cap {
  font-size: 0.6rem;
  font-weight: 800;
  white-space: nowrap;
  flex-shrink: 0;
  padding: 0.02rem 0.34rem;
  border-radius: 9999px;
  color: var(--dt-info-text);
  background: var(--dt-info-surface);
  border: 1px solid var(--dt-info-border);
}
.sec__cap--full {
  color: var(--dt-critical-text);
  background: var(--dt-critical-surface);
  border-color: var(--dt-critical-border);
}
.sec__cap--free {
  color: var(--dt-neutral-text);
  background: var(--dt-neutral-surface);
  border-color: var(--dt-neutral-border);
}
.sec__units {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.sec__empty {
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
  opacity: 0.6;
}
.uchip {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  min-width: 0;
  max-width: 100%;
  padding: 0.18rem 0.4rem;
  border-radius: 6px;
  border: 1px solid var(--tone-border);
  background: var(--tone-surface);
  color: var(--tone-text);
  font-size: 0.64rem;
  font-weight: 800;
  cursor: pointer;
  text-align: left;
}
.uchip--sel {
  box-shadow: var(--dt-focus-ring);
}
.uchip--dim {
  opacity: 0.28;
}
.uchip__sup {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 9999px;
  flex-shrink: 0;
}
.uchip__sup--ok {
  background: var(--dt-ok-solid);
}
.uchip__sup--neutral {
  background: var(--dt-neutral-solid);
}
.uchip__sup--critical {
  background: var(--dt-critical-solid);
}
.uchip__lbl {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  min-width: 0;
}
.uchip__off {
  margin-left: auto;
  color: var(--dt-critical-text);
  font-weight: 900;
}
.uchip--done {
  cursor: default;
  --tone-surface: var(--dt-ok-surface);
  --tone-border: var(--dt-ok-border);
  --tone-text: var(--dt-ok-text);
  opacity: 0.7;
}
.uchip--ok {
  --tone-surface: var(--dt-ok-surface);
  --tone-border: var(--dt-ok-border);
  --tone-text: var(--dt-ok-text);
}
.uchip--info {
  --tone-surface: var(--dt-info-surface);
  --tone-border: var(--dt-info-border);
  --tone-text: var(--dt-info-text);
}
.uchip--warn {
  --tone-surface: var(--dt-warn-surface);
  --tone-border: var(--dt-warn-border);
  --tone-text: var(--dt-warn-text);
}
.uchip--action {
  --tone-surface: var(--dt-action-surface);
  --tone-border: var(--dt-action-border);
  --tone-text: var(--dt-action-text);
}
.uchip--critical {
  --tone-surface: var(--dt-critical-surface);
  --tone-border: var(--dt-critical-border);
  --tone-text: var(--dt-critical-text);
}
.uchip--neutral {
  --tone-surface: var(--dt-neutral-surface);
  --tone-border: var(--dt-neutral-border);
  --tone-text: var(--dt-neutral-text);
}
.sf__legend {
  display: flex;
  flex-wrap: wrap;
  gap: 0.6rem;
  align-items: center;
  padding: 0.5rem 0.2rem 0;
  border-top: 1px solid var(--dt-border);
}
.legend__item {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.64rem;
  color: var(--dt-neutral-text);
  font-weight: 700;
}
.legend__dot {
  width: 0.6rem;
  height: 0.6rem;
  border-radius: 3px;
}
.legend__dot--info {
  background: var(--dt-info-solid);
}
.legend__dot--warn {
  background: var(--dt-warn-solid);
}
.legend__dot--action {
  background: var(--dt-action-solid);
}
.legend__dot--ok {
  background: var(--dt-ok-solid);
}
.legend__dot--critical {
  background: var(--dt-critical-solid);
}
.legend__sup {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 9999px;
}
.legend__sup--ok {
  background: var(--dt-ok-solid);
}
.legend__sup--neutral {
  background: var(--dt-neutral-solid);
}
.legend__sup--critical {
  background: var(--dt-critical-solid);
}
.legend__off {
  color: var(--dt-critical-text);
  font-weight: 900;
  font-size: 0.7rem;
}
.legend__sep {
  width: 1px;
  height: 0.9rem;
  background: var(--dt-border);
}
</style>
