import { defineStore } from 'pinia'

/* ============================================================
 * Recursos da fábrica ("Funcionários") — pessoas E recursos
 * produtivos (braço mecânico, célula automática, equipamento).
 * NÃO confundir com "utilizador da aplicação": só alguns recursos
 * têm conta de acesso (hasAccount). Os restantes existem apenas
 * como recursos atribuíveis a linhas, secções ou ações.
 * ============================================================ */

export type ResourceType =
  | 'operator' // operador humano
  | 'quality' // técnico de qualidade
  | 'logistics' // logística
  | 'supervisor' // supervisor/gerente
  | 'robot' // braço mecânico
  | 'cell' // célula automática
  | 'equipment' // equipamento/posto

export interface Resource {
  id: string
  name: string
  type: ResourceType
  hasAccount: boolean // tem conta de acesso à aplicação
  active: boolean
  lineId: string | null // linha atribuída (opcional)
}

export const RESOURCE_LABEL: Record<ResourceType, string> = {
  operator: 'Operador (humano)',
  quality: 'Técnico de qualidade',
  logistics: 'Logística',
  supervisor: 'Supervisor / Gerente',
  robot: 'Braço mecânico',
  cell: 'Célula automática',
  equipment: 'Equipamento / posto',
}

const HUMAN_TYPES: ResourceType[] = ['operator', 'quality', 'logistics', 'supervisor']
export function isHuman(type: ResourceType): boolean {
  return HUMAN_TYPES.includes(type)
}

let seq = 100

function seedResources(): Resource[] {
  return [
    { id: 'R1', name: 'Ana Marques', type: 'supervisor', hasAccount: true, active: true, lineId: null },
    { id: 'R2', name: 'João Pereira', type: 'operator', hasAccount: true, active: true, lineId: 'L1' },
    { id: 'R3', name: 'Rui Costa', type: 'operator', hasAccount: false, active: true, lineId: 'L2' },
    { id: 'R4', name: 'Marta Silva', type: 'quality', hasAccount: true, active: true, lineId: 'L4' },
    { id: 'R5', name: 'Pedro Sousa', type: 'logistics', hasAccount: true, active: true, lineId: 'L4' },
    { id: 'R6', name: 'Robô soldadura KR-210', type: 'robot', hasAccount: false, active: true, lineId: 'L1' },
    { id: 'R7', name: 'Célula de pintura A', type: 'cell', hasAccount: false, active: true, lineId: 'L2' },
    { id: 'R8', name: 'Posto de inspeção ótica', type: 'equipment', hasAccount: false, active: false, lineId: 'L3' },
  ]
}

export const useResourcesStore = defineStore('resources', {
  state: () => ({
    resources: seedResources() as Resource[],
  }),
  getters: {
    humans(state): Resource[] {
      return state.resources.filter((r) => isHuman(r.type))
    },
    machines(state): Resource[] {
      return state.resources.filter((r) => !isHuman(r.type))
    },
    withAccount(state): Resource[] {
      return state.resources.filter((r) => r.hasAccount)
    },
    activeCount(state): number {
      return state.resources.filter((r) => r.active).length
    },
  },
  actions: {
    addResource(input: { name: string; type: ResourceType; hasAccount: boolean; lineId?: string | null }) {
      seq += 1
      this.resources.unshift({
        id: `R${seq}`,
        name: input.name.trim() || 'Sem nome',
        type: input.type,
        hasAccount: isHuman(input.type) ? input.hasAccount : false, // máquinas nunca têm conta
        active: true,
        lineId: input.lineId ?? null,
      })
    },
    updateResource(id: string, patch: Partial<Omit<Resource, 'id'>>) {
      const r = this.resources.find((x) => x.id === id)
      if (!r) return
      Object.assign(r, patch)
      if (!isHuman(r.type)) r.hasAccount = false
    },
    toggleActive(id: string) {
      const r = this.resources.find((x) => x.id === id)
      if (r) r.active = !r.active
    },
    removeResource(id: string) {
      this.resources = this.resources.filter((x) => x.id !== id)
    },
  },
})
