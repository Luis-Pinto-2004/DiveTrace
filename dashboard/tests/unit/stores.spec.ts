import { describe, it, expect } from 'vitest'
import { useAuthStore } from '@/stores/auth'
import { usePreferencesStore } from '@/stores/preferences'

describe('auth store', () => {
  it('começa sem sessão', () => {
    const auth = useAuthStore()
    expect(auth.isAuthenticated).toBe(false)
    expect(auth.roleKey).toBeNull()
  })

  it('persiste o utilizador e expõe getters de perfil', () => {
    const auth = useAuthStore()
    auth.setUser({
      id: 'q1',
      name: 'Ana Qualidade',
      username: 'qualidade',
      role: 'Técnico de qualidade',
      roleKey: 'quality',
      permissions: ['Quality.View'],
    })
    expect(auth.isAuthenticated).toBe(true)
    expect(auth.backendRole).toBe('QualityTechnician')
    expect(auth.homeView).toBe('reconditioning')
    expect(localStorage.getItem('user')).toContain('qualidade')
  })

  it('admin tem acesso a qualquer permissão', () => {
    const auth = useAuthStore()
    auth.setUser({
      id: 'a',
      name: 'Admin',
      username: 'admin',
      role: 'Administrador',
      roleKey: 'admin',
      permissions: [],
    })
    expect(auth.can('MasterData.Manage')).toBe(true)
  })

  it('logout limpa a sessão', () => {
    const auth = useAuthStore()
    auth.setUser({
      id: 'o',
      name: 'Op',
      username: 'operador',
      role: 'Operador',
      roleKey: 'operator',
    })
    auth.logout()
    expect(auth.isAuthenticated).toBe(false)
    expect(localStorage.getItem('user')).toBeNull()
  })
})

describe('preferences store', () => {
  it('alterna o tema e persiste', () => {
    const prefs = usePreferencesStore()
    expect(prefs.theme).toBe('light')
    prefs.toggleTheme()
    expect(prefs.theme).toBe('dark')
    expect(prefs.isDark).toBe(true)
    expect(localStorage.getItem('theme')).toBe('dark')
  })

  it('aceita apenas locales suportados', () => {
    const prefs = usePreferencesStore()
    prefs.setLocale('en')
    expect(prefs.locale).toBe('en')
    prefs.setLocale('pt-PT')
    expect(prefs.locale).toBe('pt-PT')
  })
})
