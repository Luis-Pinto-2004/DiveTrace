<script setup lang="ts">
import { computed } from 'vue'
import { unitStatusLabel, unitStatusTone, type Unit } from '@/stores/operations'

const props = defineProps<{ unit: Unit; selected?: boolean }>()
defineEmits<{ (e: 'select', id: string): void }>()

const tone = computed(() => unitStatusTone(props.unit))
const title = computed(() => `${props.unit.label} · ${props.unit.product} · ${unitStatusLabel(props.unit)}`)
const attention = computed(
  () => props.unit.quality === 'pending' || props.unit.quality === 'recoverable',
)
</script>

<template>
  <button
    type="button"
    class="dt-chip"
    :class="[`dt-chip--${tone}`, { 'dt-chip--selected': selected, 'dt-chip--pulse': attention }]"
    :title="title"
    :aria-label="title"
    @click="$emit('select', unit.id)"
  >
    <span
      class="dt-chip__dot"
      aria-hidden="true"
    />
    <span class="dt-chip__label">{{ unit.label }}</span>
  </button>
</template>

<style scoped>
.dt-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  padding: 0.2rem 0.45rem;
  border-radius: 7px;
  border: 1px solid var(--tone-border);
  background: var(--tone-surface);
  color: var(--tone-text);
  font-size: 0.6875rem;
  font-weight: 800;
  cursor: pointer;
  max-width: 100%;
  transition: transform var(--dt-motion-fast) var(--dt-ease), box-shadow var(--dt-motion-fast) var(--dt-ease);
}
.dt-chip__label {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.dt-chip:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 10px rgba(15, 23, 42, 0.12);
}
.dt-chip--selected {
  box-shadow: var(--dt-focus-ring);
}
.dt-chip__dot {
  width: 0.45rem;
  height: 0.45rem;
  border-radius: 9999px;
  background: var(--tone-solid);
  flex-shrink: 0;
}
.dt-chip--pulse .dt-chip__dot {
  animation: dt-pulse 1.4s ease-in-out infinite;
}
@keyframes dt-pulse {
  0%, 100% {
    box-shadow: 0 0 0 0 var(--tone-solid);
    opacity: 1;
  }
  50% {
    box-shadow: 0 0 0 4px transparent;
    opacity: 0.55;
  }
}
.dt-chip--ok {
  --tone-surface: var(--dt-ok-surface);
  --tone-border: var(--dt-ok-border);
  --tone-text: var(--dt-ok-text);
  --tone-solid: var(--dt-ok-solid);
}
.dt-chip--info {
  --tone-surface: var(--dt-info-surface);
  --tone-border: var(--dt-info-border);
  --tone-text: var(--dt-info-text);
  --tone-solid: var(--dt-info-solid);
}
.dt-chip--warn {
  --tone-surface: var(--dt-warn-surface);
  --tone-border: var(--dt-warn-border);
  --tone-text: var(--dt-warn-text);
  --tone-solid: var(--dt-warn-solid);
}
.dt-chip--action {
  --tone-surface: var(--dt-action-surface);
  --tone-border: var(--dt-action-border);
  --tone-text: var(--dt-action-text);
  --tone-solid: var(--dt-action-solid);
}
.dt-chip--critical {
  --tone-surface: var(--dt-critical-surface);
  --tone-border: var(--dt-critical-border);
  --tone-text: var(--dt-critical-text);
  --tone-solid: var(--dt-critical-solid);
}
.dt-chip--neutral {
  --tone-surface: var(--dt-neutral-surface);
  --tone-border: var(--dt-neutral-border);
  --tone-text: var(--dt-neutral-text);
  --tone-solid: var(--dt-neutral-solid);
}
</style>
