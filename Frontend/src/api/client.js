/**
 * Axios instance with JWT interceptors.
 * Access token stored in memory (not localStorage) for XSS mitigation.
 * The backend issues a 120-minute JWT — no refresh endpoint exists.
 * On 401 the user is redirected to login.
 */
import axios from 'axios';

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

/** In-memory access token store */
let _accessToken = null;

export const tokenStore = {
  get: () => _accessToken,
  set: (token) => { _accessToken = token; },
  clear: () => { _accessToken = null; },
};

const apiClient = axios.create({
  baseURL: BASE_URL,
  withCredentials: false, // no cookie-based refresh; token is in-memory only
  headers: { 'Content-Type': 'application/json' },
});

// Attach Authorization header on every request
apiClient.interceptors.request.use((config) => {
  const token = tokenStore.get();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// On 401 clear token and force re-login
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      tokenStore.clear();
      window.dispatchEvent(new Event('auth:logout'));
    }
    return Promise.reject(error);
  }
);

export default apiClient;
