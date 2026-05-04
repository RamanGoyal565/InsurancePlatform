import { useState } from 'react';
import PropTypes from 'prop-types';
import { Button, CircularProgress } from '@mui/material';
import PaymentIcon from '@mui/icons-material/Payment';
import { createRazorpayOrder, loadRazorpayScript, verifyRazorpayPayment } from '../../api/razorpay';
import { useAuth } from '../../app/AuthContext';
import { useToast } from '../../app/ToastContext';

/**
 * Razorpay payment button.
 * Creates an order, opens the Razorpay modal, verifies on success.
 *
 * @param {{ amount, policyId?, customerPolicyId?, description?, onSuccess, label }} props
 */
export default function RazorpayButton({
  amount,
  policyId,
  customerPolicyId,
  description,
  onSuccess,
  label = 'Pay with Razorpay',
  disabled = false,
}) {
  const { user } = useAuth();
  const toast = useToast();
  const [loading, setLoading] = useState(false);

  const handlePay = async () => {
    setLoading(true);
    try {
      await loadRazorpayScript();

      const order = await createRazorpayOrder({
        customerId: user.userId,
        policyId: policyId || undefined,
        amount,
        description: description || 'Insurance Premium Payment',
      });

      const options = {
        key: order.keyId,
        amount: Math.round(amount * 100), // paise
        currency: order.currency,
        name: 'Trust Guard',
        description: description || 'Insurance Premium Payment',
        order_id: order.orderId,
        prefill: {
          name: user.name,
          email: user.email,
        },
        theme: { color: '#1565C0' },
        handler: async (response) => {
          try {
            await verifyRazorpayPayment({
              customerId: user.userId,
              policyId: policyId || undefined,
              customerPolicyId: customerPolicyId || undefined,
              razorpayOrderId: response.razorpay_order_id,
              razorpayPaymentId: response.razorpay_payment_id,
              razorpaySignature: response.razorpay_signature,
              amount,
            });
            toast.success('Payment successful!');
            onSuccess?.();
          } catch {
            toast.error('Payment verification failed. Please contact support.');
          }
        },
        modal: {
          ondismiss: () => setLoading(false),
        },
      };

      const rzp = new window.Razorpay(options);
      rzp.on('payment.failed', (response) => {
        toast.error(`Payment failed: ${response.error.description}`);
        setLoading(false);
      });
      rzp.open();
    } catch (e) {
      const msg = e.response?.data?.message || e.message || 'Failed to initiate payment.';
      toast.error(msg);
      setLoading(false);
    }
  };

  return (
    <Button
      variant="contained"
      color="primary"
      startIcon={loading ? <CircularProgress size={18} color="inherit" /> : <PaymentIcon />}
      onClick={handlePay}
      disabled={disabled || loading}
      fullWidth
    >
      {loading ? 'Processing...' : label}
    </Button>
  );
}

RazorpayButton.propTypes = {
  amount: PropTypes.number.isRequired,
  policyId: PropTypes.string,
  customerPolicyId: PropTypes.string,
  description: PropTypes.string,
  onSuccess: PropTypes.func,
  label: PropTypes.string,
  disabled: PropTypes.bool,
};
