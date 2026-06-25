import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AppShell from '@/components/layout/AppShell.vue'
import LoginView from '@/views/LoginView.vue'

// Vistas com carregamento diferido (code-splitting) para manter o arranque leve.
const CockpitView = () => import('@/views/CockpitView.vue')
const OrdersView = () => import('@/views/OrdersView.vue')
const LineAnalysisView = () => import('@/views/LineAnalysisView.vue')
const QualityBoardView = () => import('@/views/QualityBoardView.vue')
const TraceMapView = () => import('@/views/TraceMapView.vue')
const CustomerView = () => import('@/views/CustomerView.vue')
const ManagementView = () => import('@/views/ManagementView.vue')
const ResourcesView = () => import('@/views/ResourcesView.vue')
const ProfileView = () => import('@/views/ProfileView.vue')
const ClassicView = () => import('@/views/ClassicView.vue')

// Perfis "internos" (fábrica) — tudo menos o cliente.
const INTERNAL = ['admin', 'supervisor', 'operator', 'quality']

const routes: RouteRecordRaw[] = [
  { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
  {
    path: '/',
    component: AppShell,
    children: [
      { path: '', redirect: '/cockpit' },
      {
        path: 'cockpit',
        name: 'cockpit',
        component: CockpitView,
        meta: { title: 'Painel de operações', subtitle: 'Visão visual da fábrica e prioridades do turno', roles: INTERNAL },
      },
      {
        path: 'ordens',
        name: 'orders',
        component: OrdersView,
        meta: { title: 'Encomendas e produção', subtitle: 'Aceitar encomendas e iniciar produção', roles: ['admin', 'supervisor'] },
      },
      {
        path: 'linhas',
        name: 'lines',
        component: LineAnalysisView,
        meta: { title: 'Análise por linha', subtitle: 'Carga, gargalos e ações por unidade', roles: INTERNAL },
      },
      {
        path: 'qualidade',
        name: 'quality',
        component: QualityBoardView,
        meta: { title: 'Quadro de qualidade', subtitle: 'Decisões pendentes de qualidade', roles: ['admin', 'supervisor', 'quality'] },
      },
      {
        path: 'rastreabilidade',
        name: 'trace',
        component: TraceMapView,
        meta: { title: 'Mapa de rastreabilidade', subtitle: 'Grafo técnico: encomenda, unidade, suporte, secções, qualidade e eventos', roles: INTERNAL },
      },
      {
        path: 'cliente',
        name: 'customer',
        component: CustomerView,
        meta: { title: 'As minhas encomendas', subtitle: 'Submeter pedidos e acompanhar o estado', roles: ['admin', 'client'] },
      },
      {
        path: 'gestao',
        name: 'management',
        component: ManagementView,
        meta: { title: 'Gestão de dados', subtitle: 'Criar produtos, linhas e secções', roles: ['admin', 'supervisor'] },
      },
      {
        path: 'funcionarios',
        name: 'resources',
        component: ResourcesView,
        meta: {
          title: 'Funcionários e recursos',
          subtitle: 'Pessoas e recursos produtivos da fábrica',
          roles: ['admin', 'supervisor'],
        },
      },
      {
        path: 'perfil',
        name: 'profile',
        component: ProfileView,
        meta: { title: 'O meu perfil', subtitle: 'Gerir os seus dados e preferências' },
      },
    ],
  },
  // Interface clássico: sem botão e restrito a administração (mantém funcionalidades legadas).
  { path: '/classico', name: 'classic', component: ClassicView, meta: { roles: ['admin'] } },
  { path: '/:pathMatch(.*)*', redirect: '/cockpit' },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 }
  },
})

function landingFor(roleKey: string | null): string {
  if (roleKey === 'client') return '/cliente'
  return '/cockpit'
}

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta?.public) {
    if (auth.isAuthenticated && to.name === 'login') return landingFor(auth.roleKey)
    return true
  }
  if (!auth.isAuthenticated) {
    return { name: 'login' }
  }
  const roles = to.meta?.roles as string[] | undefined
  if (roles && roles.length && !auth.isAdmin && (!auth.roleKey || !roles.includes(auth.roleKey))) {
    return landingFor(auth.roleKey)
  }
  return true
})

export default router
