<script setup lang="ts">
import { onMounted, ref, watch, onUnmounted } from 'vue';
import * as echarts from 'echarts';

const props = defineProps<{
    title: string;
    xAxisData: string[];
    seriesData: { name: string; type: 'bar' | 'line'; data: number[] }[];
}>();

const chartRef = ref<HTMLElement | null>(null);
let chartInstance: echarts.ECharts | null = null;

const initChart = () => {
    if (!chartRef.value) return;
    
    chartInstance = echarts.init(chartRef.value);
    setOptions();
};

const setOptions = () => {
    if (!chartInstance) return;

    const option = {
        title: {
            text: props.title,
            left: 'center'
        },
        tooltip: {
            trigger: 'axis'
        },
        legend: {
            data: props.seriesData.map(s => s.name),
            bottom: 0
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '10%',
            containLabel: true
        },
        xAxis: {
            type: 'category',
            boundaryGap: true, // true for bar
            data: props.xAxisData
        },
        yAxis: {
            type: 'value'
        },
        series: props.seriesData.map(s => ({
            name: s.name,
            type: s.type,
            data: s.data,
            smooth: true,
            itemStyle: s.type === 'bar' ? { borderRadius: [4, 4, 0, 0] } : undefined
        }))
    };

    chartInstance.setOption(option);
};

watch(() => [props.seriesData, props.title], () => {
    setOptions();
}, { deep: true });

onMounted(() => {
    initChart();
    window.addEventListener('resize', resizeChart);
});

onUnmounted(() => {
    window.removeEventListener('resize', resizeChart);
    chartInstance?.dispose();
});

const resizeChart = () => {
    chartInstance?.resize();
};
</script>

<template>
  <div ref="chartRef" class="chart-container"></div>
</template>

<style scoped>
.chart-container {
    width: 100%;
    height: 400px;
}
</style>
