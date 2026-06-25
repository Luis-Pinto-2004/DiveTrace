<script setup lang="ts">
import { computed, ref } from 'vue'
import {
  useOperationsStore,
  routeView,
  supportState,
  unitStatusTone,
  type Unit,
} from '@/stores/operations'
import RouteStepper from '@/components/factory/RouteStepper.vue'
import TimelineStep from '@/components/common/TimelineStep.vue'
import EmptyState from '@/components/common/EmptyState.vue'
import { formatRelativeTime } from '@/utils/format'

const ops = useOperationsStore()

// ------- Tipos do grafo de rastreabilidade --------------------------------
type NodeType =
  | 'customer'
  | 'order'
  | 'material'
  | 'unit'
  | 'support'
  | 'section'
  | 'quality'
  | 'recond'
  | 'scrap'
  | 'rack'
  | 'event'
type EdgeKind = 'belongs' | 'produces' | 'material' | 'support' | 'location' | 'quality' | 'recond' | 'scrap' | 'rack' | 'event'

interface GNode {
  id: string
  type: NodeType
  code: string
  sub?: string
  tone: string
  unit?: Unit
  fields?: Array<{ k: string; v: string }>
}
interface GEdge {
  from: string
  to: string
  kind: EdgeKind
}

const CATS: Array<{ key: NodeType; label: string }> = [
  { key: 'customer', label: 'Cliente' },
  { key: 'order', label: 'Ordem de fabrico' },
  { key: 'material', label: 'Materiais / lotes' },
  { key: 'unit', label: 'Unidades' },
  { key: 'support', label: 'Suporte' },
  { key: 'section', label: 'Secção / Linha' },
  { key: 'quality', label: 'Qualidade' },
  { key: 'recond', label: 'Recondicionamento' },
  { key: 'scrap', label: 'Sucata' },
  { key: 'rack', label: 'Rack' },
  { key: 'event', label: 'Eventos' },
]
const TYPE_LABEL: Record<NodeType, string> = {
  customer: 'Cliente',
  order: 'Ordem',
  material: 'Lote',
  unit: 'Unidade',
  support: 'Suporte',
  section: 'Secção',
  quality: 'Qualidade',
  recond: 'Recond.',
  scrap: 'Sucata',
  rack: 'Rack',
  event: 'Evento',
}

// ------- Perspetivas + seleção do elemento --------------------------------
type Perspective = 'factory' | 'unit' | 'order'
const perspective = ref<Perspective>('factory')
const focusUnitId = ref<string>('')
const focusOrderId = ref<string>('')

const orderedUnits = computed(() =>
  [...ops.units].sort((a, b) => a.label.localeCompare(b.label)),
)
const ordersWithUnits = computed(() =>
  ops.orders.filter((o) => ops.units.some((u) => u.orderId === o.id)),
)

// Categorias visíveis (materiais e eventos escondidos por omissão = secundários).
const hidden = ref<Set<NodeType>>(new Set<NodeType>(['material', 'event']))
function toggleCat(k: NodeType) {
  const s = new Set(hidden.value)
  if (s.has(k)) s.delete(k)
  else s.add(k)
  hidden.value = s
}

// ------- Materiais demo por encomenda (lotes) -----------------------------
const MAT_BY_PRODUCT: Record<string, string[]> = {
  Capô: ['Chapa de aço', 'Tinta base'],
  'Porta dianteira direita': ['Chapa de aço', 'Vedante'],
  'Porta dianteira esquerda': ['Chapa de aço', 'Vedante'],
  'Painel lateral': ['Chapa de aço', 'Tinta base'],
  Tejadilho: ['Vidro', 'Tinta base'],
}
function materialsForOrder(ref: string, product: string) {
  const suffix = ref.replace(/^OF-/, '')
  const mats = MAT_BY_PRODUCT[product] ?? ['Chapa de aço', 'Tinta']
  return mats.map((m, i) => ({ id: `mat:${suffix}:${i}`, code: `LOTE-${suffix}-${String(i + 1).padStart(2, '0')}`, name: m }))
}

