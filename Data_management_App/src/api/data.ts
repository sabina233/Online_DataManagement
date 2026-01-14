import api from '../services/api';
import type { DataRecord } from '../stores/data';

export const getBrands = () => {
    return api.get<any, string[]>('/data/brands');
};

export const getRecords = (brand: string) => {
    return api.get<any, DataRecord[]>(`/data/records/${brand}`);
};

export const saveRecord = (record: DataRecord) => {
    return api.post('/data/records', record);
};
