import { defineStore } from 'pinia'
import {
  ROLE_PROFILES,
  backendRoleFor,
  homeViewForRole,
  isAdminRole,
  roleLabel,
  type RoleKey,
} from '@/utils/access'

const USER_KEY = 'user'

/**
 * Contexto de utilizador autenticado (demo/local).
 * Subconjunto estável da estrutura usada em `App.vue`, partilhando
 * a mesma chave de localStorage para coerência durante a migração.
 */
export interface SessionUser {
  id: string
  name: string
  username: string
  email?: string
  phone?: string
  role: string
  roleKey: RoleKey
  permissions?: string[]
}

function readStoredUser(): SessionUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as Partial<SessionUser>
    if (!parsed.username || !parsed.roleKey) return null
    return parsed as SessionUser
  } catch {
    return null
  }
}

/**
 * NOTA: A autenticação é local/demo. Não representa segurança de
 * produção — ver docs/NOTAS_TECNICAS.md. As permissões efetivas são
 * sempre reavaliadas no backend por perfil.
 */
export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: readStoredUser() as SessionUser | null,
  }),
  getters: {
    isAuthenticated: (state): boolean => state.user !== null,
    roleKey: (state): RoleKey | null => state.user?.roleKey ?? null,
    roleLabelPt(): string {
      return roleLabel(this.roleKey)
    },
    backendRole(): string {
      return backendRoleFor(this.roleKey)
    },
    isAdmin(): boolean {
      return isAdminRole(this.roleKey)
    },
    homeView(): string {
      return homeViewForRole(this.roleKey)
    },
    permissions: (state): string[] => state.user?.permissions ?? [],
  },
  actions: {
    setUser(user: SessionUser | null) {
      this.user = user
      try {
        if (user) localStorage.setItem(USER_KEY, JSON.stringify(user))
        else localStorage.removeItem(USER_KEY)
      } catch {
        /* armazenamento indisponível */
      }
    },
    /** Atualiza os dados pessoais do próprio utilizador (e persiste). */
    updateProfile(patch: { name?: string; email?: string; phone?: string }) {
      if (!this.user) return
      const next: SessionUser = { ...this.user }
      if (patch.name !== undefined) {
        const trimmed = patch.name.trim()
        if (trimmed) next.name = trimmed
      }
      if (patch.email !== undefined) next.email = patch.email.trim() || undefined
      if (patch.phone !== undefined) next.phone = patch.phone.trim() || undefined
      this.setUser(next)
    },
    /** Verifica uma permissão pontual. Admin tem acesso total. */
    can(permission: string): boolean {
      if (this.isAdmin) return true
      return this.permissions.includes(permission)
    },
    logout() {
      this.setUser(null)
    },
  },
})

export { ROLE_PROFILES }
