<script setup lang="ts">
import { computed, ref } from 'vue'
import { useOperationsStore } from '@/stores/operations'
import EmptyState from '@/components/common/EmptyState.vue'

const ops = useOperationsStore()

const SECTION_TYPES = ['Produção', 'Qualidade', 'Armazém', 'Rastreio', 'Transferência', 'Retrabalho', 'Logística pós-linha']
const LINE_GROUPS = ['montagem', 'pintura', 'qualidade']

// Produto
const productName = ref('')
const productInfo = ref('')
// Matéria-prima
const materialName = ref('')
const materialInfo = ref('')
// Linha
const lineCode = ref('')
const lineName = ref('')
const lineGroup = ref(LINE_GROUPS[0])
// Secção
const sectionLineId = ref(ops.lines[0]?.id ?? '')
const sectionName = ref('')
const sectionType = ref(SECTION_TYPES[0])
const sectionZone = ref('')

const toast = ref('')
function flash(message: string) {
  toast.value = message
  window.setTimeout(() => (toast.value = ''), 2600)
}

const sectionsOfSelected = computed(() => ops.lineById(sectionLineId.value)?.sections ?? [])

function submitProduct() {
  const created = ops.addProduct(productName.value, productInfo.value)
  if (created) {
    flash(`Produto "${created.name}" criado.`)
    productName.value = ''
    productInfo.value = ''
  }
}
function submitMaterial() {
  const created = ops.addMaterial(materialName.value, materialInfo.value)
  if (created) {
    flash(`Matéria-prima "${created.name}" criada.`)
    materialName.value = ''
    materialInfo.value = ''
  }
}
function submitLine() {
  const created = ops.addLine({ code: lineCode.value, name: lineName.value, group: lineGroup.value })
  if (created) {
    flash(`Linha "${created.code}" criada.`)
    lineCode.value = ''
    lineName.value = ''
  }
}
function submitSection() {
  const created = ops.addSection(sectionLineId.value, {
    name: sectionName.value,
    type: sectionType.value,
    zone: sectionZone.value,
  })
  if (created) {
    flash(`Secção "${created.name}" adicionada.`)
    sectionName.value = ''
    sectionZone.value = ''
  }
}
</script>

