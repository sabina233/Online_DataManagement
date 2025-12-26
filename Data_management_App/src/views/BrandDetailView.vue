<template>
  <div class="brand-detail">
    <div class="page-title">
        <div class="breadcrumb">
            <span class="root" @click="$router.push('/')">Brands</span>
            <span class="separator">/</span>
            <span class="active">{{ currentBrand }}</span>
        </div>
    </div>

    <!-- Annual Preview Section (Consolidated) -->
    <div class="card table-container">
        <div class="section-toolbar no-header">
            <div class="filter-group">
                <div class="filter-item">
                    <label>{{ t('brand.year_filter') }}:</label>
                    <select v-model="timeFilter" class="input-std">
                        <option value="All">All Years</option>
                        <option>2024</option>
                        <option>2025</option>
                    </select>
                </div>
                <div class="filter-item">
                    <label>{{ t('brand.location_filter') }}:</label>
                    <select v-model="locationFilter" class="input-std">
                        <option value="All">All Regions</option>
                        <option v-for="loc in uniqueLocations" :key="loc" :value="loc">{{ loc }}</option>
                    </select>
                </div>
                <div class="filter-item">
                    <label>{{ t('brand.date_filter') }}:</label>
                    <select v-model="quarterFilter" class="input-std">
                        <option value="All">All Period</option>
                        <option value="Q1">Q1</option><option value="Q2">Q2</option>
                        <option value="Q3">Q3</option><option value="Q4">Q4</option>
                        <option v-for="m in monthsList" :key="m.key" :value="m.key">{{ m.label }}</option>
                    </select>
                </div>
            </div>
            <button class="btn btn-secondary btn-sm" @click="exportYearTable">
                <Download :size="14" /> {{ t('brand.export') }}
            </button>
        </div>

        <div class="table-scroll">
            <table>
                <thead>
                    <tr>
                        <th class="sticky-col loc-col">Location</th>
                        <th class="sticky-col item-col">Item</th>
                         <template v-for="col in variableMonths" :key="col.key">
                              <th :class="{'q-header': col.isQ}">{{ col.label }} AC</th>
                              <th :class="{'q-header': col.isQ}">{{ col.label }} FC</th>
                              <th :class="{'q-header': col.isQ}">{{ col.label }} AC vs FC</th>
                         </template>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="row in paginatedRecords" :key="row.id">
                        <td class="sticky-col loc-col">{{ row.location }}</td>
                        <td class="sticky-col item-col">{{ row.item }}</td>
                        <template v-for="col in variableMonths" :key="col.key + '_vals'">
                            <td :class="{'bg-q': col.isQ}">{{ formatNum((row as any)[`${col.key}_ac`]) }}</td>
                            <td :class="{'bg-q': col.isQ}">{{ formatNum((row as any)[`${col.key}_fc`]) }}</td>
                            <td :class="[
                                    (row as any)[`${col.key}_diff`] < 100 ? 'text-danger' : 'text-success',
                                    {'bg-q': col.isQ}
                                ]">
                                {{ formatNum((row as any)[`${col.key}_diff`]).includes('.') ? (row as any)[`${col.key}_diff`].toFixed(2) : (row as any)[`${col.key}_diff`] }}%
                            </td>
                        </template>
                    </tr>
                    <tr v-if="filteredRecords.length === 0">
                        <td colspan="50" class="empty-state">{{ t('brand.no_data') }}</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Pagination -->
        <div class="pagination" v-if="totalPages > 1">
            <button class="btn btn-secondary btn-sm" :disabled="currentPage === 1" @click="currentPage--">Prev</button>
            <span class="page-info">Page {{ currentPage }} of {{ totalPages }}</span>
            <button class="btn btn-secondary btn-sm" :disabled="currentPage === totalPages" @click="currentPage++">Next</button>
        </div>
    </div>

    <!-- Chart Section -->
    <div class="charts-grid">
        <div class="card chart-card">
            <ChartComponent 
                :title="chartTitle" 
                :xAxisData="chartXAxis" 
                :seriesData="chartSeries" 
            />
        </div>
        <div class="card chart-card">
            <ChartComponent 
                :title="ratioChartTitle" 
                :xAxisData="chartXAxis" 
                :seriesData="ratioChartSeries" 
            />
        </div>
        <div class="card chart-card full-width-chart">
            <ChartComponent 
                :title="regionalChartTitle" 
                :xAxisData="chartXAxis" 
                :seriesData="regionalChartSeries" 
            />
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useDataStore } from '../stores/data';
import ChartComponent from '../components/ChartComponent.vue';
import { useI18n } from 'vue-i18n';
import * as XLSX from 'xlsx';
import { Download } from 'lucide-vue-next';

