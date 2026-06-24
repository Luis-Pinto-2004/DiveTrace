<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue'
import { MarkerType, Position, VueFlow, useVueFlow, type Edge, type Node } from '@vue-flow/core'
import '@vue-flow/core/dist/style.css'
import '@vue-flow/core/dist/theme-default.css'
import { api, getApiErrorMessage } from '../services/api'

type GraphMode = 'factory' | 'product-unit' | 'order'
type GraphDetailMode = 'simple' | 'technical'

const props = defineProps<{
  initialProductUnitId?: number | null
  initialOrderId?: number | null
}>()

type TraceGraphNodeDto = {
  id: string
  type: string
  label: string
  subtitle?: string
  status?: string
  severity: string
  group?: string
  metadata: Record<string, unknown>
}

type TraceGraphEdgeDto = {
  id: string
  source: string
  target: string
  type: string
  label: string
  timestamp?: string
  severity: string
  metadata: Record<string, unknown>
}

type TraceGraphSummaryDto = {
  status: string
  currentLine?: string
  currentSection?: string
  qualityStatus?: string
  hasOpenIssues: boolean
  metrics: Record<string, unknown>
  recommendation: string
}

type TraceGraphLegendItemDto = {
  type: string
  label: string
  color: string
  description: string
}

type TraceGraphWarningDto = {
  code: string
  message: string
  severity: string
}

type TraceGraphDto = {
  scope: string
  title: string
  summary: TraceGraphSummaryDto
  nodes: TraceGraphNodeDto[]
  edges: TraceGraphEdgeDto[]
  legend: TraceGraphLegendItemDto[]
  warnings: TraceGraphWarningDto[]
}

type TraceGraphOptionDto = {
  id: number
  code: string
  label: string
  status?: string
  group?: string
}

type TraceGraphOptionsDto = {
  productUnits: TraceGraphOptionDto[]
  manufacturingOrders: TraceGraphOptionDto[]
  lines: TraceGraphOptionDto[]
  statuses: string[]
  qualityStatuses: string[]
  nodeTypes: string[]
  supportedModes: string[]
}

type FlowNodeData = {
  graphNode: TraceGraphNodeDto
  compact: boolean
  selected: boolean
}

type FlowNode = Node<FlowNodeData>
type FlowEdge = Edge

const modeLabels: Record<GraphMode, string> = {
  factory: 'Chão de fábrica',
  'product-unit': 'Rota da unidade',
  order: 'Ordem de fabrico',
}

const metadataLabels: Record<string, string> = {
  activeSupports: 'Suportes ativos',
  allowsLineTransferIn: 'Aceita transferência de linha',
  allowsLineTransferOut: 'Permite transferência de linha',
  associationType: 'Tipo de associação',
  blockedUnits: 'Unidades bloqueadas',
  currentSectionId: 'Secção atual',
  currentSupportId: 'Suporte atual',
  customerCode: 'Código do cliente',
  displayOrder: 'Ordem visual',
  dueDate: 'Data prevista',
  eventType: 'Tipo de evento',
  isTransferPoint: 'Ponto de transferência',
  isReconditioned: 'Recondicionada',
  qualityDisposition: 'Disposição de qualidade',
  recoveryStatus: 'Estado de recuperação',
  reconditionReason: 'Motivo de recondicionamento',
  reconditionedAt: 'Recondicionada em',
  layoutColumn: 'Coluna',
  layoutRow: 'Linha',
  lineId: 'ID da linha',
  lineTransfers: 'Transferências de linha',
  lotNumber: 'Lote',
  materialName: 'Material',
  movementAt: 'Movimento em',
  orderId: 'ID da ordem',
  plannedQuantity: 'Quantidade planeada',
  product: 'Produto',
  publicTrackingCode: 'Código público',
  qualityStatus: 'Estado de qualidade',
  quantity: 'Quantidade',
  recordedAt: 'Registado em',
  routeState: 'Estado da rota',
  sectionCode: 'Código da secção',
  sectionId: 'ID da secção',
  sectionType: 'Tipo de secção',
  source: 'Origem',
  supportCode: 'Código do suporte',
  targetSectionId: 'Secção destino',
  unitId: 'ID da unidade',
  unitType: 'Tipo de unidade',
  wipUnits: 'Unidades WIP',
}

const graph = ref<TraceGraphDto | null>(null)
const options = ref<TraceGraphOptionsDto | null>(null)
const loading = ref(false)
const errorMessage = ref('')
const activeMode = ref<GraphMode>('factory')
const selectedProductUnitId = ref<number | null>(null)
const selectedOrderId = ref<number | null>(null)
const selectedNodeId = ref<string | null>(null)
const selectedLine = ref('all')
const selectedStatus = ref('all')
const selectedQuality = ref('all')
const selectedNodeType = ref('all')
const compactMode = ref(false)
const displayMode = ref<GraphDetailMode>('simple')
const showEvents = ref(true)
const showMaterials = ref(true)
const focusedSectionId = ref<number | null>(null)
let ready = false

const { fitView, setViewport } = useVueFlow('trace-graph')

const availableModes = computed(() => {
  const supported = new Set(options.value?.supportedModes ?? ['factory', 'product-unit', 'order'])
  return (Object.keys(modeLabels) as GraphMode[]).filter((mode) => supported.has(mode))
})

const canSelectProductUnit = computed(() => availableModes.value.includes('product-unit') && (options.value?.productUnits.length ?? 0) > 0)
const canSelectOrder = computed(() => availableModes.value.includes('order') && (options.value?.manufacturingOrders.length ?? 0) > 0)

const selectedNode = computed(() => graph.value?.nodes.find((node) => node.id === selectedNodeId.value) ?? null)

const selectedNodeEdges = computed(() => {
  if (!selectedNode.value || !graph.value) return []
  return graph.value.edges.filter((edge) => edge.source === selectedNode.value?.id || edge.target === selectedNode.value?.id)
})

const visibleGraphNodes = computed(() => {
  if (!graph.value) return []
  return graph.value.nodes.filter((node) => {
    if (displayMode.value === 'simple' && technicalNodeTypes.has(node.type)) return false
    if (!showEvents.value && node.type === 'Event') return false
    if (!showMaterials.value && node.type === 'MaterialLot') return false
    if (selectedNodeType.value !== 'all' && node.type !== selectedNodeType.value) return false
    if (selectedStatus.value !== 'all' && node.status !== selectedStatus.value) return false
    if (selectedQuality.value !== 'all' && String(node.metadata.qualityStatus ?? node.status ?? '') !== selectedQuality.value) return false
    if (selectedLine.value !== 'all' && !nodeBelongsToLine(node, selectedLine.value)) return false
    if (focusedSectionId.value && !nodeBelongsToFocusedSection(node, focusedSectionId.value)) return false
    return true
  })
})

