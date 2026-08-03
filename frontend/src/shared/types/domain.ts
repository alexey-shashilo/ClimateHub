export type BuildingStatus = 'normal' | 'warning' | 'critical' | 'offline' | 'unknown';

export interface BuildingSummary {
  id: string;
  name: string;
  status: BuildingStatus;
  floorsCount: number;
  roomsCount: number;
  roomsWithWarnings: number;
  devicesOnline: number;
  devicesOffline: number;
  updatedAt: string;
}

export interface FloorSummary {
  id: string;
  buildingId: string;
  name: string;
  number: number;
  rooms: RoomSummary[];
}

export type RoomStatus = 'normal' | 'warning' | 'critical' | 'offline' | 'stale' | 'unknown';

export interface RoomSummary {
  id: string;
  floorId: string;
  name: string;
  type?: string;
  status: RoomStatus;
  environment: RoomEnvironmentSummary;
  activeWarningsCount: number;
  devicesOnline: number;
  devicesTotal: number;
  updatedAt: string;
}

export interface RoomEnvironmentSummary {
  temperatureC?: number;
  relativeHumidityPct?: number;
  co2Ppm?: number;
  illuminanceLux?: number;
  quality?: MeasurementQuality;
}

export type MeasurementQuality =
  | 'valid'
  | 'estimated'
  | 'stale'
  | 'outOfRange'
  | 'sensorError'
  | 'rejected'
  | 'unknown';

export interface EnvironmentTarget {
  minimum?: number;
  maximum?: number;
  preferred?: number;
}

export interface EnvironmentParameter {
  value: number;
  unit: string;
  measuredAt: string;
  receivedAt: string;
  quality: MeasurementQuality;
  sourceDeviceId: string;
  target?: EnvironmentTarget;
}

export interface RoomEnvironmentResponse {
  roomId: string;
  roomName: string;
  status: RoomStatus;
  parameters: {
    temperature?: EnvironmentParameter;
    relativeHumidity?: EnvironmentParameter;
    co2?: EnvironmentParameter;
    illuminance?: EnvironmentParameter;
  };
  updatedAt: string;
}

export type EnvironmentParameterCode = 'temperature' | 'humidity' | 'co2' | 'illuminance';

export type HistoryAggregation = 'raw' | '1m' | '15m' | '1h';

export interface EnvironmentHistoryPoint {
  timestamp: string;
  value: number;
  quality: MeasurementQuality;
}

export interface EnvironmentHistoryResponse {
  roomId: string;
  parameter: EnvironmentParameterCode;
  unit: string;
  from: string;
  to: string;
  aggregation: HistoryAggregation;
  points: EnvironmentHistoryPoint[];
}

export type DeviceConnectivityStatus = 'online' | 'offline' | 'suspected' | 'synchronizing' | 'operational';

export type DeviceStatus = 'registered' | 'active' | 'inactive' | 'decommissioned';

export interface DeviceCapability {
  code: string;
  kind: 'measurement' | 'control' | 'system';
  unit?: string;
  minimum?: number;
  maximum?: number;
  writable: boolean;
  available: boolean;
}

export interface DeviceSummary {
  id: string;
  hardwareId: string;
  name: string;
  model: string;
  deviceType: string;
  status: DeviceStatus;
  connectivity: DeviceConnectivityStatus;
  firmwareVersion?: string;
  lastSeenAt?: string;
  assignedRoomId?: string;
  assignedRoomName?: string;
  capabilities: DeviceCapability[];
}

export interface RegisterDeviceRequest {
  hardwareId: string;
  name: string;
  model: string;
  deviceType: string;
  protocolVersion: string;
  roomId?: string;
  capabilityCodes: string[];
}

export interface ApiProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  errorCode?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export type DataFreshness = 'fresh' | 'aging' | 'stale' | 'unknown';

export type NeedStatus = 'detected' | 'planning' | 'planned' | 'executing' | 'waitingForEffect' | 'satisfied' | 'blocked' | 'cancelled' | 'expired';
export type NeedSeverity = 'low' | 'medium' | 'high' | 'critical';
export type NeedType = 'temperatureHeating' | 'temperatureCooling' | 'humidityIncrease' | 'humidityDecrease' | 'co2Reduction' | 'illuminanceIncrease' | 'illuminanceDecrease';
export type ControlMode = 'monitorOnly' | 'manual' | 'automatic' | 'disabled';

export interface NeedDto {
  id: string;
  type: string;
  severity: string;
  status: string;
  desiredMin: number;
  desiredMax: number;
  desiredPreferred: number;
  currentValue?: number;
  deviation: number;
  sourceParameterCode?: string;
  roomId: string;
  selectedCapabilityCode?: string;
  activeCommandId?: string;
  selectedDeviceId?: string;
  planningFailureCode?: string | null;
  controlMode?: ControlMode;
  violationSince?: string;
  stableSince?: string;
  cooldownUntil?: string;
  effectEvaluationDueAt?: string;
  lastCommandCreatedAt?: string;
  lastMeaningfulImprovementAt?: string;
  planningAttemptCount?: number;
  commandAttemptCount?: number;
  lastCommandId?: string;
  createdAt: string;
  updatedAt?: string;
  resolvedAt?: string;
  version: number;
}

export interface NeedEvaluationDto {
  id: number;
  needId: string;
  trigger: string;
  previousStatus?: string;
  newStatus?: string;
  outcome?: string;
  failureCode?: string;
  commandId?: string;
  evaluatedAt: string;
  correlationId?: string;
  causationId?: string;
}

export interface PolicyDimension {
  minimum?: number;
  maximum?: number;
  preferred?: number;
  controlMode?: ControlMode;
}

export interface PolicyDto {
  temperature?: PolicyDimension;
  humidity?: PolicyDimension;
  co2?: PolicyDimension;
  illuminance?: PolicyDimension;
}

export interface EngineeringSystemDto {
  id: string;
  buildingId: string;
  name: string;
  systemType: string;
  lifecycle: string;
  operationalStatus: string;
  controlMode: string;
  priority: number;
  description?: string;
  capabilities: SystemCapabilityDto[];
  resources: EngineeringResourceDto[];
  zoneCount: number;
  deviceCount: number;
  version: number;
}

export interface SystemCapabilityDto {
  code: string;
  dataType?: string;
  unit?: string;
  minimum?: number;
  maximum?: number;
  supportsModulation: boolean;
}

export interface EngineeringResourceDto {
  code: string;
  unit?: string;
  maximum: number;
  available: number;
  reserved: number;
  used: number;
  priority: number;
}

export interface SystemZoneDto {
  id: string;
  name: string;
  priority: number;
  roomIds: string[];
}

export interface DeviceBindingDto {
  id: string;
  deviceId: string;
  role: string;
  priority: number;
  enabled: boolean;
}

export interface CommandPlanDto {
  id: string;
  engineeringSystemId: string;
  needType: string;
  capabilityCode: string;
  status: string;
  requestedValue: number;
  valueUnit?: string;
  strategyName?: string;
  steps: CommandPlanStepDto[];
  resourceAllocations: ResourceAllocationDto[];
  createdAt: string;
  completedAt?: string;
  failureCode?: string;
  version: number;
}

export interface CommandPlanStepDto {
  capabilityCode: string;
  operation: string;
  requestedValue: number;
  valueUnit?: string;
  deviceId?: string;
  sequence: number;
  deviceRole?: string;
  status: string;
}

export interface ResourceAllocationDto {
  resourceCode: string;
  amount: number;
}