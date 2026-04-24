import { useState, useCallback } from 'react';
import type { StorageFileRef } from '../types';

interface OneDrivePickItem {
  id: string;
  name: string;
  size?: number;
  parentReference?: { driveId?: string };
  '@sharePoint.endpoint'?: string;
}

interface PickerPortCommand {
  command: string;
  resource?: string;
  items?: OneDrivePickItem[];
}

interface PickerPortMessage {
  type: string;
  id: string;
  data: PickerPortCommand;
}

interface UseOneDrivePickerOptions {
  tenantUrl: string;
  clientId: string;
  multiSelect?: boolean;
}

export function useOneDrivePicker({
  tenantUrl,
  clientId: _clientId,
  multiSelect = true,
}: UseOneDrivePickerOptions) {
  const [selectedFiles, setSelectedFiles] = useState<StorageFileRef[]>([]);
  const [isOpen, setIsOpen] = useState(false);

  const openPicker = useCallback(async () => {
    setIsOpen(true);
    const channelId = crypto.randomUUID();
    const options = {
      sdk: '8.0',
      entry: { oneDrive: {} },
      authentication: {},
      messaging: { origin: window.location.origin, channelId },
      typesAndSources: {
        filters: ['pdf', 'docx'],
        mode: multiSelect ? 'multiple' : 'single',
      },
    };

    let win: Window | null = null;
    let port: MessagePort | null = null;

    const handlePortMessage = async (event: MessageEvent<PickerPortMessage>) => {
      const payload = event.data;
      if (payload.type === 'command') {
        port?.postMessage({ type: 'acknowledge', id: payload.id });
        const cmd = payload.data;
        if (cmd.command === 'authenticate') {
          // Delegated auth — return empty token; picker handles auth via its own UI
          port?.postMessage({
            type: 'result',
            id: payload.id,
            data: { result: 'token', token: '' },
          });
        } else if (cmd.command === 'pick') {
          const picked: StorageFileRef[] = (cmd.items ?? []).map((item) => ({
            providerType: 'OneDrive' as const,
            fileId: `${item.parentReference?.driveId ?? ''}:${item.id}`,
            fileName: item.name,
            fileSizeBytes: item.size ?? 0,
            containerOrDrive: item['@sharePoint.endpoint'],
          }));
          setSelectedFiles(picked);
          setIsOpen(false);
          port?.postMessage({ type: 'result', id: payload.id, data: { result: 'success' } });
          window.removeEventListener('message', handleMessage);
          win?.close();
        } else if (cmd.command === 'close') {
          setIsOpen(false);
          window.removeEventListener('message', handleMessage);
          win?.close();
        }
      }
    };

    // eslint-disable-next-line prefer-const
    function handleMessage(event: MessageEvent<{ type?: string; channelId?: string }>) {
      if (!win || event.source !== win) return;
      const msg = event.data;
      if (msg.type === 'initialize' && msg.channelId === channelId) {
        port = event.ports[0];
        port.addEventListener('message', handlePortMessage as unknown as EventListener);
        port.start();
        port.postMessage({ type: 'activate' });
      }
    }

    window.addEventListener('message', handleMessage);

    const queryString = new URLSearchParams({
      filePicker: JSON.stringify(options),
      locale: 'en-us',
    });
    const url = `${tenantUrl}/_layouts/15/FilePicker.aspx?${queryString.toString()}`;
    win = window.open('', 'Picker', 'width=1080,height=680');
    if (!win) {
      setIsOpen(false);
      window.removeEventListener('message', handleMessage);
      return;
    }

    const form = win.document.createElement('form');
    form.setAttribute('action', url);
    form.setAttribute('method', 'POST');
    win.document.body.appendChild(form);
    form.submit();
  }, [tenantUrl, multiSelect]);

  const clearSelection = useCallback(() => setSelectedFiles([]), []);

  return { openPicker, selectedFiles, isOpen, clearSelection };
}
