import { getApiClient } from './client';
import type { BuildingSummary, RoomSummary, RoomEnvironmentResponse, EnvironmentHistoryResponse, DeviceSummary, NeedDto, NeedEvaluationDto, PolicyDto } from '@shared/types/domain';

export const buildingApi = {
  list: (signal?: AbortSignal) =>
    getApiClient().get<BuildingSummary[]>('/api/v1/buildings', signal),
  getById: (id: string, signal?: AbortSignal) =>
    getApiClient().get<BuildingSummary>(`/api/v1/buildings/${id}`, signal),
  getFloors: (buildingId: string, signal?: AbortSignal) =>
    getApiClient().get<any[]>(`/api/v1/buildings/${buildingId}/floors`, signal),
  create: (data: { name: string; address?: string }) =>
    getApiClient().post<any>('/api/v1/buildings', data),
  createFloor: (buildingId: string, data: { name: string; level: number }) =>
    getApiClient().post<any>(`/api/v1/buildings/${buildingId}/floors`, data),
  createRoom: (floorId: string, data: { name: string; purpose?: string }) =>
    getApiClient().post<any>(`/api/v1/floors/${floorId}/rooms`, data),
  update: (id: string, data: { name: string }) =>
    getApiClient().put<any>(`/api/v1/buildings/${id}`, data),
  remove: (id: string) =>
    getApiClient().delete<any>(`/api/v1/buildings/${id}`),
  updateFloor: (id: string, data: { name: string; level: number }) =>
    getApiClient().put<any>(`/api/v1/floors/${id}`, data),
  removeFloor: (floorId: string, buildingId: string) =>
    getApiClient().delete<any>(`/api/v1/floors/${floorId}?buildingId=${buildingId}`),
  updateRoom: (id: string, data: { name: string; purpose?: string }) =>
    getApiClient().put<any>(`/api/v1/rooms/${id}`, data),
  removeRoom: (roomId: string, floorId: string) =>
    getApiClient().delete<any>(`/api/v1/rooms/${roomId}?floorId=${floorId}`),
};

export const roomApi = {
  getById: (id: string, signal?: AbortSignal) =>
    getApiClient().get<RoomSummary>(`/api/v1/rooms/${id}`, signal),
  getByFloor: (floorId: string, signal?: AbortSignal) =>
    getApiClient().get<RoomSummary[]>(`/api/v1/floors/${floorId}/rooms`, signal),
  getEnvironment: (roomId: string, signal?: AbortSignal) =>
    getApiClient().get<RoomEnvironmentResponse>(`/api/v1/rooms/${roomId}/environment`, signal),
  getHistory: (
    roomId: string,
    from: string,
    to: string,
    parameter?: string,
    signal?: AbortSignal,
  ) => {
    const params = new URLSearchParams({ from, to });
    if (parameter) params.set('parameter', parameter);
    return getApiClient().get<EnvironmentHistoryResponse>(
      `/api/v1/rooms/${roomId}/environment/history?${params}`,
      signal,
    );
  },
  getDevices: (roomId: string, signal?: AbortSignal) =>
    getApiClient().get<DeviceSummary[]>(`/api/v1/rooms/${roomId}/devices`, signal),
};

export const policyApi = {
  get: (roomId: string, signal?: AbortSignal) =>
    getApiClient().get<any>(`/api/v1/rooms/${roomId}/policy`, signal),
  update: (roomId: string, data: any) =>
    getApiClient().put<any>(`/api/v1/rooms/${roomId}/policy`, data),
};

