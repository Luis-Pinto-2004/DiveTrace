import type { RoleKey } from '@/utils/access'

export interface NavLink {
  to: string
  label: string
  icon: string
  /** Perfis com acesso (vazio = todos os autenticados). */
  roles?: RoleKey[]
}

export interface NavSection {
  title: string
  links: NavLink[]
}

// Perfis "internos" (fábrica) — tudo menos o cliente.
const INTERNAL: RoleKey[] = ['admin', 'supervisor', 'operator', 'quality', 'logistics']

export const NAV_SECTIONS: NavSection[] = [
  {
    title: 'Operação',
    links: [
      { to: '/cockpit', label: 'Painel de operações', icon: 'grid', roles: INTERNAL },
      { to: '/ordens', label: 'Encomendas e produção', icon: 'orders', roles: ['admin', 'supervisor'] },
      { to: '/linhas', label: 'Análise por linha', icon: 'line', roles: INTERNAL },
    ],
  },
  {
    title: 'Decisão',
    links: [
      { to: '/qualidade', label: 'Quadro de qualidade', icon: 'quality', roles: ['admin', 'supervisor', 'quality'] },
      { to: '/rastreabilidade', label: 'Mapa de rastreabilidade', icon: 'map', roles: INTERNAL },
    ],
  },
  {
    title: 'Cliente',
    links: [{ to: '/cliente', label: 'As minhas encomendas', icon: 'customer', roles: ['admin', 'client'] }],
  },
  {
    title: 'Administração',
    links: [
      { to: '/funcionarios', label: 'Funcionários e recursos', icon: 'user', roles: ['admin', 'supervisor'] },
      { to: '/gestao', label: 'Gestão de dados', icon: 'settings', roles: ['admin', 'logistics'] },
    ],
  },
  {
    title: 'Conta',
    links: [{ to: '/perfil', label: 'O meu perfil', icon: 'user' }],
  },
]

export function visibleSections(roleKey: RoleKey | null): NavSection[] {
  return NAV_SECTIONS.map((section) => ({
    ...section,
    links: section.links.filter((link) => !link.roles || (roleKey && link.roles.includes(roleKey))),
  })).filter((section) => section.links.length > 0)
}
