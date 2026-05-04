import { createContext, useCallback, useContext, useState } from 'react';
import PropTypes from 'prop-types';
import { Alert, Snackbar, Stack } from '@mui/material';

const ToastContext = createContext(null);

let _id = 0;

export function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([]);

  const addToast = useCallback((message, severity = 'info') => {
    const id = ++_id;
    setToasts((prev) => [...prev, { id, message, severity }]);
    setTimeout(() => setToasts((prev) => prev.filter((t) => t.id !== id)), 4000);
  }, []);

  const toast = {
    success: (msg) => addToast(msg, 'success'),
    error: (msg) => addToast(msg, 'error'),
    info: (msg) => addToast(msg, 'info'),
    warning: (msg) => addToast(msg, 'warning'),
  };

  return (
    <ToastContext.Provider value={toast}>
      {children}
      <Stack spacing={1} sx={{ position: 'fixed', bottom: 24, right: 24, zIndex: 9999 }}>
        {toasts.map((t) => (
          <Snackbar key={t.id} open anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}>
            <Alert severity={t.severity} variant="filled" sx={{ minWidth: 280 }}>
              {t.message}
            </Alert>
          </Snackbar>
        ))}
      </Stack>
    </ToastContext.Provider>
  );
}

ToastProvider.propTypes = { children: PropTypes.node.isRequired };

export function useToast() {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within ToastProvider');
  return ctx;
}
