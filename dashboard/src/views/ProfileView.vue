<script setup lang="ts">
import { computed, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { usePreferencesStore } from '@/stores/preferences'
import { applyProfileUpdate, changePassword } from '@/data/demoUsers'
import { roleLabel } from '@/utils/access'

const auth = useAuthStore()
const prefs = usePreferencesStore()

const initials = computed(() => (auth.user?.name ?? 'U').slice(0, 1).toUpperCase())
const username = computed(() => auth.user?.username ?? '')

// Dados pessoais (editáveis)
const name = ref(auth.user?.name ?? '')
const email = ref(auth.user?.email ?? '')
const phone = ref(auth.user?.phone ?? '')
const personalMsg = ref<{ tone: 'ok' | 'err'; text: string } | null>(null)

function savePersonal() {
  if (!name.value.trim()) {
    personalMsg.value = { tone: 'err', text: 'O nome não pode ficar vazio.' }
    return
  }
  auth.updateProfile({ name: name.value, email: email.value, phone: phone.value })
  applyProfileUpdate(username.value, { name: name.value, email: email.value, phone: phone.value })
  personalMsg.value = { tone: 'ok', text: 'Dados atualizados.' }
  window.setTimeout(() => (personalMsg.value = null), 2600)
}

const personalDirty = computed(
  () =>
    name.value !== (auth.user?.name ?? '') ||
    email.value !== (auth.user?.email ?? '') ||
    phone.value !== (auth.user?.phone ?? ''),
)

// Palavra-passe
const currentPw = ref('')
const newPw = ref('')
const confirmPw = ref('')
const pwMsg = ref<{ tone: 'ok' | 'err'; text: string } | null>(null)

function savePassword() {
  if (newPw.value !== confirmPw.value) {
    pwMsg.value = { tone: 'err', text: 'A confirmação não coincide.' }
    return
  }
  const result = changePassword(username.value, currentPw.value, newPw.value)
  if (!result.ok) {
    pwMsg.value = { tone: 'err', text: result.error ?? 'Não foi possível alterar.' }
    return
  }
  pwMsg.value = { tone: 'ok', text: 'Palavra-passe alterada.' }
  currentPw.value = ''
  newPw.value = ''
  confirmPw.value = ''
  window.setTimeout(() => (pwMsg.value = null), 2600)
}

const canSubmitPw = computed(() => currentPw.value.length > 0 && newPw.value.length >= 4 && confirmPw.value.length > 0)
</script>

<template>
  <div class="prof">
    <!-- Identidade -->
    <section class="card prof__id">
      <div class="prof__avatar">
        {{ initials }}
      </div>
      <div class="prof__id-text">
        <p class="prof__id-name">
          {{ auth.user?.name }}
        </p>
        <p class="prof__id-meta">
          <span class="prof__chip">{{ roleLabel(auth.roleKey) }}</span>
          <span class="prof__id-user">@{{ username }}</span>
        </p>
      </div>
    </section>

    <div class="prof__grid">
      <!-- Dados pessoais -->
      <section class="card">
        <h2 class="card__title">
          Dados pessoais
        </h2>
        <label class="fld"><span>Nome a apresentar</span><input
          v-model="name"
          type="text"
          autocomplete="name"
        ></label>
        <label class="fld"><span>Email</span><input
          v-model="email"
          type="email"
          autocomplete="email"
          placeholder="nome@empresa.pt"
        ></label>
        <label class="fld"><span>Telefone</span><input
          v-model="phone"
          type="tel"
          autocomplete="tel"
          placeholder="+351 ..."
        ></label>

        <div class="fld fld--readonly">
          <span>Nome de utilizador</span>
          <div class="readonly">
            {{ username }} <em>não editável</em>
          </div>
        </div>
        <div class="fld fld--readonly">
          <span>Perfil</span>
          <div class="readonly">
            {{ roleLabel(auth.roleKey) }} <em>definido pela administração</em>
          </div>
        </div>

        <button
          type="button"
          class="btn"
          :disabled="!personalDirty"
          @click="savePersonal"
        >
          Guardar alterações
        </button>
        <p
          v-if="personalMsg"
          class="msg"
          :class="personalMsg.tone === 'ok' ? 'msg--ok' : 'msg--err'"
          role="status"
        >
          {{ personalMsg.text }}
        </p>
      </section>

      <div class="prof__col">
        <!-- Preferências -->
        <section class="card">
          <h2 class="card__title">
            Preferências
          </h2>
          <p class="fld-label">
            Tema
          </p>
          <div class="seg">
            <button
              type="button"
              class="seg__btn"
              :class="{ 'seg__btn--active': !prefs.isDark }"
              @click="prefs.setTheme('light')"
            >
              Claro
            </button>
            <button
              type="button"
              class="seg__btn"
              :class="{ 'seg__btn--active': prefs.isDark }"
              @click="prefs.setTheme('dark')"
            >
              Escuro
            </button>
          </div>
        </section>

        <!-- Segurança -->
        <section class="card">
          <h2 class="card__title">
            Segurança
          </h2>
          <label class="fld"><span>Palavra-passe atual</span><input
            v-model="currentPw"
            type="password"
            autocomplete="current-password"
          ></label>
          <label class="fld"><span>Nova palavra-passe</span><input
            v-model="newPw"
            type="password"
            autocomplete="new-password"
            placeholder="mín. 4 caracteres"
          ></label>
          <label class="fld"><span>Confirmar nova</span><input
            v-model="confirmPw"
            type="password"
            autocomplete="new-password"
          ></label>
          <button
            type="button"
            class="btn"
            :disabled="!canSubmitPw"
            @click="savePassword"
          >
            Alterar palavra-passe
          </button>
          <p
            v-if="pwMsg"
            class="msg"
            :class="pwMsg.tone === 'ok' ? 'msg--ok' : 'msg--err'"
            role="status"
          >
            {{ pwMsg.text }}
          </p>
        </section>
      </div>
    </div>
  </div>
</template>

<style scoped>
.prof {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.card {
  background: var(--dt-surface);
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius-lg);
  padding: 1rem;
  box-shadow: var(--dt-shadow-card);
}
.card__title {
  margin: 0 0 0.8rem;
  font-size: 0.95rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.prof__id {
  display: flex;
  align-items: center;
  gap: 1rem;
}
.prof__avatar {
  width: 56px;
  height: 56px;
  border-radius: 9999px;
  display: grid;
  place-items: center;
  font-size: 1.4rem;
  font-weight: 900;
  color: #fff;
  background: linear-gradient(135deg, #0877d8, #065aa7);
  flex-shrink: 0;
}
.prof__id-name {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 900;
  color: var(--dt-text-strong);
}
.prof__id-meta {
  margin: 0.25rem 0 0;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.prof__chip {
  font-size: 0.68rem;
  font-weight: 800;
  color: var(--dt-info-text);
  background: var(--dt-info-surface);
  border: 1px solid var(--dt-info-border);
  border-radius: 9999px;
  padding: 0.1rem 0.55rem;
}
.prof__id-user {
  font-size: 0.78rem;
  color: var(--dt-neutral-text);
}
.prof__grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  align-items: start;
}
@media (max-width: 900px) {
  .prof__grid {
    grid-template-columns: 1fr;
  }
}
.prof__col {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.fld {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  margin-bottom: 0.7rem;
}
.fld span {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--dt-text-strong);
}
.fld input {
  border: 1px solid var(--dt-border);
  border-radius: var(--dt-radius);
  padding: 0.55rem;
  font-size: 0.85rem;
  background: var(--dt-surface-2);
  color: var(--dt-text-strong);
  font-family: inherit;
}
.fld--readonly {
  margin-bottom: 0.7rem;
}
.readonly {
  font-size: 0.82rem;
  font-weight: 700;
  color: var(--dt-neutral-text);
  background: var(--dt-neutral-surface);
  border: 1px dashed var(--dt-border);
  border-radius: var(--dt-radius);
  padding: 0.5rem 0.6rem;
}
.readonly em {
  font-style: normal;
  font-weight: 600;
  font-size: 0.68rem;
  color: var(--dt-neutral-solid);
  margin-left: 0.3rem;
}
.fld-label {
  margin: 0 0 0.4rem;
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--dt-text-strong);
}
.seg {
  display: inline-flex;
  background: var(--dt-neutral-surface);
  border: 1px solid var(--dt-border);
  border-radius: 9999px;
  padding: 3px;
}
.seg__btn {
  border: none;
  background: transparent;
  color: var(--dt-neutral-text);
  border-radius: 9999px;
  padding: 0.4rem 1.1rem;
  font-size: 0.78rem;
  font-weight: 800;
  cursor: pointer;
}
.seg__btn--active {
  background: var(--dt-brand-500);
  color: #fff;
}
.btn {
  width: 100%;
  margin-top: 0.3rem;
  border: none;
  border-radius: var(--dt-radius);
  padding: 0.6rem;
  background: linear-gradient(135deg, #0877d8, #065aa7);
  color: #fff;
  font-weight: 900;
  font-size: 0.82rem;
  cursor: pointer;
}
.btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
.msg {
  margin: 0.6rem 0 0;
  font-size: 0.75rem;
  font-weight: 700;
}
.msg--ok {
  color: var(--dt-ok-text);
}
.msg--err {
  color: var(--dt-critical-text);
}
.hint {
  margin: 0.6rem 0 0;
  font-size: 0.66rem;
  color: var(--dt-neutral-text);
}
</style>
