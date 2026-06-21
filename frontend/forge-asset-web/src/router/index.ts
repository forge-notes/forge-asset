import { createRouter, createWebHistory } from 'vue-router'
import { TOKEN_KEY } from '../api/http'
import LoginView from '../views/LoginView.vue'
import AssetsView from '../views/AssetsView.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/assets' },
    { path: '/login', component: LoginView, meta: { guest: true } },
    { path: '/assets', component: AssetsView, meta: { requiresAuth: true } },
    { path: '/:pathMatch(.*)*', redirect: '/assets' },
  ],
})

router.beforeEach((to) => {
  const authenticated = Boolean(localStorage.getItem(TOKEN_KEY))
  if (to.meta.requiresAuth && !authenticated) return '/login'
  if (to.meta.guest && authenticated) return '/assets'
})

export default router
