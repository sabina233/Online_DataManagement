<template>
  <div class="dashboard">
    <div class="welcome-banner card">
        <h1>{{ t('home.hello', { name: authStore.user?.username }) }}</h1>
        <p>{{ t('home.welcome_text') }}</p>
    </div>

    <!-- Quick Access Section -->
    <h2 class="section-title">{{ t('home.quick_access') }}</h2>
    <div class="metrics-grid">
        <div class="card metric-card action-card" @click="$router.push('/entry')">
            <div class="content">
                <h3>{{ t('home.submit_data') }}</h3>
                <p>{{ t('home.add_new_records') }}</p>
            </div>
            <div class="icon-circle">
                <ArrowRight :size="24" />
            </div>
        </div>
        
        <div class="card metric-card action-card" @click="$router.push('/orders/kmart')">
            <div class="content">
                <h3>{{ t('home.view_report') }}</h3>
                <p>{{ t('home.kmart_desc') }}</p>
            </div>
            <div class="icon-circle blue">
                <BarChart2 :size="24" />
            </div>
        </div>

        <div class="card metric-card">
            <h3>{{ t('home.total_brands') }}</h3>
            <div class="value">{{ dataStore.brands.length }}</div>
            <div class="sub">{{ t('home.active_tracking') }}</div>
        </div>
    </div>

    <!-- System Status Section -->
    <h2 class="section-title" style="margin-top: 32px;">{{ t('home.system_status') }}</h2>
    <div class="status-grid">
        <div class="card status-card">
            <div class="status-icon"><Server :size="20"/></div>
            <div class="status-info">
                <div class="label">{{ t('home.server_status') }}</div>
                <div class="val text-success">{{ t('home.online') }}</div>
            </div>
        </div>
         <div class="card status-card">
            <div class="status-icon"><Database :size="20"/></div>
            <div class="status-info">
                <div class="label">{{ t('home.database_status') }}</div>
                <div class="val text-success">{{ t('home.connected') }}</div>
            </div>
        </div>
         <div class="card status-card">
            <div class="status-icon"><Activity :size="20"/></div>
            <div class="status-info">
                <div class="label">{{ t('home.last_sync') }}</div>
                <div class="val">{{ new Date().toLocaleTimeString() }}</div>
            </div>
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '../stores/auth';
import { useDataStore } from '../stores/data';
import { useI18n } from 'vue-i18n';
import { ArrowRight, BarChart2, Server, Database, Activity } from 'lucide-vue-next';

/**
 * 仪表盘主页：展示核心汇总指标及快速操作入口
 */
const authStore = useAuthStore();
const dataStore = useDataStore();
const { t } = useI18n();
</script>

<style scoped>
.dashboard {
    max-width: 1200px;
    margin: 0 auto;
    padding-bottom: 40px;
}

.welcome-banner {
    background: linear-gradient(135deg, var(--primary-600), var(--primary-800));
    color: rgb(240, 12, 183);
    margin-bottom: 32px;
    padding: 32px;
    border-radius: 12px;
    box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06);
}

.welcome-banner h1 {
    font-size: 2rem;
    margin-bottom: 8px;
    font-weight: 700;
}
.welcome-banner p {
    color: var(--primary-200);
    max-width: 600px;
    font-size: 1.1rem;
}

.section-title {
    font-size: 1.25rem;
    color: var(--text-main);
    margin-bottom: 16px;
    font-weight: 600;
}

.metrics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    gap: 24px;
}

.metric-card {
    padding: 24px;
    height: 100%;
    display: flex;
    flex-direction: column;
    justify-content: center;
}

.metric-card h3 {
    color: var(--text-secondary);
    font-size: 0.9rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin-bottom: 8px;
}

.metric-card .value {
    font-size: 2.5rem;
    font-weight: 700;
    color: var(--text-main);
}

.metric-card .sub {
    color: var(--success);
    font-size: 0.9rem;
    margin-top: 4px;
}

.action-card {
    cursor: pointer;
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    transition: all 0.3s;
    border: 1px solid transparent;
}

.action-card:hover {
    transform: translateY(-4px);
    border-color: var(--primary-200);
    box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
}

.action-card .content h3 {
    color: var(--text-main);
    font-size: 1.1rem;
    font-weight: 600;
    margin-bottom: 4px;
    text-transform: none;
}
.action-card .content p {
    color: var(--text-secondary);
    font-size: 0.9rem;
}

.icon-circle {
    width: 48px;
    height: 48px;
    border-radius: 50%;
    background: var(--primary-50);
    color: var(--primary-600);
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
}
.icon-circle.blue {
    background: #eff6ff;
    color: #3b82f6;
}

/* Status Grid */
.status-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    gap: 20px;
}

.status-card {
    padding: 16px;
    display: flex;
    align-items: center;
    gap: 16px;
}

.status-icon {
    width: 40px;
    height: 40px;
    border-radius: 8px;
    background: #f1f5f9;
    color: #64748b;
    display: flex;
    align-items: center;
    justify-content: center;
}

.status-info .label {
    font-size: 0.8rem;
    color: var(--text-secondary);
    margin-bottom: 2px;
}
.status-info .val {
    font-weight: 600;
    font-size: 0.95rem;
}
.text-success { color: var(--success); }
</style>
