export type Status = 'Active' | 'In Progress' | 'Completed' | 'Blocked' | 'Rework' | 'Scrap' | 'Stored' | 'Loaded' | 'Available' | 'PASS' | 'FAIL' | 'Pending'

export interface DashboardSummary {
  appName: string
  subtitle: string
  generatedAt: string
  counts: { openOrders: number; activeUnits: number; activeSupports: number; qualityIssues: number; rackAssignments: number }
  wipBySection: Array<{ sectionId: number; section: string; sectionCode: string; sectionType: string; activeSupports: number; productUnits: number }>
  recentEvents: Array<{ supportCode: string; section: string; eventType: string; dateTime: string }>
  qualityAlerts: Array<{ unitCode: string; severity: string; status: string; description: string; createdAt: string }>
}
export interface ManufacturingOrder { id: number; orderNumber: string; productId: number; variantId?: number; plannedQty: number; scheduledUntil: string; status: Status | string; observations?: string }
export interface ProductUnit { id: number; unitCode: string; unitType: string; status: Status | string; qualityStatus: Status | string; currentSupportId?: number; currentSectionId?: number; manufacturingOrderId: number }
export interface Support { id: number; supportCode: string; status: Status | string; currentSectionId?: number }
export interface Rack { id: number; rackCode: string; status: Status | string; sectionId?: number }
export interface RawMaterial { id: number; name: string; info?: string }
export interface LotRawMaterial { id: number; rawMaterialId: number; sectionId?: number; lotNumber: string; lotQuantity: number; lotUnit: string }
export interface QualityRecord { id: number; productUnitId: number; result: 'PASS' | 'FAIL' | string; recordedAt: string; notes?: string }
export interface PredictionRecord { id: number; manufacturingOrderId?: number; modelVersion: string; modelType: string; lastDate: string; createdAt?: string; confidence?: number; status?: string }

const now = new Date().toISOString()

export const demoDashboard: DashboardSummary = {
  appName: 'DriveTrace Core',
  subtitle: 'DRIVOLUTION WP3 — WIP Traceability and Monitoring Platform',
  generatedAt: now,
  counts: { openOrders: 1, activeUnits: 5, activeSupports: 5, qualityIssues: 2, rackAssignments: 1 },
  wipBySection: [
    { sectionId: 1, section: 'Raw Materials', sectionCode: 'SEC-RAW', sectionType: 'Warehouse', activeSupports: 0, productUnits: 0 },
    { sectionId: 2, section: 'Support Assignment', sectionCode: 'SEC-SUPPORT', sectionType: 'Tracking', activeSupports: 0, productUnits: 0 },
    { sectionId: 3, section: 'Blanking / Stamping / Cutting', sectionCode: 'SEC-STAMP', sectionType: 'Production', activeSupports: 1, productUnits: 1 },
    { sectionId: 4, section: 'Hemming & Welding', sectionCode: 'SEC-WELD', sectionType: 'Production', activeSupports: 1, productUnits: 1 },
    { sectionId: 5, section: 'Painting', sectionCode: 'SEC-PAINT', sectionType: 'Production', activeSupports: 1, productUnits: 1 },
    { sectionId: 6, section: 'Quality Control', sectionCode: 'SEC-QC', sectionType: 'Quality', activeSupports: 2, productUnits: 2 },
    { sectionId: 7, section: 'Rack Storage', sectionCode: 'SEC-RACK', sectionType: 'Post-line Logistics', activeSupports: 0, productUnits: 0 },
  ],
  recentEvents: [
    { supportCode: 'SUP-003', section: 'Quality Control', eventType: 'QualityFailure', dateTime: now },
    { supportCode: 'SUP-002', section: 'Painting', eventType: 'Movement', dateTime: now },
    { supportCode: 'SUP-005', section: 'Blanking / Stamping / Cutting', eventType: 'Movement', dateTime: now },
  ],
  qualityAlerts: [
    { unitCode: 'DU-003', severity: 'Major', status: 'Blocked', description: 'Alinhamento fora da tolerância nominal; decisão do responsável de qualidade necessária.', createdAt: now },
    { unitCode: 'DU-004', severity: 'Medium', status: 'Rework', description: 'Falta de enchimento no cordão de soldadura; encaminhar para retrabalho controlado.', createdAt: now },
  ],
}

