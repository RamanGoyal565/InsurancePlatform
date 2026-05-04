import { useQuery } from '@tanstack/react-query';
import {
  getClaimReports, getDashboard, getPerformanceReports,
  getPolicyCustomersReport, getPolicyReports, getRevenueReports,
  getTicketReports, getUserReports,
} from '../api/reports';

export function useDashboard() {
  return useQuery({ queryKey: ['dashboard'], queryFn: getDashboard });
}

export function useTicketReports(from, to) {
  return useQuery({
    queryKey: ['ticket-reports', from, to],
    queryFn: () => getTicketReports(from, to),
  });
}

export function useClaimReports(from, to) {
  return useQuery({
    queryKey: ['claim-reports', from, to],
    queryFn: () => getClaimReports(from, to),
  });
}

export function useRevenueReports(from, to) {
  return useQuery({
    queryKey: ['revenue-reports', from, to],
    queryFn: () => getRevenueReports(from, to),
  });
}

export function usePolicyReports(from, to) {
  return useQuery({
    queryKey: ['policy-reports', from, to],
    queryFn: () => getPolicyReports(from, to),
  });
}

export function usePolicyCustomersReport(policyId, from, to) {
  return useQuery({
    queryKey: ['policy-customers-report', policyId, from, to],
    queryFn: () => getPolicyCustomersReport(policyId, from, to),
    enabled: !!policyId,
  });
}

export function useUserReports(from, to) {
  return useQuery({
    queryKey: ['user-reports', from, to],
    queryFn: () => getUserReports(from, to),
  });
}

export function usePerformanceReports(from, to) {
  return useQuery({
    queryKey: ['performance-reports', from, to],
    queryFn: () => getPerformanceReports(from, to),
  });
}
