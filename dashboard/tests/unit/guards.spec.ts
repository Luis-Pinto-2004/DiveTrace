import { describe, it, expect } from 'vitest'
import { resolveNavigation, canAccessView, type GuardSession } from '@/router/guards'
import { findViewByKey } from '@/router/routes'

const admin: GuardSession = { isAuthenticated: true, roleKey: 'admin', permissions: [] }
const anon: GuardSession = { isAuthenticated: false, roleKey: null, permissions: [] }
const operator: GuardSession = {
  isAuthenticated: true,
  roleKey: 'operator',
  permissions: ['ProductUnits.View'],
}
const client: GuardSession = {
  isAuthenticated: true,
  roleKey: 'client',
  permissions: ['Orders.View'],
}

describe('resolveNavigation', () => {
  it('redireciona utilizadores não autenticados para login', () => {
    const outcome = resolveNavigation('overview', anon)
    expect(outcome).toEqual({ type: 'redirect', toKey: 'login', reason: 'unauthenticated' })
  })

  it('permite admin em qualquer vista', () => {
    expect(resolveNavigation('parameters', admin).type).toBe('allow')
    expect(resolveNavigation('traceGraph', admin).type).toBe('allow')
  })

  it('permite operador na bancada (tem permissão)', () => {
    expect(resolveNavigation('operator', operator).type).toBe('allow')
  })

  it('bloqueia operador na administração (sem permissão)', () => {
    const outcome = resolveNavigation('parameters', operator)
    expect(outcome.type).toBe('redirect')
    if (outcome.type === 'redirect') expect(outcome.reason).toBe('forbidden')
  })

  it('isola o cliente das vistas internas', () => {
    const outcome = resolveNavigation('operator', client)
    expect(outcome.type).toBe('redirect')
  })

  it('redireciona vistas desconhecidas para a home do perfil', () => {
    const outcome = resolveNavigation('vista-inexistente', operator)
    expect(outcome.type).toBe('redirect')
    if (outcome.type === 'redirect') {
      expect(outcome.reason).toBe('unknown-view')
      expect(outcome.toKey).toBe('operator')
    }
  })
})

describe('canAccessView', () => {
  it('respeita o campo roles das rotas de cliente', () => {
    const route = findViewByKey('customerNewOrder')!
    expect(canAccessView(route, client)).toBe(false) // falta Orders.Manage
    expect(
      canAccessView(route, { ...client, permissions: ['Orders.Manage'] }),
    ).toBe(true)
    expect(canAccessView(route, operator)).toBe(false) // perfil errado
  })
})
