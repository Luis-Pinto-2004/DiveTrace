<script setup lang="ts">
withDefaults(
  defineProps<{
    /** Número de linhas-esqueleto. */
    rows?: number
    /** Mensagem anunciada a leitores de ecrã. */
    label?: string
  }>(),
  { rows: 3, label: 'A carregar…' },
)
</script>

<template>
  <div
    class="dt-skeleton"
    role="status"
    aria-live="polite"
    :aria-label="label"
  >
    <span class="sr-only">{{ label }}</span>
    <div
      v-for="row in rows"
      :key="row"
      class="dt-skeleton__row"
      aria-hidden="true"
    >
      <div class="dt-skeleton__bar dt-skeleton__bar--lead" />
      <div class="dt-skeleton__bar" />
    </div>
  </div>
</template>

<style scoped>
.dt-skeleton {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0.5rem 0;
}
.dt-skeleton__row {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.dt-skeleton__bar {
  height: 0.75rem;
  border-radius: 9999px;
  background: linear-gradient(
    90deg,
    var(--dt-neutral-surface) 25%,
    var(--dt-neutral-border) 37%,
    var(--dt-neutral-surface) 63%
  );
  background-size: 400% 100%;
  animation: dt-shimmer 1.4s ease infinite;
}
.dt-skeleton__bar--lead {
  width: 45%;
  height: 0.9rem;
}
@keyframes dt-shimmer {
  0% {
    background-position: 100% 0;
  }
  100% {
    background-position: 0 0;
  }
}
.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
</style>