const visibleNodeIds = computed(() => new Set(visibleGraphNodes.value.map((node) => node.id)))

const visibleGraphEdges = computed(() => {
  if (!graph.value) return []
  return graph.value.edges.filter((edge) => visibleNodeIds.value.has(edge.source) && visibleNodeIds.value.has(edge.target))
})

const flowNodes = computed<FlowNode[]>(() => {
  const positions = buildLayout(visibleGraphNodes.value, graph.value?.edges ?? [], activeMode.value)
  return visibleGraphNodes.value.map((node) => ({
    id: node.id,
    type: 'trace',
    position: positions.get(node.id) ?? { x: 0, y: 0 },
    data: {
      graphNode: node,
      compact: compactMode.value,
      selected: selectedNodeId.value === node.id,
    },
    draggable: false,
    connectable: false,
    selectable: true,
    focusable: true,
    sourcePosition: Position.Right,
    targetPosition: Position.Left,
    class: ['trace-flow-node', `trace-flow-node-${node.type}`, `trace-flow-node-${node.severity}`],
    ariaLabel: `${nodeTypeLabel(node.type)} ${node.label}`,
  }))
})

const flowEdges = computed<FlowEdge[]>(() => {
  return visibleGraphEdges.value.map((edge) => ({
    id: edge.id,
    source: edge.source,
    target: edge.target,
    type: 'smoothstep',
    label: compactMode.value ? undefined : edge.label,
    animated: ['movement', 'line-transfer', 'current-location', 'transfer'].includes(edge.type),
    markerEnd: { type: MarkerType.ArrowClosed, color: edgeColor(edge.severity), width: 16, height: 16 },
    style: {
      stroke: edgeColor(edge.severity),
      strokeWidth: edge.severity === 'warning' || edge.type === 'line-transfer' ? 3 : 2,
      strokeDasharray: edge.type === 'route' || edge.severity === 'history' ? '7 6' : undefined,
    },
    labelStyle: {
      fill: '#334155',
      fontWeight: 800,
      fontSize: 11,
    },
    labelBgStyle: {
      fill: '#ffffff',
      fillOpacity: 0.86,
    },
    data: edge,
  }))
})

const metricEntries = computed(() => {
  const metrics = graph.value?.summary.metrics ?? {}
  return Object.entries(metrics).map(([key, value]) => ({
    key,
    label: metricLabel(key),
    value: metricValue(value),
  }))
})

const selectedMetadataEntries = computed(() => {
  const metadata = selectedNode.value?.metadata ?? {}
  return Object.entries(metadata)
    .filter((entry) => entry[1] !== null && entry[1] !== undefined && entry[1] !== '')
    .slice(0, 18)
    .map(([key, value]) => ({ key, label: metadataLabels[key] ?? splitCamelCase(key), value: metricValue(value) }))
})

const technicalNodeTypes = new Set(['Customer', 'ManufacturingOrder', 'Product', 'MaterialLot', 'Event'])

const graphDecision = computed(() => {
  const summary = graph.value?.summary
  if (!summary) {
    return {
      state: 'Sem mapa carregado',
      attention: 'Selecione modo, unidade ou ordem para carregar o mapa.',
      action: 'Atualizar mapa',
      evidence: 'Sem métricas disponíveis.',
      tone: 'trace-decision-muted',
    }
  }

  const topMetric = metricEntries.value[0]
  if (summary.hasOpenIssues) {
    return {
      state: statusLabel(summary.status),
      attention: summary.qualityStatus ? `Qualidade: ${statusLabel(summary.qualityStatus)}` : 'Existem alertas operacionais.',
      action: summary.recommendation,
      evidence: topMetric ? `${topMetric.label}: ${topMetric.value}` : `${visibleGraphNodes.value.length} nós visíveis`,
      tone: 'trace-decision-danger',
    }
  }

  return {
    state: statusLabel(summary.status),
    attention: summary.currentSection || summary.currentLine || 'Fluxo operacional estável.',
    action: summary.recommendation || 'Manter monitorização operacional.',
    evidence: topMetric ? `${topMetric.label}: ${topMetric.value}` : `${visibleGraphNodes.value.length} nós visíveis`,
    tone: 'trace-decision-ok',
  }
})

onMounted(async () => {
  await loadOptions()
  ready = true
  await loadGraph()
})

watch([activeMode, selectedProductUnitId, selectedOrderId], async () => {
  if (!ready) return
  await loadGraph()
})

watch(
  () => props.initialProductUnitId,
  async (unitId) => {
    if (!unitId || !ready) return
    selectedProductUnitId.value = unitId
    activeMode.value = 'product-unit'
    await loadGraph()
  },
)

watch(
  () => props.initialOrderId,
  async (orderId) => {
    if (!orderId || !ready) return
    selectedOrderId.value = orderId
    activeMode.value = 'order'
    await loadGraph()
  },
)

watch([selectedLine, selectedStatus, selectedQuality, selectedNodeType, showEvents, showMaterials, compactMode, displayMode, focusedSectionId], () => {
  void fitGraph()
})

async function loadOptions() {
  errorMessage.value = ''
  try {
    const response = await api.get<TraceGraphOptionsDto>('/trace-graph/options')
    options.value = response.data
    selectedProductUnitId.value = props.initialProductUnitId ?? response.data.productUnits[0]?.id ?? null
    selectedOrderId.value = props.initialOrderId ?? response.data.manufacturingOrders[0]?.id ?? null
    if (!response.data.supportedModes.includes(activeMode.value)) {
      activeMode.value = (response.data.supportedModes[0] as GraphMode | undefined) ?? 'factory'
    }
  } catch (error) {
    errorMessage.value = permissionAwareError(error)
  }
}

async function loadGraph() {
  loading.value = true
  errorMessage.value = ''
  selectedNodeId.value = null
  try {
    const path = graphPath()
    if (!path) {
      graph.value = null
      return
    }
    const response = await api.get<TraceGraphDto>(path)
    graph.value = response.data
    syncFiltersWithGraph()
    await fitGraph()
  } catch (error) {
    graph.value = null
    errorMessage.value = permissionAwareError(error)
  } finally {
    loading.value = false
  }
}

function graphPath() {
  if (activeMode.value === 'factory') return '/trace-graph/factory'
  if (activeMode.value === 'product-unit') return selectedProductUnitId.value ? `/trace-graph/product-unit/${selectedProductUnitId.value}` : ''
  return selectedOrderId.value ? `/trace-graph/manufacturing-order/${selectedOrderId.value}` : ''
}

function setMode(mode: GraphMode) {
  if (!availableModes.value.includes(mode)) return
  activeMode.value = mode
  focusedSectionId.value = null
}

