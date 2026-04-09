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
                    <option v-for="y in [2024, 2025, 2026,2027,2028]" :key="y" :value="y">{{ y }}</option>
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
            
            <button class="btn btn-secondary" @click="clearData" :disabled="rows.length <= 1 && (!rows[0] || !rows[0].location)">
                <Trash2 :size="16" /> {{ t('entry.clear') || 'Clear' }}
            </button>
            <button class="btn btn-secondary" @click="addRow">
                <Plus :size="16" /> {{ t('entry.add_row') }}
            </button>
            <button class="btn btn-primary" @click="handleSave" :disabled="isSaving || !selectedBrand">
                <Save :size="16" /> {{ isSaving ? t('entry.saving') : t('entry.save') }}
            </button>
            <button class="btn btn-warning" v-if="selectedBrand" @click="openEditModal">
                <Edit :size="16" /> {{ t('entry.edit') || 'Edit' }}
            </button>
        </div>
        
        <span v-if="saveMessage" class="save-msg" :class="{ error: isError }">{{ saveMessage }}</span>
    </div>

    <div v-if="!selectedBrand" class="alert-info">
        Please select a <strong>Brand</strong> to start entering or importing data.
    </div>

    <div v-if="selectedBrand" class="paste-section card">
        <div 
            class="paste-area" 
            contenteditable="true" 
            @paste="handlePaste"
            placeholder="📋 Paste Excel data here (Click and Ctrl+V)"
        ></div>
        <div class="paste-hint">
            <small class="text-secondary">
                Supports auto-detection of multiple table blocks (e.g., Q1, Q2, Q3 tables). 
                Tables must have "Location" headers. Month columns (e.g., "Jan AC", "Feb FC") are auto-mapped.
            </small>
        </div>
    </div>

    <div v-if="selectedBrand" class="editor-container card">
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
    <!-- Edit Modal -->
    <div v-if="showEditModal" class="modal-overlay">
        <div class="modal-content large-modal">
            <div class="modal-header">
                <div class="header-left">
                    <h3>修改数据 - {{ selectedBrand }} {{ selectedYear }}</h3>
                    <button class="btn btn-add-row" @click="addNewRowToExisting">
                        <Plus :size="16" /> 添加数据行
                    </button>
                </div>
                <button class="close-btn" @click="showEditModal = false">×</button>
            </div>
            <div class="modal-body">
                <div v-if="loadingExisting" class="loading-state">Loading...</div>
                <div v-else class="table-scroll">
                    <table>
                        <thead>
                            <tr>
                                <th class="sticky-col action-col">操作</th>
                                <th class="sticky-col loc-col">Location</th>
                                <th class="sticky-col item-col">Item</th>
                                <template v-for="m in months" :key="m.key">
                                    <th>{{ m.label }} AC</th>
                                    <th>{{ m.label }} FC</th>
                                </template>
                            </tr>
                        </thead>
                        <tbody>
                            <tr v-for="row in existingRows" :key="row.id">
                                <td class="sticky-col action-col">
                                    <button class="btn-icon delete" @click="markForDeletion(row)" title="删除此行">
                                        <Trash2 :size="16" />
                                    </button>
                                </td>
                                <td class="sticky-col loc-col">
                                    <input type="text" v-model="row.location" class="input-cell" />
                                </td>
                                <td class="sticky-col item-col">
                                    <input type="text" v-model="row.item" class="input-cell" />
                                </td>
                                <template v-for="m in months" :key="m.key">
                                    <td><input type="text" v-model.lazy="row[`${m.key}_ac`]" @blur="formatRowNumber(row, `${m.key}_ac`)" class="num-cell" /></td>
                                    <td><input type="text" v-model.lazy="row[`${m.key}_fc`]" @blur="formatRowNumber(row, `${m.key}_fc`)" class="num-cell" /></td>
                                </template>
                            </tr>
                            <tr v-if="existingRows.length === 0">
                                <td colspan="30" class="text-center p-4">暂无数据</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
            <div class="modal-footer">
                <div v-if="rowsToDelete.length > 0" class="delete-hint text-danger">
                    将会删除 {{ rowsToDelete.length }} 条记录
                </div>
                <button class="btn btn-secondary" @click="showEditModal = false">取消</button>
                <button class="btn btn-primary" @click="saveExistingChanges" :disabled="isSavingExisting">
                    {{ isSavingExisting ? '保存中...' : '确认保存' }}
                </button>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useDataStore } from '../stores/data';
