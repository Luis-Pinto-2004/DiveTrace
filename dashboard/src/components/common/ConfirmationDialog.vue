<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { StatusTone } from '@/utils/status'

const props = withDefaults(
  defineProps<{
    open: boolean
    title: string
    message?: string
    confirmLabel?: string
    cancelLabel?: string
    tone?: StatusTone
  }>(),
  {
    confirmLabel: 'Confirmar',
    cancelLabel: 'Cancelar',
    tone: 'critical',
  },
)

const emit = defineEmits<{ (e: 'confirm'): void; (e: 'cancel'): void }>()

const confirmButton = ref<HTMLButtonElement | null>(null)

function onKeydown(event: KeyboardEvent) {
  if (!props.open) return
  if (event.key === 'Escape') emit('cancel')
}

watch(
  () => props.open,
  async (isOpen) => {
    if (isOpen) {
      await Promise.resolve()
      confirmButton.value?.focus()
    }
  },
)

onMounted(() => document.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => document.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="dt-dialog__overlay"
      @click.self="emit('cancel')"
    >
      <div
        class="dt-dialog"
        role="alertdialog"
        aria-modal="true"
        :aria-label="title"
      >
        <h2 class="dt-dialog__title">
          {{ title }}
        </h2>
        <p
          v-if="message"
          class="dt-dialog__message"
        >
          {{ message }}
        </p>
        <div class="dt-dialog__actions">
          <button
            type="button"
            class="dt-dialog__btn dt-dialog__btn--ghost"
            @click="emit('cancel')"
          >
            {{ cancelLabel }}
          </button>
          <button
            ref="confirmButton"
            type="button"
            class="dt-dialog__btn"
            :class="`dt-dialog__btn--${tone}`"
            @click="emit('confirm')"
          >
            {{ confirmLabel }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.dt-dialog__overlay {
  position: fixed;
  inset: 0;
  z-index: 60;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgba(2, 6, 23, 0.55);
  backdrop-filter: blur(2px);
}
.dt-dialog {
  width: min(28rem, 100%);
  border-radius: var(--dt-radius-lg);
  background: var(--dt-surface);
  padding: 1.5rem;
  box-shadow: var(--dt-shadow-pop);
}
:global(.dark) .dt-dialog {
  background: var(--dt-surface);
  color: #e2e8f0;
}
.dt-dialog__title {
  margin: 0;
  font-size: 1.0625rem;
  font-weight: 800;
}
.dt-dialog__message {
  margin: 0.5rem 0 0;
  font-size: 0.875rem;
  color: var(--dt-neutral-text);
}
.dt-dialog__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
  margin-top: 1.25rem;
}
.dt-dialog__btn {
  border-radius: var(--dt-radius);
  padding: 0.5rem 0.9rem;
  font-size: 0.8125rem;
  font-weight: 800;
  border: 1px solid transparent;
  cursor: pointer;
  color: #fff;
}
.dt-dialog__btn--ghost {
  background: transparent;
  color: var(--dt-neutral-text);
  border-color: var(--dt-neutral-border);
}
.dt-dialog__btn--critical {
  background: var(--dt-critical-solid);
}
.dt-dialog__btn--action {
  background: var(--dt-action-solid);
}
.dt-dialog__btn--info {
  background: var(--dt-info-solid);
}
.dt-dialog__btn--ok {
  background: var(--dt-ok-solid);
}
.dt-dialog__btn--warn {
  background: var(--dt-warn-solid);
  color: #1f2937;
}
.dt-dialog__btn--neutral {
  background: var(--dt-neutral-solid);
}
</style>
