export type Status = 'Planned' | 'Active' | 'In Progress' | 'Completed' | 'Blocked' | 'Cancelled' | 'Rework' | 'Scrap' | 'Stored' | 'Loaded' | 'Available' | 'PASS' | 'FAIL' | 'Pending' | 'Open' | string

export interface DashboardSummary {
  appName: string
  subtitle: string
  generatedAt: string
  counts: { openOrders: number; activeUnits: number; activeSupports: number; qualityIssues: number; rackAssignments: number; reconditionedUnits?: number; recoveryCandidates?: number; inRecovery?: number; recoveryRate?: number }
  wipBySection: Array<{ sectionId: number; section: string; sectionCode: string; sectionType: string; activeSupports: number; productUnits: number }>
  recentEvents: Array<{ supportCode: string; section: string; eventType: string; dateTime: string }>
  qualityAlerts: Array<{ unitCode: string; severity: string; status: string; description: string; createdAt: string }>
}
export interface Product { id: number; name: string; info?: string }
export interface Variant { id: number; productId: number; variantCode: string; name: string }
export interface Customer { id: number; customerCode: string; name: string; contactEmail?: string; isActive?: boolean }
export interface ProductionLine { id: number; lineCode: string; name: string; displayOrder?: number; visualGroup?: string }
export interface ProductionLineSection { id: number; sectionCode: string; name: string; sectionType: string; lineId?: number; displayOrder?: number; layoutColumn?: number; layoutRow?: number; visualZone?: string; isTransferPoint?: boolean; allowsLineTransferIn?: boolean; allowsLineTransferOut?: boolean }
export interface ResourceRecord { id: number; name: string; type: string; function: string }
export interface ManufacturingProcess { id: number; productId: number; processName: string; info?: string }
export interface ManufacturingSectionPhase { id: number; sectionId: number; phaseInfo: string; phaseDuration: number }
export interface ManufacturingProcessPhase { id: number; manufacturingProcessId: number; manufacturingPhaseId: number; resourceId?: number; numberStepOrder: number }
export interface Checkpoint { id: number; checkpointCode: string; name: string; status: Status | string; sectionId: number }
export interface ManufacturingOrder { id: number; orderNumber: string; productId: number; variantId?: number; customerId?: number; manufacturingProcessId: number; productionLineId: number; plannedQty: number; scheduledUntil: string; status: Status | string; customerReference?: string; publicTrackingCode?: string; observations?: string }
export interface ProductUnit { id: number; unitCode: string; unitType: string; status: Status | string; qualityStatus: Status | string; currentSupportId?: number; currentSectionId?: number; manufacturingOrderId: number; variantId?: number; parentUnitId?: number; createdAt?: string; completedAt?: string; isReconditioned?: boolean; reconditionedAt?: string; reconditionReason?: string; recoveryStatus?: string; qualityDisposition?: string }
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
  subtitle: 'Plataforma de rastreabilidade e monitorização WIP',
  generatedAt: now,
  counts: { openOrders: 3, activeUnits: 5, activeSupports: 7, qualityIssues: 3, rackAssignments: 1, reconditionedUnits: 1, recoveryCandidates: 1, inRecovery: 1, recoveryRate: 50 },
  wipBySection: [
    { sectionId: 1, section: 'Matérias-primas', sectionCode: 'SEC-MP', sectionType: 'Armazém', activeSupports: 0, productUnits: 2 },
    { sectionId: 2, section: 'Atribuição de suporte', sectionCode: 'SEC-ATRIB-SUP', sectionType: 'Rastreio', activeSupports: 1, productUnits: 1 },
    { sectionId: 3, section: 'Corte e estampagem', sectionCode: 'SEC-CORTE-ESTAMP', sectionType: 'Produção', activeSupports: 1, productUnits: 1 },
    { sectionId: 4, section: 'Soldadura', sectionCode: 'SEC-SOLD', sectionType: 'Produção', activeSupports: 1, productUnits: 1 },
    { sectionId: 9, section: 'Pintura A', sectionCode: 'SEC-PINT-A', sectionType: 'Produção', activeSupports: 0, productUnits: 0 },
    { sectionId: 11, section: 'Pintura B', sectionCode: 'SEC-PINT-B', sectionType: 'Produção', activeSupports: 1, productUnits: 1 },
    { sectionId: 6, section: 'Controlo de qualidade', sectionCode: 'SEC-CQ', sectionType: 'Qualidade', activeSupports: 2, productUnits: 2 },
    { sectionId: 14, section: 'Retrabalho', sectionCode: 'SEC-RETRAB', sectionType: 'Retrabalho', activeSupports: 1, productUnits: 1 },
    { sectionId: 7, section: 'Armazenamento em rack', sectionCode: 'SEC-RACK', sectionType: 'Logística pós-linha', activeSupports: 1, productUnits: 1 },
  ],
  recentEvents: [
    { supportCode: 'SUP-006', section: 'Armazenamento em rack', eventType: 'TransferToRack', dateTime: now },
    { supportCode: 'SUP-003', section: 'Retrabalho', eventType: 'LineTransfer', dateTime: now },
    { supportCode: 'SUP-007', section: 'Pintura B', eventType: 'Movement', dateTime: now },
  ],
  qualityAlerts: [
    { unitCode: 'UP-PORTA-003', severity: 'Maior', status: 'Rework', description: 'Espessura de pintura fora de tolerância; encaminhar para retrabalho controlado.', createdAt: now },
    { unitCode: 'UP-PORTA-004', severity: 'Crítica', status: 'Blocked', description: 'Desalinhamento estrutural na zona de soldadura; decisão de qualidade obrigatória.', createdAt: now },
  ],
}

