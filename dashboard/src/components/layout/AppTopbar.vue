<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { usePreferencesStore } from '@/stores/preferences'
import { roleLabel } from '@/utils/access'

defineProps<{ title: string; subtitle?: string }>()
defineEmits<{ (e: 'toggle-sidebar'): void }>()

const router = useRouter()
const auth = useAuthStore()
const prefs = usePreferencesStore()

const initials = computed(() => (auth.user?.name ?? 'DT').slice(0, 1).toUpperCase())

function logout() {
  auth.logout()
  void router.push('/login')
}

function goProfile() {
  void router.push('/perfil')
}
</script>

<template>
  <header class="dt-tb">
    <button
      type="button"
      class="dt-tb__icon-btn"
      aria-label="Alternar menu"
      @click="$emit('toggle-sidebar')"
    >
      <svg
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        stroke-width="2"
        stroke-linecap="round"
        aria-hidden="true"
      >
        <path d="M4 6h16M4 12h16M4 18h16" />
      </svg>
    </button>

    <div class="dt-tb__title">
      <h1 class="dt-tb__h1">
        {{ title }}
      </h1>
      <p
        v-if="subtitle"
        class="dt-tb__sub"
      >
        {{ subtitle }}
      </p>
    </div>

    <div class="dt-tb__actions">
      <button
        type="button"
        class="dt-tb__icon-btn"
        :aria-label="prefs.isDark ? 'Mudar para tema claro' : 'Mudar para tema escuro'"
        @click="prefs.toggleTheme()"
      >
        <svg
          v-if="prefs.isDark"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          aria-hidden="true"
        >
          <circle
            cx="12"
            cy="12"
            r="4"
          /><path d="M12 2v2M12 20v2M4 12H2M22 12h-2M5 5l1.5 1.5M17.5 17.5L19 19M19 5l-1.5 1.5M6.5 17.5L5 19" />
        </svg>
        <svg
          v-else
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
          aria-hidden="true"
        >
          <path d="M21 12.8A9 9 0 1111.2 3 7 7 0 0021 12.8z" />
        </svg>
      </button>

      <button
        type="button"
        class="dt-tb__user"
        title="O meu perfil"
        @click="goProfile"
      >
        <div class="dt-tb__avatar">
          {{ initials }}
        </div>
        <div class="dt-tb__user-text">
          <span class="dt-tb__user-name">{{ auth.user?.name }}</span>
          <span class="dt-tb__user-role">{{ roleLabel(auth.roleKey) }}</span>
        </div>
      </button>

      <button
        type="button"
        class="dt-tb__logout"
        @click="logout"
      >
        Sair
      </button>
    </div>
  </header>
</template>

<style scoped>
.dt-tb {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  height: 60px;
  padding: 0 1rem;
  background: var(--dt-surface);
  border-bottom: 1px solid var(--dt-neutral-border);
}
:global(.dark) .dt-tb {
  background: var(--dt-surface);
  border-color: var(--dt-border);
}
.dt-tb__icon-btn {
  width: 38px;
  height: 38px;
  border-radius: 9px;
  border: 1px solid var(--dt-neutral-border);
  background: transparent;
  color: var(--dt-neutral-text);
  display: grid;
  place-items: center;
  cursor: pointer;
}
.dt-tb__icon-btn svg {
  width: 18px;
  height: 18px;
}
.dt-tb__icon-btn:hover {
  background: var(--dt-neutral-surface);
}
.dt-tb__title {
  flex: 1;
  min-width: 0;
}
.dt-tb__h1 {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 900;
  color: var(--dt-text-strong);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
:global(.dark) .dt-tb__h1 {
  color: #fff;
}
.dt-tb__sub {
  margin: 0;
  font-size: 0.6875rem;
  color: var(--dt-neutral-text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.dt-tb__actions {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}
.dt-tb__user {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  border: none;
  background: transparent;
  border-radius: 9999px;
  padding: 0.2rem 0.5rem 0.2rem 0.2rem;
  cursor: pointer;
  transition: background var(--dt-motion-fast) var(--dt-ease);
}
.dt-tb__user:hover {
  background: var(--dt-neutral-surface);
}
.dt-tb__avatar {
  width: 34px;
  height: 34px;
  border-radius: 9999px;
  background: #dbeafe;
  color: #0877d8;
  display: grid;
  place-items: center;
  font-weight: 900;
  font-size: 0.8rem;
}
:global(.dark) .dt-tb__avatar {
  background: var(--dt-info-surface);
  color: var(--dt-info-text);
}
.dt-tb__user-text {
  display: none;
  flex-direction: column;
}
@media (min-width: 768px) {
  .dt-tb__user-text {
    display: flex;
  }
}
.dt-tb__user-name {
  font-size: 0.75rem;
  font-weight: 800;
  color: var(--dt-text-strong);
  line-height: 1.1;
}
:global(.dark) .dt-tb__user-name {
  color: #e2e8f0;
}
.dt-tb__user-role {
  font-size: 0.625rem;
  color: var(--dt-neutral-text);
}
.dt-tb__logout {
  border: 1px solid var(--dt-neutral-border);
  background: transparent;
  color: var(--dt-neutral-text);
  border-radius: var(--dt-radius);
  padding: 0.4rem 0.7rem;
  font-size: 0.75rem;
  font-weight: 800;
  cursor: pointer;
}
.dt-tb__logout:hover {
  background: var(--dt-critical-surface);
  color: var(--dt-critical-text);
  border-color: var(--dt-critical-border);
}
</style>
