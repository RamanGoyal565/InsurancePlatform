import { useState } from 'react';
import {
  Avatar, Box, Button, Card, CardContent, Chip, Dialog, DialogActions,
  DialogContent, DialogTitle, Divider, Grid, IconButton, MenuItem,
  Tab, Table, TableBody, TableCell, TableHead, TableRow, Tabs,
  TextField, Tooltip, Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import VisibilityIcon from '@mui/icons-material/Visibility';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import CloseIcon from '@mui/icons-material/Close';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { useTickets, useTicket, useTicketComments, useAddComment, useCreateTicket } from '../../hooks/useTickets';
import { useCustomerPolicies } from '../../hooks/usePolicies';
import StatusChip from '../../components/ui/StatusChip';
import PdfUpload from '../../components/ui/PdfUpload';
import Base64PdfViewer from '../../components/ui/Base64PdfViewer';

const TICKET_STATUS = ['', 'Open', 'InReview', 'Assigned', 'Resolved', 'Rejected', 'Closed'];
const TICKET_TYPE = ['', 'Support', 'Claim'];

const schema = yup.object({
  title: yup.string().required('Title is required'),
  description: yup.string().required('Description is required'),
  type: yup.number().required('Type is required'),
  policyId: yup.string(),
  claimAmount: yup.number().when('type', {
    is: (v) => Number(v) === 2,
    then: (s) => s.positive('Must be positive').required('Claim amount required'),
    otherwise: (s) => s.nullable(),
  }),
});

/** Read-only ticket detail dialog for customers */
function TicketDetailDialog({ ticketId, onClose }) {
  const { data: ticket } = useTicket(ticketId);
  const { data: comments = [] } = useTicketComments(ticketId);
  const addComment = useAddComment();

  const [comment, setComment] = useState('');
  const [commentDoc, setCommentDoc] = useState({ base64: null, fileName: null });
  const [pdfViewer, setPdfViewer] = useState({ open: false, base64: null, fileName: null });

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

  if (!ticket) return null;

  const isClaim = ticket.type === 2;

  return (
    <>
      <Dialog open={!!ticketId} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Typography variant="h6" fontWeight={700}>
              TKT-{ticket.ticketId?.slice(0, 8).toUpperCase()}
            </Typography>
            <StatusChip status={TICKET_STATUS[ticket.status] || String(ticket.status)} />
          </Box>
          <IconButton onClick={onClose} size="small"><CloseIcon /></IconButton>
        </DialogTitle>

        <DialogContent dividers>
          {/* Ticket info */}
          <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', mb: 2 }}>
            <Box>
              <Typography variant="caption" color="text.secondary">Type</Typography>
              <Box mt={0.5}>
                <Chip label={TICKET_TYPE[ticket.type]} size="small" color={isClaim ? 'warning' : 'info'} />
              </Box>
            </Box>
            {ticket.policyId && (
              <Box>
                <Typography variant="caption" color="text.secondary">Policy</Typography>
                <Typography variant="body2" fontWeight={600}>
                  POL-{ticket.policyId.slice(0, 8).toUpperCase()}
                </Typography>
              </Box>
            )}
            <Box>
              <Typography variant="caption" color="text.secondary">Created</Typography>
              <Typography variant="body2">{new Date(ticket.createdAt).toLocaleString()}</Typography>
            </Box>
            <Box>
              <Typography variant="caption" color="text.secondary">Last Updated</Typography>
              <Typography variant="body2">{new Date(ticket.updatedAt).toLocaleString()}</Typography>
            </Box>
          </Box>

          <Typography variant="subtitle1" fontWeight={600} mb={0.5}>{ticket.title}</Typography>
          <Typography variant="body2" color="text.secondary" mb={2}>{ticket.description}</Typography>

          {/* Ticket attachment */}
          {ticket.documentBase64 && (
            <Tooltip title="Click to view attachment">
              <Box
                sx={{
                  display: 'inline-flex', alignItems: 'center', gap: 0.5, mb: 2,
                  cursor: 'pointer', color: 'primary.main',
                  '&:hover': { textDecoration: 'underline' },
                }}
                onClick={() => setPdfViewer({ open: true, base64: ticket.documentBase64, fileName: ticket.documentFileName })}
              >
                <PictureAsPdfIcon fontSize="small" color="error" />
                <Typography variant="body2">{ticket.documentFileName || 'View Attachment'}</Typography>
              </Box>
            </Tooltip>
          )}

          {/* Claim details */}
          {isClaim && ticket.claimDetails && (
            <Box sx={{ p: 2, bgcolor: 'grey.50', borderRadius: 2, mb: 2 }}>
              <Typography variant="subtitle2" fontWeight={600} mb={1}>Claim Details</Typography>
              <Typography variant="body2">
                Claim Amount: <strong>₹{ticket.claimDetails.claimAmount?.toLocaleString()}</strong>
              </Typography>
              <Typography variant="body2">
                Approval Status:{' '}
                <strong>{['', 'Pending', 'Verified', 'Approved', 'Rejected'][ticket.claimDetails.approvalStatus]}</strong>
              </Typography>
            </Box>
          )}

          <Divider sx={{ my: 2 }} />

          {/* Comments */}
          <Typography variant="subtitle1" fontWeight={600} mb={2}>
            Comments ({comments.length})
          </Typography>

          {comments.length === 0 && (
            <Typography variant="body2" color="text.secondary" mb={2}>No comments yet.</Typography>
          )}

          {comments.map((c) => (
            <Box key={c.commentId} sx={{ display: 'flex', gap: 1.5, mb: 2 }}>
              <Avatar sx={{ width: 32, height: 32, bgcolor: 'primary.main', fontSize: 12 }}>
                {c.userId?.slice(0, 2).toUpperCase()}
              </Avatar>
              <Box sx={{ flexGrow: 1 }}>
                <Box sx={{ display: 'flex', gap: 1, alignItems: 'center' }}>
                  <Typography variant="caption" fontWeight={600}>{c.userId?.slice(0, 8)}</Typography>
                  <Typography variant="caption" color="text.secondary">
                    {new Date(c.createdAt).toLocaleString()}
                  </Typography>
                </Box>
                <Typography variant="body2" sx={{ mt: 0.25 }}>{c.message}</Typography>
                {c.documentBase64 && (
                  <Tooltip title="Click to view document">
                    <Box
                      sx={{
                        display: 'inline-flex', alignItems: 'center', gap: 0.5, mt: 0.5,
                        cursor: 'pointer', color: 'primary.main',
                        '&:hover': { textDecoration: 'underline' },
                      }}
                      onClick={() => setPdfViewer({ open: true, base64: c.documentBase64, fileName: c.documentFileName })}
                    >
                      <PictureAsPdfIcon fontSize="small" color="error" />
                      <Typography variant="caption">{c.documentFileName || 'attachment.pdf'}</Typography>
                    </Box>
                  </Tooltip>
                )}
              </Box>
            </Box>
          ))}

          <Divider sx={{ my: 2 }} />

          {/* Add comment */}
          <Typography variant="subtitle2" fontWeight={600} mb={1}>Add a Comment</Typography>
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
        </DialogContent>

        <DialogActions>
          <Button onClick={onClose}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* PDF Viewer */}
      <Base64PdfViewer
        open={pdfViewer.open}
        base64={pdfViewer.base64}
        fileName={pdfViewer.fileName}
        onClose={() => setPdfViewer({ open: false, base64: null, fileName: null })}
      />
    </>
  );
}

