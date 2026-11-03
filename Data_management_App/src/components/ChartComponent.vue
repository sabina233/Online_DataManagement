<script setup lang="ts">
import { onMounted, ref, watch, onUnmounted } from 'vue';
import * as echarts from 'echarts';

const props = defineProps<{
    title?: string;
    xAxisData?: string[];
    seriesData?: { 
        name: string; 
        type: 'bar' | 'line'; 
        data: number[]; 
        itemStyle?: any; 
        lineStyle?: any; 
        color?: string;
    }[];
    option?: echarts.EChartsOption;
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

    if (props.option) {
        chartInstance.setOption(props.option, true);
        return;
    }

    const seriesColors = props.seriesData?.map(s => s.itemStyle?.color || s.color).filter(Boolean);

    const option: echarts.EChartsOption = {
        title: {
            text: props.title || '',
            left: 'center'
        },
        tooltip: {
            trigger: 'axis'
        },
        legend: {
            data: props.seriesData?.map(s => s.name) || [],
            bottom: 0
        },
        grid: {
            left: '3%',
            right: '4%',
            bottom: '15%',
            containLabel: true
        },
        xAxis: {
            type: 'category',
            boundaryGap: true,
            data: props.xAxisData || []
        },
        yAxis: {
            type: 'value'
        },
        ...(seriesColors && seriesColors.length > 0 ? { color: seriesColors as string[] } : {}),
        series: props.seriesData?.map(s => ({
            name: s.name,
            type: s.type,
            data: s.data,
            smooth: true,
            itemStyle: s.itemStyle || (s.type === 'bar' ? { borderRadius: [4, 4, 0, 0] } : undefined),
            lineStyle: s.lineStyle || undefined
        })) || []
    };

    chartInstance.setOption(option, true);
};

watch(() => [props.seriesData, props.title, props.option], () => {
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
