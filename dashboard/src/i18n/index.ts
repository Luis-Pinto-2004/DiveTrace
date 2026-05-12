import { en } from './en'
import { ptPT } from './pt-PT'

const dictionaries: Record<string, Record<string, string>> = {
  'pt-PT': ptPT,
  en,
}

let activeLocale = 'pt-PT'

const reversePT = Object.entries(ptPT).reduce<Record<string, string>>((acc, [english, portuguese]) => {
  acc[portuguese] = english
  return acc
}, {})

export function setActiveLocale(locale: string): void {
  activeLocale = locale === 'en' ? 'en' : 'pt-PT'
}

export function t(text: string): string {
  const dictionary = dictionaries[activeLocale] || {}
  return dictionary[text] || text
}

function translate(text: string | undefined): string {
  if (!text) return ''
  if (activeLocale === 'pt-PT') return ptPT[text] || text
  return en[text] || reversePT[text] || text
}

export function translateStatus(status: string | undefined): string {
  return translate(status)
}

export function translateSection(section: string | undefined): string {
  return translate(section)
}

export function translateMaterial(material: string | undefined): string {
  return translate(material)
}

export function translateQualityResult(result: string | undefined): string {
  return translateStatus(result)
}

export function translateUnitType(type: string | undefined): string {
  return translate(type)
}

export function translateUnitOfMeasure(unit: string | undefined): string {
  return translate(unit)
}

export const translateSectionName = translateSection
export const translateMaterialName = translateMaterial
