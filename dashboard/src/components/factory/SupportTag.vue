<script setup lang="ts">
import { computed } from 'vue'
import { useOperationsStore, supportState, type Unit } from '@/stores/operations'

const props = defineProps<{ unit: Unit; compact?: boolean }>()
const ops = useOperationsStore()
const st = computed(() => supportState(props.unit))
const code = computed(() => ops.supportById(props.unit.supportId)?.code ?? null)
</script>

<template>
  <span
    class="sup"
    :class="`sup--${st}`"
    :title="st === 'missing' ? 'Problema de rastreabilidade: sem suporte ativo' : st === 'assigned' ? `Suporte ${code}` : 'Unidade planeada, ainda sem suporte'"
  >
    <span
      class="sup__icon"
      aria-hidden="true"
    >{{ st === 'assigned' ? '◉' : st === 'missing' ? '⚠' : '○' }}</span>
    <span v-if="st === 'assigned'">{{ code }}</span>
    <span v-else-if="st === 'missing'">Sem suporte</span>
    <span v-else>{{ compact ? 'Planeada' : 'Aguarda suporte' }}</span>
  </span>
</template>

<style scoped>
.sup {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  font-size: 0.62rem;
  font-weight: 800;
  padding: 0.06rem 0.42rem;
  border-radius: 9999px;
  border: 1px solid transparent;
  white-space: nowrap;
}
.sup__icon {
  font-size: 0.62rem;
}
.sup--assigned {
  color: var(--dt-ok-text);
  background: var(--dt-ok-surface);
  border-color: var(--dt-ok-border);
}
.sup--missing {
  color: var(--dt-critical-text);
  background: var(--dt-critical-surface);
  border-color: var(--dt-critical-border);
}
.sup--planned {
  color: var(--dt-neutral-text);
  background: var(--dt-neutral-surface);
  border-color: var(--dt-neutral-border);
}
</style>
