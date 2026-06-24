import { homeViewForRole, isAdminRole, type RoleKey } from '@/utils/access'
import { findViewByKey, type ViewRoute } from './routes'

export interface GuardSession {
  isAuthenticated: boolean
  roleKey: RoleKey | null
  permissions: string[]
}

export type GuardOutcome =
  | { type: 'allow' }
  | { type: 'redirect'; toKey: string; reason: 'unauthenticated' | 'forbidden' | 'unknown-view' }

/**
 * Determina se um perfil pode aceder a uma vista.
 * Admin tem sempre acesso. Caso contrário verifica perfis permitidos
 * e a permissão necessária declarada na rota.
 */
export function canAccessView(route: ViewRoute, session: GuardSession): boolean {
  if (isAdminRole(session.roleKey)) return true
  if (route.roles && route.roles.length > 0) {
    if (!session.roleKey || !route.roles.includes(session.roleKey)) return false
  }
  if (route.permission) {
    return session.permissions.includes(route.permission)
  }
  return true
}

/**
 * Lógica de guard pura: dado o destino e a sessão, devolve a decisão.
 * Não toca em Vue Router — é testável de forma determinística e é
 * reutilizada pelo `beforeEach` do router.
 */
export function resolveNavigation(targetKey: string, session: GuardSession): GuardOutcome {
  if (!session.isAuthenticated) {
    return { type: 'redirect', toKey: 'login', reason: 'unauthenticated' }
  }
  const route = findViewByKey(targetKey)
  if (!route) {
    return { type: 'redirect', toKey: homeViewForRole(session.roleKey), reason: 'unknown-view' }
  }
  if (!canAccessView(route, session)) {
    return { type: 'redirect', toKey: homeViewForRole(session.roleKey), reason: 'forbidden' }
  }
  return { type: 'allow' }
}
