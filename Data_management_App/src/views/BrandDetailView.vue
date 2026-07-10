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
                        <option>2026</option>
                    </select>
                </div>
                <div class="filter-item">
                    <label>{{ t('brand.location_filter') }}:</label>
                    <select v-model="locationFilter" class="input-std">
                        <option value="All">All Regions</option>
                        <option v-for="loc in uniqueLocations" :key="loc" :value="loc">{{ loc }}</option>
                    </select>
                </div>
                <!-- Month/Quarter filter for Table -->
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
                <tfoot v-if="filteredRecords.length > 0">
                    <tr class="total-row">
                        <td class="sticky-col loc-col" style="font-weight: 600;">Total</td>
                        <td class="sticky-col item-col"></td>
                        <template v-for="col in variableMonths" :key="'total_' + col.key">
                            <td :class="{'bg-q': col.isQ}" style="font-weight: 600;">{{ formatNum(tableTotals[col.key + '_ac']) }}</td>
                            <td :class="{'bg-q': col.isQ}" style="font-weight: 600;">{{ formatNum(tableTotals[col.key + '_fc']) }}</td>
                            <td :class="[
                                    tableTotals[col.key + '_diff'] < 100 ? 'text-danger' : 'text-success',
                                    {'bg-q': col.isQ}
                                ]" style="font-weight: 600;">
                                {{ formatNum(tableTotals[col.key + '_diff']).includes('.') ? tableTotals[col.key + '_diff'].toFixed(2) : tableTotals[col.key + '_diff'] }}%
                            </td>
                        </template>
                    </tr>
                </tfoot>
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
        <!-- 1. Location Difference Bar Chart -->
        <div class="card chart-card">
            <div class="chart-header">
                <h3>Difference by Location (AC - FC)</h3>
                <div class="filter-item">
                     <label>Until:</label>
                     <select v-model="diffMonthRange" class="input-std">
                        <option v-for="(m, idx) in monthsList" :key="m.key" :value="idx + 1">{{ m.label }}</option>
                     </select>
                </div>
            </div>
            <ChartComponent 
                title="" 
                :xAxisData="locDiffChart.xAxis" 
                :seriesData="locDiffChart.series" 
            />
        </div>
        
        <!-- 2. Monthly Trend Line Chart (AC) -->
        <div class="card chart-card">
            <div class="chart-header">
                <h3>Monthly AC Trend</h3>
            </div>
            <ChartComponent 
                title="" 
                :xAxisData="trendChart.xAxis" 
                :seriesData="trendChart.series" 
            />
        </div>
    </div>

    <!-- YoY Analysis Section -->
    <div class="card yoy-section">
        <div class="section-toolbar no-header">
            <h3 style="margin: 0; font-size: 1rem; color: var(--text-main);">YoY Analysis</h3>
            <div class="filter-group" style="margin-left: auto;">
                <div class="filter-item">
                    <label>Total Until:</label>
                    <select v-model="yoyTotalMonthRange" class="input-std">
                        <option v-for="(m, idx) in monthsList" :key="m.key" :value="idx + 1">{{ m.label }}</option>
                    </select>
                </div>
                <div class="filter-item">
                    <label>Base Year:</label>
                    <select v-model="yoyBaseYear" class="input-std">
                        <option v-for="y in uniqueYears" :key="y" :value="y">{{ y }}</option>
                    </select>
                </div>
                <div class="filter-item">
                    <label>Target Year:</label>
                    <select v-model="yoyTargetYear" class="input-std">
                        <option v-for="y in uniqueYears" :key="y" :value="y">{{ y }}</option>
                    </select>
                </div>
            </div>
        </div>
        
        <div class="table-scroll" style="max-height: unset; border-top: 1px solid var(--border-light);">
            <table>
                <thead>
                    <tr>
                        <th class="sticky-col loc-col">Month</th>
                        <th>{{ yoyTargetYear }} AC</th>
                        <th>{{ yoyTargetYear }} FC</th>
                        <th>{{ yoyBaseYear }} AC</th>
                        <th>{{ yoyBaseYear }} FC</th>
                        <th>AC YoY (%)</th>
                        <th>FC YoY (%)</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="row in yoyTableData" :key="row.month">
                        <td class="sticky-col loc-col">{{ row.label }}</td>
                        <td>{{ formatNum(row.currAc) }}</td>
                        <td>{{ formatNum(row.currFc) }}</td>
                        <td>{{ formatNum(row.prevAc) }}</td>
                        <td>{{ formatNum(row.prevFc) }}</td>
                        <td :class="row.acYoy !== null && row.acYoy < 100 ? 'text-danger' : 'text-success'">
                            <span v-if="row.acYoy !== null">{{ row.acYoy.toFixed(2) }}%</span>
                            <span v-else>-</span>
                        </td>
                        <td :class="row.fcYoy !== null && row.fcYoy < 100 ? 'text-danger' : 'text-success'">
                            <span v-if="row.fcYoy !== null">{{ row.fcYoy.toFixed(2) }}%</span>
                            <span v-else>-</span>
                        </td>
                    </tr>
                    <tr v-if="yoyTableData.length === 0">
                        <td colspan="7" class="empty-state">{{ t('brand.no_data') || 'No Data' }}</td>
                    </tr>
                </tbody>
                <tfoot v-if="yoyTableData.length > 0">
                    <tr class="total-row">
                        <td class="sticky-col loc-col" style="font-weight: 600;">Total (Jan~{{ monthsList[yoyTotalMonthRange - 1]?.label }})</td>
                        <td style="font-weight: 600;">{{ formatNum(yoyTotals.currAc) }}</td>
                        <td style="font-weight: 600;">{{ formatNum(yoyTotals.currFc) }}</td>
                        <td style="font-weight: 600;">{{ formatNum(yoyTotals.prevAc) }}</td>
                        <td style="font-weight: 600;">{{ formatNum(yoyTotals.prevFc) }}</td>
                        <td :class="yoyTotals.acYoy !== null && yoyTotals.acYoy < 100 ? 'text-danger' : 'text-success'" style="font-weight: 600;">
                            <span v-if="yoyTotals.acYoy !== null">{{ yoyTotals.acYoy.toFixed(2) }}%</span>
                            <span v-else>-</span>
                        </td>
                        <td :class="yoyTotals.fcYoy !== null && yoyTotals.fcYoy < 100 ? 'text-danger' : 'text-success'" style="font-weight: 600;">
                            <span v-if="yoyTotals.fcYoy !== null">{{ yoyTotals.fcYoy.toFixed(2) }}%</span>
                            <span v-else>-</span>
                        </td>
                    </tr>
                </tfoot>
            </table>
        </div>
    </div>

    <!-- YoY Analysis Location Details Section -->
    <div class="card yoy-section">
        <div class="section-toolbar no-header">
            <h3 style="margin: 0; font-size: 1rem; color: var(--text-main);">YoY Analysis - Location Details</h3>
            <div class="filter-group" style="margin-left: auto;">
                <div class="filter-item">
                    <label>Total Until:</label>
                    <select v-model="yoyTotalMonthRange" class="input-std">
                        <option v-for="(m, idx) in monthsList" :key="m.key" :value="idx + 1">{{ m.label }}</option>
                    </select>
                </div>
                <div class="filter-item">
                    <label>Base Year:</label>
                    <select v-model="yoyBaseYear" class="input-std">
                        <option v-for="y in uniqueYears" :key="y" :value="y">{{ y }}</option>
                    </select>
                </div>
                <div class="filter-item">
                    <label>Target Year:</label>
                    <select v-model="yoyTargetYear" class="input-std">
                        <option v-for="y in uniqueYears" :key="y" :value="y">{{ y }}</option>
                    </select>
                </div>
            </div>
        </div>

        <div class="table-scroll" style="max-height: unset; border-top: 1px solid var(--border-light);">
            <table>
                <thead>
                    <tr>
                        <th class="sticky-col loc-col">Location</th>
                        <th class="sticky-col item-col">Month</th>
                        <th>{{ yoyTargetYear }} AC</th>
                        <th>{{ yoyTargetYear }} FC</th>
                        <th>{{ yoyBaseYear }} AC</th>
                        <th>{{ yoyBaseYear }} FC</th>
                        <th>AC YoY (%)</th>
                        <th>FC YoY (%)</th>
                    </tr>
                </thead>
                <tbody>
                    <template v-for="locGroup in yoyLocationDetailsData" :key="locGroup.location">
                        <tr v-for="(row, rowIdx) in locGroup.rows" :key="row.location + row.month">
                            <td v-if="rowIdx === 0" :rowspan="locGroup.rows.length + 1" class="sticky-col loc-col">
                                {{ row.location }}
                            </td>
                            <td class="sticky-col item-col">{{ row.label }}</td>
                            <td>{{ formatNum(row.currAc) }}</td>
                            <td>{{ formatNum(row.currFc) }}</td>
                            <td>{{ formatNum(row.prevAc) }}</td>
                            <td>{{ formatNum(row.prevFc) }}</td>
                            <td :class="row.acYoy !== null && row.acYoy < 100 ? 'text-danger' : 'text-success'">
                                <span v-if="row.acYoy !== null">{{ row.acYoy.toFixed(2) }}%</span>
                                <span v-else>-</span>
                            </td>
                            <td :class="row.fcYoy !== null && row.fcYoy < 100 ? 'text-danger' : 'text-success'">
                                <span v-if="row.fcYoy !== null">{{ row.fcYoy.toFixed(2) }}%</span>
                                <span v-else>-</span>
                            </td>
                        </tr>
                        <!-- Total row for this location -->
                        <tr class="total-row">
                            <td class="sticky-col item-col" style="font-weight: 600;">Total</td>
                            <td style="font-weight: 600;">{{ formatNum(locGroup.total.currAc) }}</td>
                            <td style="font-weight: 600;">{{ formatNum(locGroup.total.currFc) }}</td>
                            <td style="font-weight: 600;">{{ formatNum(locGroup.total.prevAc) }}</td>
                            <td style="font-weight: 600;">{{ formatNum(locGroup.total.prevFc) }}</td>
                            <td :class="locGroup.total.acYoy !== null && locGroup.total.acYoy < 100 ? 'text-danger' : 'text-success'" style="font-weight: 600;">
                                <span v-if="locGroup.total.acYoy !== null">{{ locGroup.total.acYoy.toFixed(2) }}%</span>
                                <span v-else>-</span>
                            </td>
                            <td :class="locGroup.total.fcYoy !== null && locGroup.total.fcYoy < 100 ? 'text-danger' : 'text-success'" style="font-weight: 600;">
                                <span v-if="locGroup.total.fcYoy !== null">{{ locGroup.total.fcYoy.toFixed(2) }}%</span>
                                <span v-else>-</span>
                            </td>
                        </tr>
                    </template>
                    <tr v-if="yoyLocationDetailsData.length === 0">
                        <td colspan="8" class="empty-state">{{ t('brand.no_data') || 'No Data' }}</td>
                    </tr>
                </tbody>
            </table>
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
const { t } = useI18n();

