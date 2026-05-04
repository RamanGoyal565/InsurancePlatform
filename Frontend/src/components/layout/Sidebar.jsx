import PropTypes from 'prop-types';
import { NavLink } from 'react-router-dom';
import {
  Box, Drawer, List, ListItem, ListItemButton, ListItemIcon, ListItemText,
  Typography, Avatar, Divider,
} from '@mui/material';
import DashboardIcon from '@mui/icons-material/Dashboard';
import PeopleIcon from '@mui/icons-material/People';
import PolicyIcon from '@mui/icons-material/Policy';
import ConfirmationNumberIcon from '@mui/icons-material/ConfirmationNumber';
import PaymentIcon from '@mui/icons-material/Payment';
import NotificationsIcon from '@mui/icons-material/Notifications';
import AssessmentIcon from '@mui/icons-material/Assessment';
import SearchIcon from '@mui/icons-material/Search';
import { useAuth } from '../../app/AuthContext';
import { ROLES } from '../../app/roles';
import BrandLogo from '../ui/BrandLogo';

const NAV_BY_ROLE = {
  [ROLES.ADMIN]: [
    { label: 'Dashboard', icon: <DashboardIcon />, to: '/admin/dashboard' },
    { label: 'Users & Roles', icon: <PeopleIcon />, to: '/admin/users' },
    { label: 'Policies', icon: <PolicyIcon />, to: '/admin/policies' },
    { label: 'Tickets', icon: <ConfirmationNumberIcon />, to: '/specialist/tickets' },
    { label: 'Payments', icon: <PaymentIcon />, to: '/admin/payments' },
    { label: 'Notifications', icon: <NotificationsIcon />, to: '/notifications' },
    { label: 'Admin Reports', icon: <AssessmentIcon />, to: '/admin/reports' },
  ],
  [ROLES.CUSTOMER]: [
    { label: 'Dashboard', icon: <DashboardIcon />, to: '/dashboard' },
    { label: 'Browse Policies', icon: <SearchIcon />, to: '/browse-policies' },
    { label: 'My Policies', icon: <PolicyIcon />, to: '/my-policies' },
    { label: 'Tickets & Claims', icon: <ConfirmationNumberIcon />, to: '/tickets' },
    { label: 'Payments', icon: <PaymentIcon />, to: '/payments' },
    { label: 'Notifications', icon: <NotificationsIcon />, to: '/notifications' },
  ],
  [ROLES.CLAIMS_SPECIALIST]: [
    { label: 'Dashboard', icon: <DashboardIcon />, to: '/specialist/dashboard' },
    { label: 'Notifications', icon: <NotificationsIcon />, to: '/notifications' },
  ],
  [ROLES.SUPPORT_SPECIALIST]: [
    { label: 'Dashboard', icon: <DashboardIcon />, to: '/specialist/dashboard' },
    { label: 'Notifications', icon: <NotificationsIcon />, to: '/notifications' },
  ],
};

function DrawerContent() {
  const { user } = useAuth();
  const navItems = NAV_BY_ROLE[user?.role] || [];

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
      {/* Logo */}
      <Box sx={{ p: 2.5, borderBottom: '1px solid', borderColor: 'divider' }}>
        <BrandLogo size="md" dark />
      </Box>

      <Divider />

      {/* Nav */}
      <List sx={{ flexGrow: 1, px: 1, py: 1 }}>
        {navItems.map((item) => (
          <ListItem key={item.to} disablePadding sx={{ mb: 0.5 }}>
            <ListItemButton
              component={NavLink}
              to={item.to}
              sx={{
                borderRadius: 2,
                '&.active': { bgcolor: 'primary.main', color: '#fff', '& .MuiListItemIcon-root': { color: '#fff' } },
                '&:hover': { bgcolor: 'primary.50' },
              }}
            >
              <ListItemIcon sx={{ minWidth: 36 }}>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: 14, fontWeight: 500 }} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>

      <Divider />

      {/* User info */}
      {user && (
        <Box sx={{ p: 2, display: 'flex', alignItems: 'center', gap: 1.5 }}>
          <Avatar sx={{ width: 36, height: 36, bgcolor: 'primary.main', fontSize: 14 }}>
            {user.name?.slice(0, 2).toUpperCase()}
          </Avatar>
          <Box sx={{ minWidth: 0 }}>
            <Typography variant="body2" fontWeight={600} noWrap>{user.name}</Typography>
            <Typography variant="caption" color="text.secondary" noWrap>{user.role}</Typography>
          </Box>
        </Box>
      )}
    </Box>
  );
}

export default function Sidebar({ drawerWidth, mobileOpen, onClose }) {
  return (
    <>
      {/* Mobile drawer */}
      <Drawer
        variant="temporary"
        open={mobileOpen}
        onClose={onClose}
        ModalProps={{ keepMounted: true }}
        sx={{ display: { xs: 'block', sm: 'none' }, '& .MuiDrawer-paper': { width: drawerWidth } }}
      >
        <DrawerContent />
      </Drawer>

      {/* Desktop drawer */}
      <Drawer
        variant="permanent"
        sx={{
          display: { xs: 'none', sm: 'block' },
          '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box' },
        }}
        open
      >
        <DrawerContent />
      </Drawer>
    </>
  );
}

Sidebar.propTypes = {
  drawerWidth: PropTypes.number.isRequired,
  mobileOpen: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
};
