import { Title, Text, Stack } from '@mantine/core';
import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <Stack align="center" mt={100} gap="md">
      <Title order={1}>404</Title>
      <Text size="lg" c="dimmed">Страница не найдена</Text>
      <Text component={Link} to="/" c="blue">Вернуться на главную</Text>
    </Stack>
  );
}