export const deviceApi = {
  list: (signal?: AbortSignal) =>
    getApiClient().get<DeviceSummary[]>('/api/v1/devices', signal),
  getById: (id: string, signal?: AbortSignal) =>
    getApiClient().get<DeviceSummary>(`/api/v1/devices/${id}`, signal),
  register: (data: { hardwareId: string; name: string; manufacturer: string; modelName: string; protocolVersion: string }) =>
    getApiClient().post<DeviceSummary>('/api/v1/devices', data),
  assign: (deviceId: string, roomId: string) =>
    getApiClient().post<DeviceSummary>(`/api/v1/devices/${deviceId}/assignments`, { roomId }),
  update: (id: string, data: { name: string }) =>
    getApiClient().put<DeviceSummary>(`/api/v1/devices/${id}`, data),
  remove: (id: string) =>
    getApiClient().delete<void>(`/api/v1/devices/${id}`),
};

export const needApi = {
  list: (params?: { roomId?: string }, signal?: AbortSignal) => {
    const query = params?.roomId ? `?roomId=${params.roomId}` : '';
    return getApiClient().get<NeedDto[]>(`/api/v1/needs${query}`, signal);
  },
  getById: (needId: string, signal?: AbortSignal) =>
    getApiClient().get<NeedDto>(`/api/v1/needs/${needId}`, signal),
  getByRoom: (roomId: string, signal?: AbortSignal) =>
    getApiClient().get<NeedDto[]>(`/api/v1/needs/rooms/${roomId}`, signal),
  getByBuilding: (buildingId: string, signal?: AbortSignal) =>
    getApiClient().get<NeedDto[]>(`/api/v1/needs/buildings/${buildingId}`, signal),
  evaluateRoom: (roomId: string) =>
    getApiClient().post<{ status: string }>(`/api/v1/needs/rooms/${roomId}/evaluate`),
  evaluateNeed: (needId: string) =>
    getApiClient().post<{ status: string }>(`/api/v1/needs/${needId}/evaluate`),
  execute: (needId: string) =>
    getApiClient().post<{ status: string }>(`/api/v1/needs/${needId}/execute`),
  cancel: (needId: string) =>
    getApiClient().post<{ status: string }>(`/api/v1/needs/${needId}/cancel`),
  unblock: (needId: string) =>
    getApiClient().post<{ status: string }>(`/api/v1/needs/${needId}/unblock`),
  getEvaluations: (needId: string, signal?: AbortSignal) =>
    getApiClient().get<NeedEvaluationDto[]>(`/api/v1/needs/${needId}/evaluations`, signal),
};

export const engineeringSystemApi = {
  list: (signal?: AbortSignal): Promise<any[]> => getApiClient().get('/api/v1/engineering-systems', signal),
  getById: (id: string, signal?: AbortSignal): Promise<any> => getApiClient().get(`/api/v1/engineering-systems/${id}`, signal),
  getResources: (id: string, signal?: AbortSignal): Promise<any[]> => getApiClient().get(`/api/v1/engineering-systems/${id}/resources`, signal),
  getZones: (id: string, signal?: AbortSignal): Promise<any[]> => getApiClient().get(`/api/v1/engineering-systems/${id}/zones`, signal),
  getDevices: (id: string, signal?: AbortSignal): Promise<any[]> => getApiClient().get(`/api/v1/engineering-systems/${id}/devices`, signal),
  getPlans: (id: string, signal?: AbortSignal): Promise<any[]> => getApiClient().get(`/api/v1/engineering-systems/${id}/plans`, signal),
  getStatus: (id: string, signal?: AbortSignal): Promise<any> => getApiClient().get(`/api/v1/engineering-systems/${id}/status`, signal),
  create: (data: any): Promise<any> => getApiClient().post('/api/v1/engineering-systems', data),
};

export const commandPlanApi = {
  list: (signal?: AbortSignal): Promise<any[]> => getApiClient().get('/api/v1/command-plans', signal),
  getById: (id: string, signal?: AbortSignal): Promise<any> => getApiClient().get(`/api/v1/command-plans/${id}`, signal),
  cancel: (id: string): Promise<any> => getApiClient().post(`/api/v1/command-plans/${id}/cancel`),
};

export const resourceApi = {
  list: (signal?: AbortSignal): Promise<any[]> => getApiClient().get('/api/v1/resources', signal),
};