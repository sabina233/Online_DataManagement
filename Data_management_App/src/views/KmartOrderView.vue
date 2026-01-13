<template>
  <div class="kmart-order-view">
    <div class="page-title">
        <div class="breadcrumb">
            <span class="root" @click="$router.push('/')">应用</span>
            <span class="separator">/</span>
            <span class="active">Kmart 各站点接单数据汇总</span>
        </div>
    </div>

    <!-- 过滤器 -->
    <div class="card toolbar-card">
        <div class="toolbar">
            <div class="filter-group">
                <label>年份:</label>
                <select v-model="selectedYear" class="input-std" @change="loadData">
                    <option v-for="y in availableYears" :key="y" :value="y">{{ y }}</option>
                </select>
                
                <label>月份:</label>
                <select v-model="selectedMonth" class="input-std" @change="loadData">
                    <option v-for="m in 12" :key="m" :value="m">{{ m }}月</option>
                </select>
                <span class="info">当前显示: {{ selectedMonth }} 月数据</span>
            </div>
            
            <div class="actions">
                <button class="btn btn-primary btn-sm" @click="$router.push('/orders/kmart/entry')">
                    前往录入页
                </button>
            </div>
        </div>
    </div>

    <!-- 模块 1: 当月接单汇总 -->
    <div class="summary-section">
        <div class="section-header">
            <h3>1. 当月各种类接单汇总 (Total Orders by Category - {{ selectedMonth }}月)</h3>
        </div>
        <div class="summary-grid">
            <div class="card table-card">
                <table>
                    <thead>
                        <tr>
                            <th>类别</th>
                            <th>当月数量 (Pcs)</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="cat in totalByCategory" :key="cat.name">
                            <td>{{ cat.name }}</td>
                            <td class="font-bold">{{ cat.total.toLocaleString() }}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div class="card chart-card">
                <ChartComponent :option="totalCategoryChartOption" />
            </div>
        </div>
    </div>

    <!-- 模块 2: 种类站点占比 (改为 站点接单总量占比) -->
    <div class="summary-section">
        <div class="section-header" style="display: flex; justify-content: space-between; align-items: center;">
            <h3>2. 站点接单量统计与占比 (Total Orders by Site)</h3>
            
            <!-- 模块 2 独立过滤器 -->
            <div class="module-filter">
                <select v-model="module2FilterType" class="input-std input-sm w-auto">
                    <option value="annual">年度</option>
                    <option value="half">半年度</option>
                    <option value="quarter">季度</option>
                </select>
                
                <select v-if="module2FilterType !== 'annual'" v-model="module2FilterValue" class="input-std input-sm">
                    <option v-for="opt in module2FilterOptions" :key="opt.value" :value="opt.value">
                        {{ opt.label }}
                    </option>
                </select>
            </div>
        </div>
        <div class="summary-grid">
            <div class="card table-card">
                <table class="nested-table">
                    <thead>
                        <tr>
                            <th>站点</th>
                            <th>总量 (Pcs)</th>
                            <th>占总比%</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="loc in siteWeightData" :key="loc.location">
                            <td>{{ loc.location }}</td>
                            <td>{{ loc.total.toLocaleString() }}</td>
                            <td>{{ loc.mainPercent }}%</td>
                        </tr>
                         <!-- Total Row -->
                        <tr style="background: #f8fafc; font-weight: bold;">
                            <td>TOTAL</td>
                            <td>{{ siteWeightData.reduce((s, i) => s + i.total, 0).toLocaleString() }}</td>
                            <td>100%</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div class="card chart-card">
                <ChartComponent :option="siteWeightChartOption" />
            </div>
        </div>
    </div>

    <!-- 模块 3: RFID 站点占比 -->
    <div class="summary-section">
        <div class="section-header">
            <h3>3. 各站点 RFID 占比 (RFID Ratio per Site)</h3>
        </div>
        <div class="summary-grid">
            <div class="card table-card">
                <table>
                    <thead>
                        <tr>
                            <th>地点</th>
                            <th>RFID 总量</th>
                            <th>RFID 比例</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="loc in rfidRatioData" :key="loc.location">
                            <td>{{ loc.location }}</td>
                            <td>{{ loc.rfidTotal.toLocaleString() }}</td>
                            <td class="font-bold text-blue">{{ loc.ratio }}%</td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div class="card chart-card">
                <ChartComponent :option="rfidRatioChartOption" />
            </div>
        </div>
    </div>

    <!-- 每日明细矩阵 (Detail Matrix) -->
    <div class="summary-section">
        <div class="section-header">
            <h3>每日明细矩阵 ({{ selectedMonth }}月)</h3>
        </div>
        <div class="card main-card">
            <div class="table-container">
                <table>
                    <thead>
                        <tr>
                            <th class="fixed-col col-loc">地点</th>
                            <th colspan="2" class="fixed-col col-cat-merged">类别</th>
                            <th v-for="d in daysInMonth" :key="d" class="day-col">
                               {{ d }}
                            </th>
                            <th class="total-col">合计</th>
                        </tr>
                    </thead>
                    <tbody>
                        <template v-for="locGroup in groupedData" :key="locGroup.location">
                            <template v-for="(catGroup, catIndex) in locGroup.categories" :key="locGroup.location + catGroup.category">
                                <tr v-for="(row, rowIndex) in catGroup.items" :key="row.rowKey">
                                    <td v-if="catIndex === 0 && rowIndex === 0" 
                                        :rowspan="locGroup.totalRows" 
                                        class="fixed-col col-loc">
                                        {{ locGroup.location }}
                                    </td>
                                    <td v-if="rowIndex === 0" 
                                        :rowspan="catGroup.items.length"
                                        :colspan="catGroup.category !== 'RFID' ? 2 : 1"
                                        :style="{ width: catGroup.category !== 'RFID' ? '250px' : '120px' }"
                                        class="fixed-col col-cat">
                                        {{ catGroup.category }}
                                    </td>
                                    <td v-if="catGroup.category === 'RFID'" class="fixed-col col-sub">{{ row.subCategory }}</td>
                                    <td v-for="d in daysInMonth" :key="d" class="day-cell">{{ row.dailyValues[d] || '-' }}</td>
                                    <td class="total-col font-bold">{{ calculateRowTotal(row) }}</td>
                                </tr>
                            </template>
                        </template>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import api from '../services/api';
