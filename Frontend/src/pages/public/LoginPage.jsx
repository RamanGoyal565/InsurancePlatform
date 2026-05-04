import { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  Box, Button, Card, CardContent, Divider, IconButton,
  InputAdornment, TextField, Typography, Alert,
} from '@mui/material';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import LockIcon from '@mui/icons-material/Lock';
import SecurityIcon from '@mui/icons-material/Security';
import { useAuth } from '../../app/AuthContext';
import { ROLES } from '../../app/roles';
import BrandLogo from '../../components/ui/BrandLogo';

const schema = yup.object({
  email: yup.string().email('Invalid email').required('Email is required'),
  password: yup.string().min(8, 'Min 8 characters').required('Password is required'),
});

export default function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [showPwd, setShowPwd] = useState(false);
  const [error, setError] = useState('');

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm({
    resolver: yupResolver(schema),
  });

  const onSubmit = async (data) => {
    setError('');
    try {
      const user = await login(data);
      const from = location.state?.from?.pathname;
      if (from) { navigate(from, { replace: true }); return; }
      if (user.role === ROLES.ADMIN) navigate('/admin/dashboard');
      else if (user.role === ROLES.CUSTOMER) navigate('/dashboard');
      else navigate('/specialist/dashboard');
    } catch (e) {
      setError(e.response?.data?.message || 'Invalid email or password');
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex' }}>
      {/* Left panel — car image with dark overlay */}
      <Box
        sx={{
          flex: 1,
          display: { xs: 'none', md: 'flex' },
          flexDirection: 'column',
          justifyContent: 'center',
          alignItems: 'center',
          position: 'relative',
          overflow: 'hidden',
          p: 6,
          color: 'white',
          // Car image
          backgroundImage: `url('/car-hero.jpg')`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
        }}
      >
        {/* Dark overlay so text stays readable */}
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            background: 'linear-gradient(160deg, rgba(0,32,69,0.88) 0%, rgba(13,71,161,0.75) 100%)',
            pointerEvents: 'none',
          }}
        />
        {/* Content sits above the overlay */}
        <Box sx={{ position: 'relative', zIndex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          {/* Large shield brand mark */}
          <Box sx={{ mb: 3 }}>
            <Box
              component="svg"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 24 24"
              sx={{ width: 80, height: 80, opacity: 0.95 }}
              aria-hidden="true"
            >
              <path d="M12 2 L3 6.5 V12 C3 16.9 7 21.5 12 23 C17 21.5 21 16.9 21 12 V6.5 Z" fill="#ffffff" />
              <path d="M9 12.5 L11 14.5 L15 10.5" stroke="#002045" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" fill="none" />
            </Box>
          </Box>
          <Typography variant="h4" fontWeight={800} textAlign="center" mb={2}>
            Secure Today,<br />Protected Tomorrow
          </Typography>
          <Typography textAlign="center" sx={{ opacity: 0.8, maxWidth: 360 }}>
            Comprehensive insurance solutions for you, your family, and your future.
          </Typography>
          <Box sx={{ mt: 6, display: 'flex', gap: 4 }}>
            {[
              { icon: <SecurityIcon />, label: 'Reliable Coverage' },
              { icon: <LockIcon />, label: 'Quick Claims' },
              { icon: <LockIcon />, label: '24/7 Support' },
            ].map((item, i) => (
              <Box key={i} sx={{ textAlign: 'center' }}>
                {item.icon}
                <Typography variant="caption" display="block" mt={0.5}>{item.label}</Typography>
              </Box>
            ))}
          </Box>
        </Box>
      </Box>

      {/* Right panel */}
      <Box
        sx={{
          flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center',
          p: { xs: 2, md: 6 }, bgcolor: 'background.default',
        }}
      >
        <Card sx={{ width: '100%', maxWidth: 420 }}>
          <CardContent sx={{ p: 4 }}>
            <Typography variant="h5" fontWeight={700} mb={0.5}>Welcome back!</Typography>
            <Typography variant="body2" color="text.secondary" mb={3}>
              Sign in to access your Trust Guard dashboard
            </Typography>

            {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

            <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
              <TextField
                label="Email Address"
                fullWidth
                margin="normal"
                {...register('email')}
                error={!!errors.email}
                helperText={errors.email?.message}
                autoComplete="email"
              />
              <TextField
                label="Password"
                type={showPwd ? 'text' : 'password'}
                fullWidth
                margin="normal"
                {...register('password')}
                error={!!errors.password}
                helperText={errors.password?.message}
                autoComplete="current-password"
                InputProps={{
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton onClick={() => setShowPwd(!showPwd)} edge="end">
                        {showPwd ? <VisibilityOffIcon /> : <VisibilityIcon />}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
              />

              <Box sx={{ textAlign: 'right', mb: 2 }}>
                <Link to="/forgot-password" style={{ fontSize: 13, color: '#1565C0' }}>
                  Forgot password?
                </Link>
              </Box>

              <Button
                type="submit"
                variant="contained"
                fullWidth
                size="large"
                disabled={isSubmitting}
                startIcon={<LockIcon />}
              >
                {isSubmitting ? 'Signing in...' : 'Sign In'}
              </Button>
            </Box>

            <Divider sx={{ my: 2 }}>or</Divider>

            <Button variant="outlined" fullWidth component={Link} to="/">
              Back to Home
            </Button>

            <Typography variant="caption" color="text.secondary" display="block" textAlign="center" mt={2}>
              Don&apos;t have an account?{' '}
              <Link to="/register" style={{ color: '#1565C0' }}>Create Account</Link>
            </Typography>

            <Box sx={{ mt: 2, p: 1.5, bgcolor: 'grey.50', borderRadius: 2, textAlign: 'center' }}>
              <SecurityIcon sx={{ fontSize: 14, mr: 0.5, color: 'text.secondary' }} />
              <Typography variant="caption" color="text.secondary">
                Your security is our priority. All data is protected with enterprise-grade encryption.
              </Typography>
            </Box>
          </CardContent>
        </Card>
      </Box>
    </Box>
  );
}
