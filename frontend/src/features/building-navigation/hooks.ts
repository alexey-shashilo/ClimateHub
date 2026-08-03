import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { buildingApi, roomApi, deviceApi, policyApi, needApi, engineeringSystemApi, commandPlanApi, resourceApi } from '@shared/api/endpoints';
import { queryKeys } from '@shared/lib/query-keys';

export function useBuildings() {
  return useQuery({
    queryKey: queryKeys.buildings.all,
    queryFn: ({ signal }) => buildingApi.list(signal),
    staleTime: 60000,
  });
}

export function useBuilding(buildingId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.buildings.detail(buildingId ?? ''),
    queryFn: ({ signal }) => buildingApi.getById(buildingId!, signal),
    enabled: !!buildingId,
    staleTime: 60000,
  });
}

export function useFloors(buildingId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.buildings.floors(buildingId ?? ''),
    queryFn: ({ signal }) => buildingApi.getFloors(buildingId!, signal),
    enabled: !!buildingId,
    staleTime: 60000,
  });
}

export function useRoom(roomId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.rooms.detail(roomId ?? ''),
    queryFn: ({ signal }) => roomApi.getById(roomId!, signal),
    enabled: !!roomId,
    staleTime: 30000,
  });
}

export function useRoomEnvironment(roomId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.rooms.environment(roomId ?? ''),
    queryFn: ({ signal }) => roomApi.getEnvironment(roomId!, signal),
    enabled: !!roomId,
    staleTime: 15000,
  });
}

export function useRoomHistory(
  roomId: string | undefined,
  from: string,
  to: string,
  parameter?: string,
) {
  return useQuery({
    queryKey: queryKeys.rooms.history(roomId ?? '', { from, to, parameter }),
    queryFn: ({ signal }) => roomApi.getHistory(roomId!, from, to, parameter, signal),
    enabled: !!roomId && !!from && !!to,
    staleTime: 30000,
  });
}

export function useRoomDevices(roomId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.rooms.devices(roomId ?? ''),
    queryFn: ({ signal }) => roomApi.getDevices(roomId!, signal),
    enabled: !!roomId,
    staleTime: 30000,
  });
}

export function useDevices() {
  return useQuery({
    queryKey: queryKeys.devices.all,
    queryFn: ({ signal }) => deviceApi.list(signal),
    staleTime: 30000,
  });
}

export function useDevice(deviceId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.devices.detail(deviceId ?? ''),
    queryFn: ({ signal }) => deviceApi.getById(deviceId!, signal),
    enabled: !!deviceId,
    staleTime: 30000,
  });
}

export function useRegisterDevice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { hardwareId: string; name: string; manufacturer: string; modelName: string; protocolVersion: string }) =>
      deviceApi.register(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.devices.all });
    },
  });
}

export function useAssignDevice() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ deviceId, roomId }: { deviceId: string; roomId: string }) =>
      deviceApi.assign(deviceId, roomId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.devices.all });
    },
  });
}

export function useCreateBuilding() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: { name: string; address?: string }) => buildingApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.buildings.all });
    },
  });
}

export function useCreateFloor() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ buildingId, ...data }: { buildingId: string; name: string; level: number }) =>
      buildingApi.createFloor(buildingId, data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.buildings.floors(variables.buildingId) });
    },
  });
}

export function useCreateRoom() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ floorId, ...data }: { floorId: string; name: string; purpose?: string }) =>
      buildingApi.createRoom(floorId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.buildings.all });
    },
  });
}

export function useUpdateBuilding() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: { id: string; name: string }) => buildingApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.buildings.all }),
  });
}
export function useDeleteBuilding() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => buildingApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.buildings.all }),
  });
}
export function useDeleteFloor() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ floorId, buildingId }: { floorId: string; buildingId: string }) =>
      buildingApi.removeFloor(floorId, buildingId),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.buildings.all }),
  });
}
export function useDeleteRoom() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ roomId, floorId }: { roomId: string; floorId: string }) =>
      buildingApi.removeRoom(roomId, floorId),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.buildings.all }),
  });
}
export function useDeleteDevice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deviceApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.devices.all }),
  });
}
export function useUpdateDevice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, ...data }: { id: string; name: string }) => deviceApi.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: queryKeys.devices.all }),
  });
}

export function useRoomPolicy(roomId: string | undefined) {
  return useQuery({
    queryKey: ['rooms', roomId, 'policy'],
    queryFn: ({ signal }) => policyApi.get(roomId!, signal),
    enabled: !!roomId,
    staleTime: 60000,
  });
}

export function useUpdateRoomPolicy() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ roomId, data }: { roomId: string; data: any }) => policyApi.update(roomId, data),
    onSuccess: (_data, vars) => qc.invalidateQueries({ queryKey: ['rooms', vars.roomId, 'policy'] }),
  });
}

