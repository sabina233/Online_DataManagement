<template>
  <div class="kmart-entry-view">
    <div class="page-title">
        <div class="breadcrumb">
            <span class="root" @click="$router.push('/')">应用</span>
            <span class="separator">/</span>
            <span class="root">Kmart 订单数据</span>
            <span class="separator">/</span>
            <span class="active">数据录入</span>
        </div>
    </div>

    <div class="card main-card">
        <!-- Toolbar -->
        <div class="toolbar-card card">
            <div v-if="isLoading" class="loading-overlay">加载中...</div>
            <div class="toolbar">
                <div class="filter-group">
                    <label>年份:</label>
                    <select v-model="selectedYear" class="input-std">
                        <option v-for="y in availableYears" :key="y" :value="y">{{ y }}</option>
                    </select>
                    
                    <label>月份:</label>
                    <select v-model="selectedMonth" class="input-std" @change="loadData">
                        <option v-for="m in 12" :key="m" :value="m">{{ m }}月</option>
                    </select>
                </div>
                
                <div class="actions">
                    <button class="btn btn-danger btn-sm" @click="deleteData">
                        删除当月数据
                    </button>
                    <!-- Excel 上传 -->
                    <div class="upload-btn-wrapper">
                        <button class="btn btn-secondary btn-sm">导入 Excel</button>
                        <input type="file" @change="handleFileUpload" accept=".xlsx, .xls" />
                    </div>

                    <button class="btn btn-primary btn-sm" @click="saveData" :disabled="!isDirty">
                        保存到数据库
                    </button>
                </div>
            </div>
        </div>

        <!-- Matrix Table -->
        <div class="table-container">
            <table>
                <thead>
                    <tr>
                        <th class="fixed-col col-loc">地点</th>
                        <th colspan="2" class="fixed-col col-cat-merged">类别</th>
                        <!-- Dynamic Days -->
                        <th v-for="d in daysInMonth" :key="d" class="day-col">
                           {{ selectedMonth }}月{{ d }}日
                        </th>
                    </tr>
                </thead>
                <tbody>
                    <template v-for="(locGroup, _locIndex) in schemaData" :key="locGroup.location">
                        <template v-for="(catGroup, catIndex) in locGroup.groups" :key="locGroup.location + catGroup.name + catIndex">
                            <tr v-for="(item, itemIndex) in catGroup.items" :key="locGroup.location + catGroup.name + catIndex + itemIndex">
                                <!-- Location Cell (Rowspan) -->
                                <td v-if="catIndex === 0 && itemIndex === 0" 
                                    :rowspan="getLocationRowSpan(locGroup)" 
                                    class="fixed-col col-loc loc-cell">
                                    {{ locGroup.location }}
                                </td>
                                
                                <!-- Category Cell (Rowspan & Colspan) -->
                                <td v-if="itemIndex === 0"  
                                    :rowspan="catGroup.items.length"
                                    :colspan="catGroup.name !== 'RFID' ? 2 : 1"
                                    :style="{ width: catGroup.name !== 'RFID' ? '250px' : '120px' }"
                                    class="fixed-col col-cat cat-cell">
                                    {{ catGroup.name }}
                                </td>
                                
                                <!-- Item Cell (Render only if not merged) -->
                                <td v-if="catGroup.name === 'RFID'" class="fixed-col col-sub">{{ item }}</td>
                                
                                <!-- Daily Cells -->
                                <td v-for="d in daysInMonth" :key="d" class="day-cell">
                                    <input 
                                        type="number" 
                                        v-model.number="getCellValue(locGroup.location, catGroup.name, item, d).quantity"
                                        class="cell-input" 
                                        min="0"
                                        placeholder="-"
                                        @input="markDirty"
                                    />
                                </td>
                            </tr>
                        </template>
                    </template>
                </tbody>
            </table>
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted, watch } from 'vue';
import api from '../services/api';
import { useKmartStore } from '../stores/kmart';
import { storeToRefs } from 'pinia';

// Add confirm dialog (browser native or custom) - for now using native confirm
// The user asked for "Refresh persistence", which is handled by store update.
// The user asked for "Delete Month" button.

const kmartStore = useKmartStore();
const { selectedYear, selectedMonth } = storeToRefs(kmartStore);

// Dynamic Year Range: Current Year - 2 to + 5
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

// Watch for changes to reload data (if needed, or just rely on mounted)
// In entry view, changing year/month usually implies loading different data if we were fetching existing data.
// But currently `loadData` uses these values.
watch([selectedYear, selectedMonth], () => {
    loadData();
});

