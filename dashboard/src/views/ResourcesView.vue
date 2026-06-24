<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import {
  useResourcesStore,
  RESOURCE_LABEL,
  isHuman,
  type ResourceType,
  type Resource,
} from '@/stores/resources'
import { useOperationsStore } from '@/stores/operations'
import StatStrip from '@/components/common/StatStrip.vue'
import StatusBadge from '@/components/common/StatusBadge.vue'

const res = useResourcesStore()
const ops = useOperationsStore()

type Cat = 'all' | 'people' | 'machines'
const cat = ref<Cat>('all')

const list = computed(() => {
  if (cat.value === 'people') return res.humans
  if (cat.value === 'machines') return res.machines
  return res.resources
})

const kpis = computed(() => [
  { label: 'Recursos', value: res.resources.length, tone: 'info' as const },
  { label: 'Pessoas', value: res.humans.length, tone: 'neutral' as const },
  { label: 'Máquinas', value: res.machines.length, tone: 'action' as const },
  { label: 'Com conta de acesso', value: res.withAccount.length, tone: 'ok' as const },
])

const types: ResourceType[] = ['operator', 'quality', 'logistics', 'supervisor', 'robot', 'cell', 'equipment']
const cats: Array<{ key: Cat; label: string }> = [
  { key: 'all', label: 'Todos' },
  { key: 'people', label: 'Pessoas' },
  { key: 'machines', label: 'Máquinas' },
]

const form = reactive<{ name: string; type: ResourceType; hasAccount: boolean; lineId: string }>({
  name: '',
  type: 'operator',
  hasAccount: false,
  lineId: '',
})
const formIsHuman = computed(() => isHuman(form.type))

function submit() {
  if (!form.name.trim()) return
  res.addResource({
    name: form.name,
    type: form.type,
    hasAccount: form.hasAccount,
    lineId: form.lineId || null,
  })
  form.name = ''
  form.hasAccount = false
  form.lineId = ''
}

function lineCode(id: string | null): string {
  if (!id) return '-'
  return ops.lineById(id)?.code ?? '-'
}
function category(r: Resource): string {
  return isHuman(r.type) ? 'Pessoa' : 'Máquina'
}
</script>

<template>
  <div class="res">
    <StatStrip :items="kpis" />

    <section class="card">
      <h2 class="card__title">
        Adicionar recurso
      </h2>
      <p class="card__sub">
        Um recurso pode ser uma pessoa ou um recurso produtivo (braço mecânico, célula, equipamento).
        Só pessoas podem ter conta de acesso.
      </p>
      <div class="form">
        <label class="form__field form__field--grow">
          <span>Nome / designação</span>
          <input
            v-model="form.name"
            type="text"
            placeholder="Ex.: Maria Lopes ou Robô paletizador"
            @keyup.enter="submit"
          >
        </label>
        <label class="form__field">
          <span>Tipo</span>
          <select v-model="form.type">
            <option
              v-for="t in types"
              :key="t"
              :value="t"
            >{{ RESOURCE_LABEL[t] }}</option>
          </select>
        </label>
        <label class="form__field">
          <span>Linha</span>
          <select v-model="form.lineId">
            <option value="">Sem atribuição</option>
            <option
              v-for="l in ops.lines"
              :key="l.id"
              :value="l.id"
            >{{ l.code }}</option>
          </select>
        </label>
        <label
          class="form__check"
          :class="{ 'form__check--off': !formIsHuman }"
        >
          <input
            v-model="form.hasAccount"
            type="checkbox"
            :disabled="!formIsHuman"
          >
          <span>Conta de acesso</span>
        </label>
        <button
          type="button"
          class="form__btn"
          :disabled="!form.name.trim()"
          @click="submit"
        >
          Adicionar
        </button>
      </div>
    </section>

    <section class="card">
      <div class="card__head">
        <div>
          <h2 class="card__title">
            Funcionários e recursos
          </h2>
          <p class="card__sub">
            {{ list.length }} recurso(s)
          </p>
        </div>
        <div
          class="seg"
          role="tablist"
        >
          <button
            v-for="c in cats"
            :key="c.key"
            type="button"
            class="seg__btn"
            :class="{ 'seg__btn--active': cat === c.key }"
            role="tab"
            :aria-selected="cat === c.key"
            @click="cat = c.key"
          >
            {{ c.label }}
          </button>
        </div>
      </div>

      <div class="tbl">
        <div class="tbl__head">
          <span>Nome</span>
          <span>Tipo</span>
          <span class="tbl__col-hide">Categoria</span>
          <span class="tbl__col-hide">Linha</span>
          <span>Conta</span>
          <span>Estado</span>
          <span />
        </div>
        <div
          v-for="r in list"
          :key="r.id"
          class="tbl__row"
          :class="{ 'tbl__row--off': !r.active }"
        >
          <span class="tbl__name">{{ r.name }}</span>
          <span class="tbl__type">{{ RESOURCE_LABEL[r.type] }}</span>
          <span class="tbl__col-hide">
            <StatusBadge
              :label="category(r)"
              :tone="isHuman(r.type) ? 'neutral' : 'action'"
              size="sm"
            />
          </span>
          <span class="tbl__col-hide tbl__line">{{ lineCode(r.lineId) }}</span>
          <span>
            <StatusBadge
              v-if="r.hasAccount"
              label="Acesso"
              tone="ok"
              size="sm"
            />
            <span
              v-else
              class="tbl__muted"
            >-</span>
          </span>
          <span>
            <StatusBadge
              :label="r.active ? 'Ativo' : 'Inativo'"
              :tone="r.active ? 'ok' : 'neutral'"
              size="sm"
            />
          </span>
          <span class="tbl__actions">
            <button
              type="button"
              class="tbl__btn"
              :title="r.active ? 'Desativar' : 'Ativar'"
              @click="res.toggleActive(r.id)"
            >
              {{ r.active ? 'Desativar' : 'Ativar' }}
            </button>
            <button
              type="button"
              class="tbl__btn tbl__btn--danger"
              title="Remover"
              @click="res.removeResource(r.id)"
            >
              Remover
            </button>
          </span>
        </div>
      </div>
    </section>
  </div>
