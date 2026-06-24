import { defineStore } from 'pinia'
import type { StatusTone } from '@/utils/status'

/* ============================================================
 * Modelo de operações reativo, alinhado com o esquema real da
 * base de dados (ProductionLine / ProductionLineSection /
 * ManufacturingOrder / ProductUnit / ProductUnitLocationHistory).
 * Mantém o estado em memória para a demo ser fluida sem backend.
 * Cada ação tem equivalente REST (indicado nos comentários):
 *   /api/manufacturing-orders, /api/product-units,
 *   /api/product-unit-location-history, /api/production-lines,
 *   /api/production-line-sections, /api/products, /api/raw-materials
 * ============================================================ */

export type UnitState =
  | 'queued'
  | 'active'
  | 'transfer'
  | 'completed'
  | 'scrap'

/** Disposição de qualidade (decisão tomada na Linha 4 / controlo de qualidade). */
export type QualityStatus =
  | 'none' // ainda não avaliada
  | 'pending' // em análise de qualidade
  | 'approved' // aprovado
  | 'recoverable' // não conformidade recuperável
  | 'reconditioning' // em recondicionamento
  | 'scrap' // sucata

export type OrderStatus =
  | 'submitted'
  | 'accepted'
  | 'in_production'
  | 'ready'
  | 'completed'
  | 'rejected'
  | 'cancelled'

export interface Section {
  id: string // SectionCode real (ex.: SEC-SOLD)
  name: string
  type: string // SectionType (Produção, Qualidade, Armazém, ...)
  layoutColumn: number
  zone: string // VisualZone
  isTransferPoint: boolean
  allowsTransferIn: boolean
  allowsTransferOut: boolean
  capacity: number // máx. de unidades em simultâneo (armazéns/buffers: ilimitado)
  responsibleId: string | null // recurso responsável (id em useResourcesStore)
}

export interface Line {
  id: string
  code: string // LineCode real (LINHA-01)
  name: string
  group: string // VisualGroup (montagem / pintura / qualidade)
  sections: Section[]
}

export interface Unit {
  id: string
  label: string // UnitCode
  product: string
  orderId: string | null
  lineId: string
  sectionId: string
  state: UnitState
  quality: QualityStatus
  supportId: string | null // Support ativo (rastreabilidade física no chão de fábrica)
  updatedAt: number
  history: UnitEvent[]
}

export interface UnitEvent {
  at: number
  title: string
  detail?: string
  tone?: StatusTone
}

export interface Order {
  id: string
  reference: string // código técnico de rastreabilidade (ex.: OF-PORTA-D-001)
  name: string // nome legível (ex.: Encomenda Porta Direita Standard)
  customer: string
  product: string
  quantity: number
  lineId: string
  status: OrderStatus
  createdAt: number
  acceptedAt?: number
  startedAt?: number
  completedAt?: number
  note?: string
  desiredDate?: string
}

export interface ActivityRecord {
  id: number
  at: number
  title: string
  detail?: string
  tone: StatusTone
}

export interface ProductRef {
  id: number
  name: string
  info?: string
}

export interface MaterialRef {
  id: number
  name: string
  info?: string
}

/** Suporte físico (palete/berço/skid) que torna o WIP rastreável no chão de fábrica. */
export interface Support {
  id: string
  code: string // ex.: SUP-014
  type: string // ex.: Palete, Berço, Skid
  status: 'free' | 'in_use'
}

const UNIT_STATE_TONE: Record<UnitState, StatusTone> = {
  queued: 'neutral',
  active: 'info',
  transfer: 'info',
  completed: 'ok',
  scrap: 'critical',
}

const UNIT_STATE_LABEL: Record<UnitState, string> = {
  queued: 'Em fila',
  active: 'Em produção',
  transfer: 'Em transferência',
  completed: 'Concluída',
  scrap: 'Sucata',
}

const QUALITY_LABEL: Record<QualityStatus, string> = {
  none: 'Em fluxo',
  pending: 'Em análise de qualidade',
  approved: 'Aprovado',
  recoverable: 'Recondicionável',
  reconditioning: 'Recondicionamento',
  scrap: 'Sucata',
}

const QUALITY_TONE: Record<QualityStatus, StatusTone> = {
  none: 'info',
  pending: 'warn',
  approved: 'ok',
  recoverable: 'action',
  reconditioning: 'action',
  scrap: 'critical',
}

const ORDER_STATUS_TONE: Record<OrderStatus, StatusTone> = {
  submitted: 'warn',
  accepted: 'info',
  in_production: 'info',
  ready: 'ok',
  completed: 'ok',
  rejected: 'critical',
  cancelled: 'neutral',
}

const ORDER_STATUS_LABEL: Record<OrderStatus, string> = {
  submitted: 'Aguarda aceitação',
  accepted: 'Aceite (em planeamento)',
  in_production: 'Em produção',
  ready: 'Pronta para expedição',
  completed: 'Concluída',
  rejected: 'Recusada',
  cancelled: 'Cancelada',
}

export function unitStateTone(state: UnitState): StatusTone {
  return UNIT_STATE_TONE[state]
}
export function unitStateLabel(state: UnitState): string {
  return UNIT_STATE_LABEL[state]
}
export function qualityLabel(quality: QualityStatus): string {
  return QUALITY_LABEL[quality]
}
export function qualityTone(quality: QualityStatus): StatusTone {
  return QUALITY_TONE[quality]
}

/** Estado mostrado ao utilizador: combina fluxo + decisão de qualidade. */
export function unitStatusLabel(unit: Unit): string {
  if (unit.state === 'completed') return 'Concluída'
  if (unit.state === 'scrap' || unit.quality === 'scrap') return 'Sucata'
  if (unit.quality !== 'none' && unit.quality !== 'approved') return QUALITY_LABEL[unit.quality]
  if (unit.quality === 'approved') return 'Aprovado'
  return UNIT_STATE_LABEL[unit.state]
}
export function unitStatusTone(unit: Unit): StatusTone {
  if (unit.state === 'completed' || unit.quality === 'approved') return 'ok'
  if (unit.state === 'scrap' || unit.quality === 'scrap') return 'critical'
  if (unit.quality !== 'none') return QUALITY_TONE[unit.quality]
  return UNIT_STATE_TONE[unit.state]
}
export function orderStatusTone(status: OrderStatus): StatusTone {
  return ORDER_STATUS_TONE[status]
}
export function orderStatusLabel(status: OrderStatus): string {
  return ORDER_STATUS_LABEL[status]
}

