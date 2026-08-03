import { useEffect, useCallback, useSyncExternalStore } from 'react';
import { getStoredMode, setStoredMode, resolveScheme, applyTheme } from './themeStorage';
import type { AppearanceMode, ResolvedColorScheme } from './themeStorage';

let currentMode: AppearanceMode = getStoredMode() ?? 'system';
let listeners: Set<() => void> = new Set();

function notify() { listeners.forEach(l => l()); }

function subscribe(cb: () => void) {
  listeners.add(cb);
  return () => { listeners.delete(cb); };
}

function getSnapshot(): AppearanceMode { return currentMode; }

function setMode(mode: AppearanceMode) {
  currentMode = mode;
  setStoredMode(mode);
  applyTheme(resolveScheme(mode));
  notify();
}

function initTheme() {
  const mode = getStoredMode() ?? 'system';
  currentMode = mode;
  applyTheme(resolveScheme(mode));
}

initTheme();

let systemListener: ((e: MediaQueryListEvent) => void) | null = null;

function startSystemListener() {
  const mq = window.matchMedia('(prefers-color-scheme: dark)');
  systemListener = () => {
    if (currentMode === 'system') {
      applyTheme(resolveScheme('system'));
    }
  };
  mq.addEventListener('change', systemListener);
}

if (typeof window !== 'undefined') startSystemListener();

export function useAppearance() {
  const mode = useSyncExternalStore(subscribe, getSnapshot, getSnapshot);
  const resolved: ResolvedColorScheme = resolveScheme(mode);

  const setModeCallback = useCallback((newMode: AppearanceMode) => {
    setMode(newMode);
  }, []);

  const resetMode = useCallback(() => {
    setMode('system');
  }, []);

  return { mode, resolved, setMode: setModeCallback, resetMode };
}

export type { AppearanceMode, ResolvedColorScheme };