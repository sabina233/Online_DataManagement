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

          <label>站点筛选 (Location):</label>
          <select v-model="selectedLocation" class="input-std">
            <option value="">全部站点 (All)</option>
            <option v-for="loc in allLocations" :key="loc" :value="loc">{{ loc }}</option>
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
        <div class="header-left">
          <h3>
            Global RFID — 品牌数据对比 ({{ year1 }} vs {{ year2 }}, Jan~{{ monthsList[selectedMonth - 1]?.label }})
            <span v-if="selectedLocation" class="location-badge">{{ selectedLocation }}</span>
          </h3>
        </div>
        <div class="header-right">
          <label class="filter-label">站点筛选:</label>
          <select v-model="selectedLocation" class="input-std site-select">
            <option value="">全部站点 (All)</option>
            <option v-for="loc in allLocations" :key="loc" :value="loc">{{ loc }}</option>
          </select>
        </div>
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
                <span v-if="row.yoy !== null">{{ Math.round(row.yoy) }}%</span>
                <span v-else>-</span>
              </td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="total-row">
              <td class="total-cell font-bold"><span class="total-badge">TOTAL</span></td>
              <td class="total-cell font-bold">{{ formatNum(tableTotals.year1Total) }}</td>
              <td class="total-cell font-bold">{{ formatNum(tableTotals.year2Total) }}</td>
              <td class="total-cell font-bold" :class="tableTotals.yoy !== null && tableTotals.yoy < 100 ? 'text-danger' : 'text-success'">
                <span v-if="tableTotals.yoy !== null">{{ Math.round(tableTotals.yoy) }}%</span>
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

    <!-- Location Summary Table -->
    <div class="card main-card" v-if="locationTableData.length > 0">
      <div class="section-header">
        <h3>站点数据汇总 — Location Totals ({{ year1 }} vs {{ year2 }}, Jan~{{ monthsList[selectedMonth - 1]?.label }})</h3>
      </div>

      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th>LOCATION</th>
              <th>{{ year1 }} (Jan~{{ monthsList[selectedMonth - 1]?.label }})</th>
              <th>{{ year2 }} (Jan~{{ monthsList[selectedMonth - 1]?.label }})</th>
              <th>同比 (%)</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in locationTableData" :key="row.location">
              <td class="font-bold">{{ row.location }}</td>
              <td>{{ formatNum(row.year1Total) }}</td>
              <td>{{ formatNum(row.year2Total) }}</td>
              <td :class="row.yoy !== null && row.yoy < 100 ? 'text-danger' : 'text-success'">
                <span v-if="row.yoy !== null">{{ Math.round(row.yoy) }}%</span>
                <span v-else>-</span>
              </td>
            </tr>
          </tbody>
          <tfoot>
            <tr class="total-row">
              <td class="total-cell font-bold"><span class="total-badge">TOTAL</span></td>
              <td class="total-cell font-bold">{{ formatNum(locationTableTotals.year1Total) }}</td>
              <td class="total-cell font-bold">{{ formatNum(locationTableTotals.year2Total) }}</td>
              <td class="total-cell font-bold" :class="locationTableTotals.yoy !== null && locationTableTotals.yoy < 100 ? 'text-danger' : 'text-success'">
                <span v-if="locationTableTotals.yoy !== null">{{ Math.round(locationTableTotals.yoy) }}%</span>
                <span v-else>-</span>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>

      <!-- Location Bar Chart -->
      <div class="chart-container">
        <ChartComponent :option="locationChartOption" />
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
const selectedLocation = ref('');

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

// Normalize location for display/merge (all brands consistent): NFKC full-width->half-width,
// trim, uppercase; Landmark store codes collapse to country code
const getDisplayLocation = (loc: string, brand: string) => {
    if (!loc) return 'Unknown';
    const clean = loc.toString()
        .normalize('NFKC')
        .trim()
        .toUpperCase();
    if (!clean) return 'Unknown';
    if (brand === 'Landmark-Splash') {
        const country = clean.substring(0, 2);
        return ['CN', 'BD', 'IN', 'TK', 'VN', 'KH', 'ID'].includes(country) ? country : clean;
    }
    return clean;
};

