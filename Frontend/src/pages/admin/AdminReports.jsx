import {
  Box, Card, CardContent, Typography, Alert,
  Table, TableHead, TableRow, TableCell, TableBody,
} from '@mui/material';
import {
  useRevenueReports, usePerformanceReports, useUserReports,
  useTicketReports, usePolicyReports, useClaimReports,
} from '../../hooks/useReports';
import {
  LineChart, Line, BarChart, Bar, PieChart, Pie, Cell,
  XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts';

const COLORS = ['#1565C0', '#00897B', '#F57C00', '#E53935', '#8E24AA', '#039BE5'];

// Recharts needs a concrete pixel height — we compute it from window.innerHeight
// but since this is a constant module-level value, we use a safe fixed height
// that fills the screen well on any laptop/desktop (600px ≈ 55vh on 1080p).
const CHART_H = 420;

function EmptyState({ height = CHART_H }) {
  return (
    <Box
      sx={{
        height,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'grey.50',
        borderRadius: 2,
        border: '1px dashed',
        borderColor: 'divider',
      }}
    >
      <Typography variant="body2" color="text.disabled">No data available yet</Typography>
    </Box>
  );
}

/** Renders count value inside each pie slice in white */
const renderSliceLabel = ({ cx, cy, midAngle, innerRadius, outerRadius, value }) => {
  if (!value) return null;
  const RADIAN = Math.PI / 180;
  const r = innerRadius + (outerRadius - innerRadius) * 0.55;
  const x = cx + r * Math.cos(-midAngle * RADIAN);
  const y = cy + r * Math.sin(-midAngle * RADIAN);
  return (
    <text x={x} y={y} fill="#fff" textAnchor="middle" dominantBaseline="central" fontSize={13} fontWeight={700}>
      {value}
    </text>
  );
};

export default function AdminReports() {
  const { data: revenue } = useRevenueReports();
  const { data: perf } = usePerformanceReports();
  const { data: users } = useUserReports();
  const { data: tickets } = useTicketReports();
  const { data: policies } = usePolicyReports();
  const { data: claims } = useClaimReports();

  const revTrend = revenue?.revenueTrends || [];
  const usersByRole = users ? [
    { name: 'Customers', value: users.usersByRole?.customers || 0 },
    { name: 'Claims Specialists', value: users.usersByRole?.claimsSpecialists || 0 },
    { name: 'Support Specialists', value: users.usersByRole?.supportSpecialists || 0 },
  ].filter((r) => r.value > 0) : [];
  const ticketsByStatus = tickets?.ticketsByStatus || [];
  const policiesByType = policies?.policiesByType || [];
  const specialists = perf?.claimsSpecialists || [];

  return (
    <Box sx={{ width: '100%' }}>
      {/* Header */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="h5" fontWeight={700}>Reports</Typography>
        <Typography variant="body2" color="text.secondary">Platform analytics and performance metrics</Typography>
      </Box>

      {/* ── 1. Revenue Trends — full width ── */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={700} mb={2}>Revenue Trends</Typography>
          {revTrend.length === 0 ? <EmptyState /> : (
            <ResponsiveContainer width="100%" height={CHART_H}>
              <LineChart data={revTrend} margin={{ top: 10, right: 40, left: 20, bottom: 10 }}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="period" tick={{ fontSize: 13 }} />
                <YAxis tick={{ fontSize: 13 }} tickFormatter={(v) => `₹${(v / 100000).toFixed(0)}L`} width={72} />
                <Tooltip formatter={(v) => [`₹${v.toLocaleString()}`, 'Revenue']} />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="amount"
                  name="Revenue"
                  stroke="#1565C0"
                  strokeWidth={3}
                  dot={{ r: 5 }}
                  activeDot={{ r: 7 }}
                />
              </LineChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      {/* ── 2. Ticket Status + Policies — 2 equal columns ── */}
      <Box sx={{ display: 'flex', gap: 3, mb: 3, flexWrap: 'nowrap' }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={700} mb={2}>Ticket Status Distribution</Typography>
              {ticketsByStatus.length === 0 ? <EmptyState /> : (
                <>
                  <ResponsiveContainer width="100%" height={CHART_H}>
                    <PieChart>
                      <Pie
                        data={ticketsByStatus}
                        dataKey="count"
                        nameKey="label"
                        cx="50%"
                        cy="50%"
                        outerRadius={160}
                        labelLine={false}
                        label={renderSliceLabel}
                      >
                        {ticketsByStatus.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                      </Pie>
                      <Tooltip />
                      <Legend
                        layout="horizontal"
                        verticalAlign="bottom"
                        align="center"
                        iconSize={12}
                        formatter={(v) => <span style={{ fontSize: 13 }}>{v}</span>}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                  <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', mt: 1, pt: 1, borderTop: '1px solid', borderColor: 'divider' }}>
                    <Typography variant="body2">Total: <strong>{tickets?.totalTickets ?? 0}</strong></Typography>
                    <Typography variant="body2">Open: <strong>{tickets?.openTickets ?? 0}</strong></Typography>
                    <Typography variant="body2">Closed: <strong>{tickets?.closedTickets ?? 0}</strong></Typography>
                    <Typography variant="body2">Avg Resolution: <strong>{tickets?.averageResolutionTimeHours ? `${tickets.averageResolutionTimeHours.toFixed(1)} hrs` : '—'}</strong></Typography>
                  </Box>
                </>
              )}
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={700} mb={2}>Policies by Vehicle Type</Typography>
              {policiesByType.length === 0 ? <EmptyState /> : (
                <>
                  <ResponsiveContainer width="100%" height={CHART_H}>
                    <BarChart data={policiesByType} margin={{ top: 10, right: 30, left: 10, bottom: 10 }}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis dataKey="label" tick={{ fontSize: 13 }} />
                      <YAxis tick={{ fontSize: 13 }} />
                      <Tooltip />
                      <Legend />
                      <Bar dataKey="count" name="Policies Sold" fill="#1565C0" radius={[6, 6, 0, 0]} />
                    </BarChart>
                  </ResponsiveContainer>
                  <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', mt: 1, pt: 1, borderTop: '1px solid', borderColor: 'divider' }}>
                    <Typography variant="body2">Total Sold: <strong>{policies?.totalPoliciesSold ?? 0}</strong></Typography>
                    <Typography variant="body2">Active: <strong>{policies?.activePolicies ?? 0}</strong></Typography>
                    <Typography variant="body2">Renewal Rate: <strong>{policies?.policyRenewalRate ? `${policies.policyRenewalRate.toFixed(1)}%` : '—'}</strong></Typography>
                  </Box>
                </>
              )}
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* ── 3. User Role Distribution + Claims Summary — 2 equal columns ── */}
      <Box sx={{ display: 'flex', gap: 3, mb: 3, flexWrap: 'nowrap' }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={700} mb={2}>User Role Distribution</Typography>
              {usersByRole.length === 0 ? <EmptyState /> : (
                <>
                  <ResponsiveContainer width="100%" height={CHART_H}>
                    <PieChart>
                      <Pie
                        data={usersByRole}
                        dataKey="value"
                        nameKey="name"
                        cx="50%"
                        cy="50%"
                        outerRadius={160}
                        labelLine={false}
                        label={renderSliceLabel}
                      >
                        {usersByRole.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                      </Pie>
                      <Tooltip />
                      <Legend
                        layout="horizontal"
                        verticalAlign="bottom"
                        align="center"
                        iconSize={12}
                        formatter={(v) => <span style={{ fontSize: 13 }}>{v}</span>}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                  <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', mt: 1, pt: 1, borderTop: '1px solid', borderColor: 'divider' }}>
                    <Typography variant="body2">Total: <strong>{users?.totalUsers ?? 0}</strong></Typography>
                    <Typography variant="body2">Active: <strong>{users?.activeUsers ?? 0}</strong></Typography>
                    <Typography variant="body2">Inactive: <strong>{users?.inactiveUsers ?? 0}</strong></Typography>
                  </Box>
                </>
              )}
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Card sx={{ height: '100%' }}>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={700} mb={2}>Claims Summary</Typography>
              {!claims || claims.totalClaims === 0 ? <EmptyState /> : (
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, mt: 1 }}>
                  {[
                    { label: 'Total Claims', value: claims.totalClaims, color: 'primary.main', bg: '#E3F2FD' },
                    { label: 'Approval Rate', value: claims.claimApprovalRate ? `${claims.claimApprovalRate.toFixed(1)}%` : '—', color: 'success.main', bg: '#E8F5E9' },
                    { label: 'Rejection Rate', value: claims.claimRejectionRate ? `${claims.claimRejectionRate.toFixed(1)}%` : '—', color: 'error.main', bg: '#FFEBEE' },
                    { label: 'Avg Processing Time', value: claims.averageClaimProcessingTimeHours ? `${claims.averageClaimProcessingTimeHours.toFixed(1)} hrs` : '—', color: 'warning.main', bg: '#FFF8E1' },
                  ].map((item) => (
                    <Box
                      key={item.label}
                      sx={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        p: 3,
                        bgcolor: item.bg,
                        borderRadius: 2,
                      }}
                    >
                      <Typography variant="body1" fontWeight={500} color="text.secondary">{item.label}</Typography>
                      <Typography variant="h4" fontWeight={700} color={item.color}>{item.value}</Typography>
                    </Box>
                  ))}
                </Box>
              )}
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* ── 4. Specialist Performance — full width ── */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={700} mb={2}>Specialist Performance</Typography>
          {specialists.length === 0 ? (
            <Alert severity="info">No specialist performance data available yet</Alert>
          ) : (
            <>
              <ResponsiveContainer width="100%" height={CHART_H}>
                <BarChart data={specialists} margin={{ top: 10, right: 40, left: 20, bottom: 60 }}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" tick={{ fontSize: 13 }} angle={-25} textAnchor="end" interval={0} />
                  <YAxis tick={{ fontSize: 13 }} />
                  <Tooltip />
                  <Legend verticalAlign="top" />
                  <Bar dataKey="claimsProcessed" name="Claims Processed" fill="#1565C0" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
              <Table size="small" sx={{ mt: 2 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>Name</TableCell>
                    <TableCell align="right">Claims Processed</TableCell>
                    <TableCell align="right">Approval Rate</TableCell>
                    <TableCell align="right">Avg Processing</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {specialists.map((s) => (
                    <TableRow key={s.userId} hover>
                      <TableCell>{s.name}</TableCell>
                      <TableCell align="right">{s.claimsProcessed}</TableCell>
                      <TableCell align="right">{s.approvalRate ? `${s.approvalRate.toFixed(1)}%` : '—'}</TableCell>
                      <TableCell align="right">{s.averageProcessingTimeHours ? `${(s.averageProcessingTimeHours / 24).toFixed(1)} days` : '—'}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </>
          )}
        </CardContent>
      </Card>
    </Box>
  );
}
