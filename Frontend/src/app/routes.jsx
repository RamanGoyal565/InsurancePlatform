import { lazy, Suspense } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { Box } from '@mui/material';
import { useAuth } from './AuthContext';
import { ROLES } from './roles';
import RoleGuard from '../components/RoleGuard';
import AppShell from '../components/layout/AppShell';
import PageLoader from '../components/ui/PageLoader';
import BrandLogo from '../components/ui/BrandLogo';

const wrap = (Component) => (
  <Suspense fallback={<PageLoader />}>
    <Component />
  </Suspense>
);

// Public pages
const LandingPage = lazy(() => import('../pages/public/LandingPage'));
const LoginPage = lazy(() => import('../pages/public/LoginPage'));
const RegisterPage = lazy(() => import('../pages/public/RegisterPage'));
const ForgotPasswordPage = lazy(() => import('../pages/public/ForgotPasswordPage'));
const UnauthorizedPage = lazy(() => import('../pages/public/UnauthorizedPage'));
const EmailVerificationPage = lazy(() => import('../pages/public/EmailVerificationPage'));
// Public policies page — reuses BrowsePolicies (works without auth, shows "Login to Buy")
const PublicPoliciesPage = lazy(() => import('../pages/customer/BrowsePolicies'));

// Admin pages
const AdminDashboard = lazy(() => import('../pages/admin/AdminDashboard'));
const AdminUsers = lazy(() => import('../pages/admin/AdminUsers'));
const AdminPolicies = lazy(() => import('../pages/admin/AdminPolicies'));
const AdminReports = lazy(() => import('../pages/admin/AdminReports'));
const AdminPayments = lazy(() => import('../pages/admin/AdminPayments'));

// Customer pages
const CustomerDashboard = lazy(() => import('../pages/customer/CustomerDashboard'));
const BrowsePolicies = lazy(() => import('../pages/customer/BrowsePolicies'));
const MyPolicies = lazy(() => import('../pages/customer/MyPolicies'));
const CustomerTickets = lazy(() => import('../pages/customer/CustomerTickets'));
const CustomerPayments = lazy(() => import('../pages/customer/CustomerPayments'));
const CustomerNotifications = lazy(() => import('../pages/customer/CustomerNotifications'));

// Specialist pages (shared)
const SpecialistDashboard = lazy(() => import('../pages/specialist/SpecialistDashboard'));
const TicketsManagement = lazy(() => import('../pages/specialist/TicketsManagement'));
const TicketDetail = lazy(() => import('../pages/specialist/TicketDetail'));

const ALL_AUTH = [ROLES.ADMIN, ROLES.CUSTOMER, ROLES.CLAIMS_SPECIALIST, ROLES.SUPPORT_SPECIALIST];
const ADMIN_ONLY = [ROLES.ADMIN];
const CUSTOMER_ONLY = [ROLES.CUSTOMER];
const SPECIALISTS = [ROLES.CLAIMS_SPECIALIST, ROLES.SUPPORT_SPECIALIST, ROLES.ADMIN];
const ALL_EXCEPT_ADMIN = [ROLES.CUSTOMER, ROLES.CLAIMS_SPECIALIST, ROLES.SUPPORT_SPECIALIST, ROLES.ADMIN];

