import { ActionIcon, Menu, Text, Group, Tooltip as MantineTooltip } from '@mantine/core';
import { useAppearance } from './useAppearance';
import type { AppearanceMode } from './useAppearance';

const modes: { value: AppearanceMode; label: string; icon: string }[] = [
  { value: 'light', label: 'Светлая', icon: '☀️' },
  { value: 'dark', label: 'Тёмная', icon: '🌙' },
  { value: 'system', label: 'Системная', icon: '💻' },
];

export function ThemeSwitcher() {
  const { mode, setMode } = useAppearance();
  const current = modes.find(m => m.value === mode) ?? modes[2];

  return (
    <Menu shadow="md" width={200} withinPortal>
      <Menu.Target>
        <MantineTooltip label="Тема оформления">
          <ActionIcon variant="subtle" size="lg" aria-label="Тема оформления">
            <Text size="xl" style={{ lineHeight: 1 }}>{current.icon}</Text>
          </ActionIcon>
        </MantineTooltip>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Label>Тема оформления</Menu.Label>
        {modes.map(m => (
          <Menu.Item
            key={m.value}
            leftSection={<Text size="lg" style={{ lineHeight: 1 }}>{m.icon}</Text>}
            rightSection={mode === m.value ? <Text c="climate.6">✓</Text> : null}
            onClick={() => setMode(m.value)}
            aria-current={mode === m.value ? 'true' : undefined}
          >
            <Group gap="xs">
              <Text>{m.label}</Text>
              {m.value === 'system' && (
                <Text size="xs" c="dimmed">(авто)</Text>
              )}
            </Group>
          </Menu.Item>
        ))}
      </Menu.Dropdown>
    </Menu>
  );
}