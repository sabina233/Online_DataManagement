<script setup lang="ts">
import { ref } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useDataStore } from '../stores/data';
import { useI18n } from 'vue-i18n';
import { 
  LayoutDashboard, 
  Database, 
  ChevronDown, 
  ChevronRight, 
  LogOut, 
  User,
  PlusCircle,
  Globe,
  Users,
  BarChart3
} from 'lucide-vue-next';

/**
 * 注入业务及视图工具
 */
const authStore = useAuthStore();
const dataStore = useDataStore();
const router = useRouter();
const route = useRoute();
const { t, locale } = useI18n();

// 侧边栏及用户菜单状态
const isBrandMenuOpen = ref(true);
const isUserMenuOpen = ref(false);

/**
 * 菜单切换逻辑
 */
const toggleBrandMenu = () => isBrandMenuOpen.value = !isBrandMenuOpen.value;
const toggleUserMenu = () => isUserMenuOpen.value = !isUserMenuOpen.value;

/**
 * 全局语言切换 (中/英)
 */
const toggleLanguage = () => {
    locale.value = locale.value === 'en' ? 'zh' : 'en';
};

/**
 * 退出登录
 */
const logout = () => {
  authStore.logout();
  router.push('/login');
};

/**
 * 导航至品牌详情页
 */
const navigateToBrand = (brand: string) => {
  router.push({ name: 'brand-detail', params: { name: brand } });
};
</script>

<template>
  <div class="layout-wrapper">
    <!-- Sidebar -->
    <aside class="sidebar">
      <div class="logo-area">
        <h1>DataMgt</h1>
      </div>
      
      <nav class="nav-links">
        <!-- Home -->
        <RouterLink to="/" class="nav-item" :class="{ active: route.name === 'home' }">
          <LayoutDashboard :size="20" />
          <span>{{ t('layout.dashboard') }}</span>
        </RouterLink>

        <!-- Big Screen -->
        <RouterLink to="/big-screen" class="nav-item" :class="{ active: route.name === 'big-screen' }">
          <BarChart3 :size="20" />
          <span>{{ t('layout.big_screen') }}</span>
        </RouterLink>

        <!-- Brands -->
        <div class="nav-group">
          <div class="nav-item group-header" @click="toggleBrandMenu">
            <div class="flex-row">
              <Database :size="20" />
              <span>{{ t('layout.brands') }}</span>
            </div>
            <component :is="isBrandMenuOpen ? ChevronDown : ChevronRight" :size="16" />
          </div>
          
          <div v-if="isBrandMenuOpen" class="sub-nav">
            <div 
              v-for="brand in dataStore.brands" 
              :key="brand"
              class="sub-nav-item"
              :class="{ active: route.params.name === brand }"
              @click="navigateToBrand(brand)"
            >
              {{ brand }}
            </div>
          </div>
        </div>

        <!-- Entry -->
        <RouterLink to="/entry" class="nav-item" :class="{ active: route.name === 'data-entry' }">
            <PlusCircle :size="20" />
            <span>{{ t('layout.entry') }}</span>
        </RouterLink>

        <!-- Admin: User Management -->
        <RouterLink 
            v-if="authStore.user?.role === 'admin'"
            to="/admin/users" 
            class="nav-item" 
            :class="{ active: route.name === 'user-management' }"
        >
            <Users :size="20" /> <!-- Need to import Users icon -->
            <span>User Management</span>
        </RouterLink>
      </nav>
    </aside>

    <!-- Main Content -->
    <div class="main-content">
      <!-- Header -->
      <header class="top-header">
        <div class="header-left">
           <!-- Dynamic Title based on route translation logic could be complex, simplifying -->
           <h2>{{ route.name === 'brand-detail' ? route.params.name : (route.name === 'home' ? t('layout.dashboard') : t('layout.entry')) }}</h2>
        </div>
        
        <div class="header-right">
          <!-- Language Switcher -->
           <button class="btn btn-text" @click="toggleLanguage">
             <Globe :size="18" />
             <span>{{ t('common.switch_lang') }}</span>
           </button>

          <div class="user-profile" @click="toggleUserMenu">
            <div class="avatar">
               <!-- Show image or initial -->
               <img v-if="authStore.user?.avatar" :src="authStore.user.avatar" class="avatar-img" />
               <span v-else>{{ authStore.user?.username?.charAt(0).toUpperCase() || 'U' }}</span>
            </div>
            <span class="username">{{ authStore.user?.username || 'User' }}</span>
            <ChevronDown :size="16" />
            
            <!-- Dropdown -->
            <div v-if="isUserMenuOpen" class="dropdown-menu">
              <div class="dropdown-item" @click="router.push('/profile')">
                <User :size="16" />
                <span>{{ t('layout.profile') }}</span>
              </div>
              <div class="dropdown-item" @click="logout">
                <LogOut :size="16" />
                <span>{{ t('layout.logout') }}</span>
              </div>
            </div>
          </div>
        </div>
      </header>
      
      <!-- Page View -->
      <main class="page-container">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.layout-wrapper {
  display: flex;
  height: 100vh;
  width: 100vw;
  background-color: var(--bg-body);
}