/* ---- Rota produtiva nominal (sequência esperada entre linhas/secções) ---- */
export interface RouteStage {
  key: string
  label: string
  sections: string[]
}
export const NOMINAL_ROUTE: RouteStage[] = [
  { key: 'mp', label: 'Matéria-prima', sections: ['SEC-MP'] },
  { key: 'suporte', label: 'Atribuição de suporte', sections: ['SEC-ATRIB-SUP'] },
  { key: 'fabrico', label: 'Fabrico e soldadura', sections: ['SEC-CORTE-ESTAMP', 'SEC-SOLD', 'SEC-TRANS-PINT'] },
  {
    key: 'pintura',
    label: 'Pintura',
    sections: ['SEC-PREP-PINT-A', 'SEC-PINT-A', 'SEC-CURA-A', 'SEC-PINT-B', 'SEC-INSPEC-PINT-B'],
  },
  { key: 'qualidade', label: 'Controlo de qualidade', sections: ['SEC-MONT-FINAL', 'SEC-CQ'] },
  { key: 'rack', label: 'Rack / armazém final', sections: ['SEC-RACK'] },
  { key: 'expedicao', label: 'Expedição', sections: ['SEC-EXPED'] },
]
/** Secções fora da rota nominal (recondicionamento = desvio controlado). */
export const DEVIATION_SECTIONS = ['SEC-RETRAB']

export type RouteStageStatus = 'done' | 'current' | 'pending'
export interface RouteView {
  stages: Array<{ key: string; label: string; status: RouteStageStatus }>
  currentLabel: string
  nextLabel: string | null
  offRoute: boolean
  offRouteReason: string | null
}

const QUALITY_ZONE_INDEX = NOMINAL_ROUTE.findIndex((s) => s.key === 'qualidade')

export function routeView(unit: Unit): RouteView {
  const completed = unit.state === 'completed'
  const isScrap = unit.state === 'scrap' || unit.quality === 'scrap'
  const isReconditioning = DEVIATION_SECTIONS.includes(unit.sectionId) || unit.quality === 'reconditioning'

  let currentIndex = NOMINAL_ROUTE.findIndex((s) => s.sections.includes(unit.sectionId))
  if (currentIndex < 0) currentIndex = QUALITY_ZONE_INDEX // recondicionamento ancora na zona de qualidade

  const stages = NOMINAL_ROUTE.map((s, i) => ({
    key: s.key,
    label: s.label,
    status: (completed || i < currentIndex ? 'done' : i === currentIndex ? 'current' : 'pending') as RouteStageStatus,
  }))

  let offRoute = false
  let offRouteReason: string | null = null
  if (isScrap) {
    offRoute = true
    offRouteReason = 'Fora da rota: unidade em sucata.'
  } else if (isReconditioning) {
    offRoute = true
    offRouteReason = 'Desvio controlado: em recondicionamento.'
  } else if (unit.quality === 'recoverable') {
    offRoute = true
    offRouteReason = 'Não conformidade recuperável a aguardar decisão de qualidade.'
  }

  const next = completed ? null : NOMINAL_ROUTE[currentIndex + 1]
  return {
    stages,
    currentLabel: NOMINAL_ROUTE[currentIndex]?.label ?? '-',
    nextLabel: next?.label ?? null,
    offRoute,
    offRouteReason,
  }
}

/* ---- Atribuição de suporte (marco de rastreabilidade física) ---- */
const SUPPORT_STAGE_INDEX = NOMINAL_ROUTE.findIndex((s) => s.key === 'suporte')
export type SupportState = 'planned' | 'assigned' | 'missing'

/**
 * Estado de suporte da unidade:
 *  - planned  : sem suporte e ainda não passou a atribuição (unidade planeada/preparada)
 *  - assigned : tem suporte ativo (rastreável no chão de fábrica)
 *  - missing  : já passou a atribuição mas não tem suporte → PROBLEMA de rastreabilidade
 */
export function supportState(unit: Unit): SupportState {
  if (unit.supportId) return 'assigned'
  if (unit.state === 'completed' || unit.state === 'scrap') return 'planned'
  let idx = NOMINAL_ROUTE.findIndex((s) => s.sections.includes(unit.sectionId))
  if (idx < 0) idx = QUALITY_ZONE_INDEX
  return idx > SUPPORT_STAGE_INDEX ? 'missing' : 'planned'
}

function section(
  id: string,
  name: string,
  type: string,
  column: number,
  zone: string,
  isTransferPoint = false,
  allowsTransferIn = false,
  allowsTransferOut = false,
): Section {
  return {
    id,
    name,
    type,
    layoutColumn: column,
    zone,
    isTransferPoint,
    allowsTransferIn,
    allowsTransferOut,
    capacity: 1,
    responsibleId: null,
  }
}

// Estrutura real (ver api/Data/DemoSeeder.cs).
const LINES: Line[] = [
  {
    id: 'L1',
    code: 'LINHA-01',
    name: 'Montagem e soldadura',
    group: 'montagem',
    sections: [
      section('SEC-MP', 'Matérias-primas', 'Armazém', 1, 'entrada'),
      section('SEC-ATRIB-SUP', 'Atribuição de suporte', 'Rastreio', 2, 'rastreio', true, true, true),
      section('SEC-CORTE-ESTAMP', 'Corte e estampagem', 'Produção', 3, 'fabrico'),
      section('SEC-SOLD', 'Soldadura', 'Produção', 4, 'fabrico', false, false, true),
      section('SEC-TRANS-PINT', 'Buffer transf. pintura', 'Transferência', 5, 'transferencia', true, true, true),
    ],
  },
  {
    id: 'L2',
    code: 'LINHA-02',
    name: 'Pintura A',
    group: 'pintura',
    sections: [
      section('SEC-PREP-PINT-A', 'Preparação pintura A', 'Produção', 1, 'pintura', true, true, false),
      section('SEC-PINT-A', 'Pintura A', 'Produção', 2, 'pintura', false, false, true),
      section('SEC-CURA-A', 'Cura A', 'Produção', 3, 'pintura', true, false, true),
    ],
  },
  {
    id: 'L3',
    code: 'LINHA-03',
    name: 'Pintura B',
    group: 'pintura',
    sections: [
      section('SEC-PINT-B', 'Pintura B', 'Produção', 1, 'pintura', true, true, true),
      section('SEC-INSPEC-PINT-B', 'Inspeção pintura B', 'Qualidade', 2, 'qualidade-pintura', true, false, true),
    ],
  },
  {
    id: 'L4',
    code: 'LINHA-04',
    name: 'Qualidade e recuperação',
    group: 'qualidade',
    sections: [
      section('SEC-MONT-FINAL', 'Montagem final', 'Produção', 1, 'montagem-final', true, true, true),
      section('SEC-CQ', 'Controlo de qualidade', 'Qualidade', 2, 'qualidade', true, true, true),
      section('SEC-RETRAB', 'Recondicionamento', 'Recondicionamento', 3, 'recondicionamento', true, true, true),
      section('SEC-RACK', 'Armazenamento rack', 'Logística pós-linha', 4, 'pos-linha', false, true, false),
      section('SEC-EXPED', 'Buffer expedição', 'Logística pós-linha', 5, 'pos-linha', false, true, false),
    ],
  },
]

