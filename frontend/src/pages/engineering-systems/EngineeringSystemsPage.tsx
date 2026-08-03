import { Table, Badge, Group, Text, Stack, Title, Loader, Alert } from '@mantine/core';
import { Link } from 'react-router-dom';
import { useEngineeringSystems } from '@features/building-navigation/hooks';

const lifecycleColor: Record<string, string> = {
  active: 'green', commissioning: 'blue', decommissioning: 'orange',
  maintenance: 'yellow', idle: 'gray', offline: 'red',
};

const statusColor: Record<string, string> = {
  normal: 'green', warning: 'yellow', critical: 'red', offline: 'gray',
};

const controlModeLabel: Record<string, string> = {
  automatic: 'Авто', manual: 'Ручной', monitorOnly: 'Мониторинг', disabled: 'Откл',
};

export function EngineeringSystemsPage() {
  const { data: systems, isLoading } = useEngineeringSystems();

  if (isLoading) return <Loader />;

  return (
    <Stack p="md">
      <Title order={2}>Инженерные системы</Title>
      {!systems?.length ? (
        <Alert color="blue">Нет инженерных систем.</Alert>
      ) : (
        <Table striped highlightOnHover>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Название</Table.Th>
              <Table.Th>Тип</Table.Th>
              <Table.Th>Жизненный цикл</Table.Th>
              <Table.Th>Статус</Table.Th>
              <Table.Th>Режим</Table.Th>
              <Table.Th>Способности</Table.Th>
              <Table.Th>Устройства</Table.Th>
              <Table.Th>Ресурсы</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {systems.map((s) => (
              <Table.Tr key={s.id}>
                <Table.Td>
                  <Link to={`/engineering-systems/${s.id}`}>{s.name}</Link>
                </Table.Td>
                <Table.Td>{s.systemType}</Table.Td>
                <Table.Td>
                  <Badge color={lifecycleColor[s.lifecycle?.toLowerCase()] ?? 'gray'}>{s.lifecycle}</Badge>
                </Table.Td>
                <Table.Td>
                  <Badge color={statusColor[s.operationalStatus?.toLowerCase()] ?? 'gray'} variant="dot">{s.operationalStatus}</Badge>
                </Table.Td>
                <Table.Td>
                  <Badge variant="light" color="blue">{controlModeLabel[s.controlMode] ?? s.controlMode}</Badge>
                </Table.Td>
                <Table.Td>{s.capabilities?.length ?? 0}</Table.Td>
                <Table.Td>{s.deviceCount ?? 0}</Table.Td>
                <Table.Td>{s.resources?.length ?? 0}</Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      )}
    </Stack>
  );
}