function sectionInfo(sectionId: string) {
  for (const l of ops.lines) {
    const s = l.sections.find((x) => x.id === sectionId)
    if (s) return { name: s.name, lineCode: l.code }
  }
  return { name: sectionId, lineCode: '' }
}

// ------- Construção do grafo ----------------------------------------------
function buildForUnit(nodes: Map<string, GNode>, edges: GEdge[], u: Unit) {
  const order = u.orderId ? ops.orders.find((o) => o.id === u.orderId) : undefined
  const uId = `unit:${u.id}`
  nodes.set(uId, {
    id: uId,
    type: 'unit',
    code: u.label,
    sub: u.product,
    tone: unitStatusTone(u),
    unit: u,
  })
  if (order) {
    const oId = `order:${order.id}`
    nodes.set(oId, { id: oId, type: 'order', code: order.reference, sub: order.name, tone: 'info', fields: [
      { k: 'Encomenda', v: order.name },
      { k: 'Cliente', v: order.customer },
      { k: 'Quantidade', v: `${order.quantity}` },
    ] })
    edges.push({ from: oId, to: uId, kind: 'produces' })
    const cId = `customer:${order.customer}`
    nodes.set(cId, { id: cId, type: 'customer', code: order.customer, tone: 'neutral' })
    edges.push({ from: cId, to: oId, kind: 'belongs' })
    // materiais
    for (const m of materialsForOrder(order.reference, order.product)) {
      nodes.set(m.id, { id: m.id, type: 'material', code: m.code, sub: m.name, tone: 'warn', fields: [
        { k: 'Material', v: m.name },
        { k: 'Lote', v: m.code },
      ] })
      edges.push({ from: m.id, to: uId, kind: 'material' })
    }
  }
  // suporte
  if (u.supportId) {
    const sup = ops.supportById(u.supportId)
    if (sup) {
      const sId = `support:${sup.id}`
      nodes.set(sId, { id: sId, type: 'support', code: sup.code, sub: sup.type, tone: 'action', fields: [
        { k: 'Suporte', v: `${sup.code} (${sup.type})` },
        { k: 'Estado', v: sup.status === 'in_use' ? 'Em uso' : 'Livre' },
      ] })
      edges.push({ from: uId, to: sId, kind: 'support' })
    }
  }
  // secção / linha
  const si = sectionInfo(u.sectionId)
  const secId = `section:${u.id}`
  nodes.set(secId, { id: secId, type: 'section', code: si.name, sub: si.lineCode, tone: 'neutral', fields: [
    { k: 'Secção', v: si.name },
    { k: 'Linha', v: si.lineCode },
  ] })
  edges.push({ from: uId, to: secId, kind: 'location' })
  // rack
  if (u.sectionId === 'SEC-RACK') {
    const rId = `rack:${u.id}`
    nodes.set(rId, { id: rId, type: 'rack', code: 'Rack', sub: 'Armazenamento', tone: 'ok' })
    edges.push({ from: uId, to: rId, kind: 'rack' })
  }
  // qualidade / recondicionamento / sucata
  if (u.quality === 'pending' || u.quality === 'recoverable') {
    const qId = `quality:${u.id}`
    nodes.set(qId, { id: qId, type: 'quality', code: u.quality === 'pending' ? 'Em análise' : 'Recuperável', tone: 'warn' })
    edges.push({ from: uId, to: qId, kind: 'quality' })
  } else if (u.quality === 'approved') {
    const qId = `quality:${u.id}`
    nodes.set(qId, { id: qId, type: 'quality', code: 'Aprovada', tone: 'ok' })
    edges.push({ from: uId, to: qId, kind: 'quality' })
  }
  if (u.quality === 'reconditioning') {
    const id = `recond:${u.id}`
    nodes.set(id, { id, type: 'recond', code: 'Recondicionamento', tone: 'action' })
    edges.push({ from: uId, to: id, kind: 'recond' })
  }
  if (u.state === 'scrap') {
    const id = `scrap:${u.id}`
    nodes.set(id, { id, type: 'scrap', code: 'Sucata', tone: 'critical' })
    edges.push({ from: uId, to: id, kind: 'scrap' })
  }
  // eventos
  u.history.forEach((h, i) => {
    const id = `event:${u.id}:${i}`
    nodes.set(id, { id, type: 'event', code: h.title, sub: formatRelativeTime(h.at), tone: 'info' })
    edges.push({ from: uId, to: id, kind: 'event' })
  })
}