// Capacidade por secção (armazéns/buffers ilimitados) + responsável (recurso).
// Regra: produção/qualidade = 1 por defeito; 2 só onde configurado (SOLD, PINT-A, CQ).
const SECTION_CONFIG: Record<string, { capacity?: number; responsibleId?: string }> = {
  'SEC-MP': { capacity: 99, responsibleId: 'R5' },
  'SEC-ATRIB-SUP': { capacity: 1, responsibleId: 'R5' },
  'SEC-CORTE-ESTAMP': { capacity: 1, responsibleId: 'R2' },
  'SEC-SOLD': { capacity: 2, responsibleId: 'R6' },
  'SEC-TRANS-PINT': { capacity: 1, responsibleId: 'R2' },
  'SEC-PREP-PINT-A': { capacity: 1, responsibleId: 'R3' },
  'SEC-PINT-A': { capacity: 2, responsibleId: 'R7' },
  'SEC-CURA-A': { capacity: 1, responsibleId: 'R7' },
  'SEC-PINT-B': { capacity: 1, responsibleId: 'R3' },
  'SEC-INSPEC-PINT-B': { capacity: 1, responsibleId: 'R8' },
  'SEC-MONT-FINAL': { capacity: 1, responsibleId: 'R2' },
  'SEC-CQ': { capacity: 2, responsibleId: 'R4' },
  'SEC-RETRAB': { capacity: 2, responsibleId: 'R4' },
  'SEC-RACK': { capacity: 99, responsibleId: 'R5' },
  'SEC-EXPED': { capacity: 99, responsibleId: 'R5' },
}
for (const line of LINES) {
  for (const sec of line.sections) {
    const cfg = SECTION_CONFIG[sec.id]
    if (cfg) {
      if (typeof cfg.capacity === 'number') sec.capacity = cfg.capacity
      if (cfg.responsibleId) sec.responsibleId = cfg.responsibleId
    }
  }
}

const SEED_PRODUCTS: ProductRef[] = [
  { id: 1, name: 'Porta dianteira esquerda' },
  { id: 2, name: 'Porta dianteira direita' },
  { id: 3, name: 'Capô' },
  { id: 4, name: 'Painel lateral' },
  { id: 5, name: 'Tejadilho' },
]

const SEED_MATERIALS: MaterialRef[] = [
  { id: 1, name: 'Chapa de aço galvanizado', info: 'Bobina 1.2 mm' },
  { id: 2, name: 'Primário epóxico', info: 'Base anticorrosão' },
  { id: 3, name: 'Tinta base PU', info: 'Acabamento exterior' },
]

let unitSeq = 1000
let orderSeq = 0
let activitySeq = 1
let productSeq = 100
let materialSeq = 100
let lineSeq = 100
let sectionSeq = 100

function now() {
  return Date.now()
}
function findLine(lines: Line[], id: string): Line | undefined {
  return lines.find((l) => l.id === id)
}
function nextSection(line: Line, sectionId: string): Section | null {
  const index = line.sections.findIndex((s) => s.id === sectionId)
  if (index < 0 || index >= line.sections.length - 1) return null
  return line.sections[index + 1]
}

// Família técnica por produto (para os códigos OF/UP). Mantém-se curta e previsível.
const FAMILY_BY_PRODUCT: Record<string, string> = {
  'Porta dianteira direita': 'PORTA-D',
  'Porta dianteira esquerda': 'PORTA-E',
  'Painel lateral': 'PAINEL',
  Tejadilho: 'TEJAD',
  Capô: 'CAPO',
}
function familyCode(product: string): string {
  if (FAMILY_BY_PRODUCT[product]) return FAMILY_BY_PRODUCT[product]
  const slug = product
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .slice(0, 8)
  return slug || 'PROD'
}
function ofNumber(seq: number): string {
  return String(seq).padStart(3, '0')
}

