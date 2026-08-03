import { http, HttpResponse, delay } from 'msw';
import type { NeedDto, NeedEvaluationDto, PolicyDto } from '@shared/types/domain';
import {
  mockBuildings,
  mockFloors,
  mockRooms,
  getRoomById,
  getDeviceById,
  getRoomsByFloor,
  getDevicesByRoom,
  _mockDevicesState,
  addMockDevice,
  mockEngineeringSystems,
  mockEngineeringSystemZones,
  mockEngineeringSystemDevices,
  mockEngineeringSystemStatus,
  mockCommandPlans,
} from '../database/db';

function problem(status: number, title: string, detail?: string) {
  return HttpResponse.json(
    { title, status, detail, errorCode: title },
    { status },
  );
}

export const handlers = [
  // Buildings
  http.get('*/api/v1/buildings', async () => {
    await delay(200);
    return HttpResponse.json(mockBuildings);
  }),

  http.get('*/api/v1/buildings/:buildingId', async ({ params }) => {
    await delay(150);
    const building = mockBuildings.find(b => b.id === params.buildingId);
    if (!building) return problem(404, 'BUILDING_NOT_FOUND', `Building ${params.buildingId} not found`);
    return HttpResponse.json(building);
  }),

  http.get('*/api/v1/buildings/:buildingId/floors', async ({ params }) => {
    await delay(200);
    const floors = mockFloors
      .filter(f => f.buildingId === params.buildingId)
      .map(f => ({ ...f, rooms: getRoomsByFloor(f.id) }));
    return HttpResponse.json(floors);
  }),

  http.post('*/api/v1/buildings', async ({ request }) => {
    await delay(300);
    const body = (await request.json()) as Record<string, unknown>;
    if (!body.name) return problem(400, 'VALIDATION_ERROR', 'name is required');
    const newBuilding = {
      id: `building-${Date.now()}`,
      name: body.name,
      status: 'normal',
      floorsCount: 0,
      roomsCount: 0,
      roomsWithWarnings: 0,
      devicesOnline: 0,
      devicesOffline: 0,
      updatedAt: new Date().toISOString(),
    };
    mockBuildings.push(newBuilding as any);
    return HttpResponse.json(newBuilding, { status: 201 });
  }),

  http.post('*/api/v1/buildings/:buildingId/floors', async ({ params, request }) => {
    await delay(300);
    const body = (await request.json()) as Record<string, unknown>;
    if (!body.name) return problem(400, 'VALIDATION_ERROR', 'name is required');
    const newFloor = {
      id: `floor-${Date.now()}`,
      buildingId: params.buildingId,
      name: body.name,
      number: body.level ?? 0,
      rooms: [],
    };
    mockFloors.push(newFloor as any);
    return HttpResponse.json(newFloor, { status: 201 });
  }),

  http.post('*/api/v1/floors/:floorId/rooms', async ({ params, request }) => {
    await delay(300);
    const body = (await request.json()) as Record<string, unknown>;
    if (!body.name) return problem(400, 'VALIDATION_ERROR', 'name is required');
    const floor = mockFloors.find(f => f.id === params.floorId);
    if (!floor) return problem(404, 'FLOOR_NOT_FOUND', `Floor ${params.floorId} not found`);
    const newRoom = {
      id: `room-${Date.now()}`,
      floorId: params.floorId,
      name: body.name,
      status: 'normal',
      environment: {},
      activeWarningsCount: 0,
      devicesOnline: 0,
      devicesTotal: 0,
      updatedAt: new Date().toISOString(),
    };
    mockRooms.push(newRoom as any);
    return HttpResponse.json(newRoom, { status: 201 });
  }),

  // Rooms
  http.get('*/api/v1/rooms/:roomId', async ({ params }) => {
    await delay(150);
    const room = getRoomById(params.roomId as string);
    if (!room) return problem(404, 'ROOM_NOT_FOUND', `Room ${params.roomId} not found`);
    return HttpResponse.json(room);
  }),

  http.get('*/api/v1/floors/:floorId/rooms', async ({ params }) => {
    await delay(200);
    const rooms = getRoomsByFloor(params.floorId as string);
    return HttpResponse.json(rooms);
  }),

  // Environment
  http.get('*/api/v1/rooms/:roomId/environment', async ({ params }) => {
    await delay(200);
    const room = getRoomById(params.roomId as string);
    if (!room) return problem(404, 'ROOM_NOT_FOUND');

    const now = new Date().toISOString();
    const staleTime = new Date(Date.now() - 7200000).toISOString();
    const isStale = room.status === 'stale';

    return HttpResponse.json({
      roomId: room.id,
      roomName: room.name,
      status: room.status,
      parameters: {
        temperature: room.environment.temperatureC != null ? {
          value: room.environment.temperatureC,
          unit: 'celsius',
          measuredAt: isStale ? staleTime : now,
          receivedAt: isStale ? staleTime : now,
          quality: room.environment.quality ?? 'valid',
          sourceDeviceId: 'device-001',
          target: { minimum: 20, maximum: 26, preferred: 23 },
        } : undefined,
        relativeHumidity: room.environment.relativeHumidityPct != null ? {
          value: room.environment.relativeHumidityPct,
          unit: 'percent',
          measuredAt: isStale ? staleTime : now,
          receivedAt: isStale ? staleTime : now,
          quality: room.environment.quality ?? 'valid',
          sourceDeviceId: 'device-001',
          target: { minimum: 30, maximum: 60, preferred: 45 },
        } : undefined,
        co2: room.environment.co2Ppm != null ? {
          value: room.environment.co2Ppm,
          unit: 'ppm',
          measuredAt: now,
          receivedAt: now,
          quality: 'valid',
          sourceDeviceId: 'device-002',
          target: { minimum: 0, maximum: 1000, preferred: 600 },
        } : undefined,
        illuminance: room.environment.illuminanceLux != null ? {
          value: room.environment.illuminanceLux,
          unit: 'lux',
          measuredAt: now,
          receivedAt: now,
          quality: 'valid',
          sourceDeviceId: 'device-001',
          target: { minimum: 300, maximum: 750, preferred: 500 },
        } : undefined,
      },
      updatedAt: now,
    });
  }),

  http.get('*/api/v1/rooms/:roomId/environment/history', async ({ request, params }) => {
    await delay(300);
    const room = getRoomById(params.roomId as string);
    if (!room) return problem(404, 'ROOM_NOT_FOUND');

    const url = new URL(request.url);
    const parameter = url.searchParams.get('parameter') ?? 'temperature';
    const fromParam = url.searchParams.get('from');
    const toParam = url.searchParams.get('to');

    const from = fromParam ? new Date(fromParam) : new Date(Date.now() - 86400000);
    const to = toParam ? new Date(toParam) : new Date();

    const unitMap: Record<string, string> = { temperature: 'celsius', humidity: 'percent', co2: 'ppm' };
    const baseValue: Record<string, number> = {
      temperature: room.environment.temperatureC ?? 22,
      humidity: room.environment.relativeHumidityPct ?? 45,
      co2: room.environment.co2Ppm ?? 700,
    };

    const points = [];
    const count = Math.min(48, Math.ceil((to.getTime() - from.getTime()) / 3600000));
    for (let i = 0; i < count; i++) {
      const ts = new Date(from.getTime() + (i * (to.getTime() - from.getTime())) / count);
      const variation = (Math.random() - 0.5) * (parameter === 'co2' ? 200 : parameter === 'humidity' ? 10 : 5);
      const value = Math.round((baseValue[parameter] + variation) * 10) / 10;
      points.push({
        timestamp: ts.toISOString(),
        value: Math.max(0, value),
        quality: 'valid',
      });
    }

    return HttpResponse.json({
      roomId: room.id,
      parameter,
      unit: unitMap[parameter] ?? '',
      from: from.toISOString(),
      to: to.toISOString(),
      aggregation: 'raw',
      points,
    });
  }),

  // Devices
  http.get('*/api/v1/rooms/:roomId/devices', async ({ params }) => {
    await delay(200);
    const devices = getDevicesByRoom(params.roomId as string);
    return HttpResponse.json(devices);
  }),

  http.get('*/api/v1/devices', async () => {
    await delay(200);
    return HttpResponse.json(_mockDevicesState);
  }),

  http.get('*/api/v1/devices/:deviceId', async ({ params }) => {
    await delay(150);
    const device = getDeviceById(params.deviceId as string) ?? _mockDevicesState.find(d => d.id === params.deviceId);
    if (!device) return problem(404, 'DEVICE_NOT_FOUND', `Device ${params.deviceId} not found`);
    return HttpResponse.json(device);
  }),

  http.post('*/api/v1/devices', async ({ request }) => {
    await delay(300);
    const body = await request.json() as Record<string, unknown>;

    if (!body.hardwareId) return problem(400, 'VALIDATION_ERROR', 'hardwareId is required');
    if (!body.name) return problem(400, 'VALIDATION_ERROR', 'name is required');

    const exists = _mockDevicesState.some(d => d.hardwareId === body.hardwareId);
    if (exists) return problem(409, 'DEVICE_ALREADY_REGISTERED', `HardwareId '${body.hardwareId}' is already registered`);

    const newDevice = {
      id: `device-${Date.now()}`,
      hardwareId: body.hardwareId as string,
      name: body.name as string,
      model: (body.modelName as string) ?? 'Unknown',
      deviceType: 'environmental-sensor',
      status: 'registered' as const,
      connectivity: 'offline' as const,
      lastSeenAt: undefined,
      assignedRoomId: undefined,
      assignedRoomName: undefined,
      capabilities: [
        { code: 'measure.temperature', kind: 'measurement', unit: 'celsius', minimum: -50, maximum: 100, writable: false, available: true },
        { code: 'measure.relative-humidity', kind: 'measurement', unit: 'percent', minimum: 0, maximum: 100, writable: false, available: true },
        { code: 'measure.co2', kind: 'measurement', unit: 'ppm', minimum: 0, maximum: 100000, writable: false, available: true },
      ],
    };

    addMockDevice(newDevice as any);

    return HttpResponse.json(newDevice, { status: 201 });
  }),

  http.post('*/api/v1/devices/:deviceId/assignments', async ({ params, request }) => {
    await delay(200);
    const body = await request.json() as Record<string, unknown>;
    const device = getDeviceById(params.deviceId as string) ?? _mockDevicesState.find(d => d.id === params.deviceId);
    if (!device) return problem(404, 'DEVICE_NOT_FOUND');
    if (!body.roomId) return problem(400, 'VALIDATION_ERROR', 'roomId is required');

    const room = getRoomById(body.roomId as string);
    if (!room) return problem(404, 'ROOM_NOT_FOUND');

    const updated = { ...device, assignedRoomId: body.roomId as string, assignedRoomName: room.name };
    return HttpResponse.json(updated);
  }),

  // Needs
  // ────────────────────── Needs ──────────────────────
  http.get('*/api/v1/needs', async ({ request }) => {
    await delay(200);
    const url = new URL(request.url);
    const roomId = url.searchParams.get('roomId');
    const now = new Date();
    const oneMinAgo = new Date(now.getTime() - 60000);
    const cooldownFuture = new Date(now.getTime() + 300000);

    const mockNeeds: NeedDto[] = [
      {
        id: 'need-001', type: 'co2Reduction', severity: 'high', status: 'waitingForEffect',
        desiredMin: 0, desiredMax: 1000, desiredPreferred: 600,
        currentValue: 1320, deviation: 320, sourceParameterCode: 'co2',
        roomId: 'room-001', selectedCapabilityCode: 'control.fan-speed',
        activeCommandId: 'cmd-001', selectedDeviceId: 'device-005',
        planningFailureCode: undefined, controlMode: 'automatic',
        violationSince: oneMinAgo.toISOString(), stableSince: undefined,
        cooldownUntil: undefined, effectEvaluationDueAt: cooldownFuture.toISOString(),
        
        createdAt: oneMinAgo.toISOString(), updatedAt: now.toISOString(), version: 3,
      },
      {
        id: 'need-002', type: 'co2Reduction', severity: 'low', status: 'detected',
        desiredMin: 0, desiredMax: 1000, desiredPreferred: 600,
        currentValue: 1050, deviation: 50, sourceParameterCode: 'co2',
        roomId: 'room-004', selectedCapabilityCode: undefined,
        activeCommandId: undefined, selectedDeviceId: undefined,
        planningFailureCode: undefined, controlMode: 'automatic',
        violationSince: now.toISOString(), stableSince: undefined,
        cooldownUntil: undefined, effectEvaluationDueAt: undefined,
        
        createdAt: now.toISOString(), updatedAt: undefined, version: 1,
      },
      {
        id: 'need-003', type: 'humidityDecrease', severity: 'high', status: 'blocked',
        desiredMin: 30, desiredMax: 60, desiredPreferred: 45,
        currentValue: 18, deviation: 42, sourceParameterCode: 'humidity',
        roomId: 'room-006', selectedCapabilityCode: 'control.humidifier',
        activeCommandId: undefined, selectedDeviceId: undefined,
        planningFailureCode: 'AMBIGUOUS_CAPABLE_DEVICE', controlMode: 'automatic',
        violationSince: oneMinAgo.toISOString(), stableSince: undefined,
        cooldownUntil: undefined, effectEvaluationDueAt: undefined,
        
        createdAt: oneMinAgo.toISOString(), updatedAt: now.toISOString(), version: 2,
      },
    ];

    if (roomId) {
      return HttpResponse.json(mockNeeds.filter(n => n.roomId === roomId));
    }
    return HttpResponse.json(mockNeeds);
  }),

  http.get('*/api/v1/needs/:needId', async ({ params }) => {
    await delay(150);
    const now = new Date();
    const oneMinAgo = new Date(now.getTime() - 60000);
    const cooldownFuture = new Date(now.getTime() + 300000);

    if (params.needId === 'need-001') {
      return HttpResponse.json({
        id: 'need-001', type: 'co2Reduction', severity: 'high', status: 'waitingForEffect',
        desiredMin: 0, desiredMax: 1000, desiredPreferred: 600,
        currentValue: 1320, deviation: 320, sourceParameterCode: 'co2',
        roomId: 'room-001', selectedCapabilityCode: 'control.fan-speed',
        activeCommandId: 'cmd-001', selectedDeviceId: 'device-005',
        planningFailureCode: undefined, controlMode: 'automatic',
        violationSince: oneMinAgo.toISOString(), stableSince: undefined,
        cooldownUntil: undefined, effectEvaluationDueAt: cooldownFuture.toISOString(),
        
        createdAt: oneMinAgo.toISOString(), updatedAt: now.toISOString(), version: 3,
      } satisfies NeedDto);
    }
    if (params.needId === 'need-002') {
      return HttpResponse.json({
        id: 'need-002', type: 'co2Reduction', severity: 'low', status: 'detected',
        desiredMin: 0, desiredMax: 1000, desiredPreferred: 600,
        currentValue: 1050, deviation: 50, sourceParameterCode: 'co2',
        roomId: 'room-004', selectedCapabilityCode: undefined,
        activeCommandId: undefined, selectedDeviceId: undefined,
        planningFailureCode: undefined, controlMode: 'automatic',
        violationSince: now.toISOString(), stableSince: undefined,
        cooldownUntil: undefined, effectEvaluationDueAt: undefined,
        
        createdAt: now.toISOString(), updatedAt: undefined, version: 1,
      } satisfies NeedDto);
    }
    if (params.needId === 'need-003') {
      return HttpResponse.json({
        id: 'need-003', type: 'humidityDecrease', severity: 'high', status: 'blocked',
        desiredMin: 30, desiredMax: 60, desiredPreferred: 45,
        currentValue: 18, deviation: 42, sourceParameterCode: 'humidity',
        roomId: 'room-006', selectedCapabilityCode: 'control.humidifier',
        activeCommandId: undefined, selectedDeviceId: undefined,
        planningFailureCode: 'AMBIGUOUS_CAPABLE_DEVICE', controlMode: 'automatic',
        violationSince: oneMinAgo.toISOString(), stableSince: undefined,
        cooldownUntil: undefined, effectEvaluationDueAt: undefined,
        
        createdAt: oneMinAgo.toISOString(), updatedAt: now.toISOString(), version: 2,
      } satisfies NeedDto);
    }
    return problem(404, 'NEED_NOT_FOUND');
  }),

  http.get('*/api/v1/needs/rooms/:roomId', async ({ params }) => {
    await delay(200);
    const now = new Date();
    return HttpResponse.json([
      {
        id: 'need-001', type: 'co2Reduction', severity: 'high', status: 'waitingForEffect',
        desiredMin: 0, desiredMax: 1000, desiredPreferred: 600,
        currentValue: 1320, deviation: 320, sourceParameterCode: 'co2',
        roomId: params.roomId as string, selectedCapabilityCode: 'control.fan-speed',
        activeCommandId: 'cmd-001', selectedDeviceId: 'device-005',
        planningFailureCode: undefined, controlMode: 'automatic',
        createdAt: new Date(now.getTime() - 60000).toISOString(),
        updatedAt: now.toISOString(), version: 3,
      },
    ] as NeedDto[]);
  }),

  http.get('*/api/v1/needs/buildings/:buildingId', async () => {
    await delay(200);
    const now = new Date();
    return HttpResponse.json([
      {
        id: 'need-001', type: 'co2Reduction', severity: 'high', status: 'waitingForEffect',
        desiredMin: 0, desiredMax: 1000, desiredPreferred: 600,
        currentValue: 1320, deviation: 320, sourceParameterCode: 'co2',
        roomId: 'room-001', selectedCapabilityCode: 'control.fan-speed',
        activeCommandId: 'cmd-001', selectedDeviceId: 'device-005',
        planningFailureCode: undefined, controlMode: 'automatic',
        createdAt: new Date(now.getTime() - 60000).toISOString(),
        updatedAt: now.toISOString(), version: 3,
      },
    ] as NeedDto[]);
  }),

  http.post('*/api/v1/needs/rooms/:roomId/evaluate', async () => {
    await delay(500);
    return HttpResponse.json({ status: 'evaluated' });
  }),

  http.post('*/api/v1/needs/:needId/evaluate', async () => {
    await delay(500);
    return HttpResponse.json({ status: 'planned' });
  }),

  http.post('*/api/v1/needs/:needId/execute', async ({ params }) => {
    await delay(500);
    if (params.needId === 'need-002') {
      return HttpResponse.json({ status: 'executing' });
    }
    return problem(400, 'ACTIVE_COMMAND_EXISTS');
  }),

  http.post('*/api/v1/needs/:needId/cancel', async () => {
    await delay(300);
    return HttpResponse.json({ status: 'cancelled' });
  }),

  http.post('*/api/v1/needs/:needId/unblock', async () => {
    await delay(300);
    return HttpResponse.json({ status: 'unblocked' });
  }),

  http.get('*/api/v1/needs/:needId/evaluations', async ({ params }) => {
    await delay(200);
    const now = new Date();
    const evaluations: NeedEvaluationDto[] = [
      {
        id: 1, needId: params.needId as string, trigger: 'environmentStateChanged',
        previousStatus: undefined, newStatus: 'Detected', outcome: 'Detected',
        evaluatedAt: new Date(now.getTime() - 300000).toISOString(),
      },
      {
        id: 2, needId: params.needId as string, trigger: 'periodicReconciliation',
        previousStatus: 'Detected', newStatus: 'Blocked', outcome: 'Blocked',
        failureCode: 'DEVICE_OFFLINE',
        evaluatedAt: new Date(now.getTime() - 240000).toISOString(),
      },
    ];
    return HttpResponse.json(evaluations);
  }),

  // ────────────────────── Engineering Systems ──────────────────────
  http.get('*/api/v1/engineering-systems', async () => {
    await delay(200);
    return HttpResponse.json(mockEngineeringSystems);
  }),

  http.get('*/api/v1/engineering-systems/:id', async ({ params }) => {
    await delay(150);
    const sys = mockEngineeringSystems.find(s => s.id === params.id);
    if (!sys) return problem(404, 'ENGINEERING_SYSTEM_NOT_FOUND');
    return HttpResponse.json(sys);
  }),

  http.get('*/api/v1/engineering-systems/:id/resources', async ({ params }) => {
    await delay(150);
    const sys = mockEngineeringSystems.find(s => s.id === params.id);
    if (!sys) return problem(404, 'ENGINEERING_SYSTEM_NOT_FOUND');
    return HttpResponse.json(sys.resources);
  }),

  http.get('*/api/v1/engineering-systems/:id/zones', async ({ params }) => {
    await delay(150);
    const zones = mockEngineeringSystemZones[params.id as string] ?? [];
    return HttpResponse.json(zones);
  }),

  http.get('*/api/v1/engineering-systems/:id/devices', async ({ params }) => {
    await delay(150);
    const devices = mockEngineeringSystemDevices[params.id as string] ?? [];
    return HttpResponse.json(devices);
  }),

  http.get('*/api/v1/engineering-systems/:id/plans', async ({ params }) => {
    await delay(200);
    const plans = mockCommandPlans.filter(p => p.engineeringSystemId === params.id);
    return HttpResponse.json(plans);
  }),

  http.get('*/api/v1/engineering-systems/:id/status', async ({ params }) => {
    await delay(100);
    const status = mockEngineeringSystemStatus[params.id as string] ?? {};
    return HttpResponse.json(status);
  }),

  http.post('*/api/v1/engineering-systems', async ({ request }) => {
    await delay(300);
    const body = await request.json() as Record<string, unknown>;
    const newSystem = {
      id: `es-${Date.now()}`,
      buildingId: body.buildingId ?? 'building-001',
      name: body.name ?? 'New System',
      systemType: body.systemType ?? 'ventilation',
      lifecycle: 'commissioning',
      operationalStatus: 'normal',
      controlMode: body.controlMode ?? 'automatic',
      priority: body.priority ?? 5,
      description: body.description,
      capabilities: [],
      resources: [],
      zoneCount: 0,
      deviceCount: 0,
      version: 1,
    };
    mockEngineeringSystems.push(newSystem);
    return HttpResponse.json(newSystem, { status: 201 });
  }),

  // ────────────────────── Command Plans ──────────────────────
  http.get('*/api/v1/command-plans', async () => {
    await delay(200);
    return HttpResponse.json(mockCommandPlans);
  }),

  http.get('*/api/v1/command-plans/:id', async ({ params }) => {
    await delay(150);
    const plan = mockCommandPlans.find(p => p.id === params.id);
    if (!plan) return problem(404, 'COMMAND_PLAN_NOT_FOUND');
    return HttpResponse.json(plan);
  }),

  http.post('*/api/v1/command-plans/:id/cancel', async () => {
    await delay(200);
    return HttpResponse.json({ status: 'cancelled' });
  }),

  // ────────────────────── Resources ──────────────────────
  http.get('*/api/v1/resources', async () => {
    await delay(200);
    const allResources = mockEngineeringSystems.flatMap(s => s.resources.map((r: any) => ({ ...r, systemId: s.id, systemName: s.name })));
    return HttpResponse.json(allResources);
  }),
];