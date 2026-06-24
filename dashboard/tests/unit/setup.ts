import { beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'

// matchMedia não existe em jsdom; alguns componentes/composables podem usá-lo.
if (!window.matchMedia) {
  window.matchMedia = (query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    }) as unknown as MediaQueryList
}

beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
})