const router = createBrowserRouter([
  { path: '/', element: wrap(LandingPage) },
  { path: '/login', element: wrap(LoginPage) },
  { path: '/register', element: wrap(RegisterPage) },
  { path: '/forgot-password', element: wrap(ForgotPasswordPage) },
  { path: '/verify-email', element: wrap(EmailVerificationPage) },
  { path: '/unauthorized', element: wrap(UnauthorizedPage) },
  // Public policies page — no auth required
  {
    path: '/policies',
    element: (
      <Box sx={{ minHeight: '100vh', bgcolor: '#faf9fd' }}>
        {/* Minimal nav */}
        <Box
          sx={{
            bgcolor: '#002045',
            px: { xs: 2.5, md: 4 },
            py: 1.75,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            position: 'sticky',
            top: 0,
            zIndex: 100,
          }}
        >
          <Box
            sx={{ display: 'flex', alignItems: 'center', gap: 1, cursor: 'pointer' }}
            onClick={() => window.history.back()}
          >
            <Box component="span" sx={{ color: '#fff', fontSize: 20, lineHeight: 1 }}>←</Box>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
              <BrandLogo size="sm" />
            </Box>
          </Box>
          <Box sx={{ display: 'flex', gap: 1.5 }}>
            <Box
              component="button"
              onClick={() => { window.location.href = '/login'; }}
              sx={{
                bgcolor: 'transparent',
                border: '1px solid rgba(255,255,255,0.35)',
                color: '#fff',
                fontSize: 13,
                fontWeight: 600,
                px: 2.5,
                py: 0.75,
                borderRadius: '4px',
                cursor: 'pointer',
                fontFamily: 'Inter, sans-serif',
                '&:hover': { bgcolor: 'rgba(255,255,255,0.08)' },
              }}
            >
              Log In
            </Box>
            <Box
              component="button"
              onClick={() => { window.location.href = '/register'; }}
              sx={{
                bgcolor: '#e65100',
                border: 'none',
                color: '#fff',
                fontSize: 13,
                fontWeight: 700,
                px: 2.5,
                py: 0.75,
                borderRadius: '4px',
                cursor: 'pointer',
                fontFamily: 'Inter, sans-serif',
                '&:hover': { bgcolor: '#bf360c' },
              }}
            >
              Get Started
            </Box>
          </Box>
        </Box>
        {/* Page content */}
        <Box sx={{ maxWidth: 1280, mx: 'auto', px: { xs: 2, md: 4 }, py: 5 }}>
          <Suspense fallback={<PageLoader />}>
            <PublicPoliciesPage />
          </Suspense>
        </Box>
      </Box>
    ),
  },

  // Authenticated shell
  {
    element: (
      <RoleGuard roles={ALL_AUTH}>
        <AppShell />
      </RoleGuard>
    ),
    children: [
      // Admin routes
      {
        path: '/admin/dashboard',
        element: <RoleGuard roles={ADMIN_ONLY}>{wrap(AdminDashboard)}</RoleGuard>,
      },
      {
        path: '/admin/users',
        element: <RoleGuard roles={ADMIN_ONLY}>{wrap(AdminUsers)}</RoleGuard>,
      },
      {
        path: '/admin/policies',
        element: <RoleGuard roles={ADMIN_ONLY}>{wrap(AdminPolicies)}</RoleGuard>,
      },
      {
        path: '/admin/reports',
        element: <RoleGuard roles={ADMIN_ONLY}>{wrap(AdminReports)}</RoleGuard>,
      },
      {
        path: '/admin/payments',
        element: <RoleGuard roles={ADMIN_ONLY}>{wrap(AdminPayments)}</RoleGuard>,
      },

      // Customer routes
      {
        path: '/dashboard',
        element: <RoleGuard roles={CUSTOMER_ONLY}>{wrap(CustomerDashboard)}</RoleGuard>,
      },
      {
        path: '/browse-policies',
        element: <RoleGuard roles={CUSTOMER_ONLY}>{wrap(BrowsePolicies)}</RoleGuard>,
      },
      {
        path: '/my-policies',
        element: <RoleGuard roles={CUSTOMER_ONLY}>{wrap(MyPolicies)}</RoleGuard>,
      },
      {
        path: '/tickets',
        element: <RoleGuard roles={CUSTOMER_ONLY}>{wrap(CustomerTickets)}</RoleGuard>,
      },
      {
        path: '/payments',
        element: <RoleGuard roles={CUSTOMER_ONLY}>{wrap(CustomerPayments)}</RoleGuard>,
      },
      {
        path: '/notifications',
        element: <RoleGuard roles={ALL_EXCEPT_ADMIN}>{wrap(CustomerNotifications)}</RoleGuard>,
      },

      // Specialist routes
      {
        path: '/specialist/dashboard',
        element: <RoleGuard roles={SPECIALISTS}>{wrap(SpecialistDashboard)}</RoleGuard>,
      },
      {
        path: '/specialist/tickets',
        element: <RoleGuard roles={SPECIALISTS}>{wrap(TicketsManagement)}</RoleGuard>,
      },
      {
        path: '/specialist/tickets/:ticketId',
        element: <RoleGuard roles={SPECIALISTS}>{wrap(TicketDetail)}</RoleGuard>,
      },
    ],
  },

  // Role-based default redirect
  { path: '/home', element: <RoleRedirect /> },
  { path: '*', element: <Navigate to="/" replace /> },
]);

function RoleRedirect() {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  if (user.role === ROLES.ADMIN) return <Navigate to="/admin/dashboard" replace />;
  if (user.role === ROLES.CUSTOMER) return <Navigate to="/dashboard" replace />;
  return <Navigate to="/specialist/dashboard" replace />;
}

export default router;
