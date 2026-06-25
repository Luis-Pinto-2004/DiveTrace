<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { authenticate } from '@/data/demoUsers'

interface DemoStep {
  route: string
  title: string
  message: string
  scrollTo?: string
  highlight?: string
  type?: Array<{ selector: string; text: string }>
  dwell?: number
}

const LOGIN_INDEX = 0
const DWELL = 10000

const STEPS: DemoStep[] = [
  {
    route: '/login',
    title: 'Início de sessão',
    message: 'A entrar como Administrador, o perfil que mostra todas as funcionalidades.',
    highlight: '[data-demo="login"]',
    type: [
      { selector: '[data-demo="login-user"]', text: 'admin' },
      { selector: '[data-demo="login-pass"]', text: 'admin' },
    ],
    dwell: 3000,
  },
  {
    route: '/cockpit',
    title: 'Painel de operações',
    message: 'Visão geral da fábrica e prioridades do turno: unidades em curso, qualidade e produção.',
    scrollTo: '[data-demo="producao"]',
    highlight: '[data-demo="producao"]',
  },
  {
    route: '/ordens',
    title: 'Encomendas e ordens de fabrico',
    message: 'Pedido de cliente dá origem a uma ordem de fabrico; aceitar gera as unidades individuais.',
    highlight: '[data-demo="encomendas"]',
  },
  {
    route: '/linhas',
    title: 'Produção e suporte',
    message: 'Suporte atribuído: cada unidade é rastreável ao avançar pelas secções da linha.',
    highlight: '[data-demo="linha"]',
  },
  {
    route: '/qualidade',
    title: 'Controlo de qualidade',
    message: 'Unidade em controlo de qualidade: aprovar, recondicionar ou marcar como sucata.',
    highlight: '[data-demo="qualidade"]',
  },
  {
    route: '/rastreabilidade',
    title: 'Mapa de rastreabilidade',
    message: 'Rastreabilidade completa no grafo: cliente, ordem, unidade, suporte, secções, qualidade e eventos.',
    highlight: '[data-demo="grafo"]',
  },
  {
    route: '/cliente',
    title: 'Acompanhamento pelo cliente',
    message: 'O cliente acompanha as suas encomendas. Ciclo completo: do pedido à unidade rastreável.',
    highlight: '[data-demo="cliente"]',
  },
]

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const active = computed(() => route.query.demo === '1')
const index = ref(0)
const playing = ref(false)
const step = computed(() => STEPS[index.value])
const isLast = computed(() => index.value === STEPS.length - 1)

let timer: ReturnType<typeof setTimeout> | null = null
let runId = 0

const delay = (ms: number) => new Promise((r) => setTimeout(r, ms))

function stepForRoute(path: string): number {
  const i = STEPS.findIndex((s) => s.route === path)
  return i >= 0 ? i : 0
}

// ---- destaque visual (anel sobre o elemento) -----------------------------
const ring = ref<{ top: number; left: number; width: number; height: number } | null>(null)
let highlightSel: string | null = null
function recomputeRing() {
  if (!highlightSel) {
    ring.value = null
    return
  }
  const el = document.querySelector(highlightSel)
  if (!el) {
    ring.value = null
    return
  }
  const r = el.getBoundingClientRect()
  ring.value = { top: r.top - 6, left: r.left - 6, width: r.width + 12, height: r.height + 12 }
}
function setHighlight(sel: string) {
  highlightSel = sel
  recomputeRing()
}
function clearHighlight() {
  highlightSel = null
  ring.value = null
}

// ---- escrita simulada ----------------------------------------------------
async function typeInto(selector: string, text: string) {
  const el = document.querySelector(selector) as HTMLInputElement | null
  if (!el) return
  el.focus()
  el.value = ''
  el.dispatchEvent(new Event('input', { bubbles: true }))
  for (const ch of text) {
    el.value += ch
    el.dispatchEvent(new Event('input', { bubbles: true }))
    await delay(95)
  }
  el.dispatchEvent(new Event('change', { bubbles: true }))
}

// ---- navegação preservando o modo demo -----------------------------------
async function navigate(path: string) {
  if (route.path !== path) {
    try {
      await router.push({ path, query: { demo: '1' } })
    } catch {
      /* navegação redundante ou cancelada: ignorar */
    }
    await nextTick()
  }
}

// ---- motor de passos -----------------------------------------------------
async function runStep(i: number) {
  const my = ++runId
  const s = STEPS[i]
  clearHighlight()
  await navigate(s.route)
  if (my !== runId || !active.value) return
  await delay(240)
  if (my !== runId || !active.value) return
  if (s.type) {
    for (const t of s.type) {
      await typeInto(t.selector, t.text)
      if (my !== runId || !active.value) return
    }
    if (i === LOGIN_INDEX && !auth.isAuthenticated) {
      const session = authenticate('admin', 'admin')
      if (session) auth.setUser(session)
    }
  }
  if (s.scrollTo) {
    document.querySelector(s.scrollTo)?.scrollIntoView({ behavior: 'smooth', block: 'center' })
    await delay(640)
    if (my !== runId || !active.value) return
  }
  if (s.highlight) setHighlight(s.highlight)
  if (playing.value) scheduleNext(s.dwell ?? DWELL)
}