function buildSeed(): { orders: Order[]; units: Unit[]; supports: Support[] } {
  // Conjunto demo pequeno e completo, com nomes legíveis e códigos previsíveis.
  // Convenção: OF-<FAMÍLIA>-<NNN> (ordem de fabrico) · UP-<FAMÍLIA>-<NNN>-<UU> (unidade).
  // quality = disposição no controlo de qualidade; 'none' = ainda em fluxo normal.
  type SeedUnit = { lineId: string; sectionId: string; state: UnitState; quality?: QualityStatus }
  type SeedOrder = {
    name: string
    customer: string
    product: string
    status: OrderStatus
    note?: string
    quantity?: number
    units: SeedUnit[]
  }

  const defs: SeedOrder[] = [
    // 1) EM PRODUÇÃO — 3 unidades da mesma encomenda em fases diferentes.
    {
      name: 'Encomenda Porta Direita Standard',
      customer: 'Auto Lisboa',
      product: 'Porta dianteira direita',
      status: 'in_production',
      units: [
        { lineId: 'L1', sectionId: 'SEC-SOLD', state: 'active' },
        { lineId: 'L3', sectionId: 'SEC-PINT-B', state: 'active' },
        { lineId: 'L4', sectionId: 'SEC-MONT-FINAL', state: 'active' },
      ],
    },
    // 2) EM PRODUÇÃO — resultados mistos: recondicionamento, recuperável, aprovada e sucata.
    {
      name: 'Encomenda Capô Premium',
      customer: 'Auto Lisboa',
      product: 'Capô',
      status: 'in_production',
      units: [
        { lineId: 'L4', sectionId: 'SEC-RETRAB', state: 'active', quality: 'reconditioning' },
        { lineId: 'L4', sectionId: 'SEC-CQ', state: 'active', quality: 'recoverable' },
        { lineId: 'L4', sectionId: 'SEC-RACK', state: 'active', quality: 'approved' },
        { lineId: 'L4', sectionId: 'SEC-CQ', state: 'scrap', quality: 'scrap' },
      ],
    },
    // 3) EM PRODUÇÃO — na pintura e em análise de qualidade.
    {
      name: 'Encomenda Painel Exterior Desportivo',
      customer: 'Caetano Auto',
      product: 'Painel lateral',
      status: 'in_production',
      units: [
        { lineId: 'L2', sectionId: 'SEC-CURA-A', state: 'active' },
        { lineId: 'L4', sectionId: 'SEC-CQ', state: 'active', quality: 'pending' },
        { lineId: 'L2', sectionId: 'SEC-PREP-PINT-A', state: 'queued' },
      ],
    },
    // 4) PRONTA — todas aprovadas no rack, a aguardar expedição.
    {
      name: 'Encomenda Porta Esquerda Standard',
      customer: 'Auto Lisboa',
      product: 'Porta dianteira esquerda',
      status: 'ready',
      units: [
        { lineId: 'L4', sectionId: 'SEC-RACK', state: 'active', quality: 'approved' },
        { lineId: 'L4', sectionId: 'SEC-RACK', state: 'active', quality: 'approved' },
      ],
    },
    // 5) CONCLUÍDA — entregue ao cliente.
    {
      name: 'Encomenda Tejadilho Panorâmico',
      customer: 'Auto Lisboa',
      product: 'Tejadilho',
      status: 'completed',
      units: [
        { lineId: 'L4', sectionId: 'SEC-EXPED', state: 'completed', quality: 'approved' },
        { lineId: 'L4', sectionId: 'SEC-EXPED', state: 'completed', quality: 'approved' },
      ],
    },
    // 6) EM PLANEAMENTO (aceite) — unidades em fila no armazém, ainda não iniciadas.
    {
      name: 'Encomenda Capô Standard',
      customer: 'Stellantis Setúbal',
      product: 'Capô',
      status: 'accepted',
      units: [
        { lineId: 'L1', sectionId: 'SEC-MP', state: 'queued' },
        { lineId: 'L1', sectionId: 'SEC-MP', state: 'queued' },
        { lineId: 'L1', sectionId: 'SEC-MP', state: 'queued' },
      ],
    },
    // 7) EM PLANEAMENTO (aguarda aceitação) — ainda sem unidades.
    {
      name: 'Encomenda Painel Exterior Standard',
      customer: 'Stellantis Setúbal',
      product: 'Painel lateral',
      status: 'submitted',
      note: 'Aguarda confirmação de capacidade.',
      quantity: 4,
      units: [],
    },
  ]

  const SUPPORT_TYPES = ['Palete', 'Berço', 'Skid']
  const supports: Support[] = Array.from({ length: 20 }, (_, i) => ({
    id: `S${i + 1}`,
    code: `SUP-${String(i + 1).padStart(3, '0')}`,
    type: SUPPORT_TYPES[i % SUPPORT_TYPES.length],
    status: 'free' as const,
  }))
  let nextSupport = 0
  const takeSupport = (): string | null => {
    const s = supports[nextSupport]
    if (!s) return null
    nextSupport += 1
    s.status = 'in_use'
    return s.id
  }

  // Idade de cada encomenda (minutos), distribuída para análise realista:
  // recentes (minutos), algumas horas, dia anterior e mais antigas.
  const AGO_MIN = [300, 1680, 175, 1500, 4320, 42, 12]
  const DONE_AGO_MIN = 1620 // concluída há ~27 h

  const orders: Order[] = []
  const units: Unit[] = []

  defs.forEach((d, di) => {
    orderSeq += 1
    const id = `O${orderSeq}`
    const fam = familyCode(d.product)
    const reference = `OF-${fam}-${ofNumber(orderSeq)}`
    const qty = d.units.length || d.quantity || 1
    const agoMin = AGO_MIN[di] ?? 120
    const createdAt = now() - agoMin * 60000
    const started = ['in_production', 'ready', 'completed'].includes(d.status)
    const accepted = started || d.status === 'accepted'
    orders.push({
      id,
      reference,
      name: d.name,
      customer: d.customer,
      product: d.product,
      quantity: qty,
      lineId: d.units[0]?.lineId ?? 'L1',
      status: d.status,
      createdAt,
      acceptedAt: accepted ? createdAt + Math.round(agoMin * 0.12) * 60000 : undefined,
      startedAt: started ? createdAt + Math.round(agoMin * 0.22) * 60000 : undefined,
      completedAt: d.status === 'completed' ? now() - DONE_AGO_MIN * 60000 : undefined,
      note: d.note,
    })

    d.units.forEach((u, ui) => {
      unitSeq += 1
      const label = `${reference.replace(/^OF-/, 'UP-')}-${String(ui + 1).padStart(2, '0')}`
      const beforeSupport = u.sectionId === 'SEC-MP'
      const terminal = u.state === 'completed' || u.state === 'scrap'
      const supportId = beforeSupport || terminal ? null : takeSupport()
      // Momento do último evento: concluídas/sucata mais antigas; prontas há horas;
      // em produção movidas há poucos minutos (variado por unidade).
      const recentAgo =
        u.state === 'completed'
          ? DONE_AGO_MIN
          : d.status === 'ready'
            ? 230 + ui * 9
            : u.state === 'scrap'
              ? 330 + di * 6
              : 11 + di * 9 + ui * 7
      const lastAt = Math.max(createdAt + 13 * 60000, now() - recentAgo * 60000)
      const history: Array<{ at: number; title: string; detail?: string; tone: StatusTone }> = [
        { at: createdAt, title: `Criada da encomenda ${reference}`, tone: 'info' },
      ]
      if (supportId) {
        history.push({
          at: createdAt + 12 * 60000,
          title: `Suporte ${supports.find((s) => s.id === supportId)?.code} atribuído`,
          tone: 'ok',
        })
      }
      if (u.quality === 'pending') history.push({ at: lastAt, title: 'Em análise de qualidade', tone: 'warn' })
      if (u.quality === 'recoverable') history.push({ at: lastAt, title: 'Não conformidade recuperável', tone: 'action' })
      if (u.quality === 'reconditioning')
        history.push({ at: lastAt, title: 'Enviada para recondicionamento', detail: 'Desvio controlado', tone: 'action' })
      if (u.quality === 'approved') history.push({ at: lastAt, title: 'Qualidade aprovada', tone: 'ok' })
      if (u.state === 'scrap') history.push({ at: lastAt, title: 'Marcada como sucata', tone: 'critical' })
      if (u.state === 'completed') history.push({ at: lastAt, title: 'Concluída, em expedição', tone: 'ok' })
      units.push({
        id: `U${unitSeq}`,
        label,
        product: d.product,
        orderId: id,
        lineId: u.lineId,
        sectionId: u.sectionId,
        state: u.state,
        quality: u.quality ?? 'none',
        supportId,
        updatedAt: lastAt,
        history,
      })
    })
  })

  return { orders, units, supports }
}

