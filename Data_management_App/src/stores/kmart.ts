import { defineStore } from 'pinia';
import { ref, watch } from 'vue';

export const useKmartStore = defineStore('kmart', () => {
    const currentYear = new Date().getFullYear();
    const currentMonth = new Date().getMonth() + 1;

    // Load from localStorage or use default
    const savedYear = localStorage.getItem('kmart_year');
    const savedMonth = localStorage.getItem('kmart_month');

    const selectedYear = ref(savedYear ? parseInt(savedYear) : currentYear);
    const selectedMonth = ref(savedMonth ? parseInt(savedMonth) : currentMonth);

    // Watchers for persistence
    watch(selectedYear, (newVal: number) => {
        localStorage.setItem('kmart_year', newVal.toString());
    });

    watch(selectedMonth, (newVal: number) => {
        localStorage.setItem('kmart_month', newVal.toString());
    });

    // Actions
    function setYear(year: number) {
        selectedYear.value = year;
    }

    function setMonth(month: number) {
        selectedMonth.value = month;
    }

    return { selectedYear, selectedMonth, setYear, setMonth };
});
