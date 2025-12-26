<script setup lang="ts">
import { useAuthStore } from '../stores/auth';
import { useDataStore } from '../stores/data';
import { useI18n } from 'vue-i18n';
import { ArrowRight } from 'lucide-vue-next';

/**
 * 仪表盘主页：展示核心汇总指标及快速操作入口
 */
const authStore = useAuthStore();
const dataStore = useDataStore();
const { t } = useI18n();
</script>

<template>
  <div class="dashboard">
    <div class="welcome-banner card">
        <h1>{{ t('home.hello', { name: authStore.user?.username }) }}</h1>
        <p>{{ t('home.welcome_text') }}</p>
    </div>

    <div class="metrics-grid">
        <div class="card metric-card">
            <h3>{{ t('home.total_brands') }}</h3>
            <div class="value">{{ dataStore.brands.length }}</div>
            <div class="sub">{{ t('home.active_tracking') }}</div>
        </div>
        <div class="card metric-card action-card" @click="$router.push('/entry')">
            <div class="content">
                <h3>{{ t('home.submit_data') }}</h3>
                <p>{{ t('home.add_new_records') }}</p>
            </div>
            <div class="icon-circle">
                <ArrowRight :size="24" />
            </div>
        </div>
    </div>
  </div>
</template>

<style scoped>
.dashboard {
    max-width: 1000px;
    margin: 0 auto;
}

.welcome-banner {
    background: linear-gradient(120deg, var(--primary-600), var(--primary-800));
    color: white;
    margin-bottom: 32px;
}

.welcome-banner h1 {
    font-size: 2rem;
    margin-bottom: 8px;
}
.welcome-banner p {
    color: var(--primary-100);
    max-width: 600px;
    font-size: 1.1rem;
}

.metrics-grid {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    gap: 24px;
}

.metric-card h3 {
    color: var(--text-secondary);
    font-size: 0.9rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    margin-bottom: 12px;
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
    display: flex;
    align-items: center;
    justify-content: space-between;
    transition: all 0.3s;
}

.action-card:hover {
    transform: translateY(-4px);
    border-color: var(--primary-200);
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
}
</style>
