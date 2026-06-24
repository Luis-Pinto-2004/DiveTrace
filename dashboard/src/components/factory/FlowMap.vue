<script setup lang="ts">
import { computed } from 'vue'
import { useOperationsStore, type Line } from '@/stores/operations'
import UnitChip from './UnitChip.vue'

const props = withDefaults(
  defineProps<{ lineId?: string; selectedUnitId?: string | null }>(),
  { lineId: undefined, selectedUnitId: null },
)
const emit = defineEmits<{ (e: 'select', id: string): void }>()
const ops = useOperationsStore()

const lines = computed<Line[]>(() => (props.lineId ? ops.lines.filter((l) => l.id === props.lineId) : ops.lines))
const maxWip = computed(() => ops.maxSectionWip)

function wip(sectionId: string) {
  return ops.sectionWip(sectionId)
}
function meterClass(count: number): string {
  if (count >= Math.max(3, maxWip.value)) return 'is-critical'
  if (count >= Math.max(2, Math.round(maxWip.value * 0.6))) return 'is-warn'
  return ''
}
</script>

<template>
  <div class="dt-flow">
    <div
      v-for="line in lines"
      :key="line.id"
      class="dt-flow__lane"
    >
      <div class="dt-flow__lane-head">
        <span class="dt-flow__lane-code">{{ line.code }}</span>
        <span
          class="dt-flow__lane-name"
          :title="line.name"
        >{{ line.name }}</span>
      </div>
      <div
        class="dt-flow__sections"
        :style="{ '--cols': line.sections.length }"
      >
        <div
          v-for="(sec, idx) in line.sections"
          :key="sec.id"
          class="dt-flow__section"
          :class="meterClass(wip(sec.id))"
        >
          <div class="dt-flow__section-head">
            <span
              class="dt-flow__section-name"
              :title="sec.name"
            >{{ sec.name }}</span>
            <span class="dt-flow__section-load">{{ wip(sec.id) }}</span>
          </div>
          <div class="dt-flow__meter">
            <div
              class="dt-flow__meter-fill"
              :style="{ width: `${Math.min(100, (wip(sec.id) / maxWip) * 100)}%` }"
            />
          </div>
          <div class="dt-flow__chips">
            <UnitChip
              v-for="unit in ops.unitsInSection(sec.id)"
              :key="unit.id"
              :unit="unit"
              :selected="unit.id === selectedUnitId"
              @select="(id) => emit('select', id)"
            />
            <span
              v-if="!ops.unitsInSection(sec.id).length"
              class="dt-flow__empty"
            >-</span>
          </div>
          <div
            v-if="idx < line.sections.length - 1"
            class="dt-flow__arrow"
            aria-hidden="true"
          >
            ›
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dt-flow {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}
.dt-flow__lane {
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  background: var(--dt-surface-2);
  padding: 0.75rem;
}
.dt-flow__lane-head {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  margin-bottom: 0.6rem;
}
.dt-flow__lane-code {
  font-size: 0.75rem;
  font-weight: 900;
  color: var(--dt-info-text);
}
.dt-flow__lane-name {
  font-size: 0.6875rem;
  color: var(--dt-neutral-text);
  min-width: 0;
  overflow-wrap: break-word;
}
.dt-flow__sections {
  display: flex;
  gap: 0.5rem;
  overflow-x: auto;
  padding-bottom: 0.3rem;
}
.dt-flow__section {
  position: relative;
  flex: 0 0 auto;
  width: 150px;
  background: var(--dt-surface);
  border: 1px solid var(--dt-border);
  border-radius: 10px;
  padding: 0.5rem;
  min-height: 96px;
}
.dt-flow__section.is-warn {
  border-color: var(--dt-warn-border);
  box-shadow: inset 0 0 0 1px var(--dt-warn-border);
}
.dt-flow__section.is-critical {
  border-color: var(--dt-critical-border);
  box-shadow: inset 0 0 0 1px var(--dt-critical-solid);
}
.dt-flow__section-head {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 0.3rem;
  margin-bottom: 0.3rem;
  min-width: 0;
}
.dt-flow__section-name {
  font-size: 0.6875rem;
  font-weight: 800;
  color: var(--dt-text-strong);
  line-height: 1.15;
  min-width: 0;
  overflow-wrap: break-word;
}
.dt-flow__section-load {
  font-size: 0.7rem;
  font-weight: 900;
  color: var(--dt-neutral-text);
  flex-shrink: 0;
}
.dt-flow__meter {
  height: 4px;
  border-radius: 9999px;
  background: var(--dt-neutral-surface);
  overflow: hidden;
  margin-bottom: 0.45rem;
}
.dt-flow__meter-fill {
  height: 100%;
  background: var(--dt-info-solid);
  transition: width var(--dt-motion-slow) var(--dt-ease);
}
.is-warn .dt-flow__meter-fill {
  background: var(--dt-warn-solid);
}
.is-critical .dt-flow__meter-fill {
  background: var(--dt-critical-solid);
}
.dt-flow__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.3rem;
}
.dt-flow__empty {
  font-size: 0.75rem;
  color: var(--dt-neutral-solid);
}
.dt-flow__arrow {
  position: absolute;
  right: -0.55rem;
  top: 50%;
  transform: translateY(-50%);
  font-size: 1.1rem;
  font-weight: 900;
  color: var(--dt-neutral-solid);
  z-index: 1;
}
@media (max-width: 900px) {
  .dt-flow__arrow {
    display: none;
  }
}
</style>