export const demoProducts: Product[] = [
  { id: 1, name: 'Porta automóvel', info: 'Subconjunto automóvel rastreável usado como cenário principal de demonstração.' },
  { id: 2, name: 'Subconjunto de porta', info: 'Estrutura funcional que agrega painel exterior, reforços, fecho e cablagem.' },
  { id: 3, name: 'Painel exterior de porta', info: 'Painel estampado preparado para soldadura, pintura e controlo dimensional.' },
  { id: 4, name: 'Estrutura interior de porta', info: 'Estrutura interior com reforços e pontos de fixação para montagem final.' },
]

export const demoVariants: Variant[] = [
  { id: 1, productId: 1, variantCode: 'PORTA-STD', name: 'Porta standard' },
  { id: 2, productId: 1, variantCode: 'PORTA-PRM', name: 'Porta premium' },
  { id: 3, productId: 1, variantCode: 'PORTA-REF', name: 'Porta reforçada' },
  { id: 4, productId: 1, variantCode: 'PORTA-LEV', name: 'Porta leve' },
]

export const demoCustomers: Customer[] = [
  { id: 1, customerCode: 'CLI-AUTO-001', name: 'AutoEuropa Demo', contactEmail: 'planeamento@autoeuropa-demo.pt', isActive: true },
  { id: 2, customerCode: 'CLI-OEM-002', name: 'Fornecedor OEM Norte', contactEmail: 'qualidade@oem-norte.pt', isActive: true },
  { id: 3, customerCode: 'CLI-PILOTO-003', name: 'Linha Piloto DRIVOLUTION', contactEmail: 'piloto@drivolution.pt', isActive: true },
]

export const demoProductionLines: ProductionLine[] = [
  { id: 1, lineCode: 'LINHA-01', name: 'Linha 1 - Montagem e soldadura', displayOrder: 10, visualGroup: 'montagem' },
  { id: 2, lineCode: 'LINHA-02', name: 'Linha 2 - Pintura A', displayOrder: 20, visualGroup: 'pintura' },
  { id: 3, lineCode: 'LINHA-03', name: 'Linha 3 - Pintura B', displayOrder: 30, visualGroup: 'pintura' },
  { id: 4, lineCode: 'LINHA-04', name: 'Linha 4 - Qualidade e retrabalho', displayOrder: 40, visualGroup: 'qualidade' },
]

