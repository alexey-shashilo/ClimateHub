export function formatTemperature(value: number | null | undefined): string {
  return value != null ? `${value.toFixed(1)} °C` : '—';
}

export function formatRelativeHumidity(value: number | null | undefined): string {
  return value != null ? `${Math.round(value)} %` : '—';
}

export function formatCo2(value: number | null | undefined): string {
  return value != null ? `${Math.round(value)} ppm` : '—';
}

export function formatIlluminance(value: number | null | undefined): string {
  return value != null ? `${Math.round(value)} lx` : '—';
}

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleString('ru-RU');
}

export function formatRelativeTime(iso: string | null | undefined): string {
  if (!iso) return '—';
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'только что';
  if (mins < 60) return `${mins} мин. назад`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours} ч. назад`;
  const days = Math.floor(hours / 24);
  return `${days} дн. назад`;
}

export function formatDeviceStatus(status: string): string {
  const map: Record<string, string> = {
    registered: 'Зарегистрировано',
    active: 'Активно',
    inactive: 'Неактивно',
    decommissioned: 'Списано',
    online: 'В сети',
    offline: 'Не в сети',
  };
  return map[status] ?? status;
}

export function formatMeasurementQuality(quality: string): string {
  const map: Record<string, string> = {
    valid: 'Достоверно',
    estimated: 'Расчётное',
    stale: 'Устарело',
    outOfRange: 'Вне диапазона',
    sensorError: 'Ошибка датчика',
    rejected: 'Отклонено',
    unknown: 'Неизвестно',
  };
  return map[quality] ?? quality;
}

export function formatTemperatureShort(value: number | null | undefined): string {
  return value != null ? `${value.toFixed(1)}°` : '—';
}