import ChartComponent from '../components/ChartComponent.vue';
import { useKmartStore } from '../stores/kmart';
import { storeToRefs } from 'pinia';

const kmartStore = useKmartStore();
const { selectedYear, selectedMonth } = storeToRefs(kmartStore);

// Dynamic Year Range (matches Entry View)
const availableYears = computed(() => {
    const current = new Date().getFullYear();
    const start = current - 2;
    const end = current + 5;
    const years: number[] = [];
    for (let i = start; i <= end; i++) {
        years.push(i);
    }
    return years;
});

interface ApiRecord {
    location: string;
    category: string;
    subCategory: string;
    date: string;
    quantity: number;
}

const allData = ref<ApiRecord[]>([]);

// 应用与 EntryView 一致的业务 Schema
const schemaData = [
  { location: "China", groups: [{ name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM", "半成品"] }, { name: "吊牌", items: [""] }, { name: "贴纸", items: [""] }, { name: "卡类 (袜卡/腰封)", items: [""] }, { name: "洗标", items: [""] }] },
  { location: "Vietnam", groups: [{ name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] }, { name: "吊牌", items: [""] }, { name: "贴纸", items: [""] }, { name: "卡类 (袜卡/腰封)", items: [""] }, { name: "洗标", items: [""] }] },
  { location: "Cambodia", groups: [{ name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] }, { name: "吊牌", items: [""] }, { name: "贴纸", items: [""] }, { name: "卡类 (袜卡/腰封)", items: [""] }, { name: "洗标", items: [""] }] },
  { location: "Indonesia", groups: [{ name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] }, { name: "吊牌", items: [""] }, { name: "贴纸", items: [""] }, { name: "卡类 (袜卡/腰封)", items: [""] }, { name: "洗标", items: [""] }] },
  { location: "Bangladesh", groups: [{ name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] }, { name: "吊牌", items: [""] }, { name: "贴纸", items: [""] }, { name: "卡类 (袜卡/腰封)", items: [""] }, { name: "洗标", items: [""] }, { name: "织标", items: [""] }] },
  { location: "India", groups: [{ name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] }, { name: "吊牌", items: [""] }, { name: "贴纸", items: [""] }, { name: "卡类 (袜卡/腰封)", items: [""] }, { name: "洗标", items: [""] } ] }
];

/**
 * 业务逻辑匹配标准化：全量去空格、转大写、全角转半角。
 */
const normalizeMatch = (s: any): string => {
    if (s === undefined || s === null) return '';
    return s.toString()
        .toUpperCase()
        .replace(/\s+/g, '')
        .replace(/[\uff01-\uff5e]/g, (ch: string) => String.fromCharCode(ch.charCodeAt(0) - 0xfee0));
};

// --- 计算汇总数据 ---


// --- 模块 2 筛选状态 ---
const module2FilterType = ref<'annual' | 'half' | 'quarter'>('annual');
const module2FilterValue = ref<string>('all');

const module2FilterOptions = computed(() => {
    switch (module2FilterType.value) {
        case 'annual': return [{ label: '全年', value: 'all' }];
        case 'half': return [{ label: '上半年', value: 'H1' }, { label: '下半年', value: 'H2' }];
        case 'quarter': return [
            { label: '第一季度', value: 'Q1' },
            { label: '第二季度', value: 'Q2' },
            { label: '第三季度', value: 'Q3' },
            { label: '第四季度', value: 'Q4' }
        ];
        default: return [];
    }
});

