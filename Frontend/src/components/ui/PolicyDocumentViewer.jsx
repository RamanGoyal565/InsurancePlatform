import { useEffect, useMemo, useState } from 'react';
import PropTypes from 'prop-types';
import {
  Box, Button, CircularProgress, Dialog, DialogTitle,
  IconButton, Tooltip, Typography,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import DownloadIcon from '@mui/icons-material/Download';
import { getPolicyDocument } from '../../api/policies';
import { useToast } from '../../app/ToastContext';

/**
 * Full-screen PDF viewer dialog.
 * Fetches the base64 document from the backend, renders it in an iframe,
 * and provides a Download button.
 *
 * @param {{ policyId: string|null, policyName: string, onClose: () => void }} props
 */
export default function PolicyDocumentViewer({ policyId, policyName, onClose }) {
  const toast = useToast();
  const [loading, setLoading] = useState(false);
  const [base64, setBase64] = useState(null);

  // Fetch when policyId changes
  useEffect(() => {
    if (!policyId) { setBase64(null); return; }
    setLoading(true);
    setBase64(null);
    getPolicyDocument(policyId)
      .then((data) => {
        if (data?.document) setBase64(data.document);
        else toast.error('No document available for this policy');
      })
      .catch(() => toast.error('Failed to load policy document'))
      .finally(() => setLoading(false));
  }, [policyId]); // eslint-disable-line react-hooks/exhaustive-deps

  // Build a blob URL so the iframe can render it without a data: URI length limit
  const blobUrl = useMemo(() => {
    if (!base64) return null;
    try {
      const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
      const blob = new Blob([bytes], { type: 'application/pdf' });
      return URL.createObjectURL(blob);
    } catch {
      return null;
    }
  }, [base64]);

  // Revoke blob URL on unmount / change
  useEffect(() => {
    return () => { if (blobUrl) URL.revokeObjectURL(blobUrl); };
  }, [blobUrl]);

  const handleDownload = () => {
    if (!blobUrl) return;
    const a = document.createElement('a');
    a.href = blobUrl;
    a.download = `${policyName?.replace(/\s+/g, '_') || 'policy'}.pdf`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  return (
    <Dialog
      open={!!policyId}
      onClose={onClose}
      fullScreen
      PaperProps={{ sx: { bgcolor: '#1a1a2e' } }}
    >
      {/* Toolbar */}
      <DialogTitle
        sx={{
          display: 'flex', alignItems: 'center', gap: 1,
          bgcolor: '#0f3460', color: '#fff', py: 1.5, px: 2,
        }}
      >
        <Typography variant="subtitle1" fontWeight={600} sx={{ flexGrow: 1 }} noWrap>
          {policyName || 'Policy Document'}
        </Typography>

        <Tooltip title="Download PDF">
          <span>
            <Button
              variant="contained"
              size="small"
              startIcon={<DownloadIcon />}
              onClick={handleDownload}
              disabled={!blobUrl}
              sx={{ bgcolor: '#1565C0', mr: 1 }}
            >
              Download
            </Button>
          </span>
        </Tooltip>

        <Tooltip title="Close">
          <IconButton onClick={onClose} sx={{ color: '#fff' }}>
            <CloseIcon />
          </IconButton>
        </Tooltip>
      </DialogTitle>

      {/* PDF viewer area */}
      <Box sx={{ flexGrow: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', height: 'calc(100vh - 64px)' }}>
        {loading && (
          <Box sx={{ textAlign: 'center', color: '#fff' }}>
            <CircularProgress sx={{ color: '#fff' }} />
            <Typography mt={2}>Loading document...</Typography>
          </Box>
        )}

        {!loading && blobUrl && (
          <iframe
            src={blobUrl}
            title="Policy Document"
            style={{ width: '100%', height: '100%', border: 'none' }}
          />
        )}

        {!loading && !blobUrl && policyId && (
          <Typography color="grey.400">Document could not be loaded.</Typography>
        )}
      </Box>
    </Dialog>
  );
}

PolicyDocumentViewer.propTypes = {
  /** policyId to fetch — set to null to close */
  policyId: PropTypes.string,
  policyName: PropTypes.string,
  onClose: PropTypes.func.isRequired,
};