export const demoProductionLineSections: ProductionLineSection[] = [
  { id: 1, sectionCode: 'SEC-MP', name: 'Matérias-primas', sectionType: 'Armazém', lineId: 1, displayOrder: 10 },
  { id: 2, sectionCode: 'SEC-ATRIB-SUP', name: 'Atribuição de suporte', sectionType: 'Rastreio', lineId: 1, displayOrder: 20, isTransferPoint: true, allowsLineTransferIn: true, allowsLineTransferOut: true },
  { id: 3, sectionCode: 'SEC-CORTE-ESTAMP', name: 'Corte e estampagem', sectionType: 'Produção', lineId: 1, displayOrder: 30 },
  { id: 4, sectionCode: 'SEC-SOLD', name: 'Soldadura', sectionType: 'Produção', lineId: 1, displayOrder: 40, allowsLineTransferOut: true },
  { id: 5, sectionCode: 'SEC-TRANS-PINT', name: 'Buffer de transferência para pintura', sectionType: 'Transferência', lineId: 1, displayOrder: 50, isTransferPoint: true, allowsLineTransferIn: true, allowsLineTransferOut: true },
  { id: 6, sectionCode: 'SEC-CQ', name: 'Controlo de qualidade', sectionType: 'Qualidade', lineId: 4, displayOrder: 20, isTransferPoint: true, allowsLineTransferIn: true, allowsLineTransferOut: true },
  { id: 7, sectionCode: 'SEC-RACK', name: 'Armazenamento em rack', sectionType: 'Logística pós-linha', lineId: 4, displayOrder: 40, allowsLineTransferIn: true },
  { id: 8, sectionCode: 'SEC-PREP-PINT-A', name: 'Preparação de pintura A', sectionType: 'Produção', lineId: 2, displayOrder: 10, isTransferPoint: true, allowsLineTransferIn: true },
  { id: 9, sectionCode: 'SEC-PINT-A', name: 'Pintura A', sectionType: 'Produção', lineId: 2, displayOrder: 20, allowsLineTransferOut: true },
  { id: 10, sectionCode: 'SEC-CURA-A', name: 'Cura A', sectionType: 'Produção', lineId: 2, displayOrder: 30, isTransferPoint: true, allowsLineTransferOut: true },
  { id: 11, sectionCode: 'SEC-PINT-B', name: 'Pintura B', sectionType: 'Produção', lineId: 3, displayOrder: 10, isTransferPoint: true, allowsLineTransferIn: true, allowsLineTransferOut: true },
  { id: 12, sectionCode: 'SEC-INSPEC-PINT-B', name: 'Inspeção de pintura B', sectionType: 'Qualidade', lineId: 3, displayOrder: 20, allowsLineTransferOut: true },
  { id: 13, sectionCode: 'SEC-MONT-FINAL', name: 'Montagem final', sectionType: 'Produção', lineId: 4, displayOrder: 10, isTransferPoint: true, allowsLineTransferIn: true, allowsLineTransferOut: true },
  { id: 14, sectionCode: 'SEC-RETRAB', name: 'Retrabalho', sectionType: 'Retrabalho', lineId: 4, displayOrder: 30, isTransferPoint: true, allowsLineTransferIn: true, allowsLineTransferOut: true },
  { id: 15, sectionCode: 'SEC-EXPED', name: 'Buffer de expedição', sectionType: 'Logística pós-linha', lineId: 4, displayOrder: 50, allowsLineTransferIn: true },
]

export const demoResources: ResourceRecord[] = [
  { id: 1, name: 'Operador de montagem OM-01', type: 'Operador', function: 'Atribuição de suporte e validação local' },
  { id: 2, name: 'Célula robotizada CR-01', type: 'Robot', function: 'Apoio ao corte e estampagem' },
  { id: 3, name: 'Robot de soldadura RS-02', type: 'Robot', function: 'Dobra e soldadura do subconjunto de porta' },
  { id: 4, name: 'Cabina de pintura CP-01', type: 'Máquina', function: 'Aplicação de primário, tinta base e verniz' },
  { id: 5, name: 'Inspetor de qualidade CQ-01', type: 'Operador', function: 'Controlo visual e pontos de controlo de qualidade' },
  { id: 6, name: 'Técnico de retrabalho TR-01', type: 'Operador', function: 'Correção controlada de não conformidades' },
]

