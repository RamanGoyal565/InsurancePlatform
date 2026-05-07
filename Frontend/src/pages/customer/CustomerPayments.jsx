import {
  Box, Card, CardContent, Chip, Table, TableBody,
  TableCell, TableHead, TableRow, Typography,
} from '@mui/material';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import CancelIcon from '@mui/icons-material/Cancel';
import PendingIcon from '@mui/icons-material/Pending';
import CurrencyRupeeIcon from '@mui/icons-material/CurrencyRupee';
import { usePayments } from '../../hooks/usePayments';
import StatCard from '../../components/ui/StatCard';

const STATUS_LABELS = { 1: 'Pending', 2: 'Completed', 3: 'Failed' };
const STATUS_COLORS = { 1: 'warning', 2: 'success', 3: 'error' };

export default function CustomerPayments() {
  const { data: payments = [], isLoading } = usePayments();

  const completed = payments.filter((p) => p.status === 2);
  const failed = payments.filter((p) => p.status === 3);
  const pending = payments.filter((p) => p.status === 1);
  const total = completed.reduce((s, p) => s + (p.amount || 0), 0);

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>Payments</Typography>
          <Typography variant="body2" color="text.secondary">Track and manage all payment transactions</Typography>
        </Box>
      </Box>

      {/* Overview — 4 equal cards, full width */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard title="Successful Payments" value={completed.length} icon={<CheckCircleIcon color="success" />} loading={isLoading} />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard title="Failed Payments" value={failed.length} icon={<CancelIcon color="error" />} loading={isLoading} />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard title="Pending Payments" value={pending.length} icon={<PendingIcon color="warning" />} loading={isLoading} />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard title="Total Amount" value={`₹${total.toLocaleString()}`} icon={<CurrencyRupeeIcon color="primary" />} loading={isLoading} />
        </Box>
      </Box>

      {/* Transactions */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={600} mb={2}>Payment Transactions</Typography>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Payment Reference</TableCell>
                <TableCell>Amount</TableCell>
                <TableCell>Source</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Date</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={5} align="center">Loading...</TableCell></TableRow>
              ) : payments.map((p) => (
                <TableRow key={p.paymentId} hover>
                  <TableCell sx={{ color: 'primary.main', fontWeight: 600 }}>
                    PAY-{p.paymentReference || p.paymentId?.slice(0, 8).toUpperCase()}
                  </TableCell>
                  <TableCell>₹{p.amount?.toLocaleString()}</TableCell>
                  <TableCell>
                    <Chip label={p.source || 'Manual'} size="small" variant="outlined" />
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={STATUS_LABELS[p.status] || String(p.status)}
                      color={STATUS_COLORS[p.status] || 'default'}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>{new Date(p.createdAt).toLocaleDateString()}</TableCell>
                </TableRow>
              ))}
              {payments.length === 0 && !isLoading && (
                <TableRow>
                  <TableCell colSpan={5} align="center" sx={{ py: 3, color: 'text.secondary' }}>
                    No payments found
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
