import { describe, it, expect } from 'vitest'
import { useOperationsStore } from '@/stores/operations'

function advanceToCompletion(unitId: string) {
  const ops = useOperationsStore()
  let guard = 0
  while (ops.unitById(unitId)?.state !== 'completed' && guard < 12) {
    ops.advanceUnit(unitId)
    guard += 1
  }
}

describe('operations store — ciclo de encomenda', () => {
  it('submete, aceita, inicia produção e conclui', () => {
    const ops = useOperationsStore()
    const order = ops.placeOrder({ customer: 'Teste', product: 'Capô', quantity: 3, lineId: 'L1' })
    expect(order.status).toBe('submitted')
    expect(ops.pendingOrders.some((o) => o.id === order.id)).toBe(true)

    ops.acceptOrder(order.id)
    expect(ops.orders.find((o) => o.id === order.id)?.status).toBe('accepted')
    const units = ops.units.filter((u) => u.orderId === order.id)
    expect(units).toHaveLength(3)
    const firstSection = ops.lineById('L1')!.sections[0].id
    expect(units.every((u) => u.sectionId === firstSection && u.state === 'queued')).toBe(true)

    ops.startProduction(order.id)
    expect(ops.orders.find((o) => o.id === order.id)?.status).toBe('in_production')
    expect(ops.units.filter((u) => u.orderId === order.id).every((u) => u.state === 'active')).toBe(true)

    units.forEach((u) => advanceToCompletion(u.id))
    expect(ops.units.filter((u) => u.orderId === order.id).every((u) => u.state === 'completed')).toBe(true)
    expect(ops.orders.find((o) => o.id === order.id)?.status).toBe('completed')
  })

  it('recusar uma encomenda submetida', () => {
    const ops = useOperationsStore()
    const order = ops.placeOrder({ customer: 'X', product: 'Tejadilho', quantity: 2, lineId: 'L2' })
    ops.rejectOrder(order.id, 'sem capacidade')
    expect(ops.orders.find((o) => o.id === order.id)?.status).toBe('rejected')
    // não cria unidades
    expect(ops.units.filter((u) => u.orderId === order.id)).toHaveLength(0)
  })

  it('cliente cancela apenas encomendas ainda não aceites', () => {
    const ops = useOperationsStore()
    const order = ops.placeOrder({ customer: 'AutoEuropa Demo', product: 'Capô', quantity: 2, lineId: 'L1' })
    ops.cancelOrder(order.id)
    expect(ops.orders.find((o) => o.id === order.id)?.status).toBe('cancelled')

    // depois de aceite já não pode ser cancelada
    const order2 = ops.placeOrder({ customer: 'AutoEuropa Demo', product: 'Capô', quantity: 1, lineId: 'L1' })
    ops.acceptOrder(order2.id)
    ops.cancelOrder(order2.id)
    expect(ops.orders.find((o) => o.id === order2.id)?.status).toBe('accepted')
  })
})

describe('operations store — transferências', () => {
  it('transfere dentro da mesma linha', () => {
    const ops = useOperationsStore()
    const unit = ops.units.find((u) => u.lineId === 'L1' && u.state !== 'completed' && u.state !== 'scrap')
    expect(unit).toBeDefined()
    const target = ops.lineById('L1')!.sections[3].id
    ops.transferUnit(unit!.id, target)
    expect(ops.unitById(unit!.id)?.sectionId).toBe(target)
    expect(ops.unitById(unit!.id)?.lineId).toBe('L1')
  })

  it('transfere entre linhas (interlinha)', () => {
    const ops = useOperationsStore()
    const unit = ops.units.find((u) => u.lineId === 'L1' && u.state !== 'completed' && u.state !== 'scrap')
    expect(unit).toBeDefined()
    const target = ops.lineById('L2')!.sections[1].id
    ops.transferUnitToLine(unit!.id, 'L2', target)
    expect(ops.unitById(unit!.id)?.lineId).toBe('L2')
    expect(ops.unitById(unit!.id)?.sectionId).toBe(target)
    expect(ops.unitById(unit!.id)?.state).toBe('transfer')
  })
})

describe('operations store — indicadores', () => {
  it('calcula WIP, gargalos e distribuição de estados', () => {
    const ops = useOperationsStore()
    expect(ops.wipCount).toBeGreaterThan(0)
    expect(Array.isArray(ops.bottlenecks)).toBe(true)
    const totalDist = ops.statusDistribution.reduce((acc, e) => acc + e.count, 0)
    expect(totalDist).toBeGreaterThan(0)
    expect(ops.firstPassYield).toBeGreaterThanOrEqual(0)
    expect(ops.firstPassYield).toBeLessThanOrEqual(1)
  })

  it('taxa de aprovação conta aprovadas sobre avaliadas (não só concluídas)', () => {
    const ops = useOperationsStore()
    const a = ops.approvalRate
    // seed: 5 aprovadas (1 rack + 2 prontas + 2 concluídas) + 1 análise + 1 recuperável + 1 recondicionamento + 1 sucata => 5/9
    expect(a.approved).toBe(5)
    expect(a.evaluated).toBe(9)
    expect(a.rate).toBeCloseTo(5 / 9, 5)
  })

  it('regista atividade ao agir sobre o sistema', () => {
    const ops = useOperationsStore()
    const before = ops.activity.length
    ops.placeOrder({ customer: 'Y', product: 'Capô', quantity: 1, lineId: 'L3' })
    expect(ops.activity.length).toBeGreaterThan(before)
  })
})

