import apiClient from './client';

export const getUsers = () =>
  apiClient.get('/identity/admin/users').then((r) => r.data);

/** @param {{ name: string, email: string, password: string }} data */
export const createClaimsSpecialist = (data) =>
  apiClient.post('/identity/admin/create-claims-specialist', data).then((r) => r.data);

/** @param {{ name: string, email: string, password: string }} data */
export const createSupportSpecialist = (data) =>
  apiClient.post('/identity/admin/create-support-specialist', data).then((r) => r.data);

/**
 * @param {string} userId
 * @param {{ isActive: boolean }} data
 */
export const updateUserStatus = (userId, data) =>
  apiClient.patch(`/identity/admin/users/${userId}/status`, data).then((r) => r.data);
