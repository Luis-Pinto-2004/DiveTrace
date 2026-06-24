/**
 * Mapeamento centralizado de estados de domínio para tons semânticos.
 *
 * Antes, a lógica de cores de estado estava embutida em `App.vue`
 * (função `statusClass`). Foi extraída para aqui para eliminar
 * duplicação e permitir testes determinísticos. A lógica por
 * palavra-chave é preservada exatamente; cada tom mapeia para uma
 * classe `badge-*` existente e para um token semântico.
 */

export type StatusTone = 'ok' | 'info' | 'warn' | 'action' | 'critical' | 'neutral'

const TONE_TO_BADGE: Record<StatusTone, string> = {
  ok: 'badge-green',
  info: 'badge-blue',
  warn: 'badge-amber',
  action: 'badge-amber',
  critical: 'badge-red',
  neutral: 'badge-gray',
}

/**
 * Determina o tom semântico de um estado a partir de palavras-chave.
 * A ordem de avaliação corresponde à prioridade do domínio:
 * crítico > atenção/decisão > fluxo > concluído > neutro.
 */
export function statusTone(status: string | undefined | null): StatusTone {
  const value = (status || '').toLowerCase()
  if (!value) return 'neutral'
  if (
    value.includes('fail') ||
    value.includes('blocked') ||
    value.includes('scrap') ||
    value.includes('rejected') ||
    value.includes('bloque') ||
    value.includes('reprov') ||
    value.includes('sucata')
  ) {
    return 'critical'
  }
  if (
    value.includes('rework') ||
    value.includes('pending') ||
    value.includes('recover') ||
    value.includes('recondition') ||
    value.includes('quality') ||
    value.includes('retrabalho') ||
    value.includes('pendente') ||
    value.includes('recondic')
  ) {
    return 'warn'
  }
  if (
    value.includes('transfer') ||
    value.includes('move') ||
    value.includes('active') ||
    value.includes('loaded') ||
    value.includes('progress') ||
    value.includes('transfer') ||
    value.includes('ativo') ||
    value.includes('curso')
  ) {
    return 'info'
  }
  if (
    value.includes('pass') ||
    value.includes('completed') ||
    value.includes('stored') ||
    value.includes('available') ||
    value.includes('ready') ||
    value.includes('aprov') ||
    value.includes('conclu') ||
    value.includes('dispon')
  ) {
    return 'ok'
  }
  return 'neutral'
}

/**
 * Classe `badge-*` para um estado. Equivalente à antiga `statusClass`
 * em `App.vue`, agora derivada do tom semântico.
 */
export function statusBadgeClass(status: string | undefined | null): string {
  return TONE_TO_BADGE[statusTone(status)]
}

/** Classe `badge-*` para um tom já conhecido. */
export function badgeClassForTone(tone: StatusTone): string {
  return TONE_TO_BADGE[tone]
}
