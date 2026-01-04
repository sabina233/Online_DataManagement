import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import LoginView from '../views/LoginView.vue'
import AppLayout from '../layouts/AppLayout.vue'
import HomeView from '../views/HomeView.vue'
import BrandDetailView from '../views/BrandDetailView.vue'
import DataEntryView from '../views/DataEntryView.vue'
import UserCenterView from '../views/UserCenterView.vue'
import UserManagementView from '../views/UserManagementView.vue'

const router = createRouter({
    history: createWebHistory(),
    routes: [
        {
            path: '/login',
            name: 'login',
            component: LoginView
        },
        {
            path: '/',
            component: AppLayout,
            meta: { requiresAuth: true },
            children: [
                {
                    path: '',
                    name: 'home',
                    component: HomeView
                },
                {
                    path: 'brand/:name',
                    name: 'brand-detail',
                    component: BrandDetailView
                },
                {
                    path: 'entry',
                    name: 'data-entry',
                    component: DataEntryView
                },
                {
                    path: 'profile',
                    name: 'user-profile',
                    component: UserCenterView
                },
                {
                    path: 'admin/users',
                    name: 'user-management',
                    component: UserManagementView,
                    meta: { requiresAdmin: true }
                },
                {
                    path: 'big-screen',
                    name: 'big-screen',
                    component: () => import('../views/BigScreenView.vue'),
                    meta: { title: 'layout.big_screen' }
                },
                {
                    path: 'orders/kmart',
                    name: 'kmart-order',
                    component: () => import('../views/KmartOrderView.vue'),
                    meta: { title: 'Kmart 报表' }
                },
                {
                    path: 'orders/kmart/entry',
                    name: 'kmart-entry',
                    component: () => import('../views/KmartEntryView.vue'),
                    meta: { title: 'Kmart 录入' }
                }
            ]
        }
    ]
})

/**
 * 路由守卫：处理登录拦截与权限验证
 */
router.beforeEach((to, _from, next) => {
    const authStore = useAuthStore()

    // 验证 Token 及登录状态
    if (to.meta.requiresAuth && !authStore.user) {
        next('/login')
    }
    // 验证管理员权限
    else if (to.meta.requiresAdmin && authStore.user?.role !== 'admin') {
        next('/') // 普通用户尝试访问管理页时，重定向至首页
    } else {
        next()
    }
})

export default router;