function seedActivity(): ActivityRecord[] {
  const min = 60 * 1000
  const entries: Array<{ ago: number; title: string; tone: StatusTone; detail?: string }> = [
    { ago: 8, title: 'UP-PAINEL-003-02 em análise de qualidade', tone: 'warn', detail: 'LINHA-04 · Controlo de qualidade' },
    { ago: 12, title: 'Encomenda Painel Exterior Standard recebida', tone: 'info', detail: 'OF-PAINEL-007' },
    { ago: 25, title: 'UP-PORTA-D-001-03 na montagem final', tone: 'info', detail: 'LINHA-04 · Montagem final' },
    { ago: 42, title: 'Encomenda Capô Standard aceite', tone: 'info', detail: '3 unidades em fila na LINHA-01 · OF-CAPO-006' },
    { ago: 70, title: 'UP-CAPO-002-02 com não conformidade recuperável', tone: 'action', detail: 'LINHA-04 · Controlo de qualidade' },
    { ago: 120, title: 'UP-CAPO-002-01 enviada para recondicionamento', tone: 'action', detail: 'LINHA-04 · Recondicionamento' },
    { ago: 240, title: 'UP-PORTA-E-004-01 aprovada, pronta para expedição', tone: 'ok', detail: 'LINHA-04 · Rack' },
    { ago: 1620, title: 'UP-TEJAD-005-02 concluída e expedida', tone: 'ok', detail: 'LINHA-04 · Expedição' },
  ]
  return entries.map((e, i) => ({
    id: (activitySeq += 1),
    at: Date.now() - e.ago * min - i * 1500,
    title: e.title,
    detail: e.detail,
    tone: e.tone,
  }))
}

