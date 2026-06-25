<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { authenticate } from '@/data/demoUsers'

interface DemoStep {
  route: string | null
  title: string
  points: string[]
}

const STEPS: DemoStep[] = [
  {
    route: '/cockpit',
    title: 'Visão geral da fábrica',
    points: [
      'Painel de operações como dashboard: KPIs de unidades em curso, concluídas, em análise de qualidade e recondicionamento.',
      'Visão rápida da produção: linhas, secções e unidades em curso num só ecrã.',
      'Selecionar uma unidade mostra o seu percurso, estado atual e próxima etapa.',
    ],
  },
  {
    route: '/ordens',
    title: 'Encomendas e ordens de fabrico',
    points: [
      'Uma encomenda de cliente dá origem a uma ordem de fabrico.',
      'Aceitar a encomenda cria as unidades de produto individuais.',
      'Iniciar a produção coloca as unidades na linha de montagem.',
    ],
  },
  {
    route: '/linhas',
    title: 'Análise por linha e suporte',
    points: [
      'Movimentação das unidades pelas linhas e secções da fábrica.',
      'O suporte é o elemento central da rastreabilidade: cada unidade ativa anda associada a um suporte.',
      'Carga por secção, gargalos e ações operacionais (avançar etapa, transferir).',
    ],
  },
  {
    route: '/qualidade',
    title: 'Controlo de qualidade',
    points: [
      'Decisões de qualidade na Linha 4: aprovar, recondicionar ou marcar como sucata.',
      'O recondicionamento é um desvio controlado, mantendo sempre a rastreabilidade da unidade.',
    ],
  },
  {
    route: '/rastreabilidade',
    title: 'Mapa de rastreabilidade',
    points: [
      'Grafo técnico com cliente, ordem, unidade, suporte, secção/linha, qualidade, recondicionamento, sucata, rack e eventos.',
      'Três perspetivas: fábrica, ordem de fabrico e unidade de produto.',
      'Selecionar um nó destaca as ligações relacionadas e abre o detalhe completo no painel lateral.',
    ],
  },
  {
    route: null,
    title: 'Analítica e FIWARE (separador externo)',
    points: [
      'Grafana (http://localhost:3000) mostra o histórico e as tendências de WIP, qualidade e fluxo.',
      'FIWARE / Orion-LD (http://localhost:1026) mantém o contexto da fábrica como entidades NGSI-LD.',
      'Evidencia rastreabilidade e contexto interoperável, não apenas um CRUD.',
    ],
  },
  {
    route: '/cliente',
    title: 'Área de cliente',
    points: [
      'O cliente consulta e cria as suas encomendas, sem acesso aos dados internos da fábrica.',
      'Fecha o ciclo: do pedido do cliente à unidade rastreável na fábrica.',
    ],
  },
]

const AUTO_MS = 16000

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const active = computed(() => route.query.demo === '1')
const index = ref(0)
const playing = ref(false)
let timer: ReturnType<typeof setInterval> | null = null

const step = computed(() => STEPS[index.value])
const isLast = computed(() => index.value === STEPS.length - 1)

function goToStep() {
  const target = STEPS[index.value]
  if (target.route && route.path !== target.route) {
    void router.push({ path: target.route, query: { demo: '1' } })
  }
}
function next() {
  if (index.value < STEPS.length - 1) {
    index.value += 1
    goToStep()
  } else {
    stopAuto()
  }
}
function prev() {
  if (index.value > 0) {
    index.value -= 1
    goToStep()
  }
}
function stopAuto() {
  playing.value = false
  if (timer) {
    clearInterval(timer)
    timer = null
  }
}
function togglePlay() {
  if (playing.value) {
    stopAuto()
    return
  }
  playing.value = true
  timer = setInterval(() => {
    if (isLast.value) stopAuto()
    else next()
  }, AUTO_MS)
}
function exit() {
  stopAuto()
  const query = { ...route.query }
  delete query.demo
  void router.replace({ path: route.path, query })
}

// Ao ativar o modo demo: autenticar como Administrador (sessão apenas) e ir ao 1.º passo.
watch(
  active,
  (on) => {
    if (!on) {
      stopAuto()
      return
    }
    if (!auth.isAuthenticated) {
      const session = authenticate('admin', 'admin')
      if (session) auth.setUser(session)
    }
    index.value = 0
    goToStep()
  },
  { immediate: true },
)

onBeforeUnmount(stopAuto)
</script>

<template>
  <div
    v-if="active"
    class="demo"
    role="region"
    aria-label="Modo de apresentação"
  >
    <div class="demo__bar">
      <div class="demo__head">
        <span class="demo__badge">Demo</span>
        <span class="demo__count">{{ index + 1 }} / {{ STEPS.length }}</span>
        <h2 class="demo__title">
          {{ step.title }}
        </h2>
      </div>
      <ul class="demo__points">
        <li
          v-for="(p, i) in step.points"
          :key="i"
        >
          {{ p }}
        </li>
      </ul>
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
          @click="exit"
        >
          Sair
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.demo {
  position: fixed;
  left: 50%;
  bottom: 1rem;
  transform: translateX(-50%);
  z-index: 9999;
  width: min(92vw, 720px);
  pointer-events: none;
}
.demo__bar {
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
.demo__points {
  margin: 0.55rem 0 0.7rem;
  padding-left: 1.1rem;
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.demo__points li {
  font-size: 0.76rem;
  line-height: 1.35;
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