/**
 * 品牌详情视图：展示特定品牌的年度数据表格及分析图表
 */
const route = useRoute();
const dataStore = useDataStore();
const { t, locale } = useI18n();

// 从路由参数中提取当前品牌名称
const currentBrand = computed(() => (route.params.name as string) || '');

// 表格筛选状态
const timeFilter = ref('All');
const quarterFilter = ref('All');
const locationFilter = ref('All');

/**
 * 获取当前品牌下的所有唯一大区列表，用于筛选下拉框
 */
const uniqueLocations = computed(() => {
    const locs = dataStore.records.map(r => r.location).filter(Boolean) as string[];
    return Array.from(new Set(locs));
});

// 月份常量定义
const monthsList = [
    { key: 'jan', label: 'Jan', q: 'Q1' }, { key: 'feb', label: 'Feb', q: 'Q1' }, { key: 'mar', label: 'Mar', q: 'Q1' },
    { key: 'apr', label: 'Apr', q: 'Q2' }, { key: 'may', label: 'May', q: 'Q2' }, { key: 'jun', label: 'Jun', q: 'Q2' },
    { key: 'jul', label: 'Jul', q: 'Q3' }, { key: 'aug', label: 'Aug', q: 'Q3' }, { key: 'sep', label: 'Sep', q: 'Q3' },
    { key: 'oct', label: 'Oct', q: 'Q4' }, { key: 'nov', label: 'Nov', q: 'Q4' }, { key: 'dec', label: 'Dec', q: 'Q4' }
];

// 动态图表标题
const chartTitle = computed(() => {
    return locale.value === 'zh' ? '年度数据预览' : 'Annual Data Overview';
});

/**
 * 动态计算显示的月份列（基于季度或具体月份筛选）
 */
const variableMonths = computed(() => {
   const all = [
        { key: 'jan', label: 'Jan', q: 'Q1' }, { key: 'feb', label: 'Feb', q: 'Q1' }, { key: 'mar', label: 'Mar', q: 'Q1' },
        { key: 'q1', label: 'Q1', isQ: true, q: 'Q1' },
        { key: 'apr', label: 'Apr', q: 'Q2' }, { key: 'may', label: 'May', q: 'Q2' }, { key: 'jun', label: 'Jun', q: 'Q2' },
        { key: 'q2', label: 'Q2', isQ: true, q: 'Q2' },
        { key: 'jul', label: 'Jul', q: 'Q3' }, { key: 'aug', label: 'Aug', q: 'Q3' }, { key: 'sep', label: 'Sep', q: 'Q3' },
        { key: 'q3', label: 'Q3', isQ: true, q: 'Q3' },
        { key: 'oct', label: 'Oct', q: 'Q4' }, { key: 'nov', label: 'Nov', q: 'Q4' }, { key: 'dec', label: 'Dec', q: 'Q4' },
        { key: 'q4', label: 'Q4', isQ: true, q: 'Q4' }
    ];

    if (quarterFilter.value === 'All') return all;
    // 如果选择了某个季度
    if (quarterFilter.value.startsWith('Q')) {
        return all.filter(m => m.q === quarterFilter.value);
    }
    // 如果选择了某个具体月份
    return all.filter(m => m.key === quarterFilter.value);
});

