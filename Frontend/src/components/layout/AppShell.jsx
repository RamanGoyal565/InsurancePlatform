import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Box } from '@mui/material';
import Sidebar from './Sidebar';
import TopBar from './TopBar';
import ChatbotWidget from '../ui/ChatbotWidget';
import { useAuth } from '../../app/AuthContext';
import { ROLES } from '../../app/roles';

const DRAWER_WIDTH = 240;

export default function AppShell() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const { user } = useAuth();

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: 'background.default' }}>
      <Sidebar
        drawerWidth={DRAWER_WIDTH}
        mobileOpen={mobileOpen}
        onClose={() => setMobileOpen(false)}
      />
      <Box
        component="main"
        sx={{
          flexGrow: 1,
          display: 'flex',
          flexDirection: 'column',
          ml: { sm: `${DRAWER_WIDTH}px` },
          minWidth: 0,
        }}
      >
        <TopBar drawerWidth={DRAWER_WIDTH} onMenuClick={() => setMobileOpen(true)} />
        <Box sx={{ flexGrow: 1, p: { xs: 2, md: 3 }, mt: '64px' }}>
          <Outlet />
        </Box>
      </Box>

      {/* AI Chatbot — visible to customers only */}
      {user?.role === ROLES.CUSTOMER && <ChatbotWidget />}
    </Box>
  );
}
