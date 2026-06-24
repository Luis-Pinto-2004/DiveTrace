<script setup lang="ts">
import type { StatusTone } from '@/utils/status'

withDefaults(
  defineProps<{
    /** Título do passo. */
    title: string
    /** Detalhe/descrição. */
    detail?: string
    /** Carimbo temporal. */
    timestamp?: string
    /** Tom do marcador. */
    tone?: StatusTone
    /** Último passo da linha (oculta o conector). */
    last?: boolean
  }>(),
  { tone: 'info', last: false },
)
</script>

<template>
  <li
    class="dt-step"
    :class="{ 'dt-step--last': last }"
  >
    <div
      class="dt-step__marker"
      :class="`dt-step__marker--${tone}`"
      aria-hidden="true"
    />
    <div class="dt-step__content">
      <div class="dt-step__head">
        <h4 class="dt-step__title">
          {{ title }}
        </h4>
        <span
          v-if="timestamp"
          class="dt-step__time"
        >{{ timestamp }}</span>
      </div>
      <p
        v-if="detail"
        class="dt-step__detail"
      >
        {{ detail }}
      </p>
    </div>
  </li>
</template>

<style scoped>
.dt-step {
  position: relative;
  display: flex;
  gap: 0.75rem;
  padding-bottom: 1.1rem;
}
.dt-step::before {
  content: '';
  position: absolute;
  left: 0.4375rem;
  top: 1rem;
  bottom: 0;
  width: 2px;
  background: var(--dt-neutral-border);
}
.dt-step--last::before {
  display: none;
}
.dt-step__marker {
  margin-top: 0.2rem;
  width: 0.9rem;
  height: 0.9rem;
  border-radius: 9999px;
  flex-shrink: 0;
  border: 2px solid #fff;
  box-shadow: 0 0 0 1px var(--dt-neutral-border);
}
:global(.dark) .dt-step__marker {
  border-color: var(--dt-surface);
}
.dt-step__marker--ok {
  background: var(--dt-ok-solid);
}
.dt-step__marker--info {
  background: var(--dt-info-solid);
}
.dt-step__marker--warn {
  background: var(--dt-warn-solid);
}
.dt-step__marker--action {
  background: var(--dt-action-solid);
}
.dt-step__marker--critical {
  background: var(--dt-critical-solid);
}
.dt-step__marker--neutral {
  background: var(--dt-neutral-solid);
}
.dt-step__content {
  min-width: 0;
}
.dt-step__head {
  display: flex;
  align-items: baseline;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.dt-step__title {
  margin: 0;
  font-size: 0.8125rem;
  font-weight: 700;
  color: var(--dt-text-strong);
  overflow-wrap: break-word;
}
:global(.dark) .dt-step__title {
  color: #f1f5f9;
}
.dt-step__time {
  font-size: 0.6875rem;
  color: var(--dt-neutral-text);
}
.dt-step__detail {
  margin: 0.15rem 0 0;
  font-size: 0.75rem;
  color: var(--dt-neutral-text);
}
</style>