export const demoOrders: ManufacturingOrder[] = [{ id: 1, orderNumber: 'MO-DRIVE-DOOR-001', productId: 1, variantId: 1, plannedQty: 5, scheduledUntil: now, status: 'In Progress', observations: 'Ordem demo para rastreabilidade WIP da linha de montagem de portas.' }]
export const demoUnits: ProductUnit[] = [
  { id: 1, manufacturingOrderId: 1, unitCode: 'DU-001', unitType: 'Subproduct', status: 'Active', qualityStatus: 'PASS', currentSupportId: 1, currentSectionId: 6 },
  { id: 2, manufacturingOrderId: 1, unitCode: 'DU-002', unitType: 'Subproduct', status: 'Active', qualityStatus: 'PASS', currentSupportId: 2, currentSectionId: 5 },
  { id: 3, manufacturingOrderId: 1, unitCode: 'DU-003', unitType: 'Subproduct', status: 'Blocked', qualityStatus: 'FAIL', currentSupportId: 3, currentSectionId: 6 },
  { id: 4, manufacturingOrderId: 1, unitCode: 'DU-004', unitType: 'Subproduct', status: 'Rework', qualityStatus: 'FAIL', currentSupportId: 4, currentSectionId: 4 },
  { id: 5, manufacturingOrderId: 1, unitCode: 'DU-005', unitType: 'Subproduct', status: 'Active', qualityStatus: 'Pending', currentSupportId: 5, currentSectionId: 3 },
]
export const demoSupports: Support[] = [
  { id: 1, supportCode: 'SUP-001', status: 'Loaded', currentSectionId: 6 },
  { id: 2, supportCode: 'SUP-002', status: 'Loaded', currentSectionId: 5 },
  { id: 3, supportCode: 'SUP-003', status: 'Blocked', currentSectionId: 6 },
  { id: 4, supportCode: 'SUP-004', status: 'Rework', currentSectionId: 4 },
  { id: 5, supportCode: 'SUP-005', status: 'Loaded', currentSectionId: 3 },
]
export const demoRacks: Rack[] = [ { id: 1, rackCode: 'RACK-001', status: 'Available', sectionId: 7 }, { id: 2, rackCode: 'RACK-002', status: 'Available', sectionId: 7 } ]
export const demoMaterials: RawMaterial[] = [
  { id: 1, name: 'Steel Sheet', info: 'Chapa de aço exterior usada na estrutura da porta.' },
  { id: 2, name: 'Aluminium Panel', info: 'Painel leve usado em variantes premium.' },
  { id: 3, name: 'Paint Primer', info: 'Primário aplicado antes da pintura final.' },
  { id: 4, name: 'Final Paint', info: 'Revestimento final da superfície da porta.' },
  { id: 5, name: 'Rubber Seal', info: 'Material de vedação da porta.' },
  { id: 6, name: 'Wiring Clip', info: 'Componente usado para fixar cablagem no interior da porta.' },
]
export const demoLots: LotRawMaterial[] = [
  { id: 1, rawMaterialId: 1, sectionId: 3, lotNumber: 'ST-LOT-001', lotQuantity: 250, lotUnit: 'kg' },
  { id: 2, rawMaterialId: 2, sectionId: 3, lotNumber: 'AL-LOT-001', lotQuantity: 120, lotUnit: 'kg' },
  { id: 3, rawMaterialId: 3, sectionId: 5, lotNumber: 'PAINT-LOT-001', lotQuantity: 80, lotUnit: 'L' },
  { id: 4, rawMaterialId: 5, sectionId: 4, lotNumber: 'RUBBER-LOT-001', lotQuantity: 300, lotUnit: 'm' },
]
export const demoQuality: QualityRecord[] = [
  { id: 1, productUnitId: 1, result: 'PASS', recordedAt: now, notes: 'Inspeção final aprovada.' },
  { id: 2, productUnitId: 3, result: 'FAIL', recordedAt: now, notes: 'Desvio de alinhamento detetado no controlo final.' },
  { id: 3, productUnitId: 4, result: 'FAIL', recordedAt: now, notes: 'Cordão de soldadura requer retrabalho.' },
]
export const demoPredictions: PredictionRecord[] = [
  { id: 1, manufacturingOrderId: 1, modelVersion: 'future-v1', modelType: 'CompletionTimePlaceholder', lastDate: now, createdAt: now, confidence: 0.72, status: 'Placeholder' },
]
