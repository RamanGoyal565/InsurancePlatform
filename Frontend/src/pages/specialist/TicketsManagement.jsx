import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Card, CardContent, Chip, IconButton, MenuItem,
  Tab, Table, TableBody, TableCell, TableHead, TablePagination,
  TableRow, Tabs, TextField, Typography,
} from '@mui/material';
import VisibilityIcon from '@mui/icons-material/Visibility';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import { useTickets } from '../../hooks/useTickets';
import { useAuth } from '../../app/AuthContext';
import { ROLES } from '../../app/roles';
import StatusChip from '../../components/ui/StatusChip';

const TICKET_STATUS = ['', 'Open', 'InReview', 'Assigned', 'Resolved', 'Rejected', 'Closed'];
const TICKET_TYPE = ['', 'Support', 'Claim'];

export default function TicketsManagement() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { data: allTickets = [], isLoading } = useTickets();
  const [tab, setTab] = useState(0);
  const [filterStatus, setFilterStatus] = useState('');
  const [page, setPage] = useState(0);
  const rowsPerPage = 10;

  const isAdmin = user?.role === ROLES.ADMIN;
  const isClaimsSpecialist = user?.role === ROLES.CLAIMS_SPECIALIST;
  const isSupportSpecialist = user?.role === ROLES.SUPPORT_SPECIALIST;
  const isSpecialist = isClaimsSpecialist || isSupportSpecialist;

  /**
   * Specialists only see tickets assigned to them OR unassigned ones of their type.
   * Admin sees everything.
   */
  const visibleTickets = isAdmin
    ? allTickets
    : allTickets.filter((t) => {
        const assignedToMe = t.assignedTo === user?.userId;
        const isMyType = isClaimsSpecialist ? t.type === 2 : t.type === 1;
        return assignedToMe || (!t.assignedTo && isMyType);
      });

  const supportTickets = visibleTickets.filter((t) => t.type === 1);
  const claimTickets = visibleTickets.filter((t) => t.type === 2);

  /**
   * Tab logic:
   * - ClaimsSpecialist  → only "Claim Tickets" tab (index 0 maps to claims)
   * - SupportSpecialist → only "Support Tickets" tab (index 0 maps to support)
   * - Admin             → both tabs, tab 0 = support, tab 1 = claims
   */
  const tabs = isClaimsSpecialist
    ? [{ label: `Claim Tickets (${claimTickets.length})`, tickets: claimTickets }]
    : isSupportSpecialist
      ? [{ label: `Support Tickets (${supportTickets.length})`, tickets: supportTickets }]
      : [
          { label: `Support Tickets (${supportTickets.length})`, tickets: supportTickets },
          { label: `Claim Tickets (${claimTickets.length})`, tickets: claimTickets },
        ];

  const filtered = (tabs[tab]?.tickets ?? []).filter((t) => {
    if (filterStatus && t.status !== Number(filterStatus)) return false;
    return true;
  });

  const myAssignedCount = allTickets.filter((t) => t.assignedTo === user?.userId).length;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>Tickets & Claims</Typography>
          <Typography variant="body2" color="text.secondary">
            {isAdmin
              ? 'Manage all support and claim tickets'
              : isClaimsSpecialist
                ? 'Showing claim tickets assigned to you or unassigned'
                : 'Showing support tickets assigned to you or unassigned'}
          </Typography>
        </Box>

        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
          {/* Assigned-to-me count badge — read-only info for specialists */}
          {isSpecialist && (
            <Chip
              label={`Assigned to me: ${myAssignedCount}`}
              color={myAssignedCount > 0 ? 'primary' : 'default'}
              size="small"
              variant="outlined"
            />
          )}

          <TextField
            select
            size="small"
            value={filterStatus}
            onChange={(e) => { setFilterStatus(e.target.value); setPage(0); }}
            sx={{ width: 140 }}
            label="Status"
          >
            <MenuItem value="">All Statuses</MenuItem>
            {TICKET_STATUS.filter(Boolean).map((s, i) => (
              <MenuItem key={s} value={i + 1}>{s}</MenuItem>
            ))}
          </TextField>

          {/* Admin-only: type filter (specialists already see only their type) */}
          {isAdmin && (
            <TextField
              select
              size="small"
              sx={{ width: 120 }}
              label="Type"
              defaultValue=""
              onChange={(e) => setTab(e.target.value === '2' ? 1 : 0)}
            >
              <MenuItem value="">All Types</MenuItem>
              <MenuItem value="1">Support</MenuItem>
              <MenuItem value="2">Claim</MenuItem>
            </TextField>
          )}
        </Box>
      </Box>

      <Card>
        <Box sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
          <Tabs value={tab} onChange={(_, v) => { setTab(v); setPage(0); }}>
            {tabs.map((t) => (
              <Tab key={t.label} label={t.label} />
            ))}
          </Tabs>
        </Box>

        <CardContent sx={{ p: 0 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Ticket ID</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Policy</TableCell>
                <TableCell>Customer</TableCell>
                <TableCell>Assigned To</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Last Updated</TableCell>
                <TableCell>Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={8} align="center">Loading...</TableCell>
                </TableRow>
              ) : filtered.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage).map((t) => (
                <TableRow key={t.ticketId} hover>
                  <TableCell sx={{ color: 'primary.main', fontWeight: 600 }}>
                    TKT-{t.ticketId?.slice(0, 8).toUpperCase()}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={TICKET_TYPE[t.type] || t.type}
                      size="small"
                      color={t.type === 2 ? 'warning' : 'info'}
                    />
                  </TableCell>
                  <TableCell>
                    {t.policyId ? `POL-${t.policyId.slice(0, 8).toUpperCase()}` : '—'}
                  </TableCell>
                  <TableCell>{t.customerId?.slice(0, 8)}</TableCell>
                  <TableCell>
                    {t.assignedTo
                      ? <Chip label={t.assignedTo === user?.userId ? 'Me' : t.assignedTo.slice(0, 8)} size="small" color={t.assignedTo === user?.userId ? 'primary' : 'default'} />
                      : <Chip label="Unassigned" size="small" variant="outlined" />}
                  </TableCell>
                  <TableCell>
                    <StatusChip status={TICKET_STATUS[t.status] || String(t.status)} />
                  </TableCell>
                  <TableCell>{new Date(t.updatedAt).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <IconButton
                      size="small"
                      onClick={() => navigate(`/specialist/tickets/${t.ticketId}`)}
                    >
                      <VisibilityIcon />
                    </IconButton>
                    <IconButton size="small">
                      <MoreVertIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
              {filtered.length === 0 && !isLoading && (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                    {isSpecialist
                      ? `No ${isClaimsSpecialist ? 'claim' : 'support'} tickets assigned to you or unassigned`
                      : 'No tickets found'}
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
            rowsPerPageOptions={[10]}
          />
        </CardContent>
      </Card>
    </Box>
  );
}
