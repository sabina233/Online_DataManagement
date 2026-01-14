<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch } from 'vue';
import * as echarts from 'echarts';
import { useDataStore } from '../stores/data';
import { useI18n } from 'vue-i18n';
import { 
    TrendingUp, 
    Globe, 
    Target, 
    AlertCircle,
    Clock,
    Zap
} from 'lucide-vue-next';

/**
 * 初始化 Store 和 国际化工具
 */
const dataStore = useDataStore();
const { t, locale } = useI18n();

// 状态变量
const currentTime = ref('');
const timer = ref<any>(null);

// 图表 DOM 引用
const mapChartRef = ref<HTMLElement | null>(null);
const trendChartRef = ref<HTMLElement | null>(null);
const rankChartRef = ref<HTMLElement | null>(null);

// ECharts 实例
let mapChart: echarts.ECharts | null = null;
let trendChart: echarts.ECharts | null = null;
let rankChart: echarts.ECharts | null = null;

// 从 DataStore 获取响应式记录 (确保不为 undefined)
const allRecords = computed(() => dataStore.records || []);

/**
 * 仪表盘核心指标计算
 */
const totalAc = computed(() => allRecords.value.reduce((sum, r: any) => sum + (r.jan_ac || 0) + (r.feb_ac || 0) + (r.mar_ac || 0) + (r.apr_ac || 0) + (r.may_ac || 0) + (r.jun_ac || 0) + (r.jul_ac || 0) + (r.aug_ac || 0) + (r.sep_ac || 0) + (r.oct_ac || 0) + (r.nov_ac || 0) + (r.dec_ac || 0), 0));
const totalFc = computed(() => allRecords.value.reduce((sum, r: any) => sum + (r.jan_fc || 0) + (r.feb_fc || 0) + (r.mar_fc || 0) + (r.apr_fc || 0) + (r.may_fc || 0) + (r.jun_fc || 0) + (r.jul_fc || 0) + (r.aug_fc || 0) + (r.sep_fc || 0) + (r.oct_fc || 0) + (r.nov_fc || 0) + (r.dec_fc || 0), 0));
const globalRatio = computed(() => totalFc.value === 0 ? 0 : Math.round((totalAc.value / totalFc.value) * 100));

/**
 * 复杂指标：环比增长 (MoM)
 * 查找最后一个有实际数据的月份，并与前一个月对比
 */
const momGrowth = computed(() => {
    const months = ['jan', 'feb', 'mar', 'apr', 'may', 'jun', 'jul', 'aug', 'sep', 'oct', 'nov', 'dec'];
    let currentIdx = -1;
    for (let i = 11; i >= 0; i--) {
        const sum = allRecords.value.reduce((s, r: any) => s + (r[`${months[i]}_ac`] || 0), 0);
        if (sum > 0) {
            currentIdx = i;
            break;
        }
    }
    if (currentIdx <= 0) return 0;
    
    const currVal = allRecords.value.reduce((s, r: any) => s + (r[`${months[currentIdx]}_ac`] || 0), 0);
    const prevVal = allRecords.value.reduce((s, r: any) => s + (r[`${months[currentIdx-1]}_ac`] || 0), 0);
    
    if (prevVal === 0) return 0;
    return ((currVal / prevVal - 1) * 100).toFixed(1);
});

/**
 * 获取表现最佳的数据大区
 */
const bestRegion = computed(() => {
    const locMap = new Map<string, number>();
    allRecords.value.forEach((r: any) => {
        const sum = (r.jan_ac || 0) + (r.feb_ac || 0) + (r.mar_ac || 0) + (r.apr_ac || 0) + (r.may_ac || 0) + (r.jun_ac || 0) + (r.jul_ac || 0) + (r.aug_ac || 0) + (r.sep_ac || 0) + (r.oct_ac || 0) + (r.nov_ac || 0) + (r.dec_ac || 0);
        locMap.set(r.location, (locMap.get(r.location) || 0) + sum);
    });
    let max = -1;
    let name = 'N/A';
    locMap.forEach((v, k) => {
        if (v > max) { max = v; name = k; }
    });
    return name;
});

/**
 * 更新系统时间
 */
const updateTime = () => {
    const now = new Date();
    currentTime.value = now.toLocaleTimeString();
};

/**
 * 初始化所有 ECharts 图表
 */
const initCharts = () => {
    if (mapChartRef.value) {
        mapChart = echarts.init(mapChartRef.value, 'dark');
        renderMap();
    }
    if (trendChartRef.value) {
        trendChart = echarts.init(trendChartRef.value, 'dark');
        renderTrend();
    }
    if (rankChartRef.value) {
        rankChart = echarts.init(rankChartRef.value, 'dark');
        renderRank();
    }
};

