<script setup lang="ts">
import { computed, defineAsyncComponent, onBeforeUnmount, onMounted, ref, watch, type Ref } from 'vue'
import logoUrl from './assets/branding/drivolution-logo.png'
import lineDoorUrl from './assets/branding/production-line-door.png'
import lineCarUrl from './assets/branding/production-line-car.png'
import CrudPanel from './components/CrudPanel.vue'
// Vistas pesadas carregadas sob procura (code-splitting por rota).
// Reduz drasticamente o chunk inicial: o motor de grafo (@vue-flow),
// os dashboards e o simulador deixam de pesar no arranque.
const GrafanaAnalyticsView = defineAsyncComponent(() => import('./components/GrafanaAnalyticsView.vue'))
const ProductionSimulatorView = defineAsyncComponent(() => import('./components/ProductionSimulatorView.vue'))
const ReconditioningView = defineAsyncComponent(() => import('./components/ReconditioningView.vue'))
const TraceGraphView = defineAsyncComponent(() => import('./components/TraceGraphView.vue'))
import { api, apiGet, apiPost, baseURL as apiBaseUrl, createEntity, deleteEntity, getApiErrorMessage, updateEntity } from './services/api'
import {
  demoCheckpoints,
  demoCustomers,
  demoDashboard,
  demoLots,
  demoManufacturingProcesses,
  demoManufacturingProcessPhases,
  demoManufacturingSectionPhases,
  demoMaterials,
  demoNonconformities,
  demoOrders,
  demoProductionLines,
  demoProductionLineSections,
  demoProducts,
  demoQuality,
  demoRackSupportAssignments,
  demoRacks,
  demoResources,
  demoReworkRecords,
  demoScrapRecords,
  demoSupportLocalizationHistory,
  demoSupports,
  demoUnitMaterialLotUsages,
  demoUnits,
  demoVariants,
  type Checkpoint,
  type Customer,
  type DashboardSummary,
  type LotRawMaterial,
  type ManufacturingOrder,
  type ManufacturingProcess,
  type ManufacturingProcessPhase,
  type ManufacturingSectionPhase,
  type NonconformityRecord,
  type ProductUnit,
  type Product,
  type ProductionLine,
  type ProductionLineSection,
  type QualityRecord,
  type Rack,
  type RackSupportAssignment,
  type RawMaterial,
  type ResourceRecord,
  type ReworkRecord,
  type ScrapRecord,
  type Support,
  type SupportLocalizationHistory,
  type UnitMaterialLotUsage,
  type Variant,
} from './data/demo'
import {
  locale,
  setLocale,
  theme,
  toggleTheme,
  t,
  translateMaterialName,
  translateQualityResult,
  translateSectionName,
  translateStatus,
  translateUnitOfMeasure,
  translateUnitType,
} from './prefs'

// ===== Authentication and user management =====
const isAuthenticated = ref(false)

type RoleKey = 'admin' | 'supervisor' | 'operator' | 'quality' | 'logistics' | 'client' | 'demoViewer'

type PermissionCatalog = {
  roles: Array<{ key: string; label: string; permissions: string[] }>
  permissions: Array<{ key: string; label: string }>
  activeRole: string
  activePermissions: string[]
}

const roleProfiles: Partial<Record<RoleKey, { label: string; backendRole: string; jobTitle: string }>> = {
  admin: { label: 'Administrador', backendRole: 'Administrator', jobTitle: 'Administrador do sistema' },
  supervisor: { label: 'Supervisor', backendRole: 'Supervisor', jobTitle: 'Supervisor de produção' },
  operator: { label: 'Operador', backendRole: 'Operator', jobTitle: 'Operador de produção' },
  quality: { label: 'Técnico de qualidade', backendRole: 'QualityTechnician', jobTitle: 'Técnico de qualidade' },
  logistics: { label: 'Logística', backendRole: 'Logistics', jobTitle: 'Técnico de logística' },
  client: { label: 'Cliente', backendRole: 'Customer', jobTitle: 'Cliente' },
}

const fallbackPermissionCatalog: PermissionCatalog = {
  roles: Object.entries(roleProfiles).map(([key, value]) => ({ key: value.backendRole, label: value.label, permissions: [] })),
  permissions: [],
  activeRole: 'Administrator',
  activePermissions: [
    'Users.Manage',
    'MasterData.Manage',
    'Orders.View',
    'Orders.Manage',
    'ProductUnits.View',
    'ProductUnits.Transfer',
    'ProductUnits.Trace',
    'Supports.Manage',
    'Quality.View',
    'Quality.Record',
    'Quality.Decide',
    'Racks.View',
    'Racks.Manage',
    'Materials.View',
    'Materials.Manage',
    'Grafana.View',
    'Fiware.View',
    'Fiware.Manage',
    'Simulation.Read',
    'Simulation.Run',
    'Simulation.Manage',
    'CustomerPortal.View',
    'OperationalEvents.View',
    'Reconditioning.Read',
    'Reconditioning.Write',
    'Reconditioning.Decide',
  ],
}

const frontendRolePermissions: Record<RoleKey, string[]> = {
  admin: fallbackPermissionCatalog.activePermissions,
  supervisor: [
    'Orders.View',
    'Orders.Manage',
    'ProductUnits.View',
    'ProductUnits.Transfer',
    'ProductUnits.Trace',
    'Supports.Manage',
    'Quality.View',
    'Racks.View',
    'Racks.Manage',
    'Materials.View',
    'Grafana.View',
    'OperationalEvents.View',
    'Simulation.Read',
    'Simulation.Run',
    'Simulation.Manage',
    'Reconditioning.Read',
    'Reconditioning.Write',
    'Reconditioning.Decide',
  ],
  operator: [
    'Orders.View',
    'ProductUnits.View',
    'ProductUnits.Transfer',
    'ProductUnits.Trace',
    'OperationalEvents.View',
    'Simulation.Read',
    'Simulation.Run',
    'Reconditioning.Read',
  ],
  quality: [
    'Orders.View',
    'ProductUnits.View',
    'ProductUnits.Trace',
    'Quality.View',
    'Quality.Record',
    'Quality.Decide',
    'OperationalEvents.View',
    'Simulation.Read',
    'Simulation.Run',
    'Reconditioning.Read',
    'Reconditioning.Write',
    'Reconditioning.Decide',
  ],
  logistics: [
    'ProductUnits.View',
    'ProductUnits.Trace',
    'Racks.View',
    'Racks.Manage',
    'Materials.View',
    'Materials.Manage',
    'Supports.Manage',
    'OperationalEvents.View',
    'Simulation.Read',
    'Simulation.Run',
    'Reconditioning.Read',
  ],
  client: ['CustomerPortal.View'],
  demoViewer: [
    'Orders.View',
    'ProductUnits.View',
    'ProductUnits.Trace',
    'Quality.View',
    'Racks.View',
    'Materials.View',
    'Fiware.View',
    'Grafana.View',
    'CustomerPortal.View',
    'OperationalEvents.View',
    'Simulation.Read',
    'Simulation.Run',
    'Reconditioning.Read',
  ],
}

type AuthUserContext = {
  id: string
  name: string
  username: string
  email?: string
  role: string
  roleKey: RoleKey
  permissions: string[]
  preferredLanguage?: string
  preferredTheme?: string
  department?: string
  associatedEntity?: { type?: string; id?: number; code?: string }
  customer?: { id?: number; customerCode?: string; name?: string; defaultPublicTrackingCode?: string }
  assignedLine?: { id?: number; code?: string; name?: string }
  assignedSection?: { id?: number; code?: string; name?: string; sectionType?: string }
}

type UserProfile = {
  id: string
  name: string
  username: string
  email: string
  role: string
  roleKey: RoleKey
  organization: string
  project: string
  jobTitle: string
  password: string
  active: boolean
  lastLogin?: string
}

const defaultUser: UserProfile = {
  id: 'admin',
  name: 'Administrador',
  username: 'admin',
  email: 'admin@drivetrace.local',
  role: 'Administrador',
  roleKey: 'admin',
  organization: 'DRIVOLUTION WP3',
  project: 'DriveTrace Core',
  jobTitle: 'Administrador do sistema',
  password: 'admin',
  active: true,
}

const defaultDemoUsers: UserProfile[] = [
  {
    id: 'operador',
    name: 'Operador de linha',
    username: 'operador',
    email: 'operador@drivetrace.local',
    role: 'Operador',
    roleKey: 'operator',
    organization: 'DRIVOLUTION WP3',
    project: 'DriveTrace Core',
    jobTitle: 'Operador de produção',
    password: 'operador',
    active: true,
  },
  {
    id: 'supervisor',
    name: 'Supervisor de produção',
    username: 'supervisor',
    email: 'supervisor@drivetrace.local',
    role: 'Supervisor',
    roleKey: 'supervisor',
    organization: 'DRIVOLUTION WP3',
    project: 'DriveTrace Core',
    jobTitle: 'Supervisor de produção',
    password: 'supervisor',
    active: true,
  },
  {
    id: 'qualidade',
    name: 'Técnico de qualidade',
    username: 'qualidade',
    email: 'qualidade@drivetrace.local',
    role: 'Técnico de qualidade',
    roleKey: 'quality',
    organization: 'DRIVOLUTION WP3',
    project: 'DriveTrace Core',
    jobTitle: 'Técnico de qualidade',
    password: 'qualidade',
    active: true,
  },
  {
    id: 'logistica',
    name: 'Técnico de logística',
    username: 'logistica',
    email: 'logistica@drivetrace.local',
    role: 'Logística',
    roleKey: 'logistics',
    organization: 'DRIVOLUTION WP3',
    project: 'DriveTrace Core',
    jobTitle: 'Técnico de logística',
    password: 'logistica',
    active: true,
  },
  {
    id: 'cliente',
    name: 'Cliente industrial',
    username: 'cliente',
    email: 'cliente@drivetrace.local',
    role: 'Cliente',
    roleKey: 'client',
    organization: 'AutoEuropa Demo',
    project: 'DriveTrace Core',
    jobTitle: 'Consulta de cliente',
    password: 'cliente',
    active: true,
  },
]

const users = ref<UserProfile[]>([])
const user = ref<UserProfile | null>(null)
const isRegistering = ref(false)
const loginForm = ref({ username: '', password: '' })
const emptyUserForm = () => ({ name: '', username: '', email: '', password: '', confirmPassword: '', roleKey: 'operator' as RoleKey })
const registerForm = ref(emptyUserForm())
const editingUsername = ref<string | null>(null)
const loginError = ref('')
const registerError = ref('')
const registerSuccess = ref('')
const isEditingProfile = ref(false)
const profileForm = ref({ name: '', email: '', organization: '', jobTitle: '' })
const profileError = ref('')
const profileSuccess = ref('')

function buildProfileForm(profile: UserProfile | null) {
  return {
    name: profile?.name ?? '',
    email: profile?.email ?? '',
    organization: profile?.organization ?? '',
    jobTitle: profile?.jobTitle ?? '',
  }
}

function getRoleLabel(roleKey: RoleKey) {
  return t(roleProfiles[roleKey]?.label ?? roleKey)
}

function buildUserRole(roleKey: RoleKey) {
  return roleProfiles[roleKey]?.label ?? roleKey
}

function defaultJobTitle(roleKey: RoleKey) {
  return roleProfiles[roleKey]?.jobTitle ?? 'Operador'
}

function persistUsers() {
  users.value = users.value.filter((profile) => profile.username !== 'demo' && profile.roleKey !== 'demoViewer')
  localStorage.setItem('users', JSON.stringify(users.value))
}

function persistActiveUser(profile: UserProfile | null) {
  if (profile) localStorage.setItem('user', JSON.stringify(profile))
  else localStorage.removeItem('user')
}

function setAuthenticatedUser(profile: UserProfile | null) {
  user.value = profile
  isAuthenticated.value = profile !== null
  persistActiveUser(profile)
  profileForm.value = buildProfileForm(profile)
  permissionNotice.value = ''
  if (profile) {
    activeView.value = homeViewForRole(profile.roleKey)
  }
  if (!profile) {
    isEditingProfile.value = false
    profileError.value = ''
    profileSuccess.value = ''
    serverUserContext.value = null
  }
}

function profileValue(value: string | undefined) {
  const normalized = value?.trim()
  return normalized ? normalized : t('Not set')
}

function profileJobTitle(value: string | undefined) {
  const normalized = value?.trim()
  return normalized ? t(normalized) : t('Not set')
}

function openProfileView() {
  activeView.value = 'profile'
  showUserMenu.value = false
  closeMobileSidebar()
}

function startProfileEdit() {
  if (!user.value) return
  profileForm.value = buildProfileForm(user.value)
  isEditingProfile.value = true
  profileError.value = ''
  profileSuccess.value = ''
}

function cancelProfileEdit() {
  profileForm.value = buildProfileForm(user.value)
  isEditingProfile.value = false
  profileError.value = ''
}

function saveProfileChanges() {
  profileError.value = ''
  profileSuccess.value = ''
  if (!user.value) {
    profileError.value = t('Error saving changes')
    return
  }

  const name = profileForm.value.name.trim()
  const email = profileForm.value.email.trim()
  const organization = profileForm.value.organization.trim()
  const jobTitle = profileForm.value.jobTitle.trim()

  if (!name) {
    profileError.value = t('Name is required')
    return
  }

  const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!emailPattern.test(email)) {
    profileError.value = t('Invalid email')
    return
  }

  const target = users.value.find((candidate) => candidate.username === user.value?.username)
  if (!target) {
    profileError.value = t('Error saving changes')
    return
  }

  target.name = name
  target.email = email
  target.organization = organization
  target.jobTitle = jobTitle

  setAuthenticatedUser(target)
  persistUsers()
  isEditingProfile.value = false
  profileSuccess.value = t('Changes saved successfully')
}

function ensureAdminUser() {
  const stored = localStorage.getItem('users')
  try {
    users.value = stored ? (JSON.parse(stored) as UserProfile[]) : []
  } catch {
    users.value = []
  }
  users.value = users.value.filter((profile) => profile.username !== 'demo' && profile.roleKey !== 'demoViewer')
  if (!users.value.some((u) => u.username === defaultUser.username)) {
    users.value.unshift({ ...defaultUser })
  }
  for (const demoUser of defaultDemoUsers) {
    if (!users.value.some((u) => u.username === demoUser.username)) {
      users.value.push({ ...demoUser })
    }
  }
  persistUsers()
}

function isAdmin(profile = user.value) {
  return profile?.roleKey === 'admin' || profile?.username === 'admin'
}

const permissionCatalog = ref<PermissionCatalog>({ ...fallbackPermissionCatalog })
const activePermissions = computed(() => new Set(permissionCatalog.value.activePermissions))

function backendRoleFor(roleKey?: RoleKey) {
  return roleProfiles[roleKey || 'admin']?.backendRole ?? 'DemoViewer'
}

function can(permission: string) {
  if (isAdmin()) return true
  if (!user.value) return activePermissions.value.has(permission)
  const localPermissions = frontendRolePermissions[user.value.roleKey] ?? []
  const catalogMatchesRole = permissionCatalog.value.activeRole === backendRoleFor(user.value.roleKey)
  return localPermissions.includes(permission) || (catalogMatchesRole && activePermissions.value.has(permission))
}

const canManageUsers = computed(() => can('Users.Manage'))

function activeAdminCount(nextUsers = users.value) {
  return nextUsers.filter((candidate) => candidate.roleKey === 'admin' && candidate.active !== false).length
}

function wouldKeepAnAdmin(username: string, nextRoleKey?: RoleKey, nextActive?: boolean) {
  const projected = users.value.map((candidate) => {
    if (candidate.username !== username) return candidate
    return {
      ...candidate,
      roleKey: nextRoleKey ?? candidate.roleKey,
      active: nextActive ?? candidate.active,
    }
  })
  return activeAdminCount(projected) > 0
}

function resetUserForm(clearMessages = true) {
  registerForm.value = emptyUserForm()
  editingUsername.value = null
  if (clearMessages) {
    registerError.value = ''
    registerSuccess.value = ''
  }
}

const demoLoginProfiles = computed(() => [defaultUser, ...defaultDemoUsers])

/**
 * Attempt to log the user in using local/demo users.
 */
function login() {
  loginError.value = ''
  const { username, password } = loginForm.value
  const found = users.value.find((candidate) => candidate.username === username && candidate.password === password && candidate.active !== false)
  if (found) {
    found.lastLogin = new Date().toISOString()
    setAuthenticatedUser(found)
    persistUsers()
    loginForm.value.username = ''
    loginForm.value.password = ''
    isRegistering.value = false
    void loadData(false)
  } else {
    loginError.value = t('Invalid credentials')
  }
}

function loginAsDemo(profile: UserProfile) {
  loginForm.value.username = profile.username
  loginForm.value.password = profile.password
  login()
}

function registerUser(autoLogin = false) {
  registerError.value = ''
  registerSuccess.value = ''
  if (isAuthenticated.value && !canManageUsers.value) {
    registerError.value = t('No permission to manage users')
    return
  }

  const { name, username, email, password, confirmPassword, roleKey } = registerForm.value
  const isEditing = editingUsername.value !== null
  if (!name.trim() || !username.trim() || !email.trim()) {
    registerError.value = t('All fields are required')
    return
  }
  if (!isEditing && (!password || !confirmPassword)) {
    registerError.value = t('Password is required for new users')
    return
  }
  if ((password || confirmPassword) && password !== confirmPassword) {
    registerError.value = t('Passwords do not match')
    return
  }

  const normalizedUsername = username.trim()
  if (!isEditing && users.value.some((candidate) => candidate.username.toLowerCase() === normalizedUsername.toLowerCase())) {
    registerError.value = t('Username already exists')
    return
  }

  if (isEditing) {
    const target = users.value.find((candidate) => candidate.username === editingUsername.value)
    if (!target) {
      resetUserForm()
      return
    }
    if (target.username === defaultUser.username && roleKey !== 'admin') {
      registerError.value = t('Cannot remove the main admin user')
      return
    }
    if (target.roleKey === 'admin' && roleKey !== 'admin' && !wouldKeepAnAdmin(target.username, roleKey, target.active)) {
      registerError.value = t('Cannot remove the last administrator')
      return
    }
    target.name = name.trim()
    target.email = email.trim()
    target.roleKey = roleKey
    target.role = buildUserRole(roleKey)
    target.jobTitle = target.username === defaultUser.username ? defaultUser.jobTitle : defaultJobTitle(roleKey)
    if (password) target.password = password
    if (user.value?.username === target.username) {
      setAuthenticatedUser(target)
    }
    persistUsers()
    registerSuccess.value = t('User updated')
    resetUserForm(false)
    return
  }

  const created: UserProfile = {
    id: normalizedUsername,
    name: name.trim(),
    username: normalizedUsername,
    email: email.trim(),
    role: buildUserRole(roleKey),
    roleKey,
    organization: 'DRIVOLUTION WP3',
    project: 'DriveTrace Core',
    jobTitle: defaultJobTitle(roleKey),
    password,
    active: true,
  }
  users.value.push(created)
  persistUsers()
  registerSuccess.value = t('User saved')
  registerForm.value = emptyUserForm()
  if (autoLogin) {
    setAuthenticatedUser(created)
  } else {
    isRegistering.value = false
  }
}

function removeUser(username: string) {
  registerError.value = ''
  registerSuccess.value = ''
  if (!canManageUsers.value) {
    registerError.value = t('No permission to manage users')
    return
  }
  if (username === defaultUser.username) {
    registerError.value = t('Cannot remove the main admin user')
    return
  }
  const target = users.value.find((candidate) => candidate.username === username)
  if (!target) return
  if (target.roleKey === 'admin' && !wouldKeepAnAdmin(username, target.roleKey, false)) {
    registerError.value = t('Cannot remove the last administrator')
    return
  }
  users.value = users.value.filter((candidate) => candidate.username !== username)
  if (editingUsername.value === username) resetUserForm(false)
  persistUsers()
  registerSuccess.value = t('User removed')
}

function toggleUserActive(profile: UserProfile) {
  registerError.value = ''
  registerSuccess.value = ''
  if (profile.username === defaultUser.username) {
    registerError.value = t('Cannot block the last administrator')
    return
  }
  if (profile.active && profile.roleKey === 'admin' && !wouldKeepAnAdmin(profile.username, profile.roleKey, false)) {
    registerError.value = t('Cannot block the last administrator')
    return
  }
  profile.active = !profile.active
  persistUsers()
}

function startEditUser(profile: UserProfile) {
  registerError.value = ''
  registerSuccess.value = ''
  editingUsername.value = profile.username
  registerForm.value = {
    name: profile.name,
    username: profile.username,
    email: profile.email,
    password: '',
    confirmPassword: '',
    roleKey: profile.roleKey,
  }
}

/**
 * Log out the current user. Clears localStorage and resets the auth state.
 */
function logout() {
  setAuthenticatedUser(null)
  activeView.value = 'overview'
  showUserMenu.value = false
  closeMobileSidebar()
}

// On mount, read any persisted user and locale/theme preferences. If a
// user exists in localStorage, mark as authenticated and restore the user
// profile. This allows refreshing the page without losing the session.
onMounted(() => {
  ensureAdminUser()
  const storedUser = localStorage.getItem('user')
  if (storedUser) {
    try {
      const parsed = JSON.parse(storedUser) as UserProfile
      const current = users.value.find((candidate) => candidate.username === parsed.username && candidate.active !== false)
      if (current) {
        setAuthenticatedUser(current)
      } else {
        setAuthenticatedUser(null)
      }
    } catch {
      setAuthenticatedUser(null)
    }
  }
})

// Include additional view keys for the profile and settings screens. These
// views are not part of the main domain navigation but are accessible via
// the user menu in the top bar.
type ViewKey =
  | 'overview'
  | 'operator'
  | 'customerOrders'
  | 'customerNewOrder'
  | 'customer'
  | 'traceGraph'
  | 'orders'
  | 'units'
  | 'supports'
  | 'materials'
  | 'quality'
  | 'reconditioning'
  | 'simulation'
  | 'racks'
  | 'events'
  | 'fiware'
  | 'analytics'
  | 'profile'
  | 'settings'
  | 'users'
  | 'parameters'

const activeView = ref<ViewKey>('overview')
const traceGraphInitialUnitId = ref<number | null>(null)
const traceGraphInitialOrderId = ref<number | null>(null)
const loading = ref(true)
const apiStatus = ref('Connecting to API...')
const eventStatus = ref('')
const permissionNotice = ref('')

const sidebarMinWidth = 220
const sidebarDefaultWidth = 280
const sidebarMaxWidth = 380
const sidebarWidth = ref(getStoredSidebarWidth())
const sidebarResizing = ref(false)
const appShellStyle = computed(() => ({ '--sidebar-width': `${sidebarWidth.value}px` }))

const summary = ref<DashboardSummary>(demoDashboard)
const products = ref<Product[]>(demoProducts)
const variants = ref<Variant[]>(demoVariants)
const customers = ref<Customer[]>(demoCustomers)
const productionLines = ref<ProductionLine[]>(demoProductionLines)
const productionLineSections = ref<ProductionLineSection[]>(demoProductionLineSections)
const resources = ref<ResourceRecord[]>(demoResources)
const manufacturingProcesses = ref<ManufacturingProcess[]>(demoManufacturingProcesses)
const manufacturingSectionPhases = ref<ManufacturingSectionPhase[]>(demoManufacturingSectionPhases)
const manufacturingProcessPhases = ref<ManufacturingProcessPhase[]>(demoManufacturingProcessPhases)
const checkpoints = ref<Checkpoint[]>(demoCheckpoints)
const orders = ref<ManufacturingOrder[]>(demoOrders)
const units = ref<ProductUnit[]>(demoUnits)
const supports = ref<Support[]>(demoSupports)
const racks = ref<Rack[]>(demoRacks)
const rackSupportAssignments = ref<RackSupportAssignment[]>(demoRackSupportAssignments)
const materials = ref<RawMaterial[]>(demoMaterials)
const lots = ref<LotRawMaterial[]>(demoLots)
const unitMaterialLotUsages = ref<UnitMaterialLotUsage[]>(demoUnitMaterialLotUsages)
const quality = ref<QualityRecord[]>(demoQuality)
const nonconformities = ref<NonconformityRecord[]>(demoNonconformities)
const reworkRecords = ref<ReworkRecord[]>(demoReworkRecords)
const scrapRecords = ref<ScrapRecord[]>(demoScrapRecords)
const supportHistory = ref<SupportLocalizationHistory[]>(demoSupportLocalizationHistory)

type FiwareEntityRecord = {
  id: string
  type: string
  attributes: Record<string, unknown>
}

type FiwareContextSnapshot = {
  timestamp: string
  brokerReachable: boolean
  source: string
  message: string
  entityCount: number
  relationalSnapshotCount: number
  orionLdBaseUrl: string
  entities: FiwareEntityRecord[]
  errors: string[]
}

type FiwarePublishResult = {
  timestamp: string
  brokerReachable: boolean
  message: string
  attemptedCount: number
  publishedCount: number
  failedCount: number
  staleDeletedCount: number
  orionLdBaseUrl: string
  entityIds: string[]
  staleEntityIds: string[]
  errors: string[]
}

type ReferenceRecord = {
  id?: number
  code?: string
  name?: string
  type?: string
  status?: string
  lineId?: number
}

type OperationalEventRecord = {
  id: number
  eventCode: string
  eventType: string
  productUnit?: ReferenceRecord
  support?: ReferenceRecord
  manufacturingOrder?: ReferenceRecord
  fromProductionLine?: ReferenceRecord
  toProductionLine?: ReferenceRecord
  fromSection?: ReferenceRecord
  toSection?: ReferenceRecord
  rack?: ReferenceRecord
  reasonCode?: string
  severity?: string
  source: string
  performedByUserId?: string
  occurredAt: string
  notes?: string
  isDemo: boolean
  label: string
}

type FlowSectionSummary = {
  sectionId: number
  sectionCode: string
  name: string
  sectionType: string
  wipUnits: number
  activeSupports: number
  blockedUnits: number
  isTransferPoint?: boolean
  allowsLineTransferIn?: boolean
  allowsLineTransferOut?: boolean
}

type FlowLineSummary = {
  productionLineId: number
  lineCode: string
  name: string
  wipUnits: number
  activeSupports: number
  blockedUnits: number
  lastMovementAt?: string
  sections: FlowSectionSummary[]
}

type FlowSummary = {
  generatedAt: string
  totals: {
    productionLines: number
    sections: number
    activeUnits: number
    activeSupports: number
    transferPoints: number
    transfers: number
    transfersLast24h: number
    operationalEvents?: number
    operationalEventsLast24h?: number
  }
  routeStates: Array<{ routeState: string; count: number }>
  lineSummaries: FlowLineSummary[]
  recentTransfers: Array<{
    id: number
    unit?: ReferenceRecord
    eventType: string
    reason: string
    occurredAt: string
    fromProductionLine?: ReferenceRecord
    toProductionLine?: ReferenceRecord
    fromSection?: ReferenceRecord
    toSection?: ReferenceRecord
    toSupport?: ReferenceRecord
  }>
  recentOperationalEvents?: OperationalEventRecord[]
}

type OperatorWorkbench = {
  generatedAt: string
  queues: { transferReady: number; blocked: number; rework: number; noSupport: number }
  transferTargets: Array<{ sectionId: number; sectionCode: string; name: string; sectionType: string; productionLine?: ReferenceRecord; currentWip: number }>
  units: Array<{
    id: number
    unitCode: string
    status: string
    qualityStatus: string
    currentSection?: ReferenceRecord
    currentProductionLine?: ReferenceRecord
    currentSupport?: ReferenceRecord
    routeState: string
    canTransfer: boolean
    requiresAttention: boolean
  }>
  recentTransfers: FlowSummary['recentTransfers']
}

type ProductUnitTrace = {
  unit?: {
    id: number
    unitCode: string
    status: string
    qualityStatus: string
    currentSupport?: ReferenceRecord
    currentSection?: ReferenceRecord
    currentProductionLine?: ReferenceRecord
    lastMovementAt?: string
    routeState?: string
  }
  locationHistory?: FlowSummary['recentTransfers']
  operationalEvents?: OperationalEventRecord[]
  timeline?: Array<{ eventType: string; occurredAt: string; source: string; label: string; lineCode?: string; sectionCode?: string; supportCode?: string; result?: string }>
}

