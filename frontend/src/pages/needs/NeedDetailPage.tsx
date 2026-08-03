import { useParams, Link } from 'react-router-dom';
import { Stack, Title, Text, Group, Badge, Card, Loader, Button, Tooltip, Divider, Timeline } from '@mantine/core';
import {
  useNeedDetail, useEvaluateNeed, useExecuteNeed,
  useCancelNeed, useUnblockNeed, useNeedEvaluations,
} from '@features/building-navigation/hooks';

const statusColor: Record<string, string> = {
  detected: 'gray', planning: 'blue', planned: 'indigo',
  executing: 'violet', waitingForEffect: 'cyan', satisfied: 'green',
  blocked: 'red', cancelled: 'yellow', expired: 'orange',
};

const statusLabel: Record<string, string> = {
  detected: 'Обнаружена', planning: 'Планирование', planned: 'Запланирована',
  executing: 'Выполнение', waitingForEffect: 'Ожидание изменения среды',
  satisfied: 'Удовлетворена', blocked: 'Заблокирована',
  cancelled: 'Отменена', expired: 'Истекла',
};

const failureCodeMessage: Record<string, string> = {
  CAPABLE_DEVICE_NOT_FOUND: 'Не найдено совместимое устройство',
  AMBIGUOUS_CAPABLE_DEVICE: 'Найдено несколько подходящих устройств. Требуется выбор.',
  DEVICE_OFFLINE: 'Совместимое устройство не в сети',
  ENVIRONMENT_DATA_STALE: 'Данные помещения устарели',
  NO_MEASURABLE_EFFECT: 'После воздействия не зафиксировано ожидаемое изменение среды',
  COMMAND_EXECUTION_FAILED: 'Ошибка выполнения команды',
  COMMAND_TIMED_OUT: 'Тайм-аут выполнения команды',
  CAPABILITY_PLAN_NOT_FOUND: 'Не найден план для потребности',
};

const triggerLabel: Record<string, string> = {
  environmentStateChanged: 'Изменение среды',
  periodicReconciliation: 'Периодическая проверка',
  commandSucceeded: 'Команда выполнена',
  commandFailed: 'Ошибка команды',
  commandTimedOut: 'Тайм-аут команды',
  manualRequest: 'Ручной запрос',
  startupRecovery: 'Восстановление после запуска',
};

function formatDate(d: string | undefined | null): string {
  if (!d) return '—';
  return new Date(d).toLocaleString();
}

