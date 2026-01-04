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
                <span class="info">统计截至 {{ selectedMonth }} 月底</span>
            </div>
            
            <div class="actions">
                <button class="btn btn-primary btn-sm" @click="$router.push('/orders/kmart/entry')">
                    前往录入页
                </button>
            </div>
        </div>
    </div>

    <!-- 模块 1: 总接单汇总 -->
    <div class="summary-section">
        <div class="section-header">
            <h3>1. 总的各种类接单汇总 (Total Orders by Category)</h3>
        </div>
        <div class="summary-grid">
            <div class="card table-card">
                <table>
                    <thead>
                        <tr>
                            <th>类别</th>
                            <th>总数量 (Pcs)</th>
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

    <!-- 模块 2: 种类站点占比 -->
    <div class="summary-section">
        <div class="section-header">
            <h3>2. 各种类占对应站点的比重 (Category Weight by Site)</h3>
        </div>
        <div class="summary-grid">
            <div class="card table-card">
                <table class="nested-table">
                    <thead>
                        <tr>
                            <th>地点</th>
                            <th>RFID</th>
                            <th>其他</th>
                            <th>合计</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="loc in siteWeightData" :key="loc.location">
                            <td>{{ loc.location }}</td>
                            <td>{{ loc.rfidPercent }}%</td>
                            <td>{{ loc.otherPercent }}%</td>
                            <td class="font-bold">{{ loc.total.toLocaleString() }}</td>
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

const totalByCategory = computed(() => {
    const map = new Map<string, number>();
    allData.value.forEach(d => {
        // 汇总时进行类别名称归一化（防止导入时的微差）
        // 如果是 RFID 相关则统一归为 RFID
        const isRFID = normalizeMatch(d.category) === "RFID";
        const catName = isRFID ? "RFID" : d.category;
        map.set(catName, (map.get(catName) || 0) + d.quantity);
    });
    return Array.from(map.entries()).map(([name, total]) => ({ name, total }));
});

const siteWeightData = computed(() => {
    return schemaData.map(loc => {
        const locData = allData.value.filter(d => normalizeMatch(d.location) === normalizeMatch(loc.location));
        const total = locData.reduce((s, r) => s + r.quantity, 0);
        const rfidTotal = locData.filter(d => normalizeMatch(d.category) === 'RFID').reduce((s, r) => s + r.quantity, 0);
        const otherTotal = total - rfidTotal;
        return {
            location: loc.location,
            total,
            rfidPercent: total > 0 ? Math.round((rfidTotal / total) * 100) : 0,
            otherPercent: total > 0 ? Math.round((otherTotal / total) * 100) : 0
        };
    });
});

const rfidRatioData = computed(() => {
    return siteWeightData.value.map(s => ({
        location: s.location,
        rfidTotal: Math.round(s.total * s.rfidPercent / 100),
        ratio: s.rfidPercent
    }));
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

const siteWeightChartOption = computed(() => ({
    title: { text: '各站点 RFID vs 其他占比', left: 'center' },
    tooltip: { trigger: 'axis' as const, axisPointer: { type: 'shadow' as const } },
    legend: { data: ['RFID', '其他'], bottom: 0 },
    xAxis: { type: 'category' as const, data: schemaData.map(l => l.location) },
    yAxis: { type: 'value' as const, max: 100 },
    series: [
        { name: 'RFID', type: 'bar' as const, stack: 'total', data: siteWeightData.value.map(s => s.rfidPercent) },
        { name: '其他', type: 'bar' as const, stack: 'total', data: siteWeightData.value.map(s => s.otherPercent) }
    ]
}));

const rfidRatioChartOption = computed(() => ({
    title: { text: '各站点 RFID 占比趋势', left: 'center' },
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
    const locRows = allData.value.filter(d => 
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
                // 根据标准化后的三元组过滤数据
                allData.value.filter(d => 
                    normalizeMatch(d.location) === normalizeMatch(loc.location) && 
                    normalizeMatch(d.category) === normalizeMatch(g.name) && 
                    normalizeMatch(d.subCategory) === normalizeMatch(item)
                ).forEach(d => {
                    const dt = new Date(d.date);
                    // 确保是当前选中的年月
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
        const res = await api.get<ApiRecord[]>('/Order/kmart', {
            params: { year: selectedYear.value, endMonth: selectedMonth.value }
        });
        allData.value = res.data;
    } catch (e) {
        console.error("加载报表数据失败", e);
    }
};

// import { watch } from 'vue'; // Removed duplicate
// Watch is already set up in the top script block is ideal, but let's see. 
// The previous code had `watch` at the bottom.
// I will just remove the import line if it exists separately.
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
.input-std { padding: 6px 12px; border: 1px solid #e2e8f0; border-radius: 4px; }
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
