import { z } from 'zod';

export const measurementQualitySchema = z.enum(['valid', 'estimated', 'stale', 'outOfRange', 'sensorError', 'rejected', 'unknown', 'unknown'] as const);

export const environmentTargetSchema = z.object({
  minimum: z.number().optional(),
  maximum: z.number().optional(),
  preferred: z.number().optional(),
});

export const environmentParameterSchema = z.object({
  value: z.number(),
  unit: z.string(),
  measuredAt: z.string(),
  receivedAt: z.string(),
  quality: measurementQualitySchema,
  sourceDeviceId: z.string(),
  target: environmentTargetSchema.optional(),
});

export const roomEnvironmentResponseSchema = z.object({
  roomId: z.string(),
  roomName: z.string(),
  status: z.enum(['normal', 'warning', 'critical', 'offline', 'stale', 'unknown'] as const),
  parameters: z.object({
    temperature: environmentParameterSchema.optional(),
    relativeHumidity: environmentParameterSchema.optional(),
    co2: environmentParameterSchema.optional(),
    illuminance: environmentParameterSchema.optional(),
  }),
  updatedAt: z.string(),
});

export const environmentHistoryPointSchema = z.object({
  timestamp: z.string(),
  value: z.number(),
  quality: measurementQualitySchema,
});

export const environmentHistoryResponseSchema = z.object({
  roomId: z.string(),
  parameter: z.enum(['temperature', 'humidity', 'co2'] as const),
  unit: z.string(),
  from: z.string(),
  to: z.string(),
  aggregation: z.enum(['raw', '1m', '15m', '1h'] as const),
  points: z.array(environmentHistoryPointSchema),
});

export const deviceCapabilitySchema = z.object({
  code: z.string(),
  kind: z.enum(['measurement', 'control', 'system'] as const),
  unit: z.string().optional(),
  minimum: z.number().optional(),
  maximum: z.number().optional(),
  writable: z.boolean(),
  available: z.boolean(),
});

export const deviceSummarySchema = z.object({
  id: z.string(),
  hardwareId: z.string(),
  name: z.string(),
  model: z.string(),
  deviceType: z.string(),
  status: z.enum(['registered', 'active', 'inactive', 'decommissioned'] as const),
  connectivity: z.enum(['online', 'offline', 'suspected', 'synchronizing', 'operational'] as const),
  firmwareVersion: z.string().optional(),
  lastSeenAt: z.string().optional(),
  assignedRoomId: z.string().optional(),
  assignedRoomName: z.string().optional(),
  capabilities: z.array(deviceCapabilitySchema),
});

export const registerDeviceRequestSchema = z.object({
  hardwareId: z.string().min(1, 'Hardware ID is required').max(200),
  name: z.string().min(1, 'Device name is required').max(200),
  model: z.string().min(1, 'Model is required'),
  deviceType: z.string().min(1, 'Device type is required'),
  protocolVersion: z.string().regex(/^\d+\.\d+$/, 'Protocol version must be like 1.0'),
  roomId: z.string().optional(),
  capabilityCodes: z.array(z.string()).min(1, 'At least one capability is required'),
});

export const needDtoSchema = z.object({
  id: z.string(),
  type: z.string(),
  severity: z.string(),
  status: z.string(),
  desiredMin: z.number(),
  desiredMax: z.number(),
  desiredPreferred: z.number(),
  currentValue: z.number().optional(),
  deviation: z.number(),
  sourceParameterCode: z.string().optional(),
  roomId: z.string(),
  selectedCapabilityCode: z.string().optional(),
  activeCommandId: z.string().optional(),
  selectedDeviceId: z.string().optional(),
  planningFailureCode: z.string().optional().nullable(),
  createdAt: z.string(),
  updatedAt: z.string().optional().nullable(),
  resolvedAt: z.string().optional().nullable(),
  version: z.number(),
});

export const apiProblemDetailsSchema = z.object({
  type: z.string().optional(),
  title: z.string(),
  status: z.number(),
  detail: z.string().optional(),
  instance: z.string().optional(),
  errorCode: z.string().optional(),
  traceId: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
});