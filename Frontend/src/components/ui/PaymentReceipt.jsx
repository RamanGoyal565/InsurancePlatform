import PropTypes from 'prop-types';
import {
  Box, Button, Dialog, DialogContent, Divider, Typography,
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import DownloadIcon from '@mui/icons-material/Download';
import PolicyIcon from '@mui/icons-material/Policy';

/**
 * Payment receipt dialog shown after a successful Razorpay payment.
 */
export default function PaymentReceipt({ open, onClose, receipt }) {
  if (!receipt) return null;

  const handleDownload = () => {
    const lines = [
      '========================================',
      '           TRUST GUARD                  ',
      '         PAYMENT RECEIPT                ',
      '========================================',
      '',
      `Receipt No.    : ${receipt.razorpayPaymentId}`,
      `Date           : ${new Date().toLocaleString('en-IN')}`,
      '',
      '----------------------------------------',
      'POLICY DETAILS',
      '----------------------------------------',
      `Policy Name    : ${receipt.policyName}`,
      `Vehicle No.    : ${receipt.vehicleNumber}`,
      `License No.    : ${receipt.drivingLicenseNumber}`,
      `Coverage       : 1 Year`,
      '',
      '----------------------------------------',
      'PAYMENT DETAILS',
      '----------------------------------------',
      `Amount Paid    : ₹${receipt.amount?.toLocaleString('en-IN')}`,
      `Payment Method : Razorpay`,
      `Payment ID     : ${receipt.razorpayPaymentId}`,
      `Order ID       : ${receipt.razorpayOrderId}`,
      `Status         : PAID`,
      '',
      '========================================',
      'Thank you for choosing Trust Guard',
      'Your coverage is now active.',
      '========================================',
    ].join('\n');

    const blob = new Blob([lines], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `receipt_${receipt.razorpayPaymentId}.txt`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogContent sx={{ p: 0 }}>
        {/* Header */}
        <Box
          sx={{
            background: 'linear-gradient(135deg, #1565C0, #0D47A1)',
            p: 3,
            textAlign: 'center',
            color: '#fff',
          }}
        >
          <CheckCircleIcon sx={{ fontSize: 56, mb: 1 }} />
          <Typography variant="h6" fontWeight={700}>Payment Successful!</Typography>
          <Typography variant="body2" sx={{ opacity: 0.85 }}>
            Your policy is now active
          </Typography>
        </Box>

        {/* Receipt body */}
        <Box sx={{ p: 3 }}>
          {/* Amount */}
          <Box sx={{ textAlign: 'center', mb: 2 }}>
            <Typography variant="caption" color="text.secondary">Amount Paid</Typography>
            <Typography variant="h4" fontWeight={800} color="primary">
              ₹{receipt.amount?.toLocaleString('en-IN')}
            </Typography>
          </Box>

          <Divider sx={{ mb: 2 }} />

          {/* Policy details */}
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
            <PolicyIcon color="primary" />
            <Typography variant="subtitle2" fontWeight={600}>{receipt.policyName}</Typography>
          </Box>

          {[
            { label: 'Vehicle Number', value: receipt.vehicleNumber },
            { label: 'Driving License', value: receipt.drivingLicenseNumber },
            { label: 'Coverage Period', value: '1 Year' },
          ].map((row) => (
            <Box key={row.label} sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.75 }}>
              <Typography variant="body2" color="text.secondary">{row.label}</Typography>
              <Typography variant="body2" fontWeight={600}>{row.value}</Typography>
            </Box>
          ))}

          <Divider sx={{ my: 2 }} />

          {/* Payment details */}
          {[
            { label: 'Payment Method', value: 'Razorpay' },
            { label: 'Payment ID', value: receipt.razorpayPaymentId?.slice(0, 20) + '…' },
            { label: 'Date & Time', value: new Date().toLocaleString('en-IN') },
            { label: 'Status', value: '✓ Paid' },
          ].map((row) => (
            <Box key={row.label} sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.75 }}>
              <Typography variant="body2" color="text.secondary">{row.label}</Typography>
              <Typography
                variant="body2"
                fontWeight={600}
                color={row.label === 'Status' ? 'success.main' : 'text.primary'}
              >
                {row.value}
              </Typography>
            </Box>
          ))}

          <Divider sx={{ my: 2 }} />

          {/* Actions */}
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button
              variant="outlined"
              fullWidth
              startIcon={<DownloadIcon />}
              onClick={handleDownload}
            >
              Download
            </Button>
            <Button variant="contained" fullWidth onClick={onClose}>
              Done
            </Button>
          </Box>
        </Box>
      </DialogContent>
    </Dialog>
  );
}

PaymentReceipt.propTypes = {
  open: PropTypes.bool.isRequired,
  onClose: PropTypes.func.isRequired,
  receipt: PropTypes.shape({
    policyName: PropTypes.string,
    vehicleNumber: PropTypes.string,
    drivingLicenseNumber: PropTypes.string,
    amount: PropTypes.number,
    razorpayPaymentId: PropTypes.string,
    razorpayOrderId: PropTypes.string,
  }),
};
