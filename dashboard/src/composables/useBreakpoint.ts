import { onBeforeUnmount, onMounted, ref, computed } from 'vue'

/**
 * Estado reativo do ponto de rutura do viewport.
 * Útil para alternar sidebar/drawer e densidade de tabelas.
 *
 *   const { isMobile, isTablet, width } = useBreakpoint()
 */
export function useBreakpoint(mobileMax = 768, tabletMax = 1024) {
  const width = ref(typeof window !== 'undefined' ? window.innerWidth : 1280)

  function update() {
    width.value = window.innerWidth
  }

  onMounted(() => {
    update()
    window.addEventListener('resize', update, { passive: true })
  })
  onBeforeUnmount(() => {
    window.removeEventListener('resize', update)
  })

  const isMobile = computed(() => width.value < mobileMax)
  const isTablet = computed(() => width.value >= mobileMax && width.value < tabletMax)
  const isDesktop = computed(() => width.value >= tabletMax)

  return { width, isMobile, isTablet, isDesktop }
}
