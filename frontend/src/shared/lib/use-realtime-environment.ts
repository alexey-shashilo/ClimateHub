import { useEffect, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { queryKeys } from './query-keys';
import { SseRealtimeEventSource } from './sse-realtime';
import type { RealtimeEventSource } from './realtime';

let globalSource: RealtimeEventSource | null = null;

function getEventSource(): RealtimeEventSource {
  if (!globalSource) {
    const baseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';
    globalSource = new SseRealtimeEventSource(`${baseUrl}/api/v1/events/all`);
  }
  return globalSource;
}

let globalConnection: Promise<void> | null = null;

export function useRealtimeEnvironment(roomId?: string) {
  const queryClient = useQueryClient();
  const subscribedRef = useRef(false);

  useEffect(() => {
    if (!roomId || import.meta.env.VITE_ENABLE_MOCKS === 'true') return;
    if (subscribedRef.current) return;
    subscribedRef.current = true;

    const source = getEventSource();

    if (!globalConnection) {
      globalConnection = source.connect().catch(() => {
        globalConnection = null;
      });
    }

    const unsubscribe = source.subscribe((event) => {
      if (event.roomId !== roomId) return;

      // Invalidate environment cache for this room — triggers refetch
      queryClient.invalidateQueries({
        queryKey: queryKeys.rooms.environment(roomId),
        refetchType: 'active',
      });
    });

    return () => {
      unsubscribe();
    };
  }, [roomId, queryClient]);
}