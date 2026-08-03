import { describe, it, expect } from 'vitest';
import {
  formatTemperature,
  formatRelativeHumidity,
  formatCo2,
  formatRelativeTime,
  formatMeasurementQuality,
} from '@shared/lib/formatters';

describe('formatters', () => {
  it('formatTemperature', () => {
    expect(formatTemperature(22.4)).toBe('22.4 °C');
    expect(formatTemperature(null)).toBe('—');
    expect(formatTemperature(undefined)).toBe('—');
  });

  it('formatRelativeHumidity', () => {
    expect(formatRelativeHumidity(41.7)).toBe('42 %');
    expect(formatRelativeHumidity(null)).toBe('—');
  });

  it('formatCo2', () => {
    expect(formatCo2(735)).toBe('735 ppm');
    expect(formatCo2(null)).toBe('—');
  });

  it('formatRelativeTime', () => {
    const now = new Date().toISOString();
    expect(formatRelativeTime(now)).toBe('только что');
    const fiveMinAgo = new Date(Date.now() - 300000).toISOString();
    expect(formatRelativeTime(fiveMinAgo)).toBe('5 мин. назад');
    expect(formatRelativeTime(null)).toBe('—');
    expect(formatRelativeTime(undefined)).toBe('—');
  });

  it('formatMeasurementQuality', () => {
    expect(formatMeasurementQuality('valid')).toBe('Достоверно');
    expect(formatMeasurementQuality('unknown')).toBe('Неизвестно');
    expect(formatMeasurementQuality('sensorError')).toBe('Ошибка датчика');
  });
});