import { useI18n } from 'vue-i18n';
import { Plus, Trash2, Save, Upload, Edit } from 'lucide-vue-next';
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
// Helper to generate UUID v4 compatible with insecure contexts
const generateUUID = () => {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
};

const createEmptyRow = (): EditableRow => {
    const row: EditableRow = {
        _tempId: generateUUID(),
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

const clearData = () => {
    if (confirm(t('entry.confirm_clear') || "Are you sure you want to clear all data?")) {
        rows.value = [createEmptyRow()];
        localStorage.removeItem(DRAFT_KEY);
    }
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

// --- Paste Logic ---

const handlePaste = (e: ClipboardEvent) => {
    e.preventDefault();
    const htmlData = e.clipboardData?.getData('text/html');
    const textData = e.clipboardData?.getData('text/plain');

    if (htmlData) {
        parseHtmlTable(htmlData);
    } else if (textData) {
        alert("Pasted data is strictly text/plain. HTML table format is preferred for accurate column mapping.");
    }
    
    // Clear the paste area content
    const target = e.target as HTMLElement;
    nextTick(() => target.innerHTML = '');
};

const parseHtmlTable = (html: string) => {
    const parser = new DOMParser();
    const doc = parser.parseFromString(html, 'text/html');
    const trs = doc.querySelectorAll('tr');

    if (trs.length === 0) return;

    let currentColumnMap: Record<number, { month: string, type: 'ac' | 'fc' }> = {};
    let hasFoundHeader = false;
    let locationColIdx = -1;
    let itemColIdx = -1;
    
    // Month mapping regex matches "Jan AC", "Jan FC" - STRICT match to avoid "vs" suffix (e.g. "Jan AC vs FC")
    const headerRegex = /^(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\s+(ac|fc)\s*$/i;

    let processedCount = 0;

    trs.forEach(tr => {
        const tds = Array.from(tr.querySelectorAll('td, th'));
        const rowText = tds.map(td => td.textContent?.trim() || '').join(' ').toLowerCase();

        // Detect Header Row - Only require "Location". "Item" is optional.
        if (rowText.includes('location')) {
            currentColumnMap = {};
            locationColIdx = -1;
            itemColIdx = -1;
            hasFoundHeader = true;

            tds.forEach((td, index) => {
                const text = td.textContent?.trim() || '';
                if (text.toLowerCase() === 'location') locationColIdx = index;
                else if (text.toLowerCase() === 'item' || text.toLowerCase().includes('item')) itemColIdx = index;
                else {
                    const match = text.match(headerRegex);
                    if (match && match[1] && match[2]) {
                        const monthStr = match[1].toLowerCase(); 
                        const type = match[2].toLowerCase(); 
                        // We only care about AC and FC values, NOT the ratio
                        if (type === 'ac' || type === 'fc') {
                            currentColumnMap[index] = { month: monthStr, type: type as 'ac' | 'fc' };
                        }
                    }
                }
            });
            return; 
        }

        // Process Data Row - Allow Item column to be missing
        if (hasFoundHeader && locationColIdx !== -1) {
            const locCell = tds[locationColIdx];
            const itemCell = itemColIdx !== -1 ? tds[itemColIdx] : null;
            
            const rowLocation = locCell ? locCell.textContent?.trim() : '';
            const rowItem = itemCell ? itemCell.textContent?.trim() : '';

            // Allow item to be empty
            if (rowLocation) {
                mergeOrAddRow(rowLocation, rowItem, tds, currentColumnMap);
                processedCount++;
            }
        }
    });
};

const mergeOrAddRow = (
    loc: string, 
    item: string, 
    cells: Element[], 
    colMap: Record<number, { month: string, type: 'ac' | 'fc' }>
    ) => {
    
    const normLoc = loc.toLowerCase();
    const normItem = item.toLowerCase();

    // Find existing row - match both location and item (even if item is empty)
    let targetRow = rows.value.find(r => 
        r.location.toLowerCase() === normLoc && 
        r.item.toLowerCase() === normItem
    );

    const isNew = !targetRow;
    if (isNew) {
        targetRow = createEmptyRow();
        targetRow.location = loc;
        targetRow.item = item;
    }

    // Update fields
    Object.entries(colMap).forEach(([idxStr, map]) => {
        const index = parseInt(idxStr);
        const cellText = cells[index]?.textContent?.trim() || '';
        const val = parseFloat(cellText.replace(/,/g, ''));
        
        if (!isNaN(val)) {
            const key = `${map.month}_${map.type}`;
            if (targetRow) targetRow[key] = val;
        }
    });

    if (isNew && targetRow) {
        // If initial clean
        const initialRow = rows.value[0];
        if (rows.value.length === 1 && initialRow && !initialRow.location && !initialRow.item) {
            rows.value[0] = targetRow;
        } else {
            rows.value.push(targetRow);
        }
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
            // 跳过 location 为空的行 (item 可为空)
            if (!row.location) continue;

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

// --- Edit Existing Data Logic ---
const showEditModal = ref(false);
const loadingExisting = ref(false);
const isSavingExisting = ref(false);
const existingRows = ref<any[]>([]);
const rowsToDelete = ref<number[]>([]);

const openEditModal = async () => {
    if (!selectedBrand.value) return;
    showEditModal.value = true;
    loadingExisting.value = true;
    existingRows.value = [];
    rowsToDelete.value = [];

    try {
        // Load all records for the brand
        await dataStore.loadRecords(selectedBrand.value);
        // Filter by selected year
        const year = parseInt(selectedYear.value as any);
        existingRows.value = dataStore.records
            .filter(r => r.year === year)
            // Clone deep to avoid modifying store directly until save
            .map(r => JSON.parse(JSON.stringify(r)));
    } catch (e) {
        alert("加载数据失败");
    } finally {
        loadingExisting.value = false;
    }
};

const markForDeletion = (row: any) => {
    // Add to delete list
    if (row.id) {
        rowsToDelete.value.push(row.id);
    }
    // Remove from UI list
    const idx = existingRows.value.indexOf(row);
    if (idx !== -1) existingRows.value.splice(idx, 1);
};

const addNewRowToExisting = () => {
    const newRow: any = {
        id: 0, // 0 indicates new record
        brand: selectedBrand.value,
        year: parseInt(selectedYear.value as any),
        location: '',
        item: '',
        // Initialize months
    };
    months.forEach(m => {
        newRow[`${m.key}_ac`] = 0;
        newRow[`${m.key}_fc`] = 0;
    });
    // Add to top or bottom? Top might be more visible
    existingRows.value.unshift(newRow);
    
    // Scroll to top of table
    nextTick(() => {
        const container = document.querySelector('.table-scroll');
        if (container) container.scrollTop = 0;
    });
};

const formatRowNumber = (row: any, field: string) => {
    let val = row[field];
    if (typeof val === 'string') {
        val = parseFloat(val.replace(/,/g, ''));
    }
    if (isNaN(val)) val = 0;
    row[field] = val;
};

const saveExistingChanges = async () => {
    if (!confirm("确认保存修改？这将覆盖或删除数据库中的记录。")) return;
    
    isSavingExisting.value = true;
    try {
        // 1. Delete removed rows
        for (const id of rowsToDelete.value) {
            await dataStore.deleteRecord(id, selectedBrand.value);
        }

        // 2. Update existing rows
        for (const row of existingRows.value) {
             months.forEach(m => {
                row[`${m.key}_ac`] = Number(row[`${m.key}_ac`]) || 0;
                row[`${m.key}_fc`] = Number(row[`${m.key}_fc`]) || 0;
             });
             
             // Ensure metadata is set for update
             row.brand = selectedBrand.value;
             row.updated_by = authStore.user?.username || 'system';
             row.updated_at = new Date().toISOString();
             
             await dataStore.saveRecord(row); // SaveRecord handles update if ID exists
        }

        alert("保存成功！");
        showEditModal.value = false;
        // Refresh store
        await dataStore.loadRecords(selectedBrand.value);
    } catch (e) {
        console.error(e);
        alert("保存失败，请检查控制台。");
    } finally {
        isSavingExisting.value = false;
    }
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
    min-width: 180px; text-align: center;
}
th { background: #f8fafc; position: sticky; top: 0; z-index: 10; font-weight: 600; color: var(--text-secondary); }

.sticky-col { position: sticky; left: 0; background: white; z-index: 20; }
.action-col { width: 60px; min-width: 60px; left: 0; }
.loc-col { width: 150px; min-width: 150px; left: 60px; }
.item-col { width: 180px; min-width: 250px; left: 210px; border-right: 2px solid #cbd5e1; }

input { width: 100%; border: 1px solid transparent; padding: 4px; border-radius: 4px; text-align: center; background: transparent; }
input:focus { border-color: var(--primary-color); background: #f0f9ff; outline: none; }

.modal-overlay {
    position: fixed; top: 0; left: 0; width: 100vw; height: 100vh;
    background: rgba(0,0,0,0.5); z-index: 100;
    display: flex; align-items: center; justify-content: center;
}
.modal-content {
    background: white; border-radius: 12px;
    box-shadow: 0 20px 25px -5px rgba(0,0,0,0.1), 0 10px 10px -5px rgba(0,0,0,0.04);
    display: flex; flex-direction: column; max-height: 90vh;
}
.modal-content.large-modal { width: 95vw; height: 90vh; max-width: 1400px; }
.modal-header { 
    padding: 20px 24px; border-bottom: 1px solid var(--border-light); 
    display: flex; justify-content: space-between; align-items: center; 
    background: #f8fafc; border-radius: 12px 12px 0 0;
}
.header-left { display: flex; align-items: center; gap: 16px; }
.modal-header h3 { font-size: 1.25rem; color: #1e293b; margin: 0; }
.modal-body { padding: 0; overflow-y: auto; flex: 1; background: #fff; }

.modal-footer { 
    padding: 16px 24px; border-top: 1px solid var(--border-light); 
    display: flex; justify-content: flex-end; align-items: center; gap: 12px; 
    background: #fff; border-radius: 0 0 12px 12px;
}
.delete-hint { margin-right: auto; font-size: 0.9rem; font-weight: 500; }

.input-cell { width: 100%; border: none; background: transparent; padding: 4px; text-align: left; }
.input-cell:focus { background: #f0f9ff; outline: 2px solid #3b82f6; border-radius: 4px; }
.num-cell { text-align: right; }

.btn-icon.delete { color: #ef4444; padding: 6px; border-radius: 4px; border: none; background: transparent; cursor: pointer; }
.btn-icon.delete:hover { background: #fee2e2; }

.btn-add-row { 
    padding: 6px 14px; 
    font-size: 0.85rem; 
    font-weight: 500;
    display: flex; 
    align-items: center; 
    gap: 6px; 
    border-radius: 6px; 
    border: none;
    background: #eff6ff;
    color: #3b82f6;
    transition: all 0.2s ease;
    border: 1px solid #dbeafe;
}
.btn-add-row:hover { 
    background: #3b82f6; 
    color: white;
    border-color: #3b82f6;
    box-shadow: 0 4px 6px -1px rgba(59, 130, 246, 0.2); 
    transform: translateY(-1px);
}

.close-btn { width: 32px; height: 32px; border-radius: 50%; display: flex; align-items: center; justify-content: center; transition: background 0.2s; }
.close-btn:hover { background: #e2e8f0; }

.conflict-modal { width: 450px; padding: 24px; }
.conflict-comparison { display: flex; align-items: center; gap: 15px; margin: 20px 0; }
.compare-box { flex: 1; padding: 12px; border: 1px solid var(--border-light); border-radius: 6px; background: #f9fafb; }
.compare-box.highlight { border-color: var(--primary-color); background: #eff6ff; }
.modal-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 24px; }

.paste-section { margin-bottom: 20px; padding: 16px; background: white; border-radius: 8px; border: 1px solid #e2e8f0; }
.paste-area {
    min-height: 80px;
    border: 2px dashed #cbd5e1;
    border-radius: 6px;
    padding: 12px;
    color: #64748b;
    font-size: 0.9rem;
    outline: none;
    transition: all 0.2s;
}
.paste-area:focus {
    border-color: #3b82f6;
    background: #f0f9ff;
    color: #1e293b;
}
.paste-area:empty:before {
    content: attr(placeholder);
    color: #94a3b8;
}
.paste-hint { margin-top: 8px; font-size: 0.8rem; color: #94a3b8; }
</style>
