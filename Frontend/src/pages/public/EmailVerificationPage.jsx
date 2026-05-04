import { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  Alert, Box, Button, Card, CardContent, CircularProgress, Typography,
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import MarkEmailReadIcon from '@mui/icons-material/MarkEmailRead';
import { requestOtp, verifyOtp } from '../../api/otp';

export default function EmailVerificationPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const email = location.state?.email || '';

  const [otp, setOtp] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [verified, setVerified] = useState(false);
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

  const handleVerify = async () => {
    if (otp.length !== 6) { setError('Please enter the 6-digit OTP.'); return; }
    setError('');
    setLoading(true);
    try {
      const res = await verifyOtp({ email, code: otp, purpose: 'EmailVerification' });
      if (res.success) {
        setVerified(true);
      } else {
        setError(res.message || 'Invalid OTP. Please try again.');
      }
    } catch (e) {
      setError(e.response?.data?.message || 'Verification failed.');
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    if (resendCooldown > 0) return;
    setError('');
    setLoading(true);
    try {
      await requestOtp({ email, purpose: 'EmailVerification' });
      startCooldown();
    } catch {
      setError('Failed to resend OTP.');
    } finally {
      setLoading(false);
    }
  };

  if (verified) {
    return (
      <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'background.default', p: 2 }}>
        <Card sx={{ width: '100%', maxWidth: 400 }}>
          <CardContent sx={{ p: 4, textAlign: 'center' }}>
            <CheckCircleIcon sx={{ fontSize: 64, color: 'success.main', mb: 2 }} />
            <Typography variant="h5" fontWeight={700} mb={1}>Email Verified!</Typography>
            <Typography variant="body2" color="text.secondary" mb={3}>
              Your email has been verified successfully. You can now access all features.
            </Typography>
            <Button variant="contained" fullWidth size="large" onClick={() => navigate('/dashboard')}>
              Go to Dashboard
            </Button>
          </CardContent>
        </Card>
      </Box>
    );
  }

  return (
    <Box sx={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', bgcolor: 'background.default', p: 2 }}>
      <Card sx={{ width: '100%', maxWidth: 420 }}>
        <CardContent sx={{ p: 4 }}>
          <Box sx={{ textAlign: 'center', mb: 3 }}>
            <MarkEmailReadIcon sx={{ fontSize: 56, color: 'primary.main', mb: 1 }} />
            <Typography variant="h5" fontWeight={700} mb={0.5}>Verify Your Email</Typography>
            <Typography variant="body2" color="text.secondary">
              A 6-digit OTP has been sent to <strong>{email}</strong>. Enter it below to verify your account.
            </Typography>
          </Box>

          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

          {/* OTP input boxes */}
          <Box sx={{ display: 'flex', gap: 1, justifyContent: 'center', mb: 3 }}>
            {Array.from({ length: 6 }).map((_, i) => (
              <Box
                key={i}
                component="input"
                type="text"
                inputMode="numeric"
                maxLength={1}
                value={otp[i] || ''}
                onChange={(e) => {
                  const val = e.target.value.replace(/\D/g, '');
                  const newOtp = otp.split('');
                  newOtp[i] = val;
                  setOtp(newOtp.join('').slice(0, 6));
                  if (val && e.target.nextSibling) e.target.nextSibling.focus();
                }}
                onKeyDown={(e) => {
                  if (e.key === 'Backspace' && !otp[i] && e.target.previousSibling)
                    e.target.previousSibling.focus();
                }}
                sx={{
                  width: 48, height: 56, textAlign: 'center', fontSize: 22, fontWeight: 700,
                  border: '2px solid', borderColor: otp[i] ? 'primary.main' : 'divider',
                  borderRadius: 2, outline: 'none', bgcolor: 'background.paper',
                  '&:focus': { borderColor: 'primary.main', boxShadow: '0 0 0 3px rgba(21,101,192,0.15)' },
                }}
              />
            ))}
          </Box>

          <Button
            variant="contained"
            fullWidth
            size="large"
            onClick={handleVerify}
            disabled={loading || otp.length !== 6}
            sx={{ mb: 1.5 }}
          >
            {loading ? <CircularProgress size={22} color="inherit" /> : 'Verify Email'}
          </Button>

          <Box sx={{ textAlign: 'center' }}>
            <Button
              size="small"
              onClick={handleResend}
              disabled={resendCooldown > 0 || loading}
              color="secondary"
            >
              {resendCooldown > 0 ? `Resend OTP in ${resendCooldown}s` : 'Resend OTP'}
            </Button>
          </Box>

          <Button onClick={() => navigate('/dashboard')} fullWidth sx={{ mt: 1 }} color="inherit">
            Skip for now
          </Button>
        </CardContent>
      </Card>
    </Box>
  );
}
