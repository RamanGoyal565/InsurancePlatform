import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createClaimsSpecialist, createSupportSpecialist, getUsers, updateUserStatus } from '../api/users';
import { useToast } from '../app/ToastContext';

export function useUsers() {
  return useQuery({ queryKey: ['users'], queryFn: getUsers });
}

export function useCreateClaimsSpecialist() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: createClaimsSpecialist,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['users'] }); toast.success('Claims specialist created'); },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to create user'),
  });
}

export function useCreateSupportSpecialist() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: createSupportSpecialist,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['users'] }); toast.success('Support specialist created'); },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to create user'),
  });
}

export function useUpdateUserStatus() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: ({ userId, isActive }) => updateUserStatus(userId, { isActive }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['users'] }); toast.success('User status updated'); },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to update status'),
  });
}