export const demoManufacturingProcesses: ManufacturingProcess[] = [
  { id: 1, productId: 1, processName: 'Processo de fabrico de porta automóvel', info: 'Processo nominal para rastreabilidade WIP de subconjuntos de porta automóvel.' },
]

export const demoManufacturingSectionPhases: ManufacturingSectionPhase[] = [
  { id: 1, sectionId: 1, phaseInfo: 'Preparação de matérias-primas', phaseDuration: 20 },
  { id: 2, sectionId: 2, phaseInfo: 'Atribuição de suporte', phaseDuration: 10 },
  { id: 3, sectionId: 3, phaseInfo: 'Corte e estampagem', phaseDuration: 35 },
  { id: 4, sectionId: 4, phaseInfo: 'Soldadura', phaseDuration: 45 },
  { id: 5, sectionId: 9, phaseInfo: 'Pintura A', phaseDuration: 55 },
  { id: 6, sectionId: 11, phaseInfo: 'Pintura B', phaseDuration: 55 },
  { id: 7, sectionId: 6, phaseInfo: 'Controlo final de qualidade', phaseDuration: 25 },
]

export const demoManufacturingProcessPhases: ManufacturingProcessPhase[] = [
  { id: 1, manufacturingProcessId: 1, manufacturingPhaseId: 1, numberStepOrder: 1 },
  { id: 2, manufacturingProcessId: 1, manufacturingPhaseId: 2, resourceId: 1, numberStepOrder: 2 },
  { id: 3, manufacturingProcessId: 1, manufacturingPhaseId: 3, resourceId: 2, numberStepOrder: 3 },
  { id: 4, manufacturingProcessId: 1, manufacturingPhaseId: 4, resourceId: 3, numberStepOrder: 4 },
]

export const demoCheckpoints: Checkpoint[] = [
  { id: 1, checkpointCode: 'PC-ESTAMP-001', name: 'Verificação geométrica da estampagem', status: 'Active', sectionId: 3 },
  { id: 2, checkpointCode: 'PC-SOLD-001', name: 'Controlo do cordão de soldadura', status: 'Active', sectionId: 4 },
  { id: 3, checkpointCode: 'PC-PINT-001', name: 'Verificação da espessura da pintura', status: 'Active', sectionId: 9 },
  { id: 4, checkpointCode: 'PC-PINT-002', name: 'Inspeção visual da pintura B', status: 'Active', sectionId: 12 },
  { id: 5, checkpointCode: 'PC-CQ-001', name: 'Controlo final da porta', status: 'Active', sectionId: 6 },
  { id: 6, checkpointCode: 'PC-RETRAB-001', name: 'Validação pós-retrabalho', status: 'Active', sectionId: 14 },
]

export const demoOrders: ManufacturingOrder[] = [
  { id: 1, orderNumber: 'OF-PORTA-001', productId: 1, variantId: 1, customerId: 1, manufacturingProcessId: 1, productionLineId: 1, plannedQty: 6, scheduledUntil: now, status: 'In Progress', customerReference: 'OEM-PT-2026-001', publicTrackingCode: 'TRC-PORTA-001', observations: 'Ordem demonstrativa para rastreabilidade WIP de portas automóveis.' },
  { id: 2, orderNumber: 'OF-PORTA-002', productId: 1, variantId: 2, customerId: 2, manufacturingProcessId: 1, productionLineId: 1, plannedQty: 4, scheduledUntil: now, status: 'In Progress', customerReference: 'OEM-PT-2026-002', publicTrackingCode: 'TRC-PORTA-002', observations: 'Ordem com transferência para pintura alternativa.' },
  { id: 3, orderNumber: 'OF-PORTA-003', productId: 2, variantId: 3, customerId: 3, manufacturingProcessId: 1, productionLineId: 1, plannedQty: 3, scheduledUntil: now, status: 'Blocked', customerReference: 'PILOTO-2026-003', publicTrackingCode: 'TRC-PORTA-003', observations: 'Ordem usada para cenário de falha de qualidade e retrabalho.' },
  { id: 4, orderNumber: 'OF-PORTA-004', productId: 1, variantId: 4, customerId: 1, manufacturingProcessId: 1, productionLineId: 1, plannedQty: 5, scheduledUntil: now, status: 'Planned', customerReference: 'OEM-PT-2026-004', publicTrackingCode: 'TRC-PORTA-004', observations: 'Ordem planeada para validação de capacidade pós-linha.' },
]