function setDisplayMode(mode: GraphDetailMode) {
  displayMode.value = mode
  if (mode === 'simple') {
    showEvents.value = false
    showMaterials.value = false
    selectedNodeType.value = 'all'
  } else {
    showEvents.value = true
    showMaterials.value = true
  }
}

function selectNode(node: TraceGraphNodeDto) {
  const unitId = getNumber(node.metadata, 'unitId')
  if (activeMode.value === 'factory' && node.type === 'ProductUnit' && unitId) {
    selectedProductUnitId.value = unitId
    activeMode.value = 'product-unit'
    return
  }

  selectedNodeId.value = node.id
  if (activeMode.value === 'factory' && node.type === 'Section') {
    focusedSectionId.value = getNumber(node.metadata, 'sectionId')
  }
}

function openSelectedProductUnit() {
  const unitId = selectedNode.value ? getNumber(selectedNode.value.metadata, 'unitId') : null
  if (!unitId) return
  selectedProductUnitId.value = unitId
  activeMode.value = 'product-unit'
}

function clearSelection() {
  selectedNodeId.value = null
}

function clearFocusedSection() {
  focusedSectionId.value = null
}

async function fitGraph() {
  await nextTick()
  window.setTimeout(() => {
    void fitView({ padding: 0.18, minZoom: 0.28, maxZoom: 1.25, duration: 240 })
  }, 30)
}

function resetViewport() {
  void setViewport({ x: 0, y: 0, zoom: 0.82 }, { duration: 220 })
}

function syncFiltersWithGraph() {
  if (!graph.value) return
  const nodeTypes = new Set(graph.value.nodes.map((node) => node.type))
  if (selectedNodeType.value !== 'all' && !nodeTypes.has(selectedNodeType.value)) selectedNodeType.value = 'all'
  const statuses = new Set(graph.value.nodes.map((node) => node.status).filter(Boolean))
  if (selectedStatus.value !== 'all' && !statuses.has(selectedStatus.value)) selectedStatus.value = 'all'
  if (activeMode.value !== 'factory') selectedLine.value = 'all'
}

function nodeBelongsToLine(node: TraceGraphNodeDto, lineCode: string) {
  if (node.type === 'ProductionLine') return node.subtitle === lineCode || node.group === lineCode
  return node.group === lineCode || String(node.metadata.lineCode ?? '') === lineCode
}

function nodeBelongsToFocusedSection(node: TraceGraphNodeDto, sectionId: number) {
  if (getNumber(node.metadata, 'sectionId') === sectionId) return true
  if (getNumber(node.metadata, 'currentSectionId') === sectionId) return true
  if (getNumber(node.metadata, 'targetSectionId') === sectionId) return true
  if (node.type === 'ProductionLine' && graph.value) {
    const sectionNode = graph.value.nodes.find((candidate) => getNumber(candidate.metadata, 'sectionId') === sectionId)
    return graph.value.edges.some((edge) => edge.source === node.id && edge.target === sectionNode?.id)
  }
  return false
}

function buildLayout(nodes: TraceGraphNodeDto[], edges: TraceGraphEdgeDto[], mode: GraphMode) {
  return mode === 'factory'
    ? buildFactoryLayout(nodes, edges)
    : buildTraceLayout(nodes)
}

function buildFactoryLayout(nodes: TraceGraphNodeDto[], edges: TraceGraphEdgeDto[]) {
  const positions = new Map<string, { x: number; y: number }>()

  // Espaçamentos generosos para evitar caixas e ligações sobrepostas.
  const LINE_GAP = 460 // distância vertical entre linhas
  const COL_X0 = 220 // x da 1.ª secção
  const COL_GAP = 280 // distância horizontal entre secções/colunas
  const UNIT_TOP = 104 // deslocamento da 1.ª unidade abaixo da secção
  const UNIT_GAP = 80 // distância vertical entre unidades
  const SUP_GAP = 56 // distância vertical entre suportes
  const OUTCOME_GAP = 96

  const lineNodes = nodes.filter((node) => node.type === 'ProductionLine').sort(sortByDisplay)
  const sectionNodes = nodes.filter((node) => node.type === 'Section').sort(sortByDisplay)
  const sectionPositions = new Map<number, { x: number; y: number }>()
  const sectionStackBottom = new Map<number, number>()
  const lineRows = new Map<string, number>()

  lineNodes.forEach((node, index) => {
    const y = index * LINE_GAP
    lineRows.set(node.group ?? node.subtitle ?? node.id, y)
    positions.set(node.id, { x: 0, y })
  })

  sectionNodes.forEach((node, index) => {
    const lineKey = node.group ?? ''
    const baseY = lineRows.get(lineKey) ?? Math.floor(index / 5) * LINE_GAP
    const column = getNumber(node.metadata, 'layoutColumn') ?? (index % 6) + 1
    const position = { x: COL_X0 + column * COL_GAP, y: baseY }
    const sectionId = getNumber(node.metadata, 'sectionId')
    if (sectionId) {
      sectionPositions.set(sectionId, position)
      sectionStackBottom.set(sectionId, position.y + UNIT_TOP)
    }
    positions.set(node.id, position)
  })

  // Unidades empilhadas SOB a respetiva secção (mesma coluna).
  const unitsBySection = new Map<number, TraceGraphNodeDto[]>()
  nodes
    .filter((node) => node.type === 'ProductUnit')
    .forEach((node) => {
      const sectionId = getNumber(node.metadata, 'currentSectionId') ?? 0
      if (!unitsBySection.has(sectionId)) unitsBySection.set(sectionId, [])
      unitsBySection.get(sectionId)?.push(node)
    })
  unitsBySection.forEach((unitNodes, sectionId) => {
    const anchor = sectionPositions.get(sectionId) ?? { x: COL_X0, y: 0 }
    let y = anchor.y + UNIT_TOP
    unitNodes
      .sort((a, b) => a.label.localeCompare(b.label))
      .forEach((node) => {
        positions.set(node.id, { x: anchor.x, y })
        y += UNIT_GAP
      })
    sectionStackBottom.set(sectionId, y)
  })

  // Suportes empilhados ABAIXO das unidades da mesma secção (mesma coluna).
  const supportsBySection = new Map<number, TraceGraphNodeDto[]>()
  nodes
    .filter((node) => node.type === 'Support')
    .forEach((node) => {
      const sectionId = getNumber(node.metadata, 'currentSectionId') ?? 0
      if (!supportsBySection.has(sectionId)) supportsBySection.set(sectionId, [])
      supportsBySection.get(sectionId)?.push(node)
    })
  supportsBySection.forEach((supNodes, sectionId) => {
    const anchor = sectionPositions.get(sectionId)
    let y = (sectionStackBottom.get(sectionId) ?? (anchor?.y ?? 0) + UNIT_TOP) + 14
    supNodes.forEach((node, index) => {
      if (anchor) {
        positions.set(node.id, { x: anchor.x, y })
        y += SUP_GAP
      } else {
        positions.set(node.id, { x: 980, y: index * SUP_GAP })
      }
    })
  })

  // Coluna dedicada para racks, à direita do fluxo.
  const maxSectionX = Array.from(sectionPositions.values()).reduce((m, p) => Math.max(m, p.x), COL_X0)
  const rackX = maxSectionX + COL_GAP
  nodes
    .filter((node) => node.type === 'Rack')
    .forEach((node, index) => {
      positions.set(node.id, { x: rackX, y: index * OUTCOME_GAP })
    })

  // Resultados de qualidade / recondicionamento / sucata / eventos: colunas
  // próprias mais à direita, agrupadas por tipo (separadas do fluxo principal).
  const outcomeCol: Record<string, number> = {
    Quality: 0,
    Nonconformity: 1,
    Scrap: 1,
    Rework: 2,
    ReconditionRecord: 2,
    Event: 3,
  }
  const outcomeNextY = new Map<number, number>()
  nodes.forEach((node) => {
    if (positions.has(node.id)) return
    const col = outcomeCol[node.type]
    if (col === undefined) return
    const x = rackX + COL_GAP + col * COL_GAP
    const y = outcomeNextY.get(col) ?? 0
    positions.set(node.id, { x, y })
    outcomeNextY.set(col, y + OUTCOME_GAP)
  })

  // Resto (fallback): junto a um vizinho com afastamento, ou numa coluna final.
  let fallbackY = 0
  nodes.forEach((node) => {
    if (positions.has(node.id)) return
    const edge = edges.find((candidate) => candidate.target === node.id || candidate.source === node.id)
    const neighbor = edge ? positions.get(edge.source) ?? positions.get(edge.target) : null
    if (neighbor) {
      positions.set(node.id, { x: neighbor.x + COL_GAP, y: neighbor.y + 70 })
    } else {
      positions.set(node.id, { x: rackX + COL_GAP * 5, y: fallbackY })
      fallbackY += OUTCOME_GAP
    }
  })

  return positions
}

