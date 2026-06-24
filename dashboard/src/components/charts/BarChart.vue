<script setup lang="ts">
import { computed } from 'vue'
import type { StatusTone } from '@/utils/status'

export interface BarItem {
  label: string
  value: number
  tone?: StatusTone
  caption?: string
}

const props = withDefaults(
  defineProps<{
    items: BarItem[]
    max?: number
    unit?: string
  }>(),
  {},
)

const maxValue = computed(() => props.max ?? Math.max(...props.items.map((i) => i.value), 1))
const toneColor = (tone?: StatusTone) => {
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
  <div class="dt-bars">
    <div
      v-for="item in items"
      :key="item.label"
      class="dt-bars__row"
    >
      <div class="dt-bars__head">
        <span class="dt-bars__label">{{ item.label }}</span>
        <span class="dt-bars__value">{{ item.value }}<template v-if="unit"> {{ unit }}</template></span>
      </div>
      <div class="dt-bars__track">
        <div
          class="dt-bars__fill"
          :style="{ width: `${Math.min(100, (item.value / maxValue) * 100)}%`, background: toneColor(item.tone) }"
        />
      </div>
      <span
        v-if="item.caption"
        class="dt-bars__caption"
      >{{ item.caption }}</span>
    </div>
  </div>
</template>

<style scoped>
.dt-bars {
  display: flex;
  flex-direction: column;
  gap: 0.7rem;
}
.dt-bars__head {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  margin-bottom: 0.25rem;
}
.dt-bars__label {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--dt-text-strong);
}
:global(.dark) .dt-bars__label {
  color: #e2e8f0;
}
.dt-bars__value {
  font-size: 0.75rem;
  font-weight: 800;
  color: var(--dt-neutral-text);
}
.dt-bars__track {
  height: 10px;
  border-radius: 9999px;
  background: var(--dt-neutral-surface);
  overflow: hidden;
}
.dt-bars__fill {
  height: 100%;
  border-radius: 9999px;
  transition: width var(--dt-motion-slow) var(--dt-ease);
}
.dt-bars__caption {
  font-size: 0.6875rem;
  color: var(--dt-neutral-text);
}
</style>
