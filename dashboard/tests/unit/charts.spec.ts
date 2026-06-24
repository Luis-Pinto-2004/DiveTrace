import { describe, it, expect } from 'vitest'
import { linearScale, linePath, areaPath, donutArcs, gaugeArc } from '@/utils/charts'

describe('chart helpers', () => {
  it('linearScale mapeia o domínio para o intervalo', () => {
    const scale = linearScale(0, 10, 0, 100)
    expect(scale(0)).toBe(0)
    expect(scale(5)).toBe(50)
    expect(scale(10)).toBe(100)
  })

  it('linePath devolve um caminho que começa em M', () => {
    const path = linePath([1, 2, 3], { width: 100, height: 50, padding: 5 })
    expect(path.startsWith('M')).toBe(true)
    expect(path).toContain('L')
  })

  it('areaPath fecha o caminho com Z', () => {
    const path = areaPath([1, 2, 3], { width: 100, height: 50, padding: 5 })
    expect(path.trim().endsWith('Z')).toBe(true)
  })

  it('donutArcs devolve um arco por valor e frações que somam ~1', () => {
    const arcs = donutArcs([2, 3, 5])
    expect(arcs).toHaveLength(3)
    const sum = arcs.reduce((acc, a) => acc + a.fraction, 0)
    expect(sum).toBeCloseTo(1, 5)
    expect(arcs[0].path.startsWith('M')).toBe(true)
  })

  it('gaugeArc produz um arco SVG válido', () => {
    const arc = gaugeArc(0.5)
    expect(arc).toContain('A')
    expect(arc.startsWith('M')).toBe(true)
  })

  it('gaugeArc satura entre 0 e 1', () => {
    expect(() => gaugeArc(-1)).not.toThrow()
    expect(() => gaugeArc(2)).not.toThrow()
  })
})