// 重置筛选值
watch(module2FilterType, () => {
    module2FilterValue.value = module2FilterOptions.value[0]?.value || 'all';
});

// --- 数据处理 ---

// 1. 全局筛选：仅受年份控制（API已经筛选了年份），但在此需提供给 Module 2 全年数据
// loadData 现在会加载整年数据（endMonth: 12）

// 2. 当月数据：用于 模块 1, 3 (受全局月份选择影响，严格显示当月)
const currentMonthData = computed(() => {
    return allData.value.filter(d => {
        const dt = new Date(d.date);
        return dt.getMonth() + 1 === selectedMonth.value;
    });
});

// 3. 模块 2 数据：受 Module 2 自身过滤器影响 (保持不变)
const filteredModule2Data = computed(() => {
    return allData.value.filter(d => {
        const dt = new Date(d.date);
        const month = dt.getMonth() + 1;

        if (module2FilterType.value === 'annual') return true;
        if (module2FilterType.value === 'half') {
            return module2FilterValue.value === 'H1' ? month <= 6 : month > 6;
        }
        if (module2FilterType.value === 'quarter') {
            if (module2FilterValue.value === 'Q1') return month >= 1 && month <= 3;
            if (module2FilterValue.value === 'Q2') return month >= 4 && month <= 6;
            if (module2FilterValue.value === 'Q3') return month >= 7 && month <= 9;
            if (module2FilterValue.value === 'Q4') return month >= 10 && month <= 12;
        }
        return true;
    });
});

// --- 计算汇总数据 ---

const totalByCategory = computed(() => {
    const map = new Map<string, number>();
    // 使用 currentMonthData
    currentMonthData.value.forEach(d => {
        const isRFID = normalizeMatch(d.category) === "RFID";
        const catName = isRFID ? "RFID" : d.category;
        map.set(catName, (map.get(catName) || 0) + d.quantity);
    });
    return Array.from(map.entries()).map(([name, total]) => ({ name, total }));
});

// OLD Module 2 Logic (replaced) - keeping name for ref but rewriting content
// New Module 2: Site Proportion
const siteWeightData = computed(() => {
    // 使用 filteredModule2Data
    const totalAll = filteredModule2Data.value.reduce((s, r) => s + r.quantity, 0);
    
    return schemaData.map(loc => {
        const locData = filteredModule2Data.value.filter(d => normalizeMatch(d.location) === normalizeMatch(loc.location));
        const total = locData.reduce((s, r) => s + r.quantity, 0);
        
        return {
            location: loc.location,
            total,
            mainPercent: totalAll > 0 ? ((total / totalAll) * 100).toFixed(2) : "0" // Changed for display
        };
    });
});

const rfidRatioData = computed(() => {
    // 使用 currentMonthData
    // Need to re-calculate based on currentMonthData for Module 3
    return schemaData.map(loc => {
        const locData = currentMonthData.value.filter(d => normalizeMatch(d.location) === normalizeMatch(loc.location));
        const total = locData.reduce((s, r) => s + r.quantity, 0);
        const rfidTotal = locData.filter(d => normalizeMatch(d.category) === 'RFID').reduce((s, r) => s + r.quantity, 0);
        return {
            location: loc.location,
            rfidTotal,
            ratio: total > 0 ? Math.round((rfidTotal / total) * 100) : 0
        };
    });
});

// --- 图表配置 ---

const totalCategoryChartOption = computed(() => ({
    title: { text: '各种类接单分布', left: 'center' },
    tooltip: { trigger: 'item' as const },
    series: [{
        type: 'pie' as const,
        radius: '50%',
        data: totalByCategory.value.map(c => ({ value: c.total, name: c.name })),
        emphasis: {
            itemStyle: { shadowBlur: 10, shadowOffsetX: 0, shadowColor: 'rgba(0, 0, 0, 0.5)' }
        }
    }]
}));

// Module 2 Chart
const siteWeightChartOption = computed(() => ({
    title: { text: '各站点接单量统计', left: 'center' },
    tooltip: { trigger: 'axis' as const, axisPointer: { type: 'shadow' as const } },
    xAxis: { type: 'category' as const, data: siteWeightData.value.map(l => l.location) },
    yAxis: { type: 'value' as const },
    series: [
        { 
            name: '总量', 
            type: 'bar' as const, 
            data: siteWeightData.value.map(s => s.total),
            label: { show: true, position: 'top' as const } 
        }
    ]
}));