export function useNeeds() {
  return useQuery({
    queryKey: queryKeys.needs.all,
    queryFn: ({ signal }) => needApi.list(undefined, signal),
    staleTime: 15000,
  });
}

export function useNeedsByRoom(roomId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.needs.byRoom(roomId ?? ''),
    queryFn: ({ signal }) => needApi.getByRoom(roomId!, signal),
    enabled: !!roomId,
    staleTime: 15000,
  });
}

export function useNeedDetail(needId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.needs.detail(needId ?? ''),
    queryFn: ({ signal }) => needApi.getById(needId!, signal),
    enabled: !!needId,
    staleTime: 10000,
  });
}

export function useEvaluateRoom() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (roomId: string) => needApi.evaluateRoom(roomId),
    onSuccess: (_data, roomId) => {
      qc.invalidateQueries({ queryKey: queryKeys.needs.byRoom(roomId) });
      qc.invalidateQueries({ queryKey: queryKeys.needs.all });
    },
  });
}

export function useEvaluateNeed() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (needId: string) => needApi.evaluateNeed(needId),
    onSuccess: (_data, needId) => {
      qc.invalidateQueries({ queryKey: queryKeys.needs.detail(needId) });
      qc.invalidateQueries({ queryKey: queryKeys.needs.all });
    },
  });
}

export function useExecuteNeed() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (needId: string) => needApi.execute(needId),
    onSuccess: (_data, needId) => {
      qc.invalidateQueries({ queryKey: queryKeys.needs.detail(needId) });
      qc.invalidateQueries({ queryKey: queryKeys.needs.all });
    },
  });
}

export function useCancelNeed() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (needId: string) => needApi.cancel(needId),
    onSuccess: (_data, needId) => {
      qc.invalidateQueries({ queryKey: queryKeys.needs.detail(needId) });
      qc.invalidateQueries({ queryKey: queryKeys.needs.all });
    },
  });
}

export function useUnblockNeed() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (needId: string) => needApi.unblock(needId),
    onSuccess: (_data, needId) => {
      qc.invalidateQueries({ queryKey: queryKeys.needs.detail(needId) });
      qc.invalidateQueries({ queryKey: queryKeys.needs.all });
    },
  });
}

export function useNeedEvaluations(needId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.needs.evaluations(needId ?? ''),
    queryFn: ({ signal }) => needApi.getEvaluations(needId!, signal),
    enabled: !!needId,
    staleTime: 15000,
  });
}

// Engineering Systems hooks
export function useEngineeringSystems() {
  return useQuery({
    queryKey: queryKeys.engineeringSystems.list(),
    queryFn: ({ signal }) => engineeringSystemApi.list(signal),
    staleTime: 30000,
  });
}

export function useEngineeringSystem(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.engineeringSystems.detail(id ?? ''),
    queryFn: ({ signal }) => engineeringSystemApi.getById(id!, signal),
    enabled: !!id,
    staleTime: 30000,
  });
}

export function useEngineeringSystemResources(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.engineeringSystems.resources(id ?? ''),
    queryFn: ({ signal }) => engineeringSystemApi.getResources(id!, signal),
    enabled: !!id,
    staleTime: 30000,
  });
}

export function useEngineeringSystemZones(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.engineeringSystems.zones(id ?? ''),
    queryFn: ({ signal }) => engineeringSystemApi.getZones(id!, signal),
    enabled: !!id,
    staleTime: 30000,
  });
}

export function useEngineeringSystemDevices(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.engineeringSystems.devices(id ?? ''),
    queryFn: ({ signal }) => engineeringSystemApi.getDevices(id!, signal),
    enabled: !!id,
    staleTime: 30000,
  });
}

export function useEngineeringSystemPlans(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.engineeringSystems.plans(id ?? ''),
    queryFn: ({ signal }) => engineeringSystemApi.getPlans(id!, signal),
    enabled: !!id,
    staleTime: 30000,
  });
}

export function useEngineeringSystemStatus(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.engineeringSystems.status(id ?? ''),
    queryFn: ({ signal }) => engineeringSystemApi.getStatus(id!, signal),
    enabled: !!id,
    staleTime: 15000,
  });
}

export function useCommandPlans() {
  return useQuery({
    queryKey: queryKeys.commandPlans.list(),
    queryFn: ({ signal }) => commandPlanApi.list(signal),
    staleTime: 15000,
  });
}

export function useCommandPlan(id: string | undefined) {
  return useQuery({
    queryKey: queryKeys.commandPlans.detail(id ?? ''),
    queryFn: ({ signal }) => commandPlanApi.getById(id!, signal),
    enabled: !!id,
    staleTime: 15000,
  });
}