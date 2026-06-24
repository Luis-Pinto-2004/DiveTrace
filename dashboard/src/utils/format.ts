/**
 * Formatadores PT-PT centralizados.
 *
 * Reúne formatação de datas, números, percentagens e durações com a
 * localização `pt-PT` por defeito, para a UI ser consistente e testável.
 */

const PT = 'pt-PT'

/** Data e hora curtas (ex.: "17/06/2026, 14:30"). */
export function formatDateTime(value: string | number | Date | null | undefined): string {
  if (value === null || value === undefined || value === '') return '-'
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  return new Intl.DateTimeFormat(PT, {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(date)
}

/** Apenas a data (ex.: "17/06/2026"). */
export function formatDate(value: string | number | Date | null | undefined): string {
  if (value === null || value === undefined || value === '') return '-'
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  return new Intl.DateTimeFormat(PT, { dateStyle: 'short' }).format(date)
}

/** Número com separadores PT-PT (ex.: "1 234,5"). */
export function formatNumber(
  value: number | null | undefined,
  maximumFractionDigits = 0,
): string {
  if (value === null || value === undefined || Number.isNaN(value)) return '-'
  return new Intl.NumberFormat(PT, { maximumFractionDigits }).format(value)
}

/** Percentagem a partir de uma fração 0–1 (ex.: 0.42 -> "42%"). */
export function formatPercent(
  fraction: number | null | undefined,
  maximumFractionDigits = 0,
): string {
  if (fraction === null || fraction === undefined || Number.isNaN(fraction)) return '-'
  return new Intl.NumberFormat(PT, {
    style: 'percent',
    maximumFractionDigits,
  }).format(fraction)
}

/** Duração legível a partir de minutos (ex.: 95 -> "1 h 35 min"). */
export function formatDurationMinutes(totalMinutes: number | null | undefined): string {
  if (totalMinutes === null || totalMinutes === undefined || Number.isNaN(totalMinutes)) return '-'
  const minutes = Math.max(0, Math.round(totalMinutes))
  const hours = Math.floor(minutes / 60)
  const rest = minutes % 60
  if (hours === 0) return `${rest} min`
  if (rest === 0) return `${hours} h`
  return `${hours} h ${rest} min`
}

/** Tempo relativo simples em PT-PT (ex.: "há 5 min"). */
export function formatRelativeTime(value: string | number | Date | null | undefined): string {
  if (value === null || value === undefined || value === '') return '-'
  const date = value instanceof Date ? value : new Date(value)
  if (Number.isNaN(date.getTime())) return '-'
  const diffMs = date.getTime() - Date.now()
  const diffMin = Math.round(diffMs / 60000)
  const abs = Math.abs(diffMin)
  const rtf = new Intl.RelativeTimeFormat(PT, { numeric: 'auto' })
  if (abs < 60) return rtf.format(diffMin, 'minute')
  const diffHours = Math.round(diffMin / 60)
  if (Math.abs(diffHours) < 24) return rtf.format(diffHours, 'hour')
  return rtf.format(Math.round(diffHours / 24), 'day')
}
