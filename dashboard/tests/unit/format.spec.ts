import { describe, it, expect } from 'vitest'
import {
  formatDate,
  formatNumber,
  formatPercent,
  formatDurationMinutes,
} from '@/utils/format'

describe('formatNumber', () => {
  it('devolve hífen para valores inválidos', () => {
    expect(formatNumber(null)).toBe('-')
    expect(formatNumber(undefined)).toBe('-')
    expect(formatNumber(Number.NaN)).toBe('-')
  })

  it('formata inteiros', () => {
    expect(formatNumber(1234)).toMatch(/1.?234/)
  })
})

describe('formatPercent', () => {
  it('converte fração em percentagem', () => {
    expect(formatPercent(0.42)).toContain('42')
    expect(formatPercent(1)).toContain('100')
  })

  it('devolve hífen para inválidos', () => {
    expect(formatPercent(null)).toBe('-')
  })
})

describe('formatDurationMinutes', () => {
  it('formata minutos abaixo de uma hora', () => {
    expect(formatDurationMinutes(35)).toBe('35 min')
  })

  it('formata horas exatas', () => {
    expect(formatDurationMinutes(120)).toBe('2 h')
  })

  it('formata horas e minutos', () => {
    expect(formatDurationMinutes(95)).toBe('1 h 35 min')
  })

  it('devolve hífen para inválidos', () => {
    expect(formatDurationMinutes(null)).toBe('-')
  })
})

describe('formatDate', () => {
  it('devolve hífen para datas inválidas', () => {
    expect(formatDate('not-a-date')).toBe('-')
    expect(formatDate(null)).toBe('-')
  })

  it('formata uma data ISO válida', () => {
    // O ano pode sair com 2 ou 4 dígitos consoante os dados ICU/locale.
    expect(formatDate('2026-06-17T10:00:00Z')).toMatch(/\d{2}\/\d{2}\/\d{2,4}/)
  })
})