describe('operations store — conclusão de encomenda pronta', () => {
  it('conclui uma encomenda pronta (unidades concluídas + 100%)', () => {
    const ops = useOperationsStore()
    const ready = ops.orders.find((o) => o.status === 'ready')
    expect(ready).toBeDefined()
    const ok = ops.deliverOrder(ready!.id)
    expect(ok).toBe(true)
    expect(ops.orders.find((o) => o.id === ready!.id)?.status).toBe('completed')
    const units = ops.units.filter((u) => u.orderId === ready!.id)
    expect(units.every((u) => u.state === 'completed')).toBe(true)
    expect(units.every((u) => u.supportId === null)).toBe(true)
  })

  it('não conclui encomendas que não estão prontas', () => {
    const ops = useOperationsStore()
    const inProd = ops.orders.find((o) => o.status === 'in_production')
    expect(inProd).toBeDefined()
    expect(ops.deliverOrder(inProd!.id)).toBe(false)
    expect(ops.orders.find((o) => o.id === inProd!.id)?.status).toBe('in_production')
  })
})

describe('operations store — regras de capacidade', () => {
  it('uma unidade não avança para uma secção cheia', () => {
    const ops = useOperationsStore()
    const order = ops.placeOrder({ customer: 'Teste', product: 'Capô', quantity: 2, lineId: 'L1' })
    ops.acceptOrder(order.id)
    ops.startProduction(order.id)
    const [a, b] = ops.units.filter((u) => u.orderId === order.id)
    const mp = ops.lineById('L1')!.sections[0].id // SEC-MP (buffer, cap. alargada)
    const atrib = ops.lineById('L1')!.sections[1].id // cap. 1

    ops.advanceUnit(a.id)
    expect(ops.unitById(a.id)!.sectionId).toBe(atrib)
    expect(ops.isSectionFull(atrib)).toBe(true)

    // b tenta avançar para a mesma secção (cheia): fica em espera, não sobrepõe.
    ops.advanceUnit(b.id)
    expect(ops.unitById(b.id)!.sectionId).toBe(mp)
    expect(ops.advanceInfo(b.id)!.blocked).toBe(true)
    expect(ops.sectionOccupancy(atrib)).toBe(1)
  })

  it('a transferência entre secções respeita a capacidade', () => {
    const ops = useOperationsStore()
    const order = ops.placeOrder({ customer: 'Teste', product: 'Capô', quantity: 2, lineId: 'L1' })
    ops.acceptOrder(order.id)
    ops.startProduction(order.id)
    const [a, b] = ops.units.filter((u) => u.orderId === order.id)
    const mp = ops.lineById('L1')!.sections[0].id
    const atrib = ops.lineById('L1')!.sections[1].id

    ops.advanceUnit(a.id)
    expect(ops.unitById(a.id)!.sectionId).toBe(atrib)

    // Transferir b para a secção cheia: não permitido, b fica.
    ops.transferUnit(b.id, atrib)
    expect(ops.unitById(b.id)!.sectionId).toBe(mp)
  })

  it('uma decisão de qualidade não move a unidade para uma secção cheia', () => {
    const ops = useOperationsStore()
    const cap = ops.sectionCapacity('SEC-RETRAB')
    const cqUnits = ops.units.filter((u) => u.sectionId === 'SEC-CQ' && u.state !== 'scrap')

    // Encher o recondicionamento até à capacidade.
    let i = 0
    while (ops.sectionOccupancy('SEC-RETRAB') < cap && i < cqUnits.length) {
      ops.decideQuality(cqUnits[i].id, 'recondition')
      i += 1
    }
    expect(ops.isSectionFull('SEC-RETRAB')).toBe(true)

    // Uma unidade ainda em CQ não consegue ir para o recondicionamento cheio.
    const remaining = cqUnits.find((u) => u.sectionId === 'SEC-CQ')
    if (remaining) {
      expect(ops.decisionTarget(remaining.id, 'recondition')!.blocked).toBe(true)
      ops.decideQuality(remaining.id, 'recondition')
      expect(ops.unitById(remaining.id)!.sectionId).toBe('SEC-CQ')
    }
  })
})
