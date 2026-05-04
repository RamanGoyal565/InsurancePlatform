import { useState } from 'react';
import {
  Box, Card, CardContent, Chip, MenuItem, Table, TableBody,
  TableCell, TableHead, TablePagination, TableRow, TextField, Typography,
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import PendingIcon from '@mui/icons-material/Pending';
import CurrencyRupeeIcon from '@mui/icons-material/CurrencyRupee';
import { usePayments } from '../../hooks/usePayments';
import StatCard from '../../components/ui/StatCard';

const STATUS_LABELS = { 1: 'Pending', 2: 'Completed', 3: 'Failed' };
const STATUS_COLORS = { 1: 'warning', 2: 'success', 3: 'error' };

export default function AdminPayments() {
  const { data: payments = [], isLoading } = usePayments();
  const [filterStatus, setFilterStatus] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(0);
  const rowsPerPage = 10;

  const completed = payments.filter((p) => p.status === 2);
  const failed = payments.filter((p) => p.status === 3);
  const pending = payments.filter((p) => p.status === 1);
  const total = completed.reduce((s, p) => s + (p.amount || 0), 0);

  const filtered = payments.filter((p) => {
    if (filterStatus && p.status !== Number(filterStatus)) return false;
    if (search) {
      const q = search.toLowerCase();
      return (
        p.paymentReference?.toLowerCase().includes(q) ||
        p.customerId?.toLowerCase().includes(q)
      );
    }
    return true;
  });

  return (
    <Box>
      <Typography variant="h5" fontWeight={700} mb={0.5}>Payments</Typography>
      <Typography variant="body2" color="text.secondary" mb={3}>
        Track and manage all payment transactions
      </Typography>

      {/* Overview — full width flex */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard
            title="Successful Payments"
            value={completed.length}
            icon={<CheckCircleIcon color="success" />}
            loading={isLoading}
          />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard
            title="Failed Payments"
            value={failed.length}
            icon={<CancelIcon color="error" />}
            loading={isLoading}
          />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard
            title="Pending Payments"
            value={pending.length}
            icon={<PendingIcon color="warning" />}
            loading={isLoading}
          />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard
            title="Total Amount (Completed)"
            value={`₹${total.toLocaleString()}`}
            icon={<CurrencyRupeeIcon color="primary" />}
            loading={isLoading}
          />
        </Box>
      </Box>

      {/* Filters + Table */}
      <Card>
        <CardContent>
          <Box sx={{ display: 'flex', gap: 2, mb: 2, flexWrap: 'wrap' }}>
            <TextField
              placeholder="Search by reference or customer ID..."
              size="small"
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(0); }}
              sx={{ flexGrow: 1, minWidth: 220 }}
            />
            <TextField
              select
              size="small"
              label="Status"
              value={filterStatus}
              onChange={(e) => { setFilterStatus(e.target.value); setPage(0); }}
              sx={{ width: 140 }}
            >
              <MenuItem value="">All Statuses</MenuItem>
              <MenuItem value="1">Pending</MenuItem>
              <MenuItem value="2">Completed</MenuItem>
              <MenuItem value="3">Failed</MenuItem>
            </TextField>
          </Box>

          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Payment Reference</TableCell>
                <TableCell>Customer ID</TableCell>
                <TableCell>Policy ID</TableCell>
                <TableCell>Source</TableCell>
                <TableCell align="right">Amount</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Date</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={7} align="center">Loading...</TableCell>
                </TableRow>
              ) : filtered.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage).map((p) => (
                <TableRow key={p.paymentId} hover>
                  <TableCell sx={{ color: 'primary.main', fontWeight: 600 }}>
                    {p.paymentReference || `PAY-${p.paymentId?.slice(0, 8).toUpperCase()}`}
                  </TableCell>
                  <TableCell sx={{ fontFamily: 'monospace', fontSize: 12 }}>
                    {p.customerId?.slice(0, 8)}…
                  </TableCell>
                  <TableCell sx={{ fontFamily: 'monospace', fontSize: 12 }}>
                    {p.policyId ? `${p.policyId.slice(0, 8)}…` : '—'}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={p.source || 'Manual'}
                      size="small"
                      variant="outlined"
                      color={p.source === 'PolicyWorkflow' ? 'primary' : 'default'}
                    />
                  </TableCell>
                  <TableCell align="right" sx={{ fontWeight: 600 }}>
                    ₹{p.amount?.toLocaleString()}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={STATUS_LABELS[p.status] || p.status}
                      color={STATUS_COLORS[p.status] || 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>{new Date(p.createdAt).toLocaleDateString()}</TableCell>
                </TableRow>
              ))}
              {filtered.length === 0 && !isLoading && (
                <TableRow>
                  <TableCell colSpan={7} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                    No payments found
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>

          <TablePagination
            component="div"
            count={filtered.length}
            page={page}
            onPageChange={(_, p) => setPage(p)}
            rowsPerPage={rowsPerPage}
            rowsPerPageOptions={[10, 25, 50]}
            onRowsPerPageChange={() => setPage(0)}
          />
        </CardContent>
      </Card>
    </Box>
  );
}
