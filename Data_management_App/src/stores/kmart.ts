import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useKmartStore = defineStore('kmart', () => {
    const currentYear = new Date().getFullYear();

    // State with default values
    const selectedYear = ref(currentYear);
    const selectedMonth = ref(new Date().getMonth() + 1); // Default to current month

    // Actions (can be simple setters or direct ref modification)
    function setYear(year: number) {
        selectedYear.value = year;
    }

    function setMonth(month: number) {
        selectedMonth.value = month;
    }

    return { selectedYear, selectedMonth, setYear, setMonth };
});