type CustomerOrderLookup = {
  publicTrackingCode?: string
  customer?: { customerCode?: string; name?: string }
  order?: { name?: string; status?: string; plannedQty?: number; scheduledUntil?: string; product?: string; variant?: string }
  summary?: { units: number; completed: number; inFlow: number; attention: number; lastMovementAt?: string; progressPercent?: number; progressSummary?: string; lastMilestone?: string }
  units?: Array<{
    status: string
    qualityStatus: string
    customerState?: string
    currentStage?: string
    routeState?: string
    lastMovementAt?: string
  }>
  milestones?: Array<{ eventType: string; occurredAt: string; stage?: string }>
}
type CustomerMilestone = NonNullable<CustomerOrderLookup['milestones']>[number]

function emptyFiwareContext(): FiwareContextSnapshot {
  return {
    timestamp: '',
    brokerReachable: false,
    source: 'relational-fallback',
    message: '',
    entityCount: 0,
    relationalSnapshotCount: 0,
    orionLdBaseUrl: '',
    entities: [],
    errors: [],
  }
}

function emptyFlowSummary(): FlowSummary {
  return {
    generatedAt: '',
    totals: { productionLines: 0, sections: 0, activeUnits: 0, activeSupports: 0, transferPoints: 0, transfers: 0, transfersLast24h: 0, operationalEvents: 0, operationalEventsLast24h: 0 },
    routeStates: [],
    lineSummaries: [],
    recentTransfers: [],
    recentOperationalEvents: [],
  }
}

function emptyOperatorWorkbench(): OperatorWorkbench {
  return {
    generatedAt: '',
    queues: { transferReady: 0, blocked: 0, rework: 0, noSupport: 0 },
    transferTargets: [],
    units: [],
    recentTransfers: [],
  }
}

function emptyDashboardSummary(): DashboardSummary {
  return {
    appName: 'DriveTrace Core',
    subtitle: 'Sem dados internos para este perfil',
    generatedAt: '',
    counts: { openOrders: 0, activeUnits: 0, activeSupports: 0, qualityIssues: 0, rackAssignments: 0 },
    wipBySection: [],
    recentEvents: [],
    qualityAlerts: [],
  }
}

const fiwareContext = ref<FiwareContextSnapshot>(emptyFiwareContext())
const fiwareLoading = ref(false)
const fiwareActionStatus = ref('')
const fiwareLastPublish = ref<FiwarePublishResult | null>(null)
const flowSummary = ref<FlowSummary>(emptyFlowSummary())
const operatorWorkbench = ref<OperatorWorkbench>(emptyOperatorWorkbench())
const operationalEvents = ref<OperationalEventRecord[]>([])
const serverUserContext = ref<AuthUserContext | null>(null)
const selectedUnitTrace = ref<ProductUnitTrace | null>(null)
const traceLoading = ref(false)
const transferStatus = ref('')
const transferForm = ref({
  productUnitId: '',
  toSectionId: '',
  toSupportId: '',
  reason: 'Transferência operacional',
  notes: '',
  moveCurrentSupport: true,
})
const customerLookupCode = ref('TRC-PORTA-001')
const customerLookup = ref<CustomerOrderLookup | null>(null)
const customerLookupStatus = ref('')
const customerOrders = ref<CustomerOrderLookup[]>([])
const customerOrderStatus = ref('')
const customerOrderForm = ref({ name: '', productId: '', variantId: '', quantity: 1, observations: '' })
const customerOrderFilter = ref<'all' | 'active' | 'ready' | 'completed'>('all')
const customerOrderSearch = ref('')
const customerCreatedOrder = ref<CustomerOrderLookup | null>(null)
const customerProductOptions = computed(() => (products.value.length ? products.value : demoProducts))
const customerVariantOptions = computed(() => {
  const source = variants.value.length ? variants.value : demoVariants
  const productId = Number(customerOrderForm.value.productId)
  return productId ? source.filter((variant) => variant.productId === productId) : source
})
const selectedCustomerProduct = computed(() => customerProductOptions.value.find((product) => product.id === Number(customerOrderForm.value.productId)))
const selectedCustomerVariant = computed(() => customerVariantOptions.value.find((variant) => variant.id === Number(customerOrderForm.value.variantId)))
const customerOrderDisplayName = computed(() => customerOrderForm.value.name.trim() || customerOrderFallbackName(selectedCustomerProduct.value?.name, selectedCustomerVariant.value?.name))
const customerOrderSummary = computed(() => {
  const counts = { total: customerOrders.value.length, active: 0, production: 0, ready: 0, completed: 0 }
  for (const order of customerOrders.value) {
    const bucket = customerOrderBucket(order)
    if (bucket !== 'completed' && bucket !== 'ready') counts.active++
    if (bucket === 'production') counts.production++
    if (bucket === 'ready') counts.ready++
    if (bucket === 'completed') counts.completed++
  }
  return counts
})
const filteredCustomerOrders = computed(() => {
  const search = customerOrderSearch.value.trim().toLowerCase()
  return customerOrders.value.filter((order) => {
    const bucket = customerOrderBucket(order)
    const matchesFilter = customerOrderFilter.value === 'all'
      ? true
      : customerOrderFilter.value === 'active'
        ? bucket !== 'completed' && bucket !== 'ready'
        : bucket === customerOrderFilter.value
    if (!matchesFilter) return false
    if (!search) return true
    return [
      customerOrderName(order),
      order.publicTrackingCode,
      order.order?.product,
      order.order?.variant,
      customerOrderState(order),
    ].some((value) => String(value || '').toLowerCase().includes(search))
  })
})
const customerOrderFilters = computed(() => [
  { key: 'all' as const, label: t('Todas'), count: customerOrderSummary.value.total },
  { key: 'active' as const, label: t('Ativas'), count: customerOrderSummary.value.active },
  { key: 'ready' as const, label: t('Prontas'), count: customerOrderSummary.value.ready },
  { key: 'completed' as const, label: t('Concluídas'), count: customerOrderSummary.value.completed },
])

watch(() => customerOrderForm.value.productId, () => {
  if (customerOrderForm.value.variantId && !customerVariantOptions.value.some((variant) => variant.id === Number(customerOrderForm.value.variantId))) {
    customerOrderForm.value.variantId = ''
  }
})

const manualEvent = ref({
  eventType: 'MoveSupport',
  supportCode: 'SUP-005',
  sectionCode: 'SEC-SOLD',
  productUnitCode: 'UP-PORTA-005',
  result: 'PASS',
  notes: '',
})

// The navigation items displayed in the sidebar. Each item has a key to
// control which view is active, a label (in English) that will be
// translated using the `t` function, and a simple icon.
const nav = [
  { key: 'overview', label: 'Dashboard / Line Overview', icon: '⌁' },
  { key: 'operator', label: 'Operator Workbench', icon: 'OP' },
  { key: 'customerOrders', label: 'As minhas encomendas', icon: 'EC' },
  { key: 'customerNewOrder', label: 'Nova encomenda', icon: 'NE' },
  { key: 'customer', label: 'Customer Lookup', icon: 'CU' },
  { key: 'traceGraph', label: 'Traceability Map', icon: 'MAP' },
  { key: 'orders', label: 'Manufacturing Orders', icon: 'MO' },
  { key: 'units', label: 'Product Units', icon: 'PU' },
  { key: 'supports', label: 'Supports / WIP Tracking', icon: 'SUP' },
  { key: 'materials', label: 'Materials and Lots', icon: 'LOT' },
  { key: 'quality', label: 'Quality', icon: 'QC' },
  { key: 'reconditioning', label: 'Recuperação / Recondicionamento', icon: 'RC' },
  { key: 'simulation', label: 'Simulador de produção', icon: 'SIM' },
  { key: 'racks', label: 'Racks / Post-line Logistics', icon: 'RK' },
  { key: 'events', label: 'Event Playback', icon: 'EV' },
  { key: 'fiware', label: 'FIWARE Context Monitor', icon: 'LD' },
  { key: 'analytics', label: 'Grafana Analytics', icon: 'AN' },
  { key: 'parameters', label: 'System Parameters', icon: 'CFG' },
  { key: 'users', label: 'Users', icon: 'USR' },
] as const

const viewPermissions: Partial<Record<ViewKey, string>> = {
  overview: 'ProductUnits.View',
  orders: 'Orders.View',
  units: 'ProductUnits.View',
  supports: 'ProductUnits.View',
  materials: 'Materials.View',
  quality: 'Quality.View',
  reconditioning: 'Reconditioning.Read',
  simulation: 'Simulation.Read',
  racks: 'Racks.View',
  events: 'OperationalEvents.View',
  fiware: 'Fiware.View',
  analytics: 'Grafana.View',
  users: 'Users.Manage',
  parameters: 'MasterData.Manage',
  operator: 'ProductUnits.Transfer',
  customerOrders: 'CustomerPortal.View',
  customerNewOrder: 'CustomerPortal.View',
  customer: 'CustomerPortal.View',
  traceGraph: 'ProductUnits.View',
}

const roleHomeViews: Record<RoleKey, ViewKey> = {
  admin: 'overview',
  supervisor: 'overview',
  operator: 'operator',
  quality: 'reconditioning',
  logistics: 'racks',
  client: 'customerOrders',
  demoViewer: 'overview',
}

const navGroups = [
  { key: 'Cliente', items: ['customerOrders', 'customerNewOrder', 'customer'] },
  { key: 'Operation', items: ['overview', 'operator', 'orders', 'racks', 'simulation'] },
  { key: 'Traceability', items: ['traceGraph', 'units', 'supports', 'materials', 'quality', 'reconditioning', 'customer'] },
  { key: 'Monitoring', items: ['events', 'fiware', 'analytics'] },
  { key: 'Administration', items: ['parameters', 'users'] },
] as const
function navByGroup(groupKey: typeof navGroups[number]['key']) {
  const group = navGroups.find((candidate) => candidate.key === groupKey)
  const groupItems = group?.items as readonly ViewKey[] | undefined
  return groupItems ? nav.filter((item) => groupItems.includes(item.key) && canShowNav(item.key)) : []
}

function canShowNav(key: ViewKey) {
  if (key === 'customerOrders' || key === 'customerNewOrder') return user.value?.roleKey === 'client'
  if (user.value?.roleKey === 'client') return false
  const permission = viewPermissions[key]
  return !permission || can(permission)
}

function firstAllowedDomainView() {
  return nav.find((item) => canShowNav(item.key))?.key ?? 'settings'
}

function homeViewForRole(roleKey?: RoleKey) {
  const preferred = roleHomeViews[roleKey || user.value?.roleKey || 'demoViewer']
  return canShowNav(preferred) ? preferred : firstAllowedDomainView()
}

function ensureAccessibleView() {
  if (activeView.value === 'profile' || activeView.value === 'settings') return
  if (!canShowNav(activeView.value)) activeView.value = homeViewForRole(user.value?.roleKey)
}

function openTraceGraphForUnit(unitId: number) {
  traceGraphInitialUnitId.value = unitId
  traceGraphInitialOrderId.value = null
  activeView.value = 'traceGraph'
  closeMobileSidebar()
}

function openTraceGraphForOrder(orderId: number) {
  traceGraphInitialUnitId.value = null
  traceGraphInitialOrderId.value = orderId
  activeView.value = 'traceGraph'
  closeMobileSidebar()
}

const viewTitles: Record<ViewKey, string> = {
  overview: 'Dashboard / Line Overview',
  operator: 'Operator Workbench',
  customerOrders: 'As minhas encomendas',
  customerNewOrder: 'Nova encomenda',
  customer: 'Customer Lookup',
  traceGraph: 'Traceability Map',
  orders: 'Manufacturing Orders',
  units: 'Product Units',
  supports: 'Supports / WIP Tracking',
  materials: 'Materials and Lots',
  quality: 'Quality',
  reconditioning: 'Recuperação / Recondicionamento',
  simulation: 'Simulador de produção',
  racks: 'Racks / Post-line Logistics',
  events: 'Event Playback',
  fiware: 'FIWARE Context Monitor',
  analytics: 'Grafana Analytics',
  profile: 'Profile',
  settings: 'Settings',
  users: 'Users',
  parameters: 'System Parameters',
}

const activeViewTitle = computed(() => viewTitles[activeView.value])
const viewSubtitles: Record<ViewKey, string> = {
  overview: 'Line overview subtitle',
  operator: 'Operator workbench subtitle',
  customerOrders: 'Customer orders subtitle',
  customerNewOrder: 'New customer order subtitle',
  customer: 'Customer lookup subtitle',
  traceGraph: 'Traceability map subtitle',
  orders: 'Manufacturing orders subtitle',
  units: 'Product units subtitle',
  supports: 'Supports tracking subtitle',
  materials: 'Materials subtitle',
  quality: 'Quality subtitle',
  reconditioning: 'Recuperação produtiva após não conformidade menor ou recuperável',
  simulation: 'Simulador operacional da linha de produção',
  racks: 'Racks subtitle',
  events: 'Events subtitle',
  fiware: 'FIWARE subtitle',
  analytics: 'Analytics subtitle',
  profile: 'Profile subtitle',
  settings: 'Settings subtitle',
  users: 'Users subtitle',
  parameters: 'Parameters subtitle',
}
const activeViewSubtitle = computed(() => viewSubtitles[activeView.value])

function navIcon(key: ViewKey, icon: string) {
  if (key === 'overview') return 'OV'
  return icon.length > 3 ? icon.slice(0, 3).toUpperCase() : icon
}

const sectionsById = computed(() => new Map(productionLineSections.value.map((section) => [section.id, section])))
const supportsById = computed(() => new Map(supports.value.map((support) => [support.id, support])))
const racksById = computed(() => new Map(racks.value.map((rack) => [rack.id, rack])))
const unitsById = computed(() => new Map(units.value.map((unit) => [unit.id, unit])))

const activeUnits = computed(() => units.value.filter((unit) => ['Active', 'Blocked', 'Rework'].includes(unit.status)))
const blockedUnits = computed(() => units.value.filter((unit) => ['Blocked', 'Rework', 'Scrap'].includes(unit.status) || unit.qualityStatus === 'FAIL'))
const loadedSupports = computed(() => supports.value.filter((support) => support.status === 'Loaded'))
const racksAvailable = computed(() => racks.value.filter((rack) => rack.status === 'Available'))
const activeRackAssignments = computed(() => rackSupportAssignments.value.filter((assignment) => !assignment.dateTimeOut))
const recentRackAssignments = computed(() => [...rackSupportAssignments.value].sort((a, b) => new Date(b.dateTimeIn).getTime() - new Date(a.dateTimeIn).getTime()).slice(0, 8))
const occupiedRacks = computed(() => new Set(activeRackAssignments.value.map((assignment) => assignment.rackId)).size)
const postLineSupports = computed(() => supports.value.filter((support) => {
  const section = support.currentSectionId ? sectionsById.value.get(support.currentSectionId) : undefined
  return section?.sectionCode === 'SEC-RACK' || section?.sectionType?.toLowerCase().includes('log')
}))
const openNonconformities = computed(() => nonconformities.value.filter((item) => !['Closed', 'Completed'].includes(item.status)))
const activeReworkRecords = computed(() => reworkRecords.value.filter((item) => !item.endedAt && !['Closed', 'Completed'].includes(item.status)))
const recentQualityRecords = computed(() => [...quality.value].sort((a, b) => new Date(b.recordedAt).getTime() - new Date(a.recordedAt).getTime()).slice(0, 8))
const passQualityCount = computed(() => quality.value.filter((item) => item.result === 'PASS').length)
const failQualityCount = computed(() => quality.value.filter((item) => item.result === 'FAIL').length)
const passRate = computed(() => {
  const total = passQualityCount.value + failQualityCount.value
  return total > 0 ? Math.round((passQualityCount.value / total) * 100) : null
})
const topWipSection = computed(() => {
  const sections = summary.value.wipBySection || []
  return sections.length ? [...sections].sort((a, b) => b.productUnits - a.productUnits)[0] : null
})
const fiwareCoherenceOk = computed(() => fiwareContext.value.entityCount === fiwareContext.value.relationalSnapshotCount && fiwareContext.value.brokerReachable)
const latestDataUpdateShort = computed(() => summary.value.generatedAt ? formatShortTime(summary.value.generatedAt) : '-')
const apiHealthClass = computed(() => {
  const value = apiStatus.value.toLowerCase()
  if (value.includes('offline') || value.includes('error') || value.includes('fail')) return 'api-health-error'
  if (value.includes('connecting') || value.includes('loading') || value.includes('demo')) return 'api-health-warn'
  return ''
})
const apiHealthTitle = computed(() => apiHealthClass.value === '' ? t('API disponível') : t(apiStatus.value || 'API status'))
const currentRoleLabel = computed(() => user.value ? getRoleLabel(user.value.roleKey) : '')
const roleContextCards = computed(() => {
  const roleKey = user.value?.roleKey ?? 'demoViewer'
  if (roleKey === 'operator') {
    return [
      { key: 'area', label: 'Área atribuída', value: serverUserContext.value?.assignedSection?.code || 'SEC-SOLD', detail: serverUserContext.value?.assignedLine?.name || 'Linha operacional', tone: 'tone-info' },
      { key: 'queue', label: 'Fila de execução', value: operatorWorkbench.value.units.length, detail: 'Unidades disponíveis na bancada operacional', tone: 'tone-muted' },
      { key: 'blocked', label: 'Atenção imediata', value: operatorWorkbench.value.queues.blocked, detail: 'Unidades bloqueadas ou em retrabalho', tone: operatorWorkbench.value.queues.blocked ? 'tone-warning' : 'tone-success' },
    ]
  }
  if (roleKey === 'quality') {
    return [
      { key: 'fail', label: 'Resultados reprovados', value: failQualityCount.value, detail: 'Registos reprovados sob análise', tone: failQualityCount.value ? 'tone-warning' : 'tone-success' },
      { key: 'nc', label: 'Não conformidades abertas', value: openNonconformities.value.length, detail: 'Decisão de qualidade pendente', tone: openNonconformities.value.length ? 'tone-warning' : 'tone-success' },
      { key: 'rework', label: 'Retrabalhos ativos', value: activeReworkRecords.value.length, detail: 'Unidades em recuperação controlada', tone: activeReworkRecords.value.length ? 'tone-info' : 'tone-muted' },
    ]
  }
  if (roleKey === 'logistics') {
    return [
      { key: 'racks', label: 'Racks disponíveis', value: racksAvailable.value.length, detail: 'Capacidade pós-linha livre', tone: 'tone-success' },
      { key: 'occupied', label: 'Ocupação', value: `${rackUtilization.value}%`, detail: 'Utilização atual de racks', tone: rackUtilization.value > 75 ? 'tone-warning' : 'tone-info' },
      { key: 'assignments', label: 'Atribuições ativas', value: activeRackAssignments.value.length, detail: 'Suportes ligados a racks', tone: 'tone-muted' },
    ]
  }
  if (roleKey === 'client') {
    return [
      { key: 'customer', label: 'Cliente', value: serverUserContext.value?.customer?.customerCode || 'CLI-AUTO-001', detail: serverUserContext.value?.customer?.name || 'Consulta externa', tone: 'tone-info' },
      { key: 'tracking', label: 'Código público', value: serverUserContext.value?.customer?.defaultPublicTrackingCode || customerLookupCode.value, detail: 'Visível sem dados internos de fábrica', tone: 'tone-success' },
      { key: 'scope', label: 'Âmbito', value: 'Portal', detail: 'Acesso limitado ao progresso da ordem', tone: 'tone-muted' },
    ]
  }
  if (roleKey === 'demoViewer') {
    return [
      { key: 'mode', label: 'Modo', value: 'Leitura', detail: 'Perfil demo sem escrita operacional', tone: 'tone-muted' },
      { key: 'events', label: 'Eventos', value: recentOperationalEvents.value.length, detail: 'Histórico operacional visível', tone: 'tone-info' },
      { key: 'analytics', label: 'Analítica', value: can('Grafana.View') ? 'Ativa' : 'Sem acesso', detail: 'Vista de demonstração', tone: 'tone-success' },
    ]
  }
  return [
    { key: 'role', label: 'Perfil ativo', value: currentRoleLabel.value, detail: serverUserContext.value?.department || 'Operação interna', tone: 'tone-info' },
    { key: 'users', label: 'Utilizadores ativos', value: activeUserCount.value, detail: 'Perfis locais disponíveis para demonstração', tone: 'tone-muted' },
    { key: 'orders', label: 'Ordens abertas', value: summary.value.counts.openOrders, detail: 'Seguimento operacional da produção', tone: 'tone-info' },
  ]
})
const activeUserCount = computed(() => users.value.filter((profile) => profile.active).length)
const administratorCount = computed(() => users.value.filter((profile) => profile.roleKey === 'admin').length)
const operatorCount = computed(() => users.value.filter((profile) => profile.roleKey === 'operator').length)
const rackUtilization = computed(() => racks.value.length ? Math.round((occupiedRacks.value / racks.value.length) * 100) : 0)
const overviewKpis = computed(() => [
  { key: 'orders', label: 'Open orders', value: summary.value.counts.openOrders, detail: 'Orders requiring operational follow-up', tone: 'tone-info' },
  { key: 'active-units', label: 'Active units', value: flowSummary.value.totals.activeUnits || summary.value.counts.activeUnits, detail: 'Traceable units currently in flow', tone: 'tone-info' },
  { key: 'supports', label: 'Active supports', value: summary.value.counts.activeSupports, detail: 'Supports carrying operational WIP', tone: 'tone-muted' },
  { key: 'attention', label: 'Units requiring attention', value: blockedUnits.value.length, detail: 'Blocked, rework or failed quality units.', tone: blockedUnits.value.length ? 'tone-warning' : 'tone-success' },
  { key: 'fiware', label: 'FIWARE coherence', value: fiwareCoherenceOk.value ? t('OK') : t('Review'), detail: fiwareContext.value.source || 'Orion-LD', tone: fiwareCoherenceOk.value ? 'tone-success' : 'tone-warning' },
])
const flowKpis = computed(() => [
  { key: 'lines', label: 'Production lines', value: flowSummary.value.totals.productionLines || productionLines.value.length, detail: 'Lines in the current route model', tone: 'tone-info' },
  { key: 'transfer-points', label: 'Transfer points', value: flowSummary.value.totals.transferPoints, detail: 'Sections allowing controlled line transfer', tone: 'tone-muted' },
  { key: 'transfers', label: 'Transfers today', value: flowSummary.value.totals.transfersLast24h, detail: `${flowSummary.value.totals.transfers} ${t('total product-unit movements')}`, tone: 'tone-info' },
])
const recentOperationalEvents = computed(() => {
  return (flowSummary.value.recentOperationalEvents?.length ? flowSummary.value.recentOperationalEvents : operationalEvents.value).slice(0, 12)
})
const eventKpis = computed(() => [
  { key: 'events-total', label: 'Eventos operacionais', value: flowSummary.value.totals.operationalEvents ?? operationalEvents.value.length, detail: 'Registo centralizado de eventos', tone: 'tone-info' },
  { key: 'events-today', label: 'Eventos hoje', value: flowSummary.value.totals.operationalEventsLast24h ?? 0, detail: 'Eventos registados nas últimas 24 horas', tone: 'tone-muted' },
  { key: 'event-types', label: 'Tipos de evento', value: new Set(operationalEvents.value.map((event) => event.eventType)).size, detail: 'Amostra recente atual', tone: 'tone-success' },
])
const transferTargetSections = computed(() => {
  const targets = productionLineSections.value.filter((section) => section.allowsLineTransferIn || section.isTransferPoint)
  return targets.length ? targets : productionLineSections.value
})
const selectedTransferUnit = computed(() => {
  const id = Number(transferForm.value.productUnitId)
  return Number.isFinite(id) ? unitsById.value.get(id) : undefined
})
const selectedTraceTimeline = computed(() => selectedUnitTrace.value?.timeline ?? [])
const flowLineFilter = ref('all')
const flowTransferFilter = ref<'all' | 'transfers' | 'attention'>('all')
const flowLineFilterOptions = computed(() => [
  { value: 'all', label: t('All lines') },
  ...flowSummary.value.lineSummaries.map((line) => ({ value: String(line.productionLineId), label: `${line.lineCode} · ${line.name}` })),
])
const visibleFlowLineSummaries = computed(() => {
  if (flowLineFilter.value === 'all') return flowSummary.value.lineSummaries
  return flowSummary.value.lineSummaries.filter((line) => String(line.productionLineId) === flowLineFilter.value)
})
const attentionUnitCodes = computed(() => new Set(blockedUnits.value.map((unit) => unit.unitCode)))
const filteredFlowTransfers = computed(() => {
  return flowSummary.value.recentTransfers
    .filter((movement) => flowLineFilter.value === 'all' || [movement.fromProductionLine?.id, movement.toProductionLine?.id].some((id) => String(id) === flowLineFilter.value))
    .filter((movement) => {
      if (flowTransferFilter.value === 'all') return true
      if (flowTransferFilter.value === 'transfers') return isLineTransfer(movement)
      return isAttentionMovement(movement)
    })
    .slice(0, 8)
})
const rackKpis = computed(() => [
  { key: 'available', label: 'Available racks', value: racksAvailable.value.length, detail: 'Racks available for post-line storage', tone: 'tone-success' },
  { key: 'occupied', label: 'Occupied racks', value: occupiedRacks.value, detail: `${rackUtilization.value}% ${t('Rack utilization')}`, tone: occupiedRacks.value ? 'tone-info' : 'tone-muted' },
  { key: 'assignments', label: 'Active rack assignments', value: activeRackAssignments.value.length, detail: 'Temporal rack-support logistics links', tone: 'tone-info' },
  { key: 'post-line', label: 'Supports in post-line', value: postLineSupports.value.length, detail: 'Supports currently in rack logistics area', tone: 'tone-muted' },
])
const fiwareStatusCards = computed(() => [
  { key: 'connection', label: 'Connection status', value: fiwareConnectionText(), detail: fiwareContext.value.orionLdBaseUrl || 'Orion-LD', tone: fiwareContext.value.brokerReachable ? 'tone-success' : 'tone-warning' },
  { key: 'source', label: 'Source', value: fiwareSourceText(), detail: 'Current context provider', tone: 'tone-info' },
  { key: 'entities', label: 'Entities', value: fiwareContext.value.entityCount, detail: `${fiwareContext.value.relationalSnapshotCount} ${t('relational records expected')}`, tone: fiwareCoherenceOk.value ? 'tone-success' : 'tone-warning' },
  { key: 'updated', label: 'Last update', value: fiwareContext.value.timestamp ? formatDate(fiwareContext.value.timestamp) : '-', detail: fiwareCoherenceOk.value ? t('Context coherence confirmed') : t('Context review recommended'), tone: fiwareCoherenceOk.value ? 'tone-success' : 'tone-warning' },
])
const userStatusCards = computed(() => [
  { key: 'total', label: 'Existing users', value: users.value.length, detail: 'Local dashboard users', tone: 'tone-info' },
  { key: 'active', label: 'Active profiles', value: activeUserCount.value, detail: 'Profiles allowed to access the dashboard', tone: 'tone-success' },
  { key: 'admins', label: 'Administrators', value: administratorCount.value, detail: 'Profiles with management permissions', tone: 'tone-warning' },
  { key: 'operators', label: 'Operators', value: operatorCount.value, detail: 'Operational profiles', tone: 'tone-muted' },
])
const sidebarProfileName = computed(() => (user.value ? getRoleLabel(user.value.roleKey) : ''))
const analyticsOperationalData = computed(() => ({
  summary: summary.value,
  flowSummary: flowSummary.value,
  operatorWorkbench: operatorWorkbench.value,
  orders: orders.value,
  units: units.value,
  supports: supports.value,
  racks: racks.value,
  rackSupportAssignments: rackSupportAssignments.value,
  quality: quality.value,
  nonconformities: nonconformities.value,
  reworkRecords: reworkRecords.value,
  supportHistory: supportHistory.value,
}))

