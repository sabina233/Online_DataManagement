<template>
  <div class="data-entry">
    <div class="header-section">
        <h3><Plus :size="20" /> {{ t('entry.title') }}</h3>
        <p class="text-secondary">{{ t('entry.subtitle') }}</p>
    </div>

    <!-- Configuration Bar -->
    <div class="actions-bar">
        <div class="filter-group">
            <div class="filter-item">
                <label>Brand:</label>
                <select v-model="selectedBrand" class="input-std brand-select">
                    <option disabled value="">Select Brand</option>
                    <option v-for="b in dataStore.brands" :key="b" :value="b">{{ b }}</option>
                </select>
            </div>
            <div class="filter-item">
                <label>{{ t('brand.year_filter') }}:</label>
                <select v-model="selectedYear" class="input-std">
                    <option v-for="y in [2024, 2025, 2026]" :key="y" :value="y">{{ y }}</option>
                </select>
            </div>
        </div>

        <div class="button-group">
            <div class="file-upload-wrapper" @dragover.prevent @drop.prevent="handleDrop">
                <label class="btn btn-secondary btn-file">
                    <Upload :size="16" /> Import Excel
                    <input type="file" accept=".xlsx, .xls" @change="handleFileUpload" hidden />
                </label>
            </div>
            
            <button class="btn btn-secondary" @click="addRow">
                <Plus :size="16" /> {{ t('entry.add_row') }}
            </button>
            <button class="btn btn-primary" @click="handleSave" :disabled="isSaving || !selectedBrand">
                <Save :size="16" /> {{ isSaving ? t('entry.saving') : t('entry.save') }}
            </button>
        </div>
        
        <span v-if="saveMessage" class="save-msg" :class="{ error: isError }">{{ saveMessage }}</span>
    </div>

    <div v-if="!selectedBrand" class="alert-info">
        Please select a <strong>Brand</strong> to start entering or importing data.
    </div>

    <div v-else class="editor-container card">
        <div class="table-wrapper">
            <table>
                <thead>
                    <tr>
                        <th class="sticky-col action-col"></th>
                        <th class="sticky-col loc-col">Location</th>
                        <th class="sticky-col item-col">Item</th>
                        <th v-for="m in months" :key="m.key" colspan="2" class="month-header">{{ m.label }}</th>
                    </tr>
                    <tr>
                        <th class="sticky-col action-col"></th>
                        <th class="sticky-col loc-col"></th>
                        <th class="sticky-col item-col"></th>
                        <template v-for="m in months" :key="m.key">
                            <th class="sub-th">AC</th>
                            <th class="sub-th">FC</th>
                        </template>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="(row, index) in rows" :key="row._tempId">
                        <td class="sticky-col action-col">
                            <button class="btn-icon danger" @click="removeRow(index)" title="Remove">
                                <Trash2 :size="16" />
                            </button>
                        </td>
                        <td class="sticky-col loc-col">
                            <input type="text" v-model="row.location" :placeholder="t('entry.location_ph')" />
                        </td>
                        <td class="sticky-col item-col">
                            <input type="text" v-model="row.item" placeholder="Item Name" />
                        </td>
                        <template v-for="m in months" :key="m.key">
                            <td><input type="number" v-model.number="row[m.key + '_ac']" /></td>
                            <td><input type="number" v-model.number="row[m.key + '_fc']" /></td>
                        </template>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>

    <!-- Conflict Dialog -->
    <div v-if="conflictData" class="modal-overlay">
        <div class="modal card conflict-modal">
            <h3>⚠️ {{ t('entry.conflict_title') || 'Data Conflict' }}</h3>
            <p>{{ t('entry.conflict_msg') || 'Existing data found for this period. Compare and choose action:' }}</p>
            
            <div class="conflict-comparison">
                <div class="compare-box">
                    <h4>Current values in DB</h4>
                    <p>AC: {{ conflictData.dbValue.ac }}</p>
                    <p>FC: {{ conflictData.dbValue.fc }}</p>
                </div>
                <div class="arrow">→</div>
                <div class="compare-box highlight">
                    <h4>Your new values</h4>
                    <p>AC: {{ conflictData.newValue.ac }}</p>
                    <p>FC: {{ conflictData.newValue.fc }}</p>
                </div>
            </div>

            <div class="modal-actions">
                <button class="btn btn-secondary" @click="conflictData = null">Cancel</button>
                <button class="btn btn-primary" @click="resolveConflict">Replace Existing</button>
            </div>
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useDataStore } from '../stores/data';
import { useI18n } from 'vue-i18n';
import { Plus, Trash2, Save, Upload } from 'lucide-vue-next';
import * as XLSX from 'xlsx';

