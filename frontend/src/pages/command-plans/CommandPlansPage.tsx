import { useState } from 'react';
import { Table, Badge, Group, Text, Stack, Title, Loader, Alert, Select } from '@mantine/core';
import { Link } from 'react-router-dom';
import { useCommandPlans } from '@features/building-navigation/hooks';

const planStatusColor: Record<string, string> = {
  created: 'gray', planning: 'blue', reserved: 'cyan', ready: 'indigo',
  executing: 'violet', succeeded: 'green', failed: 'red', cancelled: 'yellow',
};

function formatDate(d: string | undefined | null): string {
  if (!d) return '—';
  return new Date(d).toLocaleString();
}

export function CommandPlansPage() {
  const { data: plans, isLoading } = useCommandPlans();
  const [statusFilter, setStatusFilter] = useState<string | null>(null);

  if (isLoading) return <Loader />;

  const statuses = [...new Set(plans?.map(p => p.status) ?? [])];
  const filtered = statusFilter
    ? plans?.filter(p => p.status === statusFilter)
    : plans;

  return (
    <Stack p="md">
      <Group justify="space-between">
        <Title order={2}>Планы команд</Title>
        {statuses.length > 0 && (
          <Select
            placeholder="Фильтр по статусу"
            data={['', ...statuses]}
            value={statusFilter}
            onChange={setStatusFilter}
            clearable
            w={200}
          />
        )}
      </Group>
      {!filtered?.length ? (
        <Alert color="blue">Нет планов команд.</Alert>
      ) : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>ID</Table.Th>
              <Table.Th>Статус</Table.Th>
              <Table.Th>Инж. система</Table.Th>
              <Table.Th>Потребность</Table.Th>
              <Table.Th>Способность</Table.Th>
              <Table.Th>Значение</Table.Th>
              <Table.Th>Шаги</Table.Th>
              <Table.Th>Создан</Table.Th>
              <Table.Th>Завершён</Table.Th>
              <Table.Th>Ошибка</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {filtered.map((p) => (
              <Table.Tr key={p.id}>
                <Table.Td>
                  <Link to={`/command-plans/${p.id}`}>{p.id}</Link>
                </Table.Td>
                <Table.Td>
                  <Badge color={planStatusColor[p.status?.toLowerCase()] ?? 'gray'}>{p.status}</Badge>
                </Table.Td>
                <Table.Td>{p.engineeringSystemId?.slice(0, 12) ?? '—'}</Table.Td>
                <Table.Td>{p.needType ?? '—'}</Table.Td>
                <Table.Td>{p.capabilityCode ?? '—'}</Table.Td>
                <Table.Td>{p.requestedValue ?? '—'}{p.valueUnit ? ` ${p.valueUnit}` : ''}</Table.Td>
                <Table.Td>{p.steps?.length ?? 0}</Table.Td>
                <Table.Td>{formatDate(p.createdAt)}</Table.Td>
                <Table.Td>{formatDate(p.completedAt)}</Table.Td>
                <Table.Td>
                  {p.failureCode && <Badge color="red" size="sm">{p.failureCode}</Badge>}
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}
    </Stack>
  );
}