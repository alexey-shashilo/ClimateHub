import { useParams } from 'react-router-dom';
import { useState } from 'react';
import {
  Title, Text, Group, SegmentedControl, Loader, Center, Alert, Stack, Badge,
  Paper,
} from '@mantine/core';
import { useRoomHistory } from '@features/building-navigation/hooks';
import { formatDateTime } from '@shared/lib/formatters';
import { useAppearance } from '@shared/theme/useAppearance';
import { getChartTheme } from '@shared/theme/chartTheme';
import type { EnvironmentParameterCode } from '@shared/types/domain';
import ReactEChartsCore from 'echarts-for-react/lib/core';
import * as echarts from 'echarts/core';
import { LineChart } from 'echarts/charts';
import { GridComponent, TooltipComponent, LegendComponent, DataZoomComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';

echarts.use([LineChart, GridComponent, TooltipComponent, LegendComponent, DataZoomComponent, CanvasRenderer]);

const PRESETS = [
  { label: '1 ч', value: '1h' },
  { label: '24 ч', value: '24h' },
  { label: '7 д', value: '7d' },
];

function getPresetRange(preset: string): { from: Date; to: Date } {
  const to = new Date();
  const from = new Date();
  if (preset === '1h') from.setHours(to.getHours() - 1);
  else if (preset === '24h') from.setDate(to.getDate() - 1);
  else from.setDate(to.getDate() - 7);
  return { from, to };
}

export function EnvironmentHistoryPage() {
  const { roomId } = useParams<{ roomId: string }>();
  const [parameter, setParameter] = useState<EnvironmentParameterCode>('temperature');
  const [preset, setPreset] = useState('24h');
  const { resolved } = useAppearance();
  const chartTheme = getChartTheme(resolved);

  const { from, to } = getPresetRange(preset);
  const { data, isLoading, error } = useRoomHistory(roomId, from.toISOString(), to.toISOString(), parameter);

  const seriesColor = parameter === 'temperature' ? chartTheme.temperature
    : parameter === 'humidity' ? chartTheme.humidity : chartTheme.co2;
  const unitMap: Record<string, string> = { temperature: '°C', humidity: '%', co2: 'ppm', illuminance: 'lx' };
  const labelMap: Record<string, string> = { temperature: 'Температура', humidity: 'Влажность', co2: 'CO₂', illuminance: 'Освещённость' };

  const option = data?.points ? {
    backgroundColor: 'transparent',
    textStyle: { color: chartTheme.text, fontFamily: '-apple-system, sans-serif', fontSize: 12 },
    grid: { left: 60, right: 20, top: 20, bottom: 50 },
    xAxis: {
      type: 'time' as const,
      axisLabel: { color: chartTheme.axisLabel, fontSize: 11 },
      axisLine: { lineStyle: { color: chartTheme.grid } },
      splitLine: { lineStyle: { color: chartTheme.grid } },
    },
    yAxis: {
      type: 'value' as const,
      name: unitMap[parameter] ?? '',
      nameTextStyle: { color: chartTheme.axisLabel, fontSize: 11 },
      axisLabel: { color: chartTheme.axisLabel },
      splitLine: { lineStyle: { color: chartTheme.grid } },
    },
    tooltip: {
      trigger: 'axis' as const,
      backgroundColor: chartTheme.tooltipBg,
      borderColor: chartTheme.tooltipBorder,
      formatter: (params: any) => {
        const p = params[0];
        if (!p) return '';
        const ts = new Date(p.data[0]).toLocaleString('ru-RU');
        return `${ts}<br/>${labelMap[parameter]}: ${p.data[1].toFixed(1)} ${unitMap[parameter]}`;
      },
    },
    dataZoom: [
      { type: 'inside' as const },
      { type: 'slider' as const, bottom: 10, backgroundColor: 'transparent', borderColor: chartTheme.grid },
    ],
    series: [{
      type: 'line' as const,
      data: data.points.map(p => [new Date(p.timestamp).getTime(), p.value]),
      smooth: true,
      showSymbol: false,
      lineStyle: { width: 2, color: seriesColor },
      areaStyle: { color: seriesColor, opacity: 0.08 },
      itemStyle: { color: seriesColor },
    }],
  } : null;

  return (
    <Stack gap="md">
      <Title order={3}>История параметров</Title>

      <Group>
        <SegmentedControl
          value={parameter}
          onChange={(v) => setParameter(v as EnvironmentParameterCode)}
          data={[
            { label: 'Температура', value: 'temperature' },
            { label: 'Влажность', value: 'humidity' },
            { label: 'CO₂', value: 'co2' },
            { label: 'Освещённость', value: 'illuminance' },
          ]}
        />
        <SegmentedControl
          value={preset}
          onChange={setPreset}
          data={PRESETS}
        />
      </Group>

      {isLoading && <Center h={300}><Loader /></Center>}
      {error && <Alert color="red" title="Ошибка">Не удалось загрузить историю</Alert>}

      {data && option && (
        <Paper shadow="sm" p="md" radius="md" withBorder>
          <ReactEChartsCore
            echarts={echarts}
            option={option}
            style={{ height: 400 }}
            notMerge
          />
          <Group justify="space-between" mt="sm">
            <Text size="xs" c="dimmed">
              {data.points.length} точек · {formatDateTime(data.from)} – {formatDateTime(data.to)}
            </Text>
            <Badge size="sm" variant="light">{data.aggregation}</Badge>
          </Group>
        </Paper>
      )}

      {data && !data.points.length && !isLoading && (
        <Alert color="gray" title="Нет данных">За выбранный период данные отсутствуют</Alert>
      )}
    </Stack>
  );
}