/**
 * 注入业务 Store 与 国际化
 */
const authStore = useAuthStore();
const dataStore = useDataStore();
const { t } = useI18n();

// 定义月份常量，用于动态生成表格列
const months = [
    { key: 'jan', label: 'Jan' }, { key: 'feb', label: 'Feb' }, { key: 'mar', label: 'Mar' },
    { key: 'apr', label: 'Apr' }, { key: 'may', label: 'May' }, { key: 'jun', label: 'Jun' },
    { key: 'jul', label: 'Jul' }, { key: 'aug', label: 'Aug' }, { key: 'sep', label: 'Sep' },
    { key: 'oct', label: 'Oct' }, { key: 'nov', label: 'Nov' }, { key: 'dec', label: 'Dec' }
];

/**
 * 可编辑行数据接口
 */
interface EditableRow {
    _tempId: string;
    location: string;
    item: string;
    [key: string]: any; // 用于存储每月 ac/fc 的动态属性
}

// 页面状态：年份、品牌、保存中标识等
const selectedYear = ref(new Date().getFullYear());
const selectedBrand = ref('');
const isSaving = ref(false);
const isError = ref(false);
const saveMessage = ref('');
const conflictData = ref<any>(null); // 冲突数据详情

/**
 * 创建一个包含默认初始值的空行
 */
const createEmptyRow = (): EditableRow => {
    const row: EditableRow = {
        _tempId: crypto.randomUUID(),
        location: '',
        item: '', // Item name
    };
    months.forEach(m => {
        row[`${m.key}_ac`] = 0;
        row[`${m.key}_fc`] = 0;
    });
    return row;
};

// 响应式行数据列表
const rows = ref<EditableRow[]>([createEmptyRow()]);

/**
 * 草稿逻辑：使用 localStorage 持久化未保存的录入数据
 */
const DRAFT_KEY = 'entry_draft_v3';
const loadDraft = () => {
    const draft = localStorage.getItem(DRAFT_KEY);
    if (draft) {
        try {
            const data = JSON.parse(draft);
            rows.value = data.rows || [createEmptyRow()];
            if (data.brand) selectedBrand.value = data.brand;
            if (data.year) selectedYear.value = data.year;
        } catch(e) { console.error(e) }
    }
};
loadDraft();
// 监听 rows 变化，实时更新本地草稿
watch([rows, selectedBrand, selectedYear], () => {
    localStorage.setItem(DRAFT_KEY, JSON.stringify({
        rows: rows.value,
        brand: selectedBrand.value,
        year: selectedYear.value
    }));
}, { deep: true });

/**
 * 新增一行
 */
const addRow = () => rows.value.push(createEmptyRow());

/**
 * 删除指定索引的行
 */
const removeRow = (index: number) => {
    if (rows.value.length > 1) rows.value.splice(index, 1);
};

/**
 * Excel 导入逻辑
 */
const handleFileUpload = (e: Event) => {
    const files = (e.target as HTMLInputElement).files;
    if (files && files[0]) processExcel(files[0]);
};

const handleDrop = (e: DragEvent) => {
    const files = e.dataTransfer?.files;
    if (files && files[0]) processExcel(files[0]);
};

