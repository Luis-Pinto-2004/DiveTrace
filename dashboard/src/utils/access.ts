/**
 * Mapa central de perfis e acessos.
 *
 * Fonte única para chaves de perfil, etiquetas PT-PT, papel no backend
 * e vista inicial por perfil. Evita duplicação entre `App.vue`, o router
 * e os testes. Os valores são consistentes com a configuração existente.
 */

export type RoleKey =
  | 'admin'
  | 'supervisor'
  | 'operator'
  | 'quality'
  | 'logistics'
  | 'client'
  | 'demoViewer'

export type ViewKey = string

export interface RoleProfile {
  key: RoleKey
  /** Etiqueta visível ao utilizador (PT-PT). */
  label: string
  /** Papel correspondente no backend (.NET). */
  backendRole: string
  /** Cargo apresentado no perfil. */
  jobTitle: string
  /** Vista inicial ao autenticar. */
  homeView: ViewKey
}

export const ROLE_PROFILES: Record<RoleKey, RoleProfile> = {
  admin: {
    key: 'admin',
    label: 'Administrador',
    backendRole: 'Administrator',
    jobTitle: 'Administrador do sistema',
    homeView: 'overview',
  },
  supervisor: {
    key: 'supervisor',
    label: 'Supervisor',
    backendRole: 'Supervisor',
    jobTitle: 'Supervisor de produção',
    homeView: 'overview',
  },
  operator: {
    key: 'operator',
    label: 'Operador',
    backendRole: 'Operator',
    jobTitle: 'Operador de produção',
    homeView: 'operator',
  },
  quality: {
    key: 'quality',
    label: 'Técnico de qualidade',
    backendRole: 'QualityTechnician',
    jobTitle: 'Técnico de qualidade',
    homeView: 'reconditioning',
  },
  logistics: {
    key: 'logistics',
    label: 'Logística',
    backendRole: 'Logistics',
    jobTitle: 'Técnico de logística',
    homeView: 'racks',
  },
  client: {
    key: 'client',
    label: 'Cliente',
    backendRole: 'Customer',
    jobTitle: 'Cliente',
    homeView: 'customerOrders',
  },
  demoViewer: {
    key: 'demoViewer',
    label: 'Visitante demo',
    backendRole: 'DemoViewer',
    jobTitle: 'Visitante',
    homeView: 'overview',
  },
}

/** Vista inicial para um perfil (com fallback seguro). */
export function homeViewForRole(roleKey: RoleKey | undefined | null): ViewKey {
  return ROLE_PROFILES[roleKey as RoleKey]?.homeView ?? 'overview'
}

/** Papel de backend para um perfil. */
export function backendRoleFor(roleKey: RoleKey | undefined | null): string {
  return ROLE_PROFILES[roleKey as RoleKey]?.backendRole ?? 'DemoViewer'
}

/** Etiqueta PT-PT para um perfil. */
export function roleLabel(roleKey: RoleKey | undefined | null): string {
  return ROLE_PROFILES[roleKey as RoleKey]?.label ?? String(roleKey ?? '')
}

/** Verdadeiro se o perfil tem privilégios de administração. */
export function isAdminRole(roleKey: RoleKey | undefined | null): boolean {
  return roleKey === 'admin'
}
