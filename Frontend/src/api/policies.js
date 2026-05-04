import apiClient from './client';

export const getPolicies = () =>
  apiClient.get('/policies').then((r) => r.data);

export const getCustomerPolicies = () =>
  apiClient.get('/customer-policies').then((r) => r.data);

export const getPolicyDocument = (policyId) =>
  apiClient.get(`/policies/${policyId}/document`).then((r) => r.data);

/** @param {{ name, vehicleType, premium, coverageDetails, terms, policyDocument }} data */
export const createPolicy = (data) =>
  apiClient.post('/policies', data).then((r) => r.data);

/** @param {string} policyId */
export const updatePolicy = (policyId, data) =>
  apiClient.put(`/policies/${policyId}`, data).then((r) => r.data);

/** @param {{ policyId, vehicleNumber, drivingLicenseNumber }} data */
export const purchasePolicy = (data) =>
  apiClient.post('/purchase', data).then((r) => r.data);

export const renewPolicy = (customerPolicyId) =>
  apiClient.post(`/customer-policies/${customerPolicyId}/renew`).then((r) => r.data);