const graph = computed<{ nodes: GNode[]; edges: GEdge[] }>(() => {
  const nodes = new Map<string, GNode>()
  const edges: GEdge[] = []
  if (perspective.value === 'unit') {
    const u = ops.units.find((x) => x.id === focusUnitId.value)
    if (u) buildForUnit(nodes, edges, u)
  } else if (perspective.value === 'order') {
    const units = ops.units.filter((x) => x.orderId === focusOrderId.value)
    units.forEach((u) => buildForUnit(nodes, edges, u))
  } else {
    // fábrica: todas as encomendas com unidades
    ordersWithUnits.value.forEach((o) => {
      ops.units.filter((u) => u.orderId === o.id).forEach((u) => buildForUnit(nodes, edges, u))
    })
  }
  // filtra categorias escondidas
  const visibleNodes = Array.from(nodes.values()).filter((n) => !hidden.value.has(n.type))
  const ids = new Set(visibleNodes.map((n) => n.id))
  const visibleEdges = edges.filter((e) => ids.has(e.from) && ids.has(e.to))
  return { nodes: visibleNodes, edges: visibleEdges }
})

// ------- Layout: colunas por categoria, alinhado por faixas ---------------
const COL_W = 232
const NODE_W = 182
const NODE_H = 48
const HEAD_H = 32
const ROW_H = NODE_H + 44 // espaçamento vertical por faixa (unidade)
const ORDER_GAP = 40 // espaço extra entre encomendas
const MAT_STEP = NODE_H + 18
const MIN_GAP = NODE_H + 22 // gap mínimo garantido entre caixas na mesma coluna

const DOWNSTREAM: NodeType[] = ['support', 'section', 'quality', 'recond', 'scrap', 'rack']

