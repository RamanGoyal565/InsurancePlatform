import { useNavigate } from 'react-router-dom';
import { Box, Button, Typography } from '@mui/material';
import LockIcon from '@mui/icons-material/Lock';

export default function UnauthorizedPage() {
  const navigate = useNavigate();
  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 2 }}>
      <LockIcon sx={{ fontSize: 64, color: 'error.main' }} />
      <Typography variant="h4" fontWeight={700}>Access Denied</Typography>
      <Typography color="text.secondary">You don&apos;t have permission to view this page.</Typography>
      <Button variant="contained" onClick={() => navigate(-1)}>Go Back</Button>
    </Box>
  );
}