function statusClass(status: string | undefined) {
  const value = (status || '').toLowerCase()
  if (value.includes('fail') || value.includes('blocked') || value.includes('scrap') || value.includes('rejected')) return 'badge-red'
  if (value.includes('rework') || value.includes('pending') || value.includes('recover') || value.includes('recondition') || value.includes('quality')) return 'badge-amber'
  if (value.includes('transfer') || value.includes('move') || value.includes('active') || value.includes('loaded') || value.includes('progress')) return 'badge-blue'
  if (value.includes('pass') || value.includes('completed') || value.includes('stored') || value.includes('available') || value.includes('ready')) return 'badge-green'
  return 'badge-gray'
}

// Explicitly set the current theme. This helper wraps the reactive theme ref
// from prefs.ts so that assignments work as expected within templates.
function setTheme(newTheme: string) {
  if (newTheme === 'light' || newTheme === 'dark') {
    theme.value = newTheme
  }
}

// Reactive state controlling the visibility of the user dropdown menu in the top
// bar. Toggled when the avatar/button is clicked. The menu is hidden when
// navigating to another view or when logging out.
const showUserMenu = ref(false)
const mobileSidebarOpen = ref(false)
let desktopMediaQuery: MediaQueryList | null = null

function clampSidebarWidth(value: number) {
  return Math.min(sidebarMaxWidth, Math.max(sidebarMinWidth, Math.round(value)))
}

function getStoredSidebarWidth() {
  const stored = Number(localStorage.getItem('drivetrace.sidebar.width'))
  return Number.isFinite(stored) ? clampSidebarWidth(stored) : sidebarDefaultWidth
}

function persistSidebarWidth(value: number) {
  localStorage.setItem('drivetrace.sidebar.width', String(clampSidebarWidth(value)))
}

function startSidebarResize(event: PointerEvent) {
  if (window.innerWidth < 1024) return
  event.preventDefault()
  sidebarResizing.value = true
  document.body.classList.add('is-sidebar-resizing')
  window.addEventListener('pointermove', resizeSidebar)
  window.addEventListener('pointerup', stopSidebarResize)
  window.addEventListener('pointercancel', stopSidebarResize)
}

function resizeSidebar(event: PointerEvent) {
  if (!sidebarResizing.value) return
  sidebarWidth.value = clampSidebarWidth(event.clientX)
}

function stopSidebarResize() {
  if (!sidebarResizing.value) return
  sidebarResizing.value = false
  persistSidebarWidth(sidebarWidth.value)
  document.body.classList.remove('is-sidebar-resizing')
  window.removeEventListener('pointermove', resizeSidebar)
  window.removeEventListener('pointerup', stopSidebarResize)
  window.removeEventListener('pointercancel', stopSidebarResize)
}

function resetSidebarWidth() {
  sidebarWidth.value = sidebarDefaultWidth
  persistSidebarWidth(sidebarWidth.value)
}

function closeMobileSidebar() {
  mobileSidebarOpen.value = false
}

function toggleMobileSidebar() {
  mobileSidebarOpen.value = !mobileSidebarOpen.value
}

function navigateTo(view: ViewKey) {
  if (view !== 'profile' && view !== 'settings' && !canShowNav(view)) {
    permissionNotice.value = 'Não tem permissão para aceder a esta vista.'
    activeView.value = homeViewForRole(user.value?.roleKey)
    showUserMenu.value = false
    closeMobileSidebar()
    return
  }
  permissionNotice.value = ''
  activeView.value = view
  showUserMenu.value = false
  closeMobileSidebar()
}

watch(activeView, () => {
  showUserMenu.value = false
})

function sectionName(sectionId?: number) {
  return sectionId ? translateSectionName(sectionsById.value.get(sectionId)?.name || `Section ${sectionId}`) : t('Not assigned')
}

function supportCode(supportId?: number) {
  return supportId ? supportsById.value.get(supportId)?.supportCode || `SUP ${supportId}` : t('No support')
}

function rackCode(rackId?: number) {
  return rackId ? racksById.value.get(rackId)?.rackCode || `RACK ${rackId}` : '-'
}

function unitCode(unitId?: number) {
  return unitId ? unitsById.value.get(unitId)?.unitCode || `Unit ${unitId}` : t('Unknown unit')
}

function materialName(materialId: number) {
  return translateMaterialName(materials.value.find((material) => material.id === materialId)?.name || `Material ${materialId}`)
}

function formatDate(value?: string) {
  if (!value) return '-'
  return new Date(value).toLocaleString(locale.value)
}

function formatShortTime(value?: string) {
  if (!value) return '-'
  return new Date(value).toLocaleTimeString(locale.value, { hour: '2-digit', minute: '2-digit' })
}

function normalizeToken(value: string) {
  if (!value) return value
  const hashIndex = value.lastIndexOf('#')
  if (hashIndex >= 0 && hashIndex + 1 < value.length) return value.slice(hashIndex + 1)
  const slashIndex = value.lastIndexOf('/')
  if (slashIndex >= 0 && slashIndex + 1 < value.length) return value.slice(slashIndex + 1)
  return value
}

function normalizeLegacyEntity(raw: Record<string, unknown>): FiwareEntityRecord {
  const id = typeof raw.id === 'string' ? raw.id : ''
  const type = typeof raw.type === 'string' ? normalizeToken(raw.type) : t('Unknown type')
  const attributes: Record<string, unknown> = {}

  for (const [key, value] of Object.entries(raw)) {
    if (key === 'id' || key === 'type' || key === '@context' || key === 'atContext') continue
    if (value && typeof value === 'object' && !Array.isArray(value)) {
      const nested = value as Record<string, unknown>
      if ('value' in nested) {
        attributes[normalizeToken(key)] = nested.value
        continue
      }
      if ('object' in nested) {
        attributes[normalizeToken(key)] = nested.object
        continue
      }
    }
    attributes[normalizeToken(key)] = value
  }

  return { id, type, attributes }
}

function normalizeFiwareContextPayload(payload: unknown): FiwareContextSnapshot {
  const fallback = emptyFiwareContext()
  if (!payload) return fallback

  if (Array.isArray(payload)) {
    return {
      ...fallback,
      source: 'relational-fallback',
      message: t('Legacy FIWARE response detected.'),
      entityCount: payload.length,
      relationalSnapshotCount: payload.length,
      entities: payload
        .filter((item): item is Record<string, unknown> => typeof item === 'object' && item !== null)
        .map((item) => normalizeLegacyEntity(item)),
    }
  }

  if (typeof payload !== 'object') return fallback

  const input = payload as Record<string, unknown>
  const entitiesRaw = Array.isArray(input.entities) ? input.entities : []
  const entities = entitiesRaw
    .filter((item): item is Record<string, unknown> => typeof item === 'object' && item !== null)
    .map((item) => {
      const id = typeof item.id === 'string' ? item.id : ''
      const type = typeof item.type === 'string' ? normalizeToken(item.type) : t('Unknown type')
      const attributesRaw = item.attributes && typeof item.attributes === 'object' && !Array.isArray(item.attributes)
        ? (item.attributes as Record<string, unknown>)
        : {}
      const attributes = Object.fromEntries(Object.entries(attributesRaw).map(([key, value]) => [normalizeToken(key), value]))
      return { id, type, attributes }
    })

  return {
    timestamp: typeof input.timestamp === 'string' ? input.timestamp : '',
    brokerReachable: input.brokerReachable === true,
    source: typeof input.source === 'string' ? input.source : fallback.source,
    message: typeof input.message === 'string' ? input.message : '',
    entityCount: typeof input.entityCount === 'number' ? input.entityCount : entities.length,
    relationalSnapshotCount: typeof input.relationalSnapshotCount === 'number' ? input.relationalSnapshotCount : entities.length,
    orionLdBaseUrl: typeof input.orionLdBaseUrl === 'string' ? input.orionLdBaseUrl : '',
    entities,
    errors: Array.isArray(input.errors) ? input.errors.filter((item): item is string => typeof item === 'string') : [],
  }
}

function fiwareAttributesPreview(attributes: Record<string, unknown>) {
  const entries = Object.entries(attributes)
  if (!entries.length) return t('No attributes')
  return entries
    .slice(0, 3)
    .map(([key, value]) => `${key}: ${String(value)}`)
    .join(' | ')
}

function fiwareEntityTypeLabel(type: string) {
  const labels: Record<string, string> = {
    Support: 'Support',
    ProductUnit: 'Product unit',
    Rack: 'Rack',
    ProductionLine: 'Production line',
    ProductionLineSection: 'Production line section',
    Checkpoint: 'Checkpoint',
  }
  return t(labels[type] || type)
}

function fiwareConnectionText() {
  return fiwareContext.value.brokerReachable ? t('Connected') : t('Unavailable')
}

function fiwareSourceText() {
  if (fiwareContext.value.source === 'orion-ld') return t('Orion-LD')
  if (fiwareContext.value.source === 'relational-fallback') return t('Relational fallback')
  return fiwareContext.value.source || '-'
}

function fiwareSummaryMessage() {
  if (fiwareContext.value.source === 'orion-ld') {
    return fiwareContext.value.entityCount > 0
      ? t('FIWARE context loaded from Orion-LD.')
      : t('Orion-LD reachable but no entities published.')
  }
  return t('Using relational fallback because Orion-LD is unavailable.')
}

type EntityRecord = Record<string, unknown> & { id?: number }
type CrudMessage = { type: 'success' | 'error'; text: string }
type CrudOption = { value: string | number; label: string }
type CrudField = {
  key: string
  label: string
  type?: 'text' | 'number' | 'select' | 'textarea' | 'datetime'
  required?: boolean
  min?: number
  nullable?: boolean
  disabledOnEdit?: boolean
  options?: () => CrudOption[]
}
type CrudColumn = {
  key: string
  label: string
  badge?: boolean
  format?: (value: unknown, item: EntityRecord) => string
}
type CrudConfig = {
  key: string
  title: string
  description?: string
  path: string
  writePermission?: string
  itemsRef: Ref<EntityRecord[]>
  fields: CrudField[]
  columns: CrudColumn[]
  newItem: () => EntityRecord
  maxVisibleRows?: number
  maxTableHeight?: string
  confirmDelete?: boolean
  allowDelete?: boolean
  validate?: (form: EntityRecord, editingId?: number) => string
  canDelete?: (item: EntityRecord) => true | string
}

const crudForms = ref<Record<string, EntityRecord>>({})
const crudOpen = ref<Record<string, boolean>>({})
const crudEditingId = ref<Record<string, number | null>>({})
const crudMessages = ref<Record<string, CrudMessage | undefined>>({})
const parameterTab = ref('products')

function nowInput() {
  return toDateTimeInput(new Date().toISOString())
}

function toDateTimeInput(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000)
  return local.toISOString().slice(0, 16)
}

function fromDateTimeInput(value: unknown, nullable = false) {
  if (!value) return nullable ? null : new Date().toISOString()
  const date = new Date(String(value))
  return Number.isNaN(date.getTime()) ? (nullable ? null : new Date().toISOString()) : date.toISOString()
}

function formatOptionalDate(value: unknown) {
  return typeof value === 'string' ? formatDate(value) : '-'
}

function entityOptions<T extends { id: number }>(items: T[], label: (item: T) => string, includeEmpty = true): CrudOption[] {
  const base = includeEmpty ? [{ value: '', label: t('Select option') }] : []
  return [...base, ...items.map((item) => ({ value: item.id, label: label(item) }))]
}

const productOptions = () => entityOptions(products.value, (item) => translateMaterialName(item.name), false)
const variantOptions = () => entityOptions(variants.value, (item) => `${item.variantCode} · ${translateMaterialName(item.name)}`)
const customerOptions = () => entityOptions(customers.value, (item) => `${item.customerCode} · ${item.name}`)
const orderOptions = () => entityOptions(orders.value, (item) => item.orderNumber, false)
const processOptions = () => entityOptions(manufacturingProcesses.value, (item) => item.processName, false)
const productionLineOptions = () => entityOptions(productionLines.value, (item) => `${item.lineCode} · ${item.name}`, false)
const optionalProductionLineOptions = () => entityOptions(productionLines.value, (item) => `${item.lineCode} · ${item.name}`)
const sectionOptions = () => entityOptions(productionLineSections.value, (item) => `${item.sectionCode} · ${translateSectionName(item.name)}`)
const requiredSectionOptions = () => entityOptions(productionLineSections.value, (item) => `${item.sectionCode} · ${translateSectionName(item.name)}`, false)
const supportOptions = () => entityOptions(supports.value, (item) => item.supportCode)
const requiredSupportOptions = () => entityOptions(supports.value, (item) => item.supportCode, false)
const unitOptions = () => entityOptions(units.value, (item) => item.unitCode, false)
const optionalUnitOptions = () => entityOptions(units.value, (item) => item.unitCode)
const materialOptions = () => entityOptions(materials.value, (item) => translateMaterialName(item.name), false)
const lotOptions = () => entityOptions(lots.value, (item) => item.lotNumber, false)
const qualityResultOptions = () => entityOptions(quality.value, (item) => `${unitCode(item.productUnitId)} · ${translateQualityResult(item.result)}`)
const nonconformityOptions = () => entityOptions(nonconformities.value, (item) => `${unitCode(item.productUnitId)} · ${displayStatus(item.status)}`)
const checkpointOptions = () => entityOptions(checkpoints.value, (item) => `${item.checkpointCode} · ${item.name}`)
const rackOptions = () => entityOptions(racks.value, (item) => item.rackCode, false)
const phaseOptions = () => entityOptions(manufacturingSectionPhases.value, (item) => item.phaseInfo, false)
const resourceOptions = () => entityOptions(resources.value, (item) => item.name)

function simpleOptions(values: string[], includeEmpty = false) {
  const base = includeEmpty ? [{ value: '', label: t('Select option') }] : []
  return [...base, ...values.map((value) => ({ value, label: translateStatus(value) }))]
}

function getById<T extends { id: number }>(items: T[], id: unknown) {
  const numericId = Number(id)
  return items.find((item) => item.id === numericId)
}

function productLabel(id: unknown) {
  return translateMaterialName(getById(products.value, id)?.name || '-')
}

function variantLabel(id: unknown) {
  const variant = getById(variants.value, id)
  return variant ? `${variant.variantCode} · ${translateMaterialName(variant.name)}` : '-'
}

function orderLabel(id: unknown) {
  return getById(orders.value, id)?.orderNumber || '-'
}

function processLabel(id: unknown) {
  return getById(manufacturingProcesses.value, id)?.processName || '-'
}

function lineLabel(id: unknown) {
  const line = getById(productionLines.value, id)
  return line ? `${line.lineCode} · ${line.name}` : '-'
}

function customerLabel(id: unknown) {
  const customer = getById(customers.value, id)
  return customer ? `${customer.customerCode} · ${customer.name}` : '-'
}

function referenceLabel(value?: ReferenceRecord | null) {
  if (!value) return '-'
  if (value.code && value.name) return `${value.code} · ${value.name}`
  return value.name || value.code || '-'
}

const operationalEventDisplayLabels: Record<string, string> = {
  Assign: 'Atribuição',
  ASSIGN: 'Atribuição',
  Blocked: 'Bloqueado',
  Fail: 'Reprovado',
  FAIL: 'Reprovado',
  FiwarePublished: 'Contexto FIWARE publicado',
  LineTransfer: 'Transferência entre linhas',
  MoveSupport: 'Movimento de suporte',
  Movement: 'Movimento',
  PASS: 'Aprovado',
  ProductUnitMarkedReconditioned: 'Unidade marcada como recondicionada',
  QualityRecorded: 'Qualidade registada',
  RackAssigned: 'Rack atribuída',
  ReconditioningCompleted: 'Recondicionamento concluído',
  ReconditioningRejected: 'Recondicionamento rejeitado',
  ReworkCompleted: 'Retrabalho concluído',
  ScrapRecorded: 'Sucata registada',
  SeededCurrentLocation: 'Localização atual demonstrativa',
  SupportAssigned: 'Suporte atribuído',
  Transfer: 'Transferência',
  TRANSFER: 'Transferência',
  TransferToRack: 'Transferência para rack',
}

const demoTextDisplayLabels: Record<string, string> = {
  'Alignment outside nominal tolerance; quality manager decision required.': 'Alinhamento fora da tolerância nominal; decisão do responsável de qualidade necessária.',
}

function displayStatus(value?: string | null) {
  if (!value) return '-'
  return operationalEventDisplayLabels[value] ?? translateStatus(value)
}

function displayOperationalEvent(value?: string | null) {
  if (!value) return '-'
  return operationalEventDisplayLabels[value] ?? translateStatus(value)
}

function displayDemoText(value?: string | null) {
  if (!value) return '-'
  return demoTextDisplayLabels[value] ?? t(value)
}

function customerOrderBucket(order: CustomerOrderLookup): 'received' | 'planning' | 'production' | 'validation' | 'ready' | 'completed' {
  const status = `${order.order?.status || ''} ${order.summary?.progressSummary || ''} ${order.summary?.lastMilestone || ''}`.toLowerCase()
  const progress = order.summary?.progressPercent ?? 0
  if (status.includes('conclu') || status.includes('completed') || progress >= 100) return 'completed'
  if (status.includes('pronta') || status.includes('entrega') || status.includes('expedi')) return 'ready'
  if (status.includes('valida') || status.includes('qualidade') || order.summary?.attention) return 'validation'
  if (status.includes('pedido recebido') || status.includes('request received')) return 'received'
  if (status.includes('produção') || status.includes('producao') || status.includes('in progress') || progress > 0) return 'production'
  if (status.includes('plane') || status.includes('planned')) return 'planning'
  return 'received'
}

function customerOrderState(order: CustomerOrderLookup) {
  const bucket = customerOrderBucket(order)
  return {
    received: t('Pedido recebido'),
    planning: t('Planeada'),
    production: t('Em produção'),
    validation: t('Em controlo de qualidade'),
    ready: t('Pronta'),
    completed: t('Concluída'),
  }[bucket]
}

function customerOrderFallbackName(product?: string | null, variant?: string | null, trackingCode?: string | null) {
  if (product) return variant ? `${translateMaterialName(product)} · ${variant}` : translateMaterialName(product)
  return trackingCode ? `${t('Encomenda')} ${trackingCode}` : t('Encomenda sem nome')
}

function customerOrderName(order: CustomerOrderLookup) {
  return order.order?.name?.trim() || customerOrderFallbackName(order.order?.product, order.order?.variant, order.publicTrackingCode)
}

function customerOrderTone(order: CustomerOrderLookup) {
  const bucket = customerOrderBucket(order)
  if (bucket === 'completed' || bucket === 'ready') return 'tone-success'
  if (bucket === 'validation') return 'tone-warning'
  if (bucket === 'production') return 'tone-info'
  return 'tone-muted'
}

function customerNextStep(order: CustomerOrderLookup) {
  const bucket = customerOrderBucket(order)
  return {
    received: t('Próximo passo: planeamento'),
    planning: t('Próximo passo: produção'),
    production: t('Próximo passo: controlo de qualidade'),
    validation: t('Próximo passo: preparação para entrega'),
    ready: t('Próximo passo: levantamento ou expedição'),
    completed: t('Encomenda concluída'),
  }[bucket]
}

function customerProgressPercent(order?: CustomerOrderLookup | null) {
  if (!order) return 0
  const explicit = order.summary?.progressPercent
  if (typeof explicit === 'number' && Number.isFinite(explicit)) return Math.min(100, Math.max(0, explicit))
  return {
    received: 8,
    planning: 20,
    production: 55,
    validation: 75,
    ready: 92,
    completed: 100,
  }[customerOrderBucket(order)]
}

function customerProgressSteps(order?: CustomerOrderLookup | null) {
  const steps = [
    { key: 'received', label: t('Pedido recebido') },
    { key: 'planning', label: t('Planeamento') },
    { key: 'production', label: t('Produção') },
    { key: 'validation', label: t('Controlo de qualidade') },
    { key: 'ready', label: t('Pronta') },
    { key: 'completed', label: t('Concluída') },
  ]
  if (!order) return steps.map((step) => ({ ...step, state: 'pending' }))
  const bucket = customerOrderBucket(order)
  const currentIndex = steps.findIndex((step) => step.key === bucket)
  return steps.map((step, index) => ({
    ...step,
    state: bucket === 'completed' || index < currentIndex ? 'done' : index === currentIndex ? 'current' : 'pending',
  }))
}

function customerHistoryLabel(milestone: CustomerMilestone) {
  const text = `${milestone.eventType || ''} ${milestone.stage || ''}`.toLowerCase()
  if (text.includes('conclu') || text.includes('completed')) return t('Encomenda concluída')
  if (text.includes('pronta') || text.includes('ready')) return t('Encomenda pronta')
  if (text.includes('qualidade') || text.includes('valida') || text.includes('quality')) return t('Validação de qualidade atualizada')
  if (text.includes('rack') || text.includes('expedi') || text.includes('entrega') || text.includes('preparação final')) return t('Preparação final atualizada')
  if (text.includes('produção') || text.includes('producao') || text.includes('acabamento') || text.includes('pint') || text.includes('corte') || text.includes('sold') || text.includes('mont')) return t('Produção atualizada')
  if (text.includes('pedido recebido')) return t('Pedido recebido')
  return t('Produção atualizada')
}

function customerLatestUpdate(order?: CustomerOrderLookup | null) {
  if (!order) return '-'
  return {
    received: t('Pedido recebido'),
    planning: t('Pedido recebido'),
    production: t('Produção atualizada'),
    validation: t('Validação de qualidade atualizada'),
    ready: t('Encomenda pronta para levantamento/expedição'),
    completed: t('Encomenda concluída'),
  }[customerOrderBucket(order)]
}

function movementText(value?: ReferenceRecord | null) {
  if (!value) return '-'
  return value.code || value.name || '-'
}

function isLineTransfer(movement: FlowSummary['recentTransfers'][number]) {
  const fromLine = movement.fromProductionLine?.id || movement.fromProductionLine?.code
  const toLine = movement.toProductionLine?.id || movement.toProductionLine?.code
  const eventText = `${movement.eventType || ''} ${movement.reason || ''}`.toLowerCase()
  return Boolean(fromLine && toLine && String(fromLine) !== String(toLine)) || eventText.includes('transfer')
}

function isAttentionMovement(movement: FlowSummary['recentTransfers'][number]) {
  const unitCode = movement.unit?.code || ''
  const eventText = `${movement.eventType || ''} ${movement.reason || ''}`.toLowerCase()
  return attentionUnitCodes.value.has(unitCode) || ['quality', 'rework', 'blocked', 'fail'].some((token) => eventText.includes(token))
}

function movementBadge(movement: FlowSummary['recentTransfers'][number]) {
  const eventText = `${movement.eventType || ''} ${movement.reason || ''}`.toLowerCase()
  if (eventText.includes('rework')) return t('Rework')
  if (eventText.includes('quality') || eventText.includes('fail')) return t('Quality')
  if (eventText.includes('paint') || eventText.includes('pintura')) return t('Painting')
  if (isLineTransfer(movement)) return t('Transfer')
  return displayOperationalEvent(movement.eventType)
}

function movementTitle(movement: FlowSummary['recentTransfers'][number]) {
  const from = `${referenceLabel(movement.fromProductionLine)} / ${referenceLabel(movement.fromSection)}`
  const to = `${referenceLabel(movement.toProductionLine)} / ${referenceLabel(movement.toSection)}`
  return `${movement.unit?.code || '-'}: ${from} -> ${to}`
}

function operationalEventTarget(event: OperationalEventRecord) {
  return event.productUnit?.code || event.support?.code || event.rack?.code || event.manufacturingOrder?.code || '-'
}

function operationalEventLocation(event: OperationalEventRecord) {
  return referenceLabel(event.toSection) !== '-' ? referenceLabel(event.toSection) : referenceLabel(event.toProductionLine)
}

function operationalEventDetail(event: OperationalEventRecord) {
  return displayDemoText(event.notes || event.label || displayOperationalEvent(event.eventType))
}

function checkpointLabel(id: unknown) {
  const checkpoint = getById(checkpoints.value, id)
  return checkpoint ? `${checkpoint.checkpointCode} · ${checkpoint.name}` : '-'
}

function lotLabel(id: unknown) {
  return getById(lots.value, id)?.lotNumber || '-'
}

function rackLabel(id: unknown) {
  return getById(racks.value, id)?.rackCode || '-'
}

function numericValue(value: unknown, nullable = false) {
  if (value === '' || value === null || value === undefined) return nullable ? null : 0
  return Number(value)
}

function validateRequiredFields(config: CrudConfig, form: EntityRecord) {
  for (const field of config.fields) {
    if (!field.required) continue
    const value = form[field.key]
    if (value === '' || value === null || value === undefined) return t('Required field') + `: ${t(field.label)}`
  }
  for (const field of config.fields.filter((item) => item.type === 'number' && item.min !== undefined)) {
    const value = Number(form[field.key])
    if (Number.isNaN(value) || value < Number(field.min)) return t('Invalid numeric value') + `: ${t(field.label)}`
  }
  return ''
}

function prepareFormValue(field: CrudField, value: unknown) {
  if (field.type === 'datetime') return toDateTimeInput(typeof value === 'string' ? value : undefined)
  if (value === null || value === undefined) return ''
  return value
}

function canWriteCrud(config: CrudConfig) {
  return !config.writePermission || can(config.writePermission)
}

function startCrudAdd(config: CrudConfig) {
  if (!canWriteCrud(config)) {
    crudMessages.value[config.key] = { type: 'error', text: 'Não tem permissão para criar ou editar nesta vista.' }
    return
  }
  crudForms.value[config.key] = { ...config.newItem() }
  crudEditingId.value[config.key] = null
  crudOpen.value[config.key] = true
  crudMessages.value[config.key] = undefined
}

function startCrudEdit(config: CrudConfig, item: EntityRecord) {
  if (!canWriteCrud(config)) {
    crudMessages.value[config.key] = { type: 'error', text: 'Não tem permissão para criar ou editar nesta vista.' }
    return
  }
  const form: EntityRecord = {}
  for (const field of config.fields) {
    form[field.key] = prepareFormValue(field, item[field.key])
  }
  crudForms.value[config.key] = form
  crudEditingId.value[config.key] = Number(item.id)
  crudOpen.value[config.key] = true
  crudMessages.value[config.key] = undefined
}

function cancelCrud(config: CrudConfig) {
  crudOpen.value[config.key] = false
  crudEditingId.value[config.key] = null
}

function updateCrudField(config: CrudConfig, key: string, value: string) {
  crudForms.value[config.key] = { ...(crudForms.value[config.key] || {}), [key]: value }
}

function payloadFor(config: CrudConfig) {
  const editingId = crudEditingId.value[config.key]
  const existing = editingId ? config.itemsRef.value.find((item) => Number(item.id) === editingId) || {} : {}
  const payload: EntityRecord = { ...existing, ...(crudForms.value[config.key] || {}) }
  for (const field of config.fields) {
    if (field.type === 'number' || field.key.endsWith('Id') || field.key === 'plannedQty' || field.key === 'quantity' || field.key === 'lotQuantity' || field.key === 'phaseDuration' || field.key === 'numberStepOrder') {
      payload[field.key] = numericValue(payload[field.key], field.nullable)
    }
    if (field.type === 'datetime') {
      payload[field.key] = fromDateTimeInput(payload[field.key], field.nullable)
    }
  }
  if (editingId) payload.id = editingId
  return payload
}

async function saveCrud(config: CrudConfig) {
  if (!canWriteCrud(config)) {
    crudMessages.value[config.key] = { type: 'error', text: 'Não tem permissão para criar ou editar nesta vista.' }
    return
  }
  const form = crudForms.value[config.key] || {}
  const editingId = crudEditingId.value[config.key] || undefined
  const requiredError = validateRequiredFields(config, form)
  if (requiredError) {
    crudMessages.value[config.key] = { type: 'error', text: requiredError }
    return
  }
  const customError = config.validate?.(form, editingId)
  if (customError) {
    crudMessages.value[config.key] = { type: 'error', text: customError }
    return
  }

  try {
    const payload = payloadFor(config)
    if (editingId) await updateEntity(config.path, editingId, payload)
    else await createEntity(config.path, payload)
    crudMessages.value[config.key] = { type: 'success', text: 'Operation completed' }
    cancelCrud(config)
    await loadData(false)
  } catch (error) {
    crudMessages.value[config.key] = { type: 'error', text: `${t('Error while saving')}: ${getApiErrorMessage(error)}` }
  }
}

