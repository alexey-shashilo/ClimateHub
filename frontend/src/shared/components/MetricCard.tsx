import { Card, Text, Group, Stack, Badge } from '@mantine/core';
import { getDataFreshness, getFreshnessLabel, getFreshnessColor, getQualityColor } from '@shared/lib/freshness';
import { formatMeasurementQuality, formatRelativeTime } from '@shared/lib/formatters';

interface MetricCardProps {
  label: string;
  value: number | null | undefined;
  unit: string;
  formatted: string;
  measuredAt?: string;
  quality?: string;
  color?: string;
  target?: { minimum?: number; maximum?: number };
}

export function MetricCard({ label, value, unit, formatted, measuredAt, quality, color, target }: MetricCardProps) {
  const freshness = getDataFreshness(measuredAt);

  return (
    <Card shadow="sm" padding="lg" radius="md" withBorder>
      <Stack gap="sm">
        <Text size="sm" tt="uppercase" fw={600} c="dimmed" style={{ letterSpacing: '0.05em' }}>
          {label}
        </Text>

        {value != null ? (
          <Text size="2.5rem" fw={700} lh={1.1} c={color}>
            {formatted}
          </Text>
        ) : (
          <Text size="2.5rem" fw={700} lh={1.1} c="dimmed">—</Text>
        )}

        {target && value != null && (
          <Text size="xs" c="dimmed">
            Цель: {target.minimum != null ? `${target.minimum}–${target.maximum} ${unit}` : `≤ ${target.maximum} ${unit}`}
          </Text>
        )}

        <Group gap="xs">
          <Badge color={getQualityColor(quality as any) || 'gray'} size="sm" variant="light">
            {formatMeasurementQuality(quality ?? 'unknown')}
          </Badge>
          <Badge color={getFreshnessColor(freshness)} size="sm" variant="light">
            {getFreshnessLabel(freshness)}
          </Badge>
        </Group>

        {measuredAt && (
          <Text size="xs" c="dimmed">Измерено {formatRelativeTime(measuredAt)}</Text>
        )}
      </Stack>
    </Card>
  );
}