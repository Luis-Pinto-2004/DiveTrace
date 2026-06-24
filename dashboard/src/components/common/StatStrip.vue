<script setup lang="ts">
import KpiCard from './KpiCard.vue'
import type { StatusTone } from '@/utils/status'

export interface StatStripItem {
  label: string
  value: string | number
  unit?: string
  hint?: string
  tone?: StatusTone
  trend?: 'up' | 'down' | 'flat'
  trendLabel?: string
}

defineProps<{ items: StatStripItem[] }>()
</script>

<template>
  <div class="dt-stat-strip">
    <KpiCard
      v-for="(item, index) in items"
      :key="`${item.label}-${index}`"
      :label="item.label"
      :value="item.value"
      :unit="item.unit"
      :hint="item.hint"
      :tone="item.tone"
      :trend="item.trend"
      :trend-label="item.trendLabel"
    />
  </div>
</template>

<style scoped>
.dt-stat-strip {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}
@media (min-width: 768px) {
  .dt-stat-strip {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }
}
</style>
