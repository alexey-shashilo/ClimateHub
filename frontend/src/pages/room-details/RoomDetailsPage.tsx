import { useParams, Link } from 'react-router-dom';
import {
  Title, Text, SimpleGrid, Card, Group, Badge, Stack, Loader, Center, Alert, Button, Timeline,
} from '@mantine/core';
import { useRoom, useRoomEnvironment, useRoomDevices } from '@features/building-navigation/hooks';
import { useNeedsByRoom, useEngineeringSystems } from '@features/building-navigation/hooks';
import { useRealtimeEnvironment } from '@shared/lib/use-realtime-environment';
import { MetricCard } from '@shared/components/MetricCard';
import { StatusBadge } from '@shared/components/StatusBadge';
import { formatTemperature, formatRelativeHumidity, formatCo2, formatIlluminance, formatRelativeTime } from '@shared/lib/formatters';
import { getConnectivityLabel, getConnectivityColor } from '@shared/lib/freshness';
import { PolicyEditor } from '@features/environment-current-state/PolicyEditor';

function EngineeringSystemsSection({ roomId }: { roomId: string }) {
  const { data: needs } = useNeedsByRoom(roomId);
  const { data: systems } = useEngineeringSystems();

  const activeNeeds = needs?.filter(n => !['satisfied', 'cancelled', 'expired'].includes(n.status)) ?? [];
  const roomSystems = systems?.filter(s =>
    s.zones?.some((z: any) => z.roomIds?.includes(roomId))
  ) ?? [];

  if (!activeNeeds.length && !roomSystems.length) {
    return <Text c="dimmed" size="sm">Нет активных инженерных систем или потребностей для этого помещения.</Text>;
  }

  return (
    <Stack gap="sm">
      {activeNeeds.length > 0 && (
        <Card withBorder padding="sm">
          <Text fw={500} mb="xs">Активные потребности</Text>
          {activeNeeds.map(n => (
            <Group key={n.id} justify="space-between" mb={4}>
              <Text size="sm" component={Link} to={`/needs/${n.id}`}>{n.type}</Text>
              <Badge size="sm">{n.severity}</Badge>
            </Group>
          ))}
        </Card>
      )}

      {roomSystems.length > 0 && (
        <Card withBorder padding="sm">
          <Text fw={500} mb="xs">Связанные системы</Text>
          {roomSystems.map(s => (
            <Group key={s.id} justify="space-between" mb={4}>
              <Text size="sm" component={Link} to={`/engineering-systems/${s.id}`}>{s.name}</Text>
              <Badge size="sm">{s.systemType}</Badge>
            </Group>
          ))}
        </Card>
      )}

      <Timeline bulletSize={12} lineWidth={1}>
        {activeNeeds.slice(0, 3).map(n => {
          const sys = roomSystems.find(s =>
            s.capabilities?.some((c: any) => c.code === n.selectedCapabilityCode)
          );
          return (
            <Timeline.Item key={n.id} title={n.type} color="blue">
              <Text size="xs">Потребность → {sys ? <Link to={`/engineering-systems/${sys.id}`}>{sys.name}</Link> : '—'}</Text>
              <Text size="xs">Статус: {n.status}</Text>
            </Timeline.Item>
          );
        })}
      </Timeline>
    </Stack>
  );
}

