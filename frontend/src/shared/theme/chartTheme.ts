import type { ResolvedColorScheme } from './useAppearance';

export interface ChartThemeColors {
  text: string;
  grid: string;
  tooltipBg: string;
  tooltipBorder: string;
  axisLabel: string;
  temperature: string;
  humidity: string;
  co2: string;
  targetBand: string;
  warningBand: string;
}

const light: ChartThemeColors = {
  text: '#1a1b1e',
  grid: '#dee2e6',
  tooltipBg: '#ffffff',
  tooltipBorder: '#dee2e6',
  axisLabel: '#868e96',
  temperature: '#ff9800',
  humidity: '#00bcd4',
  co2: '#9c27b0',
  targetBand: 'rgba(47,154,114,0.12)',
  warningBand: 'rgba(255,193,7,0.12)',
};

const dark: ChartThemeColors = {
  text: '#c1c2c5',
  grid: '#373a40',
  tooltipBg: '#25262b',
  tooltipBorder: '#373a40',
  axisLabel: '#909296',
  temperature: '#ffa726',
  humidity: '#26c6da',
  co2: '#ab47bc',
  targetBand: 'rgba(47,154,114,0.18)',
  warningBand: 'rgba(255,193,7,0.18)',
};

export function getChartTheme(scheme: ResolvedColorScheme): ChartThemeColors {
  return scheme === 'dark' ? dark : light;
}

export function echartTheme(scheme: ResolvedColorScheme) {
  const c = getChartTheme(scheme);
  return {
    backgroundColor: 'transparent',
    textStyle: { color: c.text, fontFamily: '-apple-system, BlinkMacSystemFont, sans-serif', fontSize: 12 },
    grid: { borderColor: c.grid },
    tooltip: {
      backgroundColor: c.tooltipBg,
      borderColor: c.tooltipBorder,
    },
    xAxis: {
      axisLabel: { color: c.axisLabel },
      axisLine: { lineStyle: { color: c.grid } },
      splitLine: { lineStyle: { color: c.grid } },
    },
    yAxis: {
      axisLabel: { color: c.axisLabel },
      axisLine: { lineStyle: { color: c.grid } },
      splitLine: { lineStyle: { color: c.grid } },
    },
  };
}