import { useParams, Link } from 'react-router-dom';
import {
  Stack, Title, Text, Group, Badge, Card, Loader, Tabs, Table, Timeline, Progress, Button,
} from '@mantine/core';
import {
  useEngineeringSystem, useEngineeringSystemResources,
  useEngineeringSystemZones, useEngineeringSystemDevices,
  useEngineeringSystemPlans, useEngineeringSystemStatus,
} from '@features/building-navigation/hooks';

const lifecycleColor: Record<string, string> = {
  active: 'green', commissioning: 'blue', decommissioning: 'orange',
  maintenance: 'yellow', idle: 'gray', offline: 'red',
};

const statusColor: Record<string, string> = {
  normal: 'green', warning: 'yellow', critical: 'red', offline: 'gray',
};

const planStatusColor: Record<string, string> = {
  created: 'gray', planning: 'blue', reserved: 'cyan', ready: 'indigo',
  executing: 'violet', succeeded: 'green', failed: 'red', cancelled: 'yellow',
};

function formatDate(d: string | undefined | null): string {
  if (!d) return '—';
  return new Date(d).toLocaleString();
}

export function EngineeringSystemDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: system, isLoading } = useEngineeringSystem(id);
  const { data: resources } = useEngineeringSystemResources(id);
  const { data: zones } = useEngineeringSystemZones(id);
  const { data: devices } = useEngineeringSystemDevices(id);
  const { data: plans } = useEngineeringSystemPlans(id);
  const { data: status } = useEngineeringSystemStatus(id);

  if (isLoading) return <Loader />;
  if (!system) return <Text>Система не найдена.</Text>;

  return (
    <Stack p="md">
      <Group>
        <Title order={2}>{system.name}</Title>
        <Badge color={lifecycleColor[system.lifecycle?.toLowerCase()] ?? 'gray'} size="lg">{system.lifecycle}</Badge>
        <Badge color={statusColor[system.operationalStatus?.toLowerCase()] ?? 'gray'} size="lg" variant="dot">{system.operationalStatus}</Badge>
        <Badge variant="light" color="blue" size="lg">{system.controlMode}</Badge>
      </Group>
      <Text c="dimmed" size="sm">Тип: {system.systemType} · Приоритет: {system.priority} · Зон: {system.zoneCount} · Устройств: {system.deviceCount}</Text>
      {system.description && <Text>{system.description}</Text>}

      <Tabs defaultValue="summary">
        <Tabs.List>
          <Tabs.Tab value="summary">Сводка</Tabs.Tab>
          <Tabs.Tab value="capabilities">Способности</Tabs.Tab>
          <Tabs.Tab value="zones">Зоны</Tabs.Tab>
          <Tabs.Tab value="devices">Устройства</Tabs.Tab>
          <Tabs.Tab value="resources">Ресурсы</Tabs.Tab>
          <Tabs.Tab value="plans">Планы команд</Tabs.Tab>
          <Tabs.Tab value="needs">Активные потребности</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="summary" pt="md">
          <Card withBorder>
            <Title order={4}>Статус системы</Title>
            {status ? (
              <Stack gap="sm" mt="sm">
                {Object.entries(status).map(([key, val]) => (
                  <Group key={key} justify="space-between">
                    <Text fw={500}>{key}</Text>
                    <Text>{String(val)}</Text>
                  </Group>
                ))}
              </Stack>
            ) : (
              <Text c="dimmed" size="sm" mt="sm">Нет данных о статусе</Text>
            )}
          </Card>
        </Tabs.Panel>

        <Tabs.Panel value="capabilities" pt="md">
          {system.capabilities?.length ? (
            <Table striped>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Код</Table.Th>
                  <Table.Th>Тип данных</Table.Th>
                  <Table.Th>Ед. изм.</Table.Th>
                  <Table.Th>Мин</Table.Th>
                  <Table.Th>Макс</Table.Th>
                  <Table.Th>Модуляция</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {system.capabilities.map((c: any) => (
                  <Table.Tr key={c.code}>
                    <Table.Td><Text fw={500}>{c.code}</Text></Table.Td>
                    <Table.Td>{c.dataType ?? '—'}</Table.Td>
                    <Table.Td>{c.unit ?? '—'}</Table.Td>
                    <Table.Td>{c.minimum ?? '—'}</Table.Td>
                    <Table.Td>{c.maximum ?? '—'}</Table.Td>
                    <Table.Td>{c.supportsModulation ? 'Да' : 'Нет'}</Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          ) : (
            <Text c="dimmed">Нет способностей</Text>
          )}
        </Tabs.Panel>

        <Tabs.Panel value="zones" pt="md">
          {zones?.length ? (
            <Stack gap="sm">
              {zones.map((z: any) => (
                <Card key={z.id} withBorder padding="sm">
                  <Group justify="space-between">
                    <Text fw={500}>{z.name}</Text>
                    <Badge>Приоритет {z.priority}</Badge>
                  </Group>
                  <Text size="sm" c="dimmed">Комнат: {z.roomIds?.length ?? 0}</Text>
                </Card>
              ))}
            </Stack>
          ) : (
            <Text c="dimmed">Нет зон</Text>
          )}
        </Tabs.Panel>

        <Tabs.Panel value="devices" pt="md">
          {devices?.length ? (
            <Table striped>
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Устройство</Table.Th>
                  <Table.Th>Роль</Table.Th>
                  <Table.Th>Приоритет</Table.Th>
                  <Table.Th>Статус</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {devices.map((d: any) => (
                  <Table.Tr key={d.id}>
                    <Table.Td>
                      <Link to={`/devices/${d.deviceId ?? d.id}`}>{d.deviceId ?? d.id}</Link>
                    </Table.Td>
                    <Table.Td>{d.role ?? '—'}</Table.Td>
                    <Table.Td>{d.priority ?? '—'}</Table.Td>
                    <Table.Td>
                      <Badge color={d.enabled ? 'green' : 'gray'}>{d.enabled ? 'Вкл' : 'Выкл'}</Badge>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          ) : (
            <Text c="dimmed">Нет привязанных устройств</Text>
          )}
        </Tabs.Panel>

        <Tabs.Panel value="resources" pt="md">
          {resources?.length ? (
            <Stack gap="sm">
              {resources.map((r: any) => (
                <Card key={r.code} withBorder padding="sm">
                  <Group justify="space-between">
                    <Text fw={500}>{r.code}</Text>
                    <Badge>Приоритет {r.priority}</Badge>
                  </Group>
                  {r.unit && <Text size="sm" c="dimmed">Ед. изм.: {r.unit}</Text>}
                  <Progress.Root size="md" mt="sm">
                    <Progress.Section value={(r.used / r.maximum) * 100} color="blue">
                      <Progress.Label>Использовано: {r.used}</Progress.Label>
                    </Progress.Section>
                    <Progress.Section value={(r.reserved / r.maximum) * 100} color="yellow">
                      <Progress.Label>Зарезервировано: {r.reserved}</Progress.Label>
                    </Progress.Section>
                  </Progress.Root>
                  <Group justify="space-between" mt={4}>
                    <Text size="xs">Доступно: {r.available}</Text>
                    <Text size="xs">Макс: {r.maximum}</Text>
                  </Group>
                </Card>
              ))}
            </Stack>
          ) : (
            <Text c="dimmed">Нет ресурсов</Text>
          )}
        </Tabs.Panel>

        <Tabs.Panel value="plans" pt="md">
          {plans?.length ? (
            <Timeline active={plans.length - 1} bulletSize={16} lineWidth={2}>
              {plans.map((p: any) => (
                <Timeline.Item key={p.id} title={
                  <Group gap="xs">
                    <Link to={`/command-plans/${p.id}`}>{p.id}</Link>
                    <Badge color={planStatusColor[p.status?.toLowerCase()] ?? 'gray'} size="sm">{p.status}</Badge>
                  </Group>
                }>
                  <Text size="sm">{p.needType} → {p.capabilityCode}</Text>
                  <Text size="xs" c="dimmed">Шагов: {p.steps?.length ?? 0} · {formatDate(p.createdAt)}</Text>
                </Timeline.Item>
              ))}
            </Timeline>
          ) : (
            <Text c="dimmed">Нет планов команд</Text>
          )}
        </Tabs.Panel>

        <Tabs.Panel value="needs" pt="md">
          <Text c="dimmed" size="sm">Активные потребности, связанные с этой системой, отображаются на странице потребностей.</Text>
          <Button variant="subtle" component={Link} to="/needs" mt="sm">Перейти к потребностям</Button>
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}