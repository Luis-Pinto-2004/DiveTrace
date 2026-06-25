<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useOperationsStore, type Unit } from '@/stores/operations'
import { authenticate } from '@/data/demoUsers'

type Maybe<T> = T | (() => T)
interface DemoStep {
  route: string
  title: string
  message: string
  query?: Maybe<Record<string, string>>
  scrollTo?: string
  highlight?: Maybe<string | null>
  type?: Array<{ selector: string; text: string }>
  action?: () => void
  badge?: Maybe<string>
  dwell?: number
}

const LOGIN_INDEX = 0
const DWELL = 12000

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const ops = useOperationsStore()

// ---- unidade acompanhada (o caso concreto seguido do início ao fim) ------
const trackedId = ref('')
const trackedOrderId = ref('O1')
const trackedUnit = computed<Unit | undefined>(() => ops.unitById(trackedId.value))
const trackedLabel = computed(() => trackedUnit.value?.label ?? '')
const trackedSectionName = computed(() =>
  trackedUnit.value ? ops.sectionName(trackedUnit.value.sectionId) : '',
)
function resolveTracked() {
  const u =
    ops.units.find((x) => x.orderId === 'O1' && x.sectionId === 'SEC-MONT-FINAL') ??
    ops.units.find((x) => x.sectionId === 'SEC-MONT-FINAL' && x.state === 'active') ??
    ops.units.find((x) => {
      const o = ops.orders.find((ord) => ord.id === x.orderId)
      return o?.status === 'in_production' && x.state === 'active'
    })
  trackedId.value = u?.id ?? ''
  trackedOrderId.value = u?.orderId ?? 'O1'
}

// ---- ações da demo — só estado cliente, nunca a base de dados -------------
function selectTracked() {
  if (trackedId.value) ops.selectUnit(trackedId.value)
}
function supportTracked() {
  const u = trackedUnit.value
  if (u) ops.ensureSupport(u)
  if (trackedId.value) ops.selectUnit(trackedId.value)
}
function advanceTracked() {
  const id = trackedId.value
  if (!id) return
  const info = ops.advanceInfo(id)
  if (info && !info.atEnd && info.blocked) {
    // Libertar uma vaga concluindo uma inspeção pendente na secção de destino.
    const blocker = ops.units.find(
      (x) => x.sectionId === 'SEC-CQ' && x.quality === 'pending' && x.id !== id,
    )
    if (blocker) ops.decideQuality(blocker.id, 'approve')
  }
  ops.selectUnit(id)
  ops.advanceUnit(id)
}
function approveTracked() {
  if (trackedId.value) ops.decideQuality(trackedId.value, 'approve')
}

const STEPS: DemoStep[] = [
  {
    route: '/login',
    title: 'Início de sessão',
    message: 'Entramos como Administrador para acompanhar uma encomenda do início ao fim.',
    highlight: '[data-demo="login"]',
    type: [
      { selector: '[data-demo="login-user"]', text: 'admin' },
      { selector: '[data-demo="login-pass"]', text: 'admin' },
    ],
    dwell: 3200,
  },
  {
    route: '/cockpit',
    title: 'Painel de operações',
    message: 'Vamos seguir uma unidade. O painel mostra onde está no fluxo de produção.',
    action: selectTracked,
    badge: 'Unidade selecionada',
    scrollTo: '[data-demo="producao"]',
    highlight: '[data-demo="producao"]',
  },
  {
    route: '/ordens',
    title: 'Encomenda em produção',
    message: 'Esta é a encomenda da unidade acompanhada. Cada peça é uma unidade individual.',
    highlight: () => `[data-demo-order="${trackedOrderId.value}"]`,
  },
  {
    route: '/linhas',
    title: 'Suporte e rastreabilidade',
    message: 'No chão de fábrica a unidade segue num suporte: é isso que a torna rastreável.',
    action: supportTracked,
    badge: 'Suporte ativo',
    highlight: () => (trackedId.value ? `[data-demo-unit="${trackedId.value}"]` : '[data-demo="linha"]'),
  },
  {
    route: '/linhas',
    title: 'Avanço pela rota',
    message: 'A unidade avança para a etapa seguinte, e só se a secção tiver capacidade.',
    action: advanceTracked,
    badge: () => `Avançou para ${ops.sectionName(trackedUnit.value?.sectionId ?? '')}`,
    highlight: () => (trackedId.value ? `[data-demo-unit="${trackedId.value}"]` : '[data-demo="ocupacao"]'),
    dwell: 13000,
  },
  {
    route: '/qualidade',
    title: 'Controlo de qualidade',
    message: 'No controlo de qualidade decide-se: aprovar, recondicionar ou sucata.',
    action: approveTracked,
    badge: 'Qualidade validada, segue para rack',
    highlight: () => (trackedId.value ? `[data-demo-unit="${trackedId.value}"]` : '[data-demo="qualidade"]'),
    dwell: 13000,
  },
  {
    route: '/cliente',
    title: 'Acompanhamento pelo cliente',
    message: 'O cliente vê o estado da sua encomenda, sem aceder aos dados internos da fábrica.',
    highlight: () => `[data-demo-order="${trackedOrderId.value}"]`,
  },
  {
    route: '/rastreabilidade',
    title: 'Mapa de rastreabilidade',
    message: 'O grafo mostra todo o percurso da ordem: unidade, suporte, secções, qualidade e eventos.',
    query: () => ({ gp: 'order', go: trackedOrderId.value || 'O1' }),
    highlight: '[data-demo="grafo"]',
    dwell: 14000,
  },
]

const active = computed(() => route.query.demo === '1')
const index = ref(0)
const playing = ref(false)
const actionLabel = ref('')
const step = computed(() => STEPS[index.value])
const isLast = computed(() => index.value === STEPS.length - 1)
const showTracked = computed(() => !!trackedLabel.value && step.value.route !== '/login')

