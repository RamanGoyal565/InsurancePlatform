import { useState } from 'react';
import {
  Alert, Box, Button, Card, CardContent, Chip, CircularProgress,
  Dialog, DialogContent, DialogTitle, Divider, Grid,
  IconButton, Step, StepLabel, Stepper, TextField, Typography,
} from '@mui/material';
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar';
import LocalShippingIcon from '@mui/icons-material/LocalShipping';
import TwoWheelerIcon from '@mui/icons-material/TwoWheeler';
import CheckIcon from '@mui/icons-material/Check';
import DescriptionIcon from '@mui/icons-material/Description';
import CloseIcon from '@mui/icons-material/Close';
import PaymentIcon from '@mui/icons-material/Payment';
import LoginIcon from '@mui/icons-material/Login';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { usePolicies, useCustomerPolicies } from '../../hooks/usePolicies';
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import PolicyDocumentViewer from '../../components/ui/PolicyDocumentViewer';
import PaymentReceipt from '../../components/ui/PaymentReceipt';
import { useAuth } from '../../app/AuthContext';
import { useToast } from '../../app/ToastContext';
import { createRazorpayOrder, loadRazorpayScript, verifyRazorpayPayment } from '../../api/razorpay';
import { purchasePolicy } from '../../api/policies';

/** Split coverage — semicolon-separated only: "Item one;Item two;Item three" */
const splitCoverage = (str) =>
  str ? str.split(';').map((s) => s.trim()).filter(Boolean) : [];

// Active statuses that block a duplicate vehicle purchase
const ACTIVE_STATUSES = new Set([1, 2, 3]); // Pending=1, Active=2, Renewed=3

const VEHICLE_ICONS = {
  1: <DirectionsCarIcon sx={{ fontSize: 48 }} />,
  2: <LocalShippingIcon sx={{ fontSize: 48 }} />,
  3: <TwoWheelerIcon sx={{ fontSize: 48 }} />,
};
const VEHICLE_LABELS = { 1: 'Car', 2: 'Truck', 3: 'Bike' };
const VEHICLE_COLORS = { 1: '#E3F2FD', 2: '#E8F5E9', 3: '#FFF3E0' };
const VEHICLE_BORDER  = { 1: '#90CAF9', 2: '#A5D6A7', 3: '#FFCC80' };

const schema = yup.object({
  vehicleNumber: yup.string().required('Vehicle number is required'),
  drivingLicenseNumber: yup.string().required('Driving license is required'),
});

