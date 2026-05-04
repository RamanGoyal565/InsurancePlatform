import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box, Card, CardContent, Chip, IconButton, MenuItem,
  Tab, Table, TableBody, TableCell, TableHead, TablePagination,
  TableRow, Tabs, TextField, Typography,
} from '@mui/material';
import VisibilityIcon from '@mui/icons-material/Visibility';
import ConfirmationNumberIcon from '@mui/icons-material/ConfirmationNumber';
import AssignmentIcon from '@mui/icons-material/Assignment';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import PendingIcon from '@mui/icons-material/Pending';
import RateReviewIcon from '@mui/icons-material/RateReview';
import { useAuth } from '../../app/AuthContext';
import { useTickets } from '../../hooks/useTickets';
import { ROLES } from '../../app/roles';
import StatCard from '../../components/ui/StatCard';
import StatusChip from '../../components/ui/StatusChip';

const TICKET_STATUS = ['', 'Open', 'InReview', 'Assigned', 'Resolved', 'Rejected', 'Closed'];
const TICKET_TYPE = ['', 'Support', 'Claim'];

export default function SpecialistDashboard() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const { data: allTickets = [], isLoading } = useTickets();

  const [tab, setTab] = useState(0);
  const [filterStatus, setFilterStatus] = useState('');
  const [page, setPage] = useState(0);
  const rowsPerPage = 10;

  const isClaimsSpecialist = user?.role === ROLES.CLAIMS_SPECIALIST;
  const isSupportSpecialist = user?.role === ROLES.SUPPORT_SPECIALIST;

  // Filter tickets relevant to this specialist
  const myTickets = allTickets.filter((t) => {
    const assignedToMe = t.assignedTo === user?.userId;
    const isMyType = isClaimsSpecialist ? t.type === 2 : t.type === 1;
    return assignedToMe || (!t.assignedTo && isMyType);
  });

  // KPI counts
  const assignedToMe = myTickets.filter((t) => t.assignedTo === user?.userId);
  const openTickets = myTickets.filter((t) => t.status === 1);
  const inReview = myTickets.filter((t) => t.status === 2);
  const resolved = myTickets.filter((t) => t.status === 4);

  // Tab: specialists see only their type
  const tabs = isClaimsSpecialist
    ? [{ label: `Claim Tickets (${myTickets.length})`, tickets: myTickets }]
    : [{ label: `Support Tickets (${myTickets.length})`, tickets: myTickets }];

  const filtered = (tabs[tab]?.tickets ?? []).filter((t) => {
    if (filterStatus && t.status !== Number(filterStatus)) return false;
    return true;
  });

  const roleLabel = isClaimsSpecialist ? 'Claims Specialist' : 'Support Specialist';
  const ticketLabel = isClaimsSpecialist ? 'claim' : 'support';

  return (
    <Box>
      {/* ── Header ── */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="h5" fontWeight={700}>Welcome, {user?.name}</Typography>
        <Typography variant="body2" color="text.secondary">{roleLabel} Dashboard</Typography>
      </Box>

      {/* ── KPI Cards — full width flex ── */}
      <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard
            title="Total Tickets"
            value={myTickets.length}
            icon={<ConfirmationNumberIcon color="primary" />}
            loading={isLoading}
          />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard
            title="Assigned to Me"
            value={assignedToMe.length}
            icon={<AssignmentIcon color="warning" />}
            loading={isLoading}
          />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard
            title="In Review"
            value={inReview.length}
            icon={<RateReviewIcon color="info" />}
            loading={isLoading}
          />
        </Box>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <StatCard
            title="Resolved"
            value={resolved.length}
            icon={<CheckCircleIcon color="success" />}
            loading={isLoading}
          />
        </Box>
      </Box>

      {/* ── Ticket Table ── */}
      <Card>
        {/* Table header row */}
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            px: 2,
            pt: 1,
            borderBottom: 1,
            borderColor: 'divider',
            flexWrap: 'wrap',
            gap: 1,
          }}
        >
          <Tabs value={tab} onChange={(_, v) => { setTab(v); setPage(0); }}>
            {tabs.map((t) => <Tab key={t.label} label={t.label} />)}
          </Tabs>

          <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', pb: 1 }}>
            <Chip
              label={`Assigned to me: ${assignedToMe.length}`}
              color={assignedToMe.length > 0 ? 'primary' : 'default'}
              size="small"
              variant="outlined"
            />
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
          </Box>
        </Box>

        <CardContent sx={{ p: 0 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Ticket ID</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Title</TableCell>
                <TableCell>Policy</TableCell>
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
                <TableRow
                  key={t.ticketId}
                  hover
                  sx={{ cursor: 'pointer' }}
                  onClick={() => navigate(`/specialist/tickets/${t.ticketId}`)}
                >
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
                  <TableCell sx={{ maxWidth: 200 }}>
                    <Typography variant="body2" noWrap>{t.title}</Typography>
                  </TableCell>
                  <TableCell>
                    {t.policyId ? `POL-${t.policyId.slice(0, 8).toUpperCase()}` : '—'}
                  </TableCell>
                  <TableCell>
                    {t.assignedTo
                      ? (
                        <Chip
                          label={t.assignedTo === user?.userId ? 'Me' : t.assignedTo.slice(0, 8)}
                          size="small"
                          color={t.assignedTo === user?.userId ? 'primary' : 'default'}
                        />
                      )
                      : <Chip label="Unassigned" size="small" variant="outlined" />}
                  </TableCell>
                  <TableCell>
                    <StatusChip status={TICKET_STATUS[t.status] || String(t.status)} />
                  </TableCell>
                  <TableCell>{new Date(t.updatedAt).toLocaleDateString()}</TableCell>
                  <TableCell onClick={(e) => e.stopPropagation()}>
                    <IconButton
                      size="small"
                      color="primary"
                      onClick={() => navigate(`/specialist/tickets/${t.ticketId}`)}
                    >
                      <VisibilityIcon />
                    </IconButton>
                  </TableCell>
                </TableRow>
              ))}
              {filtered.length === 0 && !isLoading && (
                <TableRow>
                  <TableCell colSpan={8} align="center" sx={{ py: 4, color: 'text.secondary' }}>
                    No {ticketLabel} tickets assigned to you or unassigned
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
            rowsPerPageOptions={[10, 25]}
            onRowsPerPageChange={(e) => { setPage(0); }}
          />
        </CardContent>
      </Card>
    </Box>
  );
}
