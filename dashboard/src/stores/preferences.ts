import { defineStore } from 'pinia'

export type ThemeMode = 'light' | 'dark'
export type LocaleCode = 'pt-PT' | 'en'

const THEME_KEY = 'theme'
const LOCALE_KEY = 'locale'

function readStored<T extends string>(key: string, fallback: T, allowed: readonly T[]): T {
  try {
    const value = localStorage.getItem(key) as T | null
    return value && allowed.includes(value) ? value : fallback
  } catch {
    return fallback
  }
}

function applyTheme(mode: ThemeMode) {
  const root = document.documentElement
  if (mode === 'dark') root.classList.add('dark')
  else root.classList.remove('dark')
}

/**
 * Estado de preferências de interface.
 *
 * Usa as MESMAS chaves de localStorage que a app já utilizava
 * (`theme`, `locale`), garantindo coerência durante a migração
 * incremental para Pinia, sem regressões.
 */
export const usePreferencesStore = defineStore('preferences', {
  state: () => ({
    theme: readStored<ThemeMode>(THEME_KEY, 'light', ['light', 'dark']),
    locale: readStored<LocaleCode>(LOCALE_KEY, 'pt-PT', ['pt-PT', 'en']),
  }),
  getters: {
    isDark: (state): boolean => state.theme === 'dark',
  },
  actions: {
    setTheme(mode: ThemeMode) {
      this.theme = mode
      try {
        localStorage.setItem(THEME_KEY, mode)
      } catch {
        /* armazenamento indisponível — preferência não persiste */
      }
      applyTheme(mode)
    },
    toggleTheme() {
      this.setTheme(this.theme === 'light' ? 'dark' : 'light')
    },
    setLocale(code: LocaleCode) {
      this.locale = code
      try {
        localStorage.setItem(LOCALE_KEY, code)
      } catch {
        /* armazenamento indisponível */
      }
    },
    /** Aplica o tema atual ao DOM (chamar no arranque). */
    hydrate() {
      applyTheme(this.theme)
    },
  },
})
