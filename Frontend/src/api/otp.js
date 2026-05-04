import apiClient from './client';

/**
 * Request a 6-digit OTP sent to the user's email.
 * @param {{ email: string, purpose: 'ForgotPassword'|'EmailVerification' }} data
 */
export const requestOtp = (data) =>
  apiClient.post('/identity/auth/otp/request', data).then((r) => r.data);

/**
 * Verify an OTP code.
 * @param {{ email: string, code: string, purpose: string }} data
 */
export const verifyOtp = (data) =>
  apiClient.post('/identity/auth/otp/verify', data).then((r) => r.data);

/**
 * Reset password using a verified OTP.
 * @param {{ email: string, code: string, newPassword: string }} data
 */
export const resetPassword = (data) =>
  apiClient.post('/identity/auth/otp/reset-password', data).then((r) => r.data);
