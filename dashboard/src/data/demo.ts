export type Status = 'Planned' | 'Active' | 'In Progress' | 'Completed' | 'Blocked' | 'Cancelled' | 'Rework' | 'Scrap' | 'Stored' | 'Loaded' | 'Available' | 'PASS' | 'FAIL' | 'Pending' | 'Open'

export interface DashboardSummary {
  appName: string
  subtitle: string
  generatedAt: string
  counts: { openOrders: number; activeUnits: number; activeSupports: number; qualityIssues: number; rackAssignments: number }
  wipBySection: Array<{ sectionId: number; section: string; sectionCode: string; sectionType: string; activeSupports: number; productUnits: number }>
  recentEvents: Array<{ supportCode: string; section: string; eventType: string; dateTime: string }>
  qualityAlerts: Array<{ unitCode: string; severity: string; status: string; description: string; createdAt: string }>
}
export interface Product { id: number; name: string; info?: string }
export interface Variant { id: number; productId: number; variantCode: string; name: string }
export interface ProductionLine { id: number; lineCode: string; name: string }
export interface ProductionLineSection { id: number; sectionCode: string; name: string; sectionType: string; lineId?: number }
export interface ResourceRecord { id: number; name: string; type: string; function: string }
export interface ManufacturingProcess { id: number; productId: number; processName: string; info?: string }
export interface ManufacturingSectionPhase { id: number; sectionId: number; phaseInfo: string; phaseDuration: number }
export interface ManufacturingProcessPhase { id: number; manufacturingProcessId: number; manufacturingPhaseId: number; resourceId?: number; numberStepOrder: number }
export interface Checkpoint { id: number; checkpointCode: string; name: string; status: Status | string; sectionId: number }
export interface ManufacturingOrder { id: number; orderNumber: string; productId: number; variantId?: number; manufacturingProcessId: number; productionLineId: number; plannedQty: number; scheduledUntil: string; status: Status | string; observations?: string }
export interface ProductUnit { id: number; unitCode: string; unitType: string; status: Status | string; qualityStatus: Status | string; currentSupportId?: number; currentSectionId?: number; manufacturingOrderId: number; variantId?: number; parentUnitId?: number; createdAt?: string; completedAt?: string }
export interface Support { id: number; supportCode: string; status: Status | string; currentSectionId?: number }
export interface Rack { id: number; rackCode: string; status: Status | string; sectionId?: number }
export interface RawMaterial { id: number; name: string; info?: string }
export interface LotRawMaterial { id: number; rawMaterialId: number; sectionId?: number; lotNumber: string; lotQuantity: number; lotUnit: string }
export interface UnitMaterialLotUsage { id: number; productUnitId: number; lotId: number; associationType: string; quantity: number }
export interface QualityRecord { id: number; productUnitId: number; checkpointId?: number; result: 'PASS' | 'FAIL' | string; recordedAt: string; notes?: string }
export interface NonconformityRecord { id: number; productUnitId: number; qualityResultId?: number; severity: string; status: string; description?: string; createdAt?: string }
export interface ReworkRecord { id: number; productUnitId: number; nonconformityId?: number; startedAt: string; endedAt?: string; status: string; notes?: string }
export interface ScrapRecord { id: number; productUnitId: number; nonconformityId?: number; scrappedAt: string; reason?: string }
export interface RackSupportAssignment { id: number; rackId: number; supportId: number; dateTimeIn: string; dateTimeOut?: string }
export interface SupportLocalizationHistory { id: number; supportId: number; sectionId: number; dateTime: string; eventType: string }
export interface PredictionRecord { id: number; manufacturingOrderId?: number; modelVersion: string; modelType: string; lastDate?: string; createdAt?: string; confidence?: number; status?: string }

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

export const demoProducts: Product[] = [
  { id: 1, name: 'Porta automóvel', info: 'Subproduto automóvel rastreável usado como cenário demo principal da V1.' },
  { id: 2, name: 'Módulo de carroçaria', info: 'Produto de nível superior para montagem final.' },
]

