import { useParams, Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import {
  Title, Text, SimpleGrid, Card, Group, Badge, Stack, Loader, Center, Alert, Button, Modal,
  TextInput, NumberInput, ActionIcon, Tooltip,
} from '@mantine/core';
import { IconPencil, IconTrash, IconPlus } from '@tabler/icons-react';
import { useBuilding, useFloors, useCreateBuilding, useCreateFloor, useCreateRoom,
  useUpdateBuilding, useDeleteBuilding, useDeleteFloor, useDeleteRoom } from '@features/building-navigation/hooks';
import { StatusBadge } from '@shared/components/StatusBadge';
import { formatTemperatureShort, formatRelativeTime } from '@shared/lib/formatters';
import type { RoomSummary, FloorSummary } from '@shared/types/domain';

function RoomCard({ room, floorId, onDelete }: { room: RoomSummary; floorId: string; onDelete: (id: string) => void }) {
  const env = room.environment;
  return (
    <Card shadow="sm" padding="md" radius="md" withBorder style={{ position: 'relative' }}>
      <Group justify="space-between" mb="xs">
        <Text fw={600} component={Link} to={`/rooms/${room.id}`} style={{ textDecoration: 'none', color: 'inherit' }}>
          {room.name}
        </Text>
        <StatusBadge status={room.status} size="xs" />
      </Group>
      <SimpleGrid cols={3} spacing="xs" mb="xs">
        <Stack gap={0}>
          <Text size="xs" c="dimmed" tt="uppercase" style={{ letterSpacing: '0.05em' }}>T</Text>
          <Text fw={600} size="lg">{formatTemperatureShort(env.temperatureC)}</Text>
        </Stack>
        <Stack gap={0}>
          <Text size="xs" c="dimmed" tt="uppercase" style={{ letterSpacing: '0.05em' }}>RH</Text>
          <Text fw={600} size="lg">{env.relativeHumidityPct != null ? `${Math.round(env.relativeHumidityPct)}%` : '—'}</Text>
        </Stack>
        <Stack gap={0}>
          <Text size="xs" c="dimmed" tt="uppercase" style={{ letterSpacing: '0.05em' }}>CO₂</Text>
          <Text fw={600} size="lg">{env.co2Ppm != null ? `${Math.round(env.co2Ppm)}` : '—'}</Text>
        </Stack>
      </SimpleGrid>
      <Group gap="xs">
        <Text size="xs" c="dimmed">{room.devicesOnline}/{room.devicesTotal} уст.</Text>
        {room.activeWarningsCount > 0 && <Badge color="yellow" size="xs" variant="light">{room.activeWarningsCount} пред.</Badge>}
        <Text size="xs" c="dimmed" ml="auto">{formatRelativeTime(room.updatedAt)}</Text>
        <Tooltip label="Удалить помещение"><ActionIcon variant="subtle" color="red" size="sm" onClick={() => onDelete(room.id)}><IconTrash size={14} /></ActionIcon></Tooltip>
      </Group>
    </Card>
  );
}

export function BuildingOverviewPage() {
  const { buildingId } = useParams<{ buildingId: string }>();
  const { data: building, isLoading: buildingLoading, error: buildingError } = useBuilding(buildingId);
  const { data: floors, isLoading: floorsLoading } = useFloors(buildingId);
  const navigate = useNavigate();
  const deleteBuilding = useDeleteBuilding();
  const deleteFloor = useDeleteFloor();
  const deleteRoom = useDeleteRoom();
  const updateBuilding = useUpdateBuilding();

  const [createBuildingOpened, setCreateBuildingOpened] = useState(false);
  const [addFloorOpened, setAddFloorOpened] = useState(false);
  const [addRoomFloorId, setAddRoomFloorId] = useState<string | null>(null);
  const [editBuildingOpened, setEditBuildingOpened] = useState(false);
  const [editName, setEditName] = useState('');

  if (!buildingId) return <Center h={400}><Stack align="center"><Title order={3}>Добро пожаловать</Title><Button onClick={() => setCreateBuildingOpened(true)}>+ Создать здание</Button></Stack></Center>;

  if (buildingLoading || floorsLoading) return <Center h={300}><Loader /></Center>;
  if (buildingError || !building) return <Center h={400}><Stack align="center"><Title order={3}>Здание не найдено</Title><Button onClick={() => setCreateBuildingOpened(true)}>+ Создать здание</Button></Stack></Center>;

  return (
    <Stack gap="lg">
      <Group justify="space-between">
        <div>
          <Title order={2}>{building.name}</Title>
          <Text c="dimmed" size="sm">
            {building.floorsCount} эт. · {building.roomsCount} пом. · {building.devicesOnline} уст. в сети ·
            Обновлено {formatRelativeTime(building.updatedAt)}
          </Text>
        </div>
        <Group gap={4}>
          <Tooltip label="Редактировать"><ActionIcon variant="subtle" onClick={() => { setEditName(building.name); setEditBuildingOpened(true); }}><IconPencil size={18} /></ActionIcon></Tooltip>
          <Tooltip label="Удалить"><ActionIcon variant="subtle" color="red" onClick={() => { if (confirm('Удалить здание?')) deleteBuilding.mutate(building.id, { onSuccess: () => navigate('/') }); }}><IconTrash size={18} /></ActionIcon></Tooltip>
          <Button leftSection={<IconPlus size={16} />} onClick={() => setAddFloorOpened(true)}>Этаж</Button>
        </Group>
      </Group>

      {(!floors || floors.length === 0) && <Alert color="gray" title="Нет этажей">Добавьте первый этаж</Alert>}

      {floors?.map((floor: FloorSummary) => (
        <div key={floor.id}>
          <Group mb="sm" justify="space-between">
            <Group><Title order={4}>{floor.name}</Title><Badge size="lg" variant="light">{floor.rooms.length} пом.</Badge></Group>
            <Group gap={4}>
              <Tooltip label="Удалить этаж"><ActionIcon variant="subtle" color="red" size="sm" onClick={() => { if (confirm('Удалить этаж?')) deleteFloor.mutate({ floorId: floor.id, buildingId: building.id }); }}><IconTrash size={16} /></ActionIcon></Tooltip>
              <Button variant="outline" size="xs" leftSection={<IconPlus size={14} />} onClick={() => setAddRoomFloorId(floor.id)}>Помещение</Button>
            </Group>
          </Group>
          <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
            {floor.rooms.length === 0 && <Text size="sm" c="dimmed" py="md">Нет помещений</Text>}
            {floor.rooms.map((room: RoomSummary) => (
              <RoomCard key={room.id} room={room} floorId={floor.id} onDelete={(rid) => { if (confirm('Удалить помещение?')) deleteRoom.mutate({ roomId: rid, floorId: floor.id }); }} />
            ))}
          </SimpleGrid>
        </div>
      ))}

      <Modal opened={createBuildingOpened} onClose={() => setCreateBuildingOpened(false)} title="Создать здание" size="sm">
        <CreateBuildingForm onClose={() => setCreateBuildingOpened(false)} />
      </Modal>
      <Modal opened={editBuildingOpened} onClose={() => setEditBuildingOpened(false)} title="Редактировать здание" size="sm">
        <Stack gap="md">
          <TextInput label="Название" value={editName} onChange={e => setEditName(e.currentTarget.value)} />
          <Group justify="flex-end">
            <Button variant="outline" onClick={() => setEditBuildingOpened(false)}>Отмена</Button>
            <Button loading={updateBuilding.isPending} onClick={async () => { await updateBuilding.mutateAsync({ id: building.id, name: editName }); setEditBuildingOpened(false); }}>Сохранить</Button>
          </Group>
        </Stack>
      </Modal>
      <AddFloorModal buildingId={buildingId} opened={addFloorOpened} onClose={() => setAddFloorOpened(false)} />
      {addRoomFloorId && <AddRoomModal floorId={addRoomFloorId} opened={!!addRoomFloorId} onClose={() => setAddRoomFloorId(null)} />}
    </Stack>
  );
}

function CreateBuildingForm({ onClose }: { onClose: () => void }) {
  const [name, setName] = useState(''); const create = useCreateBuilding();
  return <Stack gap="md">
    <TextInput label="Название" required value={name} onChange={e => setName(e.currentTarget.value)} />
    <Group justify="flex-end"><Button variant="outline" onClick={onClose}>Отмена</Button>
      <Button loading={create.isPending} disabled={!name.trim()} onClick={async () => { await create.mutateAsync({ name }); setName(''); onClose(); }}>Создать</Button></Group>
  </Stack>;
}

function AddFloorModal({ buildingId, opened, onClose }: { buildingId: string; opened: boolean; onClose: () => void }) {
  const [name, setName] = useState(''); const [level, setLevel] = useState<number | ''>(1); const createFloor = useCreateFloor();
  return <Modal opened={opened} onClose={onClose} title="Добавить этаж" size="sm">
    <Stack gap="md">
      <TextInput label="Название" required value={name} onChange={e => setName(e.currentTarget.value)} />
      <NumberInput label="Номер" value={level} onChange={v => setLevel(v as number)} min={-5} max={100} />
      <Group justify="flex-end"><Button variant="outline" onClick={onClose}>Отмена</Button>
        <Button loading={createFloor.isPending} disabled={!name.trim()} onClick={async () => { await createFloor.mutateAsync({ buildingId, name, level: level || 0 }); setName(''); onClose(); }}>Добавить</Button></Group>
    </Stack>
  </Modal>;
}

function AddRoomModal({ floorId, opened, onClose }: { floorId: string; opened: boolean; onClose: () => void }) {
  const [name, setName] = useState(''); const createRoom = useCreateRoom();
  return <Modal opened={opened} onClose={onClose} title="Добавить помещение" size="sm">
    <Stack gap="md">
      <TextInput label="Название" required value={name} onChange={e => setName(e.currentTarget.value)} />
      <Group justify="flex-end"><Button variant="outline" onClick={onClose}>Отмена</Button>
        <Button loading={createRoom.isPending} disabled={!name.trim()} onClick={async () => { await createRoom.mutateAsync({ floorId, name }); setName(''); onClose(); }}>Добавить</Button></Group>
    </Stack>
  </Modal>;
}