function buildTraceLayout(nodes: TraceGraphNodeDto[]) {
  const positions = new Map<string, { x: number; y: number }>()
  const sections = nodes.filter((node) => node.type === 'Section').sort(sortByDisplay)
  const sectionRank = new Map(sections.map((node, index) => [node.id, index]))
  const rankByType: Record<string, number> = {
    Customer: 0,
    ManufacturingOrder: 0,
    Product: 1,
    ProductUnit: 2,
    Section: 3,
    Support: 4,
    Rack: 5,
    MaterialLot: 5,
    Quality: 6,
    Nonconformity: 7,
    Rework: 7,
    ReconditionRecord: 8,
    Scrap: 7,
    Event: 9,
  }
  const buckets = new Map<number, TraceGraphNodeDto[]>()
  nodes.forEach((node) => {
    const rank = node.type === 'Section' ? 3 + (sectionRank.get(node.id) ?? 0) : rankByType[node.type] ?? 9
    if (!buckets.has(rank)) buckets.set(rank, [])
    buckets.get(rank)?.push(node)
  })

  Array.from(buckets.entries()).sort((a, b) => a[0] - b[0]).forEach(([rank, bucket]) => {
    const x = rank * 280
    bucket.sort(sortByDisplay).forEach((node, index) => {
      positions.set(node.id, { x, y: index * 122 })
    })
  })

  return positions
}

function sortByDisplay(a: TraceGraphNodeDto, b: TraceGraphNodeDto) {
  return (getNumber(a.metadata, 'displayOrder') ?? getNumber(a.metadata, 'layoutColumn') ?? 999)
    - (getNumber(b.metadata, 'displayOrder') ?? getNumber(b.metadata, 'layoutColumn') ?? 999)
    || a.label.localeCompare(b.label)
}

function getNumber(metadata: Record<string, unknown>, key: string) {
  const value = metadata[key]
  return typeof value === 'number' ? value : null
}

function permissionAwareError(error: unknown) {
  const message = getApiErrorMessage(error)
  return message.includes('403') ? 'Não tem permissão para visualizar este mapa de rastreabilidade.' : message
}

function nodeTypeLabel(type: string) {
  return {
    Customer: 'Cliente',
    Event: 'Evento',
    ManufacturingOrder: 'Ordem',
    MaterialLot: 'Lote',
    Nonconformity: 'NC',
    Product: 'Produto',
    ProductUnit: 'Unidade',
    ProductionLine: 'Linha',
    Quality: 'Qualidade',
    Rack: 'Rack',
    ReconditionRecord: 'Recondicionamento',
    Rework: 'Retrabalho',
    Scrap: 'Sucata',
    Section: 'Secção',
    Support: 'Suporte',
  }[type] ?? type
}

function metricLabel(key: string) {
  return {
    blockedUnits: 'Unidades bloqueadas',
    completedUnits: 'Unidades concluídas',
    currentSection: 'Secção atual',
    fiwareEntities: 'Entidades FIWARE',
    lineTransfers: 'Transferências',
    materialLots: 'Lotes materiais',
    openIssues: 'Problemas abertos',
    occupiedRacks: 'Racks ocupadas',
    plannedQuantity: 'Planeadas',
    productUnits: 'Unidades',
    qualityIssues: 'Problemas qualidade',
    reconditionRecords: 'Registos de recondicionamento',
    reconditionedUnits: 'Unidades recondicionadas',
    recoveryCandidates: 'Candidatas a recuperação',
    reworkUnits: 'Unidades em retrabalho',
    routeSteps: 'Etapas de rota',
    topWipSection: 'Secção com maior WIP',
    unitsInFlow: 'Unidades em fluxo',
    wipUnits: 'WIP',
  }[key] ?? splitCamelCase(key)
}

function metricValue(value: unknown) {
  if (value === null || value === undefined || value === '') return 'n/d'
  if (typeof value === 'boolean') return value ? 'Sim' : 'Não'
  if (typeof value === 'number') return Intl.NumberFormat('pt-PT').format(value)
  if (typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(value)) return new Intl.DateTimeFormat('pt-PT', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value))
  if (Array.isArray(value)) return value.join(', ')
  return String(value)
}

function splitCamelCase(value: string) {
  return value.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/^./, (letter) => letter.toUpperCase())
}

