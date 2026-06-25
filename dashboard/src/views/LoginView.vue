<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { authenticate, DEMO_USERS } from '@/data/demoUsers'
import logoUrl from '@/assets/branding/drivolution-logo.png'

const router = useRouter()
const auth = useAuthStore()

const username = ref('')
const password = ref('')
const error = ref('')

const landingByRole: Record<string, string> = {
  client: '/cliente',
  quality: '/qualidade',
  operator: '/cockpit',
  supervisor: '/cockpit',
  admin: '/cockpit',
}

function go() {
  const roleKey = auth.roleKey ?? 'admin'
  void router.push(landingByRole[roleKey] ?? '/cockpit')
}

function submit() {
  error.value = ''
  const session = authenticate(username.value, password.value)
  if (!session) {
    error.value = 'Credenciais inválidas. Use um dos perfis demo abaixo.'
    return
  }
  auth.setUser(session)
  go()
}

function quick(roleKey: string) {
  const demo = DEMO_USERS.find((u) => u.roleKey === roleKey)
  if (!demo) return
  username.value = demo.username
  password.value = demo.password
  submit()
}
</script>

<template>
  <div class="dt-login">
    <div
      class="dt-login__panel"
      data-demo="login"
    >
      <div class="dt-login__brand">
        <div class="dt-login__logo">
          <img
            :src="logoUrl"
            alt="Drivolution"
            class="dt-login__logo-img"
          >
        </div>
        <h1 class="dt-login__title">
          <span class="dt-login__title-main">DriveTrace</span>
          <span class="dt-login__title-accent">Core</span>
        </h1>
      </div>

      <form
        class="dt-login__form"
        @submit.prevent="submit"
      >
        <label class="dt-login__field">
          <span>Utilizador</span>
          <input
            v-model="username"
            type="text"
            autocomplete="username"
            placeholder="supervisor"
            data-demo="login-user"
          >
        </label>
        <label class="dt-login__field">
          <span>Palavra-passe</span>
          <input
            v-model="password"
            type="password"
            autocomplete="current-password"
            placeholder="••••••"
            data-demo="login-pass"
          >
        </label>
        <p
          v-if="error"
          class="dt-login__error"
          role="alert"
        >
          {{ error }}
        </p>
        <button
          type="submit"
          class="dt-login__submit"
        >
          Entrar
        </button>
      </form>

      <div class="dt-login__divider">
        <span>Acesso rápido demo</span>
      </div>
      <div class="dt-login__quick">
        <button
          v-for="u in DEMO_USERS"
          :key="u.roleKey"
          type="button"
          class="dt-login__chip"
          @click="quick(u.roleKey)"
        >
          {{ u.username }}
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dt-login {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 1.5rem;
  background: radial-gradient(1200px 600px at 50% -10%, #0b2948 0%, #061a30 60%, #04101f 100%);
}
.dt-login__panel {
  width: min(26rem, 100%);
  background: var(--dt-surface);
  border-radius: 18px;
  padding: 1.75rem;
  box-shadow: 0 30px 70px rgba(0, 0, 0, 0.35);
}
:global(.dark) .dt-login__panel {
  background: var(--dt-surface);
}
.dt-login__brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.7rem;
  margin-bottom: 1.5rem;
  text-align: center;
}
.dt-login__logo {
  display: flex;
  justify-content: center;
}
.dt-login__logo-img {
  height: 40px;
  width: auto;
  max-width: 240px;
  display: block;
}
.dt-login__title {
  margin: 0;
  display: inline-flex;
  align-items: baseline;
  gap: 0.36rem;
  line-height: 1;
}
.dt-login__title-main {
  font-size: 1.45rem;
  font-weight: 800;
  letter-spacing: 0.03em;
  color: var(--dt-text-strong);
}
:global(.dark) .dt-login__title-main {
  color: #fff;
}
.dt-login__title-accent {
  font-size: 1.45rem;
  font-weight: 800;
  letter-spacing: 0.06em;
  text-transform: uppercase;
  color: var(--dt-brand-500);
}
.dt-login__form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
.dt-login__field {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
}
.dt-login__field span {
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--dt-text-strong);
}
:global(.dark) .dt-login__field span {
  color: #e2e8f0;
}
.dt-login__field input {
  border: 1px solid var(--dt-neutral-border);
  border-radius: var(--dt-radius);
  padding: 0.6rem 0.7rem;
  font-size: 0.875rem;
  background: var(--dt-surface);
}
:global(.dark) .dt-login__field input {
  background: var(--dt-surface-2);
  color: #e2e8f0;
}
.dt-login__error {
  margin: 0;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--dt-critical-text);
}
.dt-login__submit {
  margin-top: 0.25rem;
  border: none;
  border-radius: var(--dt-radius);
  padding: 0.7rem;
  background: linear-gradient(135deg, #0877d8, #065aa7);
  color: #fff;
  font-weight: 900;
  font-size: 0.875rem;
  cursor: pointer;
}
.dt-login__divider {
  display: flex;
  align-items: center;
  text-align: center;
  margin: 1.1rem 0 0.8rem;
  color: var(--dt-neutral-text);
  font-size: 0.6875rem;
  font-weight: 700;
}
.dt-login__divider::before,
.dt-login__divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: var(--dt-neutral-border);
}
.dt-login__divider span {
  padding: 0 0.6rem;
}
.dt-login__quick {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}
.dt-login__chip {
  border: 1px solid var(--dt-neutral-border);
  background: var(--dt-neutral-surface);
  border-radius: 9999px;
  padding: 0.35rem 0.7rem;
  font-size: 0.75rem;
  font-weight: 800;
  color: var(--dt-text-strong);
  cursor: pointer;
}
:global(.dark) .dt-login__chip {
  color: #cbd5e1;
}
.dt-login__chip:hover {
  border-color: #0877d8;
  color: #0877d8;
}
.dt-login__note {
  margin: 0.9rem 0 0;
  font-size: 0.625rem;
  color: var(--dt-neutral-text);
  text-align: center;
}
</style>
