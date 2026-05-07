import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  Alert, Avatar, Box, Button, Card, CardContent, Chip, CircularProgress, Divider,
  Grid, IconButton, MenuItem, TextField, Tooltip, Typography,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import CheckIcon from '@mui/icons-material/Check';
import CloseIcon from '@mui/icons-material/Close';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import PersonAddIcon from '@mui/icons-material/PersonAdd';
import {
  useTicket, useTicketComments, useUpdateTicketStatus,
  useAddComment, useApproveClaim, useRejectClaim, useAssignTicket,
} from '../../hooks/useTickets';
import { useUsers } from '../../hooks/useUsers';
import { useAuth } from '../../app/AuthContext';
import { ROLES } from '../../app/roles';
import StatusChip from '../../components/ui/StatusChip';
import PdfUpload from '../../components/ui/PdfUpload';
import Base64PdfViewer from '../../components/ui/Base64PdfViewer';

const TICKET_STATUS = ['', 'Open', 'InReview', 'Assigned', 'Resolved', 'Rejected', 'Closed'];
const TICKET_TYPE = ['', 'Support', 'Claim'];

/**
 * Valid forward transitions per current status.
 * Status values: Open=1, InReview=2, Assigned=3, Resolved=4, Rejected=5, Closed=6
 *
 * Flow: Open → Assigned → InReview → Resolved | Rejected → Closed
 */
const NEXT_STATUSES = {
  1: [3],          // Open → Assigned
  3: [2],          // Assigned → InReview
  2: [4, 5],       // InReview → Resolved or Rejected
  4: [6],          // Resolved → Closed
  5: [6],          // Rejected → Closed
  6: [],           // Closed — terminal
};

