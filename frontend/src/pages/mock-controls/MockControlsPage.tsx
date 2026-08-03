import { Title, Text, Stack, Card, Button, Group, Badge } from '@mantine/core';
import { useNavigate } from 'react-router-dom';

export function MockControlsPage() {
  const navigate = useNavigate();

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <Title order={3}>Mock Controls</Title>
        <Badge color="yellow" variant="light">Development Only</Badge>
      </Group>
      <Text c="dimmed" size="sm">
        Панель для переключения сценариев состояния помещений в development mode.
      </Text>
      <Text c="dimmed" size="xs">
        Сценарии переключаются через mock database. Для полной функциональности требуется доработка MSW handlers для state overrides.
      </Text>
      <Button variant="outline" onClick={() => navigate('/buildings/building-001')}>
        К обзору здания
      </Button>
    </Stack>
  );
}