const processExcel = async (file: File) => {
    if (!selectedBrand.value) {
        alert("Please select a Brand first!");
        return;
    }
    const data = await file.arrayBuffer();
    const workbook = XLSX.read(data);
    const firstSheetName = workbook.SheetNames[0];
    if (!firstSheetName) return;
    const worksheet = workbook.Sheets[firstSheetName];
    if (!worksheet) return;
    const distinctJson = XLSX.utils.sheet_to_json(worksheet, { header: 1 }) as any[][];

    if (distinctJson.length < 2) return;

    // Assumed Header mapping: Row 0 is Year/Month, Row 1 is AC/FC... simplified parsing
    // But typical Excel import needs clear headers. Let's assume standard format:
    // Location | Item | Jan AC | Jan FC | ...
    
    // Simple heuristic parser: skip header row(s) and look for columns
    // This part requires alignment with user's Excel template.
    // For now, I'll attempt to map by index if headers match loosely, or just simplistic mapping.
    // Let's assume the first row with "Location" is the header.
    
    let headerRowIndex = distinctJson.findIndex(r => r.some((c:any) => String(c).toLowerCase().includes('location')));
    if (headerRowIndex === -1) headerRowIndex = 0; // fallback

    const headerRow = distinctJson[headerRowIndex];
    if (!headerRow) return;
    const headers = headerRow.map((h:any) => String(h).trim().toLowerCase());
    const newRows: EditableRow[] = [];

    // Map month names to expected keys
    const monthMap: {[key:string]: string} = {
        'jan': 'jan', 'feb': 'feb', 'mar': 'mar', 'apr': 'apr', 'may': 'may', 'jun': 'jun',
        'jul': 'jul', 'aug': 'aug', 'sep': 'sep', 'oct': 'oct', 'nov': 'nov', 'dec': 'dec'
    };

    for (let i = headerRowIndex + 1; i < distinctJson.length; i++) {
        const rowData = distinctJson[i];
        if (!rowData || rowData.length === 0) continue;

        const row = createEmptyRow();
        
        // Find columns based on headers locally
        headers.forEach((h, idx) => {
            if (h === 'location') row.location = rowData[idx];
            if (h === 'item') row.item = rowData[idx];
            
            // Try to match "Jan AC", "Jan FC"
            for (const mStr in monthMap) {
                if (h.startsWith(mStr)) {
                    const key = monthMap[mStr];
                    if (key) {
                        // check suffix
                        if (h.includes('ac') && !h.includes('vs')) {
                             // Use 'as any' to bypass specific indexing strictness if needed, or ensure row is Indexable
                             row[`${key}_ac`] = Number(rowData[idx]) || 0;
                        }
                        if (h.includes('fc') && !h.includes('vs')) {
                             row[`${key}_fc`] = Number(rowData[idx]) || 0;
                        }
                    }
                }
            }
        });

        if (row.location && row.item) {
            newRows.push(row);
        }
    }
    
    if (newRows.length > 0) {
        if (confirm(`Found ${newRows.length} rows. Append to current list?`)) {
            rows.value.push(...newRows);
        }
    } else {
        alert("No valid data found. Check column headers (Location, Item, Jan AC, Jan FC...)." );
    }
};

/**
 * 核心保存逻辑：将表格数据分行保存至后端
 */
const handleSave = async () => {
    if (!selectedBrand.value) {
        alert("Select Brand");
        return;
    }

    isSaving.value = true;
    isError.value = false;
    saveMessage.value = '';

    try {
        for (const row of rows.value) {
            // 跳过信息不全的空行
            if (!row.item || !row.location) continue;

            // 调用后端保存
            await performSave(row);
        }

        saveMessage.value = t('entry.success') || "Successfully Saved";
        // 清空输入列表并重置草稿
        rows.value = [createEmptyRow()];
        localStorage.removeItem(DRAFT_KEY);
    } catch (e) {
        isError.value = true;
        saveMessage.value = t('entry.error') || "Save Failed";
        console.error(e);
    } finally {
        isSaving.value = false;
    }
};

/**
 * 冲突解决逻辑
 */