export const demoUnits: ProductUnit[] = [
  { id: 1, manufacturingOrderId: 1, variantId: 1, unitCode: 'UP-PORTA-001', unitType: 'Subproduto', status: 'Completed', qualityStatus: 'PASS', currentSupportId: 1, currentSectionId: 6, createdAt: now, completedAt: now },
  { id: 2, manufacturingOrderId: 1, variantId: 2, unitCode: 'UP-PORTA-002', unitType: 'Subproduto', status: 'Completed', qualityStatus: 'PASS', currentSupportId: 2, currentSectionId: 6, createdAt: now, completedAt: now },
  { id: 3, manufacturingOrderId: 3, variantId: 2, unitCode: 'UP-PORTA-003', unitType: 'Subproduto', status: 'Rework', qualityStatus: 'FAIL', currentSupportId: 3, currentSectionId: 14, createdAt: now },
  { id: 4, manufacturingOrderId: 3, variantId: 3, unitCode: 'UP-PORTA-004', unitType: 'Subproduto', status: 'Blocked', qualityStatus: 'FAIL', currentSupportId: 4, currentSectionId: 4, createdAt: now },
  { id: 5, manufacturingOrderId: 1, variantId: 1, unitCode: 'UP-PORTA-005', unitType: 'Subproduto', status: 'Active', qualityStatus: 'Pending', currentSupportId: 5, currentSectionId: 3, createdAt: now },
  { id: 6, manufacturingOrderId: 2, variantId: 4, unitCode: 'UP-PORTA-006', unitType: 'Subproduto', status: 'Completed', qualityStatus: 'PASS', currentSupportId: 6, currentSectionId: 7, createdAt: now, completedAt: now },
  { id: 7, manufacturingOrderId: 2, variantId: 2, unitCode: 'UP-PORTA-007', unitType: 'Subproduto', status: 'Active', qualityStatus: 'Pending', currentSupportId: 7, currentSectionId: 11, createdAt: now },
  { id: 8, manufacturingOrderId: 4, variantId: 4, unitCode: 'UP-PORTA-008', unitType: 'Subproduto', status: 'Active', qualityStatus: 'Pending', currentSupportId: 8, currentSectionId: 2, createdAt: now },
  { id: 9, manufacturingOrderId: 4, variantId: 1, unitCode: 'UP-PORTA-009', unitType: 'Subproduto', status: 'Planned', qualityStatus: 'Pending', currentSectionId: 1, createdAt: now },
  { id: 10, manufacturingOrderId: 4, variantId: 3, unitCode: 'UP-PORTA-010', unitType: 'Subproduto', status: 'Planned', qualityStatus: 'Pending', currentSectionId: 1, createdAt: now },
  { id: 11, manufacturingOrderId: 2, variantId: 1, unitCode: 'UP-PORTA-011', unitType: 'Subproduto', status: 'Completed', qualityStatus: 'PASS', currentSupportId: 9, currentSectionId: 7, createdAt: now, completedAt: now, isReconditioned: true, reconditionedAt: now, reconditionReason: 'Risco superficial recuperado por polimento controlado.', recoveryStatus: 'Reconditioned', qualityDisposition: 'Reconditioned' },
]