export default function TicketDetail() {
  const { ticketId } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();

  const { data: ticket, isLoading: ticketLoading } = useTicket(ticketId);
  const { data: comments = [] } = useTicketComments(ticketId);
  const { data: allUsers = [] } = useUsers();

  const updateStatus = useUpdateTicketStatus();
  const addComment = useAddComment();
  const approveClaim = useApproveClaim();
  const rejectClaim = useRejectClaim();
  const assignTicket = useAssignTicket();

  const [comment, setComment] = useState('');
  const [rejectReason, setRejectReason] = useState('');
  const [commentDoc, setCommentDoc] = useState({ base64: null, fileName: null });
  const [assignTo, setAssignTo] = useState('');

  // PDF viewer state
  const [pdfViewer, setPdfViewer] = useState({ open: false, base64: null, fileName: null });
  const openPdf = (base64, fileName) => setPdfViewer({ open: true, base64, fileName });
  const closePdf = () => setPdfViewer({ open: false, base64: null, fileName: null });

  if (ticketLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: 200 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!ticket) {
    return (
      <Box>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(-1)}>Back</Button>
        <Alert severity="error" sx={{ mt: 2 }}>Ticket not found or you don&apos;t have access to it.</Alert>
      </Box>
    );
  }

  const isAdmin = user?.role === ROLES.ADMIN;
  const isClaimsSpecialist = user?.role === ROLES.CLAIMS_SPECIALIST;
  const isClaim = ticket.type === 2;

  // Claim is already decided if approval status is Approved (3) or Rejected (4)
  const claimAlreadyDecided =
    isClaim &&
    ticket.claimDetails &&
    (ticket.claimDetails.approvalStatus === 3 || ticket.claimDetails.approvalStatus === 4);

  // Ticket is in a terminal state — status cannot be changed
  const isTerminal = ticket.status === 4 || ticket.status === 5 || ticket.status === 6; // Resolved, Rejected, Closed

  // Filter specialists relevant to this ticket type for the assign dropdown
  const relevantSpecialists = allUsers.filter((u) =>
    isClaim
      ? u.role === 'ClaimsSpecialist'
      : u.role === 'SupportSpecialist'
  );

  const handleComment = async () => {
    if (!comment.trim()) return;
    await addComment.mutateAsync({
      ticketId,
      message: comment,
      documentBase64: commentDoc.base64 || undefined,
      documentFileName: commentDoc.fileName || undefined,
    });
    setComment('');
    setCommentDoc({ base64: null, fileName: null });
  };

  const handleAssign = async () => {
    if (!assignTo) return;
    await assignTicket.mutateAsync({ ticketId, assignedToUserId: assignTo });
    setAssignTo('');
  };

  return (
    <Box>
      {/* Top bar */}
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3, flexWrap: 'wrap' }}>
        <IconButton onClick={() => navigate(-1)}><ArrowBackIcon /></IconButton>
        <Typography variant="h6" fontWeight={700}>
          TKT-{ticket.ticketId?.slice(0, 8).toUpperCase()}
        </Typography>
        <StatusChip status={TICKET_STATUS[ticket.status] || String(ticket.status)} />
        <Box sx={{ flexGrow: 1 }} />
        {/* Only show status dropdown when there are valid next steps */}
        {!isTerminal && (NEXT_STATUSES[ticket.status] ?? []).length > 0 && (
          <TextField
            select size="small" label="Move to"
            value=""
            onChange={(e) => updateStatus.mutate({ ticketId, status: Number(e.target.value) })}
            sx={{ width: 160 }}
          >
            {(NEXT_STATUSES[ticket.status] ?? []).map((statusVal) => (
              <MenuItem key={statusVal} value={statusVal}>
                {TICKET_STATUS[statusVal]}
              </MenuItem>
            ))}
          </TextField>
        )}
      </Box>

      <Grid container spacing={3}>
        {/* ── Main content ── */}
        <Grid item xs={12} md={8}>

          {/* Ticket info card */}
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Grid container spacing={2}>
                <Grid item xs={6} sm={3}>
                  <Typography variant="caption" color="text.secondary">Type</Typography>
                  <Box mt={0.5}>
                    <Chip label={TICKET_TYPE[ticket.type]} size="small" color={isClaim ? 'warning' : 'info'} />
                  </Box>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="caption" color="text.secondary">Policy</Typography>
                  <Typography variant="body2" fontWeight={600}>
                    {ticket.policyId ? `POL-${ticket.policyId.slice(0, 8).toUpperCase()}` : '—'}
                  </Typography>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="caption" color="text.secondary">Customer</Typography>
                  <Typography variant="body2" fontWeight={600}>
                    {ticket.customerId?.slice(0, 8)}
                  </Typography>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="caption" color="text.secondary">Created On</Typography>
                  <Typography variant="body2">{new Date(ticket.createdAt).toLocaleString()}</Typography>
                </Grid>
                <Grid item xs={6} sm={3}>
                  <Typography variant="caption" color="text.secondary">Assigned To</Typography>
                  <Typography variant="body2" fontWeight={600}>
                    {ticket.assignedTo
                      ? allUsers.find((u) => u.userId === ticket.assignedTo)?.name
                        || ticket.assignedTo.slice(0, 8)
                      : <Chip label="Unassigned" size="small" variant="outlined" />}
                  </Typography>
                </Grid>
              </Grid>

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle1" fontWeight={600} mb={1}>{ticket.title}</Typography>
              <Typography variant="body2" color="text.secondary">{ticket.description}</Typography>

              {/* Ticket-level document — shown for ALL ticket types */}
              {ticket.documentBase64 && (
                <Box
                  sx={{
                    display: 'inline-flex', alignItems: 'center', gap: 0.5, mt: 1.5,
                    cursor: 'pointer', color: 'primary.main',
                    '&:hover': { textDecoration: 'underline' },
                  }}
                  onClick={() => openPdf(ticket.documentBase64, ticket.documentFileName)}
                >
                  <PictureAsPdfIcon fontSize="small" color="error" />
                  <Typography variant="body2" fontWeight={500}>
                    {ticket.documentFileName || 'View Attachment'}
                  </Typography>
                </Box>
              )}

              {/* Claim details */}
              {isClaim && ticket.claimDetails && (
                <Box sx={{ mt: 2, p: 2, bgcolor: 'grey.50', borderRadius: 2 }}>
                  <Typography variant="subtitle2" fontWeight={600} mb={1}>Claim Details</Typography>
                  <Typography variant="body2">
                    Claim Amount: <strong>₹{ticket.claimDetails.claimAmount?.toLocaleString()}</strong>
                  </Typography>
                  <Typography variant="body2">
                    Approval Status:{' '}
                    <strong>
                      {['', 'Pending', 'Verified', 'Approved', 'Rejected'][ticket.claimDetails.approvalStatus]}
                    </strong>
                  </Typography>
                </Box>
              )}
            </CardContent>
          </Card>

          {/* Comments */}
          <Card>
            <CardContent>
              <Typography variant="subtitle1" fontWeight={600} mb={2}>
                Comments ({comments.length})
              </Typography>

              {comments.length === 0 && (
                <Typography variant="body2" color="text.secondary" mb={2}>
                  No comments yet.
                </Typography>
              )}

              {comments.map((c) => (
                <Box key={c.commentId} sx={{ display: 'flex', gap: 1.5, mb: 2 }}>
                  <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 12 }}>
                    {(allUsers.find((u) => u.userId === c.userId)?.name || c.userId)
                      ?.slice(0, 2).toUpperCase()}
                  </Avatar>
                  <Box sx={{ flexGrow: 1 }}>
                    <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
                      <Typography variant="caption" fontWeight={600}>
                        {allUsers.find((u) => u.userId === c.userId)?.name || c.userId?.slice(0, 8)}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {new Date(c.createdAt).toLocaleString()}
                      </Typography>
                    </Box>
                    <Typography variant="body2" sx={{ mt: 0.25 }}>{c.message}</Typography>

                    {/* Comment PDF attachment — click to view */}
                    {c.documentBase64 && (
                      <Tooltip title="Click to view document">
                        <Box
                          sx={{
                            display: 'inline-flex', alignItems: 'center', gap: 0.5, mt: 0.5,
                            cursor: 'pointer', color: 'primary.main',
                            '&:hover': { textDecoration: 'underline' },
                          }}
                          onClick={() => openPdf(c.documentBase64, c.documentFileName)}
                        >
                          <PictureAsPdfIcon fontSize="small" color="error" />
                          <Typography variant="caption">
                            {c.documentFileName || 'attachment.pdf'}
                          </Typography>
                        </Box>
                      </Tooltip>
                    )}
                  </Box>
                </Box>
              ))}

              <Divider sx={{ my: 2 }} />

              {/* Add comment */}
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                <TextField
                  fullWidth size="small"
                  placeholder="Write a comment..."
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                  multiline rows={2}
                />
                <PdfUpload
                  onChange={(b64, name) => setCommentDoc({ base64: b64, fileName: name })}
                />
                <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                  <Button
                    variant="contained"
                    onClick={handleComment}
                    disabled={!comment.trim() || addComment.isPending}
                  >
                    Post Comment
                  </Button>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        {/* ── Sidebar ── */}
        <Grid item xs={12} md={4}>

          {/* Admin: Assign / Reassign ticket */}
          {isAdmin && (
            <Card sx={{ mb: 2 }}>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>
                  <PersonAddIcon sx={{ fontSize: 18, mr: 0.5, verticalAlign: 'middle' }} />
                  {ticket.assignedTo ? 'Reassign Ticket' : 'Assign Ticket'}
                </Typography>

                {/* Current assignment banner */}
                {ticket.assignedTo && (
                  <Box sx={{ mb: 2, p: 1.5, bgcolor: 'primary.50', borderRadius: 1, display: 'flex', alignItems: 'center', gap: 1 }}>
                    <PersonAddIcon fontSize="small" color="primary" />
                    <Box>
                      <Typography variant="caption" color="text.secondary">Currently assigned to</Typography>
                      <Typography variant="body2" fontWeight={600}>
                        {allUsers.find((u) => u.userId === ticket.assignedTo)?.name
                          || ticket.assignedTo.slice(0, 8)}
                      </Typography>
                    </Box>
                  </Box>
                )}

                {relevantSpecialists.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    No {isClaim ? 'claims' : 'support'} specialists available.
                  </Typography>
                ) : (
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                    <TextField
                      select
                      fullWidth
                      size="small"
                      label={`${ticket.assignedTo ? 'Reassign' : 'Assign'} to ${isClaim ? 'Claims' : 'Support'} Specialist`}
                      value={assignTo}
                      onChange={(e) => setAssignTo(e.target.value)}
                    >
                      <MenuItem value="">Select specialist</MenuItem>
                      {relevantSpecialists.map((u) => (
                        <MenuItem key={u.userId} value={u.userId}>
                          <Box>
                            <Typography variant="body2">{u.name}</Typography>
                            <Typography variant="caption" color="text.secondary">{u.email}</Typography>
                          </Box>
                        </MenuItem>
                      ))}
                    </TextField>
                    <Button
                      variant="contained"
                      fullWidth
                      color={ticket.assignedTo ? 'warning' : 'primary'}
                      startIcon={<PersonAddIcon />}
                      onClick={handleAssign}
                      disabled={!assignTo || assignTicket.isPending}
                    >
                      {assignTicket.isPending
                        ? (ticket.assignedTo ? 'Reassigning...' : 'Assigning...')
                        : (ticket.assignedTo ? 'Reassign' : 'Assign')}
                    </Button>
                  </Box>
                )}
              </CardContent>
            </Card>
          )}

          {/* Claims specialist: approve / reject — only when not yet decided */}
          {isClaimsSpecialist && isClaim && !claimAlreadyDecided && (
            <Card sx={{ mb: 2 }}>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={600} mb={2}>Decision</Typography>
                <Button
                  variant="contained" color="success" fullWidth
                  startIcon={<CheckIcon />} sx={{ mb: 1 }}
                  onClick={() => approveClaim.mutate(ticketId)}
                  disabled={approveClaim.isPending}
                >
                  Approve Claim
                </Button>
                <Button
                  variant="contained" color="error" fullWidth
                  startIcon={<CloseIcon />}
                  onClick={() => rejectClaim.mutate(ticketId)}
                  disabled={rejectClaim.isPending}
                >
                  Reject Claim
                </Button>
                <TextField
                  label="Reason (if rejecting)" fullWidth multiline rows={2}
                  size="small" sx={{ mt: 1 }}
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                />
              </CardContent>
            </Card>
          )}

          {/* Show final decision status when already decided */}
          {isClaimsSpecialist && isClaim && claimAlreadyDecided && (
            <Card sx={{ mb: 2 }}>
              <CardContent>
                <Typography variant="subtitle1" fontWeight={600} mb={1}>Decision</Typography>
                <Box
                  sx={{
                    p: 2,
                    borderRadius: 2,
                    bgcolor: ticket.claimDetails.approvalStatus === 3 ? 'success.50' : 'error.50',
                    border: '1px solid',
                    borderColor: ticket.claimDetails.approvalStatus === 3 ? 'success.light' : 'error.light',
                    display: 'flex',
                    alignItems: 'center',
                    gap: 1,
                  }}
                >
                  {ticket.claimDetails.approvalStatus === 3
                    ? <CheckIcon color="success" />
                    : <CloseIcon color="error" />}
                  <Typography
                    variant="body1"
                    fontWeight={600}
                    color={ticket.claimDetails.approvalStatus === 3 ? 'success.main' : 'error.main'}
                  >
                    Claim {ticket.claimDetails.approvalStatus === 3 ? 'Approved' : 'Rejected'}
                  </Typography>
                </Box>
              </CardContent>
            </Card>
          )}
        </Grid>
      </Grid>

      {/* PDF Viewer */}
      <Base64PdfViewer
        open={pdfViewer.open}
        base64={pdfViewer.base64}
        fileName={pdfViewer.fileName}
        onClose={closePdf}
      />
    </Box>
  );
}
