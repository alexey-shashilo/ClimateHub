import type { RoomSummary, FloorSummary, BuildingSummary, DeviceSummary } from '@shared/types/domain';

export const defaultBuildingId = 'building-001';

export const mockBuildings: BuildingSummary[] = [
  {
    id: 'building-001',
    name: 'Главный дом',
    status: 'normal',
    floorsCount: 2,
    roomsCount: 7,
    roomsWithWarnings: 2,
    devicesOnline: 10,
    devicesOffline: 1,
    updatedAt: new Date().toISOString(),
  },
];

export const mockFloors: FloorSummary[] = [
  {
    id: 'floor-001',
    buildingId: 'building-001',
    name: 'Первый этаж',
    number: 1,
    rooms: [],
  },
  {
    id: 'floor-002',
    buildingId: 'building-001',
    name: 'Второй этаж',
    number: 2,
    rooms: [],
  },
];

export const mockRooms: RoomSummary[] = [
  {
    id: 'room-001',
    floorId: 'floor-001',
    name: 'Гостиная',
    status: 'normal',
    environment: { temperatureC: 22.4, relativeHumidityPct: 42, co2Ppm: 735, illuminanceLux: 650, quality: 'valid' },
    activeWarningsCount: 0,
    devicesOnline: 2,
    devicesTotal: 2,
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'room-002',
    floorId: 'floor-001',
    name: 'Кухня',
    status: 'normal',
    environment: { temperatureC: 24.1, relativeHumidityPct: 38, co2Ppm: 680, illuminanceLux: 800, quality: 'valid' },
    activeWarningsCount: 1,
    devicesOnline: 1,
    devicesTotal: 1,
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'room-003',
    floorId: 'floor-001',
    name: 'Прихожая',
    status: 'stale',
    environment: { temperatureC: 20.0, relativeHumidityPct: 45, co2Ppm: 500, illuminanceLux: 100, quality: 'stale' },
    activeWarningsCount: 0,
    devicesOnline: 0,
    devicesTotal: 1,
    updatedAt: new Date(Date.now() - 3600000 * 2).toISOString(),
  },
  {
    id: 'room-004',
    floorId: 'floor-002',
    name: 'Спальня',
    status: 'warning',
    environment: { temperatureC: 23.5, relativeHumidityPct: 55, co2Ppm: 1250, illuminanceLux: 200, quality: 'valid' },
    activeWarningsCount: 1,
    devicesOnline: 2,
    devicesTotal: 2,
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'room-005',
    floorId: 'floor-002',
    name: 'Детская',
    status: 'normal',
    environment: { temperatureC: 22.8, relativeHumidityPct: 48, co2Ppm: 720, illuminanceLux: 500, quality: 'valid' },
    activeWarningsCount: 0,
    devicesOnline: 1,
    devicesTotal: 1,
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'room-006',
    floorId: 'floor-002',
    name: 'Кабинет',
    status: 'warning',
    environment: { temperatureC: 21.7, relativeHumidityPct: 18, co2Ppm: 1100, illuminanceLux: 450, quality: 'valid' },
    activeWarningsCount: 2,
    devicesOnline: 2,
    devicesTotal: 3,
    updatedAt: new Date().toISOString(),
  },
  {
    id: 'room-007',
    floorId: 'floor-002',
    name: 'Ванная',
    status: 'normal',
    environment: { temperatureC: 25.0, relativeHumidityPct: 65, co2Ppm: 600, illuminanceLux: 300, quality: 'valid' },
    activeWarningsCount: 0,
    devicesOnline: 1,
    devicesTotal: 1,
    updatedAt: new Date().toISOString(),
  },
];