export function RoomDetailsPage() {
  const { roomId } = useParams<{ roomId: string }>();
  const { data: room, isLoading: roomLoading, error: roomError } = useRoom(roomId);
  const { data: env, isLoading: envLoading } = useRoomEnvironment(roomId);
  const { data: devices, isLoading: devicesLoading } = useRoomDevices(roomId);
  useRealtimeEnvironment(roomId);

  if (roomLoading || envLoading) {
    return <Center h={300}><Loader /></Center>;
  }

  if (roomError || !room) {
    return <Alert color="red" title="Ошибка">{roomError ? 'Помещение не найдено' : 'Ошибка загрузки'}</Alert>;
  }

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-start">
        <Stack gap={4}>
          <Title order={2}>{env?.roomName ?? room.name}</Title>
          <Group gap="xs">
            <StatusBadge status={room.status} />
            <Text c="dimmed" size="sm">Обновлено {formatRelativeTime(env?.updatedAt ?? room.updatedAt)}</Text>
          </Group>
        </Stack>
        <Group>
          <Button variant="outline" size="sm" component={Link} to={`/rooms/${roomId}/history`}>
            История
          </Button>
          <Button variant="outline" size="sm" component={Link} to={`/rooms/${roomId}/devices`}>
            Устройства
          </Button>
        </Group>
      </Group>

      <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md">
        <MetricCard
          label="Температура"
          value={env?.parameters?.temperature?.value}
          unit="°C"
          formatted={formatTemperature(env?.parameters?.temperature?.value)}
          measuredAt={env?.parameters?.temperature?.measuredAt}
          quality={env?.parameters?.temperature?.quality}
          color="chartTemperature.6"
          target={env?.parameters?.temperature?.target}
        />
        <MetricCard
          label="Влажность"
          value={env?.parameters?.relativeHumidity?.value}
          unit="%"
          formatted={formatRelativeHumidity(env?.parameters?.relativeHumidity?.value)}
          measuredAt={env?.parameters?.relativeHumidity?.measuredAt}
          quality={env?.parameters?.relativeHumidity?.quality}
          color="chartHumidity.6"
          target={env?.parameters?.relativeHumidity?.target}
        />
        <MetricCard
          label="CO₂"
          value={env?.parameters?.co2?.value}
          unit="ppm"
          formatted={formatCo2(env?.parameters?.co2?.value)}
          measuredAt={env?.parameters?.co2?.measuredAt}
          quality={env?.parameters?.co2?.quality}
          color={env?.parameters?.co2?.value != null && env.parameters.co2.value > 1000 ? 'statusCritical.6' : 'chartCo2.6'}
          target={env?.parameters?.co2?.target}
        />
        <MetricCard
          label="Освещённость"
          value={env?.parameters?.illuminance?.value}
          unit="lx"
          formatted={formatIlluminance(env?.parameters?.illuminance?.value)}
          measuredAt={env?.parameters?.illuminance?.measuredAt}
          quality={env?.parameters?.illuminance?.quality}
          color="chartHumidity.6"
          target={env?.parameters?.illuminance?.target}
        />
      </SimpleGrid>

      <PolicyEditor roomId={roomId!} />

      <Card shadow="sm" padding="md" radius="md" withBorder>
        <Group justify="space-between" mb="md">
          <Title order={4}>Инженерные системы</Title>
          <Button variant="subtle" size="sm" component={Link} to="/engineering-systems">
            Все системы
          </Button>
        </Group>
        <EngineeringSystemsSection roomId={roomId!} />
      </Card>

      <Card shadow="sm" padding="md" radius="md" withBorder>
        <Group justify="space-between" mb="md">
          <Title order={4}>Устройства помещения</Title>
          <Button variant="subtle" size="sm" component={Link} to={`/rooms/${roomId}/devices`}>
            Все устройства
          </Button>
        </Group>
        {devicesLoading ? (
          <Loader size="sm" />
        ) : !devices?.length ? (
          <Text c="dimmed" size="sm">Нет устройств в этом помещении</Text>
        ) : (
          <Stack gap="xs">
            {devices?.slice(0, 5).map(d => (
              <Group key={d.id} justify="space-between">
                <div>
                  <Text size="sm" fw={500}>{d.name}</Text>
                  <Text size="xs" c="dimmed">{d.model}</Text>
                </div>
                <Group gap="xs">
                  <Badge color={getConnectivityColor(d.connectivity)} size="sm" variant="dot">
                    {getConnectivityLabel(d.connectivity)}
                  </Badge>
                  {d.lastSeenAt && (
                    <Text size="xs" c="dimmed">{formatRelativeTime(d.lastSeenAt)}</Text>
                  )}
                </Group>
              </Group>
            ))}
          </Stack>
        )}
      </Card>
    </Stack>
  );
}