import { Table, Badge, Group, Text, Stack, Title, Loader, Alert, Select } from '@mantine/core';
import { Link } from 'react-router-dom';
import { useNeeds } from '@features/building-navigation/hooks';

const statusColor: Record<string, string> = {
  detected: 'gray', planning: 'blue', planned: 'indigo',
  executing: 'violet', waitingForEffect: 'cyan', satisfied: 'green',
  blocked: 'red', cancelled: 'yellow', expired: 'orange',
};

const severityColor: Record<string, string> = {
  low: 'gray', medium: 'yellow', high: 'orange', critical: 'red',
};

const controlModeLabel: Record<string, string> = {
  automatic: 'Авто', manual: 'Ручной', monitorOnly: 'Мониторинг', disabled: 'Откл',
};

const failureCodeMessage: Record<string, string> = {
  CAPABLE_DEVICE_NOT_FOUND: 'Не найдено совместимое устройство',
  AMBIGUOUS_CAPABLE_DEVICE: 'Найдено несколько подходящих устройств. Требуется выбор.',
  DEVICE_OFFLINE: 'Совместимое устройство не в сети',
  ENVIRONMENT_DATA_STALE: 'Данные помещения устарели',
  NO_MEASURABLE_EFFECT: 'После воздействия не зафиксировано ожидаемое изменение среды',
  COMMAND_EXECUTION_FAILED: 'Ошибка выполнения команды',
  COMMAND_TIMED_OUT: 'Тайм-аут выполнения команды',
  CAPABILITY_PLAN_NOT_FOUND: 'Не найден план для потребности',
};

export function NeedsPage() {
  const { data: needs, isLoading } = useNeeds();

  if (isLoading) return <Loader />;

  return (
    <Stack p="md">
      <Title order={2}>Потребности (Needs)</Title>
      {!needs?.length ? (
        <Alert color="blue">Нет активных потребностей.</Alert>
      ) : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Тип</Table.Th>
              <Table.Th>Помещение</Table.Th>
              <Table.Th>Важность</Table.Th>
              <Table.Th>Статус</Table.Th>
              <Table.Th>Режим</Table.Th>
              <Table.Th>Значение</Table.Th>
              <Table.Th>Цель</Table.Th>
              <Table.Th>Отклонение</Table.Th>
              <Table.Th>Ошибка</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {needs.map((n) => (
              <Table.Tr key={n.id}>
                <Table.Td>
                  <Link to={`/needs/${n.id}`}>{n.type}</Link>
                </Table.Td>
                <Table.Td>
                  <Link to={`/rooms/${n.roomId}`}>{n.roomId.slice(0, 8)}</Link>
                </Table.Td>
                <Table.Td>
                  <Badge color={severityColor[n.severity.toLowerCase()] ?? 'gray'}>{n.severity}</Badge>
                </Table.Td>
                <Table.Td>
                  <Badge color={statusColor[n.status.toLowerCase()] ?? 'gray'}>{n.status}</Badge>
                </Table.Td>
                <Table.Td>
                  <Badge variant="light" color="blue">{controlModeLabel[n.controlMode ?? 'monitorOnly'] ?? n.controlMode}</Badge>
                </Table.Td>
                <Table.Td>{n.currentValue ?? '—'}</Table.Td>
                <Table.Td>{n.desiredMin}–{n.desiredMax}</Table.Td>
                <Table.Td>{n.deviation}</Table.Td>
                <Table.Td>
                  {n.planningFailureCode && (
                    <Badge color="red" size="sm" title={failureCodeMessage[n.planningFailureCode] ?? n.planningFailureCode}>
                      {n.planningFailureCode.slice(0, 20)}
                    </Badge>
                  )}
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}
    </Stack>
  );
}