<script setup lang="ts">
import { computed, onMounted, ref, type Ref } from 'vue'
import logoUrl from './assets/branding/drivolution-logo.png'
import lineDoorUrl from './assets/branding/production-line-door.png'
import lineCarUrl from './assets/branding/production-line-car.png'
import CrudPanel from './components/CrudPanel.vue'
import { api, apiGet, apiPost, baseURL as apiBaseUrl, createEntity, deleteEntity, getApiErrorMessage, updateEntity } from './services/api'
import {
  demoCheckpoints,
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
  demoPredictions,
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
  type PredictionRecord,
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

type RoleKey = 'admin' | 'operator' | 'client'

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
  if (roleKey === 'admin') return t('Administrator')
  if (roleKey === 'client') return t('Client')
  return t('Operator')
}

function buildUserRole(roleKey: RoleKey) {
  if (roleKey === 'admin') return 'Administrador'
  if (roleKey === 'client') return 'Cliente'
  return 'Funcionário/Operador'
}

function defaultJobTitle(roleKey: RoleKey) {
  if (roleKey === 'admin') return 'Administrador do sistema'
  if (roleKey === 'client') return 'Cliente'
  return 'Operador'
}

function persistUsers() {
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
  if (!profile) {
    isEditingProfile.value = false
    profileError.value = ''
    profileSuccess.value = ''
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
  if (!users.value.some((u) => u.username === defaultUser.username)) {
    users.value.unshift({ ...defaultUser })
  }
  persistUsers()
}

function isAdmin(profile = user.value) {
  return profile?.roleKey === 'admin' || profile?.username === 'admin'
}

const canManageUsers = computed(() => isAdmin())

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
  } else {
    loginError.value = t('Invalid credentials')
  }
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
  | 'orders'
  | 'units'
  | 'supports'
  | 'materials'
  | 'quality'
  | 'racks'
  | 'events'
  | 'fiware'
  | 'profile'
  | 'settings'
  | 'users'
  | 'parameters'
  | 'predictions'

const activeView = ref<ViewKey>('overview')
const loading = ref(true)
const apiStatus = ref('Connecting to API...')
const eventStatus = ref('')

const summary = ref<DashboardSummary>(demoDashboard)
const products = ref<Product[]>(demoProducts)
const variants = ref<Variant[]>(demoVariants)
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
const predictions = ref<PredictionRecord[]>(demoPredictions)
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

const fiwareContext = ref<FiwareContextSnapshot>(emptyFiwareContext())
const fiwareLoading = ref(false)
const fiwareActionStatus = ref('')
const fiwareLastPublish = ref<FiwarePublishResult | null>(null)

const manualEvent = ref({
  eventType: 'MoveSupport',
  supportCode: 'SUP-005',
  sectionCode: 'SEC-WELD',
  productUnitCode: 'DU-005',
  result: 'PASS',
  notes: '',
})

// The navigation items displayed in the sidebar. Each item has a key to
// control which view is active, a label (in English) that will be
// translated using the `t` function, and a simple icon. If you add
// additional domain views in the future (e.g. users or predictions) include
// them here.
const nav = [
  { key: 'overview', label: 'Dashboard / Line Overview', icon: '⌁' },
  { key: 'orders', label: 'Manufacturing Orders', icon: 'MO' },
  { key: 'units', label: 'Product Units', icon: 'PU' },
  { key: 'supports', label: 'Supports / WIP Tracking', icon: 'SUP' },
  { key: 'materials', label: 'Materials and Lots', icon: 'LOT' },
  { key: 'quality', label: 'Quality', icon: 'QC' },
  { key: 'racks', label: 'Racks / Post-line Logistics', icon: 'RK' },
  { key: 'events', label: 'Event Playback', icon: 'EV' },
  { key: 'fiware', label: 'FIWARE Context Monitor', icon: 'LD' },
  { key: 'predictions', label: 'Predictions', icon: 'PR' },
  { key: 'parameters', label: 'System Parameters', icon: 'CFG' },
  { key: 'users', label: 'Users', icon: 'USR' },
] as const

const adminOnlyViews = new Set<ViewKey>(['users', 'parameters'])

function canShowNav(key: ViewKey) {
  return !adminOnlyViews.has(key) || canManageUsers.value
}

const viewTitles: Record<ViewKey, string> = {
  overview: 'Dashboard / Line Overview',
  orders: 'Manufacturing Orders',
  units: 'Product Units',
  supports: 'Supports / WIP Tracking',
  materials: 'Materials and Lots',
  quality: 'Quality',
  racks: 'Racks / Post-line Logistics',
  events: 'Event Playback',
  fiware: 'FIWARE Context Monitor',
  profile: 'Profile',
  settings: 'Settings',
  users: 'Users',
  parameters: 'System Parameters',
  predictions: 'Predictions',
}

const activeViewTitle = computed(() => viewTitles[activeView.value])

function navIcon(key: ViewKey, icon: string) {
  return key === 'overview' ? 'OV' : icon
}

const sectionsById = computed(() => new Map(productionLineSections.value.map((section) => [section.id, section])))
const supportsById = computed(() => new Map(supports.value.map((support) => [support.id, support])))
const unitsById = computed(() => new Map(units.value.map((unit) => [unit.id, unit])))

const activeUnits = computed(() => units.value.filter((unit) => ['Active', 'Blocked', 'Rework'].includes(unit.status)))
const blockedUnits = computed(() => units.value.filter((unit) => ['Blocked', 'Rework', 'Scrap'].includes(unit.status) || unit.qualityStatus === 'FAIL'))

