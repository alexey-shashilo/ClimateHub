import { z } from 'zod';
import {
  roomEnvironmentResponseSchema,
  environmentHistoryResponseSchema,
  deviceSummarySchema,
  registerDeviceRequestSchema,
  apiProblemDetailsSchema,
} from './schemas';
import type { BuildingSummary, FloorSummary, RoomSummary, DeviceCapability, NeedDto } from '../types/domain';

export type RoomEnvironmentResponse = z.infer<typeof roomEnvironmentResponseSchema>;
export type EnvironmentHistoryResponse = z.infer<typeof environmentHistoryResponseSchema>;
export type DeviceSummary = z.infer<typeof deviceSummarySchema>;
export type RegisterDeviceRequest = z.infer<typeof registerDeviceRequestSchema>;
export type ApiProblemDetails = z.infer<typeof apiProblemDetailsSchema>;

export const schemas = {
  roomEnvironmentResponse: roomEnvironmentResponseSchema,
  environmentHistoryResponse: environmentHistoryResponseSchema,
  deviceSummary: deviceSummarySchema,
  registerDeviceRequest: registerDeviceRequestSchema,
  apiProblemDetails: apiProblemDetailsSchema,
};

export {
  BuildingSummary,
  FloorSummary,
  RoomSummary,
  DeviceCapability,
  NeedDto,
};