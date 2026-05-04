import apiClient from './client';

/** Admin: get all payments */
export const getPayments = () =>
  apiClient.get('/payments').then((r) => r.data);

/** Customer: get their own payments */
export const getMyPayments = () =>
  apiClient.get('/payments/my').then((r) => r.data);

/** @param {{ customerId, policyId?, amount }} data */
export const createPayment = (data) =>
  apiClient.post('/payments', data).then((r) => r.data);