const layout = computed(() => {
  const g = graph.value
  const nodeById = new Map(g.nodes.map((n) => [n.id, n]))
  // categorias visíveis -> colunas
  const present = CATS.filter((c) => g.nodes.some((n) => n.type === c.key))
  const colX = new Map<NodeType, number>()
  present.forEach((c, i) => colX.set(c.key, i * COL_W + 10))

  // encomenda de cada unidade (edge order -> unit, kind produces)
  const orderOfUnit = new Map<string, string>()
  for (const e of g.edges) if (e.kind === 'produces') orderOfUnit.set(e.to, e.from)
  // unidade dona de cada nó downstream / evento (edge unit -> node)
  const ownerUnit = new Map<string, string>()
  for (const e of g.edges) {
    if ([...DOWNSTREAM, 'event'].includes(e.kind) && nodeById.get(e.from)?.type === 'unit') {
      ownerUnit.set(e.to, e.from)
    }
  }
  // eventos por unidade
  const eventsByUnit = new Map<string, string[]>()
  for (const n of g.nodes) {
    if (n.type !== 'event') continue
    const ou = ownerUnit.get(n.id)
    if (!ou) continue
    if (!eventsByUnit.has(ou)) eventsByUnit.set(ou, [])
    eventsByUnit.get(ou)!.push(n.id)
  }

  const y = new Map<string, number>()
  const orderCenters = new Map<string, number[]>()
  let cursor = HEAD_H
  let lastOrder: string | undefined
  g.nodes
    .filter((n) => n.type === 'unit')
    .forEach((u) => {
      const ord = orderOfUnit.get(u.id)
      if (lastOrder !== undefined && ord !== lastOrder) cursor += ORDER_GAP
      lastOrder = ord
      const evs = eventsByUnit.get(u.id) ?? []
      const span = Math.max(1, evs.length)
      const bandTop = cursor
      const centerY = bandTop + (span * ROW_H) / 2 - NODE_H / 2
      y.set(u.id, centerY)
      evs.forEach((evId, i) => y.set(evId, bandTop + i * ROW_H))
      if (ord) {
        if (!orderCenters.has(ord)) orderCenters.set(ord, [])
        orderCenters.get(ord)!.push(centerY)
      }
      cursor = bandTop + span * ROW_H
    })

  // downstream alinhado ao centro da unidade dona
  for (const n of g.nodes) {
    if (DOWNSTREAM.includes(n.type)) {
      const ou = ownerUnit.get(n.id)
      if (ou && y.has(ou)) y.set(n.id, y.get(ou)!)
    }
  }
  // ordens centradas nas suas unidades
  for (const o of g.nodes) {
    if (o.type !== 'order') continue
    const cs = orderCenters.get(o.id)
    if (cs && cs.length) y.set(o.id, cs.reduce((a, b) => a + b, 0) / cs.length)
  }
  // materiais empilhados junto à encomenda (na coluna de materiais)
  const matsByOrder = new Map<string, GNode[]>()
  for (const m of g.nodes) {
    if (m.type !== 'material') continue
    const e = g.edges.find((x) => x.kind === 'material' && x.from === m.id)
    const ord = e?.to ? orderOfUnit.get(e.to) : undefined
    const key = ord ?? 'noord'
    if (!matsByOrder.has(key)) matsByOrder.set(key, [])
    matsByOrder.get(key)!.push(m)
  }
  matsByOrder.forEach((list, ord) => {
    const base = ord !== 'noord' ? y.get(ord) ?? HEAD_H : HEAD_H
    list.forEach((m, i) => y.set(m.id, base + i * MAT_STEP))
  })
  // clientes centrados nas suas encomendas (edge customer -> order, belongs)
  for (const c of g.nodes) {
    if (c.type !== 'customer') continue
    const cs = g.edges
      .filter((e) => e.kind === 'belongs' && e.from === c.id)
      .map((e) => y.get(e.to))
      .filter((v): v is number => v != null)
    if (cs.length) y.set(c.id, cs.reduce((a, b) => a + b, 0) / cs.length)
  }

  const pos = new Map<string, { x: number; y: number }>()
  for (const n of g.nodes) {
    pos.set(n.id, { x: colX.get(n.type) ?? 0, y: y.get(n.id) ?? HEAD_H })
  }

  // Compactação por coluna: garante um gap mínimo entre caixas (sem sobreposição).
  // Preserva o alinhamento por faixa quando já há espaço; só afasta onde colidiria.
  const byColumn = new Map<number, GNode[]>()
  for (const n of g.nodes) {
    const x = pos.get(n.id)!.x
    if (!byColumn.has(x)) byColumn.set(x, [])
    byColumn.get(x)!.push(n)
  }
  for (const list of byColumn.values()) {
    list.sort((a, b) => pos.get(a.id)!.y - pos.get(b.id)!.y)
    let prevY = -Infinity
    for (const n of list) {
      const p = pos.get(n.id)!
      if (p.y < prevY + MIN_GAP) p.y = prevY + MIN_GAP
      prevY = p.y
    }
  }

  let maxY = 0
  for (const p of pos.values()) maxY = Math.max(maxY, p.y + NODE_H)
  return { cols: present, pos, width: present.length * COL_W + 10, height: maxY + 20 }
})

function nodePos(id: string) {
  return layout.value.pos.get(id) ?? { x: 0, y: 0 }
}
function edgePath(e: GEdge): string {
  const a = nodePos(e.from)
  const b = nodePos(e.to)
  const x1 = a.x + NODE_W
  const y1 = a.y + NODE_H / 2
  const x2 = b.x
  const y2 = b.y + NODE_H / 2
  const dx = Math.max(28, Math.abs(x2 - x1) / 2)
  return `M ${x1} ${y1} C ${x1 + dx} ${y1}, ${x2 - dx} ${y2}, ${x2} ${y2}`
}
const EDGE_STYLE: Record<EdgeKind, { tone: string; dash: string }> = {
  belongs: { tone: 'neutral', dash: '' },
  produces: { tone: 'info', dash: '' },
  material: { tone: 'warn', dash: '5 4' },
  support: { tone: 'action', dash: '5 4' },
  location: { tone: 'info', dash: '' },
  quality: { tone: 'ok', dash: '' },
  recond: { tone: 'action', dash: '' },
  scrap: { tone: 'critical', dash: '' },
  rack: { tone: 'ok', dash: '' },
  event: { tone: 'neutral', dash: '2 4' },
}