/**
 * 渲染区域分布饼图
 */
const renderMap = () => {
    if (!mapChart) return;
    
    const locMap = new Map<string, { ac: number, fc: number }>();
    allRecords.value.forEach((r: any) => {
        const val = locMap.get(r.location) || { ac: 0, fc: 0 };
        const ac = (r.jan_ac || 0) + (r.feb_ac || 0) + (r.mar_ac || 0) + (r.apr_ac || 0) + (r.may_ac || 0) + (r.jun_ac || 0) + (r.jul_ac || 0) + (r.aug_ac || 0) + (r.sep_ac || 0) + (r.oct_ac || 0) + (r.nov_ac || 0) + (r.dec_ac || 0);
        const fc = (r.jan_fc || 0) + (r.feb_fc || 0) + (r.mar_fc || 0) + (r.apr_fc || 0) + (r.may_fc || 0) + (r.jun_fc || 0) + (r.jul_fc || 0) + (r.aug_fc || 0) + (r.sep_fc || 0) + (r.oct_fc || 0) + (r.nov_fc || 0) + (r.dec_fc || 0);
        locMap.set(r.location, { ac: val.ac + ac, fc: val.fc + fc });
    });

    const data = Array.from(locMap.entries()).map(([name, val]) => ({
        name,
        value: val.ac,
        ratio: val.fc === 0 ? 0 : (val.ac / val.fc * 100).toFixed(1)
    })).sort((a, b) => b.value - a.value);

    mapChart.setOption({
        backgroundColor: 'transparent',
        title: { text: t('big_screen.distribution'), left: 'left', textStyle: { color: '#66fcf1', fontSize: 14 } },
        tooltip: { trigger: 'item', formatter: '{b}<br/>Value: {c}<br/>Ratio: {ratio}%' },
        series: [{
            type: 'pie',
            radius: ['45%', '75%'],
            center: ['50%', '55%'],
            avoidLabelOverlap: false,
            itemStyle: { borderRadius: 10, borderColor: '#0b0c10', borderWidth: 3 },
            label: { show: false },
            emphasis: { label: { show: true, fontSize: 16, fontWeight: 'bold', color: '#66fcf1' } },
            data: data
        }]
    });
};

/**
 * 渲染年度趋势分析图（含预测逻辑）
 */
const renderTrend = () => {
    if (!trendChart) return;
    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const acData = months.map(m => allRecords.value.reduce((sum, r: any) => sum + (r[`${m.toLowerCase()}_ac`] || 0), 0));
    const fcData = months.map(m => allRecords.value.reduce((sum, r: any) => sum + (r[`${m.toLowerCase()}_fc`] || 0), 0));

    // 预测分析
    let lastIdxWithData = -1;
    for (let i = acData.length - 1; i >= 0; i--) {
        const val = acData[i];
        if (val !== undefined && val !== null && val > 0) {
            lastIdxWithData = i;
            break;
        }
    }

    const predData = acData.map((v, i) => {
        if (v !== null && v > 0) return null; 
        if (lastIdxWithData < 1) return null;
        const lastVal = acData[lastIdxWithData] || 0;
        const prevVal = acData[lastIdxWithData - 1] || 0;
        if (prevVal === 0) return lastVal;
        const growthRate = lastVal / prevVal;
        const pred = lastVal * Math.pow(growthRate, i - lastIdxWithData);
        return Math.round(pred > 0 ? pred : 0);
    });

    trendChart.setOption({
        backgroundColor: 'transparent',
        tooltip: { trigger: 'axis' },
        legend: { data: [t('big_screen.total_actual'), t('big_screen.total_forecast'), t('big_screen.predictive')], textStyle: { color: '#888' }, bottom: 0 },
        grid: { top: '15%', bottom: '15%', left: '10%', right: '5%' },
        xAxis: { type: 'category', data: months, axisLabel: { color: '#888' } },
        yAxis: { type: 'value', splitLine: { lineStyle: { color: '#1f2833' } }, axisLabel: { color: '#888' } },
        series: [
            { name: t('big_screen.total_actual'), type: 'line', data: acData, smooth: true, areaStyle: { opacity: 0.1 }, itemStyle: { color: '#66fcf1' }, symbolSize: 6 },
            { name: t('big_screen.total_forecast'), type: 'line', data: fcData, smooth: true, lineStyle: { type: 'dashed', width: 1 }, itemStyle: { color: '#45a29e' } },
            { name: t('big_screen.predictive'), type: 'line', data: predData, smooth: true, lineStyle: { type: 'dotted', width: 2 }, itemStyle: { color: '#c5c6c7' } }
        ]
    });
};

