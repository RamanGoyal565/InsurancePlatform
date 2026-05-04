import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getNotifications, markNotificationRead } from '../api/notifications';

export function useNotifications() {
  return useQuery({ queryKey: ['notifications'], queryFn: getNotifications, refetchInterval: 30000 });
}

export function useMarkNotificationRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ notificationId, isRead }) => markNotificationRead(notificationId, isRead),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['notifications'] }),
  });
}
