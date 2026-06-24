<script setup lang="ts">
import { computed, useId } from 'vue'

const props = defineProps<{
  /** Etiqueta visível do campo. */
  label: string
  /** Texto de ajuda associado ao campo. */
  help?: string
  /** Mensagem de erro (quando presente, marca o campo como inválido). */
  error?: string
  /** Marca o campo como obrigatório. */
  required?: boolean
}>()

const fieldId = useId()
const helpId = computed(() => (props.help ? `${fieldId}-help` : undefined))
const errorId = computed(() => (props.error ? `${fieldId}-error` : undefined))
const describedBy = computed(() =>
  [helpId.value, errorId.value].filter(Boolean).join(' ') || undefined,
)
</script>

<template>
  <div
    class="dt-field"
    :class="{ 'dt-field--invalid': !!error }"
  >
    <label
      :for="fieldId"
      class="dt-field__label"
    >
      {{ label }}
      <span
        v-if="required"
        class="dt-field__required"
        aria-hidden="true"
      >*</span>
    </label>
    <!--
      O componente filho (input/select/textarea) deve ligar-se via slot.
      Passamos id e aria-describedby para o consumidor aplicar no controlo.
    -->
    <slot
      :field-id="fieldId"
      :described-by="describedBy"
      :invalid="!!error"
    />
    <p
      v-if="help"
      :id="helpId"
      class="dt-field__help"
    >
      {{ help }}
    </p>
    <p
      v-if="error"
      :id="errorId"
      class="dt-field__error"
      role="alert"
    >
      {{ error }}
    </p>
  </div>
</template>

<style scoped>
.dt-field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}
.dt-field__label {
  font-size: 0.75rem;
  font-weight: 700;
  color: #1f2937;
}
:global(.dark) .dt-field__label {
  color: #e2e8f0;
}
.dt-field__required {
  color: var(--dt-critical-text);
}
.dt-field__help {
  margin: 0;
  font-size: 0.6875rem;
  color: var(--dt-neutral-text);
}
.dt-field__error {
  margin: 0;
  font-size: 0.6875rem;
  font-weight: 700;
  color: var(--dt-critical-text);
}
.dt-field--invalid :slotted(input),
.dt-field--invalid :slotted(select),
.dt-field--invalid :slotted(textarea) {
  border-color: var(--dt-critical-solid) !important;
}
</style>
