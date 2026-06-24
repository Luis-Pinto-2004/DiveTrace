<script setup lang="ts">
import { computed } from 'vue'
import { donutArcs } from '@/utils/charts'
import type { StatusTone } from '@/utils/status'

export interface DonutSlice {
  label: string
  value: number
  tone: StatusTone
}

const props = defineProps<{
  slices: DonutSlice[]
  centerLabel?: string
  centerValue?: string | number
}>()

const arcs = computed(() => donutArcs(props.slices.map((s) => s.value)))
const total = computed(() => props.slices.reduce((sum, s) => sum + s.value, 0))
const toneColor = (tone: StatusTone) => {
  switch (tone) {
    case 'ok':
      return 'var(--dt-ok-solid)'
    case 'warn':
      return 'var(--dt-warn-solid)'
    case 'action':
      return 'var(--dt-action-solid)'
    case 'critical':
      return 'var(--dt-critical-solid)'
    case 'neutral':
      return 'var(--dt-neutral-solid)'
    default:
      return 'var(--dt-info-solid)'
  }
}
</script>

<template>
  <div class="dt-donut">
    <svg
      viewBox="0 0 160 160"
      class="dt-donut__svg"
      role="img"
      aria-label="Distribuição por estado"
    >
      <path
        v-for="(arc, idx) in arcs"
        :key="idx"
        :d="arc.path"
        :fill="toneColor(slices[idx].tone)"
      />
      <text
        x="80"
        y="74"
        text-anchor="middle"
        class="dt-donut__value"
      >{{ centerValue ?? total }}</text>
      <text
        x="80"
        y="92"
        text-anchor="middle"
        class="dt-donut__label"
      >{{ centerLabel ?? 'unidades' }}</text>
    </svg>
    <ul class="dt-donut__legend">
      <li
        v-for="slice in slices"
        :key="slice.label"
      >
        <span
          class="dt-donut__dot"
          :style="{ background: toneColor(slice.tone) }"
        />
        <span class="dt-donut__legend-label">{{ slice.label }}</span>
        <span class="dt-donut__legend-value">{{ slice.value }}</span>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.dt-donut {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}
.dt-donut__svg {
  width: 150px;
  height: 150px;
  flex-shrink: 0;
}
.dt-donut__value {
  font-size: 26px;
  font-weight: 900;
  fill: var(--dt-text-strong);
}
:global(.dark) .dt-donut__value {
  fill: #fff;
}
.dt-donut__label {
  font-size: 10px;
  font-weight: 700;
  fill: var(--dt-neutral-text);
}
.dt-donut__legend {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  min-width: 0;
  flex: 1;
}
.dt-donut__legend li {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.75rem;
}
.dt-donut__dot {
  width: 0.7rem;
  height: 0.7rem;
  border-radius: 3px;
  flex-shrink: 0;
}
.dt-donut__legend-label {
  color: var(--dt-neutral-text);
  flex: 1;
}
.dt-donut__legend-value {
  font-weight: 800;
  color: var(--dt-text-strong);
}
:global(.dark) .dt-donut__legend-value {
  color: #e2e8f0;
}
</style>