// 从路由参数中提取当前品牌名称
const currentBrand = computed(() => (route.params.name as string) || '');

// 表格筛选状态
const timeFilter = ref('All');
const quarterFilter = ref('All');
const locationFilter = ref('All');

// Chart 1 Filter: Jan to [diffMonthRange]
const diffMonthRange = ref(12); // Default to December (Full Year)

/**
 * 获取当前品牌下的所有唯一大区列表，用于筛选下拉框
 */
const uniqueLocations = computed(() => {
    const locs = dataStore.records.map(r => r.location).filter(Boolean) as string[];
    return Array.from(new Set(locs));
});

// 月份常量定义
const monthsKeyList = ['jan','feb','mar','apr','may','jun','jul','aug','sep','oct','nov','dec'];
const monthsList = [
    { key: 'jan', label: 'Jan', q: 'Q1' }, { key: 'feb', label: 'Feb', q: 'Q1' }, { key: 'mar', label: 'Mar', q: 'Q1' },
    { key: 'apr', label: 'Apr', q: 'Q2' }, { key: 'may', label: 'May', q: 'Q2' }, { key: 'jun', label: 'Jun', q: 'Q2' },
    { key: 'jul', label: 'Jul', q: 'Q3' }, { key: 'aug', label: 'Aug', q: 'Q3' }, { key: 'sep', label: 'Sep', q: 'Q3' },
    { key: 'oct', label: 'Oct', q: 'Q4' }, { key: 'nov', label: 'Nov', q: 'Q4' }, { key: 'dec', label: 'Dec', q: 'Q4' }
];

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