// ------- Zoom / Pan -------------------------------------------------------
const zoom = ref(0.85)
const panX = ref(12)
const panY = ref(10)
const dragging = ref(false)
let lastX = 0
let lastY = 0
function clamp(v: number, lo: number, hi: number) {
  return Math.min(hi, Math.max(lo, v))
}
function onWheel(e: WheelEvent) {
  zoom.value = clamp(zoom.value * (e.deltaY < 0 ? 1.12 : 0.89), 0.35, 2.4)
}
function onDown(e: PointerEvent) {
  if ((e.target as Element).closest?.('.tg-node')) return
  dragging.value = true
  lastX = e.clientX
  lastY = e.clientY
}
function onMove(e: PointerEvent) {
  if (!dragging.value) return
  panX.value += e.clientX - lastX
  panY.value += e.clientY - lastY
  lastX = e.clientX
  lastY = e.clientY
}
function onUp() {
  dragging.value = false
}
function zoomIn() {
  zoom.value = clamp(zoom.value * 1.15, 0.35, 2.4)
}
function zoomOut() {
  zoom.value = clamp(zoom.value * 0.87, 0.35, 2.4)
}
function resetView() {
  zoom.value = 0.85
  panX.value = 12
  panY.value = 10
}

// ------- Seleção + realce por rato/seleção --------------------------------
const selectedId = ref<string>('')
const hoverId = ref<string>('')
const selectedNode = computed(() => graph.value.nodes.find((n) => n.id === selectedId.value) ?? null)
const activeId = computed(() => hoverId.value || selectedId.value)
const activeNeighbors = computed(() => {
  const id = activeId.value
  const s = new Set<string>()
  if (!id) return s
  s.add(id)
  for (const e of graph.value.edges) {
    if (e.from === id) s.add(e.to)
    if (e.to === id) s.add(e.from)
  }
  return s
})
function edgeIsActive(e: GEdge) {
  const id = activeId.value
  return !!id && (e.from === id || e.to === id)
}
function edgeOpacity(e: GEdge) {
  if (!activeId.value) return 0.22
  return edgeIsActive(e) ? 0.95 : 0.05
}
function nodeOpacity(n: GNode) {
  if (!activeId.value) return 1
  return activeNeighbors.value.has(n.id) ? 1 : 0.28
}
function pick(n: GNode) {
  selectedId.value = n.id
  if (n.unit) ops.selectUnit(n.unit.id)
}
const selRecentEvents = computed(() =>
  selectedNode.value?.unit ? [...selectedNode.value.unit.history].reverse().slice(0, 4) : [],
)
</script>

