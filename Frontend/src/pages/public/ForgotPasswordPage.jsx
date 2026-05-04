import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Alert, Box, Button, Card, CardContent, IconButton,
  InputAdornment, TextField, Typography, CircularProgress,
} from '@mui/material';
import EmailIcon from '@mui/icons-material/Email';
import LockResetIcon from '@mui/icons-material/LockReset';
import VisibilityIcon from '@mui/icons-material/Visibility';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import { requestOtp, verifyOtp, resetPassword } from '../../api/otp';

const STEPS = ['Enter Email', 'Verify OTP', 'New Password'];

export default function ForgotPasswordPage() {
  const navigate = useNavigate();
  const [step, setStep] = useState(0); // 0=email, 1=otp, 2=password, 3=done
  const [email, setEmail] = useState('');
  const [otp, setOtp] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPwd, setShowPwd] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [resendCooldown, setResendCooldown] = useState(0);

  const startCooldown = () => {
    setResendCooldown(60);
    const interval = setInterval(() => {
      setResendCooldown((prev) => {
        if (prev <= 1) { clearInterval(interval); return 0; }
        return prev - 1;
      });
    }, 1000);
  };

  const handleRequestOtp = async () => {
    if (!email.trim()) { setError('Please enter your email address.'); return; }
    setError('');
    setLoading(true);
    try {
      await requestOtp({ email: email.trim(), purpose: 'ForgotPassword' });
      setStep(1);
      startCooldown();
    } catch (e) {
      setError(e.response?.data?.message || 'Failed to send OTP. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyOtp = async () => {
    if (otp.length !== 6) { setError('Please enter the 6-digit OTP.'); return; }
    setError('');
    setLoading(true);
    try {
      const res = await verifyOtp({ email: email.trim(), code: otp, purpose: 'ForgotPassword' });
      if (res.success) {
        setStep(2);
      } else {
        setError(res.message || 'Invalid OTP. Please try again.');
      }
    } catch (e) {
      setError(e.response?.data?.message || 'OTP verification failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async () => {
    if (newPassword.length < 8) { setError('Password must be at least 8 characters.'); return; }
    if (newPassword !== confirmPassword) { setError('Passwords do not match.'); return; }
    setError('');
    setLoading(true);
    try {
      const res = await resetPassword({ email: email.trim(), code: otp, newPassword });
      if (res.success) {
        setStep(3);
      } else {
        setError(res.message || 'Password reset failed.');
      }
    } catch (e) {
      setError(e.response?.data?.message || 'Password reset failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    if (resendCooldown > 0) return;
    setError('');
    setLoading(true);
    try {
      await requestOtp({ email: email.trim(), purpose: 'ForgotPassword' });
      startCooldown();
    } catch {
      setError('Failed to resend OTP.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'background.default', p: 2 }}>
      <Card sx={{ width: '100%', maxWidth: 420 }}>
        <CardContent sx={{ p: 4 }}>

          {/* Step indicator */}
          <Box sx={{ display: 'flex', justifyContent: 'center', gap: 1, mb: 3 }}>
            {STEPS.map((label, i) => (
              <Box key={label} sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                <Box
                  sx={{
                    width: 28, height: 28, borderRadius: '50%', display: 'flex',
                    alignItems: 'center', justifyContent: 'center', fontSize: 12, fontWeight: 700,
                    bgcolor: i < step ? 'success.main' : i === step ? 'primary.main' : 'grey.300',
                    color: i <= step ? '#fff' : 'text.secondary',
                  }}
                >
                  {i < step ? '✓' : i + 1}
                </Box>
                {i < STEPS.length - 1 && (
                  <Box sx={{ width: 24, height: 2, bgcolor: i < step ? 'success.main' : 'grey.300' }} />
                )}
              </Box>
            ))}
          </Box>

          {/* Step 0: Email */}
          {step === 0 && (
            <>
              <Typography variant="h5" fontWeight={700} mb={0.5}>Forgot Password</Typography>
              <Typography variant="body2" color="text.secondary" mb={3}>
                Enter your registered email and we&apos;ll send you a 6-digit OTP.
              </Typography>
              {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
              <TextField
                label="Email Address"
                fullWidth
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleRequestOtp()}
                InputProps={{ startAdornment: <EmailIcon sx={{ mr: 1, color: 'text.secondary' }} /> }}
                sx={{ mb: 2 }}
              />
              <Button variant="contained" fullWidth size="large" onClick={handleRequestOtp} disabled={loading}>
                {loading ? <CircularProgress size={22} color="inherit" /> : 'Send OTP'}
              </Button>
            </>
          )}

          {/* Step 1: OTP */}
          {step === 1 && (
            <>
              <Typography variant="h5" fontWeight={700} mb={0.5}>Enter OTP</Typography>
              <Typography variant="body2" color="text.secondary" mb={3}>
                A 6-digit OTP has been sent to <strong>{email}</strong>. It expires in 10 minutes.
              </Typography>
              {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
              <TextField
                label="6-Digit OTP"
                fullWidth
                value={otp}
                onChange={(e) => setOtp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                onKeyDown={(e) => e.key === 'Enter' && handleVerifyOtp()}
                inputProps={{ maxLength: 6, style: { letterSpacing: 8, fontSize: 22, textAlign: 'center' } }}
                sx={{ mb: 2 }}
              />
              <Button variant="contained" fullWidth size="large" onClick={handleVerifyOtp} disabled={loading || otp.length !== 6}>
                {loading ? <CircularProgress size={22} color="inherit" /> : 'Verify OTP'}
              </Button>
              <Box sx={{ mt: 2, textAlign: 'center' }}>
                <Button
                  size="small"
                  onClick={handleResend}
                  disabled={resendCooldown > 0 || loading}
                  color="secondary"
                >
                  {resendCooldown > 0 ? `Resend OTP in ${resendCooldown}s` : 'Resend OTP'}
                </Button>
              </Box>
            </>
          )}

          {/* Step 2: New Password */}
          {step === 2 && (
            <>
              <Typography variant="h5" fontWeight={700} mb={0.5}>Set New Password</Typography>
              <Typography variant="body2" color="text.secondary" mb={3}>
                OTP verified. Enter your new password below.
              </Typography>
              {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
              <TextField
                label="New Password"
                type={showPwd ? 'text' : 'password'}
                fullWidth
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                sx={{ mb: 2 }}
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
                label="Confirm New Password"
                type="password"
                fullWidth
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleResetPassword()}
                sx={{ mb: 2 }}
              />
              <Button
                variant="contained"
                fullWidth
                size="large"
                startIcon={<LockResetIcon />}
                onClick={handleResetPassword}
                disabled={loading}
              >
                {loading ? <CircularProgress size={22} color="inherit" /> : 'Reset Password'}
              </Button>
            </>
          )}

          {/* Step 3: Done */}
          {step === 3 && (
            <Box sx={{ textAlign: 'center' }}>
              <CheckCircleIcon sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
              <Typography variant="h5" fontWeight={700} mb={1}>Password Reset!</Typography>
              <Typography variant="body2" color="text.secondary" mb={3}>
                Your password has been reset successfully. You can now log in with your new password.
              </Typography>
              <Button variant="contained" fullWidth size="large" onClick={() => navigate('/login')}>
                Go to Login
              </Button>
            </Box>
          )}

          {step < 3 && (
            <Button component={Link} to="/login" fullWidth sx={{ mt: 2 }}>
              Back to Login
            </Button>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