const resolveConflict = async () => {
    if (!conflictData.value) return;
    try {
        await performSave(conflictData.value.rowRef, true);
        conflictData.value = null;
        saveMessage.value = t('entry.success');
    } catch (e) {
        isError.value = true;
        saveMessage.value = t('entry.error');
    }
};

/**
 * 执行单行数据的核心 API 调用
 */
const performSave = async (row: EditableRow, _isUpdate = false) => {
    const record: any = {
        brand: selectedBrand.value,
        year: selectedYear.value,
        location: row.location,
        item: row.item,
        updated_by: authStore.user?.username || 'system',
        updated_at: new Date().toISOString()
    };
    
    // 填充 1-12 月的 AC 和 FC 字段
    months.forEach(m => {
        record[`${m.key}_ac`] = row[`${m.key}_ac`];
        record[`${m.key}_fc`] = row[`${m.key}_fc`];
    });

    // 调用 DataStore 的保存方法
    await dataStore.saveRecord(record);
};
</script>

<style scoped>
.data-entry { max-width: 100%; padding: 20px; }
.header-section { margin-bottom: 24px; }
.actions-bar { 
    display: flex; gap: 20px; align-items: center; margin-bottom: 20px; flex-wrap: wrap;
    padding: 16px; background: white; border-radius: 8px; border: 1px solid var(--border-light);
}

.filter-group { display: flex; gap: 15px; align-items: center; }
.filter-item { display: flex; align-items: center; gap: 8px; }
.filter-item label { font-size: 0.9rem; color: var(--text-secondary); white-space: nowrap; }

.brand-select { width: 180px; font-weight: 500; }
.input-std { padding: 6px 12px; border: 1px solid var(--border-light); border-radius: 6px; }

.button-group { display: flex; gap: 10px; margin-left: auto; }

.file-upload-wrapper {
    border: 1px dashed var(--border-light);
    border-radius: 6px;
    padding: 2px 8px;
    transition: all 0.2s;
}
.file-upload-wrapper:hover { border-color: var(--primary-color); background: var(--primary-50); }
.btn-file { cursor: pointer; display: flex; align-items: center; gap: 6px; }

.save-msg { font-size: 0.9rem; color: var(--success); margin-left: 12px; }
.save-msg.error { color: var(--danger); }

.alert-info { padding: 40px; text-align: center; background: #eff6ff; color: #1e3a8a; border-radius: 8px; }

.editor-container { padding: 0; overflow: hidden; }
.table-wrapper { overflow-x: auto; max-height: 600px; }

table { width: 100%; border-collapse: separate; border-spacing: 0; }
th, td { 
    padding: 12px; border-bottom: 1px solid var(--border-light); border-right: 1px solid var(--border-light);
    min-width: 100px; text-align: center;
}
th { background: #f8fafc; position: sticky; top: 0; z-index: 10; font-weight: 600; color: var(--text-secondary); }

.sticky-col { position: sticky; left: 0; background: white; z-index: 20; }
.action-col { width: 60px; min-width: 60px; left: 0; }
.loc-col { width: 150px; min-width: 150px; left: 60px; }
.item-col { width: 180px; min-width: 180px; left: 210px; border-right: 2px solid #cbd5e1; }

input { width: 100%; border: 1px solid transparent; padding: 4px; border-radius: 4px; text-align: center; background: transparent; }
input:focus { border-color: var(--primary-color); background: #f0f9ff; outline: none; }

.modal-overlay { 
    position: fixed; top: 0; left: 0; right: 0; bottom: 0; 
    background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; 
}
.conflict-modal { width: 450px; padding: 24px; }
.conflict-comparison { display: flex; align-items: center; gap: 15px; margin: 20px 0; }
.compare-box { flex: 1; padding: 12px; border: 1px solid var(--border-light); border-radius: 6px; background: #f9fafb; }
.compare-box.highlight { border-color: var(--primary-color); background: #eff6ff; }
.modal-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 24px; }
</style>
