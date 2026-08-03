export const queryKeys = {
  needs: {
    all: ['needs'] as const,
    detail: (id: string) => ['needs', id] as const,
    byRoom: (roomId: string) => ['needs', 'rooms', roomId] as const,
    byBuilding: (buildingId: string) => ['needs', 'buildings', buildingId] as const,
    evaluations: (needId: string) => ['needs', needId, 'evaluations'] as const,
  },
  buildings: {
    all: ['buildings'] as const,
    detail: (id: string) => ['buildings', id] as const,
    floors: (buildingId: string) => ['buildings', buildingId, 'floors'] as const,
  },
  rooms: {
    detail: (roomId: string) => ['rooms', roomId] as const,
    environment: (roomId: string) => ['rooms', roomId, 'environment'] as const,
    history: (roomId: string, params?: Record<string, unknown>) =>
      ['rooms', roomId, 'environment', 'history', params] as const,
    devices: (roomId: string) => ['rooms', roomId, 'devices'] as const,
  },
  devices: {
    all: ['devices'] as const,
    detail: (deviceId: string) => ['devices', deviceId] as const,
  },
  engineeringSystems: {
    all: ['engineering-systems'] as const,
    list: () => [...queryKeys.engineeringSystems.all, 'list'] as const,
    detail: (id: string) => [...queryKeys.engineeringSystems.all, 'detail', id] as const,
    resources: (id: string) => [...queryKeys.engineeringSystems.all, 'resources', id] as const,
    zones: (id: string) => [...queryKeys.engineeringSystems.all, 'zones', id] as const,
    devices: (id: string) => [...queryKeys.engineeringSystems.all, 'devices', id] as const,
    plans: (id: string) => [...queryKeys.engineeringSystems.all, 'plans', id] as const,
    status: (id: string) => [...queryKeys.engineeringSystems.all, 'status', id] as const,
  },
  commandPlans: {
    all: ['command-plans'] as const,
    list: () => [...queryKeys.commandPlans.all, 'list'] as const,
    detail: (id: string) => [...queryKeys.commandPlans.all, 'detail', id] as const,
  },
};