<script setup lang="ts">
import { computed } from 'vue'
import { areaPath, linePath } from '@/utils/charts'

const props = withDefaults(
  defineProps<{
    values: number[]
    labels?: string[]
    height?: number
    tone?: 'info' | 'ok' | 'action'
  }>(),
  { height: 160, tone: 'info' },
)

const W = 520
const box = computed(() => ({ width: W, height: props.height, padding: 18 }))
const max = computed(() => Math.max(...props.values, 1))
const area = computed(() => areaPath(props.values, box.value, max.value))
const line = computed(() => linePath(props.values, box.value, max.value))
const colorVar = computed(() =>
  props.tone === 'ok' ? 'var(--dt-ok-solid)' : props.tone === 'action' ? 'var(--dt-action-solid)' : 'var(--dt-info-solid)',
)
const gridLines = computed(() => {
  const lines = []
  for (let i = 1; i <= 3; i += 1) {
    const y = box.value.padding + ((box.value.height - 2 * box.value.padding) * i) / 4
    lines.push(y)
  }
  return lines
})
const tickLabels = computed(() => {
  if (!props.labels?.length) return []
  const step = Math.ceil(props.labels.length / 6)
  return props.labels
    .map((label, i) => ({ label, i }))
    .filter((entry) => entry.i % step === 0)
})
const xFor = (i: number) =>
  box.value.padding + (i / Math.max(props.values.length - 1, 1)) * (W - 2 * box.value.padding)
</script>

<template>
  <svg
    :viewBox="`0 0 ${W} ${height}`"
    class="dt-area"
    role="img"
    aria-label="Gráfico de evolução"
  >
    <defs>
      <linearGradient
        :id="`area-${tone}`"
        x1="0"
        y1="0"
        x2="0"
        y2="1"
      >
        <stop
          offset="0%"
          :stop-color="colorVar"
          stop-opacity="0.28"
        />
        <stop
          offset="100%"
          :stop-color="colorVar"
          stop-opacity="0.02"
        />
      </linearGradient>
    </defs>
    <line
      v-for="(y, idx) in gridLines"
      :key="idx"
      :x1="box.padding"
      :x2="W - box.padding"
      :y1="y"
      :y2="y"
      stroke="var(--dt-neutral-border)"
      stroke-dasharray="3 5"
    />
    <path
      :d="area"
      :fill="`url(#area-${tone})`"
    />
    <path
      :d="line"
      fill="none"
      :stroke="colorVar"
      stroke-width="2.5"
      stroke-linejoin="round"
      stroke-linecap="round"
    />
    <circle
      v-if="values.length"
      :cx="xFor(values.length - 1)"
      :cy="box.padding + (box.height - 2 * box.padding) * (1 - (values[values.length - 1] / max))"
      r="4"
      :fill="colorVar"
    />
    <text
      v-for="tick in tickLabels"
      :key="tick.i"
      :x="xFor(tick.i)"
      :y="height - 4"
      text-anchor="middle"
      class="dt-area__label"
    >{{ tick.label }}</text>
  </svg>
</template>

<style scoped>
.dt-area {
  width: 100%;
  height: auto;
  display: block;
}
.dt-area__label {
  font-size: 9px;
  font-weight: 600;
  fill: var(--dt-neutral-text);
}
</style>