let timer: ReturnType<typeof setTimeout> | null = null
let runId = 0
const delay = (ms: number) => new Promise((r) => setTimeout(r, ms))
function resolve<T>(v: Maybe<T> | undefined): T | undefined {
  return typeof v === 'function' ? (v as () => T)() : v
}
function stepForRoute(path: string): number {
  const i = STEPS.findIndex((s) => s.route === path)
  return i >= 0 ? i : 0
}

// ---- destaque visual -----------------------------------------------------
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
function setHighlight(sel: string | null) {
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
async function navigate(path: string, extra: Record<string, string> = {}) {
  const queryDiffers = Object.keys(extra).some((k) => route.query[k] !== extra[k])
  if (route.path !== path || queryDiffers) {
    try {
      await router.push({ path, query: { demo: '1', ...extra } })
    } catch {
      /* navegação redundante/cancelada: ignorar */
    }
    await nextTick()
  }
}

// ---- motor de passos: destacar, observar, agir, observar -----------------
async function runStep(i: number) {
  const my = ++runId
  const s = STEPS[i]
  actionLabel.value = ''
  clearHighlight()
  await navigate(s.route, resolve(s.query) ?? {})
  if (my !== runId || !active.value) return
  await delay(420)
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
    await delay(620)
    if (my !== runId || !active.value) return
  }

  const hl = resolve(s.highlight) ?? null
  if (hl) setHighlight(hl)

  if (s.action) {
    await delay(1100) // observar o elemento antes da ação
    if (my !== runId || !active.value) return
    s.action()
    actionLabel.value = resolve(s.badge) ?? ''
    await delay(360) // deixar a UI atualizar
    if (my !== runId || !active.value) return
    if (hl) setHighlight(hl) // re-localizar (o elemento pode ter mudado)
  }

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
  ops.resetDemo()
  resolveTracked()
  index.value = 0
  playing.value = true
  void runStep(0)
}
function exit() {
  clearTimer()
  playing.value = false
  clearHighlight()
  actionLabel.value = ''
  window.scrollTo({ top: 0 })
  const query = { ...route.query }
  delete query.demo
  delete query.gp
  delete query.go
  delete query.gu
  void router.replace({ path: route.path, query })
}

function start() {
  // Repõe o cenário para um estado conhecido (repetível, não destrutivo) e fixa a unidade.
  ops.resetDemo()
  resolveTracked()
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
      actionLabel.value = ''
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
    <section
      class="demo__bar"
      role="region"
      aria-label="Modo de apresentação"
    >
      <header class="demo__top">
        <span class="demo__chip">Demo</span>
        <span class="demo__count">{{ index + 1 }} / {{ STEPS.length }}</span>
        <h2 class="demo__title">
          {{ step.title }}
        </h2>
        <transition name="demo-pop">
          <span
            v-if="actionLabel"
            class="demo__done"
          >✓ {{ actionLabel }}</span>
        </transition>
      </header>

      <p
        v-if="showTracked"
        class="demo__track"
      >
        A acompanhar: <strong>{{ trackedLabel }}</strong>
        <span
          v-if="trackedSectionName"
          class="demo__where"
        >· {{ trackedSectionName }}</span>
      </p>

      <p class="demo__message">
        {{ step.message }}
      </p>

      <footer class="demo__controls">
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
          {{ playing ? 'Pausar' : 'Retomar' }}
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
      </footer>
    </section>
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
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--dt-brand-500) 32%, transparent),
    0 0 0 9999px color-mix(in srgb, #0b2948 16%, transparent);
  transition: top 0.28s ease, left 0.28s ease, width 0.28s ease, height 0.28s ease;
  pointer-events: none;
}
.demo__bar {
  position: fixed;
  left: 50%;
  bottom: 1.1rem;
  transform: translateX(-50%);
  z-index: 9992;
  width: min(94vw, 760px);
  pointer-events: auto;
  background: var(--dt-surface);
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  box-shadow: var(--dt-shadow-card);
  padding: 0.85rem 1.05rem 0.8rem;
}
.demo__top {
  display: flex;
  align-items: center;
  gap: 0.55rem;
  flex-wrap: wrap;
}
.demo__chip {
  background: var(--dt-brand-500);
  color: #fff;
  font-size: 0.6rem;
  font-weight: 900;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  padding: 0.16rem 0.5rem;
  border-radius: 9999px;
}
.demo__count {
  font-size: 0.7rem;
  font-weight: 800;
  color: var(--dt-neutral-text);
  font-variant-numeric: tabular-nums;
}
.demo__title {
  margin: 0;
  font-size: 0.98rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.demo__done {
  margin-left: auto;
  font-size: 0.7rem;
  font-weight: 800;
  color: var(--dt-ok-text);
  background: var(--dt-ok-surface);
  border: 1px solid var(--dt-ok-border);
  border-radius: 9999px;
  padding: 0.2rem 0.55rem;
  white-space: nowrap;
}
.demo__track {
  margin: 0.5rem 0 0.25rem;
  font-size: 0.74rem;
  color: var(--dt-neutral-text);
}
.demo__track strong {
  color: var(--dt-text-strong);
}
.demo__where {
  margin-left: 0.25rem;
  font-weight: 700;
  color: var(--dt-brand-600, var(--dt-brand-500));
}
.demo__message {
  margin: 0.35rem 0 0.7rem;
  font-size: 0.82rem;
  line-height: 1.42;
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
  padding: 0.36rem 0.72rem;
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
.demo-pop-enter-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}
.demo-pop-enter-from {
  opacity: 0;
  transform: translateY(-4px) scale(0.96);
}
</style>