/**
 * 渲染区域排行榜
 */
const renderRank = () => {
    if (!rankChart) return;
    const locMap = new Map<string, number>();
    allRecords.value.forEach((r: any) => {
        const sum = (r.jan_ac || 0) + (r.feb_ac || 0) + (r.mar_ac || 0) + (r.apr_ac || 0) + (r.may_ac || 0) + (r.jun_ac || 0) + (r.jul_ac || 0) + (r.aug_ac || 0) + (r.sep_ac || 0) + (r.oct_ac || 0) + (r.nov_ac || 0) + (r.dec_ac || 0);
        locMap.set(r.location, (locMap.get(r.location) || 0) + sum);
    });

    const sorted = Array.from(locMap.entries()).sort((a, b) => b[1] - a[1]).slice(0, 5);

    rankChart.setOption({
        backgroundColor: 'transparent',
        title: { text: t('big_screen.regions_rank'), left: 'left', textStyle: { color: '#66fcf1', fontSize: 14 } },
        grid: { left: '3%', right: '10%', bottom: '3%', top: '30px', containLabel: true },
        xAxis: { type: 'value', splitLine: { show: false }, axisLabel: { show: false } },
        yAxis: { type: 'category', data: sorted.map(d => d[0]).reverse(), axisLabel: { color: '#ccc' } },
        series: [{
            type: 'bar',
            data: sorted.map(d => d[1]).reverse(),
            itemStyle: {
                color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
                    { offset: 0, color: '#1f2833' },
                    { offset: 1, color: '#66fcf1' }
                ]),
                borderRadius: [0, 4, 4, 0]
            },
            label: { show: true, position: 'right', color: '#66fcf1', formatter: '{c}' }
        }]
    });
};

/**
 * 组件生命周期
 */
onMounted(async () => {
    // 默认加载全局数据
    await dataStore.loadAllRecords();
    
    updateTime();
    timer.value = setInterval(updateTime, 1000);
    
    initCharts();
    window.addEventListener('resize', handleResize);
});

onUnmounted(() => {
    if (timer.value) clearInterval(timer.value);
    window.removeEventListener('resize', handleResize);
    mapChart?.dispose();
    trendChart?.dispose();
    rankChart?.dispose();
});

const handleResize = () => {
    mapChart?.resize();
    trendChart?.resize();
    rankChart?.resize();
};

watch([allRecords, locale], () => {
    renderMap();
    renderTrend();
    renderRank();
}, { deep: true });

const toggleLang = () => {
    locale.value = locale.value === 'en' ? 'zh' : 'en';
};

</script>

