import apiClient from './client';

const fmt = (d) => (d ? d.toISOString() : undefined);

export const getDashboard = () =>
  apiClient.get('/admin/dashboard').then((r) => r.data);

export const getTicketReports = (from, to) =>
  apiClient.get('/admin/reports/tickets', { params: { fromUtc: fmt(from), toUtc: fmt(to) } }).then((r) => r.data);

export const getClaimReports = (from, to) =>
  apiClient.get('/admin/reports/claims', { params: { fromUtc: fmt(from), toUtc: fmt(to) } }).then((r) => r.data);

export const getRevenueReports = (from, to) =>
  apiClient.get('/admin/reports/revenue', { params: { fromUtc: fmt(from), toUtc: fmt(to) } }).then((r) => r.data);

export const getPolicyReports = (from, to) =>
  apiClient.get('/admin/reports/policies', { params: { fromUtc: fmt(from), toUtc: fmt(to) } }).then((r) => r.data);

export const getPolicyCustomersReport = (policyId, from, to) =>
  apiClient.get(`/admin/reports/policies/${policyId}/customers`, { params: { fromUtc: fmt(from), toUtc: fmt(to) } }).then((r) => r.data);

export const getUserReports = (from, to) =>
  apiClient.get('/admin/reports/users', { params: { fromUtc: fmt(from), toUtc: fmt(to) } }).then((r) => r.data);

export const getPerformanceReports = (from, to) =>
  apiClient.get('/admin/reports/performance', { params: { fromUtc: fmt(from), toUtc: fmt(to) } }).then((r) => r.data);