<template>
  <div class="tg">
    <!-- Seletor de perspetiva + filtros + zoom -->
    <div class="tg__controls">
      <div class="tg__row">
        <div class="seg">
          <button
            type="button"
            class="seg__btn"
            :class="{ 'seg__btn--on': perspective === 'factory' }"
            @click="perspective = 'factory'"
          >
            Fábrica
          </button>
          <button
            type="button"
            class="seg__btn"
            :class="{ 'seg__btn--on': perspective === 'unit' }"
            @click="perspective = 'unit'"
          >
            Unidade de produto
          </button>
          <button
            type="button"
            class="seg__btn"
            :class="{ 'seg__btn--on': perspective === 'order' }"
            @click="perspective = 'order'"
          >
            Ordem de fabrico
          </button>
        </div>

        <select
          v-if="perspective === 'unit'"
          v-model="focusUnitId"
          class="tg__select"
        >
          <option
            value=""
            disabled
          >
            Escolher unidade…
          </option>
          <option
            v-for="u in orderedUnits"
            :key="u.id"
            :value="u.id"
          >
            {{ u.label }} · {{ u.product }}
          </option>
        </select>
        <select
          v-else-if="perspective === 'order'"
          v-model="focusOrderId"
          class="tg__select"
        >
          <option
            value=""
            disabled
          >
            Escolher ordem de fabrico…
          </option>
          <option
            v-for="o in ordersWithUnits"
            :key="o.id"
            :value="o.id"
          >
            {{ o.reference }} · {{ o.name }}
          </option>
        </select>

        <div class="tg__zoom">
          <button
            type="button"
            class="zbtn"
            title="Reduzir"
            @click="zoomOut"
          >
            −
          </button>
          <button
            type="button"
            class="zbtn"
            title="Repor"
            @click="resetView"
          >
            ⟲
          </button>
          <button
            type="button"
            class="zbtn"
            title="Ampliar"
            @click="zoomIn"
          >
            +
          </button>
        </div>
      </div>

      <div class="tg__row tg__cats">
        <span class="tg__cats-label">Mostrar:</span>
        <button
          v-for="c in CATS"
          :key="c.key"
          type="button"
          class="chip chip--ghost"
          :class="{ 'chip--off': hidden.has(c.key) }"
          @click="toggleCat(c.key)"
        >
          {{ c.label }}
        </button>
      </div>
    </div>

    <div class="tg__body">
      <div class="tg__canvas">
        <svg
          v-if="graph.nodes.length"
          class="tg__svg"
          :class="{ 'is-dragging': dragging }"
          @wheel.prevent="onWheel"
          @pointerdown="onDown"
          @pointermove="onMove"
          @pointerup="onUp"
          @pointerleave="onUp"
        >
          <defs>
            <marker
              id="tg-ar"
              viewBox="0 0 10 10"
              refX="9"
              refY="5"
              markerWidth="6"
              markerHeight="6"
              orient="auto-start-reverse"
            >
              <path
                d="M 0 0 L 10 5 L 0 10 z"
                fill="var(--dt-neutral-solid)"
              />
            </marker>
          </defs>
          <g :transform="`translate(${panX} ${panY}) scale(${zoom})`">
            <rect
              :width="layout.width"
              :height="layout.height"
              fill="transparent"
            />
            <!-- cabeçalhos de categoria -->
            <text
              v-for="(c, i) in layout.cols"
              :key="c.key"
              :x="i * COL_W + 10"
              :y="16"
              class="tg-cat"
            >{{ c.label }}</text>

            <!-- ligações -->
            <path
              v-for="(e, i) in graph.edges"
              :key="i"
              class="tg-edge"
              :d="edgePath(e)"
              fill="none"
              :stroke="`var(--dt-${EDGE_STYLE[e.kind].tone}-solid)`"
              :stroke-dasharray="EDGE_STYLE[e.kind].dash"
              :stroke-width="edgeIsActive(e) ? 2.6 : e.kind === 'location' || e.kind === 'produces' ? 1.8 : 1.3"
              :opacity="edgeOpacity(e)"
              marker-end="url(#tg-ar)"
            />

            <!-- nós -->
            <g
              v-for="n in graph.nodes"
              :key="n.id"
              class="tg-node"
              :class="{ 'is-sel': n.id === selectedId }"
              :transform="`translate(${nodePos(n.id).x} ${nodePos(n.id).y})`"
              :style="{ opacity: nodeOpacity(n) }"
              @click="pick(n)"
              @mouseenter="hoverId = n.id"
              @mouseleave="hoverId = ''"
            >
              <title>{{ TYPE_LABEL[n.type] }} · {{ n.code }}</title>
              <rect
                :width="NODE_W"
                :height="NODE_H"
                rx="9"
                :fill="`var(--dt-${n.tone}-surface)`"
                :stroke="n.id === selectedId ? 'var(--dt-text-strong)' : `var(--dt-${n.tone}-border)`"
                :stroke-width="n.id === selectedId ? 2.5 : 1.4"
              />
              <rect
                v-if="n.unit && routeView(n.unit).offRoute"
                :x="-2.5"
                :y="-2.5"
                :width="NODE_W + 5"
                :height="NODE_H + 5"
                rx="11"
                fill="none"
                stroke="var(--dt-critical-solid)"
                stroke-width="1.5"
                stroke-dasharray="4 3"
              />
              <rect
                :x="0"
                :y="0"
                :width="4"
                :height="NODE_H"
                rx="2"
                :fill="`var(--dt-${n.tone}-solid)`"
              />
              <text
                :x="12"
                :y="17"
                class="tg-type"
              >{{ TYPE_LABEL[n.type] }}</text>
              <text
                :x="12"
                :y="34"
                class="tg-code"
                :fill="`var(--dt-${n.tone}-text)`"
              >{{ n.code.length > 24 ? n.code.slice(0, 23) + '…' : n.code }}</text>
              <circle
                v-if="n.unit"
                :cx="NODE_W - 12"
                :cy="14"
                r="4"
                :fill="`var(--dt-${supportState(n.unit) === 'assigned' ? 'ok' : supportState(n.unit) === 'missing' ? 'critical' : 'neutral'}-solid)`"
              />
            </g>
          </g>
        </svg>
        <EmptyState
          v-else
          class="tg__empty"
          title="Escolha o que analisar"
          description="Selecione a perspetiva (Fábrica, Unidade ou Ordem) e, se aplicável, o elemento a analisar."
          icon="◎"
        />

        <!-- legenda discreta -->
        <div
          v-if="graph.nodes.length"
          class="tg__legend"
        >
          <span class="lg"><i style="background: var(--dt-info-solid)" />Em produção</span>
          <span class="lg"><i style="background: var(--dt-ok-solid)" />Aprovado/concluído</span>
          <span class="lg"><i style="background: var(--dt-warn-solid)" />Qualidade</span>
          <span class="lg"><i style="background: var(--dt-action-solid)" />Recond./suporte</span>
          <span class="lg"><i style="background: var(--dt-critical-solid)" />Sucata</span>
          <span class="lg"><i class="ring" />Fora da rota</span>
        </div>
      </div>

      <!-- painel lateral -->
      <aside class="tg__panel">
        <template v-if="selectedNode">
          <p class="tg__panel-eyebrow">
            {{ TYPE_LABEL[selectedNode.type] }}
          </p>
          <h3 class="tg__panel-title">
            {{ selectedNode.code }}
          </h3>
          <p
            v-if="selectedNode.sub"
            class="tg__panel-meta"
          >
            {{ selectedNode.sub }}
          </p>

          <template v-if="selectedNode.unit">
            <RouteStepper :unit="selectedNode.unit" />
            <p class="tg__panel-h">
              Eventos recentes
            </p>
            <ul class="tg__panel-events">
              <TimelineStep
                v-for="(event, idx) in selRecentEvents"
                :key="idx"
                :title="event.title"
                :detail="event.detail"
                :timestamp="formatRelativeTime(event.at)"
                :tone="event.tone"
                :last="idx === selRecentEvents.length - 1"
              />
            </ul>
          </template>
          <dl
            v-else-if="selectedNode.fields && selectedNode.fields.length"
            class="tg__panel-fields"
          >
            <div
              v-for="f in selectedNode.fields"
              :key="f.k"
              class="tg__panel-field"
            >
              <dt>{{ f.k }}</dt>
              <dd>{{ f.v }}</dd>
            </div>
          </dl>
        </template>
        <EmptyState
          v-else
          title="Selecione um nó"
          description="Toque num nó do grafo para ver os detalhes completos."
          icon="◎"
        />
      </aside>
    </div>
  </div>
