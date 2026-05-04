import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createPayment, getMyPayments, getPayments } from '../api/payments';
import { useToast } from '../app/ToastContext';
import { useAuth } from '../app/AuthContext';
import { ROLES } from '../app/roles';

/**
 * Returns payments for the current user:
 * - Admin → GET /payments (all payments)
 * - Customer → GET /payments/my (their own payments only)
 */
export function usePayments() {
  const { user } = useAuth();
  const isAdmin = user?.role === ROLES.ADMIN;
  return useQuery({
    queryKey: ['payments', user?.role],
    queryFn: isAdmin ? getPayments : getMyPayments,
    enabled: !!user,
  });
}

export function useCreatePayment() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: createPayment,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['payments'] }); toast.success('Payment processed'); },
    onError: (e) => toast.error(e.response?.data?.message || 'Payment failed'),
  });
}
