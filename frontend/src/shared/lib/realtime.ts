export type RealtimeEventHandler = (event: RealtimeEvent) => void;

export interface RealtimeEvent {
  type: string;
  roomId?: string;
  deviceId?: string;
  timestamp?: string;
  data?: unknown;
}

export type Unsubscribe = () => void;

export interface RealtimeEventSource {
  connect(): Promise<void>;
  disconnect(): Promise<void>;
  subscribe(handler: RealtimeEventHandler): Unsubscribe;
}

export class NoopRealtimeEventSource implements RealtimeEventSource {
  async connect() { /* no-op */ }
  async disconnect() { /* no-op */ }
  subscribe(_handler: RealtimeEventHandler): Unsubscribe {
    return () => { /* no-op */ };
  }
}