export const demoVariants: Variant[] = [
  { id: 1, productId: 1, variantCode: 'DOOR-STD', name: 'Porta standard' },
  { id: 2, productId: 1, variantCode: 'DOOR-PRM', name: 'Porta premium' },
  { id: 3, productId: 1, variantCode: 'DOOR-REINF', name: 'Porta reforçada' },
]

export const demoProductionLines: ProductionLine[] = [
  { id: 1, lineCode: 'DL-01', name: 'Linha de Montagem de Portas' },
]

export const demoProductionLineSections: ProductionLineSection[] = [
  { id: 1, sectionCode: 'SEC-RAW', name: 'Matérias-primas', sectionType: 'Armazém', lineId: 1 },
  { id: 2, sectionCode: 'SEC-SUPPORT', name: 'Atribuição do suporte', sectionType: 'Rastreio', lineId: 1 },
  { id: 3, sectionCode: 'SEC-STAMP', name: 'Corte / Estampagem', sectionType: 'Produção', lineId: 1 },
  { id: 4, sectionCode: 'SEC-WELD', name: 'Dobra e Soldadura', sectionType: 'Produção', lineId: 1 },
  { id: 5, sectionCode: 'SEC-PAINT', name: 'Pintura', sectionType: 'Produção', lineId: 1 },
  { id: 6, sectionCode: 'SEC-QC', name: 'Controlo de Qualidade', sectionType: 'Qualidade', lineId: 1 },
  { id: 7, sectionCode: 'SEC-RACK', name: 'Armazenamento em Rack', sectionType: 'Logística Pós-Linha', lineId: 1 },
]

export const demoResources: ResourceRecord[] = [
  { id: 1, name: 'Operador A', type: 'Operador', function: 'Atribuição do suporte e validação local' },
  { id: 2, name: 'Célula Robotizada R1', type: 'Robot', function: 'Apoio ao corte e estampagem' },
]

export const demoManufacturingProcesses: ManufacturingProcess[] = [
  { id: 1, productId: 1, processName: 'Processo de Fabrico de Porta', info: 'Processo linear nominal para rastreabilidade WIP.' },
]

export const demoManufacturingSectionPhases: ManufacturingSectionPhase[] = [
  { id: 1, sectionId: 1, phaseInfo: 'Preparação de materiais', phaseDuration: 20 },
  { id: 2, sectionId: 2, phaseInfo: 'Atribuição do suporte', phaseDuration: 10 },
  { id: 3, sectionId: 3, phaseInfo: 'Corte e Estampagem', phaseDuration: 35 },
]

export const demoManufacturingProcessPhases: ManufacturingProcessPhase[] = [
  { id: 1, manufacturingProcessId: 1, manufacturingPhaseId: 1, numberStepOrder: 1 },
  { id: 2, manufacturingProcessId: 1, manufacturingPhaseId: 2, resourceId: 1, numberStepOrder: 2 },
]

export const demoCheckpoints: Checkpoint[] = [
  { id: 1, checkpointCode: 'CP-STAMP-01', name: 'Verificação de geometria na estampagem', status: 'Active', sectionId: 3 },
  { id: 2, checkpointCode: 'CP-WELD-01', name: 'Verificação do cordão de soldadura', status: 'Active', sectionId: 4 },
  { id: 3, checkpointCode: 'CP-PAINT-01', name: 'Verificação da espessura de pintura', status: 'Active', sectionId: 5 },
  { id: 4, checkpointCode: 'CP-QC-01', name: 'Controlo final de qualidade da porta', status: 'Active', sectionId: 6 },
]