export const mockDevices: DeviceSummary[] = [
  {
    id: 'device-001',
    hardwareId: 'ESP32-S3-001',
    name: 'Датчик гостиной',
    model: 'ClimateNode v2',
    deviceType: 'environmental-sensor',
    status: 'active',
    connectivity: 'online',
    firmwareVersion: '2.1.0',
    lastSeenAt: new Date().toISOString(),
    assignedRoomId: 'room-001',
    assignedRoomName: 'Гостиная',
    capabilities: [
      { code: 'measure.temperature', kind: 'measurement', unit: 'celsius', minimum: -50, maximum: 100, writable: false, available: true },
      { code: 'measure.relative-humidity', kind: 'measurement', unit: 'percent', minimum: 0, maximum: 100, writable: false, available: true },
      { code: 'measure.co2', kind: 'measurement', unit: 'ppm', minimum: 0, maximum: 100000, writable: false, available: true },
    ],
  },
  {
    id: 'device-002',
    hardwareId: 'STM32-001',
    name: 'Датчик CO₂ спальни',
    model: 'CO2Sensor Pro',
    deviceType: 'co2-sensor',
    status: 'active',
    connectivity: 'online',
    lastSeenAt: new Date().toISOString(),
    assignedRoomId: 'room-004',
    assignedRoomName: 'Спальня',
    capabilities: [
      { code: 'measure.co2', kind: 'measurement', unit: 'ppm', minimum: 0, maximum: 100000, writable: false, available: true },
    ],
  },
  {
    id: 'device-003',
    hardwareId: 'ESP32-S3-002',
    name: 'Метеостанция',
    model: 'ClimateNode v2',
    deviceType: 'environmental-sensor',
    status: 'active',
    connectivity: 'online',
    firmwareVersion: '2.1.0',
    lastSeenAt: new Date().toISOString(),
    assignedRoomId: 'room-002',
    assignedRoomName: 'Кухня',
    capabilities: [
      { code: 'measure.temperature', kind: 'measurement', unit: 'celsius', minimum: -50, maximum: 100, writable: false, available: true },
      { code: 'measure.relative-humidity', kind: 'measurement', unit: 'percent', minimum: 0, maximum: 100, writable: false, available: true },
      { code: 'measure.co2', kind: 'measurement', unit: 'ppm', minimum: 0, maximum: 100000, writable: false, available: true },
    ],
  },
  {
    id: 'device-004',
    hardwareId: 'STM32-002',
    name: 'Увлажнитель',
    model: 'Humidifier Pro',
    deviceType: 'humidifier',
    status: 'active',
    connectivity: 'online',
    lastSeenAt: new Date().toISOString(),
    assignedRoomId: 'room-006',
    assignedRoomName: 'Кабинет',
    capabilities: [
      { code: 'control.humidifier', kind: 'control', unit: 'percent', minimum: 0, maximum: 100, writable: true, available: true },
    ],
  },
  {
    id: 'device-005',
    hardwareId: 'BRZ-001',
    name: 'Бризер гостиной',
    model: 'Breezer 3000',
    deviceType: 'ventilation',
    status: 'active',
    connectivity: 'online',
    firmwareVersion: '1.4.2',
    lastSeenAt: new Date().toISOString(),
    assignedRoomId: 'room-001',
    assignedRoomName: 'Гостиная',
    capabilities: [
      { code: 'control.fan-speed', kind: 'control', unit: 'rpm', minimum: 0, maximum: 3000, writable: true, available: true },
    ],
  },
  {
    id: 'device-006',
    hardwareId: 'ESP32-S3-003',
    name: 'Датчик детской',
    model: 'ClimateNode v1',
    deviceType: 'environmental-sensor',
    status: 'active',
    connectivity: 'offline',
    lastSeenAt: new Date(Date.now() - 7200000).toISOString(),
    assignedRoomId: 'room-005',
    assignedRoomName: 'Детская',
    capabilities: [
      { code: 'measure.temperature', kind: 'measurement', unit: 'celsius', minimum: -50, maximum: 100, writable: false, available: false },
    ],
  },
  {
    id: 'device-007',
    hardwareId: 'ESP32-S3-004',
    name: 'Датчик прихожей',
    model: 'ClimateNode v2',
    deviceType: 'environmental-sensor',
    status: 'inactive',
    connectivity: 'offline',
    firmwareVersion: '2.0.0',
    lastSeenAt: new Date(Date.now() - 86400000).toISOString(),
    assignedRoomId: 'room-003',
    assignedRoomName: 'Прихожая',
    capabilities: [
      { code: 'measure.temperature', kind: 'measurement', unit: 'celsius', minimum: -50, maximum: 100, writable: false, available: true },
      { code: 'measure.relative-humidity', kind: 'measurement', unit: 'percent', minimum: 0, maximum: 100, writable: false, available: true },
    ],
  },
  {
    id: 'device-008',
    hardwareId: 'STM32-003',
    name: 'Контроллер CO₂',
    model: 'CO2Sensor Basic',
    deviceType: 'co2-sensor',
    status: 'active',
    connectivity: 'online',
    lastSeenAt: new Date().toISOString(),
    assignedRoomId: 'room-006',
    assignedRoomName: 'Кабинет',
    capabilities: [
      { code: 'measure.co2', kind: 'measurement', unit: 'ppm', minimum: 0, maximum: 100000, writable: false, available: true },
      { code: 'measure.temperature', kind: 'measurement', unit: 'celsius', minimum: -50, maximum: 100, writable: false, available: true },
      { code: 'measure.relative-humidity', kind: 'measurement', unit: 'percent', minimum: 0, maximum: 100, writable: false, available: true },
    ],
  },
];