<template>
  <div class="big-screen-wrapper">
    <!-- Header -->
    <header class="bs-header">
        <div class="header-left">
            <div class="time-box">
                <Clock :size="18" />
                <span>{{ currentTime }}</span>
            </div>
        </div>
        <div class="header-center">
            <h1>{{ t('big_screen.title') }}</h1>
            <div class="sub-line">{{ t('big_screen.subtitle') }}</div>
        </div>
        <div class="header-right">
            <button class="lang-toggle-btn" @click="toggleLang">
                {{ locale === 'en' ? '中文' : 'EN' }}
            </button>
            <div class="status-indicator">
                <span class="dot pulse"></span>
                <span>{{ t('big_screen.system_online') }}</span>
            </div>
        </div>
    </header>

    <!-- Main Grid -->
    <div class="bs-content">
        <!-- Top KPIs -->
        <div class="kpi-row">
            <div class="kpi-card">
                <div class="kpi-icon"><Globe :size="24" /></div>
                <div class="kpi-info">
                    <div class="label">{{ t('big_screen.total_actual') }}</div>
                    <div class="value">{{ totalAc.toLocaleString() }}</div>
                    <div class="sub-info" :class="Number(momGrowth) >= 0 ? 'up' : 'down'">
                        {{ t('big_screen.mom_growth') }}: {{ Number(momGrowth) > 0 ? '+' : '' }}{{ momGrowth }}%
                    </div>
                </div>
            </div>
            <div class="kpi-card">
                <div class="kpi-icon blue"><TrendingUp :size="24" /></div>
                <div class="kpi-info">
                    <div class="label">{{ t('big_screen.total_forecast') }}</div>
                    <div class="value">{{ totalFc.toLocaleString() }}</div>
                    <div class="sub-info">{{ t('big_screen.achievement') }}: {{ globalRatio }}%</div>
                </div>
            </div>
            <div class="kpi-card">
                <div class="kpi-icon green"><Target :size="24" /></div>
                <div class="kpi-info">
                    <div class="label">{{ t('big_screen.achievement') }}</div>
                    <div class="value-bar-container">
                        <div class="value-text">{{ globalRatio }}%</div>
                        <div class="progress-bg"><div class="progress-fill" :style="{ width: globalRatio + '%' }"></div></div>
                    </div>
                </div>
            </div>
            <div class="kpi-card">
                <div class="kpi-icon orange"><AlertCircle :size="24" /></div>
                <div class="kpi-info">
                    <div class="label">{{ t('big_screen.active_brands') }}</div>
                    <div class="value">{{ dataStore.brands.length }}</div>
                    <div class="sub-info highlight">{{ t('big_screen.best_region') }}: {{ bestRegion }}</div>
                </div>
            </div>
            <!-- Mock Log Card to Make it fuller -->
             <div class="kpi-card extra-card">
                <div class="kpi-icon purple"><Zap :size="24" /></div>
                <div class="kpi-info">
                    <div class="label">{{ t('big_screen.real_time_log') }}</div>
                    <div class="log-list">
                       <span class="log-item">[10:02] Sync Success</span>
                       <span class="log-item">[09:58] Data Updated</span>
                    </div>
                </div>
            </div>
        </div>

        <div class="dashboard-grid">
            <!-- Left Panel -->
            <div class="panel side-panel">
                <div ref="rankChartRef" class="chart-box"></div>
                 <div class="panel-footer">
                    <span>{{ t('big_screen.update_freq') }}: 60{{ t('big_screen.unit_secs') }}</span>
                </div>
            </div>

            <!-- Center Panel -->
            <div class="panel center-panel">
                <div ref="mapChartRef" class="chart-box big"></div>
            </div>

            <!-- Right Panel -->
            <div class="panel side-panel">
                <div class="panel-header">{{ t('big_screen.trend') }}</div>
                <div ref="trendChartRef" class="chart-box"></div>
                <div class="panel-footer">
                    <span>{{ t('big_screen.data_source') }}: {{ dataStore.brands.length }} {{ t('big_screen.unit_records') }}</span>
                </div>
            </div>
        </div>
    </div>
  </div>
</template>

<style scoped>
.big-screen-wrapper {
    position: fixed; top: 0; left: 0; right: 0; bottom: 0;
    background-color: #0b0c10;
    background-image: 
        radial-gradient(circle at 50% 50%, rgba(102, 252, 241, 0.05) 0%, transparent 80%),
        linear-gradient(rgba(102, 252, 241, 0.02) 1px, transparent 1px),
        linear-gradient(90deg, rgba(102, 252, 241, 0.02) 1px, transparent 1px);
    background-size: cover, 30px 30px, 30px 30px;
    color: #fff;
    z-index: 1000;
    display: flex; flex-direction: column;
    font-family: 'Inter', sans-serif;
    overflow: hidden;
}

