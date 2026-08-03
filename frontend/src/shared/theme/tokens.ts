export const spacing = {
  xs: 4,
  sm: 8,
  md: 16,
  lg: 24,
  xl: 32,
  '2xl': 48,
  '3xl': 64,
} as const;

export const radius = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 20,
} as const;

export const fontSize = {
  xs: 11,
  sm: 13,
  md: 15,
  lg: 17,
  xl: 21,
  '2xl': 25,
  '3xl': 32,
} as const;

export const fontFamily = '-apple-system, BlinkMacSystemFont, "Segoe UI", "Inter", system-ui, sans-serif';
export const fontFamilyMonospace = '"JetBrains Mono", "Fira Code", "Consolas", monospace';

export const surfaceLevel = {
  0: '--surface-0',
  1: '--surface-1',
  2: '--surface-2',
  3: '--surface-3',
} as const;