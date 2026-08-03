import { useParams, Link } from 'react-router-dom';
import {
  Stack, Title, Text, Group, Badge, Card, Loader, Timeline, Table, Divider,
} from '@mantine/core';
import { useCommandPlan } from '@features/building-navigation/hooks';

const planStatusColor: Record<string, string> = {
  created: 'gray', planning: 'blue', reserved: 'cyan', ready: 'indigo',
  executing: 'violet', succeeded: 'green', failed: 'red', cancelled: 'yellow',
};

const stepStatusColor: Record<string, string> = {
  pending: 'gray', executing: 'violet', succeeded: 'green', failed: 'red', skipped: 'yellow',
};

function formatDate(d: string | undefined | null): string {
  if (!d) return '—';
  return new Date(d).toLocaleString();
}

export function CommandPlanDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: plan, isLoading } = useCommandPlan(id);

  if (isLoading) return <Loader />;
  if (!plan) return <Text>План не найден.</Text>;

  const phaseOrder = ['created', 'planning', 'reserved', 'ready', 'executing'];
  const currentPhaseIndex = phaseOrder.indexOf(plan.status?.toLowerCase());
  const isTerminal = ['succeeded', 'failed', 'cancelled'].includes(plan.status?.toLowerCase());

  return (
    <Stack p="md">
      <Group>
        <Title order={2}>План команд: {plan.id}</Title>
        <Badge color={planStatusColor[plan.status?.toLowerCase()] ?? 'gray'} size="lg">{plan.status}</Badge>
      </Group>

      <Card withBorder>
        <Title order={4}>Обзор</Title>
        <Stack gap="sm" mt="sm">
          <Group justify="space-between">
            <Text fw={500}>Инженерная система</Text>
            <Link to={`/engineering-systems/${plan.engineeringSystemId}`}>{plan.engineeringSystemId}</Link>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Тип потребности</Text>
            <Text>{plan.needType ?? '—'}</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Способность</Text>
            <Text>{plan.capabilityCode ?? '—'}</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Запрошенное значение</Text>
            <Text>{plan.requestedValue ?? '—'} {plan.valueUnit ?? ''}</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Стратегия</Text>
            <Text>{plan.strategyName ?? '—'}</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Создан</Text>
            <Text>{formatDate(plan.createdAt)}</Text>
          </Group>
          {plan.completedAt && (
            <Group justify="space-between">
              <Text fw={500}>Завершён</Text>
              <Text>{formatDate(plan.completedAt)}</Text>
            </Group>
          )}
          {plan.failureCode && (
            <Group justify="space-between">
              <Text fw={500}>Код ошибки</Text>
              <Badge color="red">{plan.failureCode}</Badge>
            </Group>
          )}
        </Stack>
      </Card>

      {/* Lifecycle Timeline */}
      <Card withBorder>
        <Title order={4}>Жизненный цикл</Title>
        <Timeline active={isTerminal ? phaseOrder.length : Math.max(0, currentPhaseIndex)} bulletSize={20} lineWidth={2} mt="md">
          <Timeline.Item title="Создан" color={currentPhaseIndex >= 0 ? 'gray' : 'gray'} />
          <Timeline.Item title="Планирование" color={currentPhaseIndex >= 1 ? 'blue' : 'gray'} />
          <Timeline.Item title="Резервирование" color={currentPhaseIndex >= 2 ? 'cyan' : 'gray'} />
          <Timeline.Item title="Готов" color={currentPhaseIndex >= 3 ? 'indigo' : 'gray'} />
          <Timeline.Item title="Выполнение" color={currentPhaseIndex >= 4 ? 'violet' : 'gray'} />
          {plan.steps?.map((step: any, idx: number) => (
            <Timeline.Item
              key={idx}
              title={
                <Group gap="xs">
                  <Text fw={500}>Шаг {step.sequence}: {step.operation}</Text>
                  <Badge color={stepStatusColor[step.status?.toLowerCase()] ?? 'gray'} size="sm">{step.status}</Badge>
                </Group>
              }
              color={step.status === 'succeeded' ? 'green' : step.status === 'failed' ? 'red' : 'violet'}
            >
              <Text size="sm">{step.capabilityCode} → {step.requestedValue}{step.valueUnit ? ` ${step.valueUnit}` : ''}</Text>
              {step.deviceId && <Text size="xs" c="dimmed">Устройство: {step.deviceId}</Text>}
              {step.deviceRole && <Text size="xs" c="dimmed">Роль: {step.deviceRole}</Text>}
            </Timeline.Item>
          ))}
          {isTerminal && (
            <Timeline.Item
              title={plan.status === 'succeeded' ? 'Успешно' : plan.status === 'failed' ? 'Ошибка' : 'Отменён'}
              color={plan.status === 'succeeded' ? 'green' : 'red'}
            />
          )}
        </Timeline>
      </Card>

      {/* Resource Allocations */}
      {plan.resourceAllocations?.length > 0 && (
        <Card withBorder>
          <Title order={4}>Выделение ресурсов</Title>
          <Table striped mt="sm">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Ресурс</Table.Th>
                <Table.Th>Количество</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {plan.resourceAllocations.map((ra: any) => (
                <Table.Tr key={ra.resourceCode}>
                  <Table.Td>{ra.resourceCode}</Table.Td>
                  <Table.Td>{ra.amount}</Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Card>
      )}

      {/* Steps Table */}
      {plan.steps?.length > 0 && (
        <Card withBorder>
          <Title order={4}>Шаги выполнения</Title>
          <Table striped mt="sm">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>№</Table.Th>
                <Table.Th>Операция</Table.Th>
                <Table.Th>Способность</Table.Th>
                <Table.Th>Значение</Table.Th>
                <Table.Th>Устройство</Table.Th>
                <Table.Th>Роль</Table.Th>
                <Table.Th>Статус</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {plan.steps.map((step: any) => (
                <Table.Tr key={step.sequence}>
                  <Table.Td>{step.sequence}</Table.Td>
                  <Table.Td>{step.operation}</Table.Td>
                  <Table.Td>{step.capabilityCode}</Table.Td>
                  <Table.Td>{step.requestedValue}{step.valueUnit ? ` ${step.valueUnit}` : ''}</Table.Td>
                  <Table.Td>{step.deviceId ?? '—'}</Table.Td>
                  <Table.Td>{step.deviceRole ?? '—'}</Table.Td>
                  <Table.Td>
                    <Badge color={stepStatusColor[step.status?.toLowerCase()] ?? 'gray'} size="sm">{step.status}</Badge>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Card>
      )}

      <Divider />
      <Text size="xs" c="dimmed">Версия: {plan.version}</Text>
    </Stack>
  );
}