import { defineStore } from 'pinia'

const SIDEBAR_WIDTH_KEY = 'drivetrace.sidebar.width'
const SIDEBAR_MIN = 240
const SIDEBAR_MAX = 420
const SIDEBAR_DEFAULT = 280

function clampWidth(value: number): number {
  return Math.min(SIDEBAR_MAX, Math.max(SIDEBAR_MIN, Math.round(value)))
}

function readStoredWidth(): number {
  try {
    const stored = Number(localStorage.getItem(SIDEBAR_WIDTH_KEY))
    return Number.isFinite(stored) && stored > 0 ? clampWidth(stored) : SIDEBAR_DEFAULT
  } catch {
    return SIDEBAR_DEFAULT
  }
}

/** Estado transversal de interface (não-domínio). */
export const useUiStore = defineStore('ui', {
  state: () => ({
    mobileSidebarOpen: false,
    userMenuOpen: false,
    sidebarWidth: readStoredWidth(),
    notice: '' as string,
  }),
  actions: {
    toggleMobileSidebar() {
      this.mobileSidebarOpen = !this.mobileSidebarOpen
    },
    closeMobileSidebar() {
      this.mobileSidebarOpen = false
    },
    setSidebarWidth(value: number) {
      this.sidebarWidth = clampWidth(value)
      try {
        localStorage.setItem(SIDEBAR_WIDTH_KEY, String(this.sidebarWidth))
      } catch {
        /* armazenamento indisponível */
      }
    },
    resetSidebarWidth() {
      this.setSidebarWidth(SIDEBAR_DEFAULT)
    },
    setNotice(message: string) {
      this.notice = message
    },
    clearNotice() {
      this.notice = ''
    },
  },
})

export const SIDEBAR_BOUNDS = { min: SIDEBAR_MIN, max: SIDEBAR_MAX, default: SIDEBAR_DEFAULT }
