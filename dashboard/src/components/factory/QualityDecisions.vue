<script setup lang="ts">
import { computed } from 'vue'
import { useOperationsStore, type Unit } from '@/stores/operations'

const props = defineProps<{ unit: Unit }>()
const ops = useOperationsStore()

type Decision = 'approve' | 'review' | 'recondition' | 'reconditioned' | 'scrap'
function decide(d: Decision) {
  ops.decideQuality(props.unit.id, d)
}

// Capacidade da secção de destino do recondicionamento (SEC-RETRAB).
const recondInfo = computed(() => ops.decisionTarget(props.unit.id, 'recondition'))
const recondBlocked = computed(() => recondInfo.value?.blocked ?? false)
</script>

<template>
  <div class="qd">
    <button
      type="button"
      class="qd__btn qd__btn--ok"
      @click="decide('approve')"
    >
      Aprovar
    </button>
    <button
      v-if="unit.quality !== 'pending'"
      type="button"
      class="qd__btn qd__btn--warn"
      @click="decide('review')"
    >
      Em análise
    </button>
    <button
      v-if="unit.quality === 'reconditioning'"
      type="button"
      class="qd__btn qd__btn--ok"
      @click="decide('reconditioned')"
    >
      Recond. concluído
    </button>
    <button
      v-else
      type="button"
      class="qd__btn qd__btn--action"
      :class="{ 'qd__btn--off': recondBlocked }"
      :disabled="recondBlocked"
      :title="recondBlocked ? 'Secção cheia, aguarda disponibilidade' : 'Enviar para recondicionamento'"
      @click="decide('recondition')"
    >
      Recondicionar
    </button>
    <button
      type="button"
      class="qd__btn qd__btn--danger"
      @click="decide('scrap')"
    >
      Sucata
    </button>
  </div>
  <p
    v-if="recondBlocked && unit.quality !== 'reconditioning'"
    class="qd__warn"
  >
    Recondicionamento: secção cheia, aguarda disponibilidade (cap. {{ recondInfo?.capacity }}).
  </p>
</template>

<style scoped>
.qd {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}
.qd__btn {
  border-radius: var(--dt-radius);
  padding: 0.4rem 0.7rem;
  font-size: 0.72rem;
  font-weight: 800;
  cursor: pointer;
  border: 1px solid transparent;
  color: #fff;
}
.qd__btn--ok {
  background: var(--dt-ok-solid);
}
.qd__btn--warn {
  background: var(--dt-warn-solid);
}
.qd__btn--action {
  background: var(--dt-action-solid);
}
.qd__btn--danger {
  background: var(--dt-critical-solid);
}
.qd__btn--off {
  opacity: 0.45;
  cursor: not-allowed;
}
.qd__warn {
  margin: 0.4rem 0 0;
  font-size: 0.68rem;
  font-weight: 700;
  color: var(--dt-warn-text);
}
</style>
