<script setup lang="ts">
import { computed } from 'vue'
import type { StatusTone } from '@/utils/status'

const props = withDefaults(
  defineProps<{
    /** Etiqueta curta da métrica (PT-PT). */
    label: string
    /** Valor principal. */
    value: string | number
    /** Unidade ou sufixo opcional. */
    unit?: string
    /** Texto de apoio (contexto/decisão). */
    hint?: string
    /** Tom semântico do realce lateral. */
    tone?: StatusTone
    /** Direção da tendência, se aplicável. */
    trend?: 'up' | 'down' | 'flat'
    /** Texto da tendência (ex.: "+12% vs. turno anterior"). */
    trendLabel?: string
  }>(),
  { tone: 'info' },
)

const trendSymbol = computed(() =>
  props.trend === 'up' ? '▲' : props.trend === 'down' ? '▼' : props.trend === 'flat' ? '-' : '',
)
</script>

<template>
  <article
    class="dt-kpi"
    :class="`dt-kpi--${tone}`"
  >
    <p class="dt-kpi__label">
      {{ label }}
    </p>
    <p class="dt-kpi__value">
      {{ value }}<span
        v-if="unit"
        class="dt-kpi__unit"
      > {{ unit }}</span>
    </p>
    <p
      v-if="trend && trendLabel"
      class="dt-kpi__trend"
      :class="`dt-kpi__trend--${trend}`"
    >
      <span aria-hidden="true">{{ trendSymbol }}</span> {{ trendLabel }}
    </p>
    <p
      v-if="hint"
      class="dt-kpi__hint"
    >
      {{ hint }}
    </p>
  </article>
</template>

<style scoped>
.dt-kpi {
  position: relative;
  min-width: 0;
  border-radius: var(--dt-radius-lg);
  border: 1px solid var(--dt-neutral-border);
  background: var(--dt-surface);
  padding: 1rem;
  box-shadow: 0 8px 22px rgba(15, 23, 42, 0.05);
  transition: transform var(--dt-motion-base) var(--dt-ease),
    border-color var(--dt-motion-base) var(--dt-ease);
}
.dt-kpi:hover {
  transform: translateY(-2px);
}
:global(.dark) .dt-kpi {
  background: var(--dt-surface);
  border-color: var(--dt-border);
}
.dt-kpi::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0.75rem;
  bottom: 0.75rem;
  width: 3px;
  border-radius: 0 9999px 9999px 0;
  background: var(--accent);
}
.dt-kpi--ok {
  --accent: var(--dt-ok-solid);
}
.dt-kpi--info {
  --accent: var(--dt-info-solid);
}
.dt-kpi--warn {
  --accent: var(--dt-warn-solid);
}
.dt-kpi--action {
  --accent: var(--dt-action-solid);
}
.dt-kpi--critical {
  --accent: var(--dt-critical-solid);
}
.dt-kpi--neutral {
  --accent: var(--dt-neutral-solid);
}
.dt-kpi__label {
  margin: 0 0 0 0.5rem;
  font-size: 0.6875rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.02em;
  color: var(--dt-neutral-text);
}
.dt-kpi__value {
  margin: 0.35rem 0 0 0.5rem;
  font-size: 1.875rem;
  font-weight: 900;
  letter-spacing: -0.01em;
  color: var(--dt-text-strong);
  line-height: 1.05;
}
:global(.dark) .dt-kpi__value {
  color: #fff;
}
.dt-kpi__unit {
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
}
.dt-kpi__trend {
  margin: 0.35rem 0 0 0.5rem;
  font-size: 0.6875rem;
  font-weight: 700;
}
.dt-kpi__trend--up {
  color: var(--dt-ok-text);
}
.dt-kpi__trend--down {
  color: var(--dt-critical-text);
}
.dt-kpi__trend--flat {
  color: var(--dt-neutral-text);
}
.dt-kpi__hint {
  margin: 0.5rem 0 0 0.5rem;
  font-size: 0.75rem;
  color: var(--dt-neutral-text);
}
</style>