export function getRoomById(roomId: string): RoomSummary | undefined {
  return mockRooms.find(r => r.id === roomId);
}

export function getFloorById(floorId: string): FloorSummary | undefined {
  return mockFloors.find(f => f.id === floorId);
}

export function getDeviceById(deviceId: string): DeviceSummary | undefined {
  return mockDevices.find(d => d.id === deviceId);
}

export function getRoomsByFloor(floorId: string): RoomSummary[] {
  return mockRooms.filter(r => r.floorId === floorId);
}

export function getDevicesByRoom(roomId: string): DeviceSummary[] {
  return mockDevices.filter(d => d.assignedRoomId === roomId);
}

export let _mockDevicesState = [...mockDevices];

export function addMockDevice(device: DeviceSummary): void {
  _mockDevicesState.push(device);
}

// Engineering Systems mock data
export const mockEngineeringSystems: any[] = [
  {
    id: 'es-001',
    buildingId: 'building-001',
    name: 'Вентиляция CO₂',
    systemType: 'ventilation',
    lifecycle: 'active',
    operationalStatus: 'normal',
    controlMode: 'automatic',
    priority: 10,
    description: 'Система вентиляции для контроля уровня CO₂ в жилых помещениях',
    capabilities: [
      { code: 'control.fan-speed', dataType: 'integer', unit: 'rpm', minimum: 0, maximum: 3000, supportsModulation: true },
      { code: 'control.damper', dataType: 'integer', unit: 'percent', minimum: 0, maximum: 100, supportsModulation: true },
    ],
    resources: [
      { code: 'airflow', unit: 'm3h', maximum: 500, available: 350, reserved: 100, used: 50, priority: 5 },
    ],
    zoneCount: 2,
    deviceCount: 3,
    version: 1,
  },
];

export const mockEngineeringSystemZones: Record<string, any[]> = {
  'es-001': [
    { id: 'zone-001', name: 'Гостиная зона', priority: 10, roomIds: ['room-001', 'room-002'] },
    { id: 'zone-002', name: 'Спальни зона', priority: 8, roomIds: ['room-004', 'room-005'] },
  ],
};

export const mockEngineeringSystemDevices: Record<string, any[]> = {
  'es-001': [
    { id: 'bind-001', deviceId: 'device-005', role: 'primary', priority: 10, enabled: true },
    { id: 'bind-002', deviceId: 'device-002', role: 'monitor', priority: 5, enabled: true },
  ],
};

export const mockEngineeringSystemStatus: Record<string, any> = {
  'es-001': {
    mode: 'active',
    uptime: '15d 4h',
    temperatureC: 38,
    fanSpeedRpm: 1200,
    lastMaintenance: '2026-06-15',
  },
};

export const mockCommandPlans: any[] = [
  {
    id: 'cp-001',
    engineeringSystemId: 'es-001',
    needType: 'co2Reduction',
    capabilityCode: 'control.fan-speed',
    status: 'executing',
    requestedValue: 2000,
    valueUnit: 'rpm',
    strategyName: 'ramp-up',
    steps: [
      { capabilityCode: 'control.fan-speed', operation: 'set', requestedValue: 1500, valueUnit: 'rpm', deviceId: 'device-005', sequence: 1, deviceRole: 'primary', status: 'succeeded' },
      { capabilityCode: 'control.fan-speed', operation: 'set', requestedValue: 2000, valueUnit: 'rpm', deviceId: 'device-005', sequence: 2, deviceRole: 'primary', status: 'executing' },
    ],
    resourceAllocations: [
      { resourceCode: 'airflow', amount: 100 },
    ],
    createdAt: new Date(Date.now() - 120000).toISOString(),
    completedAt: undefined,
    failureCode: undefined,
    version: 2,
  },
  {
    id: 'cp-002',
    engineeringSystemId: 'es-001',
    needType: 'co2Reduction',
    capabilityCode: 'control.fan-speed',
    status: 'succeeded',
    requestedValue: 1800,
    valueUnit: 'rpm',
    strategyName: 'direct',
    steps: [
      { capabilityCode: 'control.fan-speed', operation: 'set', requestedValue: 1800, valueUnit: 'rpm', deviceId: 'device-005', sequence: 1, deviceRole: 'primary', status: 'succeeded' },
    ],
    resourceAllocations: [
      { resourceCode: 'airflow', amount: 80 },
    ],
    createdAt: new Date(Date.now() - 3600000).toISOString(),
    completedAt: new Date(Date.now() - 3540000).toISOString(),
    failureCode: undefined,
    version: 1,
  },
];