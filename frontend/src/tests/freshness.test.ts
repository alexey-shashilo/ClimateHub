import { describe, it, expect } from 'vitest';
import { getDataFreshness } from '@shared/lib/freshness';

describe('getDataFreshness', () => {
  it('returns fresh for recent measurement', () => {
    const recent = new Date(Date.now() - 60000).toISOString();
    expect(getDataFreshness(recent)).toBe('fresh');
  });

  it('returns aging for 30 min old', () => {
    const old = new Date(Date.now() - 1800000).toISOString();
    expect(getDataFreshness(old)).toBe('aging');
  });

  it('returns stale for 2 hours old', () => {
    const stale = new Date(Date.now() - 7200000).toISOString();
    expect(getDataFreshness(stale)).toBe('stale');
  });

  it('returns unknown for null', () => {
    expect(getDataFreshness(null)).toBe('unknown');
  });
});