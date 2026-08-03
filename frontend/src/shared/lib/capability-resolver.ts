import type { DeviceCapability } from '@shared/types/domain';

export interface CapabilityUiInfo {
  code: string;
  label: string;
  unit: string;
  icon: string;
  kind: string;
}

const capabilityRegistry: Record<string, CapabilityUiInfo> = {
  'measure.temperature': { code: 'measure.temperature', label: 'Температура', unit: '°C', icon: '🌡', kind: 'measurement' },
  'measure.relative-humidity': { code: 'measure.relative-humidity', label: 'Влажность', unit: '%', icon: '💧', kind: 'measurement' },
  'measure.co2': { code: 'measure.co2', label: 'CO₂', unit: 'ppm', icon: '🌬', kind: 'measurement' },
  'measure.illuminance': { code: 'measure.illuminance', label: 'Освещённость', unit: 'lx', icon: '☀️', kind: 'measurement' },
  'control.fan-speed': { code: 'control.fan-speed', label: 'Вентилятор', unit: 'об/мин', icon: '🌀', kind: 'control' },
  'control.damper-position': { code: 'control.damper-position', label: 'Клапан', unit: '%', icon: '🚪', kind: 'control' },
  'control.relay': { code: 'control.relay', label: 'Реле', unit: '', icon: '🔌', kind: 'control' },
  'control.humidifier': { code: 'control.humidifier', label: 'Увлажнение', unit: '%', icon: '💨', kind: 'control' },
  'firmware.ota': { code: 'firmware.ota', label: 'OTA', unit: '', icon: '📡', kind: 'system' },
};

export function getCapabilityUi(code: string): CapabilityUiInfo {
  return capabilityRegistry[code] ?? { code, label: code, unit: '', icon: '📦', kind: 'unknown' };
}

export function hasMeasurementCapability(capabilities: DeviceCapability[]): boolean {
  return capabilities.some(c => c.kind === 'measurement' && c.available);
}

export function getMeasurementCapabilities(capabilities: DeviceCapability[]): DeviceCapability[] {
  return capabilities.filter(c => c.kind === 'measurement');
}