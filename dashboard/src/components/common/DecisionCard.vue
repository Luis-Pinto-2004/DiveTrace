<script setup lang="ts">
import StatusBadge from './StatusBadge.vue'
import type { StatusTone } from '@/utils/status'

defineProps<{
  /** Título da decisão (ex.: unidade/ordem). */
  title: string
  /** Contexto curto. */
  context?: string
  /** Estado/etiqueta a destacar. */
  statusLabel?: string
  /** Tom do estado. */
  tone?: StatusTone
  /** Carimbo temporal legível. */
  timestamp?: string
}>()
</script>

<template>
  <article class="dt-decision">
    <div class="dt-decision__head">
      <div class="dt-decision__title-wrap">
        <h3 class="dt-decision__title">
          {{ title }}
        </h3>
        <p
          v-if="context"
          class="dt-decision__context"
        >
          {{ context }}
        </p>
      </div>
      <StatusBadge
        v-if="statusLabel"
        :label="statusLabel"
        :tone="tone"
        size="sm"
      />
    </div>
    <div
      v-if="$slots.body"
      class="dt-decision__body"
    >
      <slot name="body" />
    </div>
    <div class="dt-decision__foot">
      <span
        v-if="timestamp"
        class="dt-decision__time"
      >{{ timestamp }}</span>
      <div
        v-if="$slots.actions"
        class="dt-decision__actions"
      >
        <slot name="actions" />
      </div>
    </div>
  </article>
</template>

<style scoped>
.dt-decision {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  border-radius: var(--dt-radius-lg);
  border: 1px solid var(--dt-neutral-border);
  background: var(--dt-surface);
  padding: 1rem;
  box-shadow: 0 8px 22px rgba(15, 23, 42, 0.05);
}
:global(.dark) .dt-decision {
  background: var(--dt-surface);
  border-color: var(--dt-border);
}
.dt-decision__head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 0.75rem;
}
.dt-decision__title-wrap {
  min-width: 0;
  flex: 1 1 auto;
}
.dt-decision__title {
  margin: 0;
  font-size: 0.9375rem;
  font-weight: 800;
  color: var(--dt-text-strong);
  overflow-wrap: break-word;
}
:global(.dark) .dt-decision__title {
  color: #f1f5f9;
}
.dt-decision__context {
  margin: 0.15rem 0 0;
  font-size: 0.75rem;
  color: var(--dt-neutral-text);
  overflow-wrap: break-word;
}
.dt-decision__foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.dt-decision__time {
  font-size: 0.6875rem;
  color: var(--dt-neutral-text);
}
.dt-decision__actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}
</style>
