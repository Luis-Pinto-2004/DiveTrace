<script setup lang="ts">
defineProps<{
  /** Título do estado vazio. */
  title: string
  /** Descrição orientada para a ação. */
  description?: string
  /** Glifo/ícone simples (texto). */
  icon?: string
}>()

defineEmits<{ (e: 'action'): void }>()
</script>

<template>
  <div
    class="dt-empty"
    role="status"
  >
    <div
      v-if="icon"
      class="dt-empty__icon"
      aria-hidden="true"
    >
      {{ icon }}
    </div>
    <h3 class="dt-empty__title">
      {{ title }}
    </h3>
    <p
      v-if="description"
      class="dt-empty__desc"
    >
      {{ description }}
    </p>
    <div
      v-if="$slots.action"
      class="dt-empty__action"
    >
      <slot name="action" />
    </div>
  </div>
</template>

<style scoped>
.dt-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.5rem;
  padding: 2.5rem 1.5rem;
  border: 1px dashed var(--dt-neutral-border);
  border-radius: var(--dt-radius-lg);
  background: var(--dt-neutral-surface);
}
.dt-empty__icon {
  width: 2.75rem;
  height: 2.75rem;
  display: grid;
  place-items: center;
  border-radius: 9999px;
  background: var(--dt-surface);
  border: 1px solid var(--dt-neutral-border);
  font-size: 1.25rem;
}
:global(.dark) .dt-empty__icon {
  background: var(--dt-surface);
}
.dt-empty__title {
  margin: 0.25rem 0 0;
  font-size: 0.95rem;
  font-weight: 800;
  color: var(--dt-text-strong);
}
:global(.dark) .dt-empty__title {
  color: #f1f5f9;
}
.dt-empty__desc {
  margin: 0;
  max-width: 34rem;
  font-size: 0.8125rem;
  color: var(--dt-neutral-text);
}
.dt-empty__action {
  margin-top: 0.5rem;
}
</style>