export const useOperationsStore = defineStore('operations', {
  state: () => {
    const seed = buildSeed()
    return {
      lines: LINES.map((l) => ({ ...l, sections: l.sections.map((s) => ({ ...s })) })) as Line[],
      units: seed.units as Unit[],
      orders: seed.orders as Order[],
      supports: seed.supports as Support[],
      activity: seedActivity() as ActivityRecord[],
      products: SEED_PRODUCTS.map((p) => ({ ...p })) as ProductRef[],
      materials: SEED_MATERIALS.map((m) => ({ ...m })) as MaterialRef[],
      selectedUnitId: null as string | null,
    }
  },

  getters: {
    lineById(state) {
      return (id: string): Line | undefined => state.lines.find((l) => l.id === id)
    },
    unitById(state) {
      return (id: string): Unit | undefined => state.units.find((u) => u.id === id)
    },
    unitsOf(state) {
      return (lineId: string): Unit[] => state.units.filter((u) => u.lineId === lineId)
    },
    unitsInSection(state) {
      return (sectionId: string): Unit[] =>
        state.units.filter((u) => u.sectionId === sectionId && u.state !== 'completed' && u.state !== 'scrap')
    },
    wipCount(state): number {
      return state.units.filter((u) => u.state !== 'completed' && u.state !== 'scrap').length
    },
    completedCount(state): number {
      return state.units.filter((u) => u.state === 'completed').length
    },
    pendingQualityCount(state): number {
      return state.units.filter((u) => u.quality === 'pending' || u.quality === 'recoverable').length
    },
    reconditioningCount(state): number {
      return state.units.filter((u) => u.quality === 'reconditioning').length
    },
    approvedCount(state): number {
      return state.units.filter((u) => u.quality === 'approved').length
    },
    scrapCount(state): number {
      return state.units.filter((u) => u.state === 'scrap' || u.quality === 'scrap').length
    },
    statusDistribution(state): Array<{ label: string; tone: StatusTone; count: number }> {
      const bucketOf = (u: Unit): { label: string; tone: StatusTone } => {
        if (u.state === 'completed') return { label: 'Concluída', tone: 'ok' }
        if (u.state === 'scrap' || u.quality === 'scrap') return { label: 'Sucata', tone: 'critical' }
        if (u.quality === 'reconditioning') return { label: 'Recondicionamento', tone: 'action' }
        if (u.quality === 'pending' || u.quality === 'recoverable') return { label: 'Em análise de qualidade', tone: 'warn' }
        if (u.state === 'transfer') return { label: 'Em transferência', tone: 'info' }
        if (u.state === 'queued') return { label: 'Em fila', tone: 'neutral' }
        return { label: 'Em produção', tone: 'info' }
      }
      const order = ['Em fila', 'Em produção', 'Em transferência', 'Em análise de qualidade', 'Recondicionamento', 'Concluída', 'Sucata']
      const map = new Map<string, { label: string; tone: StatusTone; count: number }>()
      for (const u of state.units) {
        const b = bucketOf(u)
        const e = map.get(b.label) ?? { label: b.label, tone: b.tone, count: 0 }
        e.count += 1
        map.set(b.label, e)
      }
      return order
        .map((l) => map.get(l))
        .filter((e): e is { label: string; tone: StatusTone; count: number } => !!e && e.count > 0)
    },
    // Taxa de aprovação: aprovadas / avaliadas (unidades que já tiveram disposição de qualidade).
    approvalRate(): { rate: number; approved: number; evaluated: number } {
      const units = this.units as Unit[]
      const approved = units.filter((u) => u.quality === 'approved').length
      const evaluated = units.filter((u) => u.quality !== 'none').length
      return { rate: evaluated ? approved / evaluated : 0, approved, evaluated }
    },
    firstPassYield(): number {
      return (this.approvalRate as { rate: number }).rate
    },
    freeSupports(state): Support[] {
      return state.supports.filter((s) => s.status === 'free')
    },
    supportById(state) {
      return (id: string | null): Support | undefined =>
        id ? state.supports.find((s) => s.id === id) : undefined
    },
    // Unidades com problema de rastreabilidade: passaram a atribuição sem suporte.
    supportIssues(state): Unit[] {
      return state.units.filter((u) => supportState(u) === 'missing')
    },
    supportIssueCount(): number {
      return (this.supportIssues as Unit[]).length
    },
    wipByLine(state): Array<{ lineId: string; code: string; count: number }> {
      return state.lines.map((line) => ({
        lineId: line.id,
        code: line.code,
        count: state.units.filter((u) => u.lineId === line.id && u.state !== 'completed' && u.state !== 'scrap').length,
      }))
    },
    pendingOrders(state): Order[] {
      return state.orders.filter((o) => o.status === 'submitted')
    },
    acceptedOrders(state): Order[] {
      return state.orders.filter((o) => o.status === 'accepted')
    },
    // Contagem real de unidades por secção (sem capacidade fictícia).
    sectionWip(state) {
      return (sectionId: string): number =>
        state.units.filter((u) => u.sectionId === sectionId && u.state !== 'completed' && u.state !== 'scrap').length
    },
    maxSectionWip(state): number {
      let max = 1
      for (const line of state.lines) {
        for (const sec of line.sections) {
          const c = state.units.filter((u) => u.sectionId === sec.id && u.state !== 'completed' && u.state !== 'scrap').length
          if (c > max) max = c
        }
      }
      return max
    },
    // Secções com maior acumulação de WIP (concentração), não capacidade.
    bottlenecks(state): Array<{ lineId: string; sectionId: string; name: string; used: number }> {
      const arr: Array<{ lineId: string; sectionId: string; name: string; used: number }> = []
      for (const line of state.lines) {
        for (const sec of line.sections) {
          const used = state.units.filter((u) => u.sectionId === sec.id && u.state !== 'completed' && u.state !== 'scrap').length
          if (used >= 2) arr.push({ lineId: line.id, sectionId: sec.id, name: `${line.code} · ${sec.name}`, used })
        }
      }
      return arr.sort((a, b) => b.used - a.used).slice(0, 5)
    },
    recentActivity(state): ActivityRecord[] {
      return [...state.activity].sort((a, b) => b.at - a.at).slice(0, 12)
    },
    selectedUnit(state): Unit | null {
      return state.units.find((u) => u.id === state.selectedUnitId) ?? null
    },
    sectionOccupancy(state) {
      return (sectionId: string, excludeUnitId?: string): number =>
        state.units.filter(
          (u) => u.sectionId === sectionId && u.id !== excludeUnitId && u.state !== 'completed' && u.state !== 'scrap',
        ).length
    },
    sectionCapacity(state) {
      return (sectionId: string): number => {
        for (const l of state.lines) {
          const s = l.sections.find((x) => x.id === sectionId)
          if (s) return s.capacity
        }
        return 1
      }
    },
    isSectionFull(state) {
      return (sectionId: string, excludeUnitId?: string): boolean => {
        let cap = 1
        for (const l of state.lines) {
          const s = l.sections.find((x) => x.id === sectionId)
          if (s) {
            cap = s.capacity
            break
          }
        }
        const occ = state.units.filter(
          (u) => u.sectionId === sectionId && u.id !== excludeUnitId && u.state !== 'completed' && u.state !== 'scrap',
        ).length
        return occ >= cap
      }
    },
  },

  actions: {
    log(title: string, tone: StatusTone, detail?: string) {
      this.activity.unshift({ id: (activitySeq += 1), at: now(), title, tone, detail })
      if (this.activity.length > 80) this.activity.length = 80
    },
    selectUnit(id: string | null) {
      this.selectedUnitId = id
    },
    pushUnitEvent(unit: Unit, title: string, tone: StatusTone, detail?: string) {
      unit.history.push({ at: now(), title, detail, tone })
      unit.updatedAt = now()
    },

    /** Cliente submete encomenda — POST /api/manufacturing-orders. */
    placeOrder(input: { customer: string; product: string; quantity: number; lineId: string; note?: string; desiredDate?: string; name?: string }) {
      orderSeq += 1
      const reference = `OF-${familyCode(input.product)}-${ofNumber(orderSeq)}`
      const order: Order = {
        id: `O${orderSeq}`,
        reference,
        name: input.name?.trim() || `Encomenda ${input.product}`,
        customer: input.customer,
        product: input.product,
        quantity: Math.max(1, Math.min(50, Math.round(input.quantity))),
        lineId: input.lineId,
        status: 'submitted',
        createdAt: now(),
        note: input.note,
        desiredDate: input.desiredDate,
      }
      this.orders.unshift(order)
      this.log(`Nova encomenda · ${order.name}`, 'warn', `${order.customer} · ${order.quantity}× ${order.product} (${order.reference})`)
      return order
    },

    /** Aceitar encomenda — cria ProductUnits na 1.ª secção da linha. */
    acceptOrder(orderId: string) {
      const order = this.orders.find((o) => o.id === orderId)
      if (!order || order.status !== 'submitted') return
      order.status = 'accepted'
      order.acceptedAt = now()
      const line = findLine(this.lines, order.lineId)
      const firstSection = line?.sections[0]
      if (line && firstSection) {
        const base = order.reference.replace(/^OF-/, 'UP-')
        const existing = this.units.filter((u) => u.orderId === order.id).length
        for (let i = 0; i < order.quantity; i += 1) {
          unitSeq += 1
          this.units.push({
            id: `U${unitSeq}`,
            label: `${base}-${String(existing + i + 1).padStart(3, '0')}`,
            product: order.product,
            orderId: order.id,
            lineId: line.id,
            sectionId: firstSection.id,
            state: 'queued',
            quality: 'none',
            supportId: null,
            updatedAt: now(),
            history: [{ at: now(), title: `Criada da encomenda ${order.reference}`, tone: 'info' }],
          })
        }
      }
      this.log(`Encomenda ${order.reference} aceite`, 'info', `${order.quantity} unidades em fila na ${line?.code}`)
    },

    rejectOrder(orderId: string, reason?: string) {
      const order = this.orders.find((o) => o.id === orderId)
      if (!order || order.status !== 'submitted') return
      order.status = 'rejected'
      order.note = reason ?? order.note
      this.log(`Encomenda ${order.reference} recusada`, 'critical', reason)
    },

    cancelOrder(orderId: string) {
      const order = this.orders.find((o) => o.id === orderId)
      if (!order || order.status !== 'submitted') return
      order.status = 'cancelled'
      this.log(`Encomenda ${order.reference} cancelada pelo cliente`, 'neutral')
    },

    /** Iniciar produção (queued -> active). */
    startProduction(orderId: string) {
      const order = this.orders.find((o) => o.id === orderId)
      if (!order || order.status !== 'accepted') return
      order.status = 'in_production'
      order.startedAt = now()
      this.units
        .filter((u) => u.orderId === order.id && u.state === 'queued')
        .forEach((u) => {
          u.state = 'active'
          this.pushUnitEvent(u, 'Produção iniciada', 'info')
        })
      this.log(`Produção iniciada · ${order.reference}`, 'info', order.product)
    },

    /**
     * Garante que a unidade tem suporte ativo a partir da fase de atribuição
     * (atribuição automática controlada: usa um livre ou cria um novo). Antes da
     * atribuição não é exigido (unidade planeada).
     */
    ensureSupport(unit: Unit): boolean {
      if (unit.supportId) return true
      let idx = NOMINAL_ROUTE.findIndex((s) => s.sections.includes(unit.sectionId))
      if (idx < 0) idx = QUALITY_ZONE_INDEX
      if (idx < SUPPORT_STAGE_INDEX) return true // ainda planeada
      let sup = this.supports.find((s) => s.status === 'free')
      if (!sup) {
        const n = this.supports.length + 1
        sup = { id: `S${n}`, code: `SUP-${String(n).padStart(3, '0')}`, type: 'Palete', status: 'free' }
        this.supports.push(sup)
      }
      sup.status = 'in_use'
      unit.supportId = sup.id
      this.pushUnitEvent(unit, `Suporte ${sup.code} atribuído automaticamente`, 'ok', 'Rastreabilidade garantida')
      this.log(`${unit.label} · suporte ${sup.code} (automático)`, 'ok')
      return true
    },

    advanceUnit(unitId: string) {
      const unit = this.units.find((u) => u.id === unitId)
      if (!unit) return
      const line = findLine(this.lines, unit.lineId)
      if (!line) return
      const next = nextSection(line, unit.sectionId)
      if (!next) {
        unit.state = 'completed'
        if (unit.quality === 'none' || unit.quality === 'pending') unit.quality = 'approved'
        if (unit.supportId) {
          const sup = this.supports.find((s) => s.id === unit.supportId)
          if (sup) sup.status = 'free'
          unit.supportId = null
        }
        unit.updatedAt = now()
        this.pushUnitEvent(unit, 'Concluída na linha', 'ok')
        this.log(`${unit.label} concluída`, 'ok', `${line.code}`)
        this.completeOrderIfDone(unit.orderId)
        return
      }
      // Regra de capacidade: secção cheia → fica em espera (não sobrepõe).
      if (this.isSectionFull(next.id, unit.id)) {
        this.pushUnitEvent(unit, `Em espera para ${next.name}`, 'warn', `Secção cheia (cap. ${this.sectionCapacity(next.id)})`)
        this.log(`${unit.label} em espera · ${next.name} cheia`, 'warn', line.code)
        return
      }
      unit.sectionId = next.id
      unit.state = 'active'
      unit.updatedAt = now()
      // Atribuição de suporte: ao entrar (ou já depois) garante suporte ativo.
      this.ensureSupport(unit)
      // Ao chegar ao controlo de qualidade, fica em análise.
      if (next.id === 'SEC-CQ' && unit.quality === 'none') {
        unit.quality = 'pending'
        this.pushUnitEvent(unit, 'Chegou ao controlo de qualidade', 'warn', 'Em análise de qualidade')
      } else {
        this.pushUnitEvent(unit, `Avançou para ${next.name}`, 'info')
      }
      this.log(`${unit.label} → ${next.name}`, 'info', line.code)
    },

    /** Transferência dentro da linha — POST /api/product-unit-location-history. */
    transferUnit(unitId: string, toSectionId: string, reason?: string) {
      const unit = this.units.find((u) => u.id === unitId)
      if (!unit) return
      const line = findLine(this.lines, unit.lineId)
      const target = line?.sections.find((s) => s.id === toSectionId)
      if (!line || !target) return
      if (target.id !== unit.sectionId && this.isSectionFull(target.id, unit.id)) {
        this.log(`${unit.label}: ${target.name} cheia, transferência não permitida`, 'warn', `cap. ${this.sectionCapacity(target.id)}`)
        return
      }
      const fromName = line.sections.find((s) => s.id === unit.sectionId)?.name ?? ''
      unit.sectionId = target.id
      unit.state = 'active'
      unit.updatedAt = now()
      this.ensureSupport(unit)
      this.pushUnitEvent(unit, `Transferência ${fromName} → ${target.name}`, 'info', reason ? `Motivo: ${reason}` : undefined)
      this.log(`${unit.label} transferida → ${target.name}`, 'info', reason ?? line.code)
    },

    /**
     * Transferência entre linhas (interlinha) — tratada como DESVIO/controlo
     * operacional explícito: exige motivo e regista origem → destino no histórico.
     * EventType=LineTransfer.
     */
    transferUnitToLine(unitId: string, toLineId: string, toSectionId?: string, reason?: string) {
      const unit = this.units.find((u) => u.id === unitId)
      const target = findLine(this.lines, toLineId)
      if (!unit || !target) return
      const fromLine = findLine(this.lines, unit.lineId)
      const fromSection = fromLine?.sections.find((s) => s.id === unit.sectionId)?.name ?? ''
      const targetSection = toSectionId
        ? target.sections.find((s) => s.id === toSectionId) ?? target.sections[0]
        : target.sections[0]
      if (this.isSectionFull(targetSection.id, unit.id)) {
        this.log(`${unit.label}: ${targetSection.name} cheia, desvio não permitido`, 'warn', `cap. ${this.sectionCapacity(targetSection.id)}`)
        return
      }
      unit.lineId = target.id
      unit.sectionId = targetSection.id
      unit.state = 'transfer'
      unit.updatedAt = now()
      this.ensureSupport(unit)
      const motivo = reason?.trim() || 'Desvio operacional'
      this.pushUnitEvent(
        unit,
        `Desvio interlinha · ${fromLine?.code ?? ''} → ${target.code}`,
        'action',
        `${fromSection} → ${targetSection.name} · Motivo: ${motivo}`,
      )
      this.log(`${unit.label} · desvio ${fromLine?.code} → ${target.code}`, 'action', `${targetSection.name} · ${motivo}`)
      window.setTimeout(() => {
        if (unit.state === 'transfer') unit.state = 'active'
      }, 600)
    },

    /** Atribuir suporte a uma unidade — marco de rastreabilidade física. */
    assignSupport(unitId: string, supportId: string) {
      const unit = this.units.find((u) => u.id === unitId)
      const support = this.supports.find((s) => s.id === supportId)
      if (!unit || !support || support.status === 'in_use') return
      // liberta suporte anterior, se existir
      if (unit.supportId) {
        const prev = this.supports.find((s) => s.id === unit.supportId)
        if (prev) prev.status = 'free'
      }
      unit.supportId = support.id
      support.status = 'in_use'
      unit.updatedAt = now()
      this.pushUnitEvent(unit, `Suporte ${support.code} atribuído`, 'ok', 'WIP rastreável no chão de fábrica')
      this.log(`${unit.label} · suporte ${support.code} atribuído`, 'ok')
    },

    /** Libertar o suporte de uma unidade (regressa ao conjunto disponível). */
    releaseSupport(unitId: string) {
      const unit = this.units.find((u) => u.id === unitId)
      if (!unit || !unit.supportId) return
      const support = this.supports.find((s) => s.id === unit.supportId)
      if (support) support.status = 'free'
      const code = support?.code ?? ''
      unit.supportId = null
      unit.updatedAt = now()
      this.pushUnitEvent(unit, `Suporte ${code} libertado`, 'neutral')
      this.log(`${unit.label} · suporte ${code} libertado`, 'neutral')
    },

    /**
     * Decisão de qualidade na Linha 4 (zona de validação final).
     * approve = Aprovado · review = Em análise · recoverable = NC recuperável
     * recondition = Recondicionamento · reconditioned = recondicionamento concluído
     * scrap = Sucata
     */
    decideQuality(
      unitId: string,
      decision: 'approve' | 'review' | 'recoverable' | 'recondition' | 'reconditioned' | 'scrap',
    ) {
      const unit = this.units.find((u) => u.id === unitId)
      if (!unit) return
      const line = findLine(this.lines, unit.lineId)
      const moveTo = (sectionCode: string) => {
        const l4 = findLine(this.lines, 'L4') ?? line
        const sec = l4?.sections.find((s) => s.id === sectionCode)
        if (!l4 || !sec) return
        if (sec.id !== unit.sectionId && this.isSectionFull(sec.id, unit.id)) {
          // secção de destino cheia → mantém posição (aguarda espaço), sem sobrepor
          this.pushUnitEvent(unit, `Aguarda espaço em ${sec.name}`, 'warn', `cap. ${this.sectionCapacity(sec.id)}`)
          return
        }
        unit.lineId = l4.id
        unit.sectionId = sec.id
      }
      unit.updatedAt = now()
      // Decisões de qualidade pressupõem unidade rastreável (suporte ativo).
      if (decision !== 'scrap') this.ensureSupport(unit)
      switch (decision) {
        case 'approve':
          unit.quality = 'approved'
          unit.state = 'active'
          moveTo('SEC-RACK')
          this.pushUnitEvent(unit, 'Qualidade: Aprovado', 'ok', 'Segue para rack / expedição')
          this.log(`${unit.label} · Aprovado`, 'ok')
          break
        case 'review':
          unit.quality = 'pending'
          unit.state = 'active'
          moveTo('SEC-CQ')
          this.pushUnitEvent(unit, 'Qualidade: Em análise', 'warn')
          this.log(`${unit.label} · Em análise de qualidade`, 'warn')
          break
        case 'recoverable':
          unit.quality = 'recoverable'
          unit.state = 'active'
          moveTo('SEC-CQ')
          this.pushUnitEvent(unit, 'Qualidade: Não conformidade recuperável', 'action')
          this.log(`${unit.label} · NC recuperável`, 'action')
          break
        case 'recondition':
          unit.quality = 'reconditioning'
          unit.state = 'active'
          moveTo('SEC-RETRAB')
          this.pushUnitEvent(unit, 'Qualidade: Recondicionamento', 'action', 'Desvio controlado, fora da rota nominal')
          this.log(`${unit.label} · Recondicionamento`, 'action')
          break
        case 'reconditioned':
          unit.quality = 'approved'
          unit.state = 'active'
          moveTo('SEC-RACK')
          this.pushUnitEvent(unit, 'Recondicionamento concluído · Aprovado', 'ok', 'Regressa à rota, segue para rack')
          this.log(`${unit.label} · Recondicionamento concluído`, 'ok')
          break
        case 'scrap':
          unit.quality = 'scrap'
          unit.state = 'scrap'
          this.pushUnitEvent(unit, 'Qualidade: Sucata', 'critical')
          this.log(`${unit.label} · Sucata`, 'critical')
          this.completeOrderIfDone(unit.orderId)
          break
      }
    },

    completeOrderIfDone(orderId: string | null) {
      if (!orderId) return
      const order = this.orders.find((o) => o.id === orderId)
      if (!order || order.status === 'completed') return
      const remaining = this.units.filter(
        (u) => u.orderId === orderId && u.state !== 'completed' && u.state !== 'scrap',
      )
      if (!remaining.length) {
        order.status = 'completed'
        order.completedAt = now()
        this.log(`Encomenda ${order.reference} concluída`, 'ok', order.product)
      }
    },

    /**
     * Marca uma encomenda PRONTA como concluída/entregue (expedição).
     * Só é permitido quando a encomenda está 'ready' e não há unidades pendentes,
     * em recuperação ou por validar. Conclui as unidades, liberta os suportes,
     * regista o evento operacional e leva o progresso a 100%.
     */
    deliverOrder(orderId: string): boolean {
      const order = this.orders.find((o) => o.id === orderId)
      if (!order || order.status !== 'ready') return false
      const units = this.units.filter((u) => u.orderId === orderId)
      const blocking = units.some(
        (u) =>
          u.state !== 'completed' &&
          (u.quality === 'pending' ||
            u.quality === 'recoverable' ||
            u.quality === 'reconditioning' ||
            u.quality === 'none'),
      )
      if (blocking) {
        this.log(`Não é possível concluir ${order.name}: há unidades por validar`, 'warn', order.reference)
        return false
      }
      const expLine = findLine(this.lines, 'L4')
      const exped = expLine?.sections.find((s) => s.id === 'SEC-EXPED')
      units.forEach((u) => {
        if (u.state === 'completed') return
        if (exped) {
          u.lineId = 'L4'
          u.sectionId = exped.id
        }
        u.state = 'completed'
        if (u.supportId) {
          const sup = this.supports.find((s) => s.id === u.supportId)
          if (sup) sup.status = 'free'
          u.supportId = null
        }
        u.updatedAt = now()
        this.pushUnitEvent(u, 'Expedida / entregue ao cliente', 'ok')
      })
      order.status = 'completed'
      order.completedAt = now()
      this.log(`${order.name} concluída e expedida`, 'ok', `${order.reference} · ${units.length} unidades`)
      return true
    },

    /* ---- Gestão (admin / logística) ---- */

    /** Novo produto — POST /api/products. */
    addProduct(name: string, info?: string): ProductRef | null {
      const clean = name.trim()
      if (!clean) return null
      const product: ProductRef = { id: (productSeq += 1), name: clean, info: info?.trim() || undefined }
      this.products.push(product)
      this.log(`Novo produto: ${clean}`, 'ok')
      return product
    },

    /** Nova matéria-prima — POST /api/raw-materials. */
    addMaterial(name: string, info?: string): MaterialRef | null {
      const clean = name.trim()
      if (!clean) return null
      const material: MaterialRef = { id: (materialSeq += 1), name: clean, info: info?.trim() || undefined }
      this.materials.push(material)
      this.log(`Nova matéria-prima: ${clean}`, 'ok')
      return material
    },

    /** Nova linha — POST /api/production-lines (cria uma secção de entrada). */
    addLine(input: { code: string; name: string; group: string }): Line | null {
      const code = input.code.trim()
      const name = input.name.trim()
      if (!code || !name) return null
      lineSeq += 1
      sectionSeq += 1
      const line: Line = {
        id: `LX${lineSeq}`,
        code,
        name,
        group: input.group || 'montagem',
        sections: [section(`SEC-NEW-${sectionSeq}`, 'Entrada', 'Armazém', 1, 'entrada')],
      }
      this.lines.push(line)
      this.log(`Nova linha ${code}: ${name}`, 'ok')
      return line
    },

    /** Nova secção — POST /api/production-line-sections. */
    addSection(lineId: string, input: { name: string; type: string; zone?: string }): Section | null {
      const line = findLine(this.lines, lineId)
      const name = input.name.trim()
      if (!line || !name) return null
      sectionSeq += 1
      const sec = section(
        `SEC-NEW-${sectionSeq}`,
        name,
        input.type || 'Produção',
        line.sections.length + 1,
        input.zone?.trim() || 'fabrico',
      )
      line.sections.push(sec)
      this.log(`Nova secção em ${line.code}: ${name}`, 'ok')
      return sec
    },
  },
})
