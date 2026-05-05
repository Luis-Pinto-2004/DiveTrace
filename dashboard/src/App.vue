<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import logoUrl from './assets/branding/drivolution-logo.png'
import lineDoorUrl from './assets/branding/production-line-door.png'
import lineCarUrl from './assets/branding/production-line-car.png'
import { apiGet, apiPost } from './services/api'
import {
  demoDashboard,
  demoLots,
  demoMaterials,
  demoOrders,
  demoQuality,
  demoPredictions,
  demoRacks,
  demoSupports,
  demoUnits,
  type DashboardSummary,
  type LotRawMaterial,
  type ManufacturingOrder,
  type ProductUnit,
  type QualityRecord,
  type PredictionRecord,
  type Rack,
  type RawMaterial,
  type Support,
} from './data/demo'
// Import preferences (i18n and theme) management
import { locale, setLocale, theme, toggleTheme, t, translateStatus, translateSectionName, translateMaterialName, translateUnitType, translateQualityResult } from './prefs'

// ===== Authentication and user management =====
const isAuthenticated = ref(false)

type UserProfile = {
  id: string
  name: string
  username: string
  email: string
  role: string
  roleKey: 'admin' | 'operator' | 'client'
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
  jobTitle: 'System Administrator',
  password: 'admin',
  active: true,
}

const users = ref<UserProfile[]>([])
const user = ref<UserProfile | null>(null)
const isRegistering = ref(false)
const loginForm = ref({ username: '', password: '' })
const registerForm = ref({ name: '', username: '', email: '', password: '', confirmPassword: '', roleKey: 'operator' as 'admin' | 'operator' | 'client' })
const loginError = ref('')
const registerError = ref('')
const registerSuccess = ref('')

function roleLabel(roleKey: UserProfile['roleKey']) {
  if (roleKey === 'admin') return t('Administrator')
  if (roleKey === 'client') return t('Client')
  return t('Operator')
}

function buildUserRole(roleKey: UserProfile['roleKey']) {
  if (roleKey === 'admin') return 'Administrador'
  if (roleKey === 'client') return 'Cliente'
  return 'Funcionário/Operador'
}

