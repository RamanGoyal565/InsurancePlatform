import {
  Box, Button, Card, CardContent, Chip, Divider, List, ListItem,
  ListItemText, Switch, Tab, Tabs, Typography,
} from '@mui/material';
import NotificationsIcon from '@mui/icons-material/Notifications';
import { useNotifications, useMarkNotificationRead } from '../../hooks/useNotifications';

export default function CustomerNotifications() {
  const { data: notifications = [], isLoading } = useNotifications();
  const markRead = useMarkNotificationRead();

  const unread = notifications.filter((n) => !n.isRead);
  const read = notifications.filter((n) => n.isRead);

  const markAllRead = () => {
    unread.forEach((n) => markRead.mutate({ notificationId: n.notificationId, isRead: true }));
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>Notifications</Typography>
          <Typography variant="body2" color="text.secondary">Stay updated with your policy and account activity</Typography>
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="body2">Unread only</Typography>
            <Switch size="small" />
          </Box>
          <Button size="small" onClick={markAllRead} disabled={unread.length === 0}>
            Mark all as read
          </Button>
        </Box>
      </Box>

      <Card>
        <CardContent>
          {isLoading ? (
            <Typography align="center" color="text.secondary">Loading...</Typography>
          ) : notifications.length === 0 ? (
            <Box sx={{ textAlign: 'center', py: 6 }}>
              <NotificationsIcon sx={{ fontSize: 48, color: 'text.disabled', mb: 1 }} />
              <Typography color="text.secondary">No notifications yet</Typography>
            </Box>
          ) : (
            <List disablePadding>
              {notifications.map((n, i) => (
                <Box key={n.notificationId}>
                  <ListItem
                    alignItems="flex-start"
                    sx={{
                      bgcolor: n.isRead ? 'transparent' : 'primary.50',
                      borderRadius: 1,
                      cursor: 'pointer',
                    }}
                    onClick={() => !n.isRead && markRead.mutate({ notificationId: n.notificationId, isRead: true })}
                  >
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Typography variant="body2" fontWeight={n.isRead ? 400 : 600}>
                            {n.message}
                          </Typography>
                          {!n.isRead && <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: 'primary.main' }} />}
                        </Box>
                      }
                      secondary={new Date(n.createdAt).toLocaleString()}
                    />
                  </ListItem>
                  {i < notifications.length - 1 && <Divider />}
                </Box>
              ))}
            </List>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
