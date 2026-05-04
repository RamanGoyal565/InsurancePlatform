import apiClient from './client';

export const getNotifications = () =>
  apiClient.get('/notifications').then((r) => r.data);

export const markNotificationRead = (notificationId, isRead = true) =>
  apiClient.post(`/notifications/${notificationId}/read`, { isRead }).then((r) => r.data);