function statusClass(status: string | undefined) {
  const value = (status || '').toLowerCase()
  if (value.includes('pass') || value.includes('active') || value.includes('loaded') || value.includes('progress')) return 'badge-blue'
  if (value.includes('fail') || value.includes('blocked') || value.includes('scrap')) return 'badge-red'
  if (value.includes('rework') || value.includes('pending')) return 'badge-amber'
  if (value.includes('completed') || value.includes('stored')) return 'badge-green'
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

function sectionName(sectionId?: number) {
  return sectionId ? translateSectionName(sectionsById.value.get(sectionId)?.name || `Section ${sectionId}`) : t('Not assigned')
}

function supportCode(supportId?: number) {
  return supportId ? supportsById.value.get(supportId)?.supportCode || `SUP ${supportId}` : t('No support')
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

function formatConfidence(value?: number | null) {
  return typeof value === 'number' ? `${Math.round(value * 100)}%` : '-'
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
  itemsRef: Ref<EntityRecord[]>
  fields: CrudField[]
  columns: CrudColumn[]
  newItem: () => EntityRecord
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
const orderOptions = () => entityOptions(orders.value, (item) => item.orderNumber, false)
const optionalOrderOptions = () => entityOptions(orders.value, (item) => item.orderNumber)
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
const nonconformityOptions = () => entityOptions(nonconformities.value, (item) => `${unitCode(item.productUnitId)} · ${translateStatus(item.status)}`)
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

function startCrudAdd(config: CrudConfig) {
  crudForms.value[config.key] = { ...config.newItem() }
  crudEditingId.value[config.key] = null
  crudOpen.value[config.key] = true
  crudMessages.value[config.key] = undefined
}

function startCrudEdit(config: CrudConfig, item: EntityRecord) {
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
  return ['PASS', 'FAIL'].includes(String(form.result)) ? '' : 'Result must be PASS or FAIL'
}

function supportDeleteRule(item: EntityRecord) {
  const supportId = Number(item.id)
  const hasActiveUnit = units.value.some((unit) => unit.currentSupportId === supportId && !['Completed', 'Scrap'].includes(String(unit.status)))
  return hasActiveUnit ? 'Support has active product units' : true
}

function crudPanelConfig(config: CrudConfig) {
  return {
    ...config,
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
  itemsRef: asCrudRef(orders),
  fields: [
    { key: 'orderNumber', label: 'Order number', required: true },
    { key: 'productId', label: 'Product', type: 'select', required: true, options: productOptions },
    { key: 'variantId', label: 'Variant', type: 'select', nullable: true, options: variantOptions },
    { key: 'manufacturingProcessId', label: 'Manufacturing process', type: 'select', required: true, options: processOptions },
    { key: 'productionLineId', label: 'Production line', type: 'select', required: true, options: productionLineOptions },
    { key: 'plannedQty', label: 'Planned qty', type: 'number', required: true, min: 1 },
    { key: 'scheduledUntil', label: 'Scheduled until', type: 'datetime', required: true },
    { key: 'status', label: 'Status', type: 'select', required: true, options: () => simpleOptions(['Planned', 'In Progress', 'Completed', 'Blocked', 'Cancelled']) },
    { key: 'observations', label: 'Observations', type: 'textarea' },
  ],
  columns: [
    { key: 'orderNumber', label: 'Order' },
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
    manufacturingProcessId: manufacturingProcesses.value[0]?.id || '',
    productionLineId: productionLines.value[0]?.id || '',
    plannedQty: 1,
    scheduledUntil: nowInput(),
    status: 'Planned',
    observations: '',
  }),
  validate: plannedQtyValidation,
}

const unitsCrud: CrudConfig = {
  key: 'units',
  title: 'Product Units',
  path: '/product-units',
  itemsRef: asCrudRef(units),
  confirmDelete: true,
  fields: [
    { key: 'unitCode', label: 'Unit code', required: true },
    { key: 'unitType', label: 'Unit type', type: 'select', required: true, options: () => simpleOptions(['Subproduct', 'Final']) },
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
    unitType: 'Subproduct',
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
  itemsRef: asCrudRef(unitMaterialLotUsages),
  confirmDelete: true,
  fields: [
    { key: 'productUnitId', label: 'Product unit', type: 'select', required: true, options: unitOptions },
    { key: 'lotId', label: 'Lot', type: 'select', required: true, options: lotOptions },
    { key: 'associationType', label: 'Association type', type: 'select', required: true, options: () => simpleOptions(['Consumed', 'Reserved']) },
    { key: 'quantity', label: 'Quantity', type: 'number', required: true, min: 1 },
  ],
  columns: [
    { key: 'productUnitId', label: 'Unit', format: (value) => unitCode(Number(value)) },
    { key: 'lotId', label: 'Lot', format: (value) => lotLabel(value) },
    { key: 'associationType', label: 'Type', format: (value) => translateStatus(String(value || '')) },
    { key: 'quantity', label: 'Quantity' },
  ],
  newItem: () => ({ productUnitId: units.value[0]?.id || '', lotId: lots.value[0]?.id || '', associationType: 'Consumed', quantity: 1 }),
  validate: positiveQuantityValidation,
}

const qualityResultsCrud: CrudConfig = {
  key: 'quality-results',
  title: 'Quality results',
  path: '/quality-results',
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

const predictionsCrud: CrudConfig = {
  key: 'predictions',
  title: 'Predictions',
  description: 'This section prepares future analysis of completion times, delay risk and productive deviations. In this V1 the data is demonstrative.',
  path: '/predictions',
  itemsRef: asCrudRef(predictions),
  fields: [
    { key: 'manufacturingOrderId', label: 'Manufacturing order', type: 'select', nullable: true, options: optionalOrderOptions },
    { key: 'modelVersion', label: 'Model version', required: true },
    { key: 'modelType', label: 'Model type', required: true },
    { key: 'lastDate', label: 'Last update', type: 'datetime', nullable: true },
  ],
  columns: [
    { key: 'manufacturingOrderId', label: 'Order', format: (value) => orderLabel(value) },
    { key: 'modelVersion', label: 'Model' },
    { key: 'modelType', label: 'Type', format: (value) => t(String(value || '')) },
    { key: 'lastDate', label: 'Last update', format: formatOptionalDate },
  ],
  newItem: () => ({ manufacturingOrderId: orders.value[0]?.id || '', modelVersion: 'future-v1', modelType: 'Placeholder', lastDate: nowInput(), createdAt: new Date().toISOString() }),
}

const parameterConfigs: CrudConfig[] = [
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
    ],
    columns: [
      { key: 'lineCode', label: 'Code' },
      { key: 'name', label: 'Name' },
    ],
    newItem: () => ({ lineCode: '', name: '' }),
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
    ],
    columns: [
      { key: 'sectionCode', label: 'Code' },
      { key: 'name', label: 'Name', format: (value) => translateSectionName(String(value || '')) },
      { key: 'sectionType', label: 'Type', format: (value) => translateSectionName(String(value || '')) },
      { key: 'lineId', label: 'Production line', format: (value) => lineLabel(value) },
    ],
    newItem: () => ({ sectionCode: '', name: '', sectionType: '', lineId: productionLines.value[0]?.id || '' }),
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
  if (activeView.value === 'predictions') return [predictionsCrud]
  if (activeView.value === 'parameters' && canManageUsers.value) return parameterConfigs.filter((config) => config.key === parameterTab.value)
  return []
})

async function loadData(showSpinner = true) {
  if (showSpinner) loading.value = true
  const [
    dashboardData,
    productData,
    variantData,
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
    predictionData,
    supportHistoryData,
    contextData,
  ] = await Promise.all([
    apiGet('/dashboard/summary', demoDashboard),
    apiGet('/products', demoProducts),
    apiGet('/variants', demoVariants),
    apiGet('/production-lines', demoProductionLines),
    apiGet('/production-line-sections', demoProductionLineSections),
    apiGet('/resources', demoResources),
    apiGet('/manufacturing-processes', demoManufacturingProcesses),
    apiGet('/manufacturing-section-phases', demoManufacturingSectionPhases),
    apiGet('/manufacturing-process-phases', demoManufacturingProcessPhases),
    apiGet('/checkpoints', demoCheckpoints),
    apiGet('/manufacturing-orders', demoOrders),
    apiGet('/product-units', demoUnits),
    apiGet('/supports', demoSupports),
    apiGet('/racks', demoRacks),
    apiGet('/rack-support-assignments', demoRackSupportAssignments),
    apiGet('/raw-materials', demoMaterials),
    apiGet('/lot-raw-materials', demoLots),
    apiGet('/unit-material-lot-usages', demoUnitMaterialLotUsages),
    apiGet('/quality-results', demoQuality),
    apiGet('/nonconformities', demoNonconformities),
    apiGet('/rework-records', demoReworkRecords),
    apiGet('/scrap-records', demoScrapRecords),
    apiGet('/predictions', demoPredictions),
    apiGet('/support-localization-history', demoSupportLocalizationHistory),
    apiGet('/fiware/context', emptyFiwareContext()),
  ])

  summary.value = dashboardData
  products.value = productData
  variants.value = variantData
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
  predictions.value = predictionData
  supportHistory.value = supportHistoryData
  fiwareContext.value = normalizeFiwareContextPayload(contextData)
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

onMounted(loadData)
</script>

<template>
  <!-- Top-level wrapper applies dark mode to the entire page based on the
       current theme. Vue's reactivity ensures updates are reflected on
       change. -->
  <div class="min-h-screen bg-slate-100 text-slate-900 dark:bg-slate-900 dark:text-slate-100">
    <!-- LOGIN / REGISTO -->
    <div v-if="!isAuthenticated" class="flex items-center justify-center min-h-screen p-4">
      <div class="w-full max-w-md card p-8">
        <div class="mb-6 text-center">
          <img :src="logoUrl" alt="DRIVOLUTION logo" class="mx-auto h-20 w-auto object-contain" />
          <h1 class="mt-4 text-2xl font-black tracking-tight">{{ isRegistering ? t('Register new user') : t('Login') }}</h1>
          <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">DriveTrace Core · DRIVOLUTION WP3</p>
        </div>
        <form v-if="!isRegistering" @submit.prevent="login" class="space-y-4">
          <label class="form-label">{{ t('Username') }}<input v-model="loginForm.username" class="form-input" /></label>
          <label class="form-label">{{ t('Password') }}<input type="password" v-model="loginForm.password" class="form-input" /></label>
          <p v-if="loginError" class="text-red-600 text-sm">{{ loginError }}</p>
          <button type="submit" class="btn-primary w-full">{{ t('Submit') }}</button>
          <button type="button" class="btn-secondary w-full" @click="isRegistering = true; loginError = ''">{{ t('Create account') }}</button>
          <p class="text-center text-xs text-slate-500 dark:text-slate-400">admin / admin</p>
        </form>
        <form v-else @submit.prevent="registerUser(false)" class="space-y-4">
          <label class="form-label">{{ t('Name / full name') }}<input v-model="registerForm.name" class="form-input" /></label>
          <label class="form-label">{{ t('Username') }}<input v-model="registerForm.username" class="form-input" /></label>
          <label class="form-label">{{ t('Email') }}<input v-model="registerForm.email" type="email" class="form-input" /></label>
          <label class="form-label">{{ t('Password') }}<input v-model="registerForm.password" type="password" class="form-input" /></label>
          <label class="form-label">{{ t('Confirm password') }}<input v-model="registerForm.confirmPassword" type="password" class="form-input" /></label>
          <label class="form-label">{{ t('Role') }}
            <select v-model="registerForm.roleKey" class="form-input">
              <option value="operator">{{ t('Operator') }}</option>
              <option value="client">{{ t('Client') }}</option>
              <option value="admin">{{ t('Administrator') }}</option>
            </select>
          </label>
          <p v-if="registerError" class="text-red-600 text-sm">{{ registerError }}</p>
          <p v-if="registerSuccess" class="text-green-600 text-sm">{{ registerSuccess }}</p>
          <button type="submit" class="btn-primary w-full">{{ t('Create user') }}</button>
          <button type="button" class="btn-secondary w-full" @click="isRegistering = false; registerError = ''; registerSuccess = ''">{{ t('Already have an account?') }}</button>
        </form>
      </div>
    </div>

    <!-- MAIN APPLICATION -->
    <div v-else class="app-shell">
      <!-- SIDEBAR -->
      <aside class="app-sidebar">
        <div class="app-sidebar-brand">
          <img :src="logoUrl" alt="DRIVOLUTION logo" class="mx-auto h-12 w-auto max-w-full object-contain" />
          <h1>DriveTrace Core</h1>
        </div>
        <div class="app-sidebar-nav">
          <nav>
          <button
            v-for="item in nav"
            v-show="canShowNav(item.key)"
            :key="item.key"
            class="nav-item"
            :class="activeView === item.key ? 'nav-item-active' : ''"
            @click="activeView = item.key; showUserMenu = false"
          >
            <span class="nav-icon">{{ navIcon(item.key, item.icon) }}</span>
            <span class="nav-label">{{ t(item.label) }}</span>
          </button>
          </nav>
        </div>
        <!-- Quick settings / user info section replacing the academic scope block -->
        <div class="app-sidebar-profile">
          <p class="font-semibold text-slate-950 dark:text-slate-50">{{ t('Active profile') }}: {{ user?.name }}</p>
          <p class="mt-1">{{ t('Language') }}: {{ locale === 'pt-PT' ? t('Portuguese') : t('English') }}</p>
          <p class="mt-1">{{ t('Theme') }}: {{ theme === 'dark' ? t('Dark') : t('Light') }}</p>
          <div class="app-sidebar-profile-meta">
            <p>{{ t('WIP automóvel') }}</p>
            <p>{{ t('Suporte como âncora intra-linha') }}</p>
            <p>{{ t('Rack como logística pós-linha') }}</p>
          </div>
          <div class="app-sidebar-profile-actions">
            <button class="btn-secondary flex-1" @click="openProfileView">{{ t('Profile') }}</button>
            <button class="btn-secondary flex-1" @click="activeView = 'settings'">{{ t('Settings') }}</button>
          </div>
          <button class="btn-secondary mt-2 w-full" @click="logout">{{ t('Logout') }}</button>
        </div>
      </aside>

      <main class="app-main">
        <!-- TOP BAR -->
        <header class="app-topbar">
          <div class="content-shell px-4 py-4 sm:px-5 lg:px-6 xl:px-8">
            <div class="flex flex-col justify-between gap-4 2xl:flex-row 2xl:items-start">
              <div class="flex min-w-0 flex-1 items-start gap-3 sm:gap-4">
                <img :src="logoUrl" alt="DRIVOLUTION logo" class="h-10 w-10 shrink-0 rounded-xl object-contain lg:hidden" />
                <div class="min-w-0">
                  <p class="text-xs font-bold uppercase tracking-[0.16em] text-drivolution-700 dark:text-drivolution-300">{{ t('WIP Traceability and Monitoring Platform') }}</p>
                  <h2 class="mt-1 max-w-3xl text-xl font-black leading-tight tracking-tight sm:text-2xl">{{ t(activeViewTitle) }}</h2>
                </div>
              </div>
              <div class="relative flex w-full flex-wrap items-center gap-2 sm:gap-3 2xl:w-auto 2xl:justify-end">
                <span class="w-full rounded-full bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-600 dark:bg-slate-700 dark:text-slate-200 sm:w-auto">{{ t(apiStatus) }}</span>
                <button class="btn-secondary" @click="() => loadData()">{{ t('Refresh') }}</button>
                <select v-model="locale" class="min-w-[8.5rem] rounded-2xl border border-slate-200 bg-white px-3 py-2.5 text-sm font-medium text-slate-700 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-200">
                  <option value="pt-PT">{{ t('Portuguese') }}</option>
                  <option value="en">{{ t('English') }}</option>
                </select>
                <button class="btn-secondary" @click="toggleTheme">
                  {{ theme === 'dark' ? t('Light mode') : t('Dark mode') }}
                </button>
                <div class="relative sm:ml-auto 2xl:ml-0">
                  <button @click="showUserMenu = !showUserMenu" class="flex items-center gap-2 rounded-full bg-slate-100 px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm dark:bg-slate-700 dark:text-slate-200">
                    <span class="inline-flex h-6 w-6 items-center justify-center rounded-full bg-drivolution-500 text-xs font-black text-white">{{ user?.name.charAt(0) }}</span>
                    <span class="hidden sm:inline-block">{{ user?.name }}</span>
                  </button>
                  <div v-if="showUserMenu" class="absolute right-0 z-30 mt-2 w-44 rounded-2xl border border-slate-200 bg-white p-1 shadow-lg dark:border-slate-600 dark:bg-slate-700">
                    <button class="w-full rounded-xl px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-600" @click="openProfileView">{{ t('Profile') }}</button>
                    <button class="w-full rounded-xl px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-600" @click="activeView = 'settings'; showUserMenu = false">{{ t('Settings') }}</button>
                    <button class="w-full rounded-xl px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-600" @click="logout(); showUserMenu = false">{{ t('Logout') }}</button>
                  </div>
                </div>
              </div>
            </div>
            <!-- Mobile tab navigation -->
            <div class="mt-4 flex gap-2 overflow-x-auto pb-1 lg:hidden">
              <button v-for="item in nav" v-show="canShowNav(item.key)" :key="item.key" class="mobile-tab" :class="activeView === item.key ? 'mobile-tab-active' : ''" @click="activeView = item.key; showUserMenu = false">
                {{ t(item.label) }}
              </button>
            </div>
          </div>
        </header>
        <!-- MAIN CONTENT -->
        <section class="app-content">
          <div class="content-shell">
            <div v-if="loading" class="card p-6 text-center text-slate-600 dark:bg-slate-800 dark:text-slate-200 sm:p-8">{{ t('Loading DriveTrace Core data...') }}</div>
            <template v-else>
              <!-- PROFILE VIEW -->
              <div v-if="activeView === 'profile'" class="mx-auto w-full max-w-4xl">
                <section class="card p-5 sm:p-6 lg:p-8">
                  <div class="flex flex-col gap-4 border-b border-slate-200 pb-5 dark:border-slate-700 sm:flex-row sm:items-center sm:justify-between">
                    <div class="flex items-center gap-4">
                      <span class="inline-flex h-14 w-14 items-center justify-center rounded-full bg-drivolution-500 text-xl font-black text-white">{{ user?.name?.charAt(0)?.toUpperCase() || 'U' }}</span>
                      <div>
                        <p class="text-xs font-bold uppercase tracking-[0.14em] text-drivolution-700 dark:text-drivolution-300">{{ t('Profile') }}</p>
                        <h3 class="mt-1 text-2xl font-black text-slate-950 dark:text-white">{{ user?.name }}</h3>
                        <p class="mt-1 text-sm font-semibold text-slate-600 dark:text-slate-300">{{ user ? getRoleLabel(user.roleKey) : '' }}</p>
                      </div>
                    </div>
                    <button v-if="!isEditingProfile" class="btn-primary w-full sm:w-auto" @click="startProfileEdit">{{ t('Edit profile') }}</button>
                  </div>

                  <div class="mt-5">
                    <h4 class="text-xs font-bold uppercase tracking-[0.14em] text-slate-500 dark:text-slate-400">{{ t('Account information') }}</h4>
                    <div class="mt-3 divide-y divide-slate-200 overflow-hidden rounded-2xl border border-slate-200 dark:divide-slate-700 dark:border-slate-700">
                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">{{ t('Name') }}</p>
                        <div>
                          <input v-if="isEditingProfile" v-model="profileForm.name" class="form-input py-2 sm:py-2.5" />
                          <p v-else class="text-sm font-semibold text-slate-900 dark:text-slate-100">{{ user?.name }}</p>
                        </div>
                        <button v-if="!isEditingProfile" class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2" @click="startProfileEdit">{{ t('Edit') }}</button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">{{ t('Username') }}</p>
                        <p class="text-sm font-semibold text-slate-900 dark:text-slate-100">{{ user?.username }}</p>
                        <span class="inline-flex justify-center rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-600 dark:bg-slate-700 dark:text-slate-200">{{ t('Read-only') }}</span>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">{{ t('Email') }}</p>
                        <div>
                          <input v-if="isEditingProfile" v-model="profileForm.email" type="email" class="form-input py-2 sm:py-2.5" />
                          <p v-else class="text-sm font-semibold text-slate-900 dark:text-slate-100">{{ user?.email }}</p>
                        </div>
                        <button v-if="!isEditingProfile" class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2" @click="startProfileEdit">{{ t('Edit') }}</button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">{{ t('Role') }}</p>
                        <p class="text-sm font-semibold text-slate-900 dark:text-slate-100">{{ user ? getRoleLabel(user.roleKey) : '' }}</p>
                        <span class="inline-flex justify-center rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-600 dark:bg-slate-700 dark:text-slate-200">{{ t('Read-only') }}</span>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">{{ t('Organization') }}</p>
                        <div>
                          <input v-if="isEditingProfile" v-model="profileForm.organization" class="form-input py-2 sm:py-2.5" />
                          <p v-else class="text-sm font-semibold text-slate-900 dark:text-slate-100">{{ profileValue(user?.organization) }}</p>
                        </div>
                        <button v-if="!isEditingProfile" class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2" @click="startProfileEdit">{{ t('Edit') }}</button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">{{ t('Job title') }}</p>
                        <div>
                          <input v-if="isEditingProfile" v-model="profileForm.jobTitle" class="form-input py-2 sm:py-2.5" />
                          <p v-else class="text-sm font-semibold text-slate-900 dark:text-slate-100">{{ profileJobTitle(user?.jobTitle) }}</p>
                        </div>
                        <button v-if="!isEditingProfile" class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2" @click="startProfileEdit">{{ t('Edit') }}</button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">{{ t('Current language') }}</p>
                        <p class="text-sm font-semibold text-slate-900 dark:text-slate-100">{{ locale === 'pt-PT' ? t('Portuguese') : t('English') }}</p>
                        <button class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2" @click="activeView = 'settings'">{{ t('Manage in Settings') }}</button>
                      </div>

                      <div class="grid gap-3 px-4 py-4 sm:grid-cols-[10rem_minmax(0,1fr)_auto] sm:items-center">
                        <p class="text-sm font-bold text-slate-600 dark:text-slate-300">{{ t('Current theme') }}</p>
                        <p class="text-sm font-semibold text-slate-900 dark:text-slate-100">{{ theme === 'dark' ? t('Dark') : t('Light') }}</p>
                        <button class="btn-secondary px-3 py-1.5 text-xs sm:px-3 sm:py-2" @click="activeView = 'settings'">{{ t('Manage in Settings') }}</button>
                      </div>
                    </div>
                  </div>

                  <p v-if="profileError" class="mt-4 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700 dark:border-red-700/60 dark:bg-red-900/30 dark:text-red-100">{{ profileError }}</p>
                  <p v-if="profileSuccess" class="mt-4 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-700 dark:border-emerald-700/60 dark:bg-emerald-900/30 dark:text-emerald-100">{{ profileSuccess }}</p>

                  <div class="mt-6 flex flex-col gap-3 sm:flex-row sm:justify-end">
                    <button v-if="isEditingProfile" class="btn-primary sm:order-2" @click="saveProfileChanges">{{ t('Save changes') }}</button>
                    <button v-if="isEditingProfile" class="btn-secondary sm:order-1" @click="cancelProfileEdit">{{ t('Cancel') }}</button>
                    <button class="btn-secondary sm:order-3" @click="activeView = 'overview'">{{ t('Back to dashboard') }}</button>
                    <button class="btn-secondary sm:order-4" @click="activeView = 'settings'">{{ t('Open settings') }}</button>
                  </div>
                </section>
              </div>

            <!-- SETTINGS VIEW -->
            <div v-if="activeView === 'settings'" class="card space-y-5 p-5 sm:space-y-6 sm:p-6">
              <div class="section-heading"><div><p>{{ t('Settings') }}</p><h3>{{ t('Application Settings') }}</h3></div></div>
              <div>
                <h4 class="font-bold mb-2">{{ t('Language') }}</h4>
                <div class="grid gap-2 sm:grid-cols-2">
                  <button class="btn-secondary flex-1" :class="{ 'nav-item-active': locale === 'pt-PT' }" @click="setLocale('pt-PT')">{{ t('Portuguese') }}</button>
                  <button class="btn-secondary flex-1" :class="{ 'nav-item-active': locale === 'en' }" @click="setLocale('en')">{{ t('English') }}</button>
                </div>
              </div>
              <div>
                <h4 class="font-bold mb-2">{{ t('Theme') }}</h4>
                <div class="grid gap-2 sm:grid-cols-2">
                  <button class="btn-secondary flex-1" :class="{ 'nav-item-active': theme === 'light' }" @click="setTheme('light')">{{ t('Light') }}</button>
                  <button class="btn-secondary flex-1" :class="{ 'nav-item-active': theme === 'dark' }" @click="setTheme('dark')">{{ t('Dark') }}</button>
                </div>
              </div>
              <div>
                <h4 class="font-bold mb-2">{{ t('Accessibility') }}</h4>
                <p class="text-sm text-slate-600 dark:text-slate-300">{{ t('Clear mode is the default. Dark mode, language and session preferences are persisted locally.') }}</p>
              </div>
              <div class="grid gap-4 md:grid-cols-2">
                <div class="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
                  <h4 class="font-bold">{{ t('Application information') }}</h4>
                  <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">DriveTrace Core</p>
                  <p class="mt-1 text-sm text-slate-600 dark:text-slate-300">{{ t('Active profile') }}: {{ user?.username }} · {{ user ? getRoleLabel(user.roleKey) : '' }}</p>
                </div>
                <div class="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
                  <h4 class="font-bold">{{ t('API URL') }}</h4>
                  <p class="mt-2 break-all text-sm font-semibold text-slate-600 dark:text-slate-300">{{ apiBaseUrl }}</p>
                </div>
              </div>
              <div class="rounded-2xl border border-amber-200 bg-amber-50 p-4 text-sm font-semibold text-amber-900 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-100">
                <p>{{ t('Demo/local authentication') }}</p>
                <p class="mt-1 font-medium">{{ t('The API is not protected by JWT yet. Access control is local to the dashboard for this V1.') }}</p>
              </div>
              <button class="btn-secondary" @click="activeView = 'overview'">{{ t('Back to dashboard') }}</button>
            </div>

            <!-- CRUD VIEWS -->
            <div v-if="activeCrudConfigs.length" class="space-y-5 lg:space-y-6">
              <section v-if="activeView === 'units'" class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                <div class="metric-card"><span>{{ t('Traceable units') }}</span><strong>{{ units.length }}</strong></div>
                <div class="metric-card"><span>{{ t('Active / held') }}</span><strong>{{ activeUnits.length }}</strong></div>
                <div class="metric-card"><span>{{ t('Deviations') }}</span><strong>{{ blockedUnits.length }}</strong></div>
              </section>

              <section v-if="activeView === 'supports'" class="card overflow-hidden p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Physical tracking') }}</p><h3>{{ t('Support is the intra-line anchor') }}</h3></div></div>
                <img :src="lineDoorUrl" alt="Support-based line" class="mt-5 w-full max-w-full rounded-3xl border border-slate-200 object-contain dark:border-slate-700" />
              </section>

              <section v-if="activeView === 'quality'" class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                <div class="metric-card"><span>{{ t('Results') }}</span><strong>{{ quality.length }}</strong></div>
                <div class="metric-card"><span>{{ t('PASS') }}</span><strong>{{ quality.filter((item) => item.result === 'PASS').length }}</strong></div>
                <div class="metric-card"><span>{{ t('FAIL') }}</span><strong>{{ quality.filter((item) => item.result === 'FAIL').length }}</strong></div>
              </section>

              <section v-if="activeView === 'racks'" class="grid gap-5 2xl:grid-cols-[0.95fr_1.05fr]">
                <div class="card p-5 sm:p-6">
                  <div class="section-heading"><div><p>{{ t('Post-line logistics') }}</p><h3>{{ t('Racks are not the WIP anchor') }}</h3></div></div>
                  <p class="mt-4 text-slate-600 dark:text-slate-300">{{ t('Racks only aggregate supports after the controlled line. The support remains the traceability reference for intra-line WIP.') }}</p>
                </div>
                <div class="card overflow-hidden p-5 sm:p-6">
                  <div class="section-heading"><div><p>{{ t('Extended view') }}</p><h3>{{ t('Subproduct to final assembly concept') }}</h3></div></div>
                  <img :src="lineCarUrl" alt="Automotive production line" class="mt-5 w-full max-w-full rounded-3xl border border-slate-200 object-contain dark:border-slate-700" />
                </div>
              </section>

              <section v-if="activeView === 'predictions'" class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Predictions') }}</p><h3>{{ t('Operational forecasts') }}</h3></div></div>
                <p class="mt-3 text-slate-600 dark:text-slate-300">{{ t('This section prepares future analysis of completion times, delay risk and productive deviations. In this V1 the data is demonstrative.') }}</p>
              </section>

              <section v-if="activeView === 'parameters' && canManageUsers" class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Admin') }}</p><h3>{{ t('System Parameters') }}</h3></div></div>
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
            <div v-if="activeView === 'overview'" class="space-y-5 lg:space-y-6">
              <section class="hero-card overflow-hidden">
                <div class="grid gap-6 2xl:grid-cols-[1.1fr_0.9fr] 2xl:items-center">
                  <div class="min-w-0">
                    <p class="text-sm font-bold uppercase tracking-[0.18em] text-drivolution-700">{{ t('Automotive WIP traceability') }}</p>
                    <h3 class="mt-3 max-w-3xl text-3xl font-black leading-tight tracking-tight sm:text-4xl">{{ t('DriveTrace Core monitors product units through their physical supports.') }}</h3>
                    <p class="mt-4 max-w-2xl text-sm leading-7 text-slate-600 dark:text-slate-300 sm:text-base">
                      {{ t('The V1 uses a controlled door production line: raw materials, support assignment, stamping, welding, painting, quality control and post-line rack storage.') }}
                    </p>
                    <div class="mt-6 flex flex-wrap gap-3">
                      <span class="domain-pill">{{ t('ProductUnit-centred traceability') }}</span>
                      <span class="domain-pill">{{ t('Support as intra-line anchor') }}</span>
                      <span class="domain-pill">{{ t('Rack as post-line logistics') }}</span>
                      <span class="domain-pill">{{ t('Material lot genealogy') }}</span>
                    </div>
                  </div>
                  <img :src="lineDoorUrl" alt="Door production line" class="w-full max-w-xl justify-self-center rounded-3xl border border-slate-200 bg-white object-contain shadow-sm dark:border-slate-700 dark:bg-slate-800" />
                </div>
              </section>
              <section class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3 2xl:grid-cols-5">
                <div class="metric-card"><span>{{ t('Open orders') }}</span><strong>{{ summary.counts.openOrders }}</strong></div>
                <div class="metric-card"><span>{{ t('Active units') }}</span><strong>{{ summary.counts.activeUnits }}</strong></div>
                <div class="metric-card"><span>{{ t('Active supports') }}</span><strong>{{ summary.counts.activeSupports }}</strong></div>
                <div class="metric-card"><span>{{ t('Quality issues') }}</span><strong>{{ summary.counts.qualityIssues }}</strong></div>
                <div class="metric-card"><span>{{ t('Rack assignments') }}</span><strong>{{ summary.counts.rackAssignments }}</strong></div>
              </section>
              <section class="grid gap-5 2xl:grid-cols-[1.1fr_0.9fr]">
                <div class="card p-5 sm:p-6">
                  <div class="section-heading">
                    <div>
                      <p>{{ t('Line state') }}</p>
                      <h3>{{ t('Current WIP by section') }}</h3>
                    </div>
                  </div>
                  <div class="mt-5 space-y-4">
                    <div v-for="section in summary.wipBySection" :key="section.sectionCode" class="line-step">
                      <div>
                        <p class="font-bold text-slate-950 dark:text-slate-50">{{ translateSectionName(section.section) }}</p>
                        <p class="text-xs uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">{{ translateSectionName(section.sectionType) }}</p>
                      </div>
                      <div class="flex w-full items-center gap-3 sm:min-w-40">
                        <div class="h-2 flex-1 rounded-full bg-slate-200 dark:bg-slate-700">
                          <div class="h-2 rounded-full bg-drivolution-500" :style="{ width: `${Math.min(100, section.productUnits * 28)}%` }"></div>
                        </div>
                        <span class="text-sm font-black text-slate-950 dark:text-slate-50">{{ section.productUnits }}</span>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="card p-5 sm:p-6">
                  <div class="section-heading">
                    <div>
                      <p>{{ t('Quality') }}</p>
                      <h3>{{ t('Open alerts and deviations') }}</h3>
                    </div>
                  </div>
                  <div class="mt-5 space-y-3">
                    <div v-for="alert in summary.qualityAlerts" :key="`${alert.unitCode}-${alert.createdAt}`" class="alert-card">
                      <div class="flex items-center justify-between gap-3">
                        <strong>{{ alert.unitCode }}</strong>
                        <span :class="statusClass(alert.status)">{{ translateStatus(alert.status) }}</span>
                      </div>
                      <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ alert.description }}</p>
                        <p class="mt-2 text-xs font-semibold uppercase tracking-[0.14em] text-slate-400 dark:text-slate-500">{{ translateStatus(alert.severity) }}</p>
                    </div>
                  </div>
                </div>
              </section>
              <section class="card p-5 sm:p-6">
                <div class="section-heading">
                  <div>
                    <p>{{ t('Recent events') }}</p>
                    <h3>{{ t('Support movement and audit trail') }}</h3>
                  </div>
                </div>
                <div class="table-shell">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Support') }}</th><th>{{ t('Section') }}</th><th>{{ t('Event') }}</th><th>{{ t('Timestamp') }}</th></tr></thead>
                    <tbody>
                      <tr v-for="event in summary.recentEvents" :key="`${event.supportCode}-${event.dateTime}`">
                        <td>{{ event.supportCode }}</td>
                        <td>{{ translateSectionName(event.section) }}</td>
                        <td>{{ translateStatus(event.eventType) }}</td>
                        <td>{{ formatDate(event.dateTime) }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- ORDERS VIEW -->
            <div v-if="false && activeView === 'orders'" class="card p-6">
              <div class="section-heading"><div><p>{{ t('Planning') }}</p><h3>{{ t('Manufacturing orders') }}</h3></div></div>
              <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                <table class="data-table">
                  <thead><tr><th>{{ t('Order') }}</th><th>{{ t('Status') }}</th><th>{{ t('Planned qty') }}</th><th>{{ t('Scheduled until') }}</th><th>{{ t('Notes') }}</th></tr></thead>
                  <tbody>
                    <tr v-for="order in orders" :key="order.id">
                      <td class="font-bold">{{ order.orderNumber }}</td>
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
            <div v-if="false && activeView === 'units'" class="space-y-6">
              <section class="grid gap-4 md:grid-cols-3">
                <div class="metric-card"><span>{{ t('Traceable units') }}</span><strong>{{ units.length }}</strong></div>
                <div class="metric-card"><span>{{ t('Active / held') }}</span><strong>{{ activeUnits.length }}</strong></div>
                <div class="metric-card"><span>{{ t('Deviations') }}</span><strong>{{ blockedUnits.length }}</strong></div>
              </section>
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Unitary traceability') }}</p><h3>{{ t('Product units and subproducts') }}</h3></div></div>
                <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('Type') }}</th><th>{{ t('Status') }}</th><th>{{ t('Quality') }}</th><th>{{ t('Current support') }}</th><th>{{ t('Current section') }}</th></tr></thead>
                    <tbody>
                      <tr v-for="unit in units" :key="unit.id">
                        <td class="font-bold">{{ unit.unitCode }}</td>
                        <td>{{ translateUnitType(unit.unitType) }}</td>
                        <td><span :class="statusClass(unit.status)">{{ translateStatus(unit.status) }}</span></td>
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
            <div v-if="false && activeView === 'supports'" class="space-y-6">
              <section class="card overflow-hidden p-6">
                <div class="section-heading"><div><p>{{ t('Physical tracking') }}</p><h3>{{ t('Support is the intra-line anchor') }}</h3></div></div>
                <img :src="lineDoorUrl" alt="Support-based line" class="mt-5 rounded-3xl border border-slate-200 dark:border-slate-700" />
              </section>
              <section class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                <div v-for="support in supports" :key="support.id" class="card p-5">
                  <div class="flex items-start justify-between gap-4">
                    <div><p class="text-sm font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">{{ t('Support') }}</p><h3 class="mt-1 text-xl font-black">{{ support.supportCode }}</h3></div>
                    <span :class="statusClass(support.status)">{{ translateStatus(support.status) }}</span>
                  </div>
                  <p class="mt-4 text-sm text-slate-600 dark:text-slate-300">{{ t('Current section') }}</p>
                  <p class="text-base font-bold text-slate-950 dark:text-slate-50">{{ sectionName(support.currentSectionId) }}</p>
                </div>
              </section>
            </div>

            <!-- MATERIALS VIEW -->
            <div v-if="false && activeView === 'materials'" class="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Items and lots') }}</p><h3>{{ t('Raw materials') }}</h3></div></div>
                <div class="mt-5 space-y-3">
                  <div v-for="material in materials" :key="material.id" class="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
                    <p class="font-black text-slate-950 dark:text-slate-50">{{ translateMaterialName(material.name) }}</p>
                    <p class="mt-1 text-sm text-slate-600 dark:text-slate-300">{{ material.info }}</p>
                  </div>
                </div>
              </section>
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Genealogy') }}</p><h3>{{ t('Material lots') }}</h3></div></div>
                <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Lot') }}</th><th>{{ t('Material') }}</th><th>{{ t('Quantity') }}</th><th>{{ t('Section') }}</th></tr></thead>
                    <tbody>
                      <tr v-for="lot in lots" :key="lot.id">
                        <td class="font-bold">{{ lot.lotNumber }}</td>
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
            <div v-if="false && activeView === 'quality'" class="space-y-6">
              <section class="grid gap-4 md:grid-cols-3">
                <div class="metric-card"><span>{{ t('Results') }}</span><strong>{{ quality.length }}</strong></div>
                <div class="metric-card"><span>{{ t('PASS') }}</span><strong>{{ quality.filter((item) => item.result === 'PASS').length }}</strong></div>
                <div class="metric-card"><span>{{ t('FAIL') }}</span><strong>{{ quality.filter((item) => item.result === 'FAIL').length }}</strong></div>
              </section>
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Quality evidence') }}</p><h3>{{ t('Results, nonconformities, rework and scrap') }}</h3></div></div>
                <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Unit') }}</th><th>{{ t('Result') }}</th><th>{{ t('Recorded at') }}</th><th>{{ t('Notes') }}</th></tr></thead>
                    <tbody>
                      <tr v-for="record in quality" :key="record.id">
                        <td class="font-bold">{{ unitCode(record.productUnitId) }}</td>
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
            <div v-if="false && activeView === 'racks'" class="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Post-line logistics') }}</p><h3>{{ t('Racks are not the WIP anchor') }}</h3></div></div>
                <p class="mt-4 text-slate-600 dark:text-slate-300">{{ t('Racks only aggregate supports after the controlled line. The support remains the traceability reference for intra-line WIP.') }}</p>
                <div class="mt-5 grid gap-4">
                  <div v-for="rack in racks" :key="rack.id" class="rounded-2xl border border-slate-200 p-4 dark:border-slate-700">
                    <div class="flex items-center justify-between"><strong>{{ rack.rackCode }}</strong><span :class="statusClass(rack.status)">{{ translateStatus(rack.status) }}</span></div>
                    <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">{{ sectionName(rack.sectionId) }}</p>
                  </div>
                </div>
              </section>
              <section class="card overflow-hidden p-6">
                <div class="section-heading"><div><p>{{ t('Extended view') }}</p><h3>{{ t('Subproduct to final assembly concept') }}</h3></div></div>
                <img :src="lineCarUrl" alt="Automotive production line" class="mt-5 rounded-3xl border border-slate-200 dark:border-slate-700" />
              </section>
            </div>

            <!-- EVENTS VIEW -->
            <div v-if="activeView === 'events'" class="grid gap-5 2xl:grid-cols-2">
              <section class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Simulation') }}</p><h3>{{ t('Execute event playback') }}</h3></div></div>
                <p class="mt-3 text-slate-600 dark:text-slate-300">{{ t('Advances supports across the nominal door production line and updates the audit trail.') }}</p>
                <button class="btn-primary mt-5" @click="executePlayback">{{ t('Execute playback scenario') }}</button>
              </section>
              <section class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Controlled event') }}</p><h3>{{ t('Inject manual factory event') }}</h3></div></div>
                <div class="mt-5 grid gap-4">
                  <label class="form-label">{{ t('Event type') }}<input v-model="manualEvent.eventType" class="form-input" /></label>
                  <label class="form-label">{{ t('Support code') }}<input v-model="manualEvent.supportCode" class="form-input" /></label>
                  <label class="form-label">{{ t('Section / Rack code') }}<input v-model="manualEvent.sectionCode" class="form-input" /></label>
                  <label class="form-label">{{ t('Unit code') }}<input v-model="manualEvent.productUnitCode" class="form-input" /></label>
                  <label class="form-label">{{ t('Result') }}<input v-model="manualEvent.result" class="form-input" /></label>
                  <label class="form-label">{{ t('Notes') }}<textarea v-model="manualEvent.notes" class="form-input min-h-24"></textarea></label>
                </div>
                <button class="btn-primary mt-5" @click="injectManualEvent">{{ t('Inject manual event') }}</button>
              </section>
              <p v-if="eventStatus" class="2xl:col-span-2 rounded-2xl border border-slate-200 bg-white p-4 font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300">{{ eventStatus }}</p>
              <section class="2xl:col-span-2 card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Audit trail') }}</p><h3>{{ t('Support localization history') }}</h3></div></div>
                <p class="mt-3 text-sm text-slate-600 dark:text-slate-300">{{ t('Support localization history is generated by movements and is read-only in this interface.') }}</p>
                <div class="table-shell">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Support') }}</th><th>{{ t('Section') }}</th><th>{{ t('Event') }}</th><th>{{ t('Timestamp') }}</th></tr></thead>
                    <tbody>
                      <tr v-for="history in supportHistory" :key="history.id">
                        <td>{{ supportCode(history.supportId) }}</td>
                        <td>{{ sectionName(history.sectionId) }}</td>
                        <td>{{ translateStatus(history.eventType) }}</td>
                        <td>{{ formatDate(history.dateTime) }}</td>
                      </tr>
                      <tr v-if="!supportHistory.length"><td colspan="4" class="text-center">{{ t('No records found') }}</td></tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- PREDICTIONS VIEW -->
            <div v-if="false && activeView === 'predictions'" class="space-y-6">
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Predictions') }}</p><h3>{{ t('Operational forecasts') }}</h3></div></div>
                <p class="mt-3 text-slate-600 dark:text-slate-300">{{ t('This section prepares future analysis of completion times, delay risk and productive deviations. In this V1 the data is demonstrative.') }}</p>
                <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Order') }}</th><th>{{ t('Model') }}</th><th>{{ t('Type') }}</th><th>{{ t('Last update') }}</th><th>{{ t('Confidence') }}</th><th>{{ t('Status') }}</th></tr></thead>
                    <tbody>
                      <tr v-for="prediction in predictions" :key="prediction.id">
                        <td>{{ prediction.manufacturingOrderId ? 'MO-' + prediction.manufacturingOrderId : '-' }}</td>
                        <td>{{ prediction.modelVersion }}</td>
                        <td>{{ t(prediction.modelType) }}</td>
                        <td>{{ formatDate(prediction.lastDate) }}</td>
                        <td>{{ formatConfidence(prediction.confidence) }}</td>
                        <td><span :class="statusClass(prediction.status)">{{ translateStatus(prediction.status) }}</span></td>
                      </tr>
                      <tr v-if="!predictions.length"><td colspan="6" class="text-center">{{ t('No predictions available yet.') }}</td></tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- USERS VIEW -->
            <div v-if="activeView === 'users' && canManageUsers" class="space-y-5 lg:space-y-6">
              <section class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Users') }}</p><h3>{{ editingUsername ? t('Editing user') : t('User Management') }}</h3></div></div>
                <form @submit.prevent="registerUser(false)" class="mt-5 grid gap-4 lg:grid-cols-2">
                  <label class="form-label">{{ t('Name / full name') }}<input v-model="registerForm.name" class="form-input" /></label>
                  <label class="form-label">{{ t('Username') }}<input v-model="registerForm.username" class="form-input disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500 dark:disabled:bg-slate-800" :disabled="editingUsername !== null" /></label>
                  <label class="form-label">{{ t('Email') }}<input v-model="registerForm.email" type="email" class="form-input" /></label>
                  <label class="form-label">{{ t('Role') }}
                    <select v-model="registerForm.roleKey" class="form-input disabled:cursor-not-allowed disabled:bg-slate-100 disabled:text-slate-500 dark:disabled:bg-slate-800" :disabled="editingUsername === defaultUser.username">
                      <option value="admin">{{ t('Administrator') }}</option>
                      <option value="operator">{{ t('Operator') }}</option>
                      <option value="client">{{ t('Client') }}</option>
                    </select>
                  </label>
                  <label class="form-label">{{ t('Password') }}<input v-model="registerForm.password" type="password" class="form-input" /></label>
                  <label class="form-label">{{ t('Confirm password') }}<input v-model="registerForm.confirmPassword" type="password" class="form-input" /></label>
                  <p v-if="editingUsername" class="lg:col-span-2 text-sm text-slate-500 dark:text-slate-400">{{ t('Leave password empty to keep current password') }}</p>
                  <div class="lg:col-span-2 flex flex-wrap items-center gap-3">
                    <button class="btn-primary" type="submit">{{ editingUsername ? t('Update user') : t('Create user') }}</button>
                    <button v-if="editingUsername" class="btn-secondary" type="button" @click="resetUserForm()">{{ t('Cancel') }}</button>
                    <span v-if="registerError" class="text-sm font-semibold text-red-600">{{ registerError }}</span>
                    <span v-if="registerSuccess" class="text-sm font-semibold text-green-600">{{ registerSuccess }}</span>
                  </div>
                </form>
              </section>
              <section class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Users') }}</p><h3>{{ t('Existing users') }}</h3></div></div>
                <div class="table-shell">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Username') }}</th><th>{{ t('Name') }}</th><th>{{ t('Email') }}</th><th>{{ t('Role') }}</th><th>{{ t('Status') }}</th><th>{{ t('Last update') }}</th><th></th></tr></thead>
                    <tbody>
                      <tr v-for="profile in users" :key="profile.username">
                        <td class="font-bold">{{ profile.username }}</td>
                        <td>{{ profile.name }}</td>
                        <td>{{ profile.email }}</td>
                        <td>{{ getRoleLabel(profile.roleKey) }}</td>
                        <td><span :class="statusClass(profile.active ? 'Active' : 'Blocked')">{{ profile.active ? t('Active') : t('Blocked') }}</span></td>
                        <td>{{ formatDate(profile.lastLogin) }}</td>
                        <td>
                          <div class="flex flex-wrap gap-2">
                            <button class="btn-secondary" @click="startEditUser(profile)">{{ t('Edit') }}</button>
                            <button v-if="profile.username !== 'admin'" class="btn-secondary" @click="toggleUserActive(profile)">{{ profile.active ? t('Block') : t('Unblock') }}</button>
                            <button v-if="profile.username !== 'admin'" class="btn-secondary" @click="removeUser(profile.username)">{{ t('Delete') }}</button>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- FIWARE VIEW -->
            <div v-if="activeView === 'fiware'" class="space-y-5 lg:space-y-6">
              <section class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Context broker boundary') }}</p><h3>{{ t('Current NGSI-LD-style context') }}</h3></div></div>
                <p class="mt-3 max-w-3xl text-slate-600 dark:text-slate-300">{{ t('The relational backend remains the business source of truth. Orion-LD is used for current/hot context of supports, product units and racks.') }}</p>
                <div class="mt-5 flex flex-wrap gap-3">
                  <button class="btn-secondary" :disabled="fiwareLoading" @click="refreshFiwareContext()">{{ t('Refresh context') }}</button>
                  <button class="btn-primary" :disabled="fiwareLoading" @click="publishFiware">{{ t('Publish current context to Orion-LD') }}</button>
                </div>
                <div class="mt-5 grid gap-3 md:grid-cols-4">
                  <div class="rounded-2xl border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-900/40">
                    <p class="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{{ t('Connection status') }}</p>
                    <p class="mt-2 text-sm font-black">{{ fiwareConnectionText() }}</p>
                  </div>
                  <div class="rounded-2xl border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-900/40">
                    <p class="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{{ t('Source') }}</p>
                    <p class="mt-2 text-sm font-black">{{ fiwareSourceText() }}</p>
                  </div>
                  <div class="rounded-2xl border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-900/40">
                    <p class="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{{ t('Entities') }}</p>
                    <p class="mt-2 text-sm font-black">{{ fiwareContext.entityCount }}</p>
                  </div>
                  <div class="rounded-2xl border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-900/40">
                    <p class="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{{ t('Last update') }}</p>
                    <p class="mt-2 text-sm font-black">{{ fiwareContext.timestamp ? formatDate(fiwareContext.timestamp) : '-' }}</p>
                  </div>
                </div>
                <p v-if="fiwareActionStatus" class="mt-4 rounded-2xl border border-slate-200 bg-white p-4 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200">{{ fiwareActionStatus }}</p>
                <p class="mt-3 text-sm text-slate-600 dark:text-slate-300">{{ fiwareSummaryMessage() }}</p>
                <p v-if="fiwareContext.errors.length" class="mt-3 rounded-2xl border border-red-200 bg-red-50 p-3 text-sm font-semibold text-red-700 dark:border-red-700/70 dark:bg-red-900/30 dark:text-red-100">{{ fiwareContext.errors.join(' | ') }}</p>
              </section>

              <section class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('FIWARE entities') }}</p><h3>{{ t('Published context entities') }}</h3></div></div>
                <div class="table-shell">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Type') }}</th><th>{{ t('ID') }}</th><th>{{ t('Main attributes') }}</th></tr></thead>
                    <tbody>
                      <tr v-for="entity in fiwareContext.entities" :key="entity.id">
                        <td>{{ entity.type }}</td>
                        <td class="font-mono text-xs">{{ entity.id }}</td>
                        <td>{{ fiwareAttributesPreview(entity.attributes) }}</td>
                      </tr>
                      <tr v-if="!fiwareContext.entities.length"><td colspan="3" class="text-center">{{ t('No FIWARE entities available.') }}</td></tr>
                    </tbody>
                  </table>
                </div>
              </section>

              <section v-if="fiwareLastPublish" class="card p-5 sm:p-6">
                <div class="section-heading"><div><p>{{ t('Publish summary') }}</p><h3>{{ t('Last publish result') }}</h3></div></div>
                <div class="mt-4 grid gap-3 md:grid-cols-4">
                  <div class="rounded-2xl border border-slate-200 p-3 dark:border-slate-700">
                    <p class="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{{ t('Attempted') }}</p>
                    <p class="mt-2 text-sm font-black">{{ fiwareLastPublish.attemptedCount }}</p>
                  </div>
                  <div class="rounded-2xl border border-slate-200 p-3 dark:border-slate-700">
                    <p class="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{{ t('Published') }}</p>
                    <p class="mt-2 text-sm font-black">{{ fiwareLastPublish.publishedCount }}</p>
                  </div>
                  <div class="rounded-2xl border border-slate-200 p-3 dark:border-slate-700">
                    <p class="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{{ t('Failed') }}</p>
                    <p class="mt-2 text-sm font-black">{{ fiwareLastPublish.failedCount }}</p>
                  </div>
                  <div class="rounded-2xl border border-slate-200 p-3 dark:border-slate-700">
                    <p class="text-xs font-bold uppercase tracking-[0.12em] text-slate-500 dark:text-slate-400">{{ t('Stale removed') }}</p>
                    <p class="mt-2 text-sm font-black">{{ fiwareLastPublish.staleDeletedCount }}</p>
                  </div>
                </div>
                <p v-if="fiwareLastPublish.staleEntityIds.length" class="mt-3 rounded-2xl border border-slate-200 bg-slate-50 p-3 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"><span class="font-black">{{ t('Stale entity IDs') }}:</span> {{ fiwareLastPublish.staleEntityIds.join(' | ') }}</p>
                <p v-if="fiwareLastPublish.errors.length" class="mt-3 rounded-2xl border border-red-200 bg-red-50 p-3 text-sm font-semibold text-red-700 dark:border-red-700/70 dark:bg-red-900/30 dark:text-red-100"><span class="font-black">{{ t('Synchronization errors') }}:</span> {{ fiwareLastPublish.errors.join(' | ') }}</p>
              </section>
            </div>
            </template>
          </div>
        </section>
      </main>
    </div>
  </div>
</template>
