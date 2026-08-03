import { useParams, useNavigate, Link } from 'react-router-dom';
import { useState } from 'react';
import {
  Title, Text, Group, Badge, Loader, Center, Alert, Stack, TextInput, Button, Table, ActionIcon, Tooltip,
} from '@mantine/core';
import { IconTrash, IconPencil } from '@tabler/icons-react';
import { useDevices, useRoomDevices, useDeleteDevice } from '@features/building-navigation/hooks';
import { formatRelativeTime } from '@shared/lib/formatters';
import { getConnectivityColor, getConnectivityLabel, getDeviceStatusLabel } from '@shared/lib/freshness';
import { getCapabilityUi } from '@shared/lib/capability-resolver';

export function DevicesPage() {
  const { roomId } = useParams<{ roomId: string }>();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const deleteDevice = useDeleteDevice();

  const devicesFromRoom = useRoomDevices(roomId);
  const devicesAll = useDevices();
  const { data: devices, isLoading, error } = roomId ? devicesFromRoom : devicesAll;

  const filtered = (devices ?? []).filter(d => {
    if (!search) return true;
    const q = search.toLowerCase();
    return d.name.toLowerCase().includes(q) || d.hardwareId.toLowerCase().includes(q) || d.model.toLowerCase().includes(q);
  });

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <Title order={3}>{roomId ? 'Устройства помещения' : 'Реестр устройств'}</Title>
        <Group>
          <Button component={Link} to="/devices/register">+ Регистрация</Button>
          {roomId && <Button variant="subtle" component={Link} to="/devices">Все устройства</Button>}
        </Group>
      </Group>

      <TextInput placeholder="Поиск..." value={search} onChange={(e) => setSearch(e.currentTarget.value)} />

      {isLoading && <Center h={200}><Loader /></Center>}
      {error && <Alert color="red" title="Ошибка">Не удалось загрузить устройства</Alert>}

      {filtered.length === 0 && !isLoading && (
        <Alert color="gray" title="Нет устройств">
          {search ? 'Ничего не найдено' : roomId ? 'В этом помещении нет устройств' : 'Устройства не зарегистрированы'}
        </Alert>
      )}

      <Table.ScrollContainer minWidth={600}>
        <Table striped highlightOnHover withTableBorder>
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Имя</Table.Th>
              <Table.Th>Hardware ID</Table.Th>
              <Table.Th>Модель</Table.Th>
              <Table.Th>Статус</Table.Th>
              <Table.Th>Подкл.</Table.Th>
              <Table.Th>Last Seen</Table.Th>
              <Table.Th></Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {filtered.map(d => (
              <Table.Tr key={d.id} style={{ cursor: 'pointer' }} onClick={() => navigate(`/devices/${d.id}`)}>
                <Table.Td fw={500}>{d.name}</Table.Td>
                <Table.Td><Text size="sm" ff="monospace">{d.hardwareId}</Text></Table.Td>
                <Table.Td><Text size="sm">{d.model}</Text></Table.Td>
                <Table.Td><Badge color={d.status === 'active' ? 'green' : 'gray'} size="sm" variant="light">{getDeviceStatusLabel(d.status)}</Badge></Table.Td>
                <Table.Td><Badge color={getConnectivityColor(d.connectivity)} size="sm" variant="dot">{getConnectivityLabel(d.connectivity)}</Badge></Table.Td>
                <Table.Td><Text size="xs" c="dimmed">{formatRelativeTime(d.lastSeenAt)}</Text></Table.Td>
                <Table.Td>
                  <Tooltip label="Удалить">
                    <ActionIcon variant="subtle" color="red" size="sm" onClick={(e) => { e.stopPropagation(); if (confirm('Удалить устройство?')) deleteDevice.mutate(d.id); }}>
                      <IconTrash size={14} />
                    </ActionIcon>
                  </Tooltip>
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Table.ScrollContainer>
    </Stack>
  );
}