<script setup lang="ts">
import { useOperationsStore, type Unit } from '@/stores/operations'

const props = defineProps<{ unit: Unit }>()
const ops = useOperationsStore()

type Decision = 'approve' | 'review' | 'recondition' | 'reconditioned' | 'scrap'
function decide(d: Decision) {
  ops.decideQuality(props.unit.id, d)
}
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
</style>
