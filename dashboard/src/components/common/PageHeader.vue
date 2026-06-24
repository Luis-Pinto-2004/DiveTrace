<script setup lang="ts">
defineProps<{
  /** Título da página. */
  title: string
  /** Subtítulo: idealmente a decisão/ação que a página apoia. */
  subtitle?: string
  /** Sobre-título / contexto. */
  eyebrow?: string
}>()
</script>

<template>
  <header class="dt-page-header">
    <div class="dt-page-header__text">
      <p
        v-if="eyebrow"
        class="dt-page-header__eyebrow"
      >
        {{ eyebrow }}
      </p>
      <h2 class="dt-page-header__title">
        {{ title }}
      </h2>
      <p
        v-if="subtitle"
        class="dt-page-header__subtitle"
      >
        {{ subtitle }}
      </p>
    </div>
    <div
      v-if="$slots.actions"
      class="dt-page-header__actions"
    >
      <slot name="actions" />
    </div>
  </header>
</template>

<style scoped>
.dt-page-header {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
}
.dt-page-header__text {
  min-width: 0;
}
.dt-page-header__eyebrow {
  margin: 0;
  font-size: 0.6875rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--dt-info-text);
}
.dt-page-header__title {
  margin: 0.15rem 0 0;
  font-size: 1.375rem;
  font-weight: 900;
  letter-spacing: -0.01em;
  color: var(--dt-text-strong);
}
:global(.dark) .dt-page-header__title {
  color: #fff;
}
.dt-page-header__subtitle {
  margin: 0.25rem 0 0;
  font-size: 0.875rem;
  font-weight: 600;
  color: var(--dt-neutral-text);
}
.dt-page-header__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}
</style>