async function removeCrud(config: CrudConfig, item: EntityRecord) {
  if (!canWriteCrud(config)) {
    crudMessages.value[config.key] = { type: 'error', text: 'Não tem permissão para eliminar nesta vista.' }
    return
  }
  const blocked = config.canDelete?.(item)
  if (blocked && blocked !== true) {
    crudMessages.value[config.key] = { type: 'error', text: blocked }
    return
  }
  if ((config.confirmDelete ?? true) && !window.confirm(`${t('Confirm deletion')} ${String(item.id ?? '')}?`)) return
  try {
    await deleteEntity(config.path, Number(item.id))
    crudMessages.value[config.key] = { type: 'success', text: 'Operation completed' }
    await loadData(false)
  } catch (error) {
    crudMessages.value[config.key] = { type: 'error', text: `${t('Error while deleting')}: ${getApiErrorMessage(error)}` }
  }
}

function plannedQtyValidation(form: EntityRecord) {
  return Number(form.plannedQty) > 0 ? '' : 'Planned quantity must be greater than zero'
}

function positiveQuantityValidation(form: EntityRecord) {
  return Number(form.quantity) > 0 ? '' : 'Quantity must be greater than zero'
}

function lotQuantityValidation(form: EntityRecord) {
  return Number(form.lotQuantity) >= 0 ? '' : 'Lot quantity must be greater than or equal to zero'
}

function qualityResultValidation(form: EntityRecord) {
  return ['PASS', 'FAIL'].includes(String(form.result)) ? '' : 'O resultado deve ser Aprovado ou Reprovado'
}

function supportDeleteRule(item: EntityRecord) {
  const supportId = Number(item.id)
  const hasActiveUnit = units.value.some((unit) => unit.currentSupportId === supportId && !['Completed', 'Scrap'].includes(String(unit.status)))
  return hasActiveUnit ? 'Support has active product units' : true
}

function crudPanelConfig(config: CrudConfig) {
  return {
    ...config,
    maxVisibleRows: config.maxVisibleRows ?? 7,
    maxTableHeight: config.maxTableHeight ?? 'clamp(320px, 48vh, 560px)',
    readOnly: config.writePermission ? !can(config.writePermission) : false,
    items: () => config.itemsRef.value,
  }
}

function asCrudRef<T extends EntityRecord>(value: Ref<T[]>) {
  return value as unknown as Ref<EntityRecord[]>
}

const ordersCrud: CrudConfig = {
  key: 'orders',
  title: 'Manufacturing Orders',
  path: '/manufacturing-orders',
  writePermission: 'Orders.Manage',
  itemsRef: asCrudRef(orders),
  fields: [
    { key: 'orderNumber', label: 'Order number', required: true },
    { key: 'productId', label: 'Product', type: 'select', required: true, options: productOptions },
    { key: 'variantId', label: 'Variant', type: 'select', nullable: true, options: variantOptions },
    { key: 'customerId', label: 'Customer', type: 'select', nullable: true, options: customerOptions },
    { key: 'manufacturingProcessId', label: 'Manufacturing process', type: 'select', required: true, options: processOptions },
    { key: 'productionLineId', label: 'Production line', type: 'select', required: true, options: productionLineOptions },
    { key: 'plannedQty', label: 'Planned qty', type: 'number', required: true, min: 1 },
    { key: 'scheduledUntil', label: 'Scheduled until', type: 'datetime', required: true },
    { key: 'status', label: 'Status', type: 'select', required: true, options: () => simpleOptions(['Planned', 'In Progress', 'Completed', 'Blocked', 'Cancelled']) },
    { key: 'customerReference', label: 'Customer reference' },
    { key: 'publicTrackingCode', label: 'Public tracking code' },
    { key: 'observations', label: 'Observations', type: 'textarea' },
  ],
  columns: [
    { key: 'orderNumber', label: 'Order' },
    { key: 'publicTrackingCode', label: 'Tracking code' },
    { key: 'customerId', label: 'Customer', format: (value) => customerLabel(value) },
    { key: 'productId', label: 'Product', format: (value) => productLabel(value) },
    { key: 'variantId', label: 'Variant', format: (value) => variantLabel(value) },
    { key: 'plannedQty', label: 'Planned qty' },
    { key: 'scheduledUntil', label: 'Scheduled until', format: formatOptionalDate },
    { key: 'status', label: 'Status', badge: true, format: (value) => translateStatus(String(value || '')) },
  ],
  newItem: () => ({
    orderNumber: '',
    productId: products.value[0]?.id || '',
    variantId: '',
    customerId: customers.value[0]?.id || '',
    manufacturingProcessId: manufacturingProcesses.value[0]?.id || '',
    productionLineId: productionLines.value[0]?.id || '',
    plannedQty: 1,
    scheduledUntil: nowInput(),
    status: 'Planned',
    customerReference: '',
    publicTrackingCode: '',
    observations: '',
  }),
  validate: plannedQtyValidation,
}

const unitsCrud: CrudConfig = {
  key: 'units',
  title: 'Product Units',
  path: '/product-units',
  writePermission: 'MasterData.Manage',
  itemsRef: asCrudRef(units),
  confirmDelete: true,
  fields: [
    { key: 'unitCode', label: 'Unit code', required: true },
    { key: 'unitType', label: 'Unit type', type: 'select', required: true, options: () => simpleOptions(['Subproduto', 'Final']) },
    { key: 'manufacturingOrderId', label: 'Manufacturing order', type: 'select', required: true, options: orderOptions },
    { key: 'variantId', label: 'Variant', type: 'select', nullable: true, options: variantOptions },
    { key: 'parentUnitId', label: 'Parent unit', type: 'select', nullable: true, options: optionalUnitOptions },
    { key: 'status', label: 'Status', type: 'select', required: true, options: () => simpleOptions(['Active', 'In Progress', 'Completed', 'Blocked', 'Rework', 'Scrap']) },
    { key: 'qualityStatus', label: 'Quality status', type: 'select', required: true, options: () => simpleOptions(['Pending', 'PASS', 'FAIL']) },
    { key: 'currentSupportId', label: 'Current support', type: 'select', nullable: true, options: supportOptions },
    { key: 'currentSectionId', label: 'Current section', type: 'select', nullable: true, options: sectionOptions },
  ],
  columns: [
    { key: 'unitCode', label: 'Unit' },
    { key: 'manufacturingOrderId', label: 'Order', format: (value) => orderLabel(value) },
    { key: 'variantId', label: 'Variant', format: (value) => variantLabel(value) },
    { key: 'unitType', label: 'Type', format: (value) => translateUnitType(String(value || '')) },
    { key: 'status', label: 'Status', badge: true, format: (value) => translateStatus(String(value || '')) },
    { key: 'qualityStatus', label: 'Quality', badge: true, format: (value) => translateQualityResult(String(value || '')) },
    { key: 'currentSupportId', label: 'Current support', format: (value) => supportCode(Number(value) || undefined) },
  ],
  newItem: () => ({
    unitCode: '',
    unitType: 'Subproduto',
    manufacturingOrderId: orders.value[0]?.id || '',
    variantId: '',
    parentUnitId: '',
    status: 'Active',
    qualityStatus: 'Pending',
    currentSupportId: '',
    currentSectionId: '',
    createdAt: new Date().toISOString(),
  }),
}

const supportsCrud: CrudConfig = {
  key: 'supports',
  title: 'Supports / WIP Tracking',
  path: '/supports',
  writePermission: 'Supports.Manage',
  itemsRef: asCrudRef(supports),
  confirmDelete: true,
  canDelete: supportDeleteRule,
  fields: [
    { key: 'supportCode', label: 'Support code', required: true },
    { key: 'status', label: 'Status', type: 'select', required: true, options: () => simpleOptions(['Available', 'Loaded', 'Blocked', 'Rework', 'Stored']) },
    { key: 'currentSectionId', label: 'Current section', type: 'select', nullable: true, options: sectionOptions },
  ],
  columns: [
    { key: 'supportCode', label: 'Support' },
    { key: 'status', label: 'Status', badge: true, format: (value) => translateStatus(String(value || '')) },
    { key: 'currentSectionId', label: 'Current section', format: (value) => sectionName(Number(value) || undefined) },
  ],
  newItem: () => ({ supportCode: '', status: 'Available', currentSectionId: '' }),
}

const rawMaterialsCrud: CrudConfig = {
  key: 'raw-materials',
  title: 'Raw materials',
  path: '/raw-materials',
  writePermission: 'Materials.Manage',
  itemsRef: asCrudRef(materials),
  fields: [
    { key: 'name', label: 'Name', required: true },
    { key: 'info', label: 'Info', type: 'textarea' },
  ],
  columns: [
    { key: 'name', label: 'Material', format: (value) => translateMaterialName(String(value || '')) },
    { key: 'info', label: 'Info' },
  ],
  newItem: () => ({ name: '', info: '' }),
}

const lotsCrud: CrudConfig = {
  key: 'lot-raw-materials',
  title: 'Material lots',
  path: '/lot-raw-materials',
  writePermission: 'Materials.Manage',
  itemsRef: asCrudRef(lots),
  fields: [
    { key: 'rawMaterialId', label: 'Material', type: 'select', required: true, options: materialOptions },
    { key: 'sectionId', label: 'Section', type: 'select', nullable: true, options: sectionOptions },
    { key: 'lotNumber', label: 'Lot number', required: true },
    { key: 'lotQuantity', label: 'Lot quantity', type: 'number', required: true, min: 0 },
    { key: 'lotUnit', label: 'Lot unit', type: 'select', required: true, options: () => simpleOptions(['kg', 'L', 'm', 'un.', 'pcs']) },
  ],
  columns: [
    { key: 'lotNumber', label: 'Lot' },
    { key: 'rawMaterialId', label: 'Material', format: (value) => materialName(Number(value)) },
    { key: 'lotQuantity', label: 'Quantity' },
    { key: 'lotUnit', label: 'Unit', format: (value) => translateUnitOfMeasure(String(value || '')) },
    { key: 'sectionId', label: 'Section', format: (value) => sectionName(Number(value) || undefined) },
  ],
  newItem: () => ({ rawMaterialId: materials.value[0]?.id || '', sectionId: '', lotNumber: '', lotQuantity: 0, lotUnit: 'kg' }),
  validate: lotQuantityValidation,
}

const unitMaterialLotUsagesCrud: CrudConfig = {
  key: 'unit-material-lot-usages',
  title: 'Unit material lot usages',
  path: '/unit-material-lot-usages',
  writePermission: 'Materials.Manage',
  itemsRef: asCrudRef(unitMaterialLotUsages),
  confirmDelete: true,
  fields: [
    { key: 'productUnitId', label: 'Product unit', type: 'select', required: true, options: unitOptions },
    { key: 'lotId', label: 'Lot', type: 'select', required: true, options: lotOptions },
    { key: 'associationType', label: 'Association type', type: 'select', required: true, options: () => simpleOptions(['Consumido', 'Reservado']) },
    { key: 'quantity', label: 'Quantity', type: 'number', required: true, min: 1 },
  ],
  columns: [
    { key: 'productUnitId', label: 'Unit', format: (value) => unitCode(Number(value)) },
    { key: 'lotId', label: 'Lot', format: (value) => lotLabel(value) },
    { key: 'associationType', label: 'Type', format: (value) => translateStatus(String(value || '')) },
    { key: 'quantity', label: 'Quantity' },
  ],
  newItem: () => ({ productUnitId: units.value[0]?.id || '', lotId: lots.value[0]?.id || '', associationType: 'Consumido', quantity: 1 }),
  validate: positiveQuantityValidation,
}

const qualityResultsCrud: CrudConfig = {
  key: 'quality-results',
  title: 'Quality results',
  path: '/quality-results',
  writePermission: 'Quality.Record',
  itemsRef: asCrudRef(quality),
  confirmDelete: true,
  fields: [
    { key: 'productUnitId', label: 'Product unit', type: 'select', required: true, options: unitOptions },
    { key: 'checkpointId', label: 'Checkpoint', type: 'select', nullable: true, options: checkpointOptions },
    { key: 'result', label: 'Result', type: 'select', required: true, options: () => simpleOptions(['PASS', 'FAIL']) },
    { key: 'recordedAt', label: 'Recorded at', type: 'datetime', required: true },
    { key: 'notes', label: 'Notes', type: 'textarea' },
  ],
  columns: [
    { key: 'productUnitId', label: 'Unit', format: (value) => unitCode(Number(value)) },
    { key: 'checkpointId', label: 'Checkpoint', format: (value) => checkpointLabel(value) },
    { key: 'result', label: 'Result', badge: true, format: (value) => translateQualityResult(String(value || '')) },
    { key: 'recordedAt', label: 'Recorded at', format: formatOptionalDate },
    { key: 'notes', label: 'Notes' },
  ],
  newItem: () => ({ productUnitId: units.value[0]?.id || '', checkpointId: '', result: 'PASS', recordedAt: nowInput(), notes: '' }),
  validate: qualityResultValidation,
}

const nonconformitiesCrud: CrudConfig = {
  key: 'nonconformities',
  title: 'Nonconformities',
  path: '/nonconformities',
  writePermission: 'Quality.Decide',
  itemsRef: asCrudRef(nonconformities),
  allowDelete: false,
  fields: [
    { key: 'productUnitId', label: 'Product unit', type: 'select', required: true, options: unitOptions },
    { key: 'qualityResultId', label: 'Quality result', type: 'select', nullable: true, options: qualityResultOptions },
    { key: 'severity', label: 'Severity', type: 'select', required: true, options: () => simpleOptions(['Minor', 'Medium', 'Major', 'Critical']) },
    { key: 'status', label: 'Status', type: 'select', required: true, options: () => simpleOptions(['Open', 'Blocked', 'Rework', 'Closed']) },
    { key: 'description', label: 'Description', type: 'textarea' },
  ],
  columns: [
    { key: 'productUnitId', label: 'Unit', format: (value) => unitCode(Number(value)) },
    { key: 'severity', label: 'Severity', badge: true, format: (value) => translateStatus(String(value || '')) },
    { key: 'status', label: 'Status', badge: true, format: (value) => translateStatus(String(value || '')) },
    { key: 'description', label: 'Description' },
  ],
  newItem: () => ({ productUnitId: units.value[0]?.id || '', qualityResultId: '', severity: 'Minor', status: 'Open', description: '', createdAt: new Date().toISOString() }),
}

const reworkRecordsCrud: CrudConfig = {
  key: 'rework-records',
  title: 'Rework records',
  path: '/rework-records',
  writePermission: 'Quality.Decide',
  itemsRef: asCrudRef(reworkRecords),
  allowDelete: false,
  fields: [
    { key: 'productUnitId', label: 'Product unit', type: 'select', required: true, options: unitOptions },
    { key: 'nonconformityId', label: 'Nonconformity', type: 'select', nullable: true, options: nonconformityOptions },
    { key: 'startedAt', label: 'Started at', type: 'datetime', required: true },
    { key: 'endedAt', label: 'Ended at', type: 'datetime', nullable: true },
    { key: 'status', label: 'Status', type: 'select', required: true, options: () => simpleOptions(['Open', 'In Progress', 'Completed', 'Cancelled']) },
    { key: 'notes', label: 'Notes', type: 'textarea' },
  ],
  columns: [
    { key: 'productUnitId', label: 'Unit', format: (value) => unitCode(Number(value)) },
    { key: 'nonconformityId', label: 'Nonconformity', format: (value) => value ? String(value) : '-' },
    { key: 'startedAt', label: 'Started at', format: formatOptionalDate },
    { key: 'status', label: 'Status', badge: true, format: (value) => translateStatus(String(value || '')) },
  ],
  newItem: () => ({ productUnitId: units.value[0]?.id || '', nonconformityId: '', startedAt: nowInput(), endedAt: '', status: 'Open', notes: '' }),
}

const scrapRecordsCrud: CrudConfig = {
  key: 'scrap-records',
  title: 'Scrap records',
  description: 'Scrap records are historical evidence and do not delete product units.',
  path: '/scrap-records',
  writePermission: 'Quality.Decide',
  itemsRef: asCrudRef(scrapRecords),
  allowDelete: false,
  fields: [
    { key: 'productUnitId', label: 'Product unit', type: 'select', required: true, options: unitOptions },
    { key: 'nonconformityId', label: 'Nonconformity', type: 'select', nullable: true, options: nonconformityOptions },
    { key: 'scrappedAt', label: 'Scrapped at', type: 'datetime', required: true },
    { key: 'reason', label: 'Reason', type: 'textarea' },
  ],
  columns: [
    { key: 'productUnitId', label: 'Unit', format: (value) => unitCode(Number(value)) },
    { key: 'nonconformityId', label: 'Nonconformity', format: (value) => value ? String(value) : '-' },
    { key: 'scrappedAt', label: 'Scrapped at', format: formatOptionalDate },
    { key: 'reason', label: 'Reason' },
  ],
  newItem: () => ({ productUnitId: units.value[0]?.id || '', nonconformityId: '', scrappedAt: nowInput(), reason: '' }),
}

const racksCrud: CrudConfig = {
  key: 'racks',
  title: 'Racks / Post-line Logistics',
  path: '/racks',
  writePermission: 'Racks.Manage',
  itemsRef: asCrudRef(racks),
  fields: [
    { key: 'rackCode', label: 'Rack code', required: true },
    { key: 'status', label: 'Status', type: 'select', required: true, options: () => simpleOptions(['Available', 'Loaded', 'Stored', 'Blocked']) },
    { key: 'sectionId', label: 'Section', type: 'select', nullable: true, options: sectionOptions },
  ],
  columns: [
    { key: 'rackCode', label: 'Rack' },
    { key: 'status', label: 'Status', badge: true, format: (value) => translateStatus(String(value || '')) },
    { key: 'sectionId', label: 'Section', format: (value) => sectionName(Number(value) || undefined) },
  ],
  newItem: () => ({ rackCode: '', status: 'Available', sectionId: '' }),
}

const rackAssignmentsCrud: CrudConfig = {
  key: 'rack-support-assignments',
  title: 'Rack support assignments',
  path: '/rack-support-assignments',
  writePermission: 'Racks.Manage',
  itemsRef: asCrudRef(rackSupportAssignments),
  confirmDelete: true,
  fields: [
    { key: 'rackId', label: 'Rack', type: 'select', required: true, options: rackOptions },
    { key: 'supportId', label: 'Support', type: 'select', required: true, options: requiredSupportOptions },
    { key: 'dateTimeIn', label: 'Date/time in', type: 'datetime', required: true },
    { key: 'dateTimeOut', label: 'Date/time out', type: 'datetime', nullable: true },
  ],
  columns: [
    { key: 'rackId', label: 'Rack', format: (value) => rackLabel(value) },
    { key: 'supportId', label: 'Support', format: (value) => supportCode(Number(value)) },
    { key: 'dateTimeIn', label: 'Date/time in', format: formatOptionalDate },
    { key: 'dateTimeOut', label: 'Date/time out', format: formatOptionalDate },
  ],
  newItem: () => ({ rackId: racks.value[0]?.id || '', supportId: supports.value[0]?.id || '', dateTimeIn: nowInput(), dateTimeOut: '' }),
}