</template>

<style scoped>
.tg {
  display: flex;
  flex-direction: column;
  gap: 0.7rem;
}
.tg__controls {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}
.tg__row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
}
.seg {
  display: inline-flex;
  background: var(--dt-surface-2);
  border: 1px solid var(--dt-border);
  border-radius: 9999px;
  padding: 0.15rem;
}
.seg__btn {
  border: none;
  background: transparent;
  color: var(--dt-neutral-text);
  font-size: 0.72rem;
  font-weight: 800;
  padding: 0.3rem 0.7rem;
  border-radius: 9999px;
  cursor: pointer;
}
.seg__btn--on {
  background: var(--dt-brand-500);
  color: #fff;
}
.tg__select {
  border: 1px solid var(--dt-border);
  background: var(--dt-surface);
  color: var(--dt-text-strong);
  border-radius: 8px;
  padding: 0.3rem 0.55rem;
  font-size: 0.74rem;
  font-weight: 700;
  max-width: 18rem;
}
.tg__zoom {
  display: inline-flex;
  gap: 0.25rem;
  margin-left: auto;
}
.zbtn {
  width: 1.7rem;
  height: 1.7rem;
  border: 1px solid var(--dt-border);
  background: var(--dt-surface);
  color: var(--dt-text-strong);
  border-radius: 8px;
  font-weight: 900;
  cursor: pointer;
  line-height: 1;
}
.tg__cats {
  gap: 0.3rem;
}
.tg__cats-label {
  font-size: 0.68rem;
  font-weight: 800;
  color: var(--dt-neutral-text);
}
.chip {
  border: 1px solid var(--dt-border);
  background: var(--dt-surface);
  color: var(--dt-neutral-text);
  border-radius: 9999px;
  padding: 0.2rem 0.55rem;
  font-size: 0.66rem;
  font-weight: 800;
  cursor: pointer;
}
.chip--ghost {
  background: transparent;
}
.chip--off {
  opacity: 0.4;
  text-decoration: line-through;
}
.tg__body {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 16rem;
  gap: 0.8rem;
  align-items: start;
}
@media (max-width: 900px) {
  .tg__body {
    grid-template-columns: 1fr;
  }
}
.tg__canvas {
  position: relative;
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  background: var(--dt-surface);
  overflow: hidden;
  height: 580px;
}
.tg__svg {
  width: 100%;
  height: 100%;
  cursor: grab;
  touch-action: none;
  display: block;
}
.tg__svg.is-dragging {
  cursor: grabbing;
}
.tg__empty {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}
.tg-cat {
  font-size: 11px;
  font-weight: 900;
  fill: var(--dt-neutral-text);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}
