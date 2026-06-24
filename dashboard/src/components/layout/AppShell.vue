<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import AppSidebar from './AppSidebar.vue'
import AppTopbar from './AppTopbar.vue'
import { useBreakpoint } from '@/composables/useBreakpoint'

const route = useRoute()
const { isMobile } = useBreakpoint()
const collapsed = ref(false)

const title = computed(() => (route.meta.title as string) ?? 'DriveTrace Core')
const subtitle = computed(() => route.meta.subtitle as string | undefined)

function toggleSidebar() {
  collapsed.value = !collapsed.value
}
</script>

<template>
  <div class="dt-shell">
    <AppSidebar :collapsed="collapsed || isMobile" />
    <div class="dt-shell__main">
      <AppTopbar
        :title="title"
        :subtitle="subtitle"
        @toggle-sidebar="toggleSidebar"
      />
      <main class="dt-shell__content">
        <RouterView v-slot="{ Component }">
          <Transition
            name="dt-fade"
            mode="out-in"
          >
            <component :is="Component" />
          </Transition>
        </RouterView>
      </main>
    </div>
  </div>
</template>

<style scoped>
.dt-shell {
  display: flex;
  height: 100vh;
  overflow: hidden;
  background: var(--dt-app-bg, #f4f8fb);
}
:global(.dark) .dt-shell {
  background: var(--dt-app-bg);
}
.dt-shell__main {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.dt-shell__content {
  flex: 1;
  overflow-y: auto;
  padding: 1.25rem;
}
@media (min-width: 1280px) {
  .dt-shell__content {
    padding: 1.5rem 2rem;
  }
}
.dt-fade-enter-active,
.dt-fade-leave-active {
  transition: opacity var(--dt-motion-base) var(--dt-ease), transform var(--dt-motion-base) var(--dt-ease);
}
.dt-fade-enter-from {
  opacity: 0;
  transform: translateY(6px);
}
.dt-fade-leave-to {
  opacity: 0;
}
</style>
