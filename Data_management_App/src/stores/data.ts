import { defineStore } from 'pinia';
import { ref } from 'vue';
import api from '../services/api';

/**
 * 数据记录接口定义
 */
export interface DataRecord {
    id: number;
    brand?: string;
    location?: string;
    item?: string; // 品牌/项目名称
    year: number;

    // 每月数据 (AC-实际, FC-预测, DIFF-达成率/差异)
    jan_ac: number; jan_fc: number; jan_diff: number;
    feb_ac: number; feb_fc: number; feb_diff: number;
    mar_ac: number; mar_fc: number; mar_diff: number;
    apr_ac: number; apr_fc: number; apr_diff: number;
    may_ac: number; may_fc: number; may_diff: number;
    jun_ac: number; jun_fc: number; jun_diff: number;
    jul_ac: number; jul_fc: number; jul_diff: number;
    aug_ac: number; aug_fc: number; aug_diff: number;
    sep_ac: number; sep_fc: number; sep_diff: number;
    oct_ac: number; oct_fc: number; oct_diff: number;
    nov_ac: number; nov_fc: number; nov_diff: number;
    dec_ac: number; dec_fc: number; dec_diff: number;

    // 季度汇总数据
    q1_ac: number; q1_fc: number; q1_diff: number;
    q2_ac: number; q2_fc: number; q2_diff: number;
    q3_ac: number; q3_fc: number; q3_diff: number;
    q4_ac: number; q4_fc: number; q4_diff: number;

    updated_by: string;
    updated_at: string;
}

/**
 * 业务数据管理 Store
 */
export const useDataStore = defineStore('data', () => {
    // 可选品牌列表
    const brands = ref<string[]>(['Nike', 'Adidas', 'Puma']);
    // 当前显示的记录列表
    const records = ref<DataRecord[]>([]);

    /**
     * 从后端获取品牌列表
     */
    async function fetchBrands() {
        try {
            const res = await api.get<string[]>('/Data/brands');
            if (res.data && res.data.length > 0) {
                // Backend now returns the authoritative list of 15 brands
                brands.value = res.data;
            }
        } catch (e) {
            console.error('获取品牌列表失败:', e);
        }
    }

    /**
     * 加载指定品牌的数据记录
     */
    async function loadRecords(brandName: string) {
        try {
            const res = await api.get<DataRecord[]>('/Data', { params: { brand: brandName } });
            records.value = res.data;
        } catch (e) {
            console.error('加载记录失败:', e);
            records.value = [];
        }
    }

    /**
     * 加载全量数据记录（主要用于大屏展示）
     */
    async function loadAllRecords() {
        try {
            const res = await api.get<DataRecord[]>('/Data'); // 不传参数获取所有
            records.value = res.data;
            return res.data;
        } catch (e) {
            console.error('加载全量数据失败:', e);
            records.value = [];
            return [];
        }
    }

    /**
     * 保存或更新单条数据记录
     */
    async function saveRecord(newRecord: DataRecord & { brand?: string }) {
        try {
            // Ensure brand is set (backend requires it to route to correct table)
            // Ideally brand should be passed explicitly. Fallback to item if it matches a known brand is risky if item is a SKU.
            // So we assume the caller sets 'brand'.
            const res = await api.post<DataRecord>('/Data', newRecord);

            // 更新本地状态
            const saved = res.data;
            const index = records.value.findIndex(r => r.id === saved.id);
            if (index !== -1) {
                records.value[index] = saved;
            } else {
                records.value.push(saved);
            }

        } catch (e) {
            console.error('保存记录失败:', e);
            throw e;
        }
    }

    // 初始化加载
    fetchBrands();

    return { brands, records, loadRecords, loadAllRecords, saveRecord, fetchBrands };
});