/* Sidebar */
.sidebar {
  width: 260px;
  background-color: #ffffff;
  border-right: 1px solid var(--border-light);
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
  transition: width 0.3s ease;
  z-index: 10;
}

.logo-area {
  height: 64px;
  display: flex;
  align-items: center;
  padding: 0 24px;
  border-bottom: 1px solid var(--border-light);
}

.logo-area h1 {
  font-size: 1.25rem;
  font-weight: 700;
  color: var(--primary-600);
  background: linear-gradient(135deg, var(--primary-600), var(--primary-800));
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.nav-links {
  flex: 1;
  padding: 24px 12px;
  overflow-y: auto;
}

.nav-item {
  display: flex;
  align-items: center;
  justify-content: space-between; /* For Chevron */
  padding: 12px 16px;
  margin-bottom: 4px;
  border-radius: 8px;
  color: var(--text-secondary);
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  text-decoration: none;
}
.nav-item .flex-row {
  display: flex;
  align-items: center;
  gap: 12px;
}
.nav-item > span, .nav-item .flex-row span {
  margin-left: 12px;
}

.nav-item:hover {
  background-color: var(--primary-50);
  color: var(--primary-700);
}

.nav-item.active {
  background-color: var(--primary-50);
  color: var(--primary-600);
  font-weight: 600;
}

.sub-nav {
  margin-left: 12px;
  padding-left: 12px;
  border-left: 2px solid var(--border-light);
  margin-top: 4px;
  margin-bottom: 12px;
}

.sub-nav-item {
  padding: 8px 16px;
  font-size: 0.9rem;
  color: var(--text-secondary);
  cursor: pointer;
  border-radius: 6px;
  margin-bottom: 2px;
  transition: color 0.2s;
}

.sub-nav-item:hover {
  color: var(--primary-600);
}

.sub-nav-item.active {
  color: var(--primary-600);
  background-color: #fafbfc;
  font-weight: 500;
}

/* Main Content Area */
.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* Header */
.top-header {
  height: 64px;
  background-color: white;
  border-bottom: 1px solid var(--border-light);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 32px;
  flex-shrink: 0;
}

.header-left h2 {
  font-size: 1.1rem;
  font-weight: 600;
  color: var(--text-main);
  text-transform: capitalize;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.btn-text {
  background: none;
  border: none;
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--text-secondary);
  font-weight: 500;
  cursor: pointer;
  padding: 8px 12px;
  border-radius: 8px;
  transition: all 0.2s;
}

.btn-text:hover {
  background-color: var(--primary-50);
  color: var(--primary-600);
}

.user-profile {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  position: relative;
  padding: 6px 12px;
  border-radius: 20px;
  transition: background 0.2s;
}

.user-profile:hover {
  background-color: var(--bg-body);
}

.avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background-color: var(--primary-100);
  color: var(--primary-600);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}
.avatar img { width: 100%; height: 100%; object-fit: cover; }

.avatar-small { 
    width: 32px; height: 32px; border-radius: 50%; background: var(--primary-100); color: var(--primary-600);
    display: flex; align-items: center; justify-content: center; font-size: 0.8rem; font-weight: 600;
    overflow: hidden;
}
.avatar-img { width: 100%; height: 100%; object-fit: cover; }

.username {
  font-weight: 500;
  font-size: 0.95rem;
}

.dropdown-menu {
  position: absolute;
  top: 50px;
  right: 0;
  width: 180px;
  background: white;
  border: 1px solid var(--border-light);
  box-shadow: var(--shadow-lg);
  border-radius: 8px;
  padding: 8px 0;
  z-index: 100;
  animation: fadeIn 0.2s ease;
}

.dropdown-item {
  padding: 10px 16px;
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--danger);
  cursor: pointer;
  transition: background 0.1s;
}

.dropdown-item:hover {
  background-color: #fef2f2;
}

/* Page Container */
.page-container {
  flex: 1;
  overflow-y: auto;
  padding: 32px;
  background-color: var(--bg-body);
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(-5px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
