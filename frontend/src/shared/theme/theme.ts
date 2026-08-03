import type { MantineThemeOverride, MantineSize } from '@mantine/core';

const climate: [string, string, string, string, string, string, string, string, string, string] = [
  '#e7f5f0', '#cce8dd', '#a0d4c0', '#6bbfa0', '#45ad86',
  '#2f9a72', '#21885f', '#1a7350', '#155f42', '#0f4d35',
];

export const themeColors: Record<string, [string, string, string, string, string, string, string, string, string, string]> = {
  climate,
  statusNormal: ['#e8f5e9', '#c8e6c9', '#a5d6a7', '#81c784', '#66bb6a', '#4caf50', '#43a047', '#388e3c', '#2e7d32', '#1b5e20'],
  statusWarning: ['#fff8e1', '#ffecb3', '#ffe082', '#ffd54f', '#ffca28', '#ffc107', '#ffb300', '#ffa000', '#ff8f00', '#ff6f00'],
  statusCritical: ['#ffebee', '#ffcdd2', '#ef9a9a', '#e57373', '#ef5350', '#f44336', '#e53935', '#d32f2f', '#c62828', '#b71c1c'],
  statusOffline: ['#f5f5f5', '#e0e0e0', '#bdbdbd', '#9e9e9e', '#757575', '#616161', '#424242', '#303030', '#212121', '#000000'],
  statusStale: ['#fff3e0', '#ffe0b2', '#ffcc80', '#ffb74d', '#ffa726', '#ff9800', '#fb8c00', '#f57c00', '#ef6c00', '#e65100'],
  statusInformation: ['#e3f2fd', '#bbdefb', '#90caf9', '#64b5f6', '#42a5f5', '#2196f3', '#1e88e5', '#1976d2', '#1565c0', '#0d47a1'],
  chartTemperature: ['#fff3e0', '#ffe0b2', '#ffcc80', '#ffb74d', '#ffa726', '#ff9800', '#fb8c00', '#f57c00', '#ef6c00', '#e65100'],
  chartHumidity: ['#e0f7fa', '#b2ebf2', '#80deea', '#4dd0e1', '#26c6da', '#00bcd4', '#00acc1', '#0097a7', '#00838f', '#006064'],
  chartCo2: ['#f3e5f5', '#e1bee7', '#ce93d8', '#ba68c8', '#ab47bc', '#9c27b0', '#8e24aa', '#7b1fa2', '#6a1b9a', '#4a148c'],
};

export const theme: MantineThemeOverride = {
  primaryColor: 'climate',
  colors: themeColors,
  fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", "Inter", system-ui, sans-serif',
  fontFamilyMonospace: '"JetBrains Mono", "Fira Code", "Consolas", monospace',
  headings: {
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", "Inter", system-ui, sans-serif',
    sizes: {
      h1: { fontSize: '1.75rem', fontWeight: '700', lineHeight: '1.3' },
      h2: { fontSize: '1.5rem', fontWeight: '600', lineHeight: '1.35' },
      h3: { fontSize: '1.25rem', fontWeight: '600', lineHeight: '1.4' },
      h4: { fontSize: '1.1rem', fontWeight: '600', lineHeight: '1.45' },
      h5: { fontSize: '1rem', fontWeight: '600', lineHeight: '1.5' },
      h6: { fontSize: '0.875rem', fontWeight: '600', lineHeight: '1.55' },
    },
  },
  components: {
    Card: {
      defaultProps: {
        shadow: 'sm',
        padding: 'md',
        radius: 'md',
        withBorder: true,
      },
    },
    Table: {
      defaultProps: {
        striped: true,
        highlightOnHover: true,
        withTableBorder: true,
      },
    },
    Badge: {
      defaultProps: {
        size: 'sm',
      },
    },
    Tooltip: {
      defaultProps: {
        openDelay: 300,
      },
    },
  },
  other: {
    metricFontSize: '2rem',
    metricLineHeight: 1.1,
  },
};