import { createContext, useCallback, useContext, useState, type ReactNode } from 'react';
import { Snackbar } from '@/components/ui';
import MuiAlert from '@mui/material/Alert';

type Severity = 'success' | 'error' | 'warning' | 'info';

interface Toast {
  message: string;
  severity: Severity;
  key: number;
}

interface ToastContextValue {
  success: (message: string) => void;
  error: (message: string) => void;
  warning: (message: string) => void;
  info: (message: string) => void;
}

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toast, setToast] = useState<Toast | null>(null);
  const [open, setOpen] = useState(false);

  const show = useCallback((message: string, severity: Severity) => {
    setToast({ message, severity, key: Date.now() });
    setOpen(true);
  }, []);

  const success = useCallback((message: string) => show(message, 'success'), [show]);
  const error   = useCallback((message: string) => show(message, 'error'),   [show]);
  const warning = useCallback((message: string) => show(message, 'warning'), [show]);
  const info    = useCallback((message: string) => show(message, 'info'),    [show]);

  return (
    <ToastContext.Provider value={{ success, error, warning, info }}>
      {children}
      <Snackbar
        key={toast?.key}
        open={open}
        autoHideDuration={4000}
        onClose={() => setOpen(false)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <MuiAlert
          onClose={() => setOpen(false)}
          severity={toast?.severity ?? 'info'}
          variant="filled"
          sx={{ width: '100%', minWidth: 280 }}
        >
          {toast?.message}
        </MuiAlert>
      </Snackbar>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used inside ToastProvider');
  return ctx;
}
