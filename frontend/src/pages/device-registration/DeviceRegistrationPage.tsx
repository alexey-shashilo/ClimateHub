import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Title, Text, Stack, TextInput, Select, Button, Group, Card, Alert, SimpleGrid,
} from '@mantine/core';
import { notifications } from '@mantine/notifications';
import { useRegisterDevice } from '@features/building-navigation/hooks';
import { useBuildings, useFloors } from '@features/building-navigation/hooks';
import { registerDeviceRequestSchema } from '@shared/contracts/schemas';

export function DeviceRegistrationPage() {
  const navigate = useNavigate();
  const registerMutation = useRegisterDevice();
  const { data: buildings } = useBuildings();
  const [selectedBuilding, setSelectedBuilding] = useState<string | null>(null);
  const { data: floors } = useFloors(selectedBuilding ?? undefined);

  const [form, setForm] = useState({
    hardwareId: '',
    name: '',
    manufacturer: '',
    modelName: '',
    protocolVersion: '1.0',
    roomId: '',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const allRooms = floors?.flatMap(f => f.rooms) ?? [];

  const setField = (field: string, value: string) => {
    setForm(prev => ({ ...prev, [field]: value }));
    setErrors(prev => ({ ...prev, [field]: '' }));
  };

  const handleSubmit = async () => {
    const result = registerDeviceRequestSchema.safeParse({
      hardwareId: form.hardwareId,
      name: form.name,
      model: form.manufacturer + ' ' + form.modelName,
      deviceType: 'environmental-sensor',
      protocolVersion: form.protocolVersion,
      capabilityCodes: ['measure.temperature', 'measure.relative-humidity', 'measure.co2'],
    });

    if (!result.success) {
      const fieldErrors: Record<string, string> = {};
      for (const issue of result.error.issues) {
        const path = issue.path.join('.');
        fieldErrors[path] = issue.message;
      }
      setErrors(fieldErrors);
      return;
    }

    try {
      const response = await registerMutation.mutateAsync({
        hardwareId: form.hardwareId,
        name: form.name,
        manufacturer: form.manufacturer,
        modelName: form.modelName,
        protocolVersion: form.protocolVersion,
      });

      notifications.show({
        title: 'Устройство зарегистрировано',
        message: `${form.name} (${form.hardwareId})`,
        color: 'green',
      });

      navigate(`/devices/${response.id}`);
    } catch (err: any) {
      const message = err?.details?.detail ?? err?.message ?? 'Ошибка регистрации';
      const code = err?.details?.errorCode ?? '';
      notifications.show({
        title: code === 'DEVICE_ALREADY_REGISTERED' ? 'Устройство уже существует' : 'Ошибка',
        message,
        color: 'red',
      });
    }
  };

  return (
    <Stack maw={600} mx="auto" gap="lg">
      <Title order={3}>Регистрация устройства</Title>

      <Card shadow="sm" padding="lg" radius="md" withBorder>
        <Stack gap="md">
          <TextInput
            label="Hardware ID"
            placeholder="ESP32-S3-001"
            required
            value={form.hardwareId}
            onChange={(e) => setField('hardwareId', e.currentTarget.value)}
            error={errors.hardwareId}
          />
          <TextInput
            label="Имя устройства"
            placeholder="Датчик гостиной"
            required
            value={form.name}
            onChange={(e) => setField('name', e.currentTarget.value)}
            error={errors.name}
          />
          <Group grow>
            <TextInput
              label="Производитель"
              placeholder="Acme"
              required
              value={form.manufacturer}
              onChange={(e) => setField('manufacturer', e.currentTarget.value)}
            />
            <TextInput
              label="Модель"
              placeholder="ClimateNode v2"
              required
              value={form.modelName}
              onChange={(e) => setField('modelName', e.currentTarget.value)}
            />
          </Group>
          <TextInput
            label="Версия протокола"
            placeholder="1.0"
            required
            value={form.protocolVersion}
            onChange={(e) => setField('protocolVersion', e.currentTarget.value)}
            error={errors.protocolVersion}
          />

          <Select
            label="Здание"
            placeholder="Выберите здание"
            data={buildings?.map(b => ({ value: b.id, label: b.name })) ?? []}
            value={selectedBuilding}
            onChange={setSelectedBuilding}
            clearable
          />

          <Select
            label="Помещение"
            placeholder="Выберите помещение"
            data={allRooms.map(r => ({ value: r.id, label: r.name }))}
            disabled={!selectedBuilding}
            clearable
            value={form.roomId}
            onChange={(v) => setField('roomId', v ?? '')}
          />

          <Alert color="blue" title="Capabilities">
            При регистрации будут назначены:
            <br />
            measure.temperature, measure.relative-humidity, measure.co2
          </Alert>

          {errors.capabilityCodes && (
            <Alert color="red">{errors.capabilityCodes}</Alert>
          )}

          {registerMutation.error && (
            <Alert color="red" title="Ошибка">
              {registerMutation.error.message}
            </Alert>
          )}

          <Group justify="flex-end">
            <Button variant="outline" onClick={() => navigate('/devices')}>
              Отмена
            </Button>
            <Button onClick={handleSubmit} loading={registerMutation.isPending}>
              Зарегистрировать
            </Button>
          </Group>
        </Stack>
      </Card>
    </Stack>
  );
}