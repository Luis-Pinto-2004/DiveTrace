import { describe, it, expect } from 'vitest'
import {
  ROLE_PROFILES,
  homeViewForRole,
  backendRoleFor,
  roleLabel,
  isAdminRole,
} from '@/utils/access'

describe('access map', () => {
  it('define todos os perfis demo esperados', () => {
    const keys = Object.keys(ROLE_PROFILES)
    expect(keys).toContain('admin')
    expect(keys).toContain('supervisor')
    expect(keys).toContain('operator')
    expect(keys).toContain('quality')
    expect(keys).toContain('client')
    expect(keys).not.toContain('logistics')
  })

  it('homeViewForRole devolve a vista inicial correta por perfil', () => {
    expect(homeViewForRole('supervisor')).toBe('overview')
    expect(homeViewForRole('operator')).toBe('operator')
    expect(homeViewForRole('quality')).toBe('reconditioning')
    expect(homeViewForRole('client')).toBe('customerOrders')
  })

  it('homeViewForRole usa fallback seguro para perfil desconhecido', () => {
    expect(homeViewForRole(undefined)).toBe('overview')
    expect(homeViewForRole(null)).toBe('overview')
  })

  it('backendRoleFor mapeia para os papéis do backend .NET', () => {
    expect(backendRoleFor('quality')).toBe('QualityTechnician')
    expect(backendRoleFor('client')).toBe('Customer')
    expect(backendRoleFor(undefined)).toBe('DemoViewer')
  })

  it('roleLabel devolve etiquetas PT-PT', () => {
    expect(roleLabel('admin')).toBe('Administrador')
    expect(roleLabel('supervisor')).toBe('Supervisor')
  })

  it('isAdminRole distingue o administrador', () => {
    expect(isAdminRole('admin')).toBe(true)
    expect(isAdminRole('operator')).toBe(false)
    expect(isAdminRole(null)).toBe(false)
  })
})
