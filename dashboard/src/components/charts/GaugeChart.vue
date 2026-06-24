<script setup lang="ts">
import { computed } from 'vue'
import { gaugeArc } from '@/utils/charts'

const props = withDefaults(
  defineProps<{
    /** Fração 0–1. */
    value: number
    label?: string
    /** Limiares de cor (abaixo de warn = crítico, acima de ok = bom). */
    warnBelow?: number
    okAbove?: number
  }>(),
  { warnBelow: 0.85, okAbove: 0.95 },
)

const track = gaugeArc(1)
const valueArc = computed(() => gaugeArc(props.value))
const color = computed(() => {
  if (props.value >= props.okAbove) return 'var(--dt-ok-solid)'
  if (props.value >= props.warnBelow) return 'var(--dt-warn-solid)'
  return 'var(--dt-critical-solid)'
})
const percent = computed(() => `${Math.round(props.value * 100)}%`)
</script>

<template>
  <div class="dt-gauge">
    <svg
      viewBox="0 0 160 100"
      class="dt-gauge__svg"
      role="img"
      :aria-label="`${label ?? 'Indicador'}: ${percent}`"
    >
      <path
        :d="track"
        fill="none"
        stroke="var(--dt-neutral-border)"
        stroke-width="14"
        stroke-linecap="round"
      />
      <path
        :d="valueArc"
        fill="none"
        :stroke="color"
        stroke-width="14"
        stroke-linecap="round"
      />
      <text
        x="80"
        y="78"
        text-anchor="middle"
        class="dt-gauge__value"
      >{{ percent }}</text>
    </svg>
    <p
      v-if="label"
      class="dt-gauge__label"
    >
      {{ label }}
    </p>
  </div>
</template>

<style scoped>
.dt-gauge {
  display: flex;
  flex-direction: column;
  align-items: center;
}
.dt-gauge__svg {
  width: 150px;
  height: 94px;
}
.dt-gauge__value {
  font-size: 24px;
  font-weight: 900;
  fill: var(--dt-text-strong);
}
:global(.dark) .dt-gauge__value {
  fill: #fff;
}
.dt-gauge__label {
  margin: 0.1rem 0 0;
  font-size: 0.6875rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: var(--dt-neutral-text);
}
</style>
