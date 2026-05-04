import apiClient from './client';

/**
 * Create a Razorpay order on the backend.
 * @param {{ customerId: string, policyId?: string, amount: number, description?: string }} data
 */
export const createRazorpayOrder = (data) =>
  apiClient.post('/payments/razorpay/create-order', data).then((r) => r.data);

/**
 * Verify Razorpay payment signature and record the payment.
 * @param {{ customerId, policyId?, customerPolicyId?, razorpayOrderId, razorpayPaymentId, razorpaySignature, amount }} data
 */
export const verifyRazorpayPayment = (data) =>
  apiClient.post('/payments/razorpay/verify', data).then((r) => r.data);

/**
 * Load the Razorpay checkout script dynamically.
 * @returns {Promise<void>}
 */
export const loadRazorpayScript = () =>
  new Promise((resolve, reject) => {
    if (document.getElementById('razorpay-script')) { resolve(); return; }
    const script = document.createElement('script');
    script.id = 'razorpay-script';
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.onload = resolve;
    script.onerror = reject;
    document.body.appendChild(script);
  });
