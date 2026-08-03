import type { RealtimeEventHandler, RealtimeEventSource, Unsubscribe } from './realtime';

export class SseRealtimeEventSource implements RealtimeEventSource {
  private url: string;
  private eventSource: EventSource | null = null;
  private handlers: Set<RealtimeEventHandler> = new Set();

  constructor(baseUrl: string) {
    this.url = baseUrl;
  }

  async connect(): Promise<void> {
    if (this.eventSource) return;
    return new Promise((resolve, reject) => {
      const es = new EventSource(this.url);
      es.onopen = () => resolve();
      es.onerror = (err) => reject(err);

      es.addEventListener('environment.updated', (event: MessageEvent) => {
        try {
          const data = JSON.parse(event.data);
          const parsed = {
            type: 'environment.updated',
            roomId: data.roomId,
            timestamp: data.timestamp,
            data,
          };
          this.handlers.forEach(h => h(parsed));
        } catch { /* skip malformed messages */ }
      });

      this.eventSource = es;
    });
  }

  async disconnect(): Promise<void> {
    this.eventSource?.close();
    this.eventSource = null;
  }

  subscribe(handler: RealtimeEventHandler): Unsubscribe {
    this.handlers.add(handler);
    return () => { this.handlers.delete(handler); };
  }
}