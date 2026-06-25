import type { SessionUser } from '@/stores/auth'
import type { RoleKey } from '@/utils/access'

export interface DemoCredential extends SessionUser {
  password: string
}

/**
 * Utilizadores demo (local). As credenciais espelham o interface
 * clássico, por isso a sessão é coerente entre os dois modos.
 * NÃO representa segurança de produção.
 */
export const DEMO_USERS: DemoCredential[] = [
  {
    id: 'admin',
    name: 'Administrador',
    username: 'admin',
    email: 'admin@drivetrace.local',
    role: 'Administrador',
    roleKey: 'admin',
    password: 'admin',
    permissions: [],
  },
  {
    id: 'supervisor',
    name: 'Supervisor de produção',
    username: 'supervisor',
    email: 'supervisor@drivetrace.local',
    role: 'Supervisor',
    roleKey: 'supervisor',
    password: 'supervisor',
    permissions: ['Orders.View', 'Orders.Manage', 'ProductUnits.View', 'ProductUnits.Trace', 'Quality.View', 'Racks.View', 'Supports.Manage', 'Materials.View', 'Data.Manage'],
  },
  {
    id: 'operador',
    name: 'Operador de linha',
    username: 'operador',
    email: 'operador@drivetrace.local',
    role: 'Operador',
    roleKey: 'operator',
    password: 'operador',
    permissions: ['ProductUnits.View', 'ProductUnits.Trace'],
  },
  {
    id: 'qualidade',
    name: 'Técnico de qualidade',
    username: 'qualidade',
    email: 'qualidade@drivetrace.local',
    role: 'Técnico de qualidade',
    roleKey: 'quality',
    password: 'qualidade',
    permissions: ['Quality.View', 'ProductUnits.View', 'ProductUnits.Trace'],
  },
  {
    id: 'cliente',
    name: 'Cliente industrial',
    username: 'cliente',
    email: 'cliente@drivetrace.local',
    role: 'Cliente',
    roleKey: 'client',
    password: 'cliente',
    permissions: ['Orders.View', 'Orders.Manage'],
  },
]

export function authenticate(username: string, password: string): SessionUser | null {
  const match = DEMO_USERS.find(
    (u) => u.username.toLowerCase() === username.trim().toLowerCase() && u.password === password,
  )
  if (!match) return null
  const { password: _pw, ...session } = match
  void _pw
  return session
}

export function demoUserByRole(roleKey: RoleKey): SessionUser | null {
  const match = DEMO_USERS.find((u) => u.roleKey === roleKey)
  if (!match) return null
  const { password: _pw, ...session } = match
  void _pw
  return session
}

/**
 * Sincroniza os dados pessoais editados com o registo local em memória,
 * para a sessão se manter coerente (conta local de demonstração).
 */
export function applyProfileUpdate(
  username: string,
  patch: { name?: string; email?: string; phone?: string },
): void {
  const user = DEMO_USERS.find((u) => u.username.toLowerCase() === username.toLowerCase())
  if (!user) return
  if (patch.name?.trim()) user.name = patch.name.trim()
  if (patch.email !== undefined) user.email = patch.email.trim() || undefined
  if (patch.phone !== undefined) user.phone = patch.phone.trim() || undefined
}

/**
 * Altera a palavra-passe do utilizador (conta local de demonstração).
 * Valida a palavra-passe atual; a alteração é válida durante a sessão.
 */
export function changePassword(
  username: string,
  current: string,
  next: string,
): { ok: boolean; error?: string } {
  const user = DEMO_USERS.find((u) => u.username.toLowerCase() === username.toLowerCase())
  if (!user) return { ok: false, error: 'Utilizador não encontrado.' }
  if (user.password !== current) return { ok: false, error: 'Palavra-passe atual incorreta.' }
  if (next.trim().length < 4) return { ok: false, error: 'A nova palavra-passe deve ter pelo menos 4 caracteres.' }
  user.password = next
  return { ok: true }
}
