import { describe, it, expect } from 'vitest'
import { statusTone, statusBadgeClass, badgeClassForTone } from '@/utils/status'

describe('statusTone', () => {
  it('mapeia estados críticos para "critical"', () => {
    expect(statusTone('Failed')).toBe('critical')
    expect(statusTone('Blocked')).toBe('critical')
    expect(statusTone('Scrap')).toBe('critical')
    expect(statusTone('Bloqueado')).toBe('critical')
    expect(statusTone('Sucata')).toBe('critical')
  })

  it('mapeia retrabalho/pendente para "warn"', () => {
    expect(statusTone('Rework')).toBe('warn')
    expect(statusTone('Pending')).toBe('warn')
    expect(statusTone('Recondition')).toBe('warn')
    expect(statusTone('Retrabalho')).toBe('warn')
  })

  it('mapeia fluxo normal para "info"', () => {
    expect(statusTone('In Progress')).toBe('info')
    expect(statusTone('Transfer')).toBe('info')
    expect(statusTone('Active')).toBe('info')
  })

  it('mapeia concluído/disponível para "ok"', () => {
    expect(statusTone('Completed')).toBe('ok')
    expect(statusTone('Available')).toBe('ok')
    expect(statusTone('Passed')).toBe('ok')
    expect(statusTone('Aprovado')).toBe('ok')
  })

  it('devolve "neutral" para vazio ou desconhecido', () => {
    expect(statusTone('')).toBe('neutral')
    expect(statusTone(undefined)).toBe('neutral')
    expect(statusTone(null)).toBe('neutral')
    expect(statusTone('xpto-desconhecido')).toBe('neutral')
  })

  it('é insensível a maiúsculas/minúsculas', () => {
    expect(statusTone('FAILED')).toBe('critical')
    expect(statusTone('completed')).toBe('ok')
  })
})

describe('statusBadgeClass', () => {
  it('preserva o contrato de classes badge-* do App.vue original', () => {
    expect(statusBadgeClass('Failed')).toBe('badge-red')
    expect(statusBadgeClass('Rework')).toBe('badge-amber')
    expect(statusBadgeClass('Transfer')).toBe('badge-blue')
    expect(statusBadgeClass('Completed')).toBe('badge-green')
    expect(statusBadgeClass('')).toBe('badge-gray')
  })
})

describe('badgeClassForTone', () => {
  it('mapeia cada tom para a classe correta', () => {
    expect(badgeClassForTone('ok')).toBe('badge-green')
    expect(badgeClassForTone('critical')).toBe('badge-red')
    expect(badgeClassForTone('neutral')).toBe('badge-gray')
  })
})