/**
 * 生命周期：挂载时加载数据
 */
onMounted(() => {
    if (currentBrand.value) {
        dataStore.loadRecords(currentBrand.value);
    }
});

// 监听品牌切换，实时重载数据
watch(currentBrand, (newVal) => {
    if (newVal) dataStore.loadRecords(newVal);
});

/**
 * 应用筛选逻辑（年份、大区）
 */
const filteredRecords = computed(() => {
    let recs = dataStore.records || [];
    if (timeFilter.value !== 'All') {
         const y = parseInt(timeFilter.value);
         recs = recs.filter(r => r.year === y);
    }
    if (locationFilter.value !== 'All') {
        recs = recs.filter(r => r.location === locationFilter.value);
    }
    return recs; 
});

// 图表配置项与数据聚合逻辑
const chartXAxis = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
const chartSeries = computed(() => {
    const getData = (key: string) => filteredRecords.value.reduce((sum, r: any) => sum + (r[key] || 0), 0);
    const acData = chartXAxis.map(m => getData(`${m.toLowerCase()}_ac`));
    const fcData = chartXAxis.map(m => getData(`${m.toLowerCase()}_fc`));
    return [
        { name: locale.value === 'zh' ? '实际值 (AC)' : 'Actual (AC)', type: 'bar' as const, data: acData },
        { name: locale.value === 'zh' ? '预测值 (FC)' : 'Forecast (FC)', type: 'line' as const, data: fcData }
    ];
});

const ratioChartTitle = computed(() => {
    return locale.value === 'zh' ? '月度达成率 (%)' : 'Monthly Achievement Rate (%)';
});

const ratioChartSeries = computed(() => {
    const getData = (mKey: string) => {
        const totalAc = filteredRecords.value.reduce((sum, r: any) => sum + (r[`${mKey}_ac`] || 0), 0);
        const totalFc = filteredRecords.value.reduce((sum, r: any) => sum + (r[`${mKey}_fc`] || 0), 0);
        if (totalFc === 0) return 0;
        return Number(((totalAc / totalFc) * 100).toFixed(2));
    };
    const ratioData = chartXAxis.map(m => getData(m.toLowerCase()));
    return [
        { name: '达成率 (%)', type: 'line' as const, data: ratioData, smooth: true }
    ];
});

const regionalChartTitle = computed(() => {
    const loc = locationFilter.value === 'All' ? (locale.value === 'zh' ? '全地区' : 'All Regions') : locationFilter.value;
    return locale.value === 'zh' ? `${loc} 趋势对比` : `${loc} Trend Comparison`;
});

const regionalChartSeries = computed(() => {
    const acData = chartXAxis.map(m => {
        return filteredRecords.value.reduce((sum, r: any) => sum + (r[`${m.toLowerCase()}_ac`] || 0), 0);
    });
    const fcData = chartXAxis.map(m => {
        return filteredRecords.value.reduce((sum, r: any) => sum + (r[`${m.toLowerCase()}_fc`] || 0), 0);
    });

    return [
        { name: locale.value === 'zh' ? '实际值' : 'Actual', type: 'line' as const, data: acData, smooth: true },
        { name: locale.value === 'zh' ? '预测值' : 'Forecast', type: 'line' as const, data: fcData, smooth: true, lineStyle: { type: 'dashed' } }
    ];
});

// 分页逻辑
const currentPage = ref(1);
const pageSize = ref(10);
const paginatedRecords = computed(() => {
    const start = (currentPage.value - 1) * pageSize.value;
    return filteredRecords.value.slice(start, start + pageSize.value);
});
const totalPages = computed(() => Math.ceil(filteredRecords.value.length / pageSize.value));

