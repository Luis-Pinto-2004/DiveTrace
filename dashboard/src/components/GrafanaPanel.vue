<template>
  <article class="grafana-panel-card">
    <div class="section-heading">
      <div class="min-w-0">
        <p>{{ t('Embedded operational dashboards') }}</p>
        <h3>{{ title }}</h3>
      </div>
      <button
        v-if="showOpenButton && canRender"
        type="button"
        class="btn-secondary w-full sm:w-auto"
        @click="openInGrafana"
      >
        {{ t('Open in Grafana') }}
      </button>
    </div>

    <p
      v-if="description"
      class="mt-3 text-sm text-slate-600 dark:text-slate-300"
    >
      {{ description }}
    </p>

    <p
      v-if="!canRender"
      class="mt-4 rounded-lg border border-amber-200 bg-amber-50 p-3 text-sm font-semibold text-amber-900 dark:border-amber-700/70 dark:bg-amber-900/30 dark:text-amber-100"
    >
      {{ t('Panel not configured') }}
    </p>

    <div
      v-else
      class="mt-4 min-w-0"
    >
      <div
        v-if="isLoading"
        class="mb-3 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm font-semibold text-slate-700 dark:border-slate-700 dark:bg-slate-900/40 dark:text-slate-200"
      >
        {{ t('Loading embedded panel...') }}
      </div>
      <iframe
        :src="iframeUrl"
        class="block w-full max-w-full rounded-lg border border-slate-200 bg-white shadow-inner dark:border-slate-800 dark:bg-slate-950"
        :style="{ height: heightStyle }"
        loading="lazy"
        referrerpolicy="no-referrer"
        @load="onLoad"
        @error="onError"
      />
      <p class="mt-3 text-xs font-semibold text-slate-500 dark:text-slate-400">
        {{ t('If the embedded view does not render, open the dashboard in Grafana and sign in if required.') }}
      </p>
      <p
        v-if="loadFailed"
        class="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm font-semibold text-red-700 dark:border-red-700/70 dark:bg-red-900/30 dark:text-red-100"
      >
        {{ t('Grafana unavailable') }}
      </p>
    </div>
  </article>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { t } from '../prefs'

type EmbedMode = 'panel' | 'dashboard'

type GrafanaPanelProps = {
  title: string
  description?: string
  dashboardUid: string
  slug: string
  panelId?: number
  from?: string
  to?: string
  refresh?: string
  height?: number | string
  mode?: EmbedMode
  showOpenButton?: boolean
  grafanaBaseUrl?: string
  theme?: 'dark' | 'light'
  orgId?: number
  kiosk?: boolean
  timezone?: string
}

const props = withDefaults(defineProps<GrafanaPanelProps>(), {
  description: '',
  from: 'now-30d',
  to: 'now',
  refresh: '30s',
  height: 320,
  mode: 'panel',
  showOpenButton: true,
  grafanaBaseUrl: '',
  theme: 'dark',
  orgId: 1,
  kiosk: true,
  timezone: 'browser',
})

const emit = defineEmits<{
  loaded: []
  failed: []
}>()

const isLoading = ref(true)
const loadFailed = ref(false)

const normalizedBaseUrl = computed(() => {
  const value = props.grafanaBaseUrl || import.meta.env.VITE_GRAFANA_BASE_URL || 'http://localhost:33010'
  return value.replace(/\/+$/, '')
})

const hasPanelId = computed(() => typeof props.panelId === 'number' && Number.isFinite(props.panelId))
const canRender = computed(() => props.mode === 'dashboard' || hasPanelId.value)

const iframeUrl = computed(() => {
  if (!canRender.value) return ''

  const pathPrefix = props.mode === 'dashboard' ? 'd' : 'd-solo'
  const query = new URLSearchParams({
    orgId: String(props.orgId),
    from: props.from,
    to: props.to,
    timezone: props.timezone,
    refresh: props.refresh,
    theme: props.theme,
  })

  if (props.mode === 'panel' && props.panelId !== undefined) {
    query.set('panelId', String(props.panelId))
  }

  let queryString = query.toString()
  if (props.mode === 'dashboard' && props.kiosk) {
    queryString = `${queryString}&kiosk`
  }

  return `${normalizedBaseUrl.value}/${pathPrefix}/${props.dashboardUid}/${props.slug}?${queryString}`
})

const heightStyle = computed(() => (typeof props.height === 'number' ? `${props.height}px` : props.height))

watch(
  () => iframeUrl.value,
  () => {
    isLoading.value = true
    loadFailed.value = false
  },
)

function onLoad() {
  isLoading.value = false
  loadFailed.value = false
  emit('loaded')
}

function onError() {
  isLoading.value = false
  loadFailed.value = true
  emit('failed')
}

function openInGrafana() {
  window.open(iframeUrl.value, '_blank', 'noopener,noreferrer')
}
</script>