<template>
  <div class="mng">
    <p
      v-if="toast"
      class="mng__toast"
      role="status"
    >
      {{ toast }}
    </p>

    <div class="mng__grid">
      <!-- Produtos -->
      <section class="card">
        <div class="card__head">
          <h2 class="card__title">
            Produtos
          </h2>
        </div>
        <label class="fld"><span>Nome</span><input
          v-model="productName"
          type="text"
          placeholder="Ex.: Guarda-lamas"
        ></label>
        <label class="fld"><span>Descrição (opcional)</span><input
          v-model="productInfo"
          type="text"
          placeholder="Notas do produto"
        ></label>
        <button
          type="button"
          class="btn"
          :disabled="!productName.trim()"
          @click="submitProduct"
        >
          Adicionar produto
        </button>
        <ul class="chips">
          <li
            v-for="p in ops.products"
            :key="p.id"
            class="chip"
          >
            {{ p.name }}
          </li>
        </ul>
      </section>

      <!-- Matérias-primas -->
      <section class="card">
        <div class="card__head">
          <h2 class="card__title">
            Matérias-primas
          </h2>
        </div>
        <label class="fld"><span>Nome</span><input
          v-model="materialName"
          type="text"
          placeholder="Ex.: Verniz acrílico"
        ></label>
        <label class="fld"><span>Descrição (opcional)</span><input
          v-model="materialInfo"
          type="text"
          placeholder="Especificação"
        ></label>
        <button
          type="button"
          class="btn"
          :disabled="!materialName.trim()"
          @click="submitMaterial"
        >
          Adicionar matéria-prima
        </button>
        <ul class="chips">
          <li
            v-for="m in ops.materials"
            :key="m.id"
            class="chip"
          >
            {{ m.name }}
          </li>
        </ul>
      </section>

      <!-- Linhas -->
      <section class="card">
        <div class="card__head">
          <h2 class="card__title">
            Linhas de produção
          </h2>
        </div>
        <label class="fld"><span>Código</span><input
          v-model="lineCode"
          type="text"
          placeholder="Ex.: LINHA-05"
        ></label>
        <label class="fld"><span>Nome</span><input
          v-model="lineName"
          type="text"
          placeholder="Ex.: Acabamento final"
        ></label>
        <label class="fld">
          <span>Grupo visual</span>
          <select v-model="lineGroup"><option
            v-for="g in LINE_GROUPS"
            :key="g"
            :value="g"
          >{{ g }}</option></select>
        </label>
        <button
          type="button"
          class="btn"
          :disabled="!lineCode.trim() || !lineName.trim()"
          @click="submitLine"
        >
          Criar linha
        </button>
        <ul class="chips">
          <li
            v-for="l in ops.lines"
            :key="l.id"
            class="chip"
          >
            {{ l.code }} · {{ l.sections.length }} secç.
          </li>
        </ul>
      </section>

      <!-- Secções -->
      <section class="card">
        <div class="card__head">
          <h2 class="card__title">
            Secções
          </h2>
        </div>
        <label class="fld">
          <span>Linha</span>
          <select v-model="sectionLineId"><option
            v-for="l in ops.lines"
            :key="l.id"
            :value="l.id"
          >{{ l.code }}: {{ l.name }}</option></select>
        </label>
        <label class="fld"><span>Nome da secção</span><input
          v-model="sectionName"
          type="text"
          placeholder="Ex.: Polimento"
        ></label>
        <label class="fld">
          <span>Tipo</span>
          <select v-model="sectionType"><option
            v-for="t in SECTION_TYPES"
            :key="t"
            :value="t"
          >{{ t }}</option></select>
        </label>
        <label class="fld"><span>Zona visual (opcional)</span><input
          v-model="sectionZone"
          type="text"
          placeholder="Ex.: acabamento"
        ></label>
        <button
          type="button"
          class="btn"
          :disabled="!sectionName.trim()"
          @click="submitSection"
        >
          Adicionar secção
        </button>
        <ul
          v-if="sectionsOfSelected.length"
          class="chips"
        >
          <li
            v-for="s in sectionsOfSelected"
            :key="s.id"
            class="chip"
          >
            {{ s.name }}
          </li>
        </ul>
        <EmptyState
          v-else
          title="Sem secções"
          description="Esta linha ainda não tem secções."
          icon="-"
        />
      </section>
    </div>
  </div>
</template>

<style scoped>
.mng {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.mng__toast {
  margin: 0;
  padding: 0.6rem 0.9rem;
  border-radius: var(--dt-radius);
  background: var(--dt-ok-surface);
  border: 1px solid var(--dt-ok-border);
  color: var(--dt-ok-text);
  font-size: 0.8rem;
  font-weight: 700;
}
.mng__grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 1rem;
}
@media (max-width: 900px) {
  .mng__grid {
    grid-template-columns: 1fr;
  }
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
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.8rem;
}
.card__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.fld {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin-bottom: 0.6rem;
}
.fld span {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--dt-text-strong);
}
.fld input,
.fld select {
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius);
  padding: 0.5rem;
  font-size: 0.82rem;
  background: var(--dt-surface-2);
  color: var(--dt-text-strong);
  font-family: inherit;
}
.btn {
  width: 100%;
  border: none;
  border-radius: var(--dt-radius);
  padding: 0.6rem;
  background: linear-gradient(135deg, #0877d8, #065aa7);
  color: #fff;
  font-weight: 900;
  font-size: 0.82rem;
  cursor: pointer;
}
.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.chips {
  list-style: none;
  margin: 0.8rem 0 0;
  padding: 0;
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}
.chip {
  font-size: 0.7rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
  background: var(--dt-neutral-surface);
  border: 1px solid var(--dt-border);
  border-radius: 9999px;
  padding: 0.2rem 0.6rem;
}
</style>