.tg-node {
  cursor: pointer;
  transition: opacity 0.15s ease;
}
.tg-edge {
  transition: opacity 0.15s ease, stroke-width 0.15s ease;
}
.tg-type {
  font-size: 8.5px;
  font-weight: 800;
  fill: var(--dt-neutral-text);
  text-transform: uppercase;
  letter-spacing: 0.03em;
}
.tg-code {
  font-size: 11px;
  font-weight: 800;
}
.tg__legend {
  position: absolute;
  left: 0.5rem;
  bottom: 0.5rem;
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem 0.6rem;
  padding: 0.35rem 0.5rem;
  background: color-mix(in srgb, var(--dt-surface) 88%, transparent);
  border: 1px solid var(--dt-border);
  border-radius: 10px;
}
.lg {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.6rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
}
.lg i {
  width: 0.55rem;
  height: 0.55rem;
  border-radius: 3px;
  display: inline-block;
}
.lg i.ring {
  background: transparent;
  border: 1.5px dashed var(--dt-critical-solid);
}
.tg__panel {
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  background: var(--dt-surface);
  padding: 0.85rem;
  position: sticky;
  top: 5rem;
}
.tg__panel-eyebrow {
  margin: 0;
  font-size: 0.62rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: var(--dt-neutral-text);
}
.tg__panel-title {
  margin: 0.1rem 0 0;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
  overflow-wrap: break-word;
}
.tg__panel-meta {
  margin: 0.15rem 0 0.6rem;
  font-size: 0.7rem;
  color: var(--dt-neutral-text);
  overflow-wrap: break-word;
}
.tg__panel-h {
  margin: 0.85rem 0 0.3rem;
  font-size: 0.7rem;
  font-weight: 800;
  color: var(--dt-text-strong);
}
.tg__panel-events {
  list-style: none;
  margin: 0;
  padding: 0;
}
.tg__panel-fields {
  margin: 0.4rem 0 0;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.tg__panel-field {
  display: flex;
  flex-direction: column;
  gap: 0.05rem;
}
.tg__panel-field dt {
  font-size: 0.62rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.03em;
  color: var(--dt-neutral-text);
}
.tg__panel-field dd {
  margin: 0;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--dt-text-strong);
  overflow-wrap: break-word;
}
</style>
