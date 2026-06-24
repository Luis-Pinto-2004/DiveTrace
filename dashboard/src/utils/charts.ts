/**
 * Ajudantes de gráficos (puros, testáveis).
 * Sem dependências — os componentes SVG consomem estas funções.
 */

export interface Box {
  width: number
  height: number
  padding: number
}

/** Escala linear de domínio [d0,d1] para intervalo [r0,r1]. */
export function linearScale(d0: number, d1: number, r0: number, r1: number) {
  const span = d1 - d0 || 1
  return (value: number): number => r0 + ((value - d0) / span) * (r1 - r0)
}

/** Caminho SVG (polilinha suave por segmentos retos) para uma série. */
export function linePath(values: number[], box: Box, maxOverride?: number): string {
  if (!values.length) return ''
  const max = maxOverride ?? Math.max(...values, 1)
  const x = linearScale(0, Math.max(values.length - 1, 1), box.padding, box.width - box.padding)
  const y = linearScale(0, max, box.height - box.padding, box.padding)
  return values
    .map((v, i) => `${i === 0 ? 'M' : 'L'} ${x(i).toFixed(2)} ${y(v).toFixed(2)}`)
    .join(' ')
}

/** Caminho de área fechada (para preenchimento sob a linha). */
export function areaPath(values: number[], box: Box, maxOverride?: number): string {
  if (!values.length) return ''
  const max = maxOverride ?? Math.max(...values, 1)
  const x = linearScale(0, Math.max(values.length - 1, 1), box.padding, box.width - box.padding)
  const y = linearScale(0, max, box.height - box.padding, box.padding)
  const top = values.map((v, i) => `${i === 0 ? 'M' : 'L'} ${x(i).toFixed(2)} ${y(v).toFixed(2)}`).join(' ')
  const baseY = (box.height - box.padding).toFixed(2)
  const lastX = x(values.length - 1).toFixed(2)
  const firstX = x(0).toFixed(2)
  return `${top} L ${lastX} ${baseY} L ${firstX} ${baseY} Z`
}

export interface Arc {
  path: string
  value: number
  fraction: number
}

/** Segmentos de donut a partir de valores. Devolve arcos SVG. */
export function donutArcs(values: number[], radius = 60, thickness = 22, cx = 80, cy = 80): Arc[] {
  const total = values.reduce((sum, v) => sum + v, 0) || 1
  const inner = radius - thickness
  let angle = -Math.PI / 2
  return values.map((value) => {
    const fraction = value / total
    const end = angle + fraction * Math.PI * 2
    const large = end - angle > Math.PI ? 1 : 0
    const x0 = cx + radius * Math.cos(angle)
    const y0 = cy + radius * Math.sin(angle)
    const x1 = cx + radius * Math.cos(end)
    const y1 = cy + radius * Math.sin(end)
    const xi1 = cx + inner * Math.cos(end)
    const yi1 = cy + inner * Math.sin(end)
    const xi0 = cx + inner * Math.cos(angle)
    const yi0 = cy + inner * Math.sin(angle)
    const path = [
      `M ${x0.toFixed(2)} ${y0.toFixed(2)}`,
      `A ${radius} ${radius} 0 ${large} 1 ${x1.toFixed(2)} ${y1.toFixed(2)}`,
      `L ${xi1.toFixed(2)} ${yi1.toFixed(2)}`,
      `A ${inner} ${inner} 0 ${large} 0 ${xi0.toFixed(2)} ${yi0.toFixed(2)}`,
      'Z',
    ].join(' ')
    angle = end
    return { path, value, fraction }
  })
}

/** Caminho de arco simples para gauge (semicírculo). */
export function gaugeArc(fraction: number, cx = 80, cy = 80, radius = 64): string {
  const clamped = Math.max(0, Math.min(1, fraction))
  const start = Math.PI
  const end = Math.PI + clamped * Math.PI
  const x0 = cx + radius * Math.cos(start)
  const y0 = cy + radius * Math.sin(start)
  const x1 = cx + radius * Math.cos(end)
  const y1 = cy + radius * Math.sin(end)
  const large = end - start > Math.PI ? 1 : 0
  return `M ${x0.toFixed(2)} ${y0.toFixed(2)} A ${radius} ${radius} 0 ${large} 1 ${x1.toFixed(2)} ${y1.toFixed(2)}`
}