// 根据用户反馈定义的业务 Schema
const schemaData = [
  {
    location: "China",
    groups: [
      { name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM", "半成品"] },
      // 非 RFID 项目移动到类别名称中，项目列表留空字符串以创建行
      { name: "吊牌", items: [""] },
      { name: "贴纸", items: [""] }, // 允许显示的名称重复，Key 会处理唯一性
      { name: "卡类 (袜卡/腰封)", items: [""] },
      { name: "洗标", items: [""] }
    ]
  },
  {
    location: "Vietnam",
    groups: [
      { name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] },
      { name: "吊牌", items: [""] },
      { name: "贴纸", items: [""] },
      { name: "卡类 (袜卡/腰封)", items: [""] },
      { name: "洗标", items: [""] }
    ]
  },
  {
      location: "Cambodia",
      groups: [
          { name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] },
          { name: "吊牌", items: [""] },
          { name: "贴纸", items: [""] },
          { name: "卡类 (袜卡/腰封)", items: [""] },
          { name: "洗标", items: [""] }
      ]
  },
  {
      location: "Indonesia",
      groups: [
          { name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] },
          { name: "吊牌", items: [""] },
          { name: "贴纸", items: [""] },
          { name: "卡类 (袜卡/腰封)", items: [""] },
          { name: "洗标", items: [""] }
      ]
  },
  {
      location: "Bangladesh",
      groups: [
          { name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] },
          { name: "吊牌", items: [""] },
          { name: "贴纸", items: [""] },
          { name: "卡类 (袜卡/腰封)", items: [""] },
          { name: "洗标", items: [""] },
          { name: "织标", items: [""] }
      ]
  },
  {
      location: "India",
      groups: [
          { name: "RFID", items: ["贴纸 44X18MM", "吊牌 45X76MM", "吊牌 45X61MM"] },
          { name: "吊牌", items: [""] },
          { name: "贴纸", items: [""] },
          { name: "卡类 (袜卡/腰封)", items: [""] },
          { name: "洗标", items: [""] }
      ]
  }
];

// 辅助计算行合并数
const getLocationRowSpan = (locGroup: any) => {
    return locGroup.groups.reduce((sum: number, g: any) => sum + g.items.length, 0);
};

// 计算当月天数
const daysInMonth = computed(() => {
    return new Date(selectedYear.value, selectedMonth.value, 0).getDate();
});

// 响应式数据存：Map<Key, Quantity>
// Key 格式: `${Location}::${Category}::${Item}::${Day}`
const dataMap = reactive(new Map<string, number>());
const initialDataMap = new Map<string, number>();

const isDirty = ref(false);
const isLoading = ref(false);

// 生成数据单元格的唯一 Key
const getCellKey = (loc: string, cat: string, item: string, day: number) => {
    return `${loc}::${cat}::${item}::${day}`;
};

// 获取单元格数值的辅助函数
const getCellValue = (loc: string, cat: string, item: string, day: number) => {
    const key = getCellKey(loc, cat, item, day);
    return {
        get quantity() { return dataMap.get(key); },
        set quantity(val: number | undefined) { 
            if (val === undefined || val === null) dataMap.delete(key);
            else dataMap.set(key, val);
        }
    }
};

// 标记数据已变动
const markDirty = () => isDirty.value = true;

/**
 * 业务逻辑匹配标准化：全量去空格、转大写、全角转半角。
 * 用于解决用户反馈的“双空格”或格式微差导致匹配失败的问题。
 */
const normalizeMatch = (s: any): string => {
    if (s === undefined || s === null) return '';
    return s.toString()
        .toUpperCase()
        .replace(/\s+/g, '') // 移除所有空白字符
        .replace(/[\uff01-\uff5e]/g, (ch: string) => String.fromCharCode(ch.charCodeAt(0) - 0xfee0)); // 全角转半角
};

// --- API 接口 ---

// 加载现有数据
// 加载现有数据
const loadData = async () => {
    try {
        isLoading.value = true;
        dataMap.clear();
        initialDataMap.clear();
        const res = await api.get<any[]>('/Order/kmart', {
            params: { year: selectedYear.value, month: selectedMonth.value }
        });
        
        res.data.forEach(rec => {
            const day = new Date(rec.date).getDate();
            
            // 尝试将 API 返回的字符串与 Schema 中的标准字符串匹配
            // 解决前后端/导入导出过程中的空格、全角半角等格式不一致导致不显示的问题
            let loc = rec.location;
            let cat = rec.category;
            let sub = rec.subCategory || "";

            // 1. 匹配 Location
            const locSchema = schemaData.find(s => normalizeMatch(s.location) === normalizeMatch(loc));
            if (locSchema) {
                loc = locSchema.location;
                
                // 2. 匹配 Category
                // 注意：API 返回的 category 可能就是 "RFID"（如果 Excel 导入时就是这样），也可能是 "RFID" 下的子项？
                // 根据 saveData 逻辑，category 应该是 schema 中的 group.name
                const groupSchema = locSchema.groups.find(g => normalizeMatch(g.name) === normalizeMatch(cat));
                if (groupSchema) {
                    cat = groupSchema.name;
                    
                    // 3. 匹配 SubCategory (仅针对 RFID)
                    if (normalizeMatch(cat) === "RFID") {
                        const itemSchema = groupSchema.items.find(i => normalizeMatch(i) === normalizeMatch(sub));
                        if(itemSchema) {
                            sub = itemSchema;
                        }
                    } else {
                        // 非 RFID 项目，subCategory 为空或 schema item (schema item is "" for others usually)
                        // 但我们的 key 生成依赖 item === "" for non-RFID
                        sub = ""; 
                    }
                }
            }

            const key = getCellKey(loc, cat, sub, day);
            dataMap.set(key, rec.quantity);
            initialDataMap.set(key, rec.quantity);
        });
        isDirty.value = false;
    } catch (e) {
        console.error("加载失败", e);
    } finally {
        isLoading.value = false;
    }
};

