import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  addComment, approveClaim, assignTicket, createTicket,
  getTicketById, getTicketComments, getTickets, rejectClaim, updateTicketStatus,
} from '../api/tickets';
import { useToast } from '../app/ToastContext';

export function useTickets() {
  return useQuery({ queryKey: ['tickets'], queryFn: getTickets });
}

/** Fetch a single ticket by ID — works for all roles, bypasses list filter */
export function useTicket(ticketId) {
  return useQuery({
    queryKey: ['ticket', ticketId],
    queryFn: () => getTicketById(ticketId),
    enabled: !!ticketId,
  });
}

export function useTicketComments(ticketId) {
  return useQuery({
    queryKey: ['ticket-comments', ticketId],
    queryFn: () => getTicketComments(ticketId),
    enabled: !!ticketId,
  });
}

export function useCreateTicket() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: createTicket,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['tickets'] }); toast.success('Ticket submitted'); },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to submit ticket'),
  });
}

export function useUpdateTicketStatus() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: ({ ticketId, status }) => updateTicketStatus(ticketId, { status }),
    onSuccess: (_, { ticketId }) => {
      qc.invalidateQueries({ queryKey: ['tickets'] });
      qc.invalidateQueries({ queryKey: ['ticket', ticketId] });
      toast.success('Status updated');
    },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to update status'),
  });
}

export function useAssignTicket() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: ({ ticketId, assignedToUserId }) => assignTicket(ticketId, { assignedToUserId }),
    onSuccess: (_, { ticketId }) => {
      qc.invalidateQueries({ queryKey: ['tickets'] });
      qc.invalidateQueries({ queryKey: ['ticket', ticketId] });
      toast.success('Ticket assigned');
    },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to assign ticket'),
  });
}

export function useAddComment() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: ({ ticketId, message, documentBase64, documentFileName }) =>
      addComment(ticketId, { message, documentBase64, documentFileName }),
    onSuccess: (_, { ticketId }) => {
      qc.invalidateQueries({ queryKey: ['ticket-comments', ticketId] });
    },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to add comment'),
  });
}

export function useApproveClaim() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: approveClaim,
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['tickets'] });
      if (data?.ticketId) qc.invalidateQueries({ queryKey: ['ticket', data.ticketId] });
      toast.success('Claim approved');
    },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to approve claim'),
  });
}

export function useRejectClaim() {
  const qc = useQueryClient();
  const toast = useToast();
  return useMutation({
    mutationFn: rejectClaim,
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['tickets'] });
      if (data?.ticketId) qc.invalidateQueries({ queryKey: ['ticket', data.ticketId] });
      toast.success('Claim rejected');
    },
    onError: (e) => toast.error(e.response?.data?.message || 'Failed to reject claim'),
  });
}
