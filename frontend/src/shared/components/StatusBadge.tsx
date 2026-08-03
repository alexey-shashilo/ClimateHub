import { Badge, Tooltip } from '@mantine/core';
import { getStatusColor, getStatusLabel } from '@shared/lib/freshness';

interface StatusBadgeProps {
  status: string;
  size?: 'xs' | 'sm' | 'md' | 'lg' | 'xl';
  variant?: 'light' | 'filled' | 'dot' | 'outline';
}

export function StatusBadge({ status, size = 'sm', variant = 'light' }: StatusBadgeProps) {
  return (
    <Tooltip label={getStatusLabel(status)}>
      <Badge color={getStatusColor(status)} size={size} variant={variant}>
        {getStatusLabel(status)}
      </Badge>
    </Tooltip>
  );
}