export default function CustomerTickets() {
  const { data: tickets = [], isLoading } = useTickets();
  const { data: policies = [] } = useCustomerPolicies();
  const createTicket = useCreateTicket();

  const [open, setOpen] = useState(false);
  const [tab, setTab] = useState(0);
  const [docBase64, setDocBase64] = useState(null);
  const [docFileName, setDocFileName] = useState(null);
  const [viewTicketId, setViewTicketId] = useState(null);

  const { register, handleSubmit, watch, reset, formState: { errors, isSubmitting } } = useForm({
    resolver: yupResolver(schema),
    defaultValues: { type: 1 },
  });

  const ticketType = watch('type');

  const handleClose = () => {
    reset();
    setDocBase64(null);
    setDocFileName(null);
    setOpen(false);
  };

  const onSubmit = async (data) => {
    await createTicket.mutateAsync({
      ...data,
      policyId: data.policyId || undefined,
      claimAmount: data.claimAmount || undefined,
      documentBase64: docBase64 || undefined,
      documentFileName: docFileName || undefined,
    });
    handleClose();
  };

  const supportTickets = tickets.filter((t) => t.type === 1);
  const claimTickets = tickets.filter((t) => t.type === 2);
  const displayed = tab === 0 ? supportTickets : claimTickets;

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>Tickets & Claims</Typography>
          <Typography variant="body2" color="text.secondary">Manage your support and claim tickets</Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => setOpen(true)}>
          Raise New Ticket
        </Button>
      </Box>

      <Card>
        <Box sx={{ borderBottom: 1, borderColor: 'divider', px: 2 }}>
          <Tabs value={tab} onChange={(_, v) => setTab(v)}>
            <Tab label={`Support Tickets (${supportTickets.length})`} />
            <Tab label={`Claim Tickets (${claimTickets.length})`} />
          </Tabs>
        </Box>
        <CardContent>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Ticket ID</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Title</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow><TableCell colSpan={6} align="center">Loading...</TableCell></TableRow>
              ) : displayed.map((t) => (
                <TableRow key={t.ticketId} hover>
                  <TableCell sx={{ color: 'primary.main', fontWeight: 600 }}>
                    TKT-{t.ticketId?.slice(0, 8).toUpperCase()}
                  </TableCell>
                  <TableCell>
                    <Chip label={TICKET_TYPE[t.type] || t.type} size="small" color={t.type === 2 ? 'warning' : 'info'} />
                  </TableCell>
                  <TableCell>{t.title}</TableCell>
                  <TableCell><StatusChip status={TICKET_STATUS[t.status] || String(t.status)} /></TableCell>
                  <TableCell>{new Date(t.createdAt).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <Tooltip title="View details & comments">
                      <IconButton size="small" color="primary" onClick={() => setViewTicketId(t.ticketId)}>
                        <VisibilityIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {displayed.length === 0 && !isLoading && (
                <TableRow>
                  <TableCell colSpan={6} align="center" sx={{ py: 3, color: 'text.secondary' }}>
                    No tickets found
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Ticket detail dialog */}
      {viewTicketId && (
        <TicketDetailDialog
          ticketId={viewTicketId}
          onClose={() => setViewTicketId(null)}
        />
      )}

      {/* Create Ticket Dialog */}
      <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
        <DialogTitle>Raise New Ticket</DialogTitle>
        <Box component="form" onSubmit={handleSubmit(onSubmit)}>
          <DialogContent>
            <Grid container spacing={2}>
              <Grid item xs={6}>
                <TextField
                  select label="Type" fullWidth
                  {...register('type')}
                  defaultValue={1}
                  error={!!errors.type}
                  helperText={errors.type?.message}
                >
                  <MenuItem value={1}>Support</MenuItem>
                  <MenuItem value={2}>Claim</MenuItem>
                </TextField>
              </Grid>
              <Grid item xs={6}>
                <TextField select label="Policy (optional)" fullWidth {...register('policyId')}>
                  <MenuItem value="">None</MenuItem>
                  {policies.map((p) => (
                    <MenuItem key={p.customerPolicyId} value={p.policyId}>{p.policyName}</MenuItem>
                  ))}
                </TextField>
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Title" fullWidth
                  {...register('title')}
                  error={!!errors.title}
                  helperText={errors.title?.message}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  label="Description" fullWidth multiline rows={3}
                  {...register('description')}
                  error={!!errors.description}
                  helperText={errors.description?.message}
                />
              </Grid>
              {Number(ticketType) === 2 && (
                <Grid item xs={12}>
                  <TextField
                    label="Claim Amount (₹)" type="number" fullWidth
                    {...register('claimAmount')}
                    error={!!errors.claimAmount}
                    helperText={errors.claimAmount?.message}
                  />
                </Grid>
              )}
              <Grid item xs={12}>
                <Typography variant="caption" color="text.secondary" display="block" mb={0.5}>
                  Attach Document (PDF, max 1 MB) — optional
                </Typography>
                <PdfUpload
                  onChange={(b64, name) => { setDocBase64(b64); setDocFileName(name); }}
                />
              </Grid>
            </Grid>
          </DialogContent>
          <DialogActions>
            <Button onClick={handleClose}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isSubmitting ? 'Submitting...' : 'Submit Ticket'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  );
}