export const demoSupports: Support[] = [
  { id: 1, supportCode: 'SUP-001', status: 'Loaded', currentSectionId: 6 },
  { id: 2, supportCode: 'SUP-002', status: 'Loaded', currentSectionId: 6 },
  { id: 3, supportCode: 'SUP-003', status: 'Rework', currentSectionId: 14 },
  { id: 4, supportCode: 'SUP-004', status: 'Blocked', currentSectionId: 4 },
  { id: 5, supportCode: 'SUP-005', status: 'Loaded', currentSectionId: 3 },
  { id: 6, supportCode: 'SUP-006', status: 'Stored', currentSectionId: 7 },
  { id: 7, supportCode: 'SUP-007', status: 'Loaded', currentSectionId: 11 },
  { id: 8, supportCode: 'SUP-008', status: 'Available', currentSectionId: 2 },
  { id: 9, supportCode: 'SUP-009', status: 'Stored', currentSectionId: 7 },
]

export const demoRacks: Rack[] = [
  { id: 1, rackCode: 'RACK-001', status: 'Stored', sectionId: 7 },
  { id: 2, rackCode: 'RACK-002', status: 'Available', sectionId: 7 },
  { id: 3, rackCode: 'RACK-003', status: 'Available', sectionId: 7 },
  { id: 4, rackCode: 'RACK-004', status: 'Blocked', sectionId: 15 },
]

export const demoMaterials: RawMaterial[] = [
  { id: 1, name: 'Chapa de aço DX56', info: 'Chapa exterior usada na estrutura da porta.' },
  { id: 2, name: 'Reforço interior da porta', info: 'Reforço estrutural aplicado antes da soldadura.' },
  { id: 3, name: 'Primário anticorrosivo', info: 'Primário aplicado antes da tinta base.' },
  { id: 4, name: 'Tinta base', info: 'Tinta final aplicada na superfície exterior.' },
  { id: 5, name: 'Verniz', info: 'Verniz de acabamento para proteção da pintura.' },
  { id: 6, name: 'Selante estrutural', info: 'Selante usado nas uniões e zonas de vedação.' },
  { id: 7, name: 'Dobradiça', info: 'Componente mecânico para montagem final da porta.' },
  { id: 8, name: 'Fecho da porta', info: 'Mecanismo de fecho para montagem final.' },
  { id: 9, name: 'Cablagem da porta', info: 'Conjunto de cablagem e clips de fixação.' },
]

export const demoLots: LotRawMaterial[] = [
  { id: 1, rawMaterialId: 1, sectionId: 3, lotNumber: 'LOTE-ACO-2026-001', lotQuantity: 250, lotUnit: 'kg' },
  { id: 2, rawMaterialId: 2, sectionId: 4, lotNumber: 'LOTE-REFORCO-2026-002', lotQuantity: 160, lotUnit: 'un.' },
  { id: 3, rawMaterialId: 3, sectionId: 9, lotNumber: 'LOTE-PRIMARIO-2026-011', lotQuantity: 80, lotUnit: 'L' },
  { id: 4, rawMaterialId: 4, sectionId: 9, lotNumber: 'LOTE-TINTA-2026-014', lotQuantity: 120, lotUnit: 'L' },
  { id: 5, rawMaterialId: 5, sectionId: 11, lotNumber: 'LOTE-VERNIZ-2026-006', lotQuantity: 90, lotUnit: 'L' },
  { id: 6, rawMaterialId: 6, sectionId: 4, lotNumber: 'LOTE-SELANTE-2026-003', lotQuantity: 300, lotUnit: 'm' },
  { id: 7, rawMaterialId: 7, sectionId: 13, lotNumber: 'LOTE-DOBRADICA-2026-008', lotQuantity: 240, lotUnit: 'un.' },
  { id: 8, rawMaterialId: 8, sectionId: 13, lotNumber: 'LOTE-FECHO-2026-009', lotQuantity: 220, lotUnit: 'un.' },
  { id: 9, rawMaterialId: 9, sectionId: 13, lotNumber: 'LOTE-CABLAGEM-2026-010', lotQuantity: 500, lotUnit: 'un.' },
]

