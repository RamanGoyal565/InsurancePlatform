import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createPolicy, getCustomerPolicies, getPolicies, getPolicyDocument,
  purchasePolicy, renewPolicy, updatePolicy,
} from '../api/policies';
import { useToast } from '../app/ToastContext';

export function usePolicies() {
  return useQuery({ queryKey: ['policies'], queryFn: getPolicies });
}

export function useCustomerPolicies() {
  return useQuery({ queryKey: ['customer-policies'], queryFn: getCustomerPolicies });
}

/**
 * Returns a mutate function — call it with policyId to fetch and download the PDF.
 * Using mutation (not query) so it only fires on user action, not on mount.
 */
export function useDownloadPolicyDocument() {
  const toast = useToast();
  return useMutation({
    mutationFn: getPolicyDocument,
    onSuccess: (data) => {
      if (!data?.document) { toast.error('No document available for this policy'); return; }
      const link = document.createElement('a');
      link.href = `data:application/pdf;base64,${data.document}`;
      link.download = `${data.name?.replace(/\s+/g, '_') || 'policy'}.pdf`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    },
    onError: () => toast.error('Failed to download policy document'),
  });
}

export function useCreatePolicy() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: createPolicy,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['policies'] }); toast.success('Policy created'); },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to create policy'),
  });
}

export function useUpdatePolicy() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: ({ policyId, ...data }) => updatePolicy(policyId, data),
    onSuccess: (_, { policyId }) => {
      // Invalidate the policy list AND the cached document so the viewer shows the new PDF
      qc.invalidateQueries({ queryKey: ['policies'] });
      qc.invalidateQueries({ queryKey: ['policy-document', policyId] });
      toast.success('Policy updated');
    },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to update policy'),
  });
}

export function usePurchasePolicy() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: purchasePolicy,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['customer-policies'] }); toast.success('Policy purchased successfully'); },
    onError: (e) => toast.error(e.response?.data?.message || 'Purchase failed'),
  });
}

export function useRenewPolicy() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: renewPolicy,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['customer-policies'] }); toast.success('Policy renewal initiated'); },
    onError: (e) => toast.error(e.response?.data?.message || 'Renewal failed'),
  });
}
