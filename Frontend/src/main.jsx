import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';

// MSW mocking removed so the app always uses the real backend.
createRoot(document.getElementById('root')).render(
  <StrictMode>
    <App />
  </StrictMode>
);
