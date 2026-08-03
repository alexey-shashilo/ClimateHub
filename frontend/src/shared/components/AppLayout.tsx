import { useState } from 'react';
import { Outlet, useNavigate, useParams, Link } from 'react-router-dom';
import {
  AppShell,
  Group,
  Text,
  NavLink,
  Burger,
  Box,
  ScrollArea,
  Button,
  Modal,
  Stack,
  TextInput,
} from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconBuilding, IconDeviceAnalytics, IconPlus, IconHome, IconAlertTriangle, IconBuildingFactory, IconFiles } from '@tabler/icons-react';
import { useBuildings, useCreateBuilding } from '@features/building-navigation/hooks';
import { ThemeSwitcher } from '@shared/theme/ThemeSwitcher';
import type { BuildingSummary } from '@shared/types/domain';

export function AppLayout() {
  const [opened, { toggle }] = useDisclosure();
  const navigate = useNavigate();
  const { data: buildings, isLoading } = useBuildings();
  const params = useParams();
  const buildingName = buildings?.[0]?.name ?? 'Climate Hub';
  const [createOpen, setCreateOpen] = useState(false);
  const [newName, setNewName] = useState('');
  const createBld = useCreateBuilding();

  return (
    <AppShell
      header={{ height: 52 }}
      navbar={{ width: 260, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="md"
    >
      <AppShell.Header>
        <Group h="100%" px="sm" justify="space-between" gap={4}>
          <Group gap={4}>
            <Burger opened={opened} onClick={toggle} hiddenFrom="sm" size="sm" />
            <IconHome size={20} stroke={1.5} />
            <Text fw={700} size="md" component={Link} to="/" style={{ textDecoration: 'none', color: 'inherit', marginLeft: 4 }}>
              Climate Hub
            </Text>
          </Group>
          <Group gap={4}>
            <Text size="xs" c="dimmed" visibleFrom="sm">{buildingName}</Text>
            <ThemeSwitcher />
          </Group>
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        <ScrollArea>
          <NavLink
            label="Needs"
            leftSection={<IconAlertTriangle size={18} stroke={1.5} />}
            onClick={() => { navigate('/needs'); toggle(); }}
          />
          <NavLink
            label="Инженерные системы"
            leftSection={<IconBuildingFactory size={18} stroke={1.5} />}
            onClick={() => { navigate('/engineering-systems'); toggle(); }}
            active={location.pathname.startsWith('/engineering-systems')}
          />
          <NavLink
            label="Планы команд"
            leftSection={<IconFiles size={18} stroke={1.5} />}
            onClick={() => { navigate('/command-plans'); toggle(); }}
            active={location.pathname.startsWith('/command-plans')}
          />
          <NavLink
            label="Здания"
            leftSection={<IconBuilding size={18} stroke={1.5} />}
            childrenOffset={28}
            defaultOpened
          >
            {isLoading ? (
              <Text size="sm" c="dimmed" px="md" py="xs">Загрузка...</Text>
            ) : !buildings?.length ? (
              <>
                <Text size="sm" c="dimmed" px="md" py="xs">Нет зданий</Text>
              </>
            ) : (
              buildings?.map((building: BuildingSummary) => (
                <NavLink
                  key={building.id}
                  label={building.name}
                  description={`${building.floorsCount} эт. · ${building.roomsCount} пом.`}
                  active={params.buildingId === building.id}
                  onClick={() => { navigate(`/buildings/${building.id}`); toggle(); }}
                />
              ))
            )}
            <Button variant="subtle" size="xs" fullWidth onClick={() => setCreateOpen(true)}>
              + Создать
            </Button>
          </NavLink>
          <NavLink
            label="Устройства"
            leftSection={<IconDeviceAnalytics size={18} stroke={1.5} />}
            onClick={() => { navigate('/devices'); toggle(); }}
          />
          <NavLink
            label="Регистрация"
            leftSection={<IconPlus size={18} stroke={1.5} />}
            onClick={() => { navigate('/devices/register'); toggle(); }}
          />
        </ScrollArea>

      <Modal opened={createOpen} onClose={() => setCreateOpen(false)} title="Создать здание" size="sm">
        <Stack gap="md">
          <TextInput label="Название" required value={newName} onChange={e => setNewName(e.currentTarget.value)} />
          <Group justify="flex-end">
            <Button variant="outline" onClick={() => setCreateOpen(false)}>Отмена</Button>
            <Button loading={createBld.isPending} disabled={!newName.trim()}
              onClick={async () => { await createBld.mutateAsync({ name: newName }); setNewName(''); setCreateOpen(false); }}>
              Создать
            </Button>
          </Group>
        </Stack>
      </Modal>
      </AppShell.Navbar>

      <AppShell.Main>
        <Box maw={1280} mx="auto">
          <Outlet />
        </Box>
      </AppShell.Main>
    </AppShell>
  );
}