export default function BrowsePolicies() {
  const { data: policies = [], isLoading } = usePolicies();
  const { data: myPolicies = [] } = useCustomerPolicies();
  const { user } = useAuth();
  const navigate = useNavigate();
  const toast = useToast();
  const qc = useQueryClient();

  // Only authenticated customers can purchase
  const isCustomer = user?.role === 'Customer';

  const [selected, setSelected] = useState(null);
  const [step, setStep] = useState(0);
  const [vehicleData, setVehicleData] = useState(null);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [viewDoc, setViewDoc] = useState(null);
  const [receipt, setReceipt] = useState(null);
  const [vehicleError, setVehicleError] = useState('');

  const { register, handleSubmit, reset, watch, formState: { errors } } = useForm({
    resolver: yupResolver(schema),
  });

  const handleClose = () => {
    setSelected(null);
    setStep(0);
    setVehicleData(null);
    setVehicleError('');
    reset();
  };

  const onDetailsSubmit = (data) => {
    // Frontend duplicate check — catches it before payment opens
    const normalized = data.vehicleNumber.trim().toUpperCase();
    const duplicate = myPolicies.find(
      (p) =>
        p.vehicleNumber?.trim().toUpperCase() === normalized &&
        ACTIVE_STATUSES.has(p.status)
    );
    if (duplicate) {
      setVehicleError(
        `Vehicle ${normalized} already has an active policy "${duplicate.policyName}". ` +
        `A vehicle cannot be covered by more than one active policy at a time.`
      );
      return;
    }
    setVehicleError('');
    setVehicleData(data);
    setStep(1);
  };

  const handleRazorpayPayment = async () => {
    if (!selected || !vehicleData) return;
    setPaymentLoading(true);
    try {
      await loadRazorpayScript();
      const order = await createRazorpayOrder({
        customerId: user.userId,
        policyId: selected.policyId,
        amount: selected.premium,
        description: `${selected.name} — Annual Premium`,
      });
      const options = {
        key: order.keyId,
        amount: Math.round(selected.premium * 100),
        currency: order.currency,
        name: 'Trust Guard',
        description: `${selected.name} — Annual Premium`,
        order_id: order.orderId,
        prefill: { name: user.name, email: user.email },
        theme: { color: '#002045' },
        handler: async (response) => {
          try {
            await verifyRazorpayPayment({
              customerId: user.userId,
              policyId: selected.policyId,
              razorpayOrderId: response.razorpay_order_id,
              razorpayPaymentId: response.razorpay_payment_id,
              razorpaySignature: response.razorpay_signature,
              amount: selected.premium,
            });
            await purchasePolicy({
              policyId: selected.policyId,
              vehicleNumber: vehicleData.vehicleNumber,
              drivingLicenseNumber: vehicleData.drivingLicenseNumber,
              paymentReference: response.razorpay_payment_id,
            });
            qc.invalidateQueries({ queryKey: ['customer-policies'] });
            qc.invalidateQueries({ queryKey: ['payments'] });
            handleClose();
            setReceipt({
              policyName: selected.name,
              vehicleNumber: vehicleData.vehicleNumber,
              drivingLicenseNumber: vehicleData.drivingLicenseNumber,
              amount: selected.premium,
              razorpayPaymentId: response.razorpay_payment_id,
              razorpayOrderId: response.razorpay_order_id,
            });
          } catch (err) {
            toast.error(err.response?.data?.message || 'Payment succeeded but policy activation failed.');
            setPaymentLoading(false);
          }
        },
        modal: { ondismiss: () => setPaymentLoading(false) },
      };
      const rzp = new window.Razorpay(options);
      rzp.on('payment.failed', (r) => { toast.error(`Payment failed: ${r.error.description}`); setPaymentLoading(false); });
      rzp.open();
    } catch (err) {
      toast.error(err.response?.data?.message || err.message || 'Failed to initiate payment.');
      setPaymentLoading(false);
    }
  };

  return (
    <Box>
      <Typography variant="h5" fontWeight={700} mb={0.5}>Browse Policies</Typography>
      <Typography variant="body2" color="text.secondary" mb={3}>
        Choose the best policy for your vehicle
      </Typography>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : policies.length === 0 ? (
        <Alert severity="info">No policies available at the moment.</Alert>
      ) : (
        <Grid container spacing={3}>
          {policies.map((p) => (
            <Grid item xs={12} sm={6} md={4} key={p.policyId}>
              <Card
                sx={{
                  height: '100%',
                  border: `1px solid ${VEHICLE_BORDER[p.vehicleType] || '#e0e0e0'}`,
                  bgcolor: VEHICLE_COLORS[p.vehicleType] || '#fafafa',
                  borderRadius: '8px',
                  boxShadow: 'none',
                  display: 'flex',
                  flexDirection: 'column',
                  transition: 'box-shadow 0.15s, transform 0.15s',
                  '&:hover': {
                    boxShadow: '0 4px 20px rgba(0,32,69,0.12)',
                    transform: 'translateY(-2px)',
                  },
                }}
              >
                <CardContent sx={{ p: 3, flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
                  {/* Icon + type chip */}
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                    <Box sx={{ color: '#002045' }}>{VEHICLE_ICONS[p.vehicleType]}</Box>
                    <Chip
                      label={VEHICLE_LABELS[p.vehicleType] || 'Vehicle'}
                      size="small"
                      sx={{
                        bgcolor: '#002045',
                        color: '#fff',
                        fontWeight: 600,
                        fontSize: 11,
                        letterSpacing: '0.04em',
                        borderRadius: '4px',
                        height: 22,
                      }}
                    />
                  </Box>

                  {/* Name */}
                  <Typography sx={{ fontSize: 16, fontWeight: 700, color: '#1a1c1e', mb: 0.5, lineHeight: 1.3 }}>
                    {p.name}
                  </Typography>

                  {/* Premium */}
                  <Typography sx={{ fontSize: 13, color: '#43474e', mb: 0.25 }}>Starting from</Typography>
                  <Typography sx={{ fontSize: 24, fontWeight: 800, color: '#002045', mb: 2, lineHeight: 1 }}>
                    ₹{p.premium?.toLocaleString()}
                    <Typography component="span" sx={{ fontSize: 13, fontWeight: 400, color: '#43474e' }}> / year</Typography>
                  </Typography>

                  {/* Coverage */}
                  <Box sx={{ flexGrow: 1, mb: 2.5 }}>
                    <Typography sx={{ fontSize: 12, fontWeight: 600, color: '#43474e', mb: 1, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                      Coverage Includes
                    </Typography>
                    {splitCoverage(p.coverageDetails).slice(0, 4).map((c, i) => (
                      <Box key={i} sx={{ display: 'flex', alignItems: 'center', gap: 0.75, mb: 0.5 }}>
                        <CheckIcon sx={{ fontSize: 13, color: '#1b5e20', flexShrink: 0 }} />
                        <Typography sx={{ fontSize: 13, color: '#1a1c1e', lineHeight: 1.4 }}>{c}</Typography>
                      </Box>
                    ))}
                  </Box>

                  {/* Actions */}
                  <Box sx={{ display: 'flex', gap: 1 }}>
                    <Button
                      variant="outlined"
                      size="small"
                      fullWidth
                      startIcon={<DescriptionIcon sx={{ fontSize: 15 }} />}
                      onClick={() => setViewDoc({ policyId: p.policyId, name: p.name })}
                      sx={{
                        borderColor: '#002045',
                        color: '#002045',
                        fontWeight: 600,
                        fontSize: 12,
                        textTransform: 'none',
                        borderRadius: '4px',
                        '&:hover': { bgcolor: 'rgba(0,32,69,0.06)' },
                      }}
                    >
                      View Doc
                    </Button>

                    {isCustomer ? (
                      <Button
                        variant="contained"
                        size="small"
                        fullWidth
                        onClick={() => { setSelected(p); setStep(0); }}
                        sx={{
                          bgcolor: '#e65100',
                          color: '#fff',
                          fontWeight: 700,
                          fontSize: 12,
                          textTransform: 'none',
                          borderRadius: '4px',
                          boxShadow: 'none',
                          '&:hover': { bgcolor: '#bf360c', boxShadow: 'none' },
                        }}
                      >
                        Buy Policy
                      </Button>
                    ) : (
                      <Button
                        variant="contained"
                        size="small"
                        fullWidth
                        startIcon={<LoginIcon sx={{ fontSize: 14 }} />}
                        onClick={() => navigate('/login')}
                        sx={{
                          bgcolor: '#002045',
                          color: '#fff',
                          fontWeight: 600,
                          fontSize: 12,
                          textTransform: 'none',
                          borderRadius: '4px',
                          boxShadow: 'none',
                          '&:hover': { bgcolor: '#1a365d', boxShadow: 'none' },
                        }}
                      >
                        Login to Buy
                      </Button>
                    )}
                  </Box>
                </CardContent>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      {/* PDF Viewer */}
      <PolicyDocumentViewer
        policyId={viewDoc?.policyId ?? null}
        policyName={viewDoc?.name}
        onClose={() => setViewDoc(null)}
      />

      {/* Payment Receipt */}
      <PaymentReceipt
        open={!!receipt}
        receipt={receipt}
        onClose={() => setReceipt(null)}
      />

      {/* Purchase Dialog */}
      <Dialog open={!!selected} onClose={handleClose} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pb: 1 }}>
          <Box>
            <Typography variant="h6" fontWeight={700}>{selected?.name}</Typography>
            <Typography variant="body2" color="text.secondary">₹{selected?.premium?.toLocaleString()} / year</Typography>
          </Box>
          <IconButton onClick={handleClose} size="small"><CloseIcon /></IconButton>
        </DialogTitle>
        <DialogContent>
          <Stepper activeStep={step} sx={{ mb: 3 }}>
            <Step><StepLabel>Vehicle Details</StepLabel></Step>
            <Step><StepLabel>Payment</StepLabel></Step>
          </Stepper>

          {step === 0 && (
            <Box component="form" onSubmit={handleSubmit(onDetailsSubmit)}>
              {/* Duplicate vehicle error */}
              {vehicleError && (
                <Alert
                  severity="error"
                  icon={<WarningAmberIcon />}
                  sx={{ mb: 2, borderRadius: '4px' }}
                >
                  {vehicleError}
                </Alert>
              )}
              <TextField
                label="Vehicle Number"
                fullWidth
                margin="normal"
                {...register('vehicleNumber')}
                error={!!errors.vehicleNumber || !!vehicleError}
                helperText={errors.vehicleNumber?.message}
                placeholder="e.g. DL01AB1234"
                onChange={() => vehicleError && setVehicleError('')}
              />
              <TextField label="Driving License Number" fullWidth margin="normal" {...register('drivingLicenseNumber')} error={!!errors.drivingLicenseNumber} helperText={errors.drivingLicenseNumber?.message} placeholder="e.g. DL1420150001234" />
              <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
                <Button onClick={handleClose} fullWidth variant="outlined">Cancel</Button>
                <Button type="submit" fullWidth variant="contained">Continue to Payment</Button>
              </Box>
            </Box>
          )}

          {step === 1 && (
            <Box>
              <Box sx={{ p: 2, bgcolor: 'grey.50', borderRadius: 2, mb: 3 }}>
                <Typography variant="subtitle2" fontWeight={600} mb={1}>Order Summary</Typography>
                {[['Policy', selected?.name], ['Vehicle', vehicleData?.vehicleNumber], ['License', vehicleData?.drivingLicenseNumber]].map(([l, v]) => (
                  <Box key={l} sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                    <Typography variant="body2" color="text.secondary">{l}</Typography>
                    <Typography variant="body2" fontWeight={600}>{v}</Typography>
                  </Box>
                ))}
                <Divider sx={{ my: 1 }} />
                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Typography variant="body1" fontWeight={700}>Total</Typography>
                  <Typography variant="body1" fontWeight={700} color="primary">₹{selected?.premium?.toLocaleString()}</Typography>
                </Box>
              </Box>
              <Alert severity="info" sx={{ mb: 2 }}>Your policy activates immediately after successful payment.</Alert>
              <Button variant="contained" fullWidth size="large" startIcon={paymentLoading ? <CircularProgress size={20} color="inherit" /> : <PaymentIcon />} onClick={handleRazorpayPayment} disabled={paymentLoading} sx={{ mb: 1 }}>
                {paymentLoading ? 'Opening Payment...' : `Pay ₹${selected?.premium?.toLocaleString()} via Razorpay`}
              </Button>
              <Button onClick={() => setStep(0)} fullWidth variant="text" disabled={paymentLoading}>← Back to Details</Button>
            </Box>
          )}
        </DialogContent>
      </Dialog>
    </Box>
  );
}
