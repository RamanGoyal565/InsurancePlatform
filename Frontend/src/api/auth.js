import apiClient from './client';

/** @param {{ email: string, password: string }} credentials */
export const login = (credentials) =>
  apiClient.post('/identity/auth/login', credentials).then((r) => r.data);

/** @param {{ name: string, email: string, password: string }} data */
export const register = (data) =>
  apiClient.post('/identity/auth/register', data).then((r) => r.data);

// Backend has no logout endpoint — token is stateless JWT, just clear client-side
export const logout = () => Promise.resolve();