function persistUsers() {
  localStorage.setItem('users', JSON.stringify(users.value))
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

function isAdmin() {
  return user.value?.roleKey === 'admin' || user.value?.username === 'admin'
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
    user.value = found
    isAuthenticated.value = true
    localStorage.setItem('user', JSON.stringify(found))
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
  const { name, username, email, password, confirmPassword, roleKey } = registerForm.value
  if (!name.trim() || !username.trim() || !email.trim() || !password || !confirmPassword) {
    registerError.value = t('All fields are required')
    return
  }
  if (password !== confirmPassword) {
    registerError.value = t('Passwords do not match')
    return
  }
  if (users.value.some((candidate) => candidate.username.toLowerCase() === username.trim().toLowerCase())) {
    registerError.value = t('Username already exists')
    return
  }
  const created: UserProfile = {
    id: username.trim(),
    name: name.trim(),
    username: username.trim(),
    email: email.trim(),
    role: buildUserRole(roleKey),
    roleKey,
    organization: 'DRIVOLUTION WP3',
    project: 'DriveTrace Core',
    jobTitle: roleKey === 'admin' ? 'System Administrator' : roleKey === 'client' ? 'Cliente' : 'Operador',
    password,
    active: true,
  }
  users.value.push(created)
  persistUsers()
  registerSuccess.value = t('User saved')
  registerForm.value = { name: '', username: '', email: '', password: '', confirmPassword: '', roleKey: 'operator' }
  if (autoLogin) {
    user.value = created
    isAuthenticated.value = true
    localStorage.setItem('user', JSON.stringify(created))
  } else {
    isRegistering.value = false
  }
}

function removeUser(username: string) {
  if (username === defaultUser.username) return
  users.value = users.value.filter((candidate) => candidate.username !== username)
  persistUsers()
}

function toggleUserActive(profile: UserProfile) {
  if (profile.username === defaultUser.username) return
  profile.active = !profile.active
  persistUsers()
}

/**
 * Log out the current user. Clears localStorage and resets the auth state.
 */
function logout() {
  localStorage.removeItem('user')
  user.value = null
  isAuthenticated.value = false
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
        user.value = current
        isAuthenticated.value = true
      } else {
        localStorage.removeItem('user')
      }
    } catch {
      localStorage.removeItem('user')
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
  | 'predictions'

const activeView = ref<ViewKey>('overview')
const loading = ref(true)
const apiStatus = ref('Connecting to API...')
const eventStatus = ref('')

const summary = ref<DashboardSummary>(demoDashboard)
const orders = ref<ManufacturingOrder[]>(demoOrders)
const units = ref<ProductUnit[]>(demoUnits)
const supports = ref<Support[]>(demoSupports)
const racks = ref<Rack[]>(demoRacks)
const materials = ref<RawMaterial[]>(demoMaterials)
const lots = ref<LotRawMaterial[]>(demoLots)
const quality = ref<QualityRecord[]>(demoQuality)
const predictions = ref<PredictionRecord[]>(demoPredictions)
const fiwareContext = ref<unknown[]>([])

const manualEvent = ref({
  eventType: 'MoveSupport',
  supportCode: 'SUP-005',
  sectionCode: 'SEC-WELD',
  productUnitCode: 'DU-005',
  result: 'PASS',
  notes: 'Manual controlled event from dashboard demo.',
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
  { key: 'users', label: 'Users', icon: 'USR' },
] as const

const sectionsById = computed(() => new Map(summary.value.wipBySection.map((section) => [section.sectionId, section])))
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
  return sectionId ? translateSectionName(sectionsById.value.get(sectionId)?.section || `Section ${sectionId}`) : t('Not assigned')
}

function supportCode(supportId?: number) {
  return supportId ? supportsById.value.get(supportId)?.supportCode || `SUP ${supportId}` : 'No support'
}

function unitCode(unitId?: number) {
  return unitId ? unitsById.value.get(unitId)?.unitCode || `Unit ${unitId}` : 'Unknown unit'
}

function materialName(materialId: number) {
  return translateMaterialName(materials.value.find((material) => material.id === materialId)?.name || `Material ${materialId}`)
}

async function loadData() {
  loading.value = true
  const [dashboardData, orderData, unitData, supportData, rackData, materialData, lotData, qualityData, predictionData, contextData] = await Promise.all([
    apiGet('/dashboard/summary', demoDashboard),
    apiGet('/manufacturing-orders', demoOrders),
    apiGet('/product-units', demoUnits),
    apiGet('/supports', demoSupports),
    apiGet('/racks', demoRacks),
    apiGet('/raw-materials', demoMaterials),
    apiGet('/lot-raw-materials', demoLots),
    apiGet('/quality-results', demoQuality),
    apiGet('/predictions', demoPredictions),
    apiGet('/fiware/context', [] as unknown[]),
  ])

  summary.value = dashboardData
  orders.value = orderData
  units.value = unitData
  supports.value = supportData
  racks.value = rackData
  materials.value = materialData
  lots.value = lotData
  quality.value = qualityData
  predictions.value = predictionData
  fiwareContext.value = contextData
  apiStatus.value = dashboardData === demoDashboard ? 'Offline demo data loaded' : 'Connected to DriveTrace Core API'
  loading.value = false
}

async function executePlayback() {
  eventStatus.value = t('Executing playback...')
  try {
    await apiPost('/events/playback', { scenario: 'door-line-demo' })
    eventStatus.value = t('Playback executed. Dashboard data refreshed.')
    await loadData()
  } catch (error) {
    eventStatus.value = 'API unavailable. Playback endpoint is ready but could not be reached from the browser.'
  }
}

async function injectManualEvent() {
  eventStatus.value = t('Sending manual controlled event...')
  try {
    await apiPost('/events/manual', manualEvent.value)
    eventStatus.value = t('Manual event accepted. Dashboard data refreshed.')
    await loadData()
  } catch (error) {
    eventStatus.value = 'API unavailable or event rejected. Check Swagger for the exact backend response.'
  }
}

async function publishFiware() {
  eventStatus.value = t('Publishing current context to Orion-LD...')
  try {
    await apiPost('/fiware/publish-current', {})
    eventStatus.value = t('FIWARE publishing request executed. If Orion-LD is running, check port 1026.')
    fiwareContext.value = await apiGet('/fiware/context', fiwareContext.value)
  } catch (error) {
    eventStatus.value = 'Could not reach the FIWARE publish endpoint. The relational demo remains usable.'
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
    <div v-else class="relative">
      <!-- SIDEBAR -->
      <aside class="fixed inset-y-0 left-0 hidden w-80 border-r border-slate-200 bg-white/95 px-6 py-6 shadow-sm dark:bg-slate-800 dark:border-slate-700 lg:block">
        <div class="mb-8 flex items-center gap-4">
          <img :src="logoUrl" alt="DRIVOLUTION logo" class="h-12 w-auto object-contain" />
          <div>
            <p class="text-xs font-bold uppercase tracking-[0.18em] text-drivolution-700">DRIVOLUTION WP3</p>
            <h1 class="text-xl font-black tracking-tight">DriveTrace Core</h1>
          </div>
        </div>
        <nav class="space-y-2">
          <button
            v-for="item in nav"
            v-show="item.key !== 'users' || isAdmin()"
            :key="item.key"
            class="nav-item"
            :class="activeView === item.key ? 'nav-item-active' : ''"
            @click="activeView = item.key; showUserMenu = false"
          >
            <span class="nav-icon">{{ item.icon }}</span>
            <span>{{ t(item.label) }}</span>
          </button>
        </nav>
        <!-- Quick settings / user info section replacing the academic scope block -->
        <div class="absolute bottom-6 left-6 right-6 rounded-3xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600 dark:bg-slate-700 dark:border-slate-600 dark:text-slate-300">
          <p class="font-semibold text-slate-950 dark:text-slate-50">{{ t('Active profile') }}: {{ user?.name }}</p>
          <p class="mt-1">{{ t('Language') }}: {{ locale === 'pt-PT' ? t('Portuguese') : t('English') }}</p>
          <p class="mt-1">{{ t('Theme') }}: {{ theme === 'dark' ? t('Dark') : t('Light') }}</p>
          <div class="mt-3 flex gap-2">
            <button class="btn-secondary flex-1" @click="activeView = 'profile'">{{ t('Profile') }}</button>
            <button class="btn-secondary flex-1" @click="activeView = 'settings'">{{ t('Settings') }}</button>
          </div>
          <button class="btn-secondary mt-2 w-full" @click="logout">{{ t('Logout') }}</button>
        </div>
      </aside>

      <main class="lg:pl-80">
        <!-- TOP BAR -->
        <header class="sticky top-0 z-20 border-b border-slate-200 bg-white/90 px-6 py-4 backdrop-blur dark:bg-slate-800 dark:border-slate-700">
          <div class="flex flex-col justify-between gap-4 xl:flex-row xl:items-center">
            <div class="flex items-center gap-4">
              <img :src="logoUrl" alt="DRIVOLUTION logo" class="h-10 w-10 rounded-xl object-contain lg:hidden" />
              <div>
                <p class="text-xs font-bold uppercase tracking-[0.12em] text-drivolution-700 dark:text-drivolution-300">{{ t('WIP Traceability and Monitoring Platform') }}</p>
                <h2 class="mt-1 text-2xl font-black tracking-tight">{{ t(nav.find((item) => item.key === activeView)?.label || '') }}</h2>
              </div>
            </div>
            <div class="flex flex-wrap items-center gap-3 relative">
              <span class="rounded-full bg-slate-100 px-4 py-2 text-sm font-semibold text-slate-600 dark:bg-slate-700 dark:text-slate-200">{{ apiStatus }}</span>
              <button class="btn-secondary" @click="loadData">{{ t('Refresh') }}</button>
              <!-- Language selector (small button) -->
              <select v-model="locale" class="rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-medium text-slate-700 dark:bg-slate-700 dark:text-slate-200 dark:border-slate-600">
                <option value="pt-PT">{{ t('Portuguese') }}</option>
                <option value="en">{{ t('English') }}</option>
              </select>
              <!-- Theme toggle button -->
              <button class="btn-secondary" @click="toggleTheme">
                {{ theme === 'dark' ? t('Light mode') : t('Dark mode') }}
              </button>
              <!-- User avatar and menu -->
              <div class="relative">
                <button @click="showUserMenu = !showUserMenu" class="flex items-center gap-2 rounded-full bg-slate-100 px-3 py-2 text-sm font-semibold text-slate-700 shadow-sm dark:bg-slate-700 dark:text-slate-200">
                  <span class="inline-flex h-6 w-6 items-center justify-center rounded-full bg-drivolution-500 text-white text-xs font-black">{{ user?.name.charAt(0) }}</span>
                  <span class="hidden sm:inline-block">{{ user?.name }}</span>
                </button>
                <div v-if="showUserMenu" class="absolute right-0 mt-2 w-40 rounded-lg border border-slate-200 bg-white shadow-lg dark:bg-slate-700 dark:border-slate-600 z-30">
                  <button class="w-full px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-600" @click="activeView = 'profile'; showUserMenu = false">{{ t('Profile') }}</button>
                  <button class="w-full px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-600" @click="activeView = 'settings'; showUserMenu = false">{{ t('Settings') }}</button>
                  <button class="w-full px-4 py-2 text-left text-sm hover:bg-slate-100 dark:hover:bg-slate-600" @click="logout(); showUserMenu = false">{{ t('Logout') }}</button>
                </div>
              </div>
            </div>
          </div>
          <!-- Mobile tab navigation -->
          <div class="mt-4 flex gap-2 overflow-x-auto pb-1 lg:hidden">
            <button v-for="item in nav" v-show="item.key !== 'users' || isAdmin()" :key="item.key" class="mobile-tab" :class="activeView === item.key ? 'mobile-tab-active' : ''" @click="activeView = item.key; showUserMenu = false">
              {{ t(item.label) }}
            </button>
          </div>
        </header>
        <!-- MAIN CONTENT -->
        <section class="px-6 py-6">
          <div v-if="loading" class="card p-8 text-center text-slate-600 dark:bg-slate-800 dark:text-slate-200">{{ t('Loading DriveTrace Core data...') }}</div>
          <template v-else>
            <!-- PROFILE VIEW -->
            <div v-if="activeView === 'profile'" class="card p-6 space-y-4">
              <div class="section-heading"><div><p>{{ t('Profile') }}</p><h3>{{ t('Profile') }}</h3></div></div>
              <p><strong>{{ t('Name') }}:</strong> {{ user?.name }}</p>
              <p><strong>{{ t('Username') }}:</strong> {{ user?.username }}</p>
              <p><strong>Email:</strong> {{ user?.email }}</p>
              <p><strong>{{ t('Role') }}:</strong> {{ user ? roleLabel(user.roleKey) : '' }}</p>
              <p><strong>{{ t('Organisation') }}:</strong> {{ user?.organization }}</p>
              <p><strong>{{ t('Project') }}:</strong> {{ user?.project }}</p>
              <p><strong>{{ t('Job Title') }}:</strong> {{ user?.jobTitle }}</p>
              <button class="btn-secondary" @click="activeView = 'overview'">{{ t('Overview') }}</button>
            </div>

            <!-- SETTINGS VIEW -->
            <div v-if="activeView === 'settings'" class="card p-6 space-y-6">
              <div class="section-heading"><div><p>{{ t('Settings') }}</p><h3>{{ t('Application Settings') }}</h3></div></div>
              <div>
                <h4 class="font-bold mb-2">{{ t('Language') }}</h4>
                <div class="flex gap-2">
                  <button class="btn-secondary flex-1" :class="{ 'nav-item-active': locale === 'pt-PT' }" @click="setLocale('pt-PT')">{{ t('Portuguese') }}</button>
                  <button class="btn-secondary flex-1" :class="{ 'nav-item-active': locale === 'en' }" @click="setLocale('en')">{{ t('English') }}</button>
                </div>
              </div>
              <div>
                <h4 class="font-bold mb-2">{{ t('Theme') }}</h4>
                <div class="flex gap-2">
                  <button class="btn-secondary flex-1" :class="{ 'nav-item-active': theme === 'light' }" @click="setTheme('light')">{{ t('Light') }}</button>
                  <button class="btn-secondary flex-1" :class="{ 'nav-item-active': theme === 'dark' }" @click="setTheme('dark')">{{ t('Dark') }}</button>
                </div>
              </div>
              <div>
                <h4 class="font-bold mb-2">{{ t('Accessibility') }}</h4>
                <p class="text-sm text-slate-600 dark:text-slate-300">Esta versão demonstra opções básicas. Futuras iterações podem oferecer alto contraste ou densidade compacta.</p>
              </div>
              <button class="btn-secondary" @click="activeView = 'overview'">{{ t('Overview') }}</button>
            </div>

            <!-- OVERVIEW (DASHBOARD) VIEW -->
            <div v-if="activeView === 'overview'" class="space-y-6">
              <section class="hero-card overflow-hidden">
                <div class="grid gap-8 xl:grid-cols-[1.1fr_0.9fr] xl:items-center">
                  <div>
                    <p class="text-sm font-bold uppercase tracking-[0.3em] text-drivolution-700">{{ t('Automotive WIP traceability') }}</p>
                    <h3 class="mt-3 max-w-3xl text-4xl font-black tracking-tight">{{ t('DriveTrace Core monitors product units through their physical supports.') }}</h3>
                    <p class="mt-4 max-w-2xl text-base leading-7 text-slate-600 dark:text-slate-300">
                      {{ t('The V1 uses a controlled door production line: raw materials, support assignment, stamping, welding, painting, quality control and post-line rack storage.') }}
                    </p>
                    <div class="mt-6 flex flex-wrap gap-3">
                      <span class="domain-pill">{{ t('ProductUnit-centred traceability') }}</span>
                      <span class="domain-pill">{{ t('Support as intra-line anchor') }}</span>
                      <span class="domain-pill">{{ t('Rack as post-line logistics') }}</span>
                      <span class="domain-pill">{{ t('Material lot genealogy') }}</span>
                    </div>
                  </div>
                  <img :src="lineDoorUrl" alt="Door production line" class="rounded-3xl border border-slate-200 bg-white object-cover shadow-sm dark:bg-slate-800 dark:border-slate-700" />
                </div>
              </section>
              <section class="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
                <div class="metric-card"><span>{{ t('Open orders') }}</span><strong>{{ summary.counts.openOrders }}</strong></div>
                <div class="metric-card"><span>{{ t('Active units') }}</span><strong>{{ summary.counts.activeUnits }}</strong></div>
                <div class="metric-card"><span>{{ t('Active supports') }}</span><strong>{{ summary.counts.activeSupports }}</strong></div>
                <div class="metric-card"><span>{{ t('Quality issues') }}</span><strong>{{ summary.counts.qualityIssues }}</strong></div>
                <div class="metric-card"><span>{{ t('Rack assignments') }}</span><strong>{{ summary.counts.rackAssignments }}</strong></div>
              </section>
              <section class="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
                <div class="card p-6">
                  <div class="section-heading">
                    <div>
                      <p>{{ t('Line state') }}</p>
                      <h3>{{ t('Current WIP by section') }}</h3>
                    </div>
                  </div>
                  <div class="mt-5 space-y-4">
                    <div v-for="section in summary.wipBySection" :key="section.sectionCode" class="line-step">
                      <div>
                        <p class="font-bold text-slate-950 dark:text-slate-50">{{ section.section }}</p>
                        <p class="text-xs uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">{{ section.sectionType }}</p>
                      </div>
                      <div class="flex min-w-40 items-center gap-3">
                        <div class="h-2 flex-1 rounded-full bg-slate-200 dark:bg-slate-700">
                          <div class="h-2 rounded-full bg-drivolution-500" :style="{ width: `${Math.min(100, section.productUnits * 28)}%` }"></div>
                        </div>
                        <span class="text-sm font-black text-slate-950 dark:text-slate-50">{{ section.productUnits }}</span>
                      </div>
                    </div>
                  </div>
                </div>
                <div class="card p-6">
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
                      <p class="mt-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-400 dark:text-slate-500">{{ translateStatus(alert.severity) }}</p>
                    </div>
                  </div>
                </div>
              </section>
              <section class="card p-6">
                <div class="section-heading">
                  <div>
                    <p>{{ t('Recent events') }}</p>
                    <h3>{{ t('Support movement and audit trail') }}</h3>
                  </div>
                </div>
                <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Support') }}</th><th>{{ t('Section') }}</th><th>{{ t('Event') }}</th><th>{{ t('Timestamp') }}</th></tr></thead>
                    <tbody>
                      <tr v-for="event in summary.recentEvents" :key="`${event.supportCode}-${event.dateTime}`">
                        <td>{{ event.supportCode }}</td>
                        <td>{{ translateSectionName(event.section) }}</td>
                        <td>{{ translateStatus(event.eventType) }}</td>
                        <td>{{ new Date(event.dateTime).toLocaleString() }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- ORDERS VIEW -->
            <div v-if="activeView === 'orders'" class="card p-6">
              <div class="section-heading"><div><p>{{ t('Planning') }}</p><h3>{{ t('Manufacturing orders') }}</h3></div></div>
              <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                <table class="data-table">
                  <thead><tr><th>{{ t('Order') }}</th><th>{{ t('Status') }}</th><th>{{ t('Planned qty') }}</th><th>{{ t('Scheduled until') }}</th><th>{{ t('Notes') }}</th></tr></thead>
                  <tbody>
                    <tr v-for="order in orders" :key="order.id">
                      <td class="font-bold">{{ order.orderNumber }}</td>
                      <td><span :class="statusClass(order.status)">{{ translateStatus(order.status) }}</span></td>
                      <td>{{ order.plannedQty }}</td>
                      <td>{{ new Date(order.scheduledUntil).toLocaleString() }}</td>
                      <td>{{ order.observations }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <!-- UNITS VIEW -->
            <div v-if="activeView === 'units'" class="space-y-6">
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
            <div v-if="activeView === 'supports'" class="space-y-6">
              <section class="card overflow-hidden p-6">
                <div class="section-heading"><div><p>{{ t('Physical tracking') }}</p><h3>{{ t('Support is the intra-line anchor') }}</h3></div></div>
                <img :src="lineDoorUrl" alt="Support-based line" class="mt-5 rounded-3xl border border-slate-200 dark:border-slate-700" />
              </section>
              <section class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                <div v-for="support in supports" :key="support.id" class="card p-5">
                  <div class="flex items-start justify-between gap-4">
                    <div><p class="text-sm font-bold uppercase tracking-[0.2em] text-slate-500 dark:text-slate-400">Support</p><h3 class="mt-1 text-xl font-black">{{ support.supportCode }}</h3></div>
                    <span :class="statusClass(support.status)">{{ translateStatus(support.status) }}</span>
                  </div>
                  <p class="mt-4 text-sm text-slate-600 dark:text-slate-300">{{ t('Current section') }}</p>
                  <p class="text-base font-bold text-slate-950 dark:text-slate-50">{{ sectionName(support.currentSectionId) }}</p>
                </div>
              </section>
            </div>

            <!-- MATERIALS VIEW -->
            <div v-if="activeView === 'materials'" class="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
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
                        <td>{{ lot.lotQuantity }} {{ lot.lotUnit }}</td>
                        <td>{{ sectionName(lot.sectionId) }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- QUALITY VIEW -->
            <div v-if="activeView === 'quality'" class="space-y-6">
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
                        <td>{{ new Date(record.recordedAt).toLocaleString() }}</td>
                        <td>{{ record.notes }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- RACKS VIEW -->
            <div v-if="activeView === 'racks'" class="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
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
            <div v-if="activeView === 'events'" class="grid gap-6 xl:grid-cols-2">
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Simulation') }}</p><h3>{{ t('Execute event playback') }}</h3></div></div>
                <p class="mt-3 text-slate-600 dark:text-slate-300">{{ t('Advances supports across the nominal door production line and updates the audit trail.') }}</p>
                <button class="btn-primary mt-5" @click="executePlayback">{{ t('Execute playback scenario') }}</button>
              </section>
              <section class="card p-6">
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
              <p v-if="eventStatus" class="xl:col-span-2 rounded-2xl border border-slate-200 bg-white p-4 font-semibold text-slate-700 dark:bg-slate-800 dark:border-slate-700 dark:text-slate-300">{{ eventStatus }}</p>
            </div>

            <!-- PREDICTIONS VIEW -->
            <div v-if="activeView === 'predictions'" class="space-y-6">
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
                        <td>{{ prediction.modelType }}</td>
                        <td>{{ new Date(prediction.lastDate).toLocaleString() }}</td>
                        <td>{{ prediction.confidence ? Math.round(prediction.confidence * 100) + '%' : '-' }}</td>
                        <td><span :class="statusClass(prediction.status)">{{ translateStatus(prediction.status) }}</span></td>
                      </tr>
                      <tr v-if="!predictions.length"><td colspan="6" class="text-center">{{ t('No predictions available yet.') }}</td></tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- USERS VIEW -->
            <div v-if="activeView === 'users' && isAdmin()" class="space-y-6">
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Users') }}</p><h3>{{ t('User Management') }}</h3></div></div>
                <form @submit.prevent="registerUser(false)" class="mt-5 grid gap-4 lg:grid-cols-2">
                  <label class="form-label">{{ t('Name / full name') }}<input v-model="registerForm.name" class="form-input" /></label>
                  <label class="form-label">{{ t('Username') }}<input v-model="registerForm.username" class="form-input" /></label>
                  <label class="form-label">{{ t('Email') }}<input v-model="registerForm.email" type="email" class="form-input" /></label>
                  <label class="form-label">{{ t('Role') }}
                    <select v-model="registerForm.roleKey" class="form-input">
                      <option value="admin">{{ t('Administrator') }}</option>
                      <option value="operator">{{ t('Operator') }}</option>
                      <option value="client">{{ t('Client') }}</option>
                    </select>
                  </label>
                  <label class="form-label">{{ t('Password') }}<input v-model="registerForm.password" type="password" class="form-input" /></label>
                  <label class="form-label">{{ t('Confirm password') }}<input v-model="registerForm.confirmPassword" type="password" class="form-input" /></label>
                  <div class="lg:col-span-2 flex flex-wrap items-center gap-3">
                    <button class="btn-primary" type="submit">{{ t('Create user') }}</button>
                    <span v-if="registerError" class="text-sm font-semibold text-red-600">{{ registerError }}</span>
                    <span v-if="registerSuccess" class="text-sm font-semibold text-green-600">{{ registerSuccess }}</span>
                  </div>
                </form>
              </section>
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Users') }}</p><h3>{{ t('Existing users') }}</h3></div></div>
                <div class="mt-5 overflow-hidden rounded-2xl border border-slate-200 dark:border-slate-700">
                  <table class="data-table">
                    <thead><tr><th>{{ t('Username') }}</th><th>{{ t('Name') }}</th><th>{{ t('Email') }}</th><th>{{ t('Role') }}</th><th>{{ t('Status') }}</th><th>{{ t('Last update') }}</th><th></th></tr></thead>
                    <tbody>
                      <tr v-for="profile in users" :key="profile.username">
                        <td class="font-bold">{{ profile.username }}</td>
                        <td>{{ profile.name }}</td>
                        <td>{{ profile.email }}</td>
                        <td>{{ roleLabel(profile.roleKey) }}</td>
                        <td><span :class="statusClass(profile.active ? 'Active' : 'Blocked')">{{ profile.active ? t('Active') : t('Blocked') }}</span></td>
                        <td>{{ profile.lastLogin ? new Date(profile.lastLogin).toLocaleString() : '-' }}</td>
                        <td class="space-x-2">
                          <button v-if="profile.username !== 'admin'" class="btn-secondary" @click="toggleUserActive(profile)">{{ profile.active ? t('Blocked') : t('Active') }}</button>
                          <button v-if="profile.username !== 'admin'" class="btn-secondary" @click="removeUser(profile.username)">{{ t('Delete') }}</button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </section>
            </div>

            <!-- FIWARE VIEW -->
            <div v-if="activeView === 'fiware'" class="space-y-6">
              <section class="card p-6">
                <div class="section-heading"><div><p>{{ t('Context broker boundary') }}</p><h3>{{ t('Current NGSI-LD-style context') }}</h3></div></div>
                <p class="mt-3 max-w-3xl text-slate-600 dark:text-slate-300">{{ t('The relational backend remains the business source of truth. Orion-LD is used for current/hot context of supports, product units and racks.') }}</p>
                <button class="btn-primary mt-5" @click="publishFiware">{{ t('Publish current context to Orion-LD') }}</button>
              </section>
              <pre class="overflow-auto rounded-3xl bg-slate-950 p-6 text-sm text-slate-100">{{ JSON.stringify(fiwareContext, null, 2) }}</pre>
            </div>
          </template>
        </section>
      </main>
    </div>
  </div>
</template>
