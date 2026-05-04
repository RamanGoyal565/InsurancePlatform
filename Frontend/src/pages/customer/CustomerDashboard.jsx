import { Box, Button, Card, CardContent, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import PolicyIcon from '@mui/icons-material/Policy';
import ConfirmationNumberIcon from '@mui/icons-material/ConfirmationNumber';
import PaymentIcon from '@mui/icons-material/Payment';
import NotificationsIcon from '@mui/icons-material/Notifications';
import DirectionsCarIcon from '@mui/icons-material/DirectionsCar';
import LocalShippingIcon from '@mui/icons-material/LocalShipping';
import TwoWheelerIcon from '@mui/icons-material/TwoWheeler';
import CheckIcon from '@mui/icons-material/Check';
import { useAuth } from '../../app/AuthContext';
import { useCustomerPolicies, usePolicies } from '../../hooks/usePolicies';
import { useTickets } from '../../hooks/useTickets';
import { usePayments } from '../../hooks/usePayments';
import { useNotifications } from '../../hooks/useNotifications';
import StatusChip from '../../components/ui/StatusChip';

const VEHICLE_ICONS = { 1: <DirectionsCarIcon />, 2: <LocalShippingIcon />, 3: <TwoWheelerIcon /> };
const VEHICLE_LABELS = { 1: 'Car', 2: 'Truck', 3: 'Bike' };
const VEHICLE_COLORS = { 1: '#E3F2FD', 2: '#E8F5E9', 3: '#FFF3E0' };

/** Split coverage — semicolon-separated only: "Item one;Item two;Item three" */
const splitCoverage = (str) =>
  str ? str.split(';').map((s) => s.trim()).filter(Boolean) : [];

export default function CustomerDashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { data: availablePolicies = [] } = usePolicies();
  const { data: myPolicies = [] } = useCustomerPolicies();
  const { data: tickets = [] } = useTickets();
  const { data: payments = [] } = usePayments();
  const { data: notifications = [] } = useNotifications();

  const activePolicies = myPolicies.filter((p) => p.status === 2).length;
  const pendingPayments = payments.filter((p) => p.status === 1);
  const pendingAmount = pendingPayments.reduce((s, p) => s + (p.amount || 0), 0);
  const openTickets = tickets.filter((t) => t.status === 1).length;
  const unreadNotifs = notifications.filter((n) => !n.isRead).length;

  const today = new Date().toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

  // Only show up to 3 policies in the browse section — only when data exists
  const browsePolicies = availablePolicies.slice(0, 3);

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 3 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>Welcome back, {user?.name?.split(' ')[0]}! 👋</Typography>
          <Typography variant="body2" color="text.secondary">Here&apos;s what&apos;s happening with your policies and services today.</Typography>
        </Box>
        <Typography variant="caption" color="text.secondary">{today}</Typography>
      </Box>

      {/* KPI Cards — 4 equal columns, full width */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
        {[
          { label: 'Active Policies', value: activePolicies, sub: 'View your active policies', to: '/my-policies', icon: <PolicyIcon color="primary" /> },
          { label: 'Pending Payments', value: pendingAmount > 0 ? `₹${pendingAmount.toLocaleString()}` : pendingPayments.length, sub: `${pendingPayments.length} payments pending`, to: '/payments', icon: <PaymentIcon color="warning" /> },
          { label: 'Open Tickets', value: openTickets, sub: 'View and track tickets', to: '/tickets', icon: <ConfirmationNumberIcon color="error" /> },
          { label: 'Unread Notifications', value: unreadNotifs, sub: 'View all notifications', to: '/notifications', icon: <NotificationsIcon color="info" /> },
        ].map((k) => (
          <Card key={k.label} sx={{ flex: 1, minWidth: 0, cursor: 'pointer' }} onClick={() => navigate(k.to)}>
            <CardContent sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <Box sx={{ minWidth: 0 }}>
                  <Typography variant="caption" color="text.secondary" noWrap>{k.label}</Typography>
                  <Typography variant="h5" fontWeight={700}>{k.value}</Typography>
                  <Typography variant="caption" color="text.secondary" noWrap>{k.sub}</Typography>
                </Box>
                {k.icon}
              </Box>
            </CardContent>
          </Card>
        ))}
      </Box>

      {/* Quick Actions — 4 equal columns, full width */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={600} mb={2}>Quick Actions</Typography>
          <Box sx={{ display: 'flex', gap: 2 }}>
            {[
              { label: 'Explore plans', sub: 'Browse Policies', to: '/browse-policies' },
              { label: 'Get support', sub: 'Raise Ticket', to: '/tickets' },
              { label: 'Initiate claim', sub: 'File a Claim', to: '/tickets' },
              { label: 'Pay premium', sub: 'Make Payment', to: '/payments' },
            ].map((a) => (
              <Button
                key={a.sub}
                variant="outlined"
                sx={{ flex: 1, minWidth: 0, py: 2, flexDirection: 'column', gap: 0.5 }}
                onClick={() => navigate(a.to)}
                endIcon={<ArrowForwardIcon />}
              >
                <Typography variant="subtitle2" noWrap>{a.sub}</Typography>
                <Typography variant="caption" color="text.secondary" noWrap>{a.label}</Typography>
              </Button>
            ))}
          </Box>
        </CardContent>
      </Card>

      {/* Browse Vehicle Policies — only shown when policies exist in the catalog */}
      {browsePolicies.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
              <Typography variant="subtitle1" fontWeight={600}>Browse Vehicle Policies</Typography>
              <Button size="small" endIcon={<ArrowForwardIcon />} onClick={() => navigate('/browse-policies')}>
                View All Policies
              </Button>
            </Box>
            <Box sx={{ display: 'flex', gap: 2 }}>
              {browsePolicies.map((p) => (
                <Box key={p.policyId} sx={{ flex: 1, minWidth: 0 }}>
                  <Card variant="outlined" sx={{ bgcolor: VEHICLE_COLORS[p.vehicleType] || '#F5F5F5', height: '100%' }}>
                    <CardContent sx={{ textAlign: 'center' }}>
                      <Box sx={{ color: 'text.secondary', mb: 1, display: 'flex', justifyContent: 'center' }}>
                        {p.vehicleType === 1 && <DirectionsCarIcon sx={{ fontSize: 48 }} />}
                        {p.vehicleType === 2 && <LocalShippingIcon sx={{ fontSize: 48 }} />}
                        {p.vehicleType === 3 && <TwoWheelerIcon sx={{ fontSize: 48 }} />}
                      </Box>
                      <Typography variant="subtitle1" fontWeight={600}>{p.name}</Typography>
                      <Typography variant="body2" color="text.secondary">Starting from</Typography>
                      <Typography variant="h6" fontWeight={700} color="primary">
                        ₹{p.premium?.toLocaleString()} / year
                      </Typography>
                      {p.coverageDetails && (
                        <Box sx={{ textAlign: 'left', mt: 1, mb: 1.5 }}>
                          {splitCoverage(p.coverageDetails).slice(0, 3).map((c, i) => (
                            <Box key={i} sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                              <CheckIcon sx={{ fontSize: 13, color: 'success.main' }} />
                              <Typography variant="caption">{c}</Typography>
                            </Box>
                          ))}
                        </Box>
                      )}
                      <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
                        <Button variant="outlined" size="small" fullWidth onClick={() => navigate('/browse-policies')}>
                          View Details
                        </Button>
                        <Button variant="contained" size="small" fullWidth onClick={() => navigate('/browse-policies')}>
                          Buy Policy
                        </Button>
                      </Box>
                    </CardContent>
                  </Card>
                </Box>
              ))}
            </Box>
          </CardContent>
        </Card>
      )}

      {/* My Policies */}
      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="subtitle1" fontWeight={600}>My Policies</Typography>
            <Button size="small" endIcon={<ArrowForwardIcon />} onClick={() => navigate('/my-policies')}>View All Policies</Button>
          </Box>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Policy No.</TableCell>
                <TableCell>Policy Name</TableCell>
                <TableCell>Vehicle No.</TableCell>
                <TableCell>Vehicle Type</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>End Date</TableCell>
                <TableCell>Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {myPolicies.slice(0, 4).map((p) => (
                <TableRow key={p.customerPolicyId} hover>
                  <TableCell sx={{ color: 'primary.main', fontWeight: 600 }}>POL-{p.customerPolicyId?.slice(0, 8).toUpperCase()}</TableCell>
                  <TableCell>{p.policyName}</TableCell>
                  <TableCell>{p.vehicleNumber}</TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                      {VEHICLE_ICONS[p.vehicleType]}
                      <Typography variant="body2">{VEHICLE_LABELS[p.vehicleType]}</Typography>
                    </Box>
                  </TableCell>
                  <TableCell><StatusChip status={['', 'Pending', 'Active', 'Renewed', 'Cancelled', 'Expired'][p.status] || p.status} /></TableCell>
                  <TableCell>{new Date(p.endDate).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <Button size="small" variant="outlined" onClick={() => navigate('/my-policies')}>
                      {p.status === 5 ? 'Buy Again' : 'Renew'}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
              {myPolicies.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 3, color: 'text.secondary' }}>
                    No policies yet. Browse and buy a policy!
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </Box>
  );
}
