import apiClient from './client';

export const getTickets = () =>
  apiClient.get('/tickets').then((r) => r.data);

export const getTicketById = (ticketId) =>
  apiClient.get(`/tickets/${ticketId}`).then((r) => r.data);

export const getTicketComments = (ticketId) =>
  apiClient.get(`/tickets/${ticketId}/comments`).then((r) => r.data);

/**
 * @param {{ title, description, type, policyId?, claimAmount?, documentBase64?, documentFileName? }} data
 */
export const createTicket = (data) =>
  apiClient.post('/tickets', data).then((r) => r.data);

export const updateTicketStatus = (ticketId, data) =>
  apiClient.put(`/tickets/${ticketId}/status`, data).then((r) => r.data);

export const assignTicket = (ticketId, data) =>
  apiClient.put(`/tickets/${ticketId}/assign`, data).then((r) => r.data);

/**
 * @param {string} ticketId
 * @param {{ message: string, documentBase64?: string, documentFileName?: string }} data
 */
export const addComment = (ticketId, data) =>
  apiClient.post(`/tickets/${ticketId}/comments`, data).then((r) => r.data);

export const approveClaim = (ticketId) =>
  apiClient.post(`/tickets/${ticketId}/approve`).then((r) => r.data);

export const rejectClaim = (ticketId) =>
  apiClient.post(`/tickets/${ticketId}/reject`).then((r) => r.data);
