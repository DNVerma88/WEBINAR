import { useEffect, useRef } from 'react';
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { tokenStorage } from '../../../shared/api/axiosClient';
import type { ScreeningProgressEvent } from '../types';

export function useScreeningProgress(
  jobId: string | null,
  onProgress: (event: ScreeningProgressEvent) => void,
  onCompleted: (jobId: string) => void,
  onFailed: (jobId: string, error: string) => void,
) {
  const handlersRef = useRef({ onProgress, onCompleted, onFailed });
  handlersRef.current = { onProgress, onCompleted, onFailed };

  useEffect(() => {
    if (!jobId) return;

    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
        accessTokenFactory: () => tokenStorage.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('ReceiveScreeningProgress', (event: ScreeningProgressEvent) => {
      if (event.jobId === jobId) {
        handlersRef.current.onProgress(event);
      }
    });

    connection.on('ScreeningJobCompleted', (completedJobId: string) => {
      if (completedJobId === jobId) {
        handlersRef.current.onCompleted(completedJobId);
      }
    });

    connection.on('ScreeningJobFailed', (failedJobId: string, error: string) => {
      if (failedJobId === jobId) {
        handlersRef.current.onFailed(failedJobId, error);
      }
    });

    connection.start().catch((err) => console.warn('[SignalR] Screening connection failed:', err));

    return () => {
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.stop().catch(() => undefined);
      }
    };
  }, [jobId]);
}
