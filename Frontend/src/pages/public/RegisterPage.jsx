import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import {
  Box, Button, Card, CardContent, Checkbox, FormControlLabel,
  IconButton, InputAdornment, TextField, Typography, Alert,
} from '@mui/material';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import { useAuth } from '../../app/AuthContext';

const schema = yup.object({
  name: yup.string().min(2, 'Min 2 characters').required('Full name is required'),
  email: yup.string().email('Invalid email').required('Email is required'),
  password: yup.string().min(8, 'Min 8 characters').required('Password is required'),
  confirmPassword: yup.string()
    .oneOf([yup.ref('password')], 'Passwords must match')
    .required('Please confirm your password'),
  terms: yup.bool().oneOf([true], 'You must accept the terms'),
});

export default function RegisterPage() {
  const { register: authRegister } = useAuth();
  const navigate = useNavigate();
  const [showPwd, setShowPwd] = useState(false);
  const [error, setError] = useState('');

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm({
    resolver: yupResolver(schema),
  });

  const onSubmit = async ({ name, email, password }) => {
    setError('');
    try {
      await authRegister({ name, email, password });
      // After registration, send OTP for email verification
      try {
        const { requestOtp } = await import('../../api/otp');
        await requestOtp({ email, purpose: 'EmailVerification' });
        navigate('/verify-email', { state: { email } });
      } catch {
        // OTP send failed — still go to dashboard, verification can be done later
        navigate('/dashboard');
      }
    } catch (e) {
      setError(e.response?.data?.message || 'Registration failed. Please try again.');
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh', display: 'flex', alignItems: 'center',
        justifyContent: 'center', bgcolor: 'background.default', p: 2,
      }}
    >
      <Card sx={{ width: '100%', maxWidth: 440 }}>
        <CardContent sx={{ p: 4 }}>
          <Typography variant="h5" fontWeight={700} mb={0.5}>Create Account</Typography>
          <Typography variant="body2" color="text.secondary" mb={3}>
            Join us and get the best coverage
          </Typography>

          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} noValidate>
            <TextField
              label="Full Name"
              fullWidth margin="normal"
              {...register('name')}
              error={!!errors.name}
              helperText={errors.name?.message}
            />
            <TextField
              label="Email Address"
              fullWidth margin="normal"
              {...register('email')}
              error={!!errors.email}
              helperText={errors.email?.message}
            />
            <TextField
              label="Password"
              type={showPwd ? 'text' : 'password'}
              fullWidth margin="normal"
              {...register('password')}
              error={!!errors.password}
              helperText={errors.password?.message}
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
            <TextField
              label="Confirm Password"
              type="password"
              fullWidth margin="normal"
              {...register('confirmPassword')}
              error={!!errors.confirmPassword}
              helperText={errors.confirmPassword?.message}
            />

            <FormControlLabel
              control={<Checkbox {...register('terms')} />}
              label={
                <Typography variant="caption">
                  I agree to the{' '}
                  <Link to="/terms" style={{ color: '#1565C0' }}>Terms of Service</Link>
                  {' '}and{' '}
                  <Link to="/privacy" style={{ color: '#1565C0' }}>Privacy Policy</Link>
                </Typography>
              }
            />
            {errors.terms && (
              <Typography variant="caption" color="error" display="block">
                {errors.terms.message}
              </Typography>
            )}

            <Button
              type="submit"
              variant="contained"
              fullWidth
              size="large"
              sx={{ mt: 2 }}
              disabled={isSubmitting}
              startIcon={<PersonAddIcon />}
            >
              {isSubmitting ? 'Creating account...' : 'Create Account'}
            </Button>
          </Box>

          <Typography variant="caption" color="text.secondary" display="block" textAlign="center" mt={2}>
            Already have an account?{' '}
            <Link to="/login" style={{ color: '#1565C0' }}>Sign In</Link>
          </Typography>
        </CardContent>
      </Card>
    </Box>
  );
}