</template>

<style scoped>
.res {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.card {
  background: var(--dt-surface);
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  padding: 1rem;
  box-shadow: var(--dt-shadow-card);
}
.card__head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 0.75rem;
  flex-wrap: wrap;
  margin-bottom: 0.8rem;
}
.card__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.card__sub {
  margin: 0.15rem 0 0.8rem;
  font-size: 0.72rem;
  color: var(--dt-neutral-text);
}
.form {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  gap: 0.7rem;
}
.form__field {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.form__field--grow {
  flex: 1 1 220px;
}
.form__field span {
  font-size: 0.7rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
}
.form__field input,
.form__field select {
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius);
  padding: 0.45rem 0.55rem;
  background: var(--dt-surface);
  color: var(--dt-text-strong);
  font-size: 0.8rem;
}
.form__check {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--dt-text-strong);
  padding-bottom: 0.5rem;
}
.form__check--off {
  opacity: 0.5;
}
.form__btn {
  background: var(--dt-brand-500);
  color: #fff;
  border: none;
  border-radius: var(--dt-radius);
  padding: 0.5rem 1rem;
  font-size: 0.8rem;
  font-weight: 800;
  cursor: pointer;
}
.form__btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.seg {
  display: inline-flex;
  background: var(--dt-neutral-surface);
  border: 1px solid var(--dt-border);
  border-radius: 9999px;
  padding: 3px;
}
.seg__btn {
  border: none;
  background: transparent;
  color: var(--dt-neutral-text);
  border-radius: 9999px;
  padding: 0.3rem 0.7rem;
  font-size: 0.72rem;
  font-weight: 800;
  cursor: pointer;
}
.seg__btn--active {
  background: var(--dt-brand-500);
  color: #fff;
}
.tbl {
  display: flex;
  flex-direction: column;
}
.tbl__head,
.tbl__row {
  display: grid;
  grid-template-columns: 1.4fr 1.2fr 0.8fr 0.6fr 0.7fr 0.7fr auto;
  gap: 0.5rem;
  align-items: center;
  padding: 0.55rem 0.4rem;
}
.tbl__head {
  font-size: 0.66rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: var(--dt-neutral-text);
  border-bottom: 1px solid var(--dt-border);
}
.tbl__row {
  border-bottom: 1px solid var(--dt-border);
  font-size: 0.8rem;
}
.tbl__row--off {
  opacity: 0.55;
}
.tbl__name {
  font-weight: 800;
  color: var(--dt-text-strong);
}
.tbl__type,
.tbl__line {
  color: var(--dt-neutral-text);
}
.tbl__muted {
  color: var(--dt-neutral-text);
}
.tbl__actions {
  display: flex;
  gap: 0.35rem;
  justify-content: flex-end;
}
.tbl__btn {
  border: 1px solid var(--dt-border);
  background: transparent;
  color: var(--dt-neutral-text);
  border-radius: var(--dt-radius);
  padding: 0.3rem 0.55rem;
  font-size: 0.7rem;
  font-weight: 800;
  cursor: pointer;
}
.tbl__btn--danger {
  color: var(--dt-critical-text);
  border-color: var(--dt-critical-border);
}
@media (max-width: 760px) {
  .tbl__col-hide {
    display: none;
  }
  .tbl__head,
  .tbl__row {
    grid-template-columns: 1.4fr 1.1fr 0.7fr 0.7fr auto;
  }
}
</style>
