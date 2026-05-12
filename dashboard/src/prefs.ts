import { ref, watch } from 'vue'
import {
  t,
  setActiveLocale,
  translateMaterial,
  translateMaterialName,
  translateQualityResult,
  translateSection,
  translateSectionName,
  translateStatus,
  translateUnitOfMeasure,
  translateUnitType,
} from './i18n'

export const locale = ref<string>(localStorage.getItem('locale') || 'pt-PT')
export const theme = ref<string>(localStorage.getItem('theme') || 'light')

function applyTheme(value: string) {
  if (value === 'dark') document.documentElement.classList.add('dark')
  else document.documentElement.classList.remove('dark')
}

applyTheme(theme.value)
setActiveLocale(locale.value)

watch(locale, (value) => {
  localStorage.setItem('locale', value)
  setActiveLocale(value)
})
watch(theme, (value) => {
  localStorage.setItem('theme', value)
  applyTheme(value)
})

export function toggleTheme(): void {
  theme.value = theme.value === 'light' ? 'dark' : 'light'
}

export function setLocale(newLocale: string): void {
  if (newLocale === 'pt-PT' || newLocale === 'en') locale.value = newLocale
}

export {
  t,
  translateMaterial,
  translateMaterialName,
  translateQualityResult,
  translateSection,
  translateSectionName,
  translateStatus,
  translateUnitOfMeasure,
  translateUnitType,
}