// All available unique locations computed from cached data
const allLocations = computed(() => {
  const locSet = new Set<string>();
  for (const brand of allBrands.value) {
    const records = brandDataCache.value.get(brand) || [];
    records.forEach((r: any) => {
      const loc = getDisplayLocation(r.location, brand);
      if (loc && loc !== 'Unknown') locSet.add(loc);
    });
  }
  return Array.from(locSet).sort();
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

// Calculate yearly total for all records of a brand (Jan ~ selectedMonth), optionally filtered by location
const calcYearTotal = (records: any[], year: number, brand: string, location?: string): number => {
  const months = monthsKeyList.slice(0, selectedMonth.value);
  let total = 0;
  records
    .filter((r: any) => {
      if (r.year !== year) return false;
      if (location) {
        return getDisplayLocation(r.location, brand) === location;
      }
      return true;
    })
    .forEach((r: any) => {
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
    const year1Total = calcYearTotal(records, year1.value, brand, selectedLocation.value);
    const year2Total = calcYearTotal(records, year2.value, brand, selectedLocation.value);
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

const chartOption = computed(() => {
  const locSuffix = selectedLocation.value ? ` [${selectedLocation.value}]` : '';
  return {
    title: {
      text: `${year1.value} vs ${year2.value} 品牌数据对比 (Jan~${monthsList[selectedMonth.value - 1]?.label})${locSuffix}`,
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
  };
});

const formatNum = (n: number) => n?.toLocaleString() || '0';

// Location summary: aggregate all records by location across all brands (merge same location, different items)
const locationTableData = computed(() => {
  const locMap = new Map<string, { year1Total: number, year2Total: number }>();

  for (const brand of allBrands.value) {
    const records = brandDataCache.value.get(brand) || [];
    const months = monthsKeyList.slice(0, selectedMonth.value);

    records.forEach((r: any) => {
      const loc = getDisplayLocation(r.location, brand);
      if (!locMap.has(loc)) {
        locMap.set(loc, { year1Total: 0, year2Total: 0 });
      }
      const entry = locMap.get(loc)!;
      if (r.year === year1.value) {
        months.forEach(m => { entry.year1Total += Number(r[`${m}_ac`]) || 0; });
      }
      if (r.year === year2.value) {
        months.forEach(m => { entry.year2Total += Number(r[`${m}_ac`]) || 0; });
      }
    });
  }

  return Array.from(locMap.entries())
    .map(([location, vals]) => ({
      location,
      year1Total: vals.year1Total,
      year2Total: vals.year2Total,
      yoy: vals.year1Total === 0 ? null : (vals.year2Total / vals.year1Total) * 100
    }))
    .filter(row => row.year1Total > 0 || row.year2Total > 0)
    .sort((a, b) => b.year2Total - a.year2Total);
});

const locationTableTotals = computed(() => {
  let year1Total = 0, year2Total = 0;
  locationTableData.value.forEach(row => {
    year1Total += row.year1Total;
    year2Total += row.year2Total;
  });
  const yoy = year1Total === 0 ? null : (year2Total / year1Total) * 100;
  return { year1Total, year2Total, yoy };
});

const locationChartOption = computed(() => ({
  title: {
    text: `${year1.value} vs ${year2.value} 站点数据对比 (Jan~${monthsList[selectedMonth.value - 1]?.label})`,
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
    data: locationTableData.value.map(r => r.location)
  },
  yAxis: {
    type: 'value' as const
  },
  series: [
    {
      name: `${year1.value} 总计`,
      type: 'bar' as const,
      data: locationTableData.value.map(r => r.year1Total),
      label: { show: false }
    },
    {
      name: `${year2.value} 总计`,
      type: 'bar' as const,
      data: locationTableData.value.map(r => r.year2Total),
      label: { show: false }
    }
  ]
}));

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
.input-std { padding: 6px 12px; border: 1px solid #cbd5e1; border-radius: 4px; font-size: 0.9rem; background: white; }
.main-card { padding: 0; }
.section-header {
  padding: 14px 16px;
  border-bottom: 1px solid #e2e8f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}
.header-left h3 { font-size: 1rem; color: #1e293b; font-weight: 600; margin: 0; display: flex; align-items: center; gap: 8px; }
.header-right { display: flex; align-items: center; gap: 8px; }
.filter-label { font-size: 0.88rem; font-weight: 600; color: #475569; }
.site-select { min-width: 140px; }
.location-badge {
  display: inline-block;
  padding: 2px 8px;
  background: #dbeafe;
  color: #1d4ed8;
  font-size: 0.82rem;
  border-radius: 4px;
  font-weight: 600;
}

.table-scroll { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; font-size: 0.9rem; }
th, td { border: 1px solid #e2e8f0; padding: 10px 16px; text-align: center; }
th { background: #f1f5f9; font-weight: 600; color: #475569; position: sticky; top: 0; }

/* TOTAL row styling: Yellow color block */
.total-row td,
.total-cell {
  background-color: #fef08a !important; /* Classic Excel/PPT highlight yellow block */
  border-top: 2px solid #eab308 !important;
  border-bottom: 2px solid #eab308 !important;
  font-weight: 700 !important;
  color: #854d0e !important;
}

.total-badge {
  display: inline-block;
  background-color: #fde047;
  color: #713f12;
  font-weight: 800;
  padding: 2px 10px;
  border-radius: 4px;
  border: 1px solid #eab308;
  letter-spacing: 0.5px;
}

.chart-container { padding: 24px; min-height: 400px; }
.font-bold { font-weight: 600; }
.text-danger { color: #ef4444 !important; font-weight: 600; }
.text-success { color: #10b981 !important; font-weight: 600; }
.empty-state { text-align: center; padding: 48px; color: #94a3b8; }
</style>
