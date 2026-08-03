import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { getStoredMode, setStoredMode, resolveScheme } from '@shared/theme/themeStorage';

describe('themeStorage', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('returns null when nothing stored', () => {
    expect(getStoredMode()).toBeNull();
  });

  it('returns stored mode', () => {
    setStoredMode('dark');
    expect(getStoredMode()).toBe('dark');
  });

  it('returns system when stored value is invalid', () => {
    localStorage.setItem('climate-hub:appearance', 'invalid');
    expect(getStoredMode()).toBeNull();
  });

  it('resolves light directly', () => {
    expect(resolveScheme('light')).toBe('light');
  });

  it('resolves dark directly', () => {
    expect(resolveScheme('dark')).toBe('dark');
  });
});