const parameterConfigs: CrudConfig[] = [
  {
    key: 'customers',
    title: 'Customers',
    path: '/customers',
    itemsRef: asCrudRef(customers),
    fields: [
      { key: 'customerCode', label: 'Customer code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'contactEmail', label: 'Email' },
    ],
    columns: [
      { key: 'customerCode', label: 'Code' },
      { key: 'name', label: 'Name' },
      { key: 'contactEmail', label: 'Email' },
    ],
    newItem: () => ({ customerCode: '', name: '', contactEmail: '', isActive: true }),
  },
  {
    key: 'products',
    title: 'Products',
    path: '/products',
    itemsRef: asCrudRef(products),
    fields: [
      { key: 'name', label: 'Name', required: true },
      { key: 'info', label: 'Info', type: 'textarea' },
    ],
    columns: [
      { key: 'name', label: 'Name', format: (value) => translateMaterialName(String(value || '')) },
      { key: 'info', label: 'Info' },
    ],
    newItem: () => ({ name: '', info: '' }),
  },
  {
    key: 'variants',
    title: 'Variants',
    path: '/variants',
    itemsRef: asCrudRef(variants),
    fields: [
      { key: 'productId', label: 'Product', type: 'select', required: true, options: productOptions },
      { key: 'variantCode', label: 'Variant code', required: true },
      { key: 'name', label: 'Name', required: true },
    ],
    columns: [
      { key: 'variantCode', label: 'Code' },
      { key: 'name', label: 'Name', format: (value) => translateMaterialName(String(value || '')) },
      { key: 'productId', label: 'Product', format: (value) => productLabel(value) },
    ],
    newItem: () => ({ productId: products.value[0]?.id || '', variantCode: '', name: '' }),
  },
  {
    key: 'production-lines',
    title: 'Production lines',
    path: '/production-lines',
    itemsRef: asCrudRef(productionLines),
    fields: [
      { key: 'lineCode', label: 'Line code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'displayOrder', label: 'Display order', type: 'number', min: 0 },
      { key: 'visualGroup', label: 'Visual group' },
    ],
    columns: [
      { key: 'lineCode', label: 'Code' },
      { key: 'name', label: 'Name' },
      { key: 'displayOrder', label: 'Display order' },
      { key: 'visualGroup', label: 'Visual group' },
    ],
    newItem: () => ({ lineCode: '', name: '', displayOrder: 0, visualGroup: '' }),
  },
  {
    key: 'production-line-sections',
    title: 'Production line sections',
    path: '/production-line-sections',
    itemsRef: asCrudRef(productionLineSections),
    fields: [
      { key: 'sectionCode', label: 'Section code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'sectionType', label: 'Section type', required: true },
      { key: 'lineId', label: 'Production line', type: 'select', nullable: true, options: optionalProductionLineOptions },
      { key: 'displayOrder', label: 'Display order', type: 'number', min: 0 },
      { key: 'layoutColumn', label: 'Layout column', type: 'number', min: 0, nullable: true },
      { key: 'layoutRow', label: 'Layout row', type: 'number', min: 0, nullable: true },
      { key: 'visualZone', label: 'Visual zone' },
    ],
    columns: [
      { key: 'sectionCode', label: 'Code' },
      { key: 'name', label: 'Name', format: (value) => translateSectionName(String(value || '')) },
      { key: 'sectionType', label: 'Type', format: (value) => translateSectionName(String(value || '')) },
      { key: 'lineId', label: 'Production line', format: (value) => lineLabel(value) },
      { key: 'displayOrder', label: 'Display order' },
    ],
    newItem: () => ({ sectionCode: '', name: '', sectionType: '', lineId: productionLines.value[0]?.id || '', displayOrder: 0, layoutColumn: '', layoutRow: '', visualZone: '' }),
  },
  {
    key: 'manufacturing-processes',
    title: 'Manufacturing processes',
    path: '/manufacturing-processes',
    itemsRef: asCrudRef(manufacturingProcesses),
    fields: [
      { key: 'productId', label: 'Product', type: 'select', required: true, options: productOptions },
      { key: 'processName', label: 'Process name', required: true },
      { key: 'info', label: 'Info', type: 'textarea' },
    ],
    columns: [
      { key: 'processName', label: 'Name' },
      { key: 'productId', label: 'Product', format: (value) => productLabel(value) },
      { key: 'info', label: 'Info' },
    ],
    newItem: () => ({ productId: products.value[0]?.id || '', processName: '', info: '' }),
  },
  {
    key: 'manufacturing-section-phases',
    title: 'Manufacturing section phases',
    path: '/manufacturing-section-phases',
    itemsRef: asCrudRef(manufacturingSectionPhases),
    fields: [
      { key: 'sectionId', label: 'Section', type: 'select', required: true, options: requiredSectionOptions },
      { key: 'phaseInfo', label: 'Phase info', required: true },
      { key: 'phaseDuration', label: 'Phase duration', type: 'number', required: true, min: 0 },
    ],
    columns: [
      { key: 'phaseInfo', label: 'Phase' },
      { key: 'sectionId', label: 'Section', format: (value) => sectionName(Number(value)) },
      { key: 'phaseDuration', label: 'Duration' },
    ],
    newItem: () => ({ sectionId: productionLineSections.value[0]?.id || '', phaseInfo: '', phaseDuration: 0 }),
  },
  {
    key: 'manufacturing-process-phases',
    title: 'Manufacturing process phases',
    path: '/manufacturing-process-phases',
    itemsRef: asCrudRef(manufacturingProcessPhases),
    fields: [
      { key: 'manufacturingProcessId', label: 'Manufacturing process', type: 'select', required: true, options: processOptions },
      { key: 'manufacturingPhaseId', label: 'Manufacturing phase', type: 'select', required: true, options: phaseOptions },
      { key: 'resourceId', label: 'Resource', type: 'select', nullable: true, options: resourceOptions },
      { key: 'numberStepOrder', label: 'Step order', type: 'number', required: true, min: 1 },
    ],
    columns: [
      { key: 'manufacturingProcessId', label: 'Process', format: (value) => processLabel(value) },
      { key: 'manufacturingPhaseId', label: 'Phase', format: (value) => getById(manufacturingSectionPhases.value, value)?.phaseInfo || '-' },
      { key: 'resourceId', label: 'Resource', format: (value) => getById(resources.value, value)?.name || '-' },
      { key: 'numberStepOrder', label: 'Step order' },
    ],
    newItem: () => ({ manufacturingProcessId: manufacturingProcesses.value[0]?.id || '', manufacturingPhaseId: manufacturingSectionPhases.value[0]?.id || '', resourceId: '', numberStepOrder: 1 }),
  },
  {
    key: 'resources',
    title: 'Resources',
    path: '/resources',
    itemsRef: asCrudRef(resources),
    fields: [
      { key: 'name', label: 'Name', required: true },
      { key: 'type', label: 'Type', required: true },
      { key: 'function', label: 'Function', required: true },
    ],
    columns: [
      { key: 'name', label: 'Name' },
      { key: 'type', label: 'Type' },
      { key: 'function', label: 'Function' },
    ],
    newItem: () => ({ name: '', type: '', function: '' }),
  },
  {
    key: 'checkpoints',
    title: 'Checkpoints',
    path: '/checkpoints',
    itemsRef: asCrudRef(checkpoints),
    fields: [
      { key: 'checkpointCode', label: 'Checkpoint code', required: true },
      { key: 'name', label: 'Name', required: true },
      { key: 'status', label: 'Status', type: 'select', required: true, options: () => simpleOptions(['Active', 'Blocked']) },
      { key: 'sectionId', label: 'Section', type: 'select', required: true, options: requiredSectionOptions },
    ],
    columns: [
      { key: 'checkpointCode', label: 'Code' },
      { key: 'name', label: 'Name' },
      { key: 'status', label: 'Status', badge: true, format: (value) => translateStatus(String(value || '')) },
      { key: 'sectionId', label: 'Section', format: (value) => sectionName(Number(value)) },
    ],
    newItem: () => ({ checkpointCode: '', name: '', status: 'Active', sectionId: productionLineSections.value[0]?.id || '' }),
  },
]

const materialCrudConfigs = [rawMaterialsCrud, lotsCrud, unitMaterialLotUsagesCrud]
const qualityCrudConfigs = [qualityResultsCrud, nonconformitiesCrud, reworkRecordsCrud, scrapRecordsCrud]
const rackCrudConfigs = [racksCrud, rackAssignmentsCrud]

const activeCrudConfigs = computed(() => {
  if (activeView.value === 'orders') return [ordersCrud]
  if (activeView.value === 'units') return [unitsCrud]
  if (activeView.value === 'supports') return [supportsCrud]
  if (activeView.value === 'materials') return materialCrudConfigs
  if (activeView.value === 'quality') return qualityCrudConfigs
  if (activeView.value === 'racks') return rackCrudConfigs
  if (activeView.value === 'parameters' && canManageUsers.value) return parameterConfigs.filter((config) => config.key === parameterTab.value)
  return []
})

async function apiGetAllowed<T>(permission: string, path: string, fallback: T, blockedFallback: T): Promise<T> {
  if (!can(permission)) return blockedFallback
  return apiGet(path, fallback)
}

async function loadData(showSpinner = true) {
  if (showSpinner) loading.value = true
  const [
    dashboardData,
    productData,
    variantData,
    customerData,
    productionLineData,
    sectionData,
    resourceData,
    processData,
    sectionPhaseData,
    processPhaseData,
    checkpointData,
    orderData,
    unitData,
    supportData,
    rackData,
    rackAssignmentData,
    materialData,
    lotData,
    usageData,
    qualityData,
    nonconformityData,
    reworkData,
    scrapData,
    supportHistoryData,
    contextData,
    flowSummaryData,
    operatorWorkbenchData,
    permissionCatalogData,
    operationalEventsData,
    authContextData,
    customerOrdersData,
  ] = await Promise.all([
    apiGetAllowed('ProductUnits.View', '/dashboard/summary', demoDashboard, emptyDashboardSummary()),
    apiGetAllowed('MasterData.Manage', '/products', demoProducts, [] as Product[]),
    apiGetAllowed('MasterData.Manage', '/variants', demoVariants, [] as Variant[]),
    apiGetAllowed('MasterData.Manage', '/customers', demoCustomers, [] as Customer[]),
    apiGetAllowed('MasterData.Manage', '/production-lines', demoProductionLines, [] as ProductionLine[]),
    apiGetAllowed('MasterData.Manage', '/production-line-sections', demoProductionLineSections, [] as ProductionLineSection[]),
    apiGetAllowed('MasterData.Manage', '/resources', demoResources, [] as ResourceRecord[]),
    apiGetAllowed('MasterData.Manage', '/manufacturing-processes', demoManufacturingProcesses, [] as ManufacturingProcess[]),
    apiGetAllowed('MasterData.Manage', '/manufacturing-section-phases', demoManufacturingSectionPhases, [] as ManufacturingSectionPhase[]),
    apiGetAllowed('MasterData.Manage', '/manufacturing-process-phases', demoManufacturingProcessPhases, [] as ManufacturingProcessPhase[]),
    apiGetAllowed('Quality.View', '/checkpoints', demoCheckpoints, [] as Checkpoint[]),
    apiGetAllowed('Orders.View', '/manufacturing-orders', demoOrders, [] as ManufacturingOrder[]),
    apiGetAllowed('ProductUnits.View', '/product-units', demoUnits, [] as ProductUnit[]),
    apiGetAllowed('ProductUnits.View', '/supports', demoSupports, [] as Support[]),
    apiGetAllowed('Racks.View', '/racks', demoRacks, [] as Rack[]),
    apiGetAllowed('Racks.View', '/rack-support-assignments', demoRackSupportAssignments, [] as RackSupportAssignment[]),
    apiGetAllowed('Materials.View', '/raw-materials', demoMaterials, [] as RawMaterial[]),
    apiGetAllowed('Materials.View', '/lot-raw-materials', demoLots, [] as LotRawMaterial[]),
    apiGetAllowed('Materials.View', '/unit-material-lot-usages', demoUnitMaterialLotUsages, [] as UnitMaterialLotUsage[]),
    apiGetAllowed('Quality.View', '/quality-results', demoQuality, [] as QualityRecord[]),
    apiGetAllowed('Quality.View', '/nonconformities', demoNonconformities, [] as NonconformityRecord[]),
    apiGetAllowed('Quality.View', '/rework-records', demoReworkRecords, [] as ReworkRecord[]),
    apiGetAllowed('Quality.View', '/scrap-records', demoScrapRecords, [] as ScrapRecord[]),
    apiGetAllowed('ProductUnits.Trace', '/support-localization-history', demoSupportLocalizationHistory, [] as SupportLocalizationHistory[]),
    apiGetAllowed('Fiware.View', '/fiware/context', emptyFiwareContext(), emptyFiwareContext()),
    apiGetAllowed('ProductUnits.View', '/operations/flow-summary', emptyFlowSummary(), emptyFlowSummary()),
    apiGetAllowed('ProductUnits.View', '/operator/workbench', emptyOperatorWorkbench(), emptyOperatorWorkbench()),
    apiGet(`/permissions/catalog?role=${encodeURIComponent(backendRoleFor(user.value?.roleKey))}`, fallbackPermissionCatalog),
    apiGetAllowed('OperationalEvents.View', '/operational-events/recent?limit=20', [] as OperationalEventRecord[], [] as OperationalEventRecord[]),
    apiGet('/auth/me', null as AuthUserContext | null),
    apiGetAllowed('CustomerPortal.View', '/customer/orders', [] as CustomerOrderLookup[], [] as CustomerOrderLookup[]),
  ])

  summary.value = dashboardData
  products.value = productData
  variants.value = variantData
  customers.value = customerData
  productionLines.value = productionLineData
  productionLineSections.value = sectionData
  resources.value = resourceData
  manufacturingProcesses.value = processData
  manufacturingSectionPhases.value = sectionPhaseData
  manufacturingProcessPhases.value = processPhaseData
  checkpoints.value = checkpointData
  orders.value = orderData
  units.value = unitData
  supports.value = supportData
  racks.value = rackData
  rackSupportAssignments.value = rackAssignmentData
  materials.value = materialData
  lots.value = lotData
  unitMaterialLotUsages.value = usageData
  quality.value = qualityData
  nonconformities.value = nonconformityData
  reworkRecords.value = reworkData
  scrapRecords.value = scrapData
  supportHistory.value = supportHistoryData
  fiwareContext.value = normalizeFiwareContextPayload(contextData)
  flowSummary.value = flowSummaryData
  operatorWorkbench.value = operatorWorkbenchData
  permissionCatalog.value = permissionCatalogData
  operationalEvents.value = operationalEventsData
  serverUserContext.value = authContextData
  customerOrders.value = customerOrdersData
  if (authContextData?.customer?.defaultPublicTrackingCode && user.value?.roleKey === 'client') {
    customerLookupCode.value = authContextData.customer.defaultPublicTrackingCode
  }
  ensureAccessibleView()
  apiStatus.value = dashboardData === demoDashboard ? 'Offline demo data loaded' : 'Connected to DriveTrace Core API'
  if (showSpinner) loading.value = false
}

async function executePlayback() {
  eventStatus.value = t('Executing playback...')
  try {
    await apiPost('/events/playback', { scenario: 'door-line-demo' })
    eventStatus.value = t('Playback executed. Dashboard data refreshed.')
    await loadData()
  } catch (error) {
    eventStatus.value = t('API unavailable. Playback endpoint is ready but could not be reached from the browser.')
  }
}

async function injectManualEvent() {
  eventStatus.value = t('Sending manual controlled event...')
  try {
    await apiPost('/events/manual', manualEvent.value)
    eventStatus.value = t('Manual event accepted. Dashboard data refreshed.')
    await loadData()
  } catch (error) {
    eventStatus.value = t('API unavailable or event rejected. Check Swagger for the exact backend response.')
  }
}

async function loadUnitTrace(unitId: number) {
  traceLoading.value = true
  transferStatus.value = ''
  try {
    const response = await api.get<ProductUnitTrace>(`/product-units/${unitId}/trace`)
    selectedUnitTrace.value = response.data
  } catch (error) {
    transferStatus.value = `${t('Could not load product unit trace.')}: ${getApiErrorMessage(error)}`
  } finally {
    traceLoading.value = false
  }
}

function prepareTransfer(unit: ProductUnit) {
  transferForm.value = {
    productUnitId: String(unit.id),
    toSectionId: String(unit.currentSectionId || transferTargetSections.value[0]?.id || ''),
    toSupportId: String(unit.currentSupportId || ''),
    reason: 'Operational transfer',
    notes: '',
    moveCurrentSupport: true,
  }
  void loadUnitTrace(unit.id)
}

async function submitUnitTransfer() {
  const unitId = Number(transferForm.value.productUnitId)
  const toSectionId = Number(transferForm.value.toSectionId)
  if (!unitId || !toSectionId) {
    transferStatus.value = t('Select a product unit and target section.')
    return
  }

  transferStatus.value = t('Registering product-unit transfer...')
  try {
    await apiPost(`/product-units/${unitId}/transfer`, {
      toSectionId,
      toSupportId: transferForm.value.toSupportId ? Number(transferForm.value.toSupportId) : null,
      reason: transferForm.value.reason,
      notes: transferForm.value.notes,
      operatorUserId: user.value?.username || 'dashboard',
      moveCurrentSupport: transferForm.value.moveCurrentSupport,
    })
    transferStatus.value = t('Product-unit transfer registered.')
    await loadData(false)
    await loadUnitTrace(unitId)
  } catch (error) {
    transferStatus.value = `${t('Transfer rejected')}: ${getApiErrorMessage(error)}`
  }
}

async function lookupCustomerOrder() {
  const code = customerLookupCode.value.trim()
  if (!code) {
    customerLookupStatus.value = t('Enter a public tracking code.')
    customerLookup.value = null
    return
  }

  customerLookupStatus.value = t('Searching customer order...')
  try {
    const response = await api.get<CustomerOrderLookup>(`/customer/orders/${encodeURIComponent(code)}`)
    customerLookup.value = response.data
    customerLookupStatus.value = t('Customer order loaded.')
  } catch (error) {
    customerLookup.value = null
    customerLookupStatus.value = `${t('Customer order not found')}: ${getApiErrorMessage(error)}`
  }
}

async function openCustomerOrderDetail(publicTrackingCode?: string) {
  const code = publicTrackingCode?.trim()
  if (!code) return
  customerLookupCode.value = code
  await lookupCustomerOrder()
}

async function submitCustomerOrder() {
  customerOrderStatus.value = ''
  customerCreatedOrder.value = null
  const quantity = Number(customerOrderForm.value.quantity)
  const orderName = customerOrderForm.value.name.trim()
  if (!orderName || !customerOrderForm.value.productId || !customerOrderForm.value.variantId || !Number.isFinite(quantity) || quantity < 1) {
    customerOrderStatus.value = t('Preencha nome, produto, variante e quantidade antes de submeter.')
    return
  }

  try {
    const created = await apiPost('/customer/orders', {
      name: orderName,
      productId: customerOrderForm.value.productId ? Number(customerOrderForm.value.productId) : null,
      variantId: customerOrderForm.value.variantId ? Number(customerOrderForm.value.variantId) : null,
      quantity,
      observations: customerOrderForm.value.observations.trim(),
    })
    customerLookup.value = created as CustomerOrderLookup
    customerCreatedOrder.value = customerLookup.value
    customerLookupCode.value = customerLookup.value.publicTrackingCode || ''
    customerOrderStatus.value = `${t('Encomenda criada com sucesso')}. ${t('Código de rastreio')}: ${customerLookup.value.publicTrackingCode || '-'}`
    customerOrderForm.value = { name: '', productId: '', variantId: '', quantity: 1, observations: '' }
    await loadData(false)
  } catch (error) {
    customerOrderStatus.value = `${t('Error saving changes')}: ${getApiErrorMessage(error)}`
  }
}

function resetCustomerOrderForm() {
  customerOrderForm.value = { name: '', productId: '', variantId: '', quantity: 1, observations: '' }
  customerOrderStatus.value = ''
  customerCreatedOrder.value = null
  activeView.value = 'customerNewOrder'
}

async function refreshFiwareContext(showStatus = true) {
  fiwareLoading.value = true
  if (showStatus) fiwareActionStatus.value = t('Refreshing FIWARE context...')

  try {
    const response = await api.get('/fiware/context')
    fiwareContext.value = normalizeFiwareContextPayload(response.data)
    if (showStatus) fiwareActionStatus.value = t('FIWARE context updated.')
  } catch (error) {
    fiwareActionStatus.value = `${t('Could not load FIWARE context from API.')}: ${getApiErrorMessage(error)}`
    fiwareContext.value = {
      ...emptyFiwareContext(),
      message: fiwareActionStatus.value,
      source: 'relational-fallback',
    }
  } finally {
    fiwareLoading.value = false
  }
}

async function publishFiware() {
  fiwareLoading.value = true
  fiwareActionStatus.value = t('Publishing current context to Orion-LD...')
  try {
    const publishResponse = await apiPost<Record<string, never>, FiwarePublishResult>('/fiware/publish-current', {})
    fiwareLastPublish.value = publishResponse
    fiwareActionStatus.value = publishResponse.failedCount > 0 || publishResponse.errors.length > 0
      ? t('FIWARE publish completed with errors.')
      : publishResponse.staleDeletedCount > 0
        ? t('FIWARE context synchronized and stale entities removed.')
        : t('FIWARE context published successfully.')
    if (publishResponse.errors.length) fiwareActionStatus.value = `${fiwareActionStatus.value} ${publishResponse.errors[0]}`
    await refreshFiwareContext(false)
  } catch (error) {
    fiwareActionStatus.value = `${t('Could not reach the FIWARE publish endpoint. The relational demo remains usable.')} ${getApiErrorMessage(error)}`
  } finally {
    fiwareLoading.value = false
  }
}

function handleDesktopMediaChange(event: MediaQueryListEvent | MediaQueryList) {
  if (event.matches) closeMobileSidebar()
}

onMounted(() => {
  if (isAuthenticated.value) {
    void loadData()
  } else {
    loading.value = false
  }
  if (typeof window !== 'undefined' && 'matchMedia' in window) {
    desktopMediaQuery = window.matchMedia('(min-width: 1024px)')
    handleDesktopMediaChange(desktopMediaQuery)
    desktopMediaQuery.addEventListener('change', handleDesktopMediaChange)
  }
})

onBeforeUnmount(() => {
  stopSidebarResize()
  if (desktopMediaQuery) {
    desktopMediaQuery.removeEventListener('change', handleDesktopMediaChange)
  }
})
</script>

<template>
  <!-- Top-level wrapper applies dark mode to the entire page based on the
       current theme. Vue's reactivity ensures updates are reflected on
       change. -->
  <div class="min-h-screen bg-slate-100 text-slate-900 dark:bg-slate-900 dark:text-slate-100">
    <!-- LOGIN / REGISTO -->
    <div
      v-if="!isAuthenticated"
      class="flex items-center justify-center min-h-screen p-4"
    >
      <div class="w-full max-w-md card p-8">
        <div class="mb-6 text-center">
          <img
            :src="logoUrl"
            alt="DRIVOLUTION logo"
            class="mx-auto h-20 w-auto object-contain"
          >
          <h1 class="mt-4 text-2xl font-black tracking-normal">
            {{ isRegistering ? t('Register new user') : t('Login') }}
          </h1>
          <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">
            DriveTrace Core · DRIVOLUTION WP3
          </p>
        </div>
        <form
          v-if="!isRegistering"
          class="space-y-4"
          @submit.prevent="login"
        >
          <label class="form-label">{{ t('Username') }}<input
            v-model="loginForm.username"
            class="form-input"
          ></label>
          <label class="form-label">{{ t('Password') }}<input
            v-model="loginForm.password"
            type="password"
            class="form-input"
          ></label>
          <p
            v-if="loginError"
            class="text-red-600 text-sm"
          >
            {{ loginError }}
          </p>
          <button
            type="submit"
            class="btn-primary w-full"
          >
            {{ t('Submit') }}
          </button>
          <button
            type="button"
            class="btn-secondary w-full"
            @click="isRegistering = true; loginError = ''"
          >
            {{ t('Create account') }}
          </button>
          <div class="rounded-lg border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-900/50">
            <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">
              Contas demo
            </p>
            <div class="mt-2 grid grid-cols-2 gap-2">
              <button
                v-for="profile in demoLoginProfiles"
                :key="profile.username"
                type="button"
                class="btn-secondary btn-compact justify-center"
                @click="loginAsDemo(profile)"
              >
                {{ profile.username }}
              </button>
            </div>
          </div>
        </form>
        <form
          v-else
          class="space-y-4"
          @submit.prevent="registerUser(false)"
        >
          <label class="form-label">{{ t('Name / full name') }}<input
            v-model="registerForm.name"
            class="form-input"
          ></label>
          <label class="form-label">{{ t('Username') }}<input
            v-model="registerForm.username"
            class="form-input"
          ></label>
          <label class="form-label">{{ t('Email') }}<input
            v-model="registerForm.email"
            type="email"
            class="form-input"
          ></label>
          <label class="form-label">{{ t('Password') }}<input
            v-model="registerForm.password"
            type="password"
            class="form-input"
          ></label>
          <label class="form-label">{{ t('Confirm password') }}<input
            v-model="registerForm.confirmPassword"
            type="password"
            class="form-input"
          ></label>
          <label class="form-label">{{ t('Role') }}
            <select
              v-model="registerForm.roleKey"
              class="form-input"
            >
              <option value="admin">{{ t('Administrator') }}</option>
              <option value="supervisor">{{ t('Supervisor') }}</option>
              <option value="operator">{{ t('Operator') }}</option>
              <option value="quality">{{ t('Quality technician') }}</option>
              <option value="logistics">{{ t('Logistics') }}</option>
              <option value="client">{{ t('Client') }}</option>
            </select>
          </label>
          <p
            v-if="registerError"
            class="text-red-600 text-sm"
          >
            {{ registerError }}
          </p>
          <p
            v-if="registerSuccess"
            class="text-green-600 text-sm"
          >
            {{ registerSuccess }}
          </p>
          <button
            type="submit"
            class="btn-primary w-full"
          >
            {{ t('Create user') }}
          </button>
          <button
            type="button"
            class="btn-secondary w-full"
            @click="isRegistering = false; registerError = ''; registerSuccess = ''"
          >
            {{ t('Already have an account?') }}
          </button>
        </form>
      </div>
    </div>

    <!-- MAIN APPLICATION -->
    <div
      v-else
      class="app-shell"
      :style="appShellStyle"
    >
      <!-- SIDEBAR -->
      <aside
        class="app-sidebar"
        :class="{ 'app-sidebar-open': mobileSidebarOpen }"
      >
        <div class="app-sidebar-brand">
          <button
            class="app-sidebar-close lg:hidden"
            type="button"
            :aria-label="t('Close navigation')"
            @click="closeMobileSidebar"
          >
            ×
          </button>
          <img
            :src="logoUrl"
            alt="DRIVOLUTION logo"
            class="h-9 w-auto max-w-full object-contain"
          >
          <h1>DriveTrace Core</h1>
        </div>
        <div class="app-sidebar-nav">
          <nav>
            <div
              v-for="group in navGroups"
              v-show="navByGroup(group.key).length"
              :key="group.key"
              class="nav-group"
            >
              <p class="nav-group-title">
                {{ t(group.key) }}
              </p>
              <button
                v-for="item in navByGroup(group.key)"
                :key="item.key"
                class="nav-item"
                :class="activeView === item.key ? 'nav-item-active' : ''"
                @click="navigateTo(item.key)"
              >
                <span class="nav-icon">{{ navIcon(item.key, item.icon) }}</span>
                <span class="nav-label">{{ t(item.label) }}</span>
              </button>
            </div>
          </nav>
        </div>
        <!-- Quick settings / user info section replacing the academic scope block -->
        <div class="app-sidebar-profile">
          <p class="font-black text-slate-950 dark:text-slate-50">
            {{ sidebarProfileName }}
          </p>
          <div class="app-sidebar-profile-actions">
            <button
              class="btn-secondary btn-compact"
              @click="openProfileView"
            >
              {{ t('Profile') }}
            </button>
            <button
              class="btn-secondary btn-compact"
              @click="navigateTo('settings')"
            >
              {{ t('Settings') }}
            </button>
            <button
              class="btn-secondary btn-compact"
              @click="logout"
            >
              {{ t('Sign out') }}
            </button>
          </div>
        </div>
        <button
          class="app-sidebar-resize-handle"
          type="button"
          :aria-label="t('Resize sidebar')"
          :title="t('Resize sidebar')"
          @pointerdown="startSidebarResize"
          @dblclick="resetSidebarWidth"
        />
      </aside>
      <button
        v-if="mobileSidebarOpen"
        type="button"
        class="app-sidebar-overlay lg:hidden"
        :aria-label="t('Close navigation')"
        @click="closeMobileSidebar"
      />

      <main class="app-main">
        <!-- TOP BAR -->
        <header class="app-topbar">
          <div class="content-shell px-3 py-2.5 sm:px-4 lg:px-5 xl:px-6">
            <div class="flex flex-col justify-between gap-2 lg:flex-row lg:items-center">
              <div class="flex min-w-0 flex-1 items-center gap-2 sm:gap-3">
                <button
                  class="icon-button lg:hidden"
                  type="button"
                  :aria-label="mobileSidebarOpen ? t('Close navigation') : t('Open navigation')"
                  @click="toggleMobileSidebar"
                >
                  ☰
                </button>
                <img
                  :src="logoUrl"
                  alt="DRIVOLUTION logo"
                  class="h-8 w-8 shrink-0 rounded-lg object-contain lg:hidden"
                >
                <div class="min-w-0">
                  <h2 class="truncate text-lg font-black leading-tight tracking-normal sm:text-xl">
                    {{ t(activeViewTitle) }}
                  </h2>
                  <p class="truncate text-xs font-semibold text-slate-600 dark:text-slate-300 sm:text-sm">
                    {{ t(activeViewSubtitle) }}
                  </p>
                </div>
              </div>
              <div class="topbar-controls">
                <button
                  class="icon-button"
                  type="button"
                  :title="t('Refresh')"
                  :aria-label="t('Refresh data')"
                  @click="() => loadData()"
                >
                  ⟳
                </button>
                <span class="text-xs font-bold text-slate-500 dark:text-slate-400">{{ t('Updated') }}: {{ latestDataUpdateShort }}</span>
                <select
                  v-model="locale"
                  class="compact-select"
                  :aria-label="t('Language')"
                >
                  <option value="pt-PT">
                    Português
                  </option>
                  <option value="en">
                    English
                  </option>
                </select>
                <button
                  class="icon-button"
                  type="button"
                  :title="theme === 'dark' ? t('Light mode') : t('Dark mode')"
                  :aria-label="theme === 'dark' ? t('Light mode') : t('Dark mode')"
                  @click="toggleTheme"
                >
                  {{ theme === 'dark' ? '☀' : '☾' }}
                </button>
                <span
                  class="api-health"
                  :class="apiHealthClass"
                  :title="apiHealthTitle"
                >
                  <span class="api-health-dot" />
                  <span>API</span>
                </span>
                <div class="relative">
                  <button
                    class="avatar-button"
                    :title="user?.name"
                    @click="showUserMenu = !showUserMenu"
                  >
                    <span class="inline-flex h-6 w-6 items-center justify-center rounded-full bg-drivolution-500 text-xs font-black text-white">{{ user?.name.charAt(0) }}</span>
                  </button>
                  <div
                    v-if="showUserMenu"
                    class="absolute right-0 z-30 mt-2 w-44 rounded-lg border border-slate-200 bg-white p-1 shadow-lg dark:border-slate-700 dark:bg-slate-900"
                  >
                    <button
                      class="w-full rounded-lg px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-800"
                      @click="openProfileView"
                    >
                      {{ t('Profile') }}
                    </button>
                    <button
                      class="w-full rounded-lg px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-800"
                      @click="navigateTo('settings')"
                    >
                      {{ t('Settings') }}
                    </button>
                    <button
                      class="w-full rounded-lg px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-800"
                      @click="logout(); showUserMenu = false"
                    >
                      {{ t('Sign out') }}
                    </button>
                  </div>
                </div>
              </div>
            </div>
            <!-- Mobile tab navigation -->
            <div class="mt-2 flex gap-2 overflow-x-auto pb-1 lg:hidden">
              <button
                v-for="item in nav"
                v-show="canShowNav(item.key)"
                :key="item.key"
                class="mobile-tab"
                :class="activeView === item.key ? 'mobile-tab-active' : ''"
                @click="navigateTo(item.key)"
              >
                {{ t(item.label) }}
              </button>
            </div>
          </div>
        </header>
        <!-- MAIN CONTENT -->
        <section class="app-content">
          <div class="content-shell">
            <p
              v-if="permissionNotice"
              class="mb-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-semibold text-amber-900 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-100"
            >
              {{ permissionNotice }}
            </p>
            <div
              v-if="loading"
              class="card p-6 text-center text-slate-600 dark:bg-slate-800 dark:text-slate-200 sm:p-8"
            >
              {{ t('Loading DriveTrace Core data...') }}
            </div>
            <template v-else>
              <div v-if="activeView === 'traceGraph'">
                <TraceGraphView
                  :initial-product-unit-id="traceGraphInitialUnitId"
                  :initial-order-id="traceGraphInitialOrderId"
                />
              </div>

              <div v-if="activeView === 'reconditioning'">
                <ReconditioningView @open-trace-graph="openTraceGraphForUnit" />
              </div>

              <div v-if="activeView === 'simulation'">
                <ProductionSimulatorView
                  @open-trace-graph-unit="openTraceGraphForUnit"
                  @open-trace-graph-order="openTraceGraphForOrder"
                  @open-analytics="navigateTo('analytics')"
                  @open-fiware="navigateTo('fiware')"
                  @refresh="loadData(false)"
                />
              </div>

              <!-- PROFILE VIEW -->
              <div
                v-if="activeView === 'profile'"
                class="mx-auto w-full max-w-4xl"
              >
                <section class="card p-5 sm:p-6 lg:p-8">
                  <div class="flex flex-col gap-4 border-b border-slate-200 pb-5 dark:border-slate-700 sm:flex-row sm:items-center sm:justify-between">
                    <div class="flex items-center gap-4">
                      <span class="inline-flex h-14 w-14 items-center justify-center rounded-full bg-drivolution-500 text-xl font-black text-white">{{ user?.name?.charAt(0)?.toUpperCase() || 'U' }}</span>
                      <div>
                        <p class="text-xs font-bold uppercase tracking-normal text-drivolution-700 dark:text-drivolution-300">
                          {{ t('Profile') }}
                        </p>
                        <h3 class="mt-1 text-2xl font-black text-slate-950 dark:text-white">
                          {{ user?.name }}
                        </h3>
                        <p class="mt-1 text-sm font-semibold text-slate-600 dark:text-slate-300">
                          {{ user ? getRoleLabel(user.roleKey) : '' }}
                        </p>
                      </div>
                    </div>
                    <button
                      v-if="!isEditingProfile"
                      class="btn-primary w-full sm:w-auto"
                      @click="startProfileEdit"
                    >
                      {{ t('Edit profile') }}
                    </button>
                  </div>

                  <div class="mt-5">
                    <h4 class="text-xs font-bold uppercase tracking-normal text-slate-500 dark:text-slate-400">
                      {{ t('Account information') }}
                    </h4>
                    <div class="mt-3 divide-y divide-slate-200 overflow-hidden rounded-lg border border-slate-200 dark:divide-slate-700 dark:border-slate-700">
                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">
                          {{ t('Name') }}
                        </p>
                        <div>
                          <input
                            v-if="isEditingProfile"
                            v-model="profileForm.name"
                            class="form-input py-2 sm:py-2.5"
                          >
                          <p
                            v-else
                            class="text-sm font-semibold text-slate-900 dark:text-slate-100"
                          >
                            {{ user?.name }}
                          </p>
                        </div>
                        <button
                          v-if="!isEditingProfile"
                          class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2"
                          @click="startProfileEdit"
                        >
                          {{ t('Edit') }}
                        </button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">
                          {{ t('Username') }}
                        </p>
                        <p class="text-sm font-semibold text-slate-900 dark:text-slate-100">
                          {{ user?.username }}
                        </p>
                        <span class="inline-flex justify-center rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-600 dark:bg-slate-700 dark:text-slate-200">{{ t('Read-only') }}</span>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">
                          {{ t('Email') }}
                        </p>
                        <div>
                          <input
                            v-if="isEditingProfile"
                            v-model="profileForm.email"
                            type="email"
                            class="form-input py-2 sm:py-2.5"
                          >
                          <p
                            v-else
                            class="text-sm font-semibold text-slate-900 dark:text-slate-100"
                          >
                            {{ user?.email }}
                          </p>
                        </div>
                        <button
                          v-if="!isEditingProfile"
                          class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2"
                          @click="startProfileEdit"
                        >
                          {{ t('Edit') }}
                        </button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">
                          {{ t('Role') }}
                        </p>
                        <p class="text-sm font-semibold text-slate-900 dark:text-slate-100">
                          {{ user ? getRoleLabel(user.roleKey) : '' }}
                        </p>
                        <span class="inline-flex justify-center rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-600 dark:bg-slate-700 dark:text-slate-200">{{ t('Read-only') }}</span>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">
                          {{ t('Organization') }}
                        </p>
                        <div>
                          <input
                            v-if="isEditingProfile"
                            v-model="profileForm.organization"
                            class="form-input py-2 sm:py-2.5"
                          >
                          <p
                            v-else
                            class="text-sm font-semibold text-slate-900 dark:text-slate-100"
                          >
                            {{ profileValue(user?.organization) }}
                          </p>
                        </div>
                        <button
                          v-if="!isEditingProfile"
                          class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2"
                          @click="startProfileEdit"
                        >
                          {{ t('Edit') }}
                        </button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">
                          {{ t('Job title') }}
                        </p>
                        <div>
                          <input
                            v-if="isEditingProfile"
                            v-model="profileForm.jobTitle"
                            class="form-input py-2 sm:py-2.5"
                          >
                          <p
                            v-else
                            class="text-sm font-semibold text-slate-900 dark:text-slate-100"
                          >
                            {{ profileJobTitle(user?.jobTitle) }}
                          </p>
                        </div>
                        <button
                          v-if="!isEditingProfile"
                          class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2"
                          @click="startProfileEdit"
                        >
                          {{ t('Edit') }}
                        </button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">
                          {{ t('Current language') }}
                        </p>
                        <p class="text-sm font-semibold text-slate-900 dark:text-slate-100">
                          {{ locale === 'pt-PT' ? t('Portuguese') : t('English') }}
                        </p>
                        <button
                          class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2"
                          @click="activeView = 'settings'"
                        >
                          {{ t('Manage in Settings') }}
                        </button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">
                          {{ t('Current theme') }}
                        </p>
                        <p class="text-sm font-semibold text-slate-900 dark:text-slate-100">
                          {{ theme === 'dark' ? t('Dark') : t('Light') }}
                        </p>
                        <button
                          class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2"
                          @click="activeView = 'settings'"
                        >
                          {{ t('Manage in Settings') }}
                        </button>
                      </div>
                    </div>
                  </div>

                  <p
                    v-if="profileError"
                    class="mt-4 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700 dark:border-red-700/60 dark:bg-red-900/30 dark:text-red-100"
                  >
                    {{ profileError }}
                  </p>
                  <p
                    v-if="profileSuccess"
                    class="mt-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700 dark:border-emerald-700/60 dark:bg-emerald-900/30 dark:text-emerald-100"
                  >
                    {{ profileSuccess }}
                  </p>

                  <div class="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
                    <button
                      v-if="isEditingProfile"
                      class="btn-primary sm:order-2"
                      @click="saveProfileChanges"
                    >
                      {{ t('Save changes') }}
                    </button>
                    <button
                      v-if="isEditingProfile"
                      class="btn-secondary sm:order-1"
                      @click="cancelProfileEdit"
                    >
                      {{ t('Cancel') }}
                    </button>
                    <button
                      class="btn-secondary sm:order-3"
                      @click="navigateTo(homeViewForRole(user?.roleKey))"
                    >
                      {{ t('Back to dashboard') }}
                    </button>
                    <button
                      class="btn-secondary sm:order-4"
                      @click="activeView = 'settings'"
                    >
                      {{ t('Open settings') }}
                    </button>
                  </div>
                </section>
              </div>

              <!-- SETTINGS VIEW -->
              <div
                v-if="activeView === 'settings'"
                class="card space-y-5 p-5 sm:space-y-6 sm:p-6"
              >
                <div class="section-heading">
                  <div><p>{{ t('Settings') }}</p><h3>{{ t('Application Settings') }}</h3></div>
                </div>
                <div>
                  <h4 class="font-bold mb-2">
                    {{ t('Language') }}
                  </h4>
                  <div class="grid gap-2 sm:grid-cols-2">
                    <button
                      class="btn-secondary flex-1"
                      :class="{ 'nav-item-active': locale === 'pt-PT' }"
                      @click="setLocale('pt-PT')"
                    >
                      {{ t('Portuguese') }}
                    </button>
                    <button
                      class="btn-secondary flex-1"
                      :class="{ 'nav-item-active': locale === 'en' }"
                      @click="setLocale('en')"
                    >
                      {{ t('English') }}
                    </button>
                  </div>
                </div>
                <div>
                  <h4 class="font-bold mb-2">
                    {{ t('Theme') }}
                  </h4>
                  <div class="grid gap-2 sm:grid-cols-2">
                    <button
                      class="btn-secondary flex-1"
                      :class="{ 'nav-item-active': theme === 'light' }"
                      @click="setTheme('light')"
                    >
                      {{ t('Light') }}
                    </button>
                    <button
                      class="btn-secondary flex-1"
                      :class="{ 'nav-item-active': theme === 'dark' }"
                      @click="setTheme('dark')"
                    >
                      {{ t('Dark') }}
                    </button>
                  </div>
                </div>
                <div>
                  <h4 class="font-bold mb-2">
                    {{ t('Accessibility') }}
                  </h4>
                  <p class="text-sm text-slate-600 dark:text-slate-300">
                    {{ t('Clear mode is the default. Dark mode, language and session preferences are persisted locally.') }}
                  </p>
                </div>
                <div class="grid gap-4 md:grid-cols-2">
                  <div class="rounded-lg border border-slate-200 p-4 dark:border-slate-700">
                    <h4 class="font-bold">
                      {{ t('Application information') }}
                    </h4>
                    <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">
                      DriveTrace Core
                    </p>
                    <p class="mt-1 text-sm text-slate-600 dark:text-slate-300">
                      {{ t('Active profile') }}: {{ user?.username }} · {{ user ? getRoleLabel(user.roleKey) : '' }}
                    </p>
                  </div>
                  <div class="rounded-lg border border-slate-200 p-4 dark:border-slate-700">
                    <h4 class="font-bold">
                      {{ t('API URL') }}
                    </h4>
                    <p class="mt-2 break-all text-sm font-semibold text-slate-600 dark:text-slate-300">
                      {{ apiBaseUrl }}
                    </p>
                  </div>
                </div>
                <div class="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm font-semibold text-amber-900 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-100">
                  <p>{{ t('Demo/local authentication') }}</p>
                  <p class="mt-1 font-medium">
                    {{ t('The API is not protected by JWT yet. Demo role headers are enforced by backend permission guards in this V1.') }}
                  </p>
                </div>
                <button
                  class="btn-secondary"
                  @click="navigateTo(homeViewForRole(user?.roleKey))"
                >
                  {{ t('Back to dashboard') }}
                </button>
              </div>

              <!-- CRUD VIEWS -->
              <div
                v-if="activeCrudConfigs.length"
                class="space-y-5 lg:space-y-6"
              >
                <section
                  v-if="activeView === 'units'"
                  class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
                >
                  <div class="metric-card">
                    <span>{{ t('Traceable units') }}</span><strong>{{ units.length }}</strong>
                  </div>
                  <div class="metric-card">
                    <span>{{ t('Active / held') }}</span><strong>{{ activeUnits.length }}</strong>
                  </div>
                  <div class="metric-card">
                    <span>{{ t('Deviations') }}</span><strong>{{ blockedUnits.length }}</strong>
                  </div>
                </section>

                <section
                  v-if="activeView === 'units'"
                  class="industrial-panel"
                >
                  <div class="section-heading">
                    <div>
                      <p>{{ t('ProductUnit route') }}</p>
                      <h3>{{ t('Trace and transfer product unit') }}</h3>
                      <p class="section-description">
                        {{ t('ProductUnit remains the traceability root; supports transport the unit and racks remain post-line logistics.') }}
                      </p>
                    </div>
                  </div>
                  <div class="mt-5 grid gap-4 xl:grid-cols-[0.9fr_1.1fr]">
                    <div class="grid gap-3">
                      <label class="form-label">{{ t('Product unit') }}
                        <select
                          v-model="transferForm.productUnitId"
                          class="form-input"
                          @change="transferForm.productUnitId && loadUnitTrace(Number(transferForm.productUnitId))"
                        >
                          <option value="">{{ t('Select option') }}</option>
                          <option
                            v-for="unit in units"
                            :key="unit.id"
                            :value="unit.id"
                          >{{ unit.unitCode }} · {{ translateStatus(String(unit.status)) }}</option>
                        </select>
                      </label>
                      <label class="form-label">{{ t('Target section') }}
                        <select
                          v-model="transferForm.toSectionId"
                          class="form-input"
                        >
                          <option value="">{{ t('Select option') }}</option>
                          <option
                            v-for="section in transferTargetSections"
                            :key="section.id"
                            :value="section.id"
                          >{{ section.sectionCode }} · {{ translateSectionName(section.name) }} · {{ lineLabel(section.lineId) }}</option>
                        </select>
                      </label>
                      <label class="form-label">{{ t('Target support') }}
                        <select
                          v-model="transferForm.toSupportId"
                          class="form-input"
                        >
                          <option value="">{{ t('Keep current support') }}</option>
                          <option
                            v-for="support in supports"
                            :key="support.id"
                            :value="support.id"
                          >{{ support.supportCode }} · {{ translateStatus(String(support.status)) }}</option>
                        </select>
                      </label>
                      <label class="form-label">{{ t('Reason') }}<input
                        v-model="transferForm.reason"
                        class="form-input"
                      ></label>
                      <label class="form-label">{{ t('Notes') }}<textarea
                        v-model="transferForm.notes"
                        class="form-input min-h-20"
                      /></label>
                      <label class="flex items-center gap-2 text-sm font-semibold text-slate-700 dark:text-slate-200">
                        <input
                          v-model="transferForm.moveCurrentSupport"
                          type="checkbox"
                          class="h-4 w-4 rounded border-slate-300"
                        >
                        {{ t('Move current support with unit') }}
                      </label>
                      <div class="flex flex-wrap gap-2">
                        <button
                          class="btn-secondary"
                          type="button"
                          :disabled="!selectedTransferUnit"
                          @click="selectedTransferUnit && prepareTransfer(selectedTransferUnit)"
                        >
                          {{ t('Load trace') }}
                        </button>
                        <button
                          class="btn-primary"
                          type="button"
                          :disabled="!can('ProductUnits.Transfer')"
                          @click="submitUnitTransfer"
                        >
                          {{ t('Register transfer') }}
                        </button>
                      </div>
                      <p
                        v-if="transferStatus"
                        class="rounded-lg border border-slate-200 bg-white p-3 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200"
                      >
                        {{ transferStatus }}
                      </p>
                    </div>
                    <div class="table-shell">
                      <table class="data-table">
                        <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('Line') }}</th><th>{{ t('Section') }}</th><th>{{ t('Route state') }}</th><th /></tr></thead>
                        <tbody>
                          <tr
                            v-for="unit in units"
                            :key="unit.id"
                          >
                            <td class="font-bold">
                              {{ unit.unitCode }}
                            </td>
                            <td>{{ lineLabel(sectionsById.get(Number(unit.currentSectionId))?.lineId) }}</td>
                            <td>{{ sectionName(unit.currentSectionId) }}</td>
                            <td><span :class="statusClass(unit.status)">{{ translateStatus(String(unit.status)) }}</span></td>
                            <td>
                              <button
                                class="btn-secondary btn-compact"
                                type="button"
                                @click="prepareTransfer(unit)"
                              >
                                {{ t('Trace') }}
                              </button>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </div>
                </section>

                <section
                  v-if="activeView === 'units' && selectedUnitTrace"
                  class="industrial-panel"
                >
                  <div class="section-heading">
                    <div>
                      <p>{{ t('Product unit trace') }}</p>
                      <h3>{{ selectedUnitTrace.unit?.unitCode || t('Selected unit') }}</h3>
                      <p class="section-description">
                        {{ referenceLabel(selectedUnitTrace.unit?.currentProductionLine) }} · {{ referenceLabel(selectedUnitTrace.unit?.currentSection) }} · {{ t(selectedUnitTrace.unit?.routeState || 'No data available') }}
                      </p>
                    </div>
                  </div>
                  <div class="table-shell compact-table-shell mt-4">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Timestamp') }}</th><th>{{ t('Event') }}</th><th>{{ t('Line') }}</th><th>{{ t('Section') }}</th><th>{{ t('Support') }}</th><th>{{ t('Notes') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="item in selectedTraceTimeline"
                          :key="`${item.eventType}-${item.occurredAt}-${item.sectionCode || ''}`"
                        >
                          <td>{{ formatDate(item.occurredAt) }}</td>
                          <td>
                            <span
                              class="event-chip"
                              :class="statusClass(item.eventType)"
                            >{{ displayOperationalEvent(item.eventType) }}</span>
                          </td>
                          <td>{{ item.lineCode || '-' }}</td>
                          <td>{{ item.sectionCode || '-' }}</td>
                          <td>{{ item.supportCode || '-' }}</td>
                          <td>{{ item.label || item.result || '-' }}</td>
                        </tr>
                        <tr v-if="!selectedTraceTimeline.length">
                          <td
                            colspan="6"
                            class="text-center"
                          >
                            {{ traceLoading ? t('Loading DriveTrace Core data...') : t('No records found') }}
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>

                <section
                  v-if="activeView === 'supports'"
                  class="card overflow-hidden p-5 sm:p-6"
                >
                  <div class="section-heading">
                    <div><p>{{ t('Physical tracking') }}</p><h3>{{ t('Support is the intra-line anchor') }}</h3></div>
                  </div>
                  <img
                    :src="lineDoorUrl"
                    alt="Support-based line"
                    class="mt-4 max-h-[16rem] w-full max-w-full rounded-lg border border-slate-200 bg-slate-50 object-contain p-2 dark:border-slate-700 dark:bg-slate-900/30"
                  >
                </section>

                <section
                  v-if="activeView === 'quality'"
                  class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3"
                >
                  <div class="metric-card">
                    <span>{{ t('Results') }}</span><strong>{{ quality.length }}</strong>
                  </div>
                  <div class="metric-card">
                    <span>{{ t('PASS') }}</span><strong>{{ quality.filter((item) => item.result === 'PASS').length }}</strong>
                  </div>
                  <div class="metric-card">
                    <span>{{ t('FAIL') }}</span><strong>{{ quality.filter((item) => item.result === 'FAIL').length }}</strong>
                  </div>
                </section>

                <section
                  v-if="activeView === 'quality'"
                  class="industrial-panel"
                >
                  <div class="section-heading">
                    <div>
                      <p>Qualidade</p>
                      <h3>Painel de decisão de qualidade</h3>
                      <p class="section-description">
                        Resultados, não conformidades, retrabalho e sucata ficam agregados para o técnico de qualidade.
                      </p>
                    </div>
                  </div>
                  <div class="mt-5 grid gap-3 sm:grid-cols-3">
                    <article
                      v-for="card in roleContextCards"
                      :key="card.key"
                      class="decision-card"
                      :class="card.tone"
                    >
                      <span>{{ card.label }}</span>
                      <strong>{{ card.value }}</strong>
                      <p>{{ card.detail }}</p>
                    </article>
                  </div>
                  <div class="mt-5 grid gap-5 xl:grid-cols-2">
                    <div>
                      <h4 class="text-sm font-black text-slate-950 dark:text-white">
                        Últimos resultados registados
                      </h4>
                      <div class="table-shell">
                        <table class="data-table">
                          <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('Result') }}</th><th>{{ t('Recorded at') }}</th><th>{{ t('Notes') }}</th></tr></thead>
                          <tbody>
                            <tr
                              v-for="record in recentQualityRecords"
                              :key="record.id"
                            >
                              <td class="font-bold">
                                {{ unitCode(record.productUnitId) }}
                              </td>
                              <td><span :class="statusClass(record.result)">{{ translateQualityResult(record.result) }}</span></td>
                              <td>{{ formatDate(record.recordedAt) }}</td>
                              <td
                                class="max-w-[18rem] truncate"
                                :title="record.notes || '-'"
                              >
                                {{ record.notes || '-' }}
                              </td>
                            </tr>
                            <tr v-if="!recentQualityRecords.length">
                              <td
                                colspan="4"
                                class="text-center"
                              >
                                {{ t('No records found') }}
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </div>
                    </div>
                    <div>
                      <h4 class="text-sm font-black text-slate-950 dark:text-white">
                        Não conformidades abertas
                      </h4>
                      <div class="table-shell">
                        <table class="data-table">
                          <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('Severity') }}</th><th>{{ t('Status') }}</th><th>{{ t('Description') }}</th></tr></thead>
                          <tbody>
                            <tr
                              v-for="item in openNonconformities"
                              :key="item.id"
                            >
                              <td class="font-bold">
                                {{ unitCode(item.productUnitId) }}
                              </td>
                              <td><span :class="statusClass(item.severity)">{{ displayStatus(item.severity) }}</span></td>
                              <td><span :class="statusClass(item.status)">{{ displayStatus(item.status) }}</span></td>
                              <td
                                class="max-w-[18rem] truncate"
                                :title="item.description || '-'"
                              >
                                {{ item.description || '-' }}
                              </td>
                            </tr>
                            <tr v-if="!openNonconformities.length">
                              <td
                                colspan="4"
                                class="text-center"
                              >
                                {{ t('No records found') }}
                              </td>
                            </tr>
                          </tbody>
                        </table>
                      </div>
                    </div>
                  </div>
                </section>

                <section
                  v-if="activeView === 'racks'"
                  class="space-y-5 lg:space-y-6"
                >
                  <div class="ops-hero">
                    <div class="grid gap-6 xl:grid-cols-[1fr_0.9fr] xl:items-center">
                      <div class="min-w-0">
                        <p class="text-sm font-bold uppercase tracking-normal text-drivolution-700">
                          {{ t('Post-line logistics') }}
                        </p>
                        <h3 class="mt-2 text-3xl font-black leading-tight tracking-normal text-slate-950 dark:text-white sm:text-4xl">
                          {{ t('Racks / Post-line Logistics') }}
                        </h3>
                        <p class="mt-3 max-w-3xl text-sm leading-7 text-slate-600 dark:text-slate-300 sm:text-base">
                          {{ t('Racks only aggregate supports after the controlled line. The support remains the traceability reference for intra-line WIP.') }}
                        </p>
                        <div class="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                          <article
                            v-for="metric in rackKpis"
                            :key="metric.key"
                            class="kpi-card"
                            :class="metric.tone"
                          >
                            <span>{{ t(metric.label) }}</span>
                            <strong>{{ metric.value }}</strong>
                            <p>{{ t(metric.detail) }}</p>
                          </article>
                        </div>
                      </div>
                      <div class="ops-hero-visual">
                        <img
                          :src="lineCarUrl"
                          alt="Automotive production line"
                          class="max-h-[14rem] w-full rounded-lg bg-white object-contain p-2 dark:bg-slate-900"
                        >
                        <p class="mt-3 text-xs font-bold uppercase tracking-normal text-slate-500 dark:text-slate-400">
                          {{ t('Rack as post-line logistics') }}
                        </p>
                      </div>
                    </div>
                  </div>
                  <div class="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
                    <details class="industrial-panel compact-help">
                      <summary class="section-heading">
                        <div><p>{{ t('Operational meaning') }}</p><h3>{{ t('What this page controls') }}</h3></div>
                      </summary>
                      <ul class="technical-list">
                        <li>{{ t('Racks aggregate supports after the controlled line.') }}</li>
                        <li>{{ t('Primary traceability remains attached to support and product unit.') }}</li>
                        <li>{{ t('Rack-support association is logistical, temporal and auditable.') }}</li>
                      </ul>
                    </details>
                    <section class="industrial-panel">
                      <div class="section-heading">
                        <div><p>{{ t('Operational decision') }}</p><h3>{{ t('Post-line capacity reading') }}</h3></div>
                      </div>
                      <div class="decision-grid mt-4">
                        <article
                          class="decision-card"
                          :class="rackUtilization > 75 ? 'tone-warning' : 'tone-success'"
                        >
                          <span>{{ t('Capacity') }}</span>
                          <strong>{{ rackUtilization }}%</strong>
                          <p>{{ rackUtilization > 75 ? t('Rack occupation is high; validate outbound logistics.') : t('Post-line rack capacity remains available.') }}</p>
                        </article>
                        <article class="decision-card tone-info">
                          <span>{{ t('Active assignments') }}</span>
                          <strong>{{ activeRackAssignments.length }}</strong>
                          <p>{{ t('Use the assignment table to audit rack entry and exit timestamps.') }}</p>
                        </article>
                      </div>
                    </section>
                  </div>
                  <section class="industrial-panel">
                    <div class="section-heading">
                      <div>
                        <p>Logística</p>
                        <h3>Atribuições rack-suporte</h3>
                        <p class="section-description">
                          A logística gere capacidade pós-linha sem substituir a rastreabilidade por suporte e unidade de produto.
                        </p>
                      </div>
                    </div>
                    <div class="mt-5 grid gap-3 sm:grid-cols-3">
                      <article
                        v-for="card in roleContextCards"
                        :key="card.key"
                        class="decision-card"
                        :class="card.tone"
                      >
                        <span>{{ card.label }}</span>
                        <strong>{{ card.value }}</strong>
                        <p>{{ card.detail }}</p>
                      </article>
                    </div>
                    <div class="table-shell mt-5">
                      <table class="data-table">
                        <thead><tr><th>{{ t('Rack') }}</th><th>{{ t('Support') }}</th><th>{{ t('Status') }}</th><th>{{ t('Date/time in') }}</th><th>{{ t('Date/time out') }}</th></tr></thead>
                        <tbody>
                          <tr
                            v-for="assignment in recentRackAssignments"
                            :key="assignment.id"
                          >
                            <td class="font-bold">
                              {{ rackCode(assignment.rackId) }}
                            </td>
                            <td>{{ supportCode(assignment.supportId) }}</td>
                            <td><span :class="statusClass(assignment.dateTimeOut ? 'Completed' : 'Active')">{{ assignment.dateTimeOut ? t('Completed') : t('Active') }}</span></td>
                            <td>{{ formatDate(assignment.dateTimeIn) }}</td>
                            <td>{{ formatDate(assignment.dateTimeOut) }}</td>
                          </tr>
                          <tr v-if="!recentRackAssignments.length">
                            <td
                              colspan="5"
                              class="text-center"
                            >
                              {{ t('No records found') }}
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </section>
                </section>

                <section
                  v-if="activeView === 'parameters' && canManageUsers"
                  class="card p-5 sm:p-6"
                >
                  <div class="section-heading">
                    <div><p>{{ t('Admin') }}</p><h3>{{ t('System Parameters') }}</h3></div>
                  </div>
                  <div class="mt-5 flex flex-wrap gap-2">
                    <button
                      v-for="config in parameterConfigs"
                      :key="config.key"
                      type="button"
                      class="mobile-tab"
                      :class="parameterTab === config.key ? 'mobile-tab-active' : ''"
                      @click="parameterTab = config.key"
                    >
                      {{ t(config.title) }}
                    </button>
                  </div>
                </section>

                <CrudPanel
                  v-for="config in activeCrudConfigs"
                  :key="config.key"
                  :config="crudPanelConfig(config)"
                  :form="crudForms[config.key] || {}"
                  :form-open="Boolean(crudOpen[config.key])"
                  :editing="crudEditingId[config.key] !== null && crudEditingId[config.key] !== undefined"
                  :message="crudMessages[config.key]"
                  :status-class="statusClass"
                  @add="startCrudAdd(config)"
                  @edit="startCrudEdit(config, $event)"
                  @cancel="cancelCrud(config)"
                  @save="saveCrud(config)"
                  @delete="removeCrud(config, $event)"
                  @update-field="(key, value) => updateCrudField(config, key, value)"
                />
              </div>

              <!-- OVERVIEW (DASHBOARD) VIEW -->
              <div
                v-if="activeView === 'overview'"
                class="space-y-5 lg:space-y-6"
              >
                <section class="ops-hero">
                  <div class="grid gap-6 xl:grid-cols-[1.1fr_0.9fr] xl:items-center">
                    <div class="min-w-0">
                      <p class="text-xs font-bold uppercase tracking-normal text-drivolution-700">
                        {{ t('Automotive WIP traceability') }}
                      </p>
                      <h3 class="mt-2 max-w-3xl text-2xl font-black leading-tight tracking-normal sm:text-3xl">
                        {{ t('Line state command overview') }}
                      </h3>
                      <p class="mt-3 max-w-2xl text-sm leading-6 text-slate-600 dark:text-slate-300">
                        {{ t('Line state command overview description') }}
                      </p>
                      <div class="mt-4 flex flex-wrap gap-2">
                        <span class="domain-pill">{{ t('ProductUnit-centred traceability') }}</span>
                        <span class="domain-pill">{{ t('Support as intra-line anchor') }}</span>
                        <span class="domain-pill">{{ t('Rack as post-line logistics') }}</span>
                        <span class="domain-pill">{{ t('Material lot genealogy') }}</span>
                      </div>
                    </div>
                    <div class="ops-hero-visual">
                      <img
                        :src="lineDoorUrl"
                        alt="Door production line"
                        class="max-h-[14rem] w-full max-w-xl justify-self-center rounded-lg bg-white object-contain p-2 dark:bg-slate-900"
                      >
                      <div class="mt-3 grid gap-2 sm:grid-cols-3">
                        <span class="system-pill"><span>{{ t('Highest WIP') }}</span><strong>{{ topWipSection ? translateSectionName(topWipSection.section) : '-' }}</strong></span>
                        <span class="system-pill"><span>{{ t('PASS rate') }}</span><strong>{{ passRate === null ? '-' : `${passRate}%` }}</strong></span>
                        <span class="system-pill"><span>{{ t('Loaded supports') }}</span><strong>{{ loadedSupports.length }}</strong></span>
                      </div>
                    </div>
                  </div>
                </section>
                <section
                  v-if="roleContextCards.length"
                  class="kpi-grid"
                >
                  <article
                    v-for="card in roleContextCards"
                    :key="card.key"
                    class="kpi-card"
                    :class="card.tone"
                  >
                    <span>{{ card.label }}</span>
                    <strong>{{ card.value }}</strong>
                    <p>{{ card.detail }}</p>
                  </article>
                </section>
                <section class="kpi-grid">
                  <article
                    v-for="metric in overviewKpis"
                    :key="metric.key"
                    class="kpi-card"
                    :class="metric.tone"
                  >
                    <span>{{ t(metric.label) }}</span>
                    <strong>{{ metric.value }}</strong>
                    <p>{{ t(metric.detail) }}</p>
                  </article>
                </section>
                <section
                  v-if="flowSummary.lineSummaries.length"
                  class="industrial-panel"
                >
                  <div class="section-heading">
                    <div>
                      <p>{{ t('Flow validation') }}</p>
                      <h3>{{ t('Multi-line flow') }}</h3>
                      <p class="section-description">
                        {{ t('Current ProductUnit WIP grouped by production line and section.') }}
                      </p>
                    </div>
                  </div>
                  <div class="mt-4 grid gap-3 sm:grid-cols-3">
                    <article
                      v-for="metric in flowKpis"
                      :key="metric.key"
                      class="kpi-card"
                      :class="metric.tone"
                    >
                      <span>{{ t(metric.label) }}</span>
                      <strong>{{ metric.value }}</strong>
                      <p>{{ t(metric.detail) }}</p>
                    </article>
                  </div>
                  <div class="flow-filter-row">
                    <select
                      v-model="flowLineFilter"
                      class="compact-select"
                      :aria-label="t('Production line')"
                    >
                      <option
                        v-for="option in flowLineFilterOptions"
                        :key="option.value"
                        :value="option.value"
                      >
                        {{ option.label }}
                      </option>
                    </select>
                    <button
                      class="mobile-tab"
                      :class="flowTransferFilter === 'all' ? 'mobile-tab-active' : ''"
                      type="button"
                      @click="flowTransferFilter = 'all'"
                    >
                      {{ t('All movements') }}
                    </button>
                    <button
                      class="mobile-tab"
                      :class="flowTransferFilter === 'transfers' ? 'mobile-tab-active' : ''"
                      type="button"
                      @click="flowTransferFilter = 'transfers'"
                    >
                      {{ t('Only transfers') }}
                    </button>
                    <button
                      class="mobile-tab"
                      :class="flowTransferFilter === 'attention' ? 'mobile-tab-active' : ''"
                      type="button"
                      @click="flowTransferFilter = 'attention'"
                    >
                      {{ t('Units in attention') }}
                    </button>
                  </div>
                  <div class="compact-card-list mt-4 grid gap-3 xl:grid-cols-2">
                    <div
                      v-for="line in visibleFlowLineSummaries"
                      :key="line.productionLineId"
                      class="line-step flex-col items-stretch"
                    >
                      <div class="flex items-center justify-between gap-3">
                        <div class="min-w-0">
                          <p class="font-bold text-slate-950 dark:text-slate-50">
                            {{ line.lineCode }} · {{ line.name }}
                          </p>
                          <p class="text-xs uppercase tracking-normal text-slate-500 dark:text-slate-400">
                            {{ line.activeSupports }} {{ t('supports') }} · {{ line.blockedUnits }} {{ t('attention') }}
                          </p>
                        </div>
                        <span class="text-sm font-black text-slate-950 dark:text-slate-50">{{ line.wipUnits }}</span>
                      </div>
                      <div class="mt-3 grid gap-2">
                        <div
                          v-for="section in line.sections"
                          :key="section.sectionId"
                          class="flex items-center gap-3"
                        >
                          <span class="min-w-0 flex-1 truncate text-xs font-semibold text-slate-600 dark:text-slate-300">{{ section.sectionCode }} · {{ translateSectionName(section.name) }}</span>
                          <div class="h-2 w-24 rounded-full bg-slate-200 dark:bg-slate-700">
                            <div
                              class="h-2 rounded-full bg-drivolution-500"
                              :style="{ width: `${Math.min(100, section.wipUnits * 34)}%` }"
                            />
                          </div>
                          <strong class="w-6 text-right text-xs">{{ section.wipUnits }}</strong>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="mt-4 rounded-lg border border-slate-200 bg-white/70 p-3 dark:border-slate-800 dark:bg-slate-950/35">
                    <div class="flex flex-wrap items-center justify-between gap-2">
                      <div>
                        <p class="text-xs font-black uppercase tracking-normal text-drivolution-700 dark:text-drivolution-300">
                          {{ t('Latest transfers between lines') }}
                        </p>
                        <h4 class="text-base font-black text-slate-950 dark:text-white">
                          {{ t('ProductUnit movement audit') }}
                        </h4>
                      </div>
                      <span class="text-xs font-bold text-slate-500 dark:text-slate-400">{{ filteredFlowTransfers.length }} {{ t('records') }}</span>
                    </div>
                    <div class="flow-list">
                      <article
                        v-for="movement in filteredFlowTransfers"
                        :key="movement.id"
                        class="flow-transfer-row"
                      >
                        <p
                          class="flow-transfer-main"
                          :title="movementTitle(movement)"
                        >
                          {{ movement.unit?.code || '-' }} · {{ movementText(movement.fromProductionLine) }} / {{ movementText(movement.fromSection) }} -> {{ movementText(movement.toProductionLine) }} / {{ movementText(movement.toSection) }}
                        </p>
                        <div class="flow-transfer-meta">
                          <span :class="statusClass(movement.eventType)">{{ movementBadge(movement) }}</span>
                          <span>{{ formatShortTime(movement.occurredAt) }}</span>
                          <span
                            class="max-w-[12rem] truncate"
                            :title="movement.reason || t('No reason recorded')"
                          >{{ movement.reason || t('No reason recorded') }}</span>
                        </div>
                      </article>
                      <p
                        v-if="!filteredFlowTransfers.length"
                        class="empty-state"
                      >
                        <strong>{{ t('No transfer movements found') }}</strong>
                      </p>
                    </div>
                  </div>
                </section>
                <section class="decision-grid">
                  <article
                    class="decision-card"
                    :class="blockedUnits.length ? 'tone-warning' : 'tone-success'"
                  >
                    <span>{{ t('Immediate attention') }}</span>
                    <strong>{{ blockedUnits.length ? `${blockedUnits.length} ${t('units requiring attention')}` : t('No blocked units right now.') }}</strong>
                    <p>{{ blockedUnits.length ? t('Prioritize containment before releasing more WIP.') : t('No immediate containment action is required from current data.') }}</p>
                  </article>
                  <article class="decision-card tone-info">
                    <span>{{ t('Flow WIP') }}</span>
                    <strong>{{ topWipSection ? `${translateSectionName(topWipSection.section)} · ${topWipSection.productUnits}` : t('No data available') }}</strong>
                    <p>{{ t('Validate capacity and exit rhythm at the highest WIP section.') }}</p>
                  </article>
                  <article
                    class="decision-card"
                    :class="fiwareCoherenceOk ? 'tone-success' : 'tone-warning'"
                  >
                    <span>{{ t('Context coherence') }}</span>
                    <strong>{{ fiwareCoherenceOk ? t('Context coherence confirmed') : t('Context review recommended') }}</strong>
                    <p>{{ t('Compare relational snapshot and Orion-LD hot context before demonstrations.') }}</p>
                  </article>
                </section>
                <section class="grid gap-5 2xl:grid-cols-[1.1fr_0.9fr]">
                  <div class="industrial-panel">
                    <div class="section-heading">
                      <div>
                        <p>{{ t('Line state') }}</p>
                        <h3>{{ t('Current WIP by section') }}</h3>
                      </div>
                    </div>
                    <div class="line-state-list mt-4">
                      <div
                        v-for="section in summary.wipBySection"
                        :key="section.sectionCode"
                        class="line-step line-state-row"
                      >
                        <div class="min-w-0">
                          <p class="font-bold text-slate-950 dark:text-slate-50">
                            {{ translateSectionName(section.section) }}
                          </p>
                          <p class="text-xs uppercase tracking-normal text-slate-500 dark:text-slate-400">
                            {{ section.sectionCode }} · {{ translateSectionName(section.sectionType) }}
                          </p>
                        </div>
                        <div class="flex w-full items-center gap-3 sm:min-w-40">
                          <div class="h-1.5 flex-1 rounded-full bg-slate-200 dark:bg-slate-700">
                            <div
                              class="h-1.5 rounded-full bg-drivolution-500"
                              :style="{ width: `${Math.min(100, section.productUnits * 28)}%` }"
                            />
                          </div>
                          <span class="text-sm font-black text-slate-950 dark:text-slate-50">{{ section.productUnits }}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div class="industrial-panel">
                    <div class="section-heading">
                      <div>
                        <p>{{ t('Quality') }}</p>
                        <h3>{{ t('Open alerts and deviations') }}</h3>
                      </div>
                    </div>
                    <div class="compact-card-list mt-5 space-y-3">
                      <div
                        v-for="alert in summary.qualityAlerts"
                        :key="`${alert.unitCode}-${alert.createdAt}`"
                        class="alert-card"
                      >
                        <div class="flex items-center justify-between gap-3">
                          <strong>{{ alert.unitCode }}</strong>
                          <span :class="statusClass(alert.status)">{{ displayStatus(alert.status) }}</span>
                        </div>
                        <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">
                          {{ displayDemoText(alert.description) }}
                        </p>
                        <p class="mt-2 text-xs font-semibold uppercase tracking-normal text-slate-400 dark:text-slate-500">
                          {{ displayStatus(alert.severity) }}
                        </p>
                      </div>
                      <p
                        v-if="!summary.qualityAlerts.length"
                        class="rounded-lg border border-emerald-200 bg-emerald-50 p-4 text-sm font-semibold text-emerald-800 dark:border-emerald-700/70 dark:bg-emerald-900/30 dark:text-emerald-100"
                      >
                        {{ t('No blocked units right now.') }}
                      </p>
                    </div>
                  </div>
                </section>
                <section class="industrial-panel">
                  <div class="section-heading">
                    <div>
                      <p>{{ t('Recent events') }}</p>
                      <h3>{{ t('Operational event log') }}</h3>
                    </div>
                  </div>
                  <div class="table-shell compact-table-shell">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Event') }}</th><th>{{ t('Target') }}</th><th>{{ t('Location') }}</th><th>{{ t('Timestamp') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="event in recentOperationalEvents.slice(0, 8)"
                          :key="event.eventCode"
                        >
                          <td>
                            <span
                              class="event-chip"
                              :class="statusClass(event.eventType)"
                            >{{ displayOperationalEvent(event.eventType) }}</span>
                          </td>
                          <td>{{ operationalEventTarget(event) }}</td>
                          <td>{{ operationalEventLocation(event) }}</td>
                          <td>{{ formatDate(event.occurredAt) }}</td>
                        </tr>
                        <tr v-if="!recentOperationalEvents.length">
                          <td
                            colspan="4"
                            class="text-center"
                          >
                            {{ t('No records found') }}
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>

              <!-- OPERATOR WORKBENCH VIEW -->
              <div
                v-if="activeView === 'operator'"
                class="space-y-5 lg:space-y-6"
              >
                <section class="ops-hero">
                  <div class="section-heading">
                    <div>
                      <p>{{ t('Operator workbench') }}</p>
                      <h3>{{ t('Shift execution queues') }}</h3>
                      <p class="section-description">
                        {{ t('Actionable ProductUnit queues from the current production flow.') }}
                      </p>
                    </div>
                    <button
                      class="btn-primary"
                      type="button"
                      @click="loadData(false)"
                    >
                      {{ t('Refresh') }}
                    </button>
                  </div>
                  <div
                    v-if="roleContextCards.length"
                    class="mt-5 grid gap-3 sm:grid-cols-3"
                  >
                    <article
                      v-for="card in roleContextCards"
                      :key="card.key"
                      class="kpi-card"
                      :class="card.tone"
                    >
                      <span>{{ card.label }}</span>
                      <strong>{{ card.value }}</strong>
                      <p>{{ card.detail }}</p>
                    </article>
                  </div>
                  <div class="kpi-grid mt-5">
                    <article class="kpi-card tone-info">
                      <span>{{ t('Transfer ready') }}</span><strong>{{ operatorWorkbench.queues.transferReady }}</strong><p>{{ t('Units at transfer-capable sections') }}</p>
                    </article>
                    <article
                      class="kpi-card"
                      :class="operatorWorkbench.queues.blocked ? 'tone-warning' : 'tone-success'"
                    >
                      <span>{{ t('Blocked') }}</span><strong>{{ operatorWorkbench.queues.blocked }}</strong><p>{{ t('Units requiring attention') }}</p>
                    </article>
                    <article class="kpi-card tone-muted">
                      <span>{{ t('Rework') }}</span><strong>{{ operatorWorkbench.queues.rework }}</strong><p>{{ t('Units currently in rework') }}</p>
                    </article>
                    <article
                      class="kpi-card"
                      :class="operatorWorkbench.queues.noSupport ? 'tone-warning' : 'tone-success'"
                    >
                      <span>{{ t('No support') }}</span><strong>{{ operatorWorkbench.queues.noSupport }}</strong><p>{{ t('Units without active transport support') }}</p>
                    </article>
                  </div>
                </section>

                <section class="grid gap-5 xl:grid-cols-[1.2fr_0.8fr]">
                  <div class="industrial-panel">
                    <div class="section-heading">
                      <div><p>{{ t('Execution') }}</p><h3>{{ t('Active ProductUnit queue') }}</h3></div>
                    </div>
                    <div class="table-shell mt-4">
                      <table class="data-table">
                        <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('Line') }}</th><th>{{ t('Section') }}</th><th>{{ t('Status') }}</th><th>{{ t('Route state') }}</th><th /></tr></thead>
                        <tbody>
                          <tr
                            v-for="unit in operatorWorkbench.units"
                            :key="unit.id"
                          >
                            <td class="font-bold">
                              {{ unit.unitCode }}
                            </td>
                            <td>{{ referenceLabel(unit.currentProductionLine) }}</td>
                            <td>{{ referenceLabel(unit.currentSection) }}</td>
                            <td><span :class="statusClass(unit.requiresAttention ? 'Blocked' : unit.status)">{{ displayStatus(unit.status) }}</span></td>
                            <td>{{ t(unit.routeState) }}</td>
                            <td>
                              <button
                                class="btn-secondary btn-compact"
                                type="button"
                                @click="prepareTransfer(unitsById.get(unit.id) || { id: unit.id, unitCode: unit.unitCode, unitType: 'Subproduto', status: unit.status, qualityStatus: unit.qualityStatus, manufacturingOrderId: 0 })"
                              >
                                {{ unit.canTransfer ? t('Transfer') : t('Trace') }}
                              </button>
                            </td>
                          </tr>
                          <tr v-if="!operatorWorkbench.units.length">
                            <td
                              colspan="6"
                              class="text-center"
                            >
                              {{ t('No operational records are currently available for this table.') }}
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </div>
                  <div class="industrial-panel">
                    <div class="section-heading">
                      <div><p>{{ t('Transfer targets') }}</p><h3>{{ t('Available target sections') }}</h3></div>
                    </div>
                    <div class="compact-card-list mt-4 grid gap-3">
                      <article
                        v-for="target in operatorWorkbench.transferTargets"
                        :key="target.sectionId"
                        class="decision-card tone-info"
                      >
                        <span>{{ referenceLabel(target.productionLine) }}</span>
                        <strong>{{ target.sectionCode }}</strong>
                        <p>{{ translateSectionName(target.name) }} · {{ target.currentWip }} {{ t('units') }}</p>
                      </article>
                      <p
                        v-if="!operatorWorkbench.transferTargets.length"
                        class="empty-state"
                      >
                        <strong>{{ t('No records found') }}</strong>
                      </p>
                    </div>
                  </div>
                </section>

                <section class="industrial-panel">
                  <div class="section-heading">
                    <div><p>{{ t('Recent transfers') }}</p><h3>{{ t('ProductUnit movement audit') }}</h3></div>
                  </div>
                  <div class="table-shell compact-table-shell mt-4">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('From') }}</th><th>{{ t('To') }}</th><th>{{ t('Event') }}</th><th>{{ t('Timestamp') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="movement in operatorWorkbench.recentTransfers"
                          :key="movement.id"
                        >
                          <td>{{ movement.unit?.code || '-' }}</td>
                          <td>{{ referenceLabel(movement.fromSection) }}</td>
                          <td>{{ referenceLabel(movement.toSection) }}</td>
                          <td>{{ displayOperationalEvent(movement.eventType) }}</td>
                          <td>{{ formatDate(movement.occurredAt) }}</td>
                        </tr>
                        <tr v-if="!operatorWorkbench.recentTransfers.length">
                          <td
                            colspan="5"
                            class="text-center"
                          >
                            {{ t('No records found') }}
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>

              <!-- CUSTOMER ORDERS VIEW -->
              <div
                v-if="activeView === 'customerOrders'"
                class="space-y-5 lg:space-y-6"
              >
                <section class="kpi-grid">
                  <article class="kpi-card tone-info">
                    <span>{{ t('Encomendas ativas') }}</span><strong>{{ customerOrderSummary.active }}</strong><p>{{ t('A decorrer ou em preparação') }}</p>
                  </article>
                  <article class="kpi-card tone-info">
                    <span>{{ t('Em produção') }}</span><strong>{{ customerOrderSummary.production }}</strong><p>{{ t('Em fabrico neste momento') }}</p>
                  </article>
                  <article class="kpi-card tone-success">
                    <span>{{ t('Prontas') }}</span><strong>{{ customerOrderSummary.ready }}</strong><p>{{ t('Preparadas para levantamento') }}</p>
                  </article>
                  <article class="kpi-card tone-muted">
                    <span>{{ t('Concluídas') }}</span><strong>{{ customerOrderSummary.completed }}</strong><p>{{ t('Entregues ou finalizadas') }}</p>
                  </article>
                </section>

                <section class="industrial-panel customer-toolbar">
                  <div class="customer-toolbar-main">
                    <label class="customer-search-field">
                      <span>{{ t('Pesquisar') }}</span>
                      <input
                        v-model="customerOrderSearch"
                        class="form-input"
                        :placeholder="t('Pesquisar por nome ou código de rastreio')"
                      >
                    </label>
                    <div class="mobile-tabs">
                      <button
                        v-for="filter in customerOrderFilters"
                        :key="filter.key"
                        class="mobile-tab"
                        :class="customerOrderFilter === filter.key ? 'mobile-tab-active' : ''"
                        type="button"
                        @click="customerOrderFilter = filter.key"
                      >
                        {{ filter.label }} <span class="ml-1 opacity-70">{{ filter.count }}</span>
                      </button>
                    </div>
                  </div>
                  <div class="flex flex-wrap gap-3">
                    <button
                      class="btn-primary"
                      type="button"
                      @click="navigateTo('customerNewOrder')"
                    >
                      {{ t('Nova encomenda') }}
                    </button>
                    <button
                      class="btn-secondary"
                      type="button"
                      @click="loadData(false)"
                    >
                      {{ t('Atualizar') }}
                    </button>
                  </div>
                </section>

                <section
                  v-if="!customerOrders.length"
                  class="customer-empty-state"
                >
                  <strong>{{ t('Ainda não existem encomendas.') }}</strong>
                  <p>{{ t('Crie uma nova encomenda para começar.') }}</p>
                  <button
                    class="btn-primary"
                    type="button"
                    @click="navigateTo('customerNewOrder')"
                  >
                    {{ t('Nova encomenda') }}
                  </button>
                </section>

                <section
                  v-else-if="!filteredCustomerOrders.length"
                  class="customer-empty-state"
                >
                  <strong>{{ t('Nenhuma encomenda encontrada com estes filtros.') }}</strong>
                  <p>{{ t('Ajuste a pesquisa ou escolha outro filtro.') }}</p>
                </section>

                <section
                  v-else
                  class="grid gap-4 xl:grid-cols-2"
                >
                  <article
                    v-for="order in filteredCustomerOrders"
                    :key="order.publicTrackingCode"
                    class="customer-order-card"
                    :class="customerOrderTone(order)"
                  >
                    <div class="flex flex-wrap items-start justify-between gap-3">
                      <p class="customer-card-code">
                        <span>{{ t('Código de rastreio') }}:</span>
                        <strong>{{ order.publicTrackingCode || '-' }}</strong>
                      </p>
                      <span class="customer-state-chip">{{ customerOrderState(order) }}</span>
                    </div>
                    <h4 class="customer-order-name">
                      {{ customerOrderName(order) }}
                    </h4>
                    <p class="mt-1 text-sm font-bold text-slate-800 dark:text-slate-100">
                      {{ order.order?.product || '-' }}<span v-if="order.order?.variant"> · {{ order.order.variant }}</span>
                    </p>
                    <p class="mt-1 text-sm text-slate-600 dark:text-slate-300">
                      {{ t('Data prevista') }}: {{ formatDate(order.order?.scheduledUntil) }}
                    </p>
                    <div class="mt-4">
                      <div class="flex items-center justify-between gap-3 text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">
                        <span>{{ customerOrderState(order) }}</span>
                        <span>{{ customerProgressPercent(order) }}%</span>
                      </div>
                      <div class="mt-2 h-2 rounded-full bg-slate-200 dark:bg-slate-700">
                        <div
                          class="h-2 rounded-full bg-drivolution-500"
                          :style="{ width: `${customerProgressPercent(order)}%` }"
                        />
                      </div>
                    </div>
                    <p class="mt-3 text-sm font-semibold text-slate-600 dark:text-slate-300">
                      {{ customerNextStep(order) }}
                    </p>
                    <button
                      class="btn-secondary mt-4"
                      type="button"
                      @click="openCustomerOrderDetail(order.publicTrackingCode)"
                    >
                      {{ t('Ver detalhe') }}
                    </button>
                  </article>
                </section>

                <section
                  v-if="customerLookup"
                  class="customer-detail-panel"
                >
                  <div class="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <p class="text-xs font-black uppercase tracking-normal text-drivolution-700 dark:text-drivolution-300">
                        {{ t('Detalhe da encomenda') }}
                      </p>
                      <h3 class="mt-1 text-xl font-black text-slate-950 dark:text-white">
                        {{ customerOrderName(customerLookup) }}
                      </h3>
                      <p class="mt-1 text-sm font-bold text-slate-700 dark:text-slate-200">
                        {{ customerLookup.order?.product || '-' }}<span v-if="customerLookup.order?.variant"> · {{ customerLookup.order.variant }}</span>
                      </p>
                      <p class="mt-1 text-sm font-semibold text-slate-600 dark:text-slate-300">
                        {{ t('Código de rastreio') }}: {{ customerLookup.publicTrackingCode }}
                      </p>
                    </div>
                    <span class="customer-state-chip">{{ customerOrderState(customerLookup) }}</span>
                  </div>
                  <div class="customer-detail-progress">
                    <article class="customer-status-card">
                      <span>{{ t('Estado da encomenda') }}</span>
                      <strong>{{ customerOrderState(customerLookup) }}</strong>
                      <p>{{ customerNextStep(customerLookup) }}</p>
                      <div class="mt-4 h-2 rounded-full bg-slate-200 dark:bg-slate-700">
                        <div
                          class="h-2 rounded-full bg-drivolution-500"
                          :style="{ width: `${customerProgressPercent(customerLookup)}%` }"
                        />
                      </div>
                    </article>
                    <div class="customer-progress-steps">
                      <div
                        v-for="step in customerProgressSteps(customerLookup)"
                        :key="step.key"
                        class="customer-progress-step"
                        :class="`customer-progress-step-${step.state}`"
                      >
                        <span />
                        <p>{{ step.label }}</p>
                      </div>
                    </div>
                  </div>
                  <div class="mt-5 grid gap-4 md:grid-cols-3">
                    <article class="customer-info-tile">
                      <span>{{ t('Estado atual') }}</span><strong>{{ customerOrderState(customerLookup) }}</strong>
                    </article>
                    <article class="customer-info-tile">
                      <span>{{ t('Data prevista') }}</span><strong>{{ formatDate(customerLookup.order?.scheduledUntil) }}</strong>
                    </article>
                    <article class="customer-info-tile">
                      <span>{{ t('Atualização mais recente') }}</span><strong>{{ customerLatestUpdate(customerLookup) }}</strong>
                    </article>
                  </div>
                  <div class="mt-5">
                    <p class="text-xs font-black uppercase tracking-normal text-slate-500 dark:text-slate-400">
                      {{ t('Histórico resumido') }}
                    </p>
                    <ol class="customer-history-list mt-3">
                      <li
                        v-for="milestone in (customerLookup.milestones || []).slice(-4)"
                        :key="`${milestone.occurredAt}-${milestone.eventType}`"
                      >
                        <span>{{ formatDate(milestone.occurredAt) }}</span>
                        <strong>{{ customerHistoryLabel(milestone) }}</strong>
                      </li>
                      <li v-if="!customerLookup.milestones?.length">
                        <span>{{ t('Pedido recebido') }}</span>
                        <strong>{{ t('A encomenda foi registada.') }}</strong>
                      </li>
                    </ol>
                  </div>
                </section>
              </div>

              <!-- NEW CUSTOMER ORDER VIEW -->
              <div
                v-if="activeView === 'customerNewOrder'"
                class="space-y-5 lg:space-y-6"
              >
                <section
                  v-if="customerCreatedOrder"
                  class="customer-confirmation"
                >
                  <span>{{ t('Encomenda criada com sucesso') }}</span>
                  <h3>{{ customerOrderName(customerCreatedOrder) }}</h3>
                  <strong>{{ t('Código de rastreio') }}: {{ customerCreatedOrder.publicTrackingCode }}</strong>
                  <p>{{ t('A encomenda será registada como pedido recebido.') }}</p>
                  <div class="mt-5 flex flex-wrap justify-center gap-3">
                    <button
                      class="btn-primary"
                      type="button"
                      @click="openCustomerOrderDetail(customerCreatedOrder.publicTrackingCode); activeView = 'customerOrders'"
                    >
                      {{ t('Ver encomenda') }}
                    </button>
                    <button
                      class="btn-secondary"
                      type="button"
                      @click="resetCustomerOrderForm"
                    >
                      {{ t('Criar nova encomenda') }}
                    </button>
                    <button
                      class="btn-secondary"
                      type="button"
                      @click="navigateTo('customerOrders')"
                    >
                      {{ t('As minhas encomendas') }}
                    </button>
                  </div>
                </section>
                <section
                  v-else
                  class="industrial-panel customer-order-single-card"
                >
                  <form
                    class="customer-order-form"
                    @submit.prevent="submitCustomerOrder"
                  >
                    <label class="form-label">{{ t('Nome da encomenda') }}
                      <input
                        v-model="customerOrderForm.name"
                        class="form-input"
                        :placeholder="t('Ex.: Porta esquerda lote junho')"
                      >
                    </label>
                    <div class="grid gap-4 md:grid-cols-2">
                      <label class="form-label">{{ t('Produto') }}
                        <select
                          v-model="customerOrderForm.productId"
                          class="form-input"
                        >
                          <option value="">{{ t('Selecione produto') }}</option>
                          <option
                            v-for="product in customerProductOptions"
                            :key="product.id"
                            :value="product.id"
                          >{{ translateMaterialName(product.name) }}</option>
                        </select>
                      </label>
                      <label class="form-label">{{ t('Variante') }}
                        <select
                          v-model="customerOrderForm.variantId"
                          class="form-input"
                        >
                          <option value="">{{ t('Selecione variante') }}</option>
                          <option
                            v-for="variant in customerVariantOptions"
                            :key="variant.id"
                            :value="variant.id"
                          >{{ variant.name }}</option>
                        </select>
                      </label>
                      <label class="form-label">{{ t('Quantidade') }}<input
                        v-model.number="customerOrderForm.quantity"
                        min="1"
                        max="99"
                        type="number"
                        class="form-input"
                      ></label>
                    </div>
                    <label class="form-label">{{ t('Observações para a encomenda') }}<textarea
                      v-model="customerOrderForm.observations"
                      class="form-input min-h-28"
                    /></label>
                    <div class="customer-order-summary-table">
                      <div>
                        <span>{{ t('Resumo da encomenda') }}</span>
                        <strong>{{ customerOrderDisplayName }}</strong>
                      </div>
                      <table>
                        <tbody>
                          <tr><th>{{ t('Nome') }}</th><td>{{ customerOrderDisplayName }}</td></tr>
                          <tr><th>{{ t('Produto') }}</th><td>{{ selectedCustomerProduct ? translateMaterialName(selectedCustomerProduct.name) : t('Por escolher') }}</td></tr>
                          <tr><th>{{ t('Variante') }}</th><td>{{ selectedCustomerVariant?.name || t('Por escolher') }}</td></tr>
                          <tr><th>{{ t('Quantidade') }}</th><td>{{ customerOrderForm.quantity || 0 }}</td></tr>
                          <tr><th>{{ t('Estado inicial') }}</th><td>{{ t('Pedido recebido') }}</td></tr>
                        </tbody>
                      </table>
                    </div>
                    <p
                      v-if="customerOrderStatus"
                      class="rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm font-semibold text-amber-800 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-100"
                    >
                      {{ customerOrderStatus }}
                    </p>
                    <div class="customer-order-submit-row">
                      <p>{{ t('A encomenda será registada como pedido recebido.') }}</p>
                      <button
                        class="btn-primary"
                        type="submit"
                      >
                        {{ t('Submeter encomenda') }}
                      </button>
                    </div>
                  </form>
                </section>
              </div>

              <!-- CUSTOMER LOOKUP VIEW -->
              <div
                v-if="activeView === 'customer'"
                class="space-y-5 lg:space-y-6"
              >
                <section class="ops-hero">
                  <div class="section-heading">
                    <div>
                      <p>{{ t('Customer tracking') }}</p>
                      <h3>{{ t('Customer order lookup') }}</h3>
                      <p class="section-description">
                        {{ t('Public tracking view for manufacturing order progress and unit milestones.') }}
                      </p>
                    </div>
                  </div>
                  <form
                    class="mt-5 flex flex-col gap-3 sm:flex-row"
                    @submit.prevent="lookupCustomerOrder"
                  >
                    <label class="form-label flex-1">{{ t('Public tracking code') }}<input
                      v-model="customerLookupCode"
                      class="form-input"
                    ></label>
                    <button
                      class="btn-primary self-end"
                      type="submit"
                    >
                      {{ t('Search') }}
                    </button>
                  </form>
                  <div
                    v-if="roleContextCards.length"
                    class="mt-5 grid gap-3 sm:grid-cols-3"
                  >
                    <article
                      v-for="card in roleContextCards"
                      :key="card.key"
                      class="kpi-card"
                      :class="card.tone"
                    >
                      <span>{{ card.label }}</span>
                      <strong>{{ card.value }}</strong>
                      <p>{{ card.detail }}</p>
                    </article>
                  </div>
                  <p
                    v-if="customerLookupStatus"
                    class="mt-4 rounded-lg border border-slate-200 bg-white p-3 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200"
                  >
                    {{ customerLookupStatus }}
                  </p>
                </section>

                <template v-if="customerLookup">
                  <section class="kpi-grid">
                    <article class="kpi-card tone-info">
                      <span>{{ t('Código público de rastreio') }}</span><strong>{{ customerLookup.publicTrackingCode || '-' }}</strong><p>{{ customerLookup.customer?.name || '-' }}</p>
                    </article>
                    <article class="kpi-card tone-muted">
                      <span>{{ t('Status') }}</span><strong>{{ translateStatus(customerLookup.order?.status || '-') }}</strong><p>{{ customerLookup.publicTrackingCode }}</p>
                    </article>
                    <article class="kpi-card tone-info">
                      <span>{{ t('Units') }}</span><strong>{{ customerLookup.summary?.units ?? 0 }}</strong><p>{{ customerLookup.summary?.inFlow ?? 0 }} {{ t('in flow') }}</p>
                    </article>
                    <article
                      class="kpi-card"
                      :class="customerLookup.summary?.attention ? 'tone-warning' : 'tone-success'"
                    >
                      <span>{{ t('Attention') }}</span><strong>{{ customerLookup.summary?.attention ?? 0 }}</strong><p>{{ t('Customer-visible quality and route state') }}</p>
                    </article>
                  </section>

                  <section class="industrial-panel">
                    <div class="section-heading">
                      <div><p>{{ t('Unit progress') }}</p><h3>{{ t('ProductUnit customer status') }}</h3></div>
                    </div>
                    <div class="table-shell mt-4">
                      <table class="data-table">
                        <thead><tr><th>{{ t('Stage') }}</th><th>{{ t('Status') }}</th><th>{{ t('Quality') }}</th><th>{{ t('Last movement') }}</th></tr></thead>
                        <tbody>
                          <tr
                            v-for="(unit, index) in customerLookup.units || []"
                            :key="`${customerLookup.publicTrackingCode}-${index}`"
                          >
                            <td>{{ unit.currentStage || '-' }}</td>
                            <td><span :class="statusClass(unit.status)">{{ displayStatus(unit.customerState || unit.status) }}</span></td>
                            <td>{{ unit.qualityStatus || '-' }}</td>
                            <td>{{ formatDate(unit.lastMovementAt) }}</td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </section>

                  <section class="industrial-panel">
                    <div class="section-heading">
                      <div><p>{{ t('Milestones') }}</p><h3>{{ t('Customer movement timeline') }}</h3></div>
                    </div>
                    <div class="table-shell mt-4">
                      <table class="data-table">
                        <thead><tr><th>{{ t('Timestamp') }}</th><th>{{ t('Último marco') }}</th><th>{{ t('Stage') }}</th></tr></thead>
                        <tbody>
                          <tr
                            v-for="milestone in customerLookup.milestones || []"
                            :key="`${milestone.occurredAt}-${milestone.eventType}`"
                          >
                            <td>{{ formatDate(milestone.occurredAt) }}</td>
                            <td>{{ milestone.eventType }}</td>
                            <td>{{ milestone.stage || '-' }}</td>
                          </tr>
                          <tr v-if="!customerLookup.milestones?.length">
                            <td
                              colspan="3"
                              class="text-center"
                            >
                              {{ t('No records found') }}
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </section>
                </template>
              </div>

              <!-- ORDERS VIEW -->
              <div
                v-if="false && activeView === 'orders'"
                class="card p-6"
              >
                <div class="section-heading">
                  <div><p>{{ t('Planning') }}</p><h3>{{ t('Manufacturing orders') }}</h3></div>
                </div>
                <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Order') }}</th><th>{{ t('Status') }}</th><th>{{ t('Planned qty') }}</th><th>{{ t('Scheduled until') }}</th><th>{{ t('Notes') }}</th></tr></thead>
                    <tbody>
                      <tr
                        v-for="order in orders"
                        :key="order.id"
                      >
                        <td class="font-bold">
                          {{ order.orderNumber }}
                        </td>
                        <td><span :class="statusClass(order.status)">{{ translateStatus(order.status) }}</span></td>
                        <td>{{ order.plannedQty }}</td>
                        <td>{{ formatDate(order.scheduledUntil) }}</td>
                        <td>{{ order.observations }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <!-- UNITS VIEW -->
              <div
                v-if="false && activeView === 'units'"
                class="space-y-6"
              >
                <section class="grid gap-4 md:grid-cols-3">
                  <div class="metric-card">
                    <span>{{ t('Traceable units') }}</span><strong>{{ units.length }}</strong>
                  </div>
                  <div class="metric-card">
                    <span>{{ t('Active / held') }}</span><strong>{{ activeUnits.length }}</strong>
                  </div>
                  <div class="metric-card">
                    <span>{{ t('Deviations') }}</span><strong>{{ blockedUnits.length }}</strong>
                  </div>
                </section>
                <section class="card p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Unitary traceability') }}</p><h3>{{ t('Product units and subproducts') }}</h3></div>
                  </div>
                  <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('Type') }}</th><th>{{ t('Status') }}</th><th>{{ t('Quality') }}</th><th>{{ t('Current support') }}</th><th>{{ t('Current section') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="unit in units"
                          :key="unit.id"
                        >
                          <td class="font-bold">
                            {{ unit.unitCode }}
                          </td>
                          <td>{{ translateUnitType(unit.unitType) }}</td>
                          <td><span :class="statusClass(unit.status)">{{ displayStatus(unit.status) }}</span></td>
                          <td><span :class="statusClass(unit.qualityStatus)">{{ translateQualityResult(unit.qualityStatus) }}</span></td>
                          <td>{{ supportCode(unit.currentSupportId) }}</td>
                          <td>{{ sectionName(unit.currentSectionId) }}</td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>

              <!-- SUPPORTS VIEW -->
              <div
                v-if="false && activeView === 'supports'"
                class="space-y-6"
              >
                <section class="card overflow-hidden p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Physical tracking') }}</p><h3>{{ t('Support is the intra-line anchor') }}</h3></div>
                  </div>
                  <img
                    :src="lineDoorUrl"
                    alt="Support-based line"
                    class="mt-5 rounded-lg border border-slate-200 dark:border-slate-700"
                  >
                </section>
                <section class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                  <div
                    v-for="support in supports"
                    :key="support.id"
                    class="card p-5"
                  >
                    <div class="flex items-start justify-between gap-4">
                      <div>
                        <p class="text-sm font-bold uppercase tracking-normal text-slate-500 dark:text-slate-400">
                          {{ t('Support') }}
                        </p><h3 class="mt-1 text-xl font-black">
                          {{ support.supportCode }}
                        </h3>
                      </div>
                      <span :class="statusClass(support.status)">{{ translateStatus(support.status) }}</span>
                    </div>
                    <p class="mt-4 text-sm text-slate-600 dark:text-slate-300">
                      {{ t('Current section') }}
                    </p>
                    <p class="text-base font-bold text-slate-950 dark:text-slate-50">
                      {{ sectionName(support.currentSectionId) }}
                    </p>
                  </div>
                </section>
              </div>

              <!-- MATERIALS VIEW -->
              <div
                v-if="false && activeView === 'materials'"
                class="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]"
              >
                <section class="card p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Items and lots') }}</p><h3>{{ t('Raw materials') }}</h3></div>
                  </div>
                  <div class="mt-5 space-y-3">
                    <div
                      v-for="material in materials"
                      :key="material.id"
                      class="rounded-2xl border border-slate-200 p-4 dark:border-slate-700"
                    >
                      <p class="font-black text-slate-950 dark:text-slate-50">
                        {{ translateMaterialName(material.name) }}
                      </p>
                      <p class="mt-1 text-sm text-slate-600 dark:text-slate-300">
                        {{ material.info }}
                      </p>
                    </div>
                  </div>
                </section>
                <section class="card p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Genealogy') }}</p><h3>{{ t('Material lots') }}</h3></div>
                  </div>
                  <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Lot') }}</th><th>{{ t('Material') }}</th><th>{{ t('Quantity') }}</th><th>{{ t('Section') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="lot in lots"
                          :key="lot.id"
                        >
                          <td class="font-bold">
                            {{ lot.lotNumber }}
                          </td>
                          <td>{{ materialName(lot.rawMaterialId) }}</td>
                          <td>{{ lot.lotQuantity }} {{ translateUnitOfMeasure(lot.lotUnit) }}</td>
                          <td>{{ sectionName(lot.sectionId) }}</td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>

              <!-- QUALITY VIEW -->
              <div
                v-if="false && activeView === 'quality'"
                class="space-y-6"
              >
                <section class="grid gap-4 md:grid-cols-3">
                  <div class="metric-card">
                    <span>{{ t('Results') }}</span><strong>{{ quality.length }}</strong>
                  </div>
                  <div class="metric-card">
                    <span>{{ t('PASS') }}</span><strong>{{ quality.filter((item) => item.result === 'PASS').length }}</strong>
                  </div>
                  <div class="metric-card">
                    <span>{{ t('FAIL') }}</span><strong>{{ quality.filter((item) => item.result === 'FAIL').length }}</strong>
                  </div>
                </section>
                <section class="card p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Quality evidence') }}</p><h3>{{ t('Results, nonconformities, rework and scrap') }}</h3></div>
                  </div>
                  <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('Result') }}</th><th>{{ t('Recorded at') }}</th><th>{{ t('Notes') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="record in quality"
                          :key="record.id"
                        >
                          <td class="font-bold">
                            {{ unitCode(record.productUnitId) }}
                          </td>
                          <td><span :class="statusClass(record.result)">{{ translateQualityResult(record.result) }}</span></td>
                          <td>{{ formatDate(record.recordedAt) }}</td>
                          <td>{{ record.notes }}</td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>

              <!-- RACKS VIEW -->
              <div
                v-if="false && activeView === 'racks'"
                class="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]"
              >
                <section class="card p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Post-line logistics') }}</p><h3>{{ t('Racks are not the WIP anchor') }}</h3></div>
                  </div>
                  <p class="mt-4 text-slate-600 dark:text-slate-300">
                    {{ t('Racks only aggregate supports after the controlled line. The support remains the traceability reference for intra-line WIP.') }}
                  </p>
                  <div class="mt-5 grid gap-4">
                    <div
                      v-for="rack in racks"
                      :key="rack.id"
                      class="rounded-2xl border border-slate-200 p-4 dark:border-slate-700"
                    >
                      <div class="flex items-center justify-between">
                        <strong>{{ rack.rackCode }}</strong><span :class="statusClass(rack.status)">{{ translateStatus(rack.status) }}</span>
                      </div>
                      <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">
                        {{ sectionName(rack.sectionId) }}
                      </p>
                    </div>
                  </div>
                </section>
                <section class="card overflow-hidden p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Extended view') }}</p><h3>{{ t('Subproduct to final assembly concept') }}</h3></div>
                  </div>
                  <img
                    :src="lineCarUrl"
                    alt="Automotive production line"
                    class="mt-5 rounded-lg border border-slate-200 dark:border-slate-700"
                  >
                </section>
              </div>

              <!-- EVENTS VIEW -->
              <div
                v-if="activeView === 'events'"
                class="grid gap-5 xl:grid-cols-2"
              >
                <section class="xl:col-span-2 kpi-grid">
                  <article
                    v-for="metric in eventKpis"
                    :key="metric.key"
                    class="kpi-card"
                    :class="metric.tone"
                  >
                    <span>{{ t(metric.label) }}</span>
                    <strong>{{ metric.value }}</strong>
                    <p>{{ t(metric.detail) }}</p>
                  </article>
                </section>
                <section class="card p-5 sm:p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Simulation') }}</p><h3>{{ t('Execute event playback') }}</h3></div>
                  </div>
                  <p class="mt-3 text-slate-600 dark:text-slate-300">
                    {{ t('Advances supports across the nominal door production line and updates the audit trail.') }}
                  </p>
                  <button
                    class="btn-primary mt-5"
                    :disabled="!can('Simulation.Manage')"
                    @click="executePlayback"
                  >
                    {{ t('Execute playback scenario') }}
                  </button>
                </section>
                <section class="card p-5 sm:p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Controlled event') }}</p><h3>{{ t('Inject manual factory event') }}</h3></div>
                  </div>
                  <div class="mt-5 grid gap-4">
                    <label class="form-label">{{ t('Event type') }}<input
                      v-model="manualEvent.eventType"
                      class="form-input"
                    ></label>
                    <label class="form-label">{{ t('Support code') }}<input
                      v-model="manualEvent.supportCode"
                      class="form-input"
                    ></label>
                    <label class="form-label">{{ t('Section / Rack code') }}<input
                      v-model="manualEvent.sectionCode"
                      class="form-input"
                    ></label>
                    <label class="form-label">{{ t('Unit code') }}<input
                      v-model="manualEvent.productUnitCode"
                      class="form-input"
                    ></label>
                    <label class="form-label">{{ t('Result') }}
                      <select
                        v-model="manualEvent.result"
                        class="form-input"
                      >
                        <option value="PASS">{{ t('PASS') }}</option>
                        <option value="FAIL">{{ t('FAIL') }}</option>
                      </select>
                    </label>
                    <label class="form-label">{{ t('Notes') }}<textarea
                      v-model="manualEvent.notes"
                      class="form-input min-h-24"
                    /></label>
                  </div>
                  <button
                    class="btn-primary mt-5"
                    :disabled="!can('ProductUnits.Transfer') && !can('Quality.Record') && !can('Racks.Manage')"
                    @click="injectManualEvent"
                  >
                    {{ t('Inject manual event') }}
                  </button>
                </section>
                <p
                  v-if="eventStatus"
                  class="2xl:col-span-2 rounded-lg border border-slate-200 bg-white p-4 font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300"
                >
                  {{ eventStatus }}
                </p>
                <section class="2xl:col-span-2 card p-5 sm:p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Operational events') }}</p><h3>{{ t('Recent event stream') }}</h3></div>
                  </div>
                  <div class="table-shell compact-table-shell">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Event') }}</th><th>{{ t('Target') }}</th><th>{{ t('Location') }}</th><th>{{ t('Source') }}</th><th>{{ t('Detail') }}</th><th>{{ t('Timestamp') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="event in recentOperationalEvents"
                          :key="event.eventCode"
                        >
                          <td>
                            <span
                              class="event-chip"
                              :class="statusClass(event.eventType)"
                            >{{ displayOperationalEvent(event.eventType) }}</span>
                          </td>
                          <td>{{ operationalEventTarget(event) }}</td>
                          <td>{{ operationalEventLocation(event) }}</td>
                          <td>{{ t(event.source) }}</td>
                          <td
                            class="max-w-[22rem] truncate"
                            :title="operationalEventDetail(event)"
                          >
                            {{ operationalEventDetail(event) }}
                          </td>
                          <td>{{ formatDate(event.occurredAt) }}</td>
                        </tr>
                        <tr v-if="!recentOperationalEvents.length">
                          <td
                            colspan="6"
                            class="text-center"
                          >
                            {{ t('No records found') }}
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
                <section class="2xl:col-span-2 card p-5 sm:p-6">
                  <div class="section-heading">
                    <div><p>{{ t('Audit trail') }}</p><h3>{{ t('Support localization history') }}</h3></div>
                  </div>
                  <p class="mt-3 text-sm text-slate-600 dark:text-slate-300">
                    {{ t('Support localization history is generated by movements and is read-only in this interface.') }}
                  </p>
                  <div class="table-shell">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Support') }}</th><th>{{ t('Section') }}</th><th>{{ t('Event') }}</th><th>{{ t('Timestamp') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="history in supportHistory"
                          :key="history.id"
                        >
                          <td>{{ supportCode(history.supportId) }}</td>
                          <td>{{ sectionName(history.sectionId) }}</td>
                          <td>{{ displayOperationalEvent(history.eventType) }}</td>
                          <td>{{ formatDate(history.dateTime) }}</td>
                        </tr>
                        <tr v-if="!supportHistory.length">
                          <td
                            colspan="4"
                            class="text-center"
                          >
                            {{ t('No records found') }}
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>

              <!-- GRAFANA ANALYTICS VIEW -->
              <div v-if="activeView === 'analytics'">
                <GrafanaAnalyticsView
                  :theme="theme"
                  :current-locale="locale"
                  :api-status="apiStatus"
                  :api-base-url="apiBaseUrl"
                  :operational-data="analyticsOperationalData"
                  @refresh="loadData(false)"
                />
              </div>

              <!-- USERS VIEW -->
              <div
                v-if="activeView === 'users' && canManageUsers"
                class="space-y-5 lg:space-y-6"
              >
                <section class="ops-hero">
                  <div class="section-heading">
                    <div class="min-w-0">
                      <p>{{ t('Admin') }}</p>
                      <h3>{{ t('User Management') }}</h3>
                      <p class="section-description">
                        {{ t('Local user management keeps demo access explicit without changing backend authentication.') }}
                      </p>
                    </div>
                  </div>
                  <div class="kpi-grid mt-5">
                    <article
                      v-for="metric in userStatusCards"
                      :key="metric.key"
                      class="kpi-card"
                      :class="metric.tone"
                    >
                      <span>{{ t(metric.label) }}</span>
                      <strong>{{ metric.value }}</strong>
                      <p>{{ t(metric.detail) }}</p>
                    </article>
                  </div>
                </section>
                <section class="industrial-panel">
                  <div class="section-heading">
                    <div><p>{{ t('Access control') }}</p><h3>{{ editingUsername ? t('Editing user') : t('Create or update user') }}</h3></div>
                  </div>
                  <form
                    class="mt-5 grid gap-4 lg:grid-cols-2"
                    @submit.prevent="registerUser(false)"
                  >
                    <label class="form-label">{{ t('Name / full name') }}<input
                      v-model="registerForm.name"
                      class="form-input"
                    ></label>
                    <label class="form-label">{{ t('Username') }}<input
                      v-model="registerForm.username"
                      class="form-input disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500 dark:disabled:bg-slate-800"
                      :disabled="editingUsername !== null"
                    ></label>
                    <label class="form-label">{{ t('Email') }}<input
                      v-model="registerForm.email"
                      type="email"
                      class="form-input"
                    ></label>
                    <label class="form-label">{{ t('Role') }}
                      <select
                        v-model="registerForm.roleKey"
                        class="form-input disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500 dark:disabled:bg-slate-800"
                        :disabled="editingUsername === defaultUser.username"
                      >
                        <option value="admin">{{ t('Administrator') }}</option>
                        <option value="supervisor">{{ t('Supervisor') }}</option>
                        <option value="operator">{{ t('Operator') }}</option>
                        <option value="quality">{{ t('Quality technician') }}</option>
                        <option value="logistics">{{ t('Logistics') }}</option>
                        <option value="client">{{ t('Client') }}</option>
                      </select>
                    </label>
                    <label class="form-label">{{ t('Password') }}<input
                      v-model="registerForm.password"
                      type="password"
                      class="form-input"
                    ></label>
                    <label class="form-label">{{ t('Confirm password') }}<input
                      v-model="registerForm.confirmPassword"
                      type="password"
                      class="form-input"
                    ></label>
                    <p
                      v-if="editingUsername"
                      class="lg:col-span-2 text-sm text-slate-500 dark:text-slate-400"
                    >
                      {{ t('Leave password empty to keep current password') }}
                    </p>
                    <div class="lg:col-span-2 flex flex-wrap items-center gap-3">
                      <button
                        class="btn-primary"
                        type="submit"
                      >
                        {{ editingUsername ? t('Update user') : t('Create user') }}
                      </button>
                      <button
                        v-if="editingUsername"
                        class="btn-secondary"
                        type="button"
                        @click="resetUserForm()"
                      >
                        {{ t('Cancel') }}
                      </button>
                      <span
                        v-if="registerError"
                        class="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-semibold text-red-700 dark:border-red-800 dark:bg-red-950/30 dark:text-red-100"
                      >{{ registerError }}</span>
                      <span
                        v-if="registerSuccess"
                        class="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm font-semibold text-emerald-700 dark:border-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-100"
                      >{{ registerSuccess }}</span>
                    </div>
                  </form>
                </section>
                <section class="industrial-panel">
                  <div class="section-heading">
                    <div><p>{{ t('Users') }}</p><h3>{{ t('Existing users') }}</h3></div>
                  </div>
                  <div class="table-shell">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Username') }}</th><th>{{ t('Name') }}</th><th>{{ t('Email') }}</th><th>{{ t('Role') }}</th><th>{{ t('Status') }}</th><th>{{ t('Last update') }}</th><th /></tr></thead>
                      <tbody>
                        <tr
                          v-for="profile in users"
                          :key="profile.username"
                        >
                          <td class="font-bold">
                            {{ profile.username }}
                          </td>
                          <td>{{ profile.name }}</td>
                          <td>{{ profile.email }}</td>
                          <td>{{ getRoleLabel(profile.roleKey) }}</td>
                          <td><span :class="statusClass(profile.active ? 'Active' : 'Blocked')">{{ profile.active ? t('Active') : t('Blocked') }}</span></td>
                          <td>{{ formatDate(profile.lastLogin) }}</td>
                          <td>
                            <div class="flex flex-wrap gap-2">
                              <button
                                class="btn-secondary btn-compact"
                                @click="startEditUser(profile)"
                              >
                                {{ t('Edit') }}
                              </button>
                              <button
                                v-if="profile.username !== 'admin'"
                                class="btn-secondary btn-compact"
                                @click="toggleUserActive(profile)"
                              >
                                {{ profile.active ? t('Block') : t('Unblock') }}
                              </button>
                              <button
                                v-if="profile.username !== 'admin'"
                                class="btn-danger btn-compact"
                                @click="removeUser(profile.username)"
                              >
                                {{ t('Delete') }}
                              </button>
                            </div>
                          </td>
                        </tr>
                        <tr v-if="!users.length">
                          <td colspan="7">
                            <div class="empty-state">
                              <strong>{{ t('No users found') }}</strong><p>{{ t('Create an operator profile to start local dashboard access.') }}</p>
                            </div>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>

              <!-- FIWARE VIEW -->
              <div
                v-if="activeView === 'fiware'"
                class="space-y-5 lg:space-y-6"
              >
                <section class="ops-hero">
                  <div class="section-heading">
                    <div class="min-w-0">
                      <p>{{ t('Context broker boundary') }}</p>
                      <h3>{{ t('Current NGSI-LD-style context') }}</h3>
                      <p class="section-description">
                        {{ t('The relational backend remains the business source of truth. Orion-LD is used for current/hot context of supports, product units and racks.') }}
                      </p>
                    </div>
                    <div class="flex w-full flex-wrap gap-2 sm:w-auto">
                      <button
                        class="btn-secondary w-full sm:w-auto"
                        :disabled="fiwareLoading"
                        @click="refreshFiwareContext()"
                      >
                        {{ t('Refresh context') }}
                      </button>
                      <button
                        class="btn-primary w-full sm:w-auto"
                        :disabled="fiwareLoading || !can('Fiware.Manage')"
                        @click="publishFiware"
                      >
                        {{ t('Publish current context to Orion-LD') }}
                      </button>
                    </div>
                  </div>
                  <div class="kpi-grid mt-5">
                    <article
                      v-for="card in fiwareStatusCards"
                      :key="card.key"
                      class="kpi-card"
                      :class="card.tone"
                    >
                      <span>{{ t(card.label) }}</span>
                      <strong>{{ card.value }}</strong>
                      <p>{{ t(card.detail) }}</p>
                    </article>
                  </div>
                  <p
                    v-if="fiwareActionStatus"
                    class="mt-4 rounded-lg border border-slate-200 bg-white p-4 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200"
                  >
                    {{ fiwareActionStatus }}
                  </p>
                  <div
                    class="mt-4 decision-card"
                    :class="fiwareCoherenceOk ? 'tone-success' : 'tone-warning'"
                  >
                    <span>{{ t('Context coherence') }}</span>
                    <strong>{{ fiwareCoherenceOk ? t('Context coherence confirmed') : t('Context review recommended') }}</strong>
                    <p>{{ fiwareSummaryMessage() }}</p>
                  </div>
                  <p
                    v-if="fiwareContext.errors.length"
                    class="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm font-semibold text-red-700 dark:border-red-700/70 dark:bg-red-900/30 dark:text-red-100"
                  >
                    {{ fiwareContext.errors.join(' | ') }}
                  </p>
                </section>

                <section class="industrial-panel">
                  <div class="section-heading">
                    <div><p>{{ t('FIWARE entities') }}</p><h3>{{ t('Published context entities') }}</h3></div>
                  </div>
                  <div class="table-shell">
                    <table class="data-table">
                      <thead><tr><th>{{ t('Type') }}</th><th>{{ t('ID') }}</th><th>{{ t('Main attributes') }}</th></tr></thead>
                      <tbody>
                        <tr
                          v-for="entity in fiwareContext.entities"
                          :key="entity.id"
                        >
                          <td><span class="badge-blue">{{ fiwareEntityTypeLabel(entity.type) }}</span></td>
                          <td class="font-mono text-xs">
                            {{ entity.id }}
                          </td>
                          <td>{{ fiwareAttributesPreview(entity.attributes) }}</td>
                        </tr>
                        <tr v-if="!fiwareContext.entities.length">
                          <td colspan="3">
                            <div class="empty-state">
                              <strong>{{ t('No FIWARE entities available.') }}</strong><p>{{ t('Publish current context or refresh Orion-LD to validate hot context.') }}</p>
                            </div>
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>
                </section>

                <section
                  v-if="fiwareLastPublish"
                  class="industrial-panel"
                >
                  <div class="section-heading">
                    <div><p>{{ t('Publish summary') }}</p><h3>{{ t('Last publish result') }}</h3></div>
                  </div>
                  <div class="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                    <div class="kpi-card tone-muted">
                      <p class="text-xs font-bold uppercase tracking-normal text-slate-500 dark:text-slate-400">
                        {{ t('Attempted') }}
                      </p>
                      <p class="mt-2 text-sm font-black">
                        {{ fiwareLastPublish.attemptedCount }}
                      </p>
                    </div>
                    <div class="kpi-card tone-success">
                      <p class="text-xs font-bold uppercase tracking-normal text-slate-500 dark:text-slate-400">
                        {{ t('Published') }}
                      </p>
                      <p class="mt-2 text-sm font-black">
                        {{ fiwareLastPublish.publishedCount }}
                      </p>
                    </div>
                    <div
                      class="kpi-card"
                      :class="fiwareLastPublish.failedCount ? 'tone-danger' : 'tone-success'"
                    >
                      <p class="text-xs font-bold uppercase tracking-normal text-slate-500 dark:text-slate-400">
                        {{ t('Failed') }}
                      </p>
                      <p class="mt-2 text-sm font-black">
                        {{ fiwareLastPublish.failedCount }}
                      </p>
                    </div>
                    <div class="kpi-card tone-info">
                      <p class="text-xs font-bold uppercase tracking-normal text-slate-500 dark:text-slate-400">
                        {{ t('Stale removed') }}
                      </p>
                      <p class="mt-2 text-sm font-black">
                        {{ fiwareLastPublish.staleDeletedCount }}
                      </p>
                    </div>
                  </div>
                  <p
                    v-if="fiwareLastPublish.staleEntityIds.length"
                    class="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
                  >
                    <span class="font-black">{{ t('Stale entity IDs') }}:</span> {{ fiwareLastPublish.staleEntityIds.join(' | ') }}
                  </p>
                  <p
                    v-if="fiwareLastPublish.errors.length"
                    class="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm font-semibold text-red-700 dark:border-red-700/70 dark:bg-red-900/30 dark:text-red-100"
                  >
                    <span class="font-black">{{ t('Synchronization errors') }}:</span> {{ fiwareLastPublish.errors.join(' | ') }}
                  </p>
                </section>
              </div>
            </template>
          </div>
        </section>
      </main>
    </div>
  </div>
</template>
