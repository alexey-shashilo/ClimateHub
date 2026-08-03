import { useState } from 'react';
import { Card, Stack, Title, Group, Text, Button, NumberInput, Alert } from '@mantine/core';
import { useRoomPolicy, useUpdateRoomPolicy } from '@features/building-navigation/hooks';

function BoundEditor({ label, unit, value, onChange }: { label: string; unit: string; value?: { minimum?: number; maximum?: number; preferred?: number }; onChange: (v: any) => void }) {
  return (
    <Stack gap="xs">
      <Text fw={500} size="sm">{label} ({unit})</Text>
      <Group grow>
        <NumberInput label="Мин" value={value?.minimum ?? ''} onChange={(v) => onChange({ ...value, minimum: v === '' ? undefined : Number(v) })} />
        <NumberInput label="Макс" value={value?.maximum ?? ''} onChange={(v) => onChange({ ...value, maximum: v === '' ? undefined : Number(v) })} />
        <NumberInput label="Предп." value={value?.preferred ?? ''} onChange={(v) => onChange({ ...value, preferred: v === '' ? undefined : Number(v) })} />
      </Group>
    </Stack>
  );
}

export function PolicyEditor({ roomId }: { roomId: string }) {
  const { data: policy, isLoading } = useRoomPolicy(roomId);
  const updatePolicy = useUpdateRoomPolicy();
  const [temp, setTemp] = useState<any>(null);
  const [hum, setHum] = useState<any>(null);
  const [co2, setCo2] = useState<any>(null);
  const [ill, setIll] = useState<any>(null);
  const [dirty, setDirty] = useState(false);

  if (isLoading) return null;

  if (temp === null && policy) {
    setTemp(policy.temperature ?? {});
    setHum(policy.humidity ?? {});
    setCo2(policy.co2 ?? {});
    setIll(policy.illuminance ?? {});
  }

  return (
    <Card shadow="sm" padding="md" radius="md" withBorder>
      <Group justify="space-between" mb="md">
        <Title order={4}>Политика помещения</Title>
        {dirty && <Button size="xs" loading={updatePolicy.isPending} onClick={async () => {
          await updatePolicy.mutateAsync({ roomId, data: { temperature: temp, humidity: hum, co2, illuminance: ill } });
          setDirty(false);
        }}>Сохранить</Button>}
      </Group>
      <Stack gap="md">
        <BoundEditor label="Температура" unit="°C" value={temp} onChange={(v) => { setTemp(v); setDirty(true); }} />
        <BoundEditor label="Влажность" unit="%" value={hum} onChange={(v) => { setHum(v); setDirty(true); }} />
        <BoundEditor label="CO₂" unit="ppm" value={co2} onChange={(v) => { setCo2(v); setDirty(true); }} />
        <BoundEditor label="Освещённость" unit="lx" value={ill} onChange={(v) => { setIll(v); setDirty(true); }} />
      </Stack>
    </Card>
  );
}