function statusLabel(status?: string) {
  if (!status) return 'Sem estado'
  return {
    Active: 'Ativo',
    Available: 'Disponível',
    Blocked: 'Bloqueado',
    Completed: 'Concluído',
    FAIL: 'Reprovado',
    'In Progress': 'Em progresso',
    Loaded: 'Carregado',
    PASS: 'Aprovado',
    Pending: 'Pendente',
    Candidate: 'Candidata',
    InRecovery: 'Em recuperação',
    Reconditioned: 'Recondicionada',
    Recoverable: 'Recuperável',
    Rejected: 'Rejeitada',
    Rework: 'Retrabalho',
    Scrap: 'Sucata',
    Stored: 'Armazenado',
  }[status] ?? status
}

function severityLabel(severity: string) {
  return {
    attention: 'Atenção',
    critical: 'Crítico',
    history: 'Histórico',
    normal: 'Normal',
    ok: 'OK',
    warning: 'Alerta',
  }[severity] ?? severity
}

function edgeColor(severity: string) {
  return {
    attention: '#f59e0b',
    critical: '#991b1b',
    history: '#94a3b8',
    normal: '#64748b',
    ok: '#059669',
    warning: '#dc2626',
  }[severity] ?? '#64748b'
}

function nodeTooltip(node: TraceGraphNodeDto) {
  return [nodeTypeLabel(node.type), node.label, node.subtitle, statusLabel(node.status)].filter(Boolean).join(' - ')
}
</script>

<template>
  <section class="trace-graph-page">
    <div class="trace-graph-toolbar">
      <div
        class="trace-mode-group"
        aria-label="Modo de visualização"
      >
        <button
          v-for="mode in availableModes"
          :key="mode"
          type="button"
          class="trace-mode-button"
          :class="{ 'trace-mode-button-active': activeMode === mode }"
          @click="setMode(mode)"
        >
          {{ modeLabels[mode] }}
        </button>
      </div>

      <div class="trace-toolbar-fields">
        <label
          v-if="activeMode === 'product-unit'"
          class="trace-field"
        >
          <span>Unidade</span>
          <select
            v-model.number="selectedProductUnitId"
            class="form-input"
            :disabled="!canSelectProductUnit"
          >
            <option
              v-for="unit in options?.productUnits ?? []"
              :key="unit.id"
              :value="unit.id"
            >{{ unit.label }}</option>
          </select>
        </label>

        <label
          v-if="activeMode === 'order'"
          class="trace-field"
        >
          <span>Ordem</span>
          <select
            v-model.number="selectedOrderId"
            class="form-input"
            :disabled="!canSelectOrder"
          >
            <option
              v-for="order in options?.manufacturingOrders ?? []"
              :key="order.id"
              :value="order.id"
            >{{ order.label }}</option>
          </select>
        </label>

        <div
          class="trace-mode-group trace-detail-mode"
          aria-label="Detalhe do mapa"
        >
          <button
            type="button"
            class="trace-mode-button"
            :class="{ 'trace-mode-button-active': displayMode === 'simple' }"
            @click="setDisplayMode('simple')"
          >
            Simples
          </button>
          <button
            type="button"
            class="trace-mode-button"
            :class="{ 'trace-mode-button-active': displayMode === 'technical' }"
            @click="setDisplayMode('technical')"
          >
            Técnico
          </button>
        </div>
        <button
          type="button"
          class="btn-secondary"
          @click="fitGraph"
        >
          Ajustar à vista
        </button>
        <button
          type="button"
          class="btn-secondary"
          @click="resetViewport"
        >
          Recentrar
        </button>
        <button
          type="button"
          class="btn-primary"
          @click="loadGraph"
        >
          Atualizar
        </button>
      </div>
    </div>

    <div
      class="trace-decision-panel"
      :class="graphDecision.tone"
    >
      <div>
        <span>Estado operacional</span>
        <strong>{{ graphDecision.state }}</strong>
      </div>
      <div>
        <span>Principal atenção</span>
        <strong>{{ graphDecision.attention }}</strong>
      </div>
      <div>
        <span>Ação recomendada</span>
        <strong>{{ graphDecision.action }}</strong>
      </div>
      <div>
        <span>Evidência</span>
        <strong>{{ graphDecision.evidence }}</strong>
      </div>
    </div>

    <div
      v-if="graph"
      class="trace-summary-grid"
    >
      <div
        class="trace-summary-card trace-summary-state"
        :class="graph.summary.hasOpenIssues ? 'trace-summary-alert' : 'trace-summary-ok'"
      >
        <span>Estado operacional</span>
        <strong>{{ statusLabel(graph.summary.status) }}</strong>
        <p>{{ graph.summary.recommendation }}</p>
      </div>
      <div
        v-for="metric in metricEntries"
        :key="metric.key"
        class="trace-summary-card"
      >
        <span>{{ metric.label }}</span>
        <strong>{{ metric.value }}</strong>
      </div>
    </div>

    <div
      v-if="graph?.warnings.length"
      class="trace-warning-row"
    >
      <div
        v-for="warning in graph.warnings"
        :key="warning.code"
        class="trace-warning"
        :class="`trace-warning-${warning.severity}`"
      >
        <strong>{{ severityLabel(warning.severity) }}</strong>
        <span>{{ warning.message }}</span>
      </div>
    </div>

    <div class="trace-workspace">
      <div class="trace-canvas-shell">
        <div class="trace-canvas-header">
          <div>
            <p>Mapa de rastreabilidade</p>
            <h3>{{ graph?.title ?? 'Sem grafo carregado' }}</h3>
          </div>
          <div class="trace-filter-grid">
            <label
              v-if="activeMode === 'factory'"
              class="trace-filter"
            >
              <span>Linha</span>
              <select
                v-model="selectedLine"
                class="compact-select"
              >
                <option value="all">Todas</option>
                <option
                  v-for="line in options?.lines ?? []"
                  :key="line.id"
                  :value="line.code"
                >{{ line.label }}</option>
              </select>
            </label>
            <label class="trace-filter">
              <span>Estado</span>
              <select
                v-model="selectedStatus"
                class="compact-select"
              >
                <option value="all">Todos</option>
                <option
                  v-for="status in options?.statuses ?? []"
                  :key="status"
                  :value="status"
                >{{ statusLabel(status) }}</option>
              </select>
            </label>
            <label class="trace-filter">
              <span>Qualidade</span>
              <select
                v-model="selectedQuality"
                class="compact-select"
              >
                <option value="all">Todas</option>
                <option
                  v-for="status in options?.qualityStatuses ?? []"
                  :key="status"
                  :value="status"
                >{{ statusLabel(status) }}</option>
              </select>
            </label>
            <label class="trace-filter">
              <span>Tipo</span>
              <select
                v-model="selectedNodeType"
                class="compact-select"
              >
                <option value="all">Todos</option>
                <option
                  v-for="type in options?.nodeTypes ?? []"
                  :key="type"
                  :value="type"
                >{{ nodeTypeLabel(type) }}</option>
              </select>
            </label>
          </div>
        </div>

        <div class="trace-toggle-row">
          <label class="trace-toggle"><input
            v-model="compactMode"
            type="checkbox"
          > Vista compacta</label>
          <label
            v-if="displayMode === 'technical'"
            class="trace-toggle"
          ><input
            v-model="showMaterials"
            type="checkbox"
          > Materiais</label>
          <label
            v-if="displayMode === 'technical'"
            class="trace-toggle"
          ><input
            v-model="showEvents"
            type="checkbox"
          > Eventos</label>
          <button
            v-if="focusedSectionId"
            type="button"
            class="btn-ghost btn-compact"
            @click="clearFocusedSection"
          >
            Limpar foco da secção
          </button>
        </div>

        <div class="trace-flow-frame">
          <div
            v-if="loading"
            class="trace-state"
          >
            <strong>A carregar grafo...</strong>
            <span>A recolher relações operacionais, materiais e qualidade.</span>
          </div>
          <div
            v-else-if="errorMessage"
            class="trace-state trace-state-error"
          >
            <strong>Não foi possível carregar o grafo</strong>
            <span>{{ errorMessage }}</span>
          </div>
          <div
            v-else-if="!graph || !flowNodes.length"
            class="trace-state"
          >
            <strong>Sem dados para apresentar</strong>
            <span>Ajuste os filtros ou selecione outra unidade/ordem.</span>
          </div>
          <VueFlow
            v-else
            id="trace-graph"
            class="trace-flow"
            :nodes="flowNodes"
            :edges="flowEdges"
            :fit-view-on-init="true"
            :min-zoom="0.25"
            :max-zoom="1.6"
            :nodes-draggable="false"
            :nodes-connectable="false"
            :elements-selectable="true"
            :pan-on-drag="true"
            :zoom-on-scroll="true"
            @pane-click="clearSelection"
          >
            <template #node-trace="{ data }">
              <button
                type="button"
                class="trace-node"
                :class="[`trace-node-${data.graphNode.type}`, `trace-node-${data.graphNode.severity}`, { 'trace-node-selected': data.selected, 'trace-node-compact': data.compact }]"
                :title="nodeTooltip(data.graphNode)"
                @click.stop="selectNode(data.graphNode)"
              >
                <span class="trace-node-kind">{{ nodeTypeLabel(data.graphNode.type) }}</span>
                <strong>{{ data.graphNode.label }}</strong>
                <span
                  v-if="!data.compact && data.graphNode.subtitle"
                  class="trace-node-subtitle"
                >{{ data.graphNode.subtitle }}</span>
                <span class="trace-node-status">{{ statusLabel(data.graphNode.status) }}</span>
              </button>
            </template>
          </VueFlow>
        </div>

        <div
          v-if="graph?.legend.length"
          class="trace-legend"
        >
          <div
            v-for="item in graph.legend"
            :key="item.type"
            class="trace-legend-item"
            :title="item.description"
          >
            <span
              class="trace-legend-dot"
              :style="{ backgroundColor: item.color }"
            />
            <strong>{{ item.label }}</strong>
          </div>
        </div>
      </div>

      <aside class="trace-detail-panel">
        <div v-if="selectedNode">
          <p class="trace-panel-eyebrow">
            {{ nodeTypeLabel(selectedNode.type) }}
          </p>
          <h3>{{ selectedNode.label }}</h3>
          <p
            v-if="selectedNode.subtitle"
            class="trace-panel-subtitle"
          >
            {{ selectedNode.subtitle }}
          </p>
          <div class="trace-detail-badges">
            <span>{{ statusLabel(selectedNode.status) }}</span>
            <span>{{ severityLabel(selectedNode.severity) }}</span>
          </div>
          <button
            v-if="selectedNode.type === 'ProductUnit'"
            type="button"
            class="btn-primary w-full"
            @click="openSelectedProductUnit"
          >
            Abrir rota da unidade
          </button>

          <div
            v-if="selectedMetadataEntries.length"
            class="trace-detail-section"
          >
            <h4>Dados operacionais</h4>
            <dl>
              <div
                v-for="entry in selectedMetadataEntries"
                :key="entry.key"
              >
                <dt>{{ entry.label }}</dt>
                <dd>{{ entry.value }}</dd>
              </div>
            </dl>
          </div>

          <div class="trace-detail-section">
            <h4>Relações</h4>
            <ul
              v-if="selectedNodeEdges.length"
              class="trace-edge-list"
            >
              <li
                v-for="edge in selectedNodeEdges"
                :key="edge.id"
              >
                <strong>{{ edge.label }}</strong>
                <span>{{ edge.source }} → {{ edge.target }}</span>
              </li>
            </ul>
            <p
              v-else
              class="trace-muted"
            >
              Sem relações visíveis com os filtros atuais.
            </p>
          </div>
        </div>

        <div
          v-else
          class="trace-detail-empty"
        >
          <strong>Selecione um nó</strong>
          <span>Use o mapa para ver detalhes de linhas, secções, unidades, suportes, racks, materiais, qualidade e eventos.</span>
        </div>
      </aside>
    </div>
  </section>