export const demoUnitMaterialLotUsages: UnitMaterialLotUsage[] = [
  { id: 1, productUnitId: 1, lotId: 1, associationType: 'Consumido', quantity: 8 },
  { id: 2, productUnitId: 1, lotId: 3, associationType: 'Consumido', quantity: 1 },
  { id: 3, productUnitId: 1, lotId: 6, associationType: 'Consumido', quantity: 2 },
  { id: 4, productUnitId: 6, lotId: 9, associationType: 'Consumido', quantity: 1 },
]

export const demoQuality: QualityRecord[] = [
  { id: 1, productUnitId: 1, checkpointId: 5, result: 'PASS', recordedAt: now, notes: 'Controlo final aprovado.' },
  { id: 2, productUnitId: 2, checkpointId: 4, result: 'PASS', recordedAt: now, notes: 'Pintura B aprovada sem desvios.' },
  { id: 3, productUnitId: 3, checkpointId: 3, result: 'FAIL', recordedAt: now, notes: 'Espessura de pintura fora de tolerância.' },
  { id: 4, productUnitId: 4, checkpointId: 2, result: 'FAIL', recordedAt: now, notes: 'Desalinhamento estrutural detetado na soldadura.' },
  { id: 5, productUnitId: 6, checkpointId: 5, result: 'PASS', recordedAt: now, notes: 'Unidade aprovada para armazenamento pós-linha.' },
  { id: 6, productUnitId: 7, checkpointId: 4, result: 'PASS', recordedAt: now, notes: 'Inspeção visual da pintura sem defeitos críticos.' },
]

export const demoNonconformities: NonconformityRecord[] = [
  { id: 1, productUnitId: 3, qualityResultId: 3, severity: 'Maior', status: 'Rework', description: 'Espessura de pintura fora de tolerância; encaminhar para retrabalho controlado.', createdAt: now },
  { id: 2, productUnitId: 4, qualityResultId: 4, severity: 'Crítica', status: 'Blocked', description: 'Desalinhamento estrutural na zona de soldadura; decisão de qualidade obrigatória.', createdAt: now },
  { id: 3, productUnitId: 7, qualityResultId: 6, severity: 'Menor', status: 'Open', description: 'Marcas superficiais ligeiras para acompanhamento na próxima inspeção.', createdAt: now },
]

export const demoReworkRecords: ReworkRecord[] = [
  { id: 1, productUnitId: 3, nonconformityId: 1, startedAt: now, status: 'Open', notes: 'Rever espessura de pintura, corrigir camada e repetir ponto de controlo.' },
  { id: 2, productUnitId: 4, nonconformityId: 2, startedAt: now, status: 'Open', notes: 'Validar desalinhamento estrutural antes de decidir recuperação ou sucata.' },
]

export const demoScrapRecords: ScrapRecord[] = [
  { id: 1, productUnitId: 4, nonconformityId: 2, scrappedAt: now, reason: 'Cenário demonstrativo de decisão de sucata por desalinhamento estrutural.' },
]

export const demoRackSupportAssignments: RackSupportAssignment[] = [
  { id: 1, rackId: 1, supportId: 6, dateTimeIn: now },
]

export const demoSupportLocalizationHistory: SupportLocalizationHistory[] = [
  { id: 1, supportId: 1, sectionId: 3, dateTime: now, eventType: 'SupportAssigned' },
  { id: 2, supportId: 1, sectionId: 4, dateTime: now, eventType: 'Movement' },
  { id: 3, supportId: 1, sectionId: 9, dateTime: now, eventType: 'LineTransfer' },
  { id: 4, supportId: 3, sectionId: 14, dateTime: now, eventType: 'LineTransfer' },
  { id: 5, supportId: 6, sectionId: 7, dateTime: now, eventType: 'TransferToRack' },
]

export const demoPredictions: PredictionRecord[] = [
  { id: 1, manufacturingOrderId: 1, modelVersion: 'previsao-v1', modelType: 'Tempo de conclusão (demo)', lastDate: now, createdAt: now, confidence: 0.78, status: 'Demonstração' },
  { id: 2, manufacturingOrderId: 2, modelVersion: 'previsao-v1', modelType: 'Risco de atraso (demo)', lastDate: now, createdAt: now, confidence: 0.64, status: 'Demonstração' },
]
