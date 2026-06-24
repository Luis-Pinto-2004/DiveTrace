<script setup lang="ts">
import { computed } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { visibleSections } from './navigation'
import logoUrl from '@/assets/branding/drivolution-logo.png'

defineProps<{ collapsed?: boolean }>()

const auth = useAuthStore()
const sections = computed(() => visibleSections(auth.roleKey))

const icons: Record<string, string> = {
  grid: 'M3 3h7v7H3V3zm0 11h7v7H3v-7zm11-11h7v7h-7V3zm0 11h7v7h-7v-7z',
  orders: 'M4 4h16v4H4V4zm0 6h16v10H4V10zm4 3h8v2H8v-2z',
  line: 'M4 18l5-6 4 3 7-9',
  quality: 'M9 12l2 2 4-4m1-5H6a2 2 0 00-2 2v12a2 2 0 002 2h12a2 2 0 002-2V7a2 2 0 00-2-2z',
  map: 'M9 4l6 2 5-2v14l-5 2-6-2-5 2V6l5-2zm0 0v14m6-12v14',
  customer: 'M12 12a4 4 0 100-8 4 4 0 000 8zm-7 8a7 7 0 0114 0H5z',
  settings: 'M12 8a4 4 0 100 8 4 4 0 000-8zM2 12h3m14 0h3M12 2v3m0 14v3M5 5l2 2m10 10l2 2M5 19l2-2m10-10l2-2',
  user: 'M12 12a4 4 0 100-8 4 4 0 000 8zm-7 8a7 7 0 0114 0',
  classic: 'M4 6h16M4 12h16M4 18h16',
}
</script>

<template>
  <aside
    class="dt-sb"
    :class="{ 'dt-sb--collapsed': collapsed }"
  >
    <div
      class="dt-sb__brand"
      :class="{ 'dt-sb__brand--collapsed': collapsed }"
    >
      <img
        :src="logoUrl"
        alt="Drivolution"
        class="dt-sb__logo-img"
      >
      <span
        v-if="!collapsed"
        class="dt-sb__brand-title"
      >
        <span class="dt-sb__brand-main">DriveTrace</span>
        <span class="dt-sb__brand-accent">Core</span>
      </span>
    </div>

    <nav
      class="dt-sb__nav"
      aria-label="Navegação principal"
    >
      <div
        v-for="section in sections"
        :key="section.title"
        class="dt-sb__group"
      >
        <p
          v-if="!collapsed"
          class="dt-sb__group-title"
        >
          {{ section.title }}
        </p>
        <RouterLink
          v-for="link in section.links"
          :key="link.to"
          :to="link.to"
          class="dt-sb__link"
          active-class="dt-sb__link--active"
          :title="link.label"
        >
          <svg
            class="dt-sb__icon"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
            aria-hidden="true"
          >
            <path :d="icons[link.icon]" />
          </svg>
          <span
            v-if="!collapsed"
            class="dt-sb__link-label"
          >{{ link.label }}</span>
        </RouterLink>
      </div>
    </nav>
  </aside>
</template>

<style scoped>
.dt-sb {
  width: 248px;
  flex-shrink: 0;
  background: var(--dt-sidebar-bg);
  color: #cbd5e1;
  display: flex;
  flex-direction: column;
  transition: width var(--dt-motion-base) var(--dt-ease);
  overflow: hidden;
}
.dt-sb--collapsed {
  width: 68px;
}
.dt-sb__brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1.15rem 1rem 1.05rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  text-align: center;
}
.dt-sb__brand--collapsed {
  align-items: center;
  padding: 1.15rem 0.5rem;
}
.dt-sb__logo-img {
  height: 34px;
  width: auto;
  max-width: 100%;
  display: block;
  /* logótipo azul sobre fundo escuro: recolorir a branco para contraste */
  filter: brightness(0) invert(1);
}
.dt-sb__brand--collapsed .dt-sb__logo-img {
  height: 26px;
  max-width: 44px;
  object-fit: contain;
  object-position: center;
}
.dt-sb__brand-title {
  display: inline-flex;
  align-items: baseline;
  gap: 0.32rem;
  line-height: 1;
}
.dt-sb__brand-main {
  font-size: 1.02rem;
  font-weight: 800;
  letter-spacing: 0.04em;
  color: #fff;
  white-space: nowrap;
}
.dt-sb__brand-accent {
  font-size: 1.02rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  color: #7dd3fc;
  text-transform: uppercase;
  white-space: nowrap;
}
.dt-sb__nav {
  flex: 1;
  overflow-y: auto;
  padding: 0.75rem 0.5rem;
}
.dt-sb__group {
  margin-bottom: 0.75rem;
}
.dt-sb__group-title {
  font-size: 0.625rem;
  font-weight: 900;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #64748b;
  padding: 0 0.6rem;
  margin: 0 0 0.35rem;
}
.dt-sb__link {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  padding: 0.55rem 0.6rem;
  border-radius: 9px;
  color: #cbd5e1;
  text-decoration: none;
  font-size: 0.8125rem;
  font-weight: 700;
  transition: background var(--dt-motion-fast) var(--dt-ease), color var(--dt-motion-fast) var(--dt-ease);
}
.dt-sb__link:hover {
  background: rgba(255, 255, 255, 0.06);
  color: #fff;
}
.dt-sb__link--active {
  background: linear-gradient(135deg, rgba(8, 119, 216, 0.35), rgba(8, 119, 216, 0.15));
  color: #fff;
  box-shadow: inset 2px 0 0 #38bdf8;
}
.dt-sb__icon {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
}
.dt-sb__link-label {
  white-space: nowrap;
}
</style>
