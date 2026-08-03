import { useParams, Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import {
  Title, Text, Group, Card, Badge, Loader, Center, Alert, Stack, SimpleGrid, Table, Button, Modal, TextInput, ActionIcon, Tooltip,
} from '@mantine/core';
import { IconTrash, IconPencil } from '@tabler/icons-react';
import { useDevice, useDeleteDevice, useUpdateDevice } from '@features/building-navigation/hooks';
import { formatRelativeTime, formatDeviceStatus } from '@shared/lib/formatters';
import { getConnectivityColor, getConnectivityLabel } from '@shared/lib/freshness';
import { getCapabilityUi } from '@shared/lib/capability-resolver';

export function DeviceDetailPage() {
  const { deviceId } = useParams<{ deviceId: string }>();
  const navigate = useNavigate();
  const { data: device, isLoading, error } = useDevice(deviceId);
  const deleteDevice = useDeleteDevice();
  const updateDevice = useUpdateDevice();
  const [editOpened, setEditOpened] = useState(false);
  const [editName, setEditName] = useState('');

  if (isLoading) return <Center h={300}><Loader /></Center>;
  if (error || !device) return <Alert color="red" title="Ошибка">Устройство не найдено</Alert>;

  return (
    <Stack gap="lg">
      <Group justify="space-between">
        <div>
          <Title order={3}>{device.name}</Title>
          <Text c="dimmed" size="sm" ff="monospace">{device.hardwareId}</Text>
        </div>
        <Group>
          <Tooltip label="Редактировать"><ActionIcon variant="subtle" onClick={() => { setEditName(device.name); setEditOpened(true); }}><IconPencil size={18} /></ActionIcon></Tooltip>
          <Tooltip label="Удалить"><ActionIcon variant="subtle" color="red" onClick={() => { if (confirm('Удалить устройство?')) deleteDevice.mutate(device.id, { onSuccess: () => navigate('/devices') }); }}><IconTrash size={18} /></ActionIcon></Tooltip>
          <Badge color={device.status === 'active' ? 'green' : 'gray'} size="lg" variant="light">
            {formatDeviceStatus(device.status)}
          </Badge>
          <Badge color={getConnectivityColor(device.connectivity)} size="lg" variant="dot">
            {getConnectivityLabel(device.connectivity)}
          </Badge>
        </Group>
      </Group>

      <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Stack gap="sm">
            <Title order={5}>Общая информация</Title>
            <Group justify="space-between"><Text size="sm" c="dimmed">ID</Text><Text size="sm" ff="monospace">{device.id}</Text></Group>
            <Group justify="space-between"><Text size="sm" c="dimmed">Hardware ID</Text><Text size="sm" ff="monospace">{device.hardwareId}</Text></Group>
            <Group justify="space-between"><Text size="sm" c="dimmed">Модель</Text><Text size="sm">{device.model}</Text></Group>
            <Group justify="space-between"><Text size="sm" c="dimmed">Тип</Text><Text size="sm">{device.deviceType}</Text></Group>
            {device.firmwareVersion && (
              <Group justify="space-between"><Text size="sm" c="dimmed">Firmware</Text><Text size="sm">{device.firmwareVersion}</Text></Group>
            )}
            <Group justify="space-between"><Text size="sm" c="dimmed">Last Seen</Text><Text size="sm">{formatRelativeTime(device.lastSeenAt)}</Text></Group>
          </Stack>
        </Card>

        <Card shadow="sm" padding="md" radius="md" withBorder>
          <Stack gap="sm">
            <Title order={5}>Назначение</Title>
            {device.assignedRoomId ? (
              <Button variant="subtle" size="xs" component={Link} to={`/rooms/${device.assignedRoomId}`}>
                {device.assignedRoomName ?? device.assignedRoomId}
              </Button>
            ) : (
              <Text size="sm" c="dimmed">Не назначено</Text>
            )}
          </Stack>
        </Card>
      </SimpleGrid>

      <Card shadow="sm" padding="md" radius="md" withBorder>
        <Title order={5} mb="md">Capabilities</Title>
        <Table.ScrollContainer minWidth={500}>
          <Table striped highlightOnHover withTableBorder>
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Code</Table.Th>
                <Table.Th>Тип</Table.Th>
                <Table.Th>Ед.</Table.Th>
                <Table.Th>Диапазон</Table.Th>
                <Table.Th>Доступно</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {device.capabilities.map(c => (
                <Table.Tr key={c.code}>
                  <Table.Td><Text ff="monospace" size="sm">{c.code}</Text></Table.Td>
                  <Table.Td><Badge size="sm" variant="light" color="gray">{c.kind}</Badge></Table.Td>
                  <Table.Td>{c.unit ?? '—'}</Table.Td>
                  <Table.Td>{c.minimum != null ? `${c.minimum}–${c.maximum}` : '—'}</Table.Td>
                  <Table.Td>
                    <Badge color={c.available ? 'green' : 'gray'} size="sm" variant="light">
                      {c.available ? 'Да' : 'Нет'}
                    </Badge>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      </Card>

      <Card shadow="sm" padding="md" radius="md" withBorder>
        <Group justify="space-between">
          <Title order={5}>OTA</Title>
          <Badge color="gray" variant="light">В разработке</Badge>
        </Group>
        <Text size="sm" c="dimmed" mt="sm">Обновление прошивки появится в будущих версиях</Text>
      </Card>

      <Modal opened={editOpened} onClose={() => setEditOpened(false)} title="Редактировать устройство" size="sm">
        <Stack gap="md">
          <TextInput label="Имя" value={editName} onChange={e => setEditName(e.currentTarget.value)} />
          <Group justify="flex-end">
            <Button variant="outline" onClick={() => setEditOpened(false)}>Отмена</Button>
            <Button loading={updateDevice.isPending} onClick={async () => { await updateDevice.mutateAsync({ id: device.id, name: editName }); setEditOpened(false); }}>Сохранить</Button>
          </Group>
        </Stack>
      </Modal>
    </Stack>
  );
}