</template>

<style scoped>
.trace-graph-page {
  display: grid;
  gap: 1rem;
}

.trace-graph-toolbar,
.trace-decision-panel,
.trace-canvas-shell,
.trace-detail-panel,
.trace-summary-card,
.trace-warning {
  border: 1px solid rgb(226 232 240 / 0.95);
  border-radius: 0.5rem;
  background: rgb(255 255 255 / 0.96);
  box-shadow: 0 10px 28px rgb(15 23 42 / 0.06);
}

.dark .trace-graph-toolbar,
.dark .trace-canvas-shell,
.dark .trace-detail-panel,
.dark .trace-summary-card,
.dark .trace-warning {
  border-color: rgb(30 41 59);
  background: rgb(15 23 42 / 0.96);
}

.trace-graph-toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem;
}

.trace-mode-group {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  border-radius: 0.5rem;
  border: 1px solid rgb(226 232 240);
  background: rgb(248 250 252);
  padding: 0.25rem;
}

.dark .trace-mode-group {
  border-color: rgb(51 65 85);
  background: rgb(2 6 23);
}

.trace-mode-button {
  min-height: 2.25rem;
  border-radius: 0.45rem;
  padding: 0.45rem 0.7rem;
  font-size: 0.78rem;
  font-weight: 900;
  color: rgb(71 85 105);
  transition: background 0.15s ease, color 0.15s ease;
}

.trace-mode-button-active {
  background: #0f766e;
  color: white;
}

.trace-detail-mode {
  background: rgb(236 253 245);
}

.dark .trace-detail-mode {
  background: rgb(6 78 59 / 0.28);
}