export function NeedDetailPage() {
  const { needId } = useParams();
  const { data: need, isLoading } = useNeedDetail(needId);
  const { data: evaluations } = useNeedEvaluations(needId);
  const evalNeed = useEvaluateNeed();
  const executeNeed = useExecuteNeed();
  const cancelNeed = useCancelNeed();
  const unblockNeed = useUnblockNeed();

  if (isLoading) return <Loader />;
  if (!need) return <Text>Need not found.</Text>;

  const canExecute = need.status === 'detected' || (need.status === 'blocked' && need.planningFailureCode);
  const canCancel = !['satisfied', 'cancelled', 'expired'].includes(need.status);
  const canUnblock = need.status === 'blocked';
  const isWaitingForEffect = need.status === 'waitingForEffect';

  return (
    <Stack p="md">
      <Group>
        <Title order={2}>Потребность: {need.type}</Title>
        <Badge color={statusColor[need.status.toLowerCase()] ?? 'gray'} size="lg">
          {statusLabel[need.status.toLowerCase()] ?? need.status}
        </Badge>
        <Badge color={need.severity === 'critical' ? 'red' : 'orange'} size="lg">{need.severity}</Badge>
        <Badge variant="light" color="blue" size="lg">{need.controlMode ?? 'monitorOnly'}</Badge>
      </Group>

      {/* Summary */}
      <Card withBorder>
        <Stack gap="sm">
          <Group justify="space-between">
            <Text fw={500}>Помещение</Text>
            <Link to={`/rooms/${need.roomId}`}>{need.roomId.slice(0, 8)}</Link>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Параметр</Text>
            <Text>{need.sourceParameterCode ?? '—'}</Text>
          </Group>
          {isWaitingForEffect && (
            <Group justify="space-between">
              <Text fw={500}>Ожидание изменения среды</Text>
              <Text>Устройство подтвердило воздействие. Система ожидает новое измерение {need.sourceParameterCode}.</Text>
            </Group>
          )}
        </Stack>
      </Card>

      {/* Environment State */}
      <Card withBorder>
        <Title order={4}>Состояние среды</Title>
        <Stack gap="sm" mt="sm">
          <Group justify="space-between">
            <Text fw={500}>{need.sourceParameterCode?.toUpperCase() ?? '—'}</Text>
            <Text>{need.currentValue ?? '—'}</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Цель</Text>
            <Text>≤ {need.desiredMax} (предпочтительно {need.desiredPreferred})</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Отклонение</Text>
            <Text>{need.deviation}</Text>
          </Group>
        </Stack>
      </Card>

      {/* Recommendation */}
      {(need.selectedCapabilityCode || need.selectedDeviceId) && (
        <Card withBorder>
          <Title order={4}>Рекомендация</Title>
          <Stack gap="sm" mt="sm">
            <Group justify="space-between">
              <Text fw={500}>Capability</Text>
              <Text>{need.selectedCapabilityCode ?? '—'}</Text>
            </Group>
            <Group justify="space-between">
              <Text fw={500}>Устройство</Text>
              <Text>{need.selectedDeviceId ?? '—'}</Text>
            </Group>
            <Group justify="space-between">
              <Text fw={500}>Активная команда</Text>
              <Text>{need.activeCommandId ?? '—'}</Text>
            </Group>
          </Stack>
        </Card>
      )}

      {/* Anti-Oscillation State */}
      <Card withBorder>
        <Title order={4}>Анти-осцилляция</Title>
        <Stack gap="sm" mt="sm">
          <Group justify="space-between">
            <Text fw={500}>Нарушение с</Text>
            <Text>{formatDate(need.violationSince)}</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Стабильна с</Text>
            <Text>{formatDate(need.stableSince)}</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Ожидание оценки эффекта до</Text>
            <Text>{formatDate(need.effectEvaluationDueAt)}</Text>
          </Group>
          <Group justify="space-between">
            <Text fw={500}>Остывание до</Text>
            <Text>{formatDate(need.cooldownUntil)}</Text>
          </Group>
        </Stack>
      </Card>

      {/* Blocking Reason */}
      {need.planningFailureCode && (
        <Card withBorder>
          <Title order={4}>Причина блокировки</Title>
          <Badge color="red" size="lg" mt="sm">
            {failureCodeMessage[need.planningFailureCode] ?? need.planningFailureCode}
          </Badge>
        </Card>
      )}

      {/* Evaluation Timeline */}
      {evaluations && evaluations.length > 0 && (
        <Card withBorder>
          <Title order={4}>История оценок</Title>
          <Timeline active={evaluations.length - 1} bulletSize={16} lineWidth={2} mt="sm">
            {evaluations.map((ev) => (
              <Timeline.Item key={ev.id} title={triggerLabel[ev.trigger] ?? ev.trigger}>
                <Text size="sm">
                  {ev.previousStatus ?? '—'} → {ev.newStatus ?? '—'}
                  {ev.failureCode && <Badge color="red" size="xs" ml="xs">{ev.failureCode}</Badge>}
                </Text>
                <Text size="xs" c="dimmed">{formatDate(ev.evaluatedAt)}</Text>
              </Timeline.Item>
            ))}
          </Timeline>
        </Card>
      )}

      {/* Actions */}
      <Group mt="md">
        <Button
          variant="light"
          loading={evalNeed.isPending}
          onClick={() => needId && evalNeed.mutate(needId)}
        >
          Оценить
        </Button>
        {canExecute && (
          <Button
            loading={executeNeed.isPending}
            onClick={() => needId && executeNeed.mutate(needId)}
          >
            Выполнить
          </Button>
        )}
        {canUnblock && (
          <Button
            color="orange"
            loading={unblockNeed.isPending}
            onClick={() => needId && unblockNeed.mutate(needId)}
          >
            Разблокировать
          </Button>
        )}
        {canCancel && (
          <Button
            color="red"
            variant="outline"
            loading={cancelNeed.isPending}
            onClick={() => needId && cancelNeed.mutate(needId)}
          >
            Отменить
          </Button>
        )}
      </Group>

      <Divider />
      <Text size="xs" c="dimmed">
        Создана: {formatDate(need.createdAt)} |
        Обновлена: {formatDate(need.updatedAt)} |
        Версия: {need.version}
      </Text>
    </Stack>
  );
}