const rfidRatioChartOption = computed(() => ({
    title: { text: '各站点 RFID 占比', left: 'center' },
    xAxis: { type: 'category' as const, data: rfidRatioData.value.map(r => r.location) },
    yAxis: { type: 'value' as const },
    series: [{
        data: rfidRatioData.value.map(r => r.ratio),
        type: 'bar' as const,
        label: { show: true, position: 'top' as const, formatter: '{c}%' }
    }]
}));

// --- 详情表格逻辑 ---

const daysInMonth = computed(() => new Date(selectedYear.value, selectedMonth.value, 0).getDate());

const calculateRowTotal = (row: any) => {
    // 使用 currentMonthData
    const locRows = currentMonthData.value.filter(d => 
        normalizeMatch(d.location) === normalizeMatch(row.location) && 
        normalizeMatch(d.category) === normalizeMatch(row.category) && 
        normalizeMatch(d.subCategory) === normalizeMatch(row.subCategory)
    );
    return locRows.reduce((s, r) => s + r.quantity, 0);
};

const groupedData = computed(() => {
    return schemaData.map(loc => {
        const categories = loc.groups.map(g => ({
            category: g.name,
            items: g.items.map(item => {
                const dailyValues: Record<number, number> = {};
                // 使用 currentMonthData
                currentMonthData.value.filter(d => 
                    normalizeMatch(d.location) === normalizeMatch(loc.location) && 
                    normalizeMatch(d.category) === normalizeMatch(g.name) && 
                    normalizeMatch(d.subCategory) === normalizeMatch(item)
                ).forEach(d => {
                    const dt = new Date(d.date);
                    // 确保是当前选中的年月 (API返回全年数据，这里currentMonthData 已经是 <= selectedMonth)
                    // 但是矩阵表只显示单月明细，所以实际上应该只过滤出 == selectedMonth的数据
                    if (dt.getFullYear() === selectedYear.value && dt.getMonth() + 1 === selectedMonth.value) {
                       dailyValues[dt.getDate()] = d.quantity;
                    }
                });
                return {
                    rowKey: `${loc.location}_${g.name}_${item}`,
                    location: loc.location,
                    category: g.name,
                    subCategory: item,
                    dailyValues
                };
            })
        }));
        return {
            location: loc.location,
            categories,
            totalRows: categories.reduce((s, c) => s + c.items.length, 0)
        };
    });
});

const loadData = async () => {
    try {
        // ALWAYS fetch full year to support Module 2 filters
        const res = await api.get<ApiRecord[]>('/Order/kmart', {
            params: { year: selectedYear.value, endMonth: 12 }
        });
        allData.value = res.data;
    } catch (e) {
        console.error("加载报表数据失败", e);
    }
};

watch([selectedYear, selectedMonth], () => {
    loadData();
});

onMounted(() => {
    loadData();
});
</script>

<style scoped>
.kmart-order-view { display: flex; flex-direction: column; height: 100%; gap: 24px; padding: 16px; background: #f8fafc; overflow-y: auto; }
.card { background: white; border-radius: 8px; border: 1px solid #e2e8f0; overflow: hidden; }
.toolbar-card { flex: 0 0 auto; }
.toolbar { padding: 12px 16px; display: flex; justify-content: space-between; align-items: center; }
.filter-group { display: flex; gap: 12px; align-items: center; }
.module-filter { display: flex; gap: 8px; }
.input-std { padding: 6px 12px; border: 1px solid #e2e8f0; border-radius: 4px; }
.input-sm { padding: 4px 8px; font-size: 0.85rem; }
/* 自适应下拉列表宽度（随内容/父容器自适应） */
.w-auto { width: auto; }
.info { color: #64748b; font-size: 0.85rem; }

.summary-section { display: flex; flex-direction: column; gap: 12px; }
.section-header h3 { font-size: 1rem; color: #1e293b; font-weight: 600; border-left: 4px solid #3b82f6; padding-left: 12px; }

.summary-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; min-height: 380px; }
.table-card { padding: 0; }
.chart-card { padding: 16px; display: flex; align-items: center; justify-content: center; }

table { width: 100%; border-collapse: collapse; font-size: 0.85rem; }
th, td { border: 1px solid #e2e8f0; padding: 10px; text-align: center; }
th { background: #f1f5f9; color: #475569; font-weight: 600; }

.table-container { overflow: auto; max-height: 600px; }
.fixed-col { position: sticky; z-index: 10; background: white; white-space: nowrap; }
.col-loc { left: 0; width: 80px; }
.col-cat-merged { left: 80px; width: 250px; }
.col-cat { left: 80px; width: 120px; }
.col-sub { left: 120px; width: 130px; }

.day-cell { min-width: 40px; }
.total-col { background: #f1f5f9; min-width: 80px; }

.font-bold { font-weight: bold; }
.text-blue { color: #2563eb; }
.text-center { text-align: center; }
</style>
