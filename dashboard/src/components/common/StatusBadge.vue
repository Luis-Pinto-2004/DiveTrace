<script setup lang="ts">
import { computed } from 'vue'
import { statusTone, type StatusTone } from '@/utils/status'

const props = withDefaults(
  defineProps<{
    /** Texto a apresentar. */
    label: string
    /** Estado de domínio (deriva o tom). Ignorado se `tone` for passado. */
    status?: string
    /** Tom semântico explícito. */
    tone?: StatusTone
    /** Tamanho do badge. */
    size?: 'sm' | 'md'
  }>(),
  { size: 'md' },
)

const resolvedTone = computed<StatusTone>(() => props.tone ?? statusTone(props.status ?? props.label))
</script>

<template>
  <span
    class="dt-badge"
    :class="[`dt-badge--${resolvedTone}`, `dt-badge--${size}`]"
    role="status"
  >
    <span
      class="dt-badge__dot"
      aria-hidden="true"
    />
    <span class="dt-badge__label">{{ label }}</span>
  </span>
</template>

<style scoped>
.dt-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  border-radius: 9999px;
  border: 1px solid var(--tone-border);
  background: var(--tone-surface);
  color: var(--tone-text);
  font-weight: 700;
  line-height: 1;
  white-space: nowrap;
  flex-shrink: 0;
  max-width: 100%;
}
.dt-badge__label {
  overflow: hidden;
  text-overflow: ellipsis;
}
.dt-badge--sm {
  padding: 0.125rem 0.5rem;
  font-size: 0.6875rem;
}
.dt-badge--md {
  padding: 0.25rem 0.625rem;
  font-size: 0.75rem;
}
.dt-badge__dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 9999px;
  background: var(--tone-solid);
  flex-shrink: 0;
}
.dt-badge--ok {
  --tone-surface: var(--dt-ok-surface);
  --tone-border: var(--dt-ok-border);
  --tone-text: var(--dt-ok-text);
  --tone-solid: var(--dt-ok-solid);
}
.dt-badge--info {
  --tone-surface: var(--dt-info-surface);
  --tone-border: var(--dt-info-border);
  --tone-text: var(--dt-info-text);
  --tone-solid: var(--dt-info-solid);
}
.dt-badge--warn {
  --tone-surface: var(--dt-warn-surface);
  --tone-border: var(--dt-warn-border);
  --tone-text: var(--dt-warn-text);
  --tone-solid: var(--dt-warn-solid);
}
.dt-badge--action {
  --tone-surface: var(--dt-action-surface);
  --tone-border: var(--dt-action-border);
  --tone-text: var(--dt-action-text);
  --tone-solid: var(--dt-action-solid);
}
.dt-badge--critical {
  --tone-surface: var(--dt-critical-surface);
  --tone-border: var(--dt-critical-border);
  --tone-text: var(--dt-critical-text);
  --tone-solid: var(--dt-critical-solid);
}
.dt-badge--neutral {
  --tone-surface: var(--dt-neutral-surface);
  --tone-border: var(--dt-neutral-border);
  --tone-text: var(--dt-neutral-text);
  --tone-solid: var(--dt-neutral-solid);
}
</style>
