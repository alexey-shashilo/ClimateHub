import { describe, it, expect } from 'vitest';
import { getCapabilityUi, hasMeasurementCapability } from '@shared/lib/capability-resolver';
import type { DeviceCapability } from '@shared/types/domain';

describe('capability-resolver', () => {
  it('returns known capability UI', () => {
    const ui = getCapabilityUi('measure.temperature');
    expect(ui.label).toBe('Температура');
    expect(ui.unit).toBe('°C');
  });

  it('returns fallback for unknown capability', () => {
    const ui = getCapabilityUi('unknown.code');
    expect(ui.label).toBe('unknown.code');
  });

  it('checks measurement capability', () => {
    const caps: DeviceCapability[] = [
      { code: 'measure.temperature', kind: 'measurement', writable: false, available: true },
      { code: 'control.relay', kind: 'control', writable: true, available: true },
    ];
    expect(hasMeasurementCapability(caps)).toBe(true);

    const noMeas: DeviceCapability[] = [
      { code: 'control.relay', kind: 'control', writable: true, available: true },
    ];
    expect(hasMeasurementCapability(noMeas)).toBe(false);
  });
});