export const demoOrders: ManufacturingOrder[] = [{ id: 1, orderNumber: 'MO-DRIVE-DOOR-001', productId: 1, variantId: 1, manufacturingProcessId: 1, productionLineId: 1, plannedQty: 5, scheduledUntil: now, status: 'In Progress', observations: 'Ordem demo para rastreabilidade WIP da linha de montagem de portas.' }]
export const demoUnits: ProductUnit[] = [
  { id: 1, manufacturingOrderId: 1, variantId: 1, unitCode: 'DU-001', unitType: 'Subproduct', status: 'Active', qualityStatus: 'PASS', currentSupportId: 1, currentSectionId: 6, createdAt: now },
  { id: 2, manufacturingOrderId: 1, variantId: 1, unitCode: 'DU-002', unitType: 'Subproduct', status: 'Active', qualityStatus: 'PASS', currentSupportId: 2, currentSectionId: 5, createdAt: now },
  { id: 3, manufacturingOrderId: 1, variantId: 2, unitCode: 'DU-003', unitType: 'Subproduct', status: 'Blocked', qualityStatus: 'FAIL', currentSupportId: 3, currentSectionId: 6, createdAt: now },
  { id: 4, manufacturingOrderId: 1, variantId: 3, unitCode: 'DU-004', unitType: 'Subproduct', status: 'Rework', qualityStatus: 'FAIL', currentSupportId: 4, currentSectionId: 4, createdAt: now },
  { id: 5, manufacturingOrderId: 1, variantId: 1, unitCode: 'DU-005', unitType: 'Subproduct', status: 'Active', qualityStatus: 'Pending', currentSupportId: 5, currentSectionId: 3, createdAt: now },
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
export const demoUnitMaterialLotUsages: UnitMaterialLotUsage[] = [
  { id: 1, productUnitId: 1, lotId: 1, associationType: 'Consumed', quantity: 8 },
  { id: 2, productUnitId: 1, lotId: 3, associationType: 'Consumed', quantity: 1 },
]
export const demoQuality: QualityRecord[] = [
  { id: 1, productUnitId: 1, checkpointId: 4, result: 'PASS', recordedAt: now, notes: 'Inspeção final aprovada.' },
  { id: 2, productUnitId: 3, checkpointId: 4, result: 'FAIL', recordedAt: now, notes: 'Desvio de alinhamento detetado no controlo final.' },
  { id: 3, productUnitId: 4, checkpointId: 2, result: 'FAIL', recordedAt: now, notes: 'Cordão de soldadura requer retrabalho.' },
]
export const demoNonconformities: NonconformityRecord[] = [
  { id: 1, productUnitId: 3, qualityResultId: 2, severity: 'Major', status: 'Blocked', description: 'Alinhamento fora da tolerância nominal; decisão do responsável de qualidade necessária.', createdAt: now },
  { id: 2, productUnitId: 4, qualityResultId: 3, severity: 'Medium', status: 'Rework', description: 'Falta de enchimento no cordão de soldadura; encaminhar para retrabalho controlado.', createdAt: now },
]
export const demoReworkRecords: ReworkRecord[] = [
  { id: 1, productUnitId: 4, nonconformityId: 2, startedAt: now, status: 'Open', notes: 'Reparar cordão de soldadura e repetir checkpoint de qualidade.' },
]
export const demoScrapRecords: ScrapRecord[] = [
  { id: 1, productUnitId: 3, nonconformityId: 1, scrappedAt: now, reason: 'Caminho de decisão de sucata para demonstração; registo mantido para auditoria e genealogia.' },
]
export const demoRackSupportAssignments: RackSupportAssignment[] = [
  { id: 1, rackId: 1, supportId: 1, dateTimeIn: now },
]
export const demoSupportLocalizationHistory: SupportLocalizationHistory[] = [
  { id: 1, supportId: 1, sectionId: 1, dateTime: now, eventType: 'SupportAssigned' },
  { id: 2, supportId: 1, sectionId: 6, dateTime: now, eventType: 'Movement' },
]
export const demoPredictions: PredictionRecord[] = [
  { id: 1, manufacturingOrderId: 1, modelVersion: 'future-v1', modelType: 'CompletionTimePlaceholder', lastDate: now, createdAt: now, confidence: 0.72, status: 'Placeholder' },
]