.trace-decision-panel {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.75rem;
  padding: 0.85rem;
  border-left: 0.35rem solid #0ea5e9;
}

.trace-decision-panel div {
  min-width: 0;
  border-right: 1px solid rgb(226 232 240);
  padding-right: 0.75rem;
}

.trace-decision-panel div:last-child {
  border-right: 0;
  padding-right: 0;
}

.trace-decision-panel span {
  display: block;
  font-size: 0.68rem;
  font-weight: 900;
  letter-spacing: 0;
  text-transform: uppercase;
  color: rgb(100 116 139);
}

.trace-decision-panel strong {
  display: block;
  margin-top: 0.25rem;
  overflow-wrap: anywhere;
  font-size: 0.9rem;
  font-weight: 950;
  line-height: 1.25;
  color: rgb(15 23 42);
}

.trace-decision-ok {
  border-left-color: #059669;
  background: linear-gradient(90deg, rgb(16 185 129 / 0.12), rgb(255 255 255 / 0.96));
}

.trace-decision-danger {
  border-left-color: #dc2626;
  background: linear-gradient(90deg, rgb(239 68 68 / 0.12), rgb(255 255 255 / 0.96));
}

.trace-decision-muted {
  border-left-color: #64748b;
}

.dark .trace-decision-panel strong {
  color: white;
}

.dark .trace-decision-panel div {
  border-color: rgb(30 41 59);
}

/* Modo escuro: o painel e os gradientes de estado desvaneciam para branco
   (corretos no claro). Aqui passam a desvanecer para o fundo escuro. */
.dark .trace-decision-panel {
  background: rgb(15 23 42 / 0.96);
}

.dark .trace-decision-ok {
  background: linear-gradient(90deg, rgb(16 185 129 / 0.22), rgb(15 23 42 / 0.96));
}

.dark .trace-decision-danger {
  background: linear-gradient(90deg, rgb(239 68 68 / 0.22), rgb(15 23 42 / 0.96));
}

.dark .trace-decision-panel span {
  color: rgb(148 163 184);
}

.trace-toolbar-fields,
.trace-filter-grid,
.trace-toggle-row,
.trace-detail-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}

.trace-field,
.trace-filter {
  display: grid;
  min-width: 12rem;
  gap: 0.25rem;
  font-size: 0.72rem;
  font-weight: 900;
  color: rgb(71 85 105);
}

.trace-filter {
  min-width: 8.5rem;
}

.dark .trace-field,
.dark .trace-filter {
  color: rgb(203 213 225);
}

.trace-summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr));
  gap: 0.75rem;
}

.trace-summary-card {
  min-width: 0;
  padding: 0.8rem;
}

.trace-summary-card span,
.trace-canvas-header p,
.trace-panel-eyebrow,
.trace-node-kind,
.trace-summary-card p {
  font-size: 0.68rem;
  font-weight: 900;
  letter-spacing: 0;
  text-transform: uppercase;
  color: rgb(100 116 139);
}

.trace-summary-card strong {
  display: block;
  margin-top: 0.25rem;
  overflow-wrap: anywhere;
  font-size: 1.35rem;
  font-weight: 950;
  line-height: 1.05;
  color: rgb(15 23 42);
}

.dark .trace-summary-card strong,
.dark .trace-canvas-header h3,
.dark .trace-detail-panel h3,
.dark .trace-detail-section h4,
.dark .trace-detail-empty strong {
  color: white;
}

.trace-summary-card p {
  margin-top: 0.55rem;
  text-transform: none;
  line-height: 1.45;
}

.trace-summary-state {
  grid-column: span 2;
}

.trace-summary-alert {
  border-color: rgb(252 165 165);
}

.trace-summary-ok {
  border-color: rgb(110 231 183);
}

.trace-warning-row {
  display: grid;
  gap: 0.5rem;
}

.trace-warning {
  display: flex;
  gap: 0.5rem;
  align-items: center;
  padding: 0.65rem 0.75rem;
  font-size: 0.86rem;
  font-weight: 700;
}

.trace-warning strong {
  color: #b45309;
}

.trace-workspace {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(15rem, 18rem);
  gap: 1rem;
  align-items: start;
}

.trace-canvas-shell {
  min-width: 0;
  overflow: hidden;
}

.trace-canvas-header {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  align-items: end;
  justify-content: space-between;
  border-bottom: 1px solid rgb(226 232 240);
  padding: 0.85rem;
}

.dark .trace-canvas-header {
  border-color: rgb(30 41 59);
}

.trace-canvas-header h3,
.trace-detail-panel h3 {
  margin-top: 0.15rem;
  overflow-wrap: anywhere;
  font-size: 1.1rem;
  font-weight: 950;
  line-height: 1.15;
  color: rgb(15 23 42);
}

.trace-toggle-row {
  border-bottom: 1px solid rgb(226 232 240);
  padding: 0.55rem 0.85rem;
}

.dark .trace-toggle-row {
  border-color: rgb(30 41 59);
}

.trace-toggle {
  display: inline-flex;
  gap: 0.35rem;
  align-items: center;
  border-radius: 0.45rem;
  border: 1px solid rgb(226 232 240);
  background: rgb(248 250 252);
  padding: 0.35rem 0.55rem;
  font-size: 0.76rem;
  font-weight: 850;
  color: rgb(51 65 85);
}

.dark .trace-toggle {
  border-color: rgb(51 65 85);
  background: rgb(2 6 23);
  color: rgb(226 232 240);
}

.trace-flow-frame {
  position: relative;
  min-height: clamp(420px, 65vh, 760px);
  background:
    linear-gradient(90deg, rgb(148 163 184 / 0.14) 1px, transparent 1px),
    linear-gradient(180deg, rgb(148 163 184 / 0.14) 1px, transparent 1px),
    #f8fafc;
  background-size: 28px 28px;
}

.dark .trace-flow-frame {
  background:
    linear-gradient(90deg, rgb(148 163 184 / 0.12) 1px, transparent 1px),
    linear-gradient(180deg, rgb(148 163 184 / 0.12) 1px, transparent 1px),
    var(--dt-app-bg);
  background-size: 28px 28px;
}

.trace-flow {
  width: 100%;
  height: clamp(420px, 65vh, 760px);
}

.trace-state {
  display: grid;
  min-height: clamp(420px, 65vh, 760px);
  place-content: center;
  gap: 0.35rem;
  padding: 1rem;
  text-align: center;
  color: rgb(71 85 105);
}

.trace-state strong {
  font-size: 1rem;
  font-weight: 950;
  color: rgb(15 23 42);
}

.dark .trace-state,
.dark .trace-state strong {
  color: rgb(226 232 240);
}

.trace-state-error strong {
  color: #b91c1c;
}

