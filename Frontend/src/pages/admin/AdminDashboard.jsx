import { useState } from 'react';
import { useQueries } from '@tanstack/react-query';
import {
  Box, Grid, Typography, Card, CardContent, Tabs, Tab,
  Table, TableBody, TableCell, TableHead, TableRow, TablePagination,
  TextField, Button, Alert, MenuItem, Select,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import {
  LineChart, Line, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts';
import StatCard from '../../components/ui/StatCard';
import {
  useDashboard, useTicketReports, useClaimReports,
  useRevenueReports, usePolicyReports, useUserReports, usePerformanceReports,
} from '../../hooks/useReports';
import { usePolicies } from '../../hooks/usePolicies';
import { getPolicyCustomersReport } from '../../api/reports';

const COLORS = ['#1565C0', '#00897B', '#F57C00', '#E53935', '#8E24AA', '#039BE5'];

const REPORT_TABS = ['Ticket Reports', 'Claim Reports', 'Revenue Reports', 'Policy Reports', 'User Reports', 'Performance Reports'];

export default function AdminDashboard() {
  const [tab, setTab] = useState(0);
  const [searchCustomer, setSearchCustomer] = useState('');
  const [selectedPolicyId, setSelectedPolicyId] = useState('');
  const [page, setPage] = useState(0);

  const { data: dashboard, isLoading: dashLoading } = useDashboard();
  const { data: ticketReports } = useTicketReports();
  const { data: claimReports } = useClaimReports();
  const { data: revenueReports } = useRevenueReports();
  const { data: policyReports } = usePolicyReports();
  const { data: userReports } = useUserReports();
  const { data: perfReports } = usePerformanceReports();
  const { data: policies = [] } = usePolicies();

  // Fetch customers for ALL policies in parallel so we can show all by default
  const allPolicyCustomerQueries = useQueries({
    queries: policies.map((p) => ({
      queryKey: ['policy-customers-report', p.policyId],
      queryFn: () => getPolicyCustomersReport(p.policyId),
      enabled: policies.length > 0,
    })),
  });

  // Merge all customers across all policies (deduplicated by customerId)
  const allCustomers = allPolicyCustomerQueries
    .flatMap((q) => q.data?.customers ?? [])
    .reduce((acc, c) => {
      if (!acc.find((x) => x.customerId === c.customerId)) acc.push(c);
      return acc;
    }, []);

  // When a specific policy is selected, filter to that policy's customers
  const selectedPolicyCustomers = selectedPolicyId
    ? (allPolicyCustomerQueries
        .find((_, i) => policies[i]?.policyId === selectedPolicyId)
        ?.data?.customers ?? [])
    : allCustomers;

  const filteredCustomers = selectedPolicyCustomers.filter((c) =>
    !searchCustomer ||
    c.customerName?.toLowerCase().includes(searchCustomer.toLowerCase()) ||
    c.customerEmail?.toLowerCase().includes(searchCustomer.toLowerCase())
  );

  // KPIs — no fake trends, only show real data
  const kpis = [
    { title: 'Total Users', value: dashboard?.totalUsers ?? 0 },
    // totalPoliciesSold is not on the dashboard endpoint — use policyReports
    { title: 'Total Policies Sold', value: policyReports?.totalPoliciesSold ?? 0 },
    { title: 'Active Policies', value: dashboard?.activePolicies ?? 0 },
    { title: 'Open Tickets', value: dashboard?.openTickets ?? 0 },
    { title: 'Total Claims', value: dashboard?.totalClaims ?? 0 },
    { title: 'Approved Claims', value: dashboard?.approvedClaims ?? 0 },
    {
      title: 'Total Revenue',
      value: dashboard?.totalRevenue
        ? dashboard.totalRevenue >= 10000000
          ? `₹${(dashboard.totalRevenue / 10000000).toFixed(2)} Cr`
          : dashboard.totalRevenue >= 100000
            ? `₹${(dashboard.totalRevenue / 100000).toFixed(2)} L`
            : `₹${dashboard.totalRevenue.toLocaleString('en-IN')}`
        : '₹0',
    },
  ];

  const revenueTrend = revenueReports?.revenueByDate?.daily?.slice(-7) || [];
  const ticketsByStatus = ticketReports?.ticketsByStatus || [];
  const policiesByType = policyReports?.policiesByType || [];
  const usersByRole = userReports ? [
    { name: 'Customers', value: userReports.usersByRole?.customers || 0 },
    { name: 'Claims Specialists', value: userReports.usersByRole?.claimsSpecialists || 0 },
    { name: 'Support Specialists', value: userReports.usersByRole?.supportSpecialists || 0 },
  ].filter((r) => r.value > 0) : [];

  return (
    <Box>
      <Typography variant="h5" fontWeight={700} mb={0.5}>Admin Dashboard & Reports</Typography>
      <Typography variant="body2" color="text.secondary" mb={3}>
        Overview of platform analytics and operational reports
      </Typography>

      {/* KPI Cards — full width flex */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3, flexWrap: 'wrap' }}>
        {kpis.map((k) => (
          <Box key={k.title} sx={{ flex: 1, minWidth: 120 }}>
            <StatCard {...k} loading={dashLoading} />
          </Box>
        ))}
      </Box>

      {/* Report Tabs */}
      <Card sx={{ mb: 3 }}>
        <Box sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
          <Tabs value={tab} onChange={(_, v) => setTab(v)} variant="scrollable" scrollButtons="auto">
            {REPORT_TABS.map((t) => <Tab key={t} label={t} />)}
          </Tabs>
        </Box>

        <CardContent sx={{ minHeight: 320 }}>
          {/* Revenue Trend */}
          {tab === 2 && (
            <Box>
              <Typography variant="subtitle1" fontWeight={600} mb={2}>Revenue Trend (Last 7 Days)</Typography>
              {revenueTrend.length === 0 ? (
                <Alert severity="info">No revenue data available yet</Alert>
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <LineChart data={revenueTrend}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="period" tick={{ fontSize: 12 }} />
                    <YAxis tick={{ fontSize: 12 }} />
                    <Tooltip formatter={(v) => `₹${v.toLocaleString()}`} />
                    <Line type="monotone" dataKey="amount" stroke="#1565C0" strokeWidth={2} dot={false} />
                  </LineChart>
                </ResponsiveContainer>
              )}
            </Box>
          )}

          {/* Ticket Status Distribution */}
          {tab === 0 && (
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Ticket Status Distribution</Typography>
                {ticketsByStatus.length === 0 ? (
                  <Alert severity="info">No ticket data available yet</Alert>
                ) : (
                  <Box sx={{ height: 300 }}>
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={ticketsByStatus}
                          dataKey="count"
                          nameKey="label"
                          cx="50%"
                          cy="40%"
                          outerRadius={85}
                          label={false}
                        >
                          {ticketsByStatus.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                        </Pie>
                        <Tooltip />
                        <Legend verticalAlign="bottom" iconSize={10} wrapperStyle={{ paddingTop: 12, fontSize: 12 }} />
                      </PieChart>
                    </ResponsiveContainer>
                  </Box>
                )}
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Summary</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                  <Typography>Total Tickets: <strong>{ticketReports?.totalTickets ?? 0}</strong></Typography>
                  <Typography>Open: <strong>{ticketReports?.openTickets ?? 0}</strong></Typography>
                  <Typography>Closed: <strong>{ticketReports?.closedTickets ?? 0}</strong></Typography>
                  <Typography>
                    Avg Resolution: <strong>{ticketReports?.averageResolutionTimeHours ? `${ticketReports.averageResolutionTimeHours.toFixed(1)} hrs` : '—'}</strong>
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          )}

          {/* Policies by Vehicle Type */}
          {tab === 3 && (
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Policies by Vehicle Type</Typography>
                {policiesByType.length === 0 ? (
                  <Alert severity="info">No policy data available yet</Alert>
                ) : (
                  <ResponsiveContainer width="100%" height={260}>
                    <BarChart data={policiesByType}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="label" tick={{ fontSize: 12 }} />
                      <YAxis tick={{ fontSize: 12 }} />
                      <Tooltip />
                      <Bar dataKey="count" fill="#1565C0" />
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Summary</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                  <Typography>Total Sold: <strong>{policyReports?.totalPoliciesSold ?? 0}</strong></Typography>
                  <Typography>Active: <strong>{policyReports?.activePolicies ?? 0}</strong></Typography>
                  <Typography>Expired: <strong>{policyReports?.expiredPolicies ?? 0}</strong></Typography>
                  <Typography>
                    Renewal Rate: <strong>{policyReports?.policyRenewalRate ? `${policyReports.policyRenewalRate.toFixed(1)}%` : '—'}</strong>
                  </Typography>
                </Box>
              </Grid>
            </Grid>
          )}

          {/* User Role Distribution */}
          {tab === 4 && (
            <Grid container spacing={3}>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>User Role Distribution</Typography>
                {usersByRole.length === 0 ? (
                  <Alert severity="info">No user data available yet</Alert>
                ) : (
                  <Box sx={{ height: 300 }}>
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={usersByRole}
                          dataKey="value"
                          nameKey="name"
                          cx="50%"
                          cy="50%"
                          outerRadius={78}
                          label={false}
                        >
                          {usersByRole.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                        </Pie>
                        <Tooltip />
                        <Legend verticalAlign="bottom" iconSize={10} wrapperStyle={{ paddingTop: 12, fontSize: 12 }} />
                      </PieChart>
                    </ResponsiveContainer>
                  </Box>
                )}
              </Grid>
              <Grid item xs={12} md={6}>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Summary</Typography>
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                  <Typography>Total Users: <strong>{userReports?.totalUsers ?? 0}</strong></Typography>
                  <Typography>Active: <strong>{userReports?.activeUsers ?? 0}</strong></Typography>
                  <Typography>Inactive: <strong>{userReports?.inactiveUsers ?? 0}</strong></Typography>
                </Box>
              </Grid>
            </Grid>
          )}

          {/* Performance */}
          {tab === 5 && (
            <Box>
              <Typography variant="subtitle1" fontWeight={600} mb={2}>Top Specialists Performance</Typography>
              {(!perfReports?.claimsSpecialists || perfReports.claimsSpecialists.length === 0) ? (
                <Alert severity="info">No performance data available yet</Alert>
              ) : (
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Specialist Name</TableCell>
                      <TableCell>Role</TableCell>
                      <TableCell align="right">Tickets Handled</TableCell>
                      <TableCell align="right">Claims Approved</TableCell>
                      <TableCell align="right">Avg. Resolution Time</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {perfReports.claimsSpecialists.map((s) => (
                      <TableRow key={s.userId}>
                        <TableCell>{s.name}</TableCell>
                        <TableCell>Claims Specialist</TableCell>
                        <TableCell align="right">{s.claimsProcessed}</TableCell>
                        <TableCell align="right">{Math.round(s.claimsProcessed * s.approvalRate / 100)}</TableCell>
                        <TableCell align="right" sx={{ color: 'warning.main' }}>
                          {s.averageProcessingTimeHours ? `${(s.averageProcessingTimeHours / 24).toFixed(1)} days` : '—'}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </Box>
          )}

          {/* Claim Reports */}
          {tab === 1 && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
              <Typography>Total Claims: <strong>{claimReports?.totalClaims ?? 0}</strong></Typography>
              <Typography>
                Approval Rate: <strong>{claimReports?.claimApprovalRate ? `${claimReports.claimApprovalRate.toFixed(1)}%` : '—'}</strong>
              </Typography>
              <Typography>
                Rejection Rate: <strong>{claimReports?.claimRejectionRate ? `${claimReports.claimRejectionRate.toFixed(1)}%` : '—'}</strong>
              </Typography>
              <Typography>
                Avg Processing: <strong>{claimReports?.averageClaimProcessingTimeHours ? `${claimReports.averageClaimProcessingTimeHours.toFixed(1)} hrs` : '—'}</strong>
              </Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      {/* Search Customers by Policy */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={600} mb={2}>
            Customers Who Bought Policies
          </Typography>
          <Grid container spacing={2} alignItems="flex-end" mb={2}>
            <Grid item xs={12} sm={4}>
              <TextField
                select
                label="Filter by Policy"
                fullWidth
                size="small"
                value={selectedPolicyId}
                onChange={(e) => { setSelectedPolicyId(e.target.value); setPage(0); }}
              >
                <MenuItem value="">All Policies</MenuItem>
                {policies.map((p) => (
                  <MenuItem key={p.policyId} value={p.policyId}>{p.name}</MenuItem>
                ))}
              </TextField>
            </Grid>
            <Grid item xs={12} sm={5}>
              <TextField
                label="Search by name, email or phone"
                fullWidth
                size="small"
                value={searchCustomer}
                onChange={(e) => setSearchCustomer(e.target.value)}
                InputProps={{ startAdornment: <SearchIcon sx={{ mr: 1, color: 'text.secondary' }} /> }}
              />
            </Grid>
            <Grid item xs={12} sm={3}>
              <Button variant="contained" startIcon={<SearchIcon />} fullWidth>Search</Button>
            </Grid>
          </Grid>

          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Customer Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Active Status</TableCell>
                <TableCell>Purchase Date</TableCell>
                <TableCell>Renewal Count</TableCell>
                <TableCell>Latest Status</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredCustomers.slice(page * 5, page * 5 + 5).map((c) => (
                <TableRow key={c.customerId}>
                  <TableCell>{c.customerName}</TableCell>
                  <TableCell>{c.customerEmail}</TableCell>
                  <TableCell>{c.isActive ? 'Active' : 'Inactive'}</TableCell>
                  <TableCell>{new Date(c.firstPurchasedAtUtc).toLocaleDateString()}</TableCell>
                  <TableCell>{c.renewalCount}</TableCell>
                  <TableCell>{c.latestStatus}</TableCell>
                </TableRow>
              ))}
              {filteredCustomers.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ color: 'text.secondary', py: 3 }}>
                  {selectedPolicyId ? 'No customers found' : 'No customers have purchased any policy yet'}
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={filteredCustomers.length}
            page={page}
            onPageChange={(_, p) => setPage(p)}
            rowsPerPage={5}
            rowsPerPageOptions={[5]}
          />
        </CardContent>
      </Card>
    </Box>
  );
}
