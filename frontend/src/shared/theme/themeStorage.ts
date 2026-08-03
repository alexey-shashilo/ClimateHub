type AppearanceMode = 'light' | 'dark' | 'system';
type ResolvedColorScheme = 'light' | 'dark';

const STORAGE_KEY = 'climate-hub:appearance';

function getStoredMode(): AppearanceMode | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'system') return stored;
  } catch { /* localStorage unavailable */ }
  return null;
}

function setStoredMode(mode: AppearanceMode): void {
  try {
    localStorage.setItem(STORAGE_KEY, mode);
  } catch { /* empty */ }
}

function getSystemScheme(): ResolvedColorScheme {
  if (typeof window === 'undefined') return 'light';
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

function resolveScheme(mode: AppearanceMode): ResolvedColorScheme {
  if (mode === 'system') return getSystemScheme();
  return mode;
}

function applyTheme(scheme: ResolvedColorScheme): void {
  document.documentElement.setAttribute('data-mantine-color-scheme', scheme);
  document.documentElement.style.colorScheme = scheme;
  const meta = document.querySelector('meta[name="theme-color"]');
  if (meta) meta.setAttribute('content', scheme === 'dark' ? '#1a1b1e' : '#ffffff');
}

export { getStoredMode, setStoredMode, getSystemScheme, resolveScheme, applyTheme };
export type { AppearanceMode, ResolvedColorScheme };