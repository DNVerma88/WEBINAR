import { useEffect, useRef } from 'react';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { useQueryClient } from '@tanstack/react-query';
import { tokenStorage } from '../api/axiosClient';
import type { NotificationDto } from '../types';

/**
 * Establishes a SignalR connection to the notification hub and listens for
 * real-time "ReceiveNotification" events.
 *
 * On receiving an event the hook:
 *  1. Invalidates the ['notifications'] React Query cache so the bell badge
 *     and the notifications page refresh automatically.
 *  2. Calls the optional `onNotification` callback so callers can show a toast.
 *
 * Token is passed via query-string so the WebSocket upgrade carries auth
 * (Authorization header is not forwarded by browsers on WS upgrade).
 */
export function useNotificationHub(
  onNotification?: (notification: NotificationDto) => void,
) {
  const queryClient = useQueryClient();
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const token = tokenStorage.getAccessToken();
    if (!token) return;

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => tokenStorage.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('ReceiveNotification', (notification: NotificationDto) => {
      // Refresh unread count badge and notification list
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      onNotification?.(notification);
    });

    connection
      .start()
      .catch((err) => console.warn('[SignalR] Connection failed:', err));

    connectionRef.current = connection;

    return () => {
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop().catch(() => undefined);
      }
    };
    // onNotification is intentionally excluded: callers should memoize it if
    // they need stable identity; re-creating the WS on every render is wrong.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [queryClient]);

  return connectionRef;
}