.trace-node {
  --node-accent: #64748b;
  display: grid;
  width: 11.8rem;
  min-height: 5.2rem;
  gap: 0.18rem;
  border: 1px solid rgb(203 213 225);
  border-left: 0.45rem solid var(--node-accent);
  border-radius: 0.5rem;
  background: white;
  padding: 0.55rem 0.65rem;
  text-align: left;
  color: rgb(15 23 42);
  box-shadow: 0 10px 24px rgb(15 23 42 / 0.08);
}

.dark .trace-node {
  border-color: rgb(51 65 85);
  background: rgb(15 23 42);
  color: white;
}

.trace-node:hover,
.trace-node-selected {
  outline: 3px solid rgb(20 184 166 / 0.28);
}

.trace-node strong {
  overflow-wrap: anywhere;
  font-size: 0.88rem;
  font-weight: 950;
  line-height: 1.05;
}

.trace-node-subtitle,
.trace-node-status {
  overflow-wrap: anywhere;
  font-size: 0.72rem;
  font-weight: 800;
  color: rgb(71 85 105);
}

.dark .trace-node-subtitle,
.dark .trace-node-status {
  color: rgb(203 213 225);
}

.trace-node-compact {
  width: 9.6rem;
  min-height: 4.2rem;
}

.trace-node-ProductionLine { --node-accent: #0369a1; }
.trace-node-Section { --node-accent: #0f766e; }
.trace-node-ProductUnit { --node-accent: #2563eb; }
.trace-node-Support { --node-accent: #7c3aed; }
.trace-node-Rack { --node-accent: #475569; }
.trace-node-MaterialLot { --node-accent: #ca8a04; }
.trace-node-Quality { --node-accent: #059669; }
.trace-node-Nonconformity,
.trace-node-Rework,
.trace-node-Scrap { --node-accent: #dc2626; }
.trace-node-Event { --node-accent: #0891b2; }
.trace-node-Customer,
.trace-node-ManufacturingOrder,
.trace-node-Product { --node-accent: #334155; }
.trace-node-warning { border-color: rgb(248 113 113); }
.trace-node-attention { border-color: rgb(245 158 11); }
.trace-node-ok { border-color: rgb(52 211 153); }
.trace-node-critical { border-color: rgb(153 27 27); }
.trace-node-history { border-color: rgb(148 163 184); }

.trace-legend {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(8.5rem, 1fr));
  gap: 0.3rem 0.6rem;
  border-top: 1px solid rgb(226 232 240);
  padding: 0.5rem 0.7rem;
}

.dark .trace-legend {
  border-color: rgb(30 41 59);
}

.trace-legend-item {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  gap: 0.35rem;
  align-items: center;
  min-width: 0;
  font-size: 0.7rem;
}

.trace-legend-dot {
  width: 0.6rem;
  height: 0.6rem;
  border-radius: 999px;
  flex-shrink: 0;
}

.trace-legend-item strong,
.trace-detail-section h4,
.trace-detail-empty strong {
  font-weight: 800;
  color: rgb(15 23 42);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.trace-legend-item small {
  display: none;
}

.trace-detail-panel {
  position: sticky;
  top: 5rem;
  display: grid;
  gap: 0.7rem;
  max-height: calc(100vh - 6rem);
  overflow: auto;
  padding: 0.8rem;
}

.trace-panel-subtitle,
.trace-detail-empty span,
.trace-muted {
  margin-top: 0.35rem;
  font-size: 0.85rem;
  font-weight: 700;
  line-height: 1.5;
  color: rgb(71 85 105);
}

.dark .trace-panel-subtitle,
.dark .trace-detail-empty span,
.dark .trace-muted,
.dark .trace-legend-item small {
  color: rgb(203 213 225);
}

.trace-detail-badges {
  margin: 0.85rem 0;
}

.trace-detail-badges span {
  border-radius: 999px;
  background: rgb(241 245 249);
  padding: 0.3rem 0.55rem;
  font-size: 0.72rem;
  font-weight: 900;
  color: rgb(51 65 85);
}

.dark .trace-detail-badges span {
  background: rgb(30 41 59);
  color: rgb(226 232 240);
}

.trace-detail-section {
  margin-top: 1rem;
  border-top: 1px solid rgb(226 232 240);
  padding-top: 0.9rem;
}

.dark .trace-detail-section {
  border-color: rgb(30 41 59);
}

.trace-detail-section dl {
  display: grid;
  gap: 0.45rem;
  margin-top: 0.65rem;
}

.trace-detail-section dl div {
  display: grid;
  grid-template-columns: minmax(7rem, 0.8fr) minmax(0, 1fr);
  gap: 0.5rem;
  border-radius: 0.45rem;
  background: rgb(248 250 252);
  padding: 0.45rem 0.55rem;
}

.dark .trace-detail-section dl div {
  background: rgb(2 6 23 / 0.75);
}

.trace-detail-section dt {
  font-size: 0.7rem;
  font-weight: 900;
  color: rgb(100 116 139);
}

.trace-detail-section dd {
  min-width: 0;
  overflow-wrap: anywhere;
  font-size: 0.78rem;
  font-weight: 800;
  color: rgb(15 23 42);
}

.dark .trace-detail-section dd {
  color: rgb(226 232 240);
}

.trace-edge-list {
  display: grid;
  gap: 0.45rem;
  margin-top: 0.65rem;
}

.trace-edge-list li {
  display: grid;
  gap: 0.2rem;
  border-radius: 0.45rem;
  border: 1px solid rgb(226 232 240);
  padding: 0.45rem 0.55rem;
  font-size: 0.76rem;
}

.dark .trace-edge-list li {
  border-color: rgb(51 65 85);
}

.trace-edge-list span {
  overflow-wrap: anywhere;
  color: rgb(100 116 139);
}

@media (max-width: 1180px) {
  .trace-workspace {
    grid-template-columns: 1fr;
  }

  .trace-detail-panel {
    position: static;
    max-height: none;
  }
}

@media (max-width: 720px) {
  .trace-decision-panel {
    grid-template-columns: 1fr;
  }

  .trace-decision-panel div {
    border-right: 0;
    border-bottom: 1px solid rgb(226 232 240);
    padding-bottom: 0.6rem;
  }

  .trace-decision-panel div:last-child {
    border-bottom: 0;
    padding-bottom: 0;
  }

  .trace-summary-state {
    grid-column: span 1;
  }

  .trace-field,
  .trace-filter,
  .trace-toolbar-fields,
  .trace-mode-group {
    width: 100%;
  }

  .trace-flow,
  .trace-flow-frame,
  .trace-state {
    height: 34rem;
    min-height: 34rem;
  }

  .trace-detail-section dl div {
    grid-template-columns: 1fr;
  }
}
</style>
