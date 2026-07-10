<template>
  <div class="global-rfid-view">
    <div class="page-title">
      <div class="breadcrumb">
        <span class="root" @click="$router.push('/')">应用</span>
        <span class="separator">/</span>
        <span class="active">Global RFID 数据汇总</span>
      </div>
    </div>

    <!-- Year Filter -->
    <div class="card toolbar-card">
      <div class="toolbar">
        <div class="filter-group">
          <label>基准年份 (Base):</label>
          <select v-model="year1" class="input-std">
            <option v-for="y in availableYears" :key="y" :value="y">{{ y }}</option>
          </select>

          <label>对比年份 (Target):</label>
          <select v-model="year2" class="input-std">
            <option v-for="y in availableYears" :key="y" :value="y">{{ y }}</option>
          </select>

          <label>截止月份:</label>
          <select v-model="selectedMonth" class="input-std">
            <option v-for="(m, idx) in monthsList" :key="m.key" :value="idx + 1">{{ m.label }}</option>
          </select>

          <button class="btn btn-primary btn-sm" @click="loadData" :disabled="loading">
            {{ loading ? '加载中...' : '查询' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Table + Chart -->
    <div class="card main-card" v-if="tableData.length > 0">
      <div class="section-header">
        <h3>Global RFID — 品牌数据对比 ({{ year1 }} vs {{ year2 }}, Jan~{{ monthsList[selectedMonth - 1]?.label }})</h3>
      </div>

      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th>BRAND</th>
              <th>{{ year1 }} (Jan~{{ monthsList[selectedMonth - 1]?.label }})</th>
              <th>{{ year2 }} (Jan~{{ monthsList[selectedMonth - 1]?.label }})</th>
              <th>同比 (%)</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in tableData" :key="row.brand">
              <td class="font-bold">{{ row.brand }}</td>
              <td>{{ formatNum(row.year1Total) }}</td>
              <td>{{ formatNum(row.year2Total) }}</td>
              <td :class="row.yoy !== null && row.yoy < 100 ? 'text-danger' : 'text-success'">
                <span v-if="row.yoy !== null">{{ row.yoy.toFixed(2) }}%</span>
                <span v-else>-</span>
              </td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="total-row">
              <td style="font-weight: 600;">TOTAL</td>
              <td style="font-weight: 600;">{{ formatNum(tableTotals.year1Total) }}</td>
              <td style="font-weight: 600;">{{ formatNum(tableTotals.year2Total) }}</td>
              <td :class="tableTotals.yoy !== null && tableTotals.yoy < 100 ? 'text-danger' : 'text-success'" style="font-weight: 600;">
                <span v-if="tableTotals.yoy !== null">{{ tableTotals.yoy.toFixed(2) }}%</span>
                <span v-else>-</span>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>

      <!-- Bar Chart -->
      <div class="chart-container">
        <ChartComponent :option="chartOption" />
      </div>
    </div>

    <!-- Empty state -->
    <div class="card main-card" v-else-if="!loading">
      <div class="empty-state">请选择年份并点击「查询」加载数据</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import api from '../services/api';
import ChartComponent from '../components/ChartComponent.vue';

// Data
const loading = ref(false);
const brandDataCache = ref<Map<string, any[]>>(new Map());
const allBrands = ref<string[]>([]);
const year1 = ref(new Date().getFullYear() - 1);
const year2 = ref(new Date().getFullYear());
const selectedMonth = ref(new Date().getMonth() + 1);

const monthsKeyList = ['jan','feb','mar','apr','may','jun','jul','aug','sep','oct','nov','dec'];
const monthsList = [
  { key: 'jan', label: 'Jan' }, { key: 'feb', label: 'Feb' }, { key: 'mar', label: 'Mar' },
  { key: 'apr', label: 'Apr' }, { key: 'may', label: 'May' }, { key: 'jun', label: 'Jun' },
  { key: 'jul', label: 'Jul' }, { key: 'aug', label: 'Aug' }, { key: 'sep', label: 'Sep' },
  { key: 'oct', label: 'Oct' }, { key: 'nov', label: 'Nov' }, { key: 'dec', label: 'Dec' }
];

const availableYears = computed(() => {
  const current = new Date().getFullYear();
  const years: number[] = [];
  for (let i = current - 5; i <= current + 2; i++) {
    years.push(i);
  }
  return years;
});

// Fetch brands list
const fetchBrands = async () => {
  try {
    const res = await api.get<string[]>('/Data/brands');
    allBrands.value = res.data;
  } catch (e) {
    console.error('获取品牌列表失败', e);
  }
};

// Calculate yearly total for all records of a brand (Jan ~ selectedMonth)
const calcYearTotal = (records: any[], year: number): number => {
  const months = monthsKeyList.slice(0, selectedMonth.value);
  let total = 0;
  records.filter((r: any) => r.year === year).forEach((r: any) => {
    months.forEach(m => {
      total += Number(r[`${m}_ac`]) || 0;
    });
  });
  return total;
};

// Load all brands data
const loadData = async () => {
  loading.value = true;
  try {
    // Fetch brands if not yet loaded
    if (allBrands.value.length === 0) {
      await fetchBrands();
    }

    // Load each brand's data (use cache if available)
    for (const brand of allBrands.value) {
      if (!brandDataCache.value.has(brand)) {
        const res = await api.get<any[]>('/Data', { params: { brand } });
        brandDataCache.value.set(brand, res.data);
      }
    }
  } catch (e) {
    console.error('加载数据失败', e);
  } finally {
    loading.value = false;
  }
};

const tableData = computed(() => {
  return allBrands.value.map(brand => {
    const records = brandDataCache.value.get(brand) || [];
    const year1Total = calcYearTotal(records, year1.value);
    const year2Total = calcYearTotal(records, year2.value);
    const yoy = year1Total === 0 ? null : (year2Total / year1Total) * 100;
    return { brand, year1Total, year2Total, yoy };
  }).filter(row => row.year1Total > 0 || row.year2Total > 0);
});

const tableTotals = computed(() => {
  let year1Total = 0, year2Total = 0;
  tableData.value.forEach(row => {
    year1Total += row.year1Total;
    year2Total += row.year2Total;
  });
  const yoy = year1Total === 0 ? null : (year2Total / year1Total) * 100;
  return { year1Total, year2Total, yoy };
});

const chartOption = computed(() => ({
  title: {
    text: `${year1.value} vs ${year2.value} 品牌数据对比 (Jan~${monthsList[selectedMonth.value - 1]?.label})`,
    left: 'center'
  },
  tooltip: {
    trigger: 'axis' as const,
    axisPointer: { type: 'shadow' as const }
  },
  legend: {
    data: [`${year1.value} 总计`, `${year2.value} 总计`],
    bottom: 0
  },
  xAxis: {
    type: 'category' as const,
    data: tableData.value.map(r => r.brand)
  },
  yAxis: {
    type: 'value' as const
  },
  series: [
    {
      name: `${year1.value} 总计`,
      type: 'bar' as const,
      data: tableData.value.map(r => r.year1Total),
      label: { show: false }
    },
    {
      name: `${year2.value} 总计`,
      type: 'bar' as const,
      data: tableData.value.map(r => r.year2Total),
      label: { show: false }
    }
  ]
}));

const formatNum = (n: number) => n?.toLocaleString() || '0';

// Auto-load on mount
fetchBrands();
</script>

<style scoped>
.global-rfid-view { display: flex; flex-direction: column; gap: 24px; padding: 16px; background: #f8fafc; }
.page-title { margin-bottom: 8px; }
.breadcrumb { font-size: 0.9rem; color: #64748b; display: flex; gap: 8px; align-items: center; }
.breadcrumb .root { cursor: pointer; }
.breadcrumb .root:hover { color: #2563eb; }
.breadcrumb .active { color: #1e293b; font-weight: 500; }

.card { background: white; border-radius: 8px; border: 1px solid #e2e8f0; overflow: hidden; }
.toolbar-card { flex: 0 0 auto; }
.toolbar { padding: 12px 16px; display: flex; justify-content: space-between; align-items: center; }
.filter-group { display: flex; gap: 12px; align-items: center; flex-wrap: wrap; }
.input-std { padding: 6px 12px; border: 1px solid #e2e8f0; border-radius: 4px; }
.main-card { padding: 0; }
.section-header { padding: 16px; border-bottom: 1px solid #e2e8f0; }
.section-header h3 { font-size: 1rem; color: #1e293b; font-weight: 600; margin: 0; }

.table-scroll { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
th, td { border: 1px solid #e2e8f0; padding: 10px 16px; text-align: center; }
th { background: #f1f5f9; font-weight: 600; color: #475569; position: sticky; top: 0; }
tfoot td { background: #f8fafc; }

.chart-container { padding: 24px; min-height: 400px; }
.font-bold { font-weight: 600; }
.text-danger { color: #ef4444; font-weight: 600; }
.text-success { color: #10b981; font-weight: 600; }
.empty-state { text-align: center; padding: 48px; color: #94a3b8; }
.total-row td { background: #f8fafc; border-top: 2px solid #cbd5e1; }
</style>