/**
 * 筛选改变时，自动将页码重置为 1
 */
watch(filteredRecords, () => {
    currentPage.value = 1;
});

/**
 * 导出当前表格数据为 Excel
 */
const exportYearTable = () => {
    const ws = XLSX.utils.json_to_sheet(filteredRecords.value);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, "年度预览");
    XLSX.writeFile(wb, `${currentBrand.value}_年度预览.xlsx`);
};

/**
 * 格式化数字，增加千分位分隔符
 */
const formatNum = (n: number) => n?.toLocaleString() || '0';

</script>

<style scoped>
.brand-detail { display: flex; flex-direction: column; gap: 24px; }
.page-title { margin-bottom: 8px; }
.breadcrumb { font-size: 0.9rem; color: var(--text-secondary); display: flex; gap: 8px; align-items: center; }
.breadcrumb .root { cursor: pointer; }
.breadcrumb .root:hover { color: var(--primary-600); }
.breadcrumb .active { color: var(--text-main); font-weight: 500; }

.card { background: white; border-radius: 12px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1), 0 2px 4px -2px rgba(0,0,0,0.1); border: 1px solid var(--border-light); overflow: hidden; }

/* Section Toolbar */
.section-toolbar {
    padding: 16px 24px;
    background: #fff; border-bottom: 1px solid var(--border-light);
    display: flex; align-items: center; justify-content: space-between;
    flex-wrap: wrap; gap: 16px;
}
.section-toolbar h3 { font-size: 1rem; font-weight: 600; color: var(--text-main); margin: 0; }
.filter-group { display: flex; gap: 12px; align-items: center; }
.filter-label { font-size: 0.9rem; color: var(--text-secondary); }
.input-std { padding: 4px 8px; border: 1px solid var(--border-light); border-radius: 4px; font-size: 0.9rem; }

.table-container { padding: 0; overflow: hidden; margin-top: 12px; }
.table-scroll { overflow-x: auto; width: 100%; max-height: 70vh; }

table { width: 100%; border-collapse: separate; border-spacing: 0; }

th, td {
    padding: 8px 12px;
    border-bottom: 1px solid var(--border-light);
    border-right: 1px solid var(--border-light);
    text-align: right;
    white-space: nowrap;
    font-size: 0.9rem;
}

th { background-color: #f8fafc; font-weight: 600; color: var(--text-secondary); position: sticky; top: 0; z-index: 30; }

/* Sticky Columns */
.sticky-col { position: sticky; background-color: white; z-index: 40; text-align: left; }
.loc-col { left: 0; width: 120px; border-right: 1px solid #e2e8f0; }
.item-col { left: 80px; width: 120px; border-right: 2px solid #cbd5e1; }

th.sticky-col { z-index: 50; background-color: #f8fafc; }

/* Highlights */
.q-header { background-color: #e0f2fe; color: #0369a1; }
.year-header { background-color: #f0fdf4; color: #15803d; }
.bg-q { background-color: #f0f9ff; font-weight: 500; }
.bg-year { background-color: #fcfdfc; font-weight: 600; border-left: 2px solid #86efac; }

.text-danger { color: #ef4444; }
.text-success { color: #10b981; }

.pagination {
    display: flex; align-items: center; justify-content: center; gap: 16px;
    padding: 16px; border-top: 1px solid var(--border-light);
    background: #f8fafc;
}
.page-info { font-size: 0.9rem; color: var(--text-secondary); }

.charts-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-top: 24px; }
.full-width-chart { grid-column: span 2; }
@media (max-width: 1024px) {
    .charts-grid { grid-template-columns: 1fr; }
    .full-width-chart { grid-column: span 1; }
}

.empty-state { text-align: center; padding: 48px; color: var(--text-light); }
.btn-sm { padding: 4px 12px; font-size: 0.8rem; height: 32px; display: flex; align-items: center; gap: 6px; }
</style>