/**
 * 获取用于展示的地区名称
 * 针对 "Landmark-Splash" 品牌，取前两个字符（国家代码）聚合
 */
const getDisplayLocation = (loc: string) => {
    if (!loc) return 'Unknown';
    if (currentBrand.value === 'Landmark-Splash') {
        const country = loc.substring(0, 2).toUpperCase();
        return ['CN', 'BD', 'IN', 'TK', 'VN', 'KH', 'ID'].includes(country) ? country : loc;
    }
    return loc;
};

/**
 * Chart 1: Location AC vs FC Grouped Bar Chart
 */
const locDiffChart = computed(() => {
    // Group Data by Display Location
    const aggregatedData = new Map<string, { ac: number, fc: number }>();
    
    filteredRecords.value.forEach((r: any) => {
        const displayLoc = getDisplayLocation(r.location);
        if (!aggregatedData.has(displayLoc)) {
            aggregatedData.set(displayLoc, { ac: 0, fc: 0 });
        }
        const entry = aggregatedData.get(displayLoc)!;
        
        for (let i = 0; i < diffMonthRange.value; i++) {
            const mKey = monthsKeyList[i];
            entry.ac += (r[`${mKey}_ac`] || 0);
            entry.fc += (r[`${mKey}_fc`] || 0);
        }
    });

    const displayLocs = Array.from(aggregatedData.keys());
    const acData = displayLocs.map(l => aggregatedData.get(l)!.ac);
    const fcData = displayLocs.map(l => aggregatedData.get(l)!.fc);

    return {
        xAxis: displayLocs,
        series: [
            { 
                name: 'AC (Jan~' + (monthsList[diffMonthRange.value - 1]?.label || '') + ')', 
                type: 'bar' as const, 
                data: acData,
                itemStyle: { color: '#3b82f6' } // Blue
            },
            { 
                name: 'FC (Jan~' + (monthsList[diffMonthRange.value - 1]?.label || '') + ')', 
                type: 'bar' as const, 
                data: fcData,
                itemStyle: { color: '#eab308' } // Yellow
            }
        ]
    };
});

