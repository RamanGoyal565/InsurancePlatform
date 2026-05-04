import { createTheme } from '@mui/material/styles';

const theme = createTheme({
  palette: {
    primary: { main: '#1565C0' },
    secondary: { main: '#00897B' },
    background: { default: '#F5F7FA', paper: '#FFFFFF' },
  },
  typography: {
    fontFamily: '"Inter", "Roboto", "Helvetica", "Arial", sans-serif',
    h5: { fontWeight: 700 },
    h6: { fontWeight: 600 },
  },
  shape: { borderRadius: 10 },
  components: {
    MuiButton: {
      styleOverrides: {
        root: { textTransform: 'none', fontWeight: 600 },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: { boxShadow: '0 2px 12px rgba(0,0,0,0.08)' },
      },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: { borderRight: 'none', boxShadow: '2px 0 12px rgba(0,0,0,0.06)' },
      },
    },
  },
});

export default theme;