function clearTimer() {
  if (timer) {
    clearTimeout(timer)
    timer = null
  }
}
function scheduleNext(ms: number) {
  clearTimer()
  timer = setTimeout(() => {
    if (playing.value && active.value) next()
  }, ms)
}
function next() {
  clearTimer()
  if (index.value < STEPS.length - 1) {
    index.value += 1
    void runStep(index.value)
  } else {
    playing.value = false
  }
}
function prev() {
  clearTimer()
  if (index.value > 0) {
    index.value -= 1
    void runStep(index.value)
  }
}
function togglePlay() {
  playing.value = !playing.value
  if (playing.value) scheduleNext(step.value.dwell ?? DWELL)
  else clearTimer()
}
function restart() {
  clearTimer()
  index.value = 0
  playing.value = true
  void runStep(0)
}
function exit() {
  clearTimer()
  playing.value = false
  clearHighlight()
  window.scrollTo({ top: 0 })
  const query = { ...route.query }
  delete query.demo
  void router.replace({ path: route.path, query })
}

function start() {
  index.value = stepForRoute(route.path)
  playing.value = true
  void runStep(index.value)
}

watch(
  active,
  (on) => {
    if (on) {
      void nextTick().then(start)
    } else {
      clearTimer()
      playing.value = false
      clearHighlight()
    }
  },
  { immediate: true },
)

function onScrollResize() {
  recomputeRing()
}
onMounted(() => {
  window.addEventListener('scroll', onScrollResize, true)
  window.addEventListener('resize', onScrollResize)
})
onBeforeUnmount(() => {
  window.removeEventListener('scroll', onScrollResize, true)
  window.removeEventListener('resize', onScrollResize)
  clearTimer()
})
</script>

<template>
  <div
    v-if="active"
    class="demo"
  >
    <div
      v-if="ring"
      class="demo__ring"
      :style="{ top: ring.top + 'px', left: ring.left + 'px', width: ring.width + 'px', height: ring.height + 'px' }"
    />
    <div
      class="demo__bar"
      role="region"
      aria-label="Modo de apresentação"
    >
      <div class="demo__head">
        <span class="demo__badge">Demo</span>
        <span class="demo__count">{{ index + 1 }} / {{ STEPS.length }}</span>
        <h2 class="demo__title">
          {{ step.title }}
        </h2>
      </div>
      <p class="demo__message">
        {{ step.message }}
      </p>
      <div class="demo__controls">
        <button
          type="button"
          class="demo__btn"
          :disabled="index === 0"
          @click="prev"
        >
          Anterior
        </button>
        <button
          type="button"
          class="demo__btn demo__btn--primary"
          @click="togglePlay"
        >
          {{ playing ? 'Pausar' : 'Reproduzir' }}
        </button>
        <button
          type="button"
          class="demo__btn"
          :disabled="isLast"
          @click="next"
        >
          Seguinte
        </button>
        <button
          type="button"
          class="demo__btn demo__btn--ghost"
          @click="restart"
        >
          Recomeçar
        </button>
        <button
          type="button"
          class="demo__btn demo__btn--ghost"
          @click="exit"
        >
          Terminar
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.demo {
  position: fixed;
  inset: 0;
  z-index: 9990;
  pointer-events: none;
}
.demo__ring {
  position: fixed;
  z-index: 9991;
  border: 2px solid var(--dt-brand-500);
  border-radius: 12px;
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--dt-brand-500) 30%, transparent),
    0 0 0 9999px color-mix(in srgb, #0b2948 18%, transparent);
  transition: top 0.25s ease, left 0.25s ease, width 0.25s ease, height 0.25s ease;
  pointer-events: none;
}
.demo__bar {
  position: fixed;
  left: 50%;
  bottom: 1rem;
  transform: translateX(-50%);
  z-index: 9992;
  width: min(92vw, 720px);
  pointer-events: auto;
  background: var(--dt-surface);
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  box-shadow: var(--dt-shadow-card);
  padding: 0.8rem 1rem;
}
.demo__head {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex-wrap: wrap;
}
.demo__badge {
  background: var(--dt-brand-500);
  color: #fff;
  font-size: 0.62rem;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  padding: 0.15rem 0.45rem;
  border-radius: 9999px;
}
.demo__count {
  font-size: 0.7rem;
  font-weight: 800;
  color: var(--dt-neutral-text);
}
.demo__title {
  margin: 0;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.demo__message {
  margin: 0.5rem 0 0.7rem;
  font-size: 0.8rem;
  line-height: 1.4;
  color: var(--dt-text-strong);
}
.demo__controls {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
  justify-content: flex-end;
}
.demo__btn {
  border: 1px solid var(--dt-border);
  background: var(--dt-surface);
  color: var(--dt-text-strong);
  border-radius: 8px;
  padding: 0.35rem 0.7rem;
  font-size: 0.74rem;
  font-weight: 800;
  cursor: pointer;
}
.demo__btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
.demo__btn--primary {
  background: var(--dt-brand-500);
  border-color: var(--dt-brand-500);
  color: #fff;
}
.demo__btn--ghost {
  color: var(--dt-neutral-text);
}
</style>
