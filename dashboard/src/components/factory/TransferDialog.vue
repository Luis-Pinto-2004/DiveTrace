<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useOperationsStore, type Unit } from '@/stores/operations'

const props = defineProps<{ open: boolean; unit: Unit | null }>()
const emit = defineEmits<{ (e: 'close'): void }>()

const ops = useOperationsStore()
const targetLineId = ref('')
const targetSectionId = ref('')
const reason = ref('')

watch(
  () => props.unit,
  (unit) => {
    if (unit) {
      targetLineId.value = unit.lineId
      targetSectionId.value = unit.sectionId
      reason.value = ''
    }
  },
  { immediate: true },
)

const targetLine = computed(() => ops.lineById(targetLineId.value))
const sections = computed(() => targetLine.value?.sections ?? [])
const isInterline = computed(() => props.unit && targetLineId.value !== props.unit.lineId)
// Desvio interlinha exige motivo explícito.
const canConfirm = computed(() => !isInterline.value || reason.value.trim().length > 0)

watch(targetLineId, () => {
  const first = sections.value[0]
  if (first && !sections.value.some((s) => s.id === targetSectionId.value)) {
    targetSectionId.value = first.id
  }
})

function confirm() {
  if (!props.unit || !canConfirm.value) return
  if (isInterline.value) {
    ops.transferUnitToLine(props.unit.id, targetLineId.value, targetSectionId.value, reason.value.trim())
  } else {
    ops.transferUnit(props.unit.id, targetSectionId.value, reason.value.trim() || undefined)
  }
  emit('close')
}
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open && unit"
      class="dt-tr__overlay"
      @click.self="emit('close')"
    >
      <div
        class="dt-tr"
        role="dialog"
        aria-modal="true"
        aria-label="Transferir unidade"
      >
        <h2 class="dt-tr__title">
          Transferir {{ unit.label }}
        </h2>
        <p class="dt-tr__sub">
          {{ unit.product }}
        </p>

        <label class="dt-tr__field">
          <span>Linha de destino</span>
          <select v-model="targetLineId">
            <option
              v-for="line in ops.lines"
              :key="line.id"
              :value="line.id"
            >
              {{ line.code }} · {{ line.name }}
            </option>
          </select>
        </label>

        <label class="dt-tr__field">
          <span>Secção de destino</span>
          <select v-model="targetSectionId">
            <option
              v-for="sec in sections"
              :key="sec.id"
              :value="sec.id"
            >{{ sec.name }}</option>
          </select>
        </label>

        <label
          v-if="isInterline"
          class="dt-tr__field"
        >
          <span>Motivo do desvio (obrigatório)</span>
          <input
            v-model="reason"
            type="text"
            placeholder="Ex.: balanceamento de carga, indisponibilidade de equipamento…"
          >
        </label>

        <p
          class="dt-tr__hint"
          :class="{ 'dt-tr__hint--interline': isInterline }"
        >
          {{ isInterline ? 'Desvio interlinha. Fica registado origem, destino e motivo.' : 'Transferência dentro da linha' }}
        </p>

        <div class="dt-tr__actions">
          <button
            type="button"
            class="dt-tr__btn dt-tr__btn--ghost"
            @click="emit('close')"
          >
            Cancelar
          </button>
          <button
            type="button"
            class="dt-tr__btn dt-tr__btn--primary"
            :disabled="!canConfirm"
            @click="confirm"
          >
            {{ isInterline ? 'Registar desvio' : 'Confirmar transferência' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.dt-tr__overlay {
  position: fixed;
  inset: 0;
  z-index: 60;
  display: grid;
  place-items: center;
  padding: 1rem;
  background: rgba(2, 6, 23, 0.55);
  backdrop-filter: blur(2px);
}
.dt-tr {
  width: min(26rem, 100%);
  background: var(--dt-surface);
  border-radius: var(--dt-radius-lg);
  padding: 1.5rem;
  box-shadow: var(--dt-shadow-pop);
}
:global(.dark) .dt-tr {
  background: var(--dt-surface);
  color: #e2e8f0;
}
.dt-tr__title {
  margin: 0;
  font-size: 1.0625rem;
  font-weight: 800;
}
.dt-tr__sub {
  margin: 0.15rem 0 1rem;
  font-size: 0.8125rem;
  color: var(--dt-neutral-text);
}
.dt-tr__field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin-bottom: 0.8rem;
}
.dt-tr__field span {
  font-size: 0.75rem;
  font-weight: 700;
}
.dt-tr__field select,
.dt-tr__field input {
  border: 1px solid var(--dt-neutral-border);
  border-radius: var(--dt-radius);
  padding: 0.5rem;
  background: var(--dt-surface);
  font-size: 0.8125rem;
}
:global(.dark) .dt-tr__field select,
:global(.dark) .dt-tr__field input {
  background: var(--dt-surface-2);
  color: #e2e8f0;
}
.dt-tr__hint {
  font-size: 0.6875rem;
  font-weight: 800;
  color: var(--dt-info-text);
  margin: 0 0 1rem;
}
.dt-tr__hint--interline {
  color: var(--dt-action-text);
}
.dt-tr__actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.5rem;
}
.dt-tr__btn {
  border-radius: var(--dt-radius);
  padding: 0.5rem 0.9rem;
  font-size: 0.8125rem;
  font-weight: 800;
  cursor: pointer;
  border: 1px solid transparent;
}
.dt-tr__btn--ghost {
  background: transparent;
  border-color: var(--dt-neutral-border);
  color: var(--dt-neutral-text);
}
.dt-tr__btn--primary {
  background: var(--dt-brand-500, #0877d8);
  color: #fff;
}
.dt-tr__btn--primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
