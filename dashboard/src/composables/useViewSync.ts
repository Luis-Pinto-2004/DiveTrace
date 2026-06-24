import { useRouter } from 'vue-router'
import { findViewByKey, findViewByPath, type ViewRoute } from '@/router/routes'

/**
 * Sincronização segura entre a vista ativa e a URL.
 *
 * Estratégia conservadora de migração incremental: a navegação
 * continua a ser controlada por `App.vue`, mas a URL passa a refletir
 * a vista (deep-linking, histórico do browser, partilha de ligações).
 * O fluxo é unidirecional vista -> URL para evitar ciclos de
 * sincronização enquanto a renderização não migra para `<router-view>`.
 */
export function useViewSync() {
  const router = useRouter()

  /** Atualiza a URL para refletir a vista atual, sem recarregar. */
  function pushView(viewKey: string): void {
    const route: ViewRoute | undefined = findViewByKey(viewKey)
    if (!route) return
    if (router.currentRoute.value.path !== route.path) {
      void router.push(route.path)
    }
  }

  /** Lê a vista inicial a partir da URL (no arranque). */
  function readInitialView(): string | null {
    const path = window.location.pathname
    return findViewByPath(path)?.key ?? null
  }

  return { pushView, readInitialView }
}