/**
 * Chart 2: Monthly AC Trend by Location (Multi-Line)
 */
const trendChart = computed(() => {
    // X Axis: Jan - Dec
    const xAxis = monthsKeyList.map(m => m.charAt(0).toUpperCase() + m.slice(1));
    
    // Group by Display Location
    const aggregatedSeries = new Map<string, number[]>();
    
    filteredRecords.value.forEach((r: any) => {
        const displayLoc = getDisplayLocation(r.location);
        if (!aggregatedSeries.has(displayLoc)) {
            aggregatedSeries.set(displayLoc, new Array(12).fill(0));
        }
        const data = aggregatedSeries.get(displayLoc)!;
        
        monthsKeyList.forEach((mKey, idx) => {
            data[idx] += (r[`${mKey}_ac`] || 0);
        });
    });

    const displayLocs = Array.from(aggregatedSeries.keys());
    const colors = ['#3b82f6', '#ef4444', '#10b981', '#f59e0b', '#8b5cf6', '#ec4899', '#6366f1', '#14b8a6'];
    
    const series = displayLocs.map((loc, idx) => ({
        name: loc,
        type: 'line' as const,
        data: aggregatedSeries.get(loc)!,
        smooth: true,
        itemStyle: { color: colors[idx % colors.length] }
    }));

    return {
        xAxis: xAxis,
        series: series
    };
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

const tableTotals = computed(() => {
    const totals: any = {};
    const records = filteredRecords.value;
    
    variableMonths.value.forEach(col => {
        let acSum = 0;
        let fcSum = 0;
        records.forEach((r: any) => {
            acSum += (Number(r[`${col.key}_ac`]) || 0);
            fcSum += (Number(r[`${col.key}_fc`]) || 0);
        });
        totals[`${col.key}_ac`] = acSum;
        totals[`${col.key}_fc`] = fcSum;
        totals[`${col.key}_diff`] = fcSum === 0 ? 0 : Number(((acSum / fcSum) * 100).toFixed(2));
    });
    return totals;
});

// For YoY Table
const uniqueYears = computed(() => {
    const years = dataStore.records.map((r: any) => r.year).filter(Boolean);
    const yearSet = new Set(years);
    const sorted = Array.from(yearSet).sort((a: any, b: any) => b - a);
    return sorted.length ? sorted : [new Date().getFullYear(), new Date().getFullYear() - 1]; // Ensure at least one to fall back to
});

const yoyTargetYear = ref(new Date().getFullYear());
const yoyBaseYear = ref(new Date().getFullYear() - 1);
const yoyTotalMonthRange = ref(new Date().getMonth() + 1 || 12);

watch(uniqueYears, (newYears) => {
    if (newYears.length) {
        if (!newYears.includes(yoyTargetYear.value)) {
            yoyTargetYear.value = newYears[0] as number;
        }
        if (!newYears.includes(yoyBaseYear.value)) {
            yoyBaseYear.value = newYears.length > 1 ? newYears[1] as number : newYears[0] as number;
        }
    }
}, { immediate: true });

const yoyTableData = computed(() => {
    const allRecords = dataStore.records || [];
    const currYear = yoyTargetYear.value as number;
    const prevYear = yoyBaseYear.value as number;

    const currYearRecs = allRecords.filter((r: any) => r.year === currYear);
    const prevYearRecs = allRecords.filter((r: any) => r.year === prevYear);

    return monthsList.map(m => {
        let currAc = 0, currFc = 0;
        let prevAc = 0, prevFc = 0;

        currYearRecs.forEach((r: any) => {
            currAc += (Number(r[`${m.key}_ac`]) || 0);
            currFc += (Number(r[`${m.key}_fc`]) || 0);
        });

        prevYearRecs.forEach((r: any) => {
            prevAc += (Number(r[`${m.key}_ac`]) || 0);
            prevFc += (Number(r[`${m.key}_fc`]) || 0);
        });

        let acYoy = prevAc === 0 ? null : (currAc / prevAc) * 100;
        let fcYoy = prevFc === 0 ? null : (currFc / prevFc) * 100;

        return {
            month: m.key,
            label: m.label,
            currAc, currFc, prevAc, prevFc,
            acYoy: acYoy !== null ? Number(acYoy.toFixed(2)) : null,
            fcYoy: fcYoy !== null ? Number(fcYoy.toFixed(2)) : null
        };
    });
});

const yoyTotals = computed(() => {
    let currAc = 0, currFc = 0, prevAc = 0, prevFc = 0;

    const activeRows = yoyTableData.value.slice(0, yoyTotalMonthRange.value);
    activeRows.forEach(row => {
        currAc += row.currAc;
        currFc += row.currFc;
        prevAc += row.prevAc;
        prevFc += row.prevFc;
    });

    let acYoy = prevAc === 0 ? null : (currAc / prevAc) * 100;
    let fcYoy = prevFc === 0 ? null : (currFc / prevFc) * 100;

    return {
        currAc, currFc, prevAc, prevFc,
        acYoy: acYoy !== null ? Number(acYoy.toFixed(2)) : null,
        fcYoy: fcYoy !== null ? Number(fcYoy.toFixed(2)) : null
    };
});

// YoY Analysis - Location Details: breaks down each month by location
const yoyLocationDetailsData = computed(() => {
    const allRecords = dataStore.records || [];
    const currYear = yoyTargetYear.value as number;
    const prevYear = yoyBaseYear.value as number;

    const currYearRecs = allRecords.filter((r: any) => r.year === currYear);
    const prevYearRecs = allRecords.filter((r: any) => r.year === prevYear);

    // Collect all unique locations
    const allLocations = [...new Set(allRecords.map(r => r.location).filter(Boolean))] as string[];
    // Sort locations alphabetically
    allLocations.sort();

    return allLocations.map(location => {
        const rows = monthsList.slice(0, yoyTotalMonthRange.value).map(m => {
            let currAc = 0, currFc = 0;
            let prevAc = 0, prevFc = 0;

            currYearRecs.filter((r: any) => r.location === location).forEach((r: any) => {
                currAc += Number(r[`${m.key}_ac`]) || 0;
                currFc += Number(r[`${m.key}_fc`]) || 0;
            });

            prevYearRecs.filter((r: any) => r.location === location).forEach((r: any) => {
                prevAc += Number(r[`${m.key}_ac`]) || 0;
                prevFc += Number(r[`${m.key}_fc`]) || 0;
            });

            let acYoy = prevAc === 0 ? null : (currAc / prevAc) * 100;
            let fcYoy = prevFc === 0 ? null : (currFc / prevFc) * 100;

            return {
                location,
                month: m.key,
                label: m.label,
                currAc, currFc, prevAc, prevFc,
                acYoy: acYoy !== null ? Number(acYoy.toFixed(2)) : null,
                fcYoy: fcYoy !== null ? Number(fcYoy.toFixed(2)) : null
            };
        });

        // Calculate total for this location across filtered months
        let totalCurrAc = 0, totalCurrFc = 0, totalPrevAc = 0, totalPrevFc = 0;
        rows.forEach(row => {
            totalCurrAc += row.currAc;
            totalCurrFc += row.currFc;
            totalPrevAc += row.prevAc;
            totalPrevFc += row.prevFc;
        });
        const totalAcYoy = totalPrevAc === 0 ? null : (totalCurrAc / totalPrevAc) * 100;
        const totalFcYoy = totalPrevFc === 0 ? null : (totalCurrFc / totalPrevFc) * 100;

        return {
            location,
            rows,
            total: {
                currAc: Number(totalCurrAc.toFixed(2)),
                currFc: Number(totalCurrFc.toFixed(2)),
                prevAc: Number(totalPrevAc.toFixed(2)),
                prevFc: Number(totalPrevFc.toFixed(2)),
                acYoy: totalAcYoy !== null ? Number(totalAcYoy.toFixed(2)) : null,
                fcYoy: totalFcYoy !== null ? Number(totalFcYoy.toFixed(2)) : null
            }
        };
    });
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
.filter-group { display: flex; gap: 12px; align-items: center; }
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
.bg-q { background-color: #f0f9ff; font-weight: 500; }
.text-danger { color: #ef4444; }
.text-success { color: #10b981; }

.pagination {
    display: flex; align-items: center; justify-content: center; gap: 16px;
    padding: 16px; border-top: 1px solid var(--border-light);
    background: #f8fafc;
}
.page-info { font-size: 0.9rem; color: var(--text-secondary); }

.charts-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-top: 24px; }
.chart-card { padding: 16px; }
.chart-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
.chart-header h3 { font-size: 1rem; margin: 0; color: var(--text-main); }
.chart-header .filter-item { display: flex; align-items: center; gap: 8px; font-size: 0.9rem; }

@media (max-width: 1024px) {
    .charts-grid { grid-template-columns: 1fr; }
}

.empty-state { text-align: center; padding: 48px; color: var(--text-light); }
.btn-sm { padding: 4px 12px; font-size: 0.8rem; height: 32px; display: flex; align-items: center; gap: 6px; }
</style>
