import { describe, it, expect } from 'vitest';
import { registerDeviceRequestSchema } from '@shared/contracts/schemas';

describe('registerDeviceRequestSchema', () => {
  it('accepts valid request', () => {
    const result = registerDeviceRequestSchema.safeParse({
      hardwareId: 'ESP32-001',
      name: 'Датчик',
      model: 'ClimateNode',
      deviceType: 'sensor',
      protocolVersion: '1.0',
      capabilityCodes: ['measure.temperature'],
    });
    expect(result.success).toBe(true);
  });

  it('rejects missing hardwareId', () => {
    const result = registerDeviceRequestSchema.safeParse({
      name: 'Датчик',
      model: 'ClimateNode',
      deviceType: 'sensor',
      protocolVersion: '1.0',
      capabilityCodes: ['measure.temperature'],
    });
    expect(result.success).toBe(false);
  });

  it('rejects invalid protocol version', () => {
    const result = registerDeviceRequestSchema.safeParse({
      hardwareId: 'ESP32-001',
      name: 'Датчик',
      model: 'ClimateNode',
      deviceType: 'sensor',
      protocolVersion: 'abc',
      capabilityCodes: ['measure.temperature'],
    });
    expect(result.success).toBe(false);
  });

  it('rejects empty capabilities', () => {
    const result = registerDeviceRequestSchema.safeParse({
      hardwareId: 'ESP32-001',
      name: 'Датчик',
      model: 'ClimateNode',
      deviceType: 'sensor',
      protocolVersion: '1.0',
      capabilityCodes: [],
    });
    expect(result.success).toBe(false);
  });
});