// 保存变动数据
const saveData = async () => {
    try {
        const payload: any[] = [];
        let changeCount = 0;

        for (const [key, qty] of dataMap.entries()) {
            // 仅发送有变动或新增的数据
            if (initialDataMap.get(key) === qty) continue;

            const parts = key.split('::');
            if (parts.length < 4) continue;
            const [loc, cat, item, dayStr] = parts;
            const day = parseInt(dayStr as string);
            const dateStr = `${selectedYear.value}-${String(selectedMonth.value).padStart(2,'0')}-${String(day).padStart(2,'0')}`;
            
            payload.push({
                location: loc,
                category: cat,
                subCategory: item,
                date: dateStr,
                quantity: qty
            });
            changeCount++;
        }
        
        if (changeCount === 0) {
            alert('没有变更需要保存。');
            return;
        }
        
        await api.post('/Order/kmart', payload.map(r => ({ ...r, date: new Date(r.date!) })));
        
        // 更新初始状态快照
        for (const [key, qty] of dataMap.entries()) {
            initialDataMap.set(key, qty);
        }
        
        isDirty.value = false;
        alert(`成功保存 ${changeCount} 条变更数据！`);
    } catch (e) {
        alert('保存失败');
    }
};

// 删除当月数据
const deleteData = async () => {
    if (!confirm(`确定要删除 ${selectedYear.value}年 ${selectedMonth.value}月 的所有数据吗？此操作不可恢复！`)) {
        return;
    }

    try {
        isLoading.value = true;
        await api.delete('/Order/kmart', {
            params: { year: selectedYear.value, month: selectedMonth.value }
        });
        
        // Clear local Data
        dataMap.clear();
        initialDataMap.clear();
        
        alert('删除成功');
        // Reload (which will be empty mostly, but confirming state)
        await loadData();
    } catch (e: any) {
        console.error("删除失败", e);
        if (e.response && e.response.status === 404) {
            alert('当前月份没有数据可删除。');
        } else {
            alert('删除操作失败，请重试。');
        }
    } finally {
        isLoading.value = false;
    }
};

// --- Excel 导入 ---
// --- Excel 导入 ---
const handleFileUpload = (event: Event) => {
    const target = event.target as HTMLInputElement;
    if (!target.files || target.files.length === 0) return;
    
    const file = target.files[0];
    const reader = new FileReader();
    reader.onload = async (e) => {
        try {
            const XLSX = await import('xlsx');
            const result = e.target?.result;
            if (!result) return;
            const data = new Uint8Array(result as ArrayBuffer);
            const workbook = XLSX.read(data, { type: 'array' });
            
            const firstSheetName = workbook.SheetNames[0];
            if (!firstSheetName) return;
            const worksheet = workbook.Sheets[firstSheetName];
            if (!worksheet) return;
            
            // 转换为 JSON (数组的数组)
            const rows = XLSX.utils.sheet_to_json(worksheet, { header: 1 }) as any[][];
            
            // 预期 Excel 格式:
            // [站点, 类别, 项目, 第1日值, 第2日值, ...]
            
            // 当前处于追踪状态的变量 (实现层级补齐)
            let currLoc = "";
            let currCat = "";
            let currItem = "";
            let dataImportedCount = 0;

            console.log("开始 Excel 导入，总行数:", rows.length);
            
            // 每次导入前先清空当前视图数据 (根据需求覆盖)
            // dataMap.clear(); 
            // initialDataMap.clear();

            rows.forEach((row, rowIndex) => {
                if (rowIndex === 0) return; // 跳过表头 row
                
                const rawLoc = row[0]?.toString().trim();
                const rawCat = row[1]?.toString().trim();
                const rawItem = row[2]?.toString().trim();

                // 层级补齐逻辑
                if (rawLoc) {
                    currLoc = rawLoc;
                    currCat = ""; 
                    currItem = "";
                }
                
                if (rawCat) {
                    currCat = rawCat;
                    currItem = ""; 
                }
                
                if (rawItem) {
                    currItem = rawItem;
                }

                if (!currLoc || !currCat) return;

                // 1. 验证站点 (Fuzzy Match)
                const locSchema = schemaData.find(s => normalizeMatch(s.location) === normalizeMatch(currLoc));
                if (!locSchema) return;
                const loc = locSchema.location;

                // 2. 验证类别 (Fuzzy Match)
                const groupSchema = locSchema.groups.find(g => normalizeMatch(g.name) === normalizeMatch(currCat));
                if (!groupSchema) return;
                const cat = groupSchema.name;

                // 3. 处理子项
                let sub = "";
                if (normalizeMatch(cat) === "RFID") {
                     const itemSchema = groupSchema.items.find(i => normalizeMatch(i) === normalizeMatch(currItem));
                     if (itemSchema) {
                         sub = itemSchema;
                     } else {
                         return; // RFID 项目未匹配，跳过
                     }
                } else {
                    sub = "";
                }

                // 4. 提取数据
                for (let d = 1; d <= daysInMonth.value; d++) {
                    const rowValue = row[d + 2];
                    if (rowValue !== undefined && rowValue !== null && rowValue !== "") {
                        const quantity = parseInt(rowValue.toString());
                        if (!isNaN(quantity)) {
                            const key = getCellKey(loc, cat, sub, d);
                            dataMap.set(key, quantity);
                            dataImportedCount++;
                        }
                    }
                }
            });

            if (dataImportedCount > 0) {
                isDirty.value = true; // 标记以便 saveData 识别
                // 自动保存
                //await saveData();
                // 自动刷新页面数据 (重新从数据库加载，确保一致性)
                alert(`成功导入 ${dataImportedCount} 条数据。请核对后点击“保存到数据库”。`);
                //await loadData();
            } else {
                alert("未识别到有效数据，请检查 Excel 模板。");
            }
        } catch (ex) {
            console.error("Excel 导入过程中发生错误:", ex);
            alert("文件读取失败。");
        } finally {
            // Reset input
             if (target) target.value = '';
        }
    };
    if (file) {
        reader.readAsArrayBuffer(file);
    }
};

onMounted(() => {
    loadData();
});
</script>

<style scoped>
.kmart-entry-view { display: flex; flex-direction: column; height: 100%; gap: 16px; }
.card { background: white; border-radius: 8px; border: 1px solid var(--border-light); overflow: hidden; display: flex; flex-direction: column; flex: 1; }
.toolbar { padding: 16px; border-bottom: 1px solid var(--border-light); display: flex; justify-content: space-between; align-items: center; }
.filter-group { display: flex; gap: 12px; align-items: center; }
.input-std { padding: 6px 12px; border: 1px solid #e2e8f0; border-radius: 4px; }
.actions { display: flex; gap: 12px; }

.upload-btn-wrapper { position: relative; overflow: hidden; display: inline-block; }
.upload-btn-wrapper input[type=file] {
  font-size: 100px; position: absolute; left: 0; top: 0; opacity: 0; cursor: pointer;
}

.table-container { flex: 1; overflow: auto; position: relative; }
table { border-collapse: separate; border-spacing: 0; min-width: 100%; font-size: 0.85rem; }
th, td { border-right: 1px solid #e2e8f0; border-bottom: 1px solid #e2e8f0; padding: 4px; text-align: center; }
th { background: #f8fafc; font-weight: 600; position: sticky; top: 0; z-index: 20; }
.fixed-col { position: sticky; z-index: 30; background: white; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
th.fixed-col { z-index: 40; background: #f1f5f9; }
.col-loc { left: 0; width: 60px; text-align: left; padding-left: 8px; }
.col-cat-merged { left: 60px; width: 250px; text-align: left; padding-left: 8px; }
.toolbar-card { flex: 0 0 auto; position: relative; }
.loading-overlay { position: absolute; top:0; left:0; right:0; bottom:0; background: rgba(255,255,255,0.7); z-index: 100; display: flex; align-items: center; justify-content: center; font-weight: bold; color: #3b82f6; }
.col-cat { left: 60px; width: 120px; text-align: center; padding-left: 8px; }
.col-sub { left: 100px; width: 130px; text-align: left; padding-left: 8px; }

.day-cell { padding: 0; min-width: 100px; }
.cell-input { width: 100%; height: 100%; border: none; text-align: center; outline: none; background: transparent; }
.cell-input:focus { background: #e0f2fe; }

.btn-danger { background-color: #ef4444; color: white; border: 1px solid #dc2626; }
.btn-danger:hover { background-color: #dc2626; }
</style>
