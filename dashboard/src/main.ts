import { createApp } from 'vue'
import { createPinia } from 'pinia'
import AppRoot from './AppRoot.vue'
import router from './router'
import { usePreferencesStore } from './stores/preferences'
import './styles/tokens.css'
import './style.css'

const app = createApp(AppRoot)
const pinia = createPinia()

app.use(pinia)
app.use(router)

// Aplica o tema persistido antes do primeiro render para evitar flash.
usePreferencesStore().hydrate()

app.mount('#app')