/* Header */
.bs-header {
    height: 80px;
    background: linear-gradient(to bottom, #1f2833 0%, transparent 100%);
    display: flex; align-items: center; justify-content: space-between;
    padding: 0 40px;
    border-bottom: 1px solid rgba(102, 252, 241, 0.1);
    box-shadow: 0 10px 30px rgba(0,0,0,0.5);
}

.header-center { text-align: center; }
.header-center h1 {
    font-size: 1.8rem; letter-spacing: 4px; font-weight: 800;
    background: linear-gradient(180deg, #fff 0%, #66fcf1 100%);
    -webkit-background-clip: text; background-clip: text; -webkit-text-fill-color: transparent;
    margin: 0;
    text-shadow: 0 0 20px rgba(102, 252, 241, 0.3);
}
.sub-line { font-size: 0.7rem; color: #45a29e; letter-spacing: 2px; margin-top: 4px; font-weight: 600; }

.header-left { display: flex; align-items: center; gap: 20px; }
.time-box { color: #66fcf1; font-weight: 600; display: flex; align-items: center; gap: 10px; font-family: monospace; font-size: 1.1rem; }

.header-right { display: flex; align-items: center; gap: 30px; }
.lang-toggle-btn {
    background: rgba(102, 252, 241, 0.1);
    border: 1px solid rgba(102, 252, 241, 0.3);
    color: #66fcf1;
    padding: 4px 12px;
    border-radius: 4px;
    font-size: 0.75rem;
    cursor: pointer;
    transition: all 0.3s;
    font-weight: 600;
}
.lang-toggle-btn:hover { background: rgba(102, 252, 241, 0.2); border-color: #66fcf1; }

.status-indicator { display: flex; align-items: center; gap: 10px; font-size: 0.7rem; color: #45a29e; font-weight: 600; }
.dot { width: 8px; height: 8px; border-radius: 50%; background: #66fcf1; box-shadow: 0 0 10px #66fcf1; }
.pulse { animation: pulse-animation 2s infinite; }

@keyframes pulse-animation {
    0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(102, 252, 241, 0.7); }
    70% { transform: scale(1.1); box-shadow: 0 0 0 10px rgba(102, 252, 241, 0); }
    100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(102, 252, 241, 0); }
}

/* Content */
.bs-content { flex: 1; padding: 24px; display: flex; flex-direction: column; gap: 24px; }

/* 5 Columns for KPIs */
.kpi-row { display: grid; grid-template-columns: repeat(5, 1fr); gap: 20px; }

.kpi-card {
    background: rgba(31, 40, 51, 0.4);
    backdrop-filter: blur(10px);
    border: 1px solid rgba(102, 252, 241, 0.1);
    padding: 20px; border-radius: 12px;
    display: flex; align-items: center; gap: 15px;
    transition: transform 0.3s, border-color 0.3s;
}
.kpi-card:hover { transform: translateY(-5px); border-color: rgba(102, 252, 241, 0.4); }

.kpi-icon { 
    width: 60px; height: 60px; border-radius: 12px; 
    background: rgba(102, 252, 241, 0.1); color: #66fcf1; 
    display: flex; align-items: center; justify-content: center;
    box-shadow: inset 0 0 15px rgba(102, 252, 241, 0.1);
    flex-shrink: 0;
}
.kpi-icon.blue { color: #3b82f6; background: rgba(59, 130, 246, 0.1); }
.kpi-icon.green { color: #10b981; background: rgba(16, 185, 129, 0.1); }
.kpi-icon.orange { color: #f59e0b; background: rgba(245, 158, 11, 0.1); }
.kpi-icon.purple { color: #a855f7; background: rgba(168, 85, 247, 0.1); }

.kpi-info { flex: 1; min-width: 0; }
.kpi-info .label { font-size: 0.7rem; color: #45a29e; font-weight: 700; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 1px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.kpi-info .value { font-size: 1.4rem; font-weight: 800; color: #fff; line-height: 1; margin-bottom: 6px; font-family: 'Oswald', sans-serif; }

.log-list { font-size: 0.75rem; color: #ccc; display: flex; flex-direction: column; gap: 4px; }
.log-item { white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.sub-info { font-size: 0.7rem; color: #45a29e; font-weight: 600; }
.sub-info.up { color: #10b981; }
.sub-info.down { color: #ef4444; }
.sub-info.highlight { color: #66fcf1; }

.value-bar-container { display: flex; flex-direction: column; gap: 6px; }
.value-text { font-size: 1.4rem; font-weight: 800; color: #66fcf1; }
.progress-bg { width: 100%; height: 4px; background: rgba(255,255,255,0.1); border-radius: 2px; overflow: hidden; }
.progress-fill { height: 100%; background: #66fcf1; box-shadow: 0 0 10px #66fcf1; border-radius: 2px; transition: width 1s ease-out; }

/* Dashboard Grid */
.dashboard-grid { flex: 1; display: grid; grid-template-columns: 350px 1fr 350px; gap: 24px; }

.panel {
    background: rgba(31, 40, 51, 0.2);
    border: 1px solid rgba(102, 252, 241, 0.05);
    border-radius: 12px;
    padding: 20px;
    display: flex; flex-direction: column;
    box-shadow: 0 4px 30px rgba(0,0,0,0.3);
}

.panel-header {
    font-size: 0.85rem; font-weight: 700; color: #66fcf1;
    margin-bottom: 20px; padding-bottom: 12px;
    border-bottom: 1px solid rgba(102, 252, 241, 0.1);
    letter-spacing: 1px;
}

.panel-footer {
    margin-top: auto;
    padding-top: 10px;
    border-top: 1px solid rgba(102, 252, 241, 0.05);
    font-size: 0.7rem;
    color: #45a29e;
    text-align: right;
}

.chart-box { flex: 1; min-height: 250px; }
.chart-box.big { min-height: 450px; }

@media (max-width: 1600px) {
    .kpi-row { grid-template-columns: repeat(3, 1fr); }
    .extra-card { display: none; }
}

@media (max-width: 1400px) {
    .dashboard-grid { grid-template-columns: 1fr 1fr; }
    .center-panel { grid-column: span 2; order: -1; }
}
</style>
