import type { DataFreshness, MeasurementQuality } from '@shared/types/domain';

export function getDataFreshness(
  measuredAt: string | null | undefined,
  policy?: { freshMinutes?: number; agingMinutes?: number },
): DataFreshness {
  if (!measuredAt) return 'unknown';
  const diff = Date.now() - new Date(measuredAt).getTime();
  const freshMin = policy?.freshMinutes ?? 10;
  const agingMin = policy?.agingMinutes ?? 60;
  if (diff < freshMin * 60000) return 'fresh';
  if (diff < agingMin * 60000) return 'aging';
  return 'stale';
}

export function getFreshnessLabel(freshness: DataFreshness): string {
  const map: Record<DataFreshness, string> = {
    fresh: 'Актуально',
    aging: 'Устаревает',
    stale: 'Устарело',
    unknown: 'Неизвестно',
  };
  return map[freshness];
}

export function getFreshnessColor(freshness: DataFreshness): string {
  const map: Record<DataFreshness, string> = {
    fresh: 'green',
    aging: 'yellow',
    stale: 'red',
    unknown: 'gray',
  };
  return map[freshness];
}

export function getStatusColor(status: string): string {
  const map: Record<string, string> = {
    normal: 'green',
    warning: 'yellow',
    critical: 'red',
    offline: 'gray',
    stale: 'orange',
    unknown: 'gray',
  };
  return map[status] ?? 'gray';
}

export function getQualityColor(quality: MeasurementQuality): string {
  const map: Record<MeasurementQuality, string> = {
    valid: 'green',
    estimated: 'blue',
    stale: 'orange',
    outOfRange: 'red',
    sensorError: 'red',
    rejected: 'gray',
    unknown: 'gray',
  };
  return map[quality] ?? 'gray';
}

export function getConnectivityColor(status: string): string {
  return status === 'online' || status === 'operational' ? 'green' : 'red';
}

export function getStatusLabel(status: string): string {
  const map: Record<string, string> = {
    normal: 'Норма',
    warning: 'Внимание',
    critical: 'Критично',
    offline: 'Нет связи',
    stale: 'Устарело',
    unknown: 'Неизвестно',
  };
  return map[status] ?? status;
}

export function getDeviceStatusLabel(status: string): string {
  const map: Record<string, string> = {
    registered: 'Зарегистрировано',
    active: 'Активно',
    inactive: 'Неактивно',
    decommissioned: 'Списано',
  };
  return map[status] ?? status;
}

export function getConnectivityLabel(status: string): string {
  const map: Record<string, string> = {
    online: 'В сети',
    offline: 'Не в сети',
    suspected: 'Предположительно',
    synchronizing: 'Синхронизация',
    operational: 'Работает',